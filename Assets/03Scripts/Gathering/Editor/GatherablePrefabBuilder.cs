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

        int stageIndex = 0;
        foreach (GatherableDefinition definition in gatherables.Where(IsEnhancementMaterialGatherable))
            BuildEnhancementMaterialPrefab(definition, ++stageIndex);

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

    private static void BuildEnhancementMaterialPrefab(GatherableDefinition definition, int stageIndex)
    {
        GatherableDropMode dropMode = definition.DropMode != GatherableDropMode.DataIngredient
            ? definition.DropMode
            : GatherableDropMode.AllConfiguredIngredients;
        int ingredientIdA = definition.configuredDropIngredientIdA;
        int ingredientIdB = definition.configuredDropIngredientIdB;

        string firstKey = GetIngredientKeyById(ingredientIdA, definition);
        string secondKey = ingredientIdB > 0 ? GetIngredientKeyById(ingredientIdB, definition) : "empty";
        string prefabName = $"EnhancementGatherable_Stage{stageIndex}_{firstKey}_{secondKey}";
        string prefabPath = $"{EnhancementMaterialsFolder}/{prefabName}.prefab";

        GameObject root = CreateGatherableRoot(prefabName, definition.gatherableId);
        GatherableObject gatherable = root.GetComponent<GatherableObject>();
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

        GameObject available = CreateStateVisual(root.transform, "Available");
        GameObject depleted = CreateStateVisual(root.transform, "Depleted");
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

    private static GameObject CreateStateVisual(Transform parent, string name)
    {
        GameObject visual = new(name);
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.color = Color.white;
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
        return GetIngredientKeyById(definition.ingredientId, definition);
    }

    private static string GetIngredientKeyById(int ingredientId, GatherableDefinition fallbackDefinition)
    {
        if (GatheringDataLookup.TryGetIngredient(ingredientId, out IngredientDefinition ingredient) &&
            !string.IsNullOrWhiteSpace(ingredient.ingredientKey))
            return SanitizeFileName(ingredient.ingredientKey);

        string fileName = Path.GetFileName(fallbackDefinition.availableSpriteResourcePath);
        if (!string.IsNullOrWhiteSpace(fileName))
            return SanitizeFileName(fileName);

        return $"id_{ingredientId}";
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
