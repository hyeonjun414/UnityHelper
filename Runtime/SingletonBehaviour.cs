using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public abstract class SingletonMonoBehavior<T> : MonoBehaviour where T : MonoBehaviour
{
    /***********************************************************************
    *                       Public Static Properties
    ***********************************************************************/
    #region .
    /// <summary> �̱��� �ν��Ͻ� Getter </summary>
    public static T I
    {
        get
        {
            // ��ü ���� Ȯ��
            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();

                // �ν��Ͻ� ���� ������Ʈ�� �������� ���� ���, �� ������Ʈ�� ���Ƿ� �����Ͽ� �ν��Ͻ� �Ҵ�
                if (_instance == null)
                {
                    // ���� ������Ʈ�� Ŭ���� ������Ʈ �߰� �� �ν��Ͻ� �Ҵ�
                    _instance = ContainerObject.GetComponent<T>();
                }
            }
            return _instance;
        }
    }

    /// <summary> �̱��� �ν��Ͻ� Getter </summary>
    public static T Instance => I;

    /// <summary> �̱��� ���ӿ�����Ʈ�� ���� </summary>
    public static GameObject ContainerObject
    {
        get
        {
            if (_containerObject == null)
                CreateContainerObject();

            return _containerObject;
        }
    }

    #endregion
    /***********************************************************************
    *                       Private Static Variables
    ***********************************************************************/
    #region .

    /// <summary> �̱��� �ν��Ͻ� </summary>
    private static T _instance;
    private static GameObject _containerObject;

    #endregion
    /***********************************************************************
    *                       Private Static Methods
    ***********************************************************************/
    #region .
    [System.Diagnostics.Conditional("DEBUG_ON")]
    protected static void DebugOnlyLog(string msg)
    {
        Debug.Log(msg);
    }

    /// <summary> ���� �θ� ���ӿ�����Ʈ�� ����ֱ� </summary>
    [System.Diagnostics.Conditional("GATHER_INTO_SAME_PARENT")]
    protected static void GatherGameObjectIntoSameParent()
    {
        string parentName = "Singleton Objects";

        // ���ӿ�����Ʈ "Singleton Objects" ã�� or ����
        GameObject parentContainer = GameObject.Find(parentName);
        if (parentContainer == null)
            parentContainer = new GameObject(parentName);

        // �θ� ������Ʈ�� �־��ֱ�
        _containerObject.transform.SetParent(parentContainer.transform);
    }

    // (����) �̱��� ������Ʈ�� ���� ���� ������Ʈ ����
    private static void CreateContainerObject()
    {
        // null�� �ƴϸ� Do Nothing
        if (_containerObject != null) return;

        // �� ���� ������Ʈ ����
        _containerObject = new GameObject($"[Singleton] {typeof(T)}");

        // �ν��Ͻ��� ���� ���, ���� ����
        if (_instance == null)
            _instance = ContainerObject.AddComponent<T>();

        GatherGameObjectIntoSameParent();
    }

    #endregion

    protected virtual void Awake()
    {
        // �̱��� �ν��Ͻ��� �̸� �������� �ʾ��� ���, �������� �ʱ�ȭ
        if (_instance == null)
        {
            DebugOnlyLog($"�̱��� ���� : {typeof(T)}, ���� ������Ʈ : {name}");

            // �̱��� ������Ʈ �ʱ�ȭ
            _instance = this as T;

            // �̱��� ������Ʈ�� ��� �ִ� ���ӿ�����Ʈ�� �ʱ�ȭ
            _containerObject = gameObject;

            GatherGameObjectIntoSameParent();
        }

        // �̱��� �ν��Ͻ��� �����ϴµ�, ������ �ƴ� ���, ������(������Ʈ)�� �ı�
        if (_instance != null && _instance != this)
        {
            DebugOnlyLog($"�̹� {typeof(T)} �̱����� �����ϹǷ� ������Ʈ�� �ı��մϴ�.");

            var components = gameObject.GetComponents<Component>();

            // ���� ���� ������Ʈ�� ������Ʈ�� �ڽŸ� �־��ٸ�, ���� ������Ʈ�� �ı�
            if (components.Length <= 2)
                Destroy(gameObject);

            // �ٸ� ������Ʈ�� �����ϸ� �ڽŸ� �ı�
            else
                Destroy(this);
        }
    }
}