using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public  class CookingData_Recipes
{
    /// <summary>
    /// 레시피 ID
    /// </summary>
    public int recipeId;
    /// <summary>
    /// 레시피 키
    /// </summary>
    public string recipeKey;
    /// <summary>
    /// 조합법
    /// </summary>
    public string combinationText;
    /// <summary>
    /// 요리 ID
    /// </summary>
    public int dishId;
    /// <summary>
    /// 요리 키
    /// </summary>
    public string dishKey;
    /// <summary>
    /// 요리명
    /// </summary>
    public string dishName;
    /// <summary>
    /// 필요 재료 ID 목록
    /// </summary>
    public string requiredIngredientIds;
    /// <summary>
    /// 필요 재료명
    /// </summary>
    public string requiredIngredientNames;
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
    /// 부가 설명
    /// </summary>
    public string additionalDescription;
    /// <summary>
    /// 레시피 위치
    /// </summary>
    public string recipeLocation;
    /// <summary>
    /// 특수 조리 방법
    /// </summary>
    public string specialCookingMethod;
    /// <summary>
    /// 정확 조합 여부
    /// </summary>
    public bool isExactRecipe;
}
