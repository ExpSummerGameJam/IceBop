using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public  class GatheringData_Gatherables
{
    /// <summary>
    /// 채집물 고유 ID
    /// </summary>
    public int gatherableId;
    /// <summary>
    /// 드랍 재료 ID
    /// </summary>
    public int ingredientId;
    /// <summary>
    /// 표시 이름
    /// </summary>
    public string displayName;
    /// <summary>
    /// 채집 전 스프라이트 이름(Atlas Key)
    /// </summary>
    public string availableSpriteResourcePath;
    /// <summary>
    /// 채집 후 스프라이트 이름(Atlas Key)
    /// </summary>
    public string depletedSpriteResourcePath;
    /// <summary>
    /// 드랍 수량
    /// </summary>
    public int dropCount;
    /// <summary>
    /// 한 번만 채집 가능 여부
    /// </summary>
    public bool canGatherOnce;
    /// <summary>
    /// 드랍 모드
    /// </summary>
    public string dropMode;
    /// <summary>
    /// 설정 드랍 재료 ID A
    /// </summary>
    public int configuredDropIngredientIdA;
    /// <summary>
    /// 설정 드랍 재료 ID B
    /// </summary>
    public int configuredDropIngredientIdB;
    /// <summary>
    /// 메모
    /// </summary>
    public string memo;
}
