public static class GatherableDropModeUtility
{
    public static GatherableDropMode Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return GatherableDropMode.DataIngredient;

        string normalized = value.Trim().Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);

        if (int.TryParse(normalized, out int index) &&
            System.Enum.IsDefined(typeof(GatherableDropMode), index))
            return (GatherableDropMode)index;

        if (System.Enum.TryParse(normalized, true, out GatherableDropMode parsed))
            return parsed;

        if (string.Equals(normalized, "random", System.StringComparison.OrdinalIgnoreCase))
            return GatherableDropMode.RandomFromConfiguredIngredients;

        if (string.Equals(normalized, "all", System.StringComparison.OrdinalIgnoreCase))
            return GatherableDropMode.AllConfiguredIngredients;

        return GatherableDropMode.DataIngredient;
    }
}
