using UnityEngine;

public static class IngredientPreparationKindUtility
{
    public static IngredientPreparationKind Parse(string value, int fallbackSeed)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            string normalized = value.Trim().Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);
            if (int.TryParse(normalized, out int index) &&
                System.Enum.IsDefined(typeof(IngredientPreparationKind), index))
                return (IngredientPreparationKind)index;

            if (System.Enum.TryParse(normalized, true, out IngredientPreparationKind parsed))
                return parsed;

            if (string.Equals(normalized, "wipe", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "dustwipe", System.StringComparison.OrdinalIgnoreCase))
                return IngredientPreparationKind.DustWiping;

            if (string.Equals(normalized, "cut", System.StringComparison.OrdinalIgnoreCase))
                return IngredientPreparationKind.Cutting;

            if (string.Equals(normalized, "grind", System.StringComparison.OrdinalIgnoreCase))
                return IngredientPreparationKind.Grinding;
        }

        return (IngredientPreparationKind)(Mathf.Abs(fallbackSeed) % 3);
    }
}
