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
    // 스프레드시트 키
    private string spreadSheetKey = "1dbVjFBgRVgdH-w6YiV_pPnQyLSrz2WeM5I2QC_Rj7lg";

    // JSON으로 변환할 시트 이름 목록
    private string appName = "Unity";
    // JSON 파일을 저장할 디렉터리
    private string outputDir = "./DataTable/";

    // 스크롤 뷰 위치
    private Vector2 scrollPosition;

    // 진행률
    private float progress = 100;

    // 진행률 메시지
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
            GUILayout.Label("설정", EditorStyles.boldLabel);
            //spreadSheetKey = EditorGUILayout.TextField("스프레드 시트 키", spreadSheetKey);
            //outputDir = EditorGUILayout.TextField("CSV 파일 저장 경로", outputDir);

            GUI.backgroundColor = UnityEngine.Color.green;
            if (GUILayout.Button("데이터 다운로드 \n 그리고 CSV 파일로 변환"))
            {
                progress = 0;
                DownloadToJson();
            }
            GUI.backgroundColor = UnityEngine.Color.white;
            if ((progress < 100) && (progress > 0))
            {
                if (EditorUtility.DisplayCancelableProgressBar("처리 중", progressMessage, progress / 100))
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
        // 입력 유효성 검사
        if (string.IsNullOrEmpty(spreadSheetKey))
        {
            Debug.LogError("스프레드시트 키를 입력하세요!");
            return;
        }

        Debug.Log("키: " + spreadSheetKey + "로 다운로드 시작");

        // 인증
        progressMessage = "인증 중...";
        var service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = GetCredential(),
            ApplicationName = appName,
        });

        progress = 5;
        EditorUtility.DisplayCancelableProgressBar("처리 중", progressMessage, progress / 100);
        progressMessage = "스프레드시트 목록 가져오는 중...";
        EditorUtility.DisplayCancelableProgressBar("처리 중", progressMessage, progress / 100);

        Spreadsheet spreadSheetData = service.Spreadsheets.Get(spreadSheetKey).Execute();
        IList<Sheet> sheets = spreadSheetData.Sheets;

        if ((sheets == null) || (sheets.Count <= 0))
        {
            Debug.LogError("데이터를 찾을 수 없습니다!");
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
            progressMessage = string.Format("{0} 처리 중...", Sheetname);
            EditorUtility.DisplayCancelableProgressBar("처리 중", progressMessage, progress / 100);
            //Create json file
            CreateCsvFile(Sheetname, valueRange);
            progress += 85 / (response.ValueRanges.Count);
        }
        progress = 100;

        AssetDatabase.Refresh();

        Debug.Log("다운로드 완료");
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

        Debug.Log(fileName + ".csv 파일을 생성했습니다.");
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

