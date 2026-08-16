using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public  class CookingData_IngredientCategories
{
    /// <summary>
    /// 카테고리 식별자
    /// </summary>
    public string categoryKey;
    /// <summary>
    /// 표시 이름
    /// </summary>
    public string displayName;
    /// <summary>
    /// 정렬 순서
    /// </summary>
    public int sortOrder;
    /// <summary>
    /// 설명
    /// </summary>
    public string description;
}
