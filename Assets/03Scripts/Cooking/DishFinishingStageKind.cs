public enum DishFinishingStageKind
{
    Stir = 0,
    BasketShake = 1,
    FryerSaltSprinkle = 2,
    FryerSaltSqueeze = 3,
    SkewerAssembly = 4,
    GuidedPlacement = 5,
}

public static class DishFinishingStageKindUtility
{
    public static DishFinishingStageKind Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DishFinishingStageKind.Stir;

        string normalized = value.Trim().Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);

        if (int.TryParse(normalized, out int index) &&
            System.Enum.IsDefined(typeof(DishFinishingStageKind), index))
            return (DishFinishingStageKind)index;

        if (System.Enum.TryParse(normalized, true, out DishFinishingStageKind parsed))
            return parsed;

        return DishFinishingStageKind.Stir;
    }
}
