using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public  class CookingData_Ingredients
{
    /// <summary>
    /// 재료 고유 ID
    /// </summary>
    public int ingredientId;
    /// <summary>
    /// 코드 키
    /// </summary>
    public string ingredientKey;
    /// <summary>
    /// 표시 이름
    /// </summary>
    public string displayName;
    /// <summary>
    /// 카테고리 키
    /// </summary>
    public string categoryKey;
    /// <summary>
    /// 등급
    /// </summary>
    public int grade;
    /// <summary>
    /// 초기 해금 여부
    /// </summary>
    public bool isDefaultUnlocked;
    /// <summary>
    /// 아이콘/스프라이트 키
    /// </summary>
    public string iconKey;
    /// <summary>
    /// 맛/성향 태그
    /// </summary>
    public string flavorTag;
    /// <summary>
    /// 월드 드랍 힘
    /// </summary>
    public float worldDropForce;
    /// <summary>
    /// 메모
    /// </summary>
    public string memo;
    /// <summary>
    /// 손질 방식
    /// </summary>
    public string preparationKind;
    /// <summary>
    /// 재료 구분
    /// </summary>
    public string ingredientRole;
    /// <summary>
    /// 관련 능력 키
    /// </summary>
    public string abilityKey;
    /// <summary>
    /// 관련 능력 표시명
    /// </summary>
    public string abilityDisplayName;
    /// <summary>
    /// 등장 지역
    /// </summary>
    public string region;
    /// <summary>
    /// 상세 설명
    /// </summary>
    public string description;
}
