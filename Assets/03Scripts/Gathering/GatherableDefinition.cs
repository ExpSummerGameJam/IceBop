using System;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

[Serializable]
public sealed class GatherableDefinition
{
    public int gatherableId;
    public int ingredientId;
    public string displayName;
    public string availableSpriteResourcePath;
    public string depletedSpriteResourcePath;
    public int dropCount;
    public bool canGatherOnce;
    public string dropMode;
    public int configuredDropIngredientIdA;
    public int configuredDropIngredientIdB;
    public string memo;

    public int SafeDropCount => Mathf.Max(1, dropCount);
    public GatherableDropMode DropMode => GatherableDropModeUtility.Parse(dropMode);
}

[Serializable]
public sealed class IngredientDefinition
{
    public int ingredientId;
    public string ingredientKey;
    public string displayName;
    public string categoryKey;
    public string preparationKind;
    public string iconKey;
    public float worldDropForce = 1.75f;
    public string description;

    public float WorldDropForce => Mathf.Max(0f, worldDropForce);

    public IngredientPreparationKind PreparationKind => IngredientPreparationKindUtility.Parse(preparationKind, ingredientId);
}

[Serializable]
public sealed class IngredientCategoryDefinition
{
    public string categoryKey;
    public string displayName;
    public int sortOrder;
    public string description;
}

[Serializable]
public sealed class DishDefinition
{
    public int dishId;
    public string dishKey;
    public string displayName;
    public string dishSpriteResourcePath;
    public string requiredIngredientIds;
    public string cookingMethod;
    public string finishingStageKind;
    public int linkedAbilityId;
    public string linkedAbilityKey;
    public string abilityEffect;
    public int anchorTongueCharges;
    public int rapidFreezeCharges;
    public int accelJumpCharges;
    public string memo;

    public bool HasCookingMethod => !string.IsNullOrWhiteSpace(cookingMethod);
    public IngredientPreparationKind CookingMethod => IngredientPreparationKindUtility.Parse(cookingMethod, dishId);
    public DishFinishingStageKind FinishingStageKind => DishFinishingStageKindUtility.Parse(finishingStageKind);
    public bool HasLinkedAbility => linkedAbilityId > 0 || !string.IsNullOrWhiteSpace(linkedAbilityKey);

    public bool MatchesIngredients(System.Collections.Generic.IReadOnlyList<int> ingredientIds, bool allowAnyOrder)
    {
        int[] requiredIds = RequiredIngredientIds;
        if (requiredIds.Length == 0 || ingredientIds == null || requiredIds.Length != ingredientIds.Count)
            return false;
        
        if (!allowAnyOrder)
        {
            for (int i = 0; i < requiredIds.Length; i++)
            {
                if (requiredIds[i] != ingredientIds[i])
                    return false;
            }

            return true;
        }

        System.Collections.Generic.Dictionary<int, int> counts = new();
        foreach (int id in requiredIds)
        {
            counts.TryGetValue(id, out int count);
            counts[id] = count + 1;
        }

        foreach (int id in ingredientIds)
        {
            if (!counts.TryGetValue(id, out int count) || count <= 0)
                return false;

            if (count == 1)
                counts.Remove(id);
            else
                counts[id] = count - 1;
        }

        return counts.Count == 0;
    }

    public int[] RequiredIngredientIds
    {
        get
        {
            if (string.IsNullOrWhiteSpace(requiredIngredientIds))
                return System.Array.Empty<int>();

            string[] parts = requiredIngredientIds.Split(',', ';', '|');
            System.Collections.Generic.List<int> ids = new();
            foreach (string part in parts)
            {
                if (int.TryParse(part.Trim(), out int id))
                    ids.Add(id);
            }

            return ids.ToArray();
        }
    }
}
