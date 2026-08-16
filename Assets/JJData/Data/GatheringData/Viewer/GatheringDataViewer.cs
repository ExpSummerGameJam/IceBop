using UnityEngine;




public  class GatheringDataViewer : MonoBehaviour
{
    public GatheringData_Gatherables[] GatherablesDataViewer;
    void Start()
    {
        GatherablesDataViewer = GatheringData.Gatherables;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateViewer()
    {
        GameObject viewer = new GameObject($"GatheringDataViewer");
        viewer.AddComponent<GatheringDataViewer>();
        GameObject.DontDestroyOnLoad(viewer);
    }
}
