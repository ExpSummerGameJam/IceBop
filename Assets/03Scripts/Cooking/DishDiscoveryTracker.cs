using System.Collections.Generic;
using UnityEngine;

public static class DishDiscoveryTracker
{
    private const string PrefsKey = "IceBop.DiscoveredDishes";

    private static HashSet<int> discoveredDishIds;

    public static void EnsureInitialized()
    {
        EnsureLoaded();
    }

    public static bool HasDiscovered(int dishId)
    {
        if (dishId <= 0)
            return false;

        EnsureLoaded();
        return discoveredDishIds.Contains(dishId);
    }

    private static void EnsureLoaded()
    {
        if (discoveredDishIds != null)
            return;

        discoveredDishIds = new HashSet<int>();

        string saved = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(saved))
        {
            foreach (string part in saved.Split(','))
            {
                if (int.TryParse(part, out int dishId))
                    discoveredDishIds.Add(dishId);
            }
        }

        CookingSystem.DishCrafted += OnDishCrafted;
    }

    private static void OnDishCrafted(CookingCraftResult result)
    {
        if (result == null || result.dishId <= 0)
            return;

        EnsureLoaded();

        if (!discoveredDishIds.Add(result.dishId))
            return;

        PlayerPrefs.SetString(PrefsKey, string.Join(",", discoveredDishIds));
        PlayerPrefs.Save();
    }
}
