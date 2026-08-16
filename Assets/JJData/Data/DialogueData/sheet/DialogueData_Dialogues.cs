using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public  class DialogueData_Dialogues
{
    /// <summary>
    /// 대화 고유 ID
    /// </summary>
    public int dialogueId;
    /// <summary>
    /// 장면 키
    /// </summary>
    public string sceneKey;
    /// <summary>
    /// 대사 순서
    /// </summary>
    public int lineOrder;
    /// <summary>
    /// 화자
    /// </summary>
    public string speaker;
    /// <summary>
    /// 대사 내용
    /// </summary>
    public string lineText;
}
