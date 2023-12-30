using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System;
using System.Net;
using System.IO;
using System.Linq;
using System.Threading;
#if UNITY_EDITOR
public class GSpreadSheetsToCSV : EditorWindow
{

    private string CLIENT_ID = "871414866606-7b9687cp1ibjokihbbfl6nrjr94j14o8.apps.googleusercontent.com";
    private string CLIENT_SECRET = "zF_J3qHpzX5e8i2V-ZEvOdGV";
    private string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
    // ���������Ʈ Ű
    private string spreadSheetKey = "1dbVjFBgRVgdH-w6YiV_pPnQyLSrz2WeM5I2QC_Rj7lg";

    // JSON���� ��ȯ�� ��Ʈ �̸� ���
    private string appName = "Unity";
    // JSON ������ ������ ���͸�
    private string outputDir = "./DataTable/";

    // ��ũ�� �� ��ġ
    private Vector2 scrollPosition;

    // �����
    private float progress = 100;

    // ����� �޽���
    private string progressMessage = "";

    [MenuItem("Utility/GSheet to CSV")]
    private static void ShowWindow()
    {
        GSpreadSheetsToCSV window = EditorWindow.GetWindow(typeof(GSpreadSheetsToCSV)) as GSpreadSheetsToCSV;
    }

    public void Init()
    {
        progress = 100;
        progressMessage = "";
        ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
    }

    private void OnGUI()
    {
        Init();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUI.skin.scrollView);
        GUILayout.BeginVertical();
        {
            GUILayout.Label("����", EditorStyles.boldLabel);
            //spreadSheetKey = EditorGUILayout.TextField("�������� ��Ʈ Ű", spreadSheetKey);
            //outputDir = EditorGUILayout.TextField("CSV ���� ���� ���", outputDir);

            GUI.backgroundColor = UnityEngine.Color.green;
            if (GUILayout.Button("������ �ٿ�ε� \n �׸��� CSV ���Ϸ� ��ȯ"))
            {
                progress = 0;
                DownloadToJson();
            }
            GUI.backgroundColor = UnityEngine.Color.white;
            if ((progress < 100) && (progress > 0))
            {
                if (EditorUtility.DisplayCancelableProgressBar("ó�� ��", progressMessage, progress / 100))
                {
                    progress = 100;
                    EditorUtility.ClearProgressBar();
                }
            }
            else
            {
                EditorUtility.ClearProgressBar();
            }
        }
        try
        {
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void DownloadToJson()
    {
        // �Է� ��ȿ�� �˻�
        if (string.IsNullOrEmpty(spreadSheetKey))
        {
            Debug.LogError("���������Ʈ Ű�� �Է��ϼ���!");
            return;
        }

        Debug.Log("Ű: " + spreadSheetKey + "�� �ٿ�ε� ����");

        // ����
        progressMessage = "���� ��...";
        var service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = GetCredential(),
            ApplicationName = appName,
        });

        progress = 5;
        EditorUtility.DisplayCancelableProgressBar("ó�� ��", progressMessage, progress / 100);
        progressMessage = "���������Ʈ ��� �������� ��...";
        EditorUtility.DisplayCancelableProgressBar("ó�� ��", progressMessage, progress / 100);

        Spreadsheet spreadSheetData = service.Spreadsheets.Get(spreadSheetKey).Execute();
        IList<Sheet> sheets = spreadSheetData.Sheets;

        if ((sheets == null) || (sheets.Count <= 0))
        {
            Debug.LogError("�����͸� ã�� �� �����ϴ�!");
            progress = 100;
            EditorUtility.ClearProgressBar();
            return;
        }

        progress = 15;

        List<string> ranges = new List<string>();
        foreach (Sheet sheet in sheets)
        {
            if (!sheet.Properties.Title.StartsWith('~'))
                ranges.Add(sheet.Properties.Title);
        }


        SpreadsheetsResource.ValuesResource.BatchGetRequest request = service.Spreadsheets.Values.BatchGet(spreadSheetKey);
        request.Ranges = ranges;
        BatchGetValuesResponse response = request.Execute();
        var jsonDic = new Dictionary<object, object>();
        foreach (ValueRange valueRange in response.ValueRanges)
        {
            string Sheetname = valueRange.Range.Split('!')[0];
            progressMessage = string.Format("{0} ó�� ��...", Sheetname);
            EditorUtility.DisplayCancelableProgressBar("ó�� ��", progressMessage, progress / 100);
            //Create json file
            CreateCsvFile(Sheetname, valueRange);
            progress += 85 / (response.ValueRanges.Count);
        }
        progress = 100;

        AssetDatabase.Refresh();

        Debug.Log("�ٿ�ε� �Ϸ�");
    }
    private void CreateCsvFile(string fileName, ValueRange valueRange)
    {
        string csvString = "";
        var maxCount = valueRange.Values.Max(t => t.Count);
        for (var i = 0; i < valueRange.Values.Count; i++)
        {
            for (var col = 0; col < maxCount; col++)
            {
                if (col < valueRange.Values[i].Count)
                {
                    var str = (string)valueRange.Values[i][col];
                    if (str.Contains(","))
                    {
                        str = str.Replace(",", "#c");
                        str = '\"' + str + '\"';
                    }
                    else if (str.Contains("\r\n") || str.Contains("\n"))
                    {
                        str = '\"' + str + '\"';
                    }

                    csvString += str;
                }

                if (col != maxCount - 1)
                {
                    csvString += ',';
                }
            }
            csvString += "\n";
        }
        StreamWriter strmWriter = new StreamWriter(outputDir + fileName + ".csv", false, System.Text.Encoding.UTF8);
        strmWriter.Write(csvString);
        strmWriter.Close();

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

        Debug.Log(fileName + ".csv ������ �����߽��ϴ�.");
    }


    private UserCredential GetCredential()
    {
        string credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        UserCredential credential = null;
        ClientSecrets clientSecrets = new ClientSecrets();
        clientSecrets.ClientId = CLIENT_ID;
        clientSecrets.ClientSecret = CLIENT_SECRET;

        try
        {
            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true)).Result;
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }

        return credential;
    }



    public bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        bool isOk = true;
        // If there are errors in the certificate chain, look at each error to determine the cause.
        if (sslPolicyErrors != SslPolicyErrors.None)
        {
            for (int i = 0; i < chain.ChainStatus.Length; i++)
            {
                if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
                {
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    bool chainIsValid = chain.Build((X509Certificate2)certificate);
                    if (!chainIsValid)
                    {
                        Debug.LogError("certificate chain is not valid");
                        isOk = false;
                    }
                }
            }
        }
        return isOk;
    }
}
#endif

