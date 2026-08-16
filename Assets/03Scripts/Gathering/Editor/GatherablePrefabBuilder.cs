using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class GatherablePrefabBuilder
{
    private const string RootFolder = "Assets/01Resources/Prefabs/Gathering";
    private const string UniqueMaterialsFolder = RootFolder + "/UniqueMaterials";
    private const string EnhancementMaterialsFolder = RootFolder + "/EnhancementMaterials";

    [MenuItem("IceBop/Build Gatherable Prefabs")]
    public static void BuildGatherablePrefabs()
    {
        GatheringDataLookup.ClearCache();
        EnsureFolder(UniqueMaterialsFolder);
        EnsureFolder(EnhancementMaterialsFolder);

        List<GatherableDefinition> gatherables = GatheringDataLookup.GetGatherables()
            .Where(definition => definition != null)
            .OrderBy(definition => definition.gatherableId)
            .ToList();

        foreach (GatherableDefinition definition in gatherables.Where(IsUniqueMaterialGatherable))
            BuildUniqueMaterialPrefab(definition);

        List<GatherableDefinition> enhancementDefinitions = gatherables
            .Where(IsEnhancementMaterialGatherable)
            .ToList();

        for (int i = 0; i < enhancementDefinitions.Count; i += 2)
        {
            GatherableDefinition first = enhancementDefinitions[i];
            GatherableDefinition second = i + 1 < enhancementDefinitions.Count ? enhancementDefinitions[i + 1] : null;
            BuildEnhancementMaterialPrefab(first, second, i / 2 + 1);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void BuildUniqueMaterialPrefab(GatherableDefinition definition)
    {
        string key = GetIngredientKey(definition);
        string prefabName = $"Gatherable_{definition.gatherableId}_{key}";
        string prefabPath = $"{UniqueMaterialsFolder}/{prefabName}.prefab";

        GameObject root = CreateGatherableRoot(prefabName, definition.gatherableId);
        GatherableObject gatherable = root.GetComponent<GatherableObject>();
        gatherable.ConfigureDropIngredients(GatherableDropMode.DataIngredient, 0, 0);
        DisableAutoApplySprites(gatherable);

        SavePrefab(root, prefabPath);
    }

    private static void BuildEnhancementMaterialPrefab(
        GatherableDefinition first,
        GatherableDefinition second,
        int groupIndex)
    {
        string firstKey = GetIngredientKey(first);
        string secondKey = second != null ? GetIngredientKey(second) : "empty";
        string prefabName = $"EnhancementGatherable_Stage{groupIndex}_{firstKey}_{secondKey}";
        string prefabPath = $"{EnhancementMaterialsFolder}/{prefabName}.prefab";

        GameObject root = CreateGatherableRoot(prefabName, first.gatherableId);
        GatherableObject gatherable = root.GetComponent<GatherableObject>();

        GatherableDropMode dropMode = first.DropMode != GatherableDropMode.DataIngredient
            ? first.DropMode
            : GatherableDropMode.RandomFromConfiguredIngredients;
        int ingredientIdA = first.configuredDropIngredientIdA > 0 ? first.configuredDropIngredientIdA : first.ingredientId;
        int ingredientIdB = first.configuredDropIngredientIdB > 0
            ? first.configuredDropIngredientIdB
            : (second != null ? second.ingredientId : 0);

        gatherable.ConfigureDropIngredients(dropMode, ingredientIdA, ingredientIdB);
        DisableAutoApplySprites(gatherable);

        SavePrefab(root, prefabPath);
    }

    private static GameObject CreateGatherableRoot(string name, int gatherableId)
    {
        GameObject root = new(name);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        CircleCollider2D trigger = root.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.9f;

        GameObject available = CreateStateVisual(root.transform, "Available", gatherableId, false);
        GameObject depleted = CreateStateVisual(root.transform, "Depleted", gatherableId, true);
        depleted.SetActive(false);

        GatherableObject gatherable = root.AddComponent<GatherableObject>();
        gatherable.Configure(
            gatherableId,
            available,
            depleted,
            available.GetComponent<SpriteRenderer>(),
            depleted.GetComponent<SpriteRenderer>());

        return root;
    }

    private static GameObject CreateStateVisual(Transform parent, string name, int gatherableId, bool depleted)
    {
        GameObject visual = new(name);
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = depleted
            ? new Vector3(0.55f, 0.35f, 1f)
            : new Vector3(0.75f, 0.75f, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.color = depleted
            ? new Color(0.28f, 0.27f, 0.25f, 1f)
            : InventoryVisualUtility.ColorFromId(gatherableId);
        renderer.sortingOrder = 10;

        return visual;
    }

    private static void DisableAutoApplySprites(GatherableObject gatherable)
    {
        SerializedObject serializedObject = new(gatherable);
        serializedObject.FindProperty("autoApplySprites").boolValue = false;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SavePrefab(GameObject root, string prefabPath)
    {
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
    }

    private static string GetIngredientKey(GatherableDefinition definition)
    {
        if (GatheringDataLookup.TryGetIngredient(definition.ingredientId, out IngredientDefinition ingredient) &&
            !string.IsNullOrWhiteSpace(ingredient.ingredientKey))
            return SanitizeFileName(ingredient.ingredientKey);

        string fileName = Path.GetFileName(definition.availableSpriteResourcePath);
        if (!string.IsNullOrWhiteSpace(fileName))
            return SanitizeFileName(fileName);

        return $"id_{definition.gatherableId}";
    }

    private static string SanitizeFileName(string value)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string sanitized = new(value
            .Trim()
            .Select(character => invalidChars.Contains(character) ? '_' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
    }

    private static bool IsUniqueMaterialGatherable(GatherableDefinition definition)
    {
        return definition.gatherableId >= 3100 && definition.gatherableId < 3200;
    }

    private static bool IsEnhancementMaterialGatherable(GatherableDefinition definition)
    {
        return definition.gatherableId >= 3200 && definition.gatherableId < 3300;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string child = Path.GetFileName(folderPath);

        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(child))
            return;

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, child);
    }
}
