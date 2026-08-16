using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public  class CookingData_RecipeSteps
{
    /// <summary>
    /// 레시피 ID
    /// </summary>
    public int recipeId;
    /// <summary>
    /// 단계 순서
    /// </summary>
    public int stepOrder;
    /// <summary>
    /// 액션명
    /// </summary>
    public string actionName;
    /// <summary>
    /// 대상 재료 ID 목록
    /// </summary>
    public string targetIngredientIds;
    /// <summary>
    /// 손질 변경
    /// </summary>
    public string preparationOverride;
    /// <summary>
    /// 단계 설명
    /// </summary>
    public string description;
}
