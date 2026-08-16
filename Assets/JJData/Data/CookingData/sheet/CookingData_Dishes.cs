using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public  class CookingData_Dishes
{
    /// <summary>
    /// 완성 요리 ID
    /// </summary>
    public int dishId;
    /// <summary>
    /// 코드 키
    /// </summary>
    public string dishKey;
    /// <summary>
    /// 표시 이름
    /// </summary>
    public string displayName;
    /// <summary>
    /// 요리 스프라이트 이름(Atlas Key)
    /// </summary>
    public string dishSpriteResourcePath;
    /// <summary>
    /// 필요 재료 ID 목록
    /// </summary>
    public string requiredIngredientIds;
    /// <summary>
    /// 메모
    /// </summary>
    public string memo;
    /// <summary>
    /// 능력 효과
    /// </summary>
    public string abilityEffect;
    /// <summary>
    /// 앵커텅 충전
    /// </summary>
    public int anchorTongueCharges;
    /// <summary>
    /// 급속빙결 충전
    /// </summary>
    public int rapidFreezeCharges;
    /// <summary>
    /// 악셀점프 충전
    /// </summary>
    public int accelJumpCharges;
    /// <summary>
    /// 레시피 위치
    /// </summary>
    public string recipeLocation;
    /// <summary>
    /// 특수 조리 방법
    /// </summary>
    public string specialCookingMethod;
    /// <summary>
    /// 조리 완성 단계 종류(코드 키)
    /// </summary>
    public string finishingStageKind;
}
