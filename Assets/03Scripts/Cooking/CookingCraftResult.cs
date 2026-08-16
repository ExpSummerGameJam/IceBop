using System;
using System.Collections.Generic;

[Serializable]
public sealed class CookingCraftResult
{
    public DishDefinition dish;
    public int dishId;
    public string dishKey;
    public string displayName;
    public IngredientPreparationKind cookingMethod;
    public int linkedAbilityId;
    public string linkedAbilityKey;
    public List<CookingIngredientEntry> ingredients = new();

    public bool HasLinkedAbility => linkedAbilityId > 0 || !string.IsNullOrWhiteSpace(linkedAbilityKey);

    public static CookingCraftResult Create(
        DishDefinition dish,
        IReadOnlyList<CookingIngredientEntry> sourceIngredients,
        IngredientPreparationKind cookingMethod)
    {
        CookingCraftResult result = new()
        {
            dish = dish,
            dishId = dish != null ? dish.dishId : 0,
            dishKey = dish != null ? dish.dishKey : string.Empty,
            displayName = dish != null && !string.IsNullOrWhiteSpace(dish.displayName) ? dish.displayName : "Mixed Dish",
            cookingMethod = cookingMethod,
            linkedAbilityId = dish != null ? dish.linkedAbilityId : 0,
            linkedAbilityKey = dish != null ? dish.linkedAbilityKey : string.Empty
        };

        if (sourceIngredients != null)
            result.ingredients.AddRange(sourceIngredients);

        return result;
    }
}
