using System;
using System.Collections.Generic;
using UnityEngine;

public class CookingSystem : MonoBehaviour
{
    public static event Action<CookingCraftResult> DishCrafted;

    public static void StartCooking(
        IReadOnlyList<CookingIngredientEntry> ingredients,
        Action<CookingCraftResult> onCompleted = null)
    {
        CookingFlowController2D.StartCookingWithResult(ingredients, onCompleted);
    }

    public static bool TryGetLinkedAbility(
        DishDefinition dish,
        out int abilityId,
        out string abilityKey)
    {
        abilityId = dish != null ? dish.linkedAbilityId : 0;
        abilityKey = dish != null ? dish.linkedAbilityKey : string.Empty;
        return abilityId > 0 || !string.IsNullOrWhiteSpace(abilityKey);
    }

    internal static void NotifyDishCrafted(CookingCraftResult result)
    {
        if (result == null)
            return;

        DishCrafted?.Invoke(result);
    }
}
