using System;
using UnityEngine;

[Serializable]
public sealed class CookingIngredientEntry
{
    public int ingredientId;
    public IngredientDefinition ingredient;
    public IngredientPreparationKind preparationKind;
    public Sprite ingredientSprite;
    public Color ingredientColor = Color.white;
    public Vector3 ingredientScale = Vector3.one;

    public string DisplayName =>
        ingredient != null && !string.IsNullOrWhiteSpace(ingredient.displayName)
            ? ingredient.displayName
            : $"Ingredient {ingredientId}";

    public static CookingIngredientEntry FromDefinition(IngredientDefinition definition)
    {
        Sprite sprite = InventoryVisualUtility.GetIngredientSprite(definition);
        return new CookingIngredientEntry
        {
            ingredientId = definition != null ? definition.ingredientId : 0,
            ingredient = definition,
            preparationKind = definition != null ? definition.PreparationKind : IngredientPreparationKind.Cutting,
            ingredientSprite = sprite,
            ingredientColor = sprite != null || definition == null
                ? Color.white
                : InventoryVisualUtility.ColorFromId(definition.ingredientId),
            ingredientScale = Vector3.one
        };
    }

    public static CookingIngredientEntry FromInventoryItem(InventoryItem2D item, IngredientDefinition definition)
    {
        CookingIngredientEntry entry = FromDefinition(definition);
        if (item == null)
            return entry;

        SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
        if (renderer == null)
            return entry;

        entry.ingredientSprite = renderer.sprite;
        entry.ingredientColor = renderer.color;
        entry.ingredientScale = item.transform.localScale;
        return entry;
    }
}
