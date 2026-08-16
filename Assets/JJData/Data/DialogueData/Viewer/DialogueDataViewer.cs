using UnityEngine;




public  class DialogueDataViewer : MonoBehaviour
{
    public DialogueData_Dialogues[] DialoguesDataViewer;
    void Start()
    {
        DialoguesDataViewer = DialogueData.Dialogues;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateViewer()
    {
        GameObject viewer = new GameObject($"DialogueDataViewer");
        viewer.AddComponent<DialogueDataViewer>();
        GameObject.DontDestroyOnLoad(viewer);
    }
}
