using UnityEngine;




public  class DataBaseNameViewer : MonoBehaviour
{
    public DataBaseName_TestClass[] TestClassDataViewer;
    void Start()
    {
        TestClassDataViewer = DataBaseName.TestClass;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateViewer()
    {
        GameObject viewer = new GameObject($"DataBaseNameViewer");
        viewer.AddComponent<DataBaseNameViewer>();
        GameObject.DontDestroyOnLoad(viewer);
    }
}
