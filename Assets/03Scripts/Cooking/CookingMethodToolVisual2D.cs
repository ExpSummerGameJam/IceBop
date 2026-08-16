using System;
using UnityEngine;

[Serializable]
public sealed class CookingMethodToolVisual2D
{
    public IngredientPreparationKind cookingMethod;
    public GameObject preparationBoardPrefab;
    public Sprite preparationBoardSprite;
    public Vector3 preparationBoardScale;
    public GameObject preparationToolPrefab;
    public Sprite preparationToolSprite;
    public Vector3 preparationToolScale;
    public GameObject mixingPotPrefab;
    public Sprite mixingPotBodySprite;
    public GameObject mixingToolPrefab;
    public Sprite mixingLadleSprite;
    public Vector3 mixingPotScale;
    public Vector3 mixingToolScale;
}
