using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the CookingStageRig prefab: one pre-built, initially-inactive instance of every
/// prep-stage tool/board/ingredient-slot and every finishing-stage rig. CookingFlowController2D
/// activates/repositions/reconfigures these instead of creating and destroying GameObjects
/// every single cook.
/// </summary>
public static class CookingStagePrefabBuilder
{
    private const string RootFolder = "Assets/01Resources/Prefabs/Cooking";
    private const string PrefabPath = RootFolder + "/CookingStageRig.prefab";
    private static readonly Vector2 BoardSize = new(3.55f, 2.18f);

    [MenuItem("IceBop/Build Cooking Stage Rig")]
    public static void BuildCookingStageRig()
    {
        EnsureFolder(RootFolder);

        GameObject root = new("CookingStageRig");

        try
        {
            BuildPreparationBoard(root.transform);
            BuildStageLabel(root.transform);
            BuildTools(root.transform);
            BuildIngredientSlots(root.transform);
            BuildFinishingStages(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void BuildPreparationBoard(Transform parent)
    {
        GameObject board = CookingRuntimeVisuals2D.CreateBoard(Vector3.zero, BoardSize, -1);
        board.transform.SetParent(parent, false);
        board.transform.localPosition = Vector3.zero;
        board.SetActive(false);
    }

    private static void BuildStageLabel(Transform parent)
    {
        GameObject labelObject = BuildWorldText(parent, "StageLabel", 2.4f, new Color(0.92f, 0.95f, 0.92f, 1f), 35);
        labelObject.SetActive(false);
    }

    private static GameObject BuildWorldText(Transform parent, string name, float width, Color color, int sortingOrder)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.text = string.Empty;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.fontSize = 2.6f;
        text.color = color;

        RectTransform rectTransform = text.rectTransform;
        rectTransform.sizeDelta = new Vector2(width, 0.6f);
        rectTransform.localPosition = Vector3.zero;

        text.renderer.sortingOrder = sortingOrder;
        return textObject;
    }

    private static void BuildTools(Transform parent)
    {
        Transform toolsRoot = FindOrCreateChild(parent, "Tools");

        BuildKnifeTool(toolsRoot);
        BuildPointerTool(toolsRoot, "Cloth", CookingRuntimeVisuals2D.GetClothSprite(), Vector3.one);
        BuildPointerTool(toolsRoot, "Grinder", CookingRuntimeVisuals2D.GetKnife1Sprite(), new Vector3(0.66f, 0.66f, 1f));
    }

    private static void BuildKnifeTool(Transform parent)
    {
        GameObject knife = new("Knife");
        knife.transform.SetParent(parent, false);
        knife.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        SpriteRenderer renderer = knife.AddComponent<SpriteRenderer>();
        Sprite sprite = CookingRuntimeVisuals2D.GetKnife1Sprite();
        renderer.sprite = sprite;
        renderer.sortingOrder = 30;

        BoxCollider2D collider = knife.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        if (sprite != null)
        {
            collider.size = sprite.bounds.size;
            collider.offset = sprite.bounds.center;
        }

        knife.AddComponent<PreparationKnifeTool2D>();
        knife.SetActive(false);
    }

    private static void BuildPointerTool(Transform parent, string name, Sprite sprite, Vector3 scale)
    {
        GameObject toolObject = new(name);
        toolObject.transform.SetParent(parent, false);
        toolObject.transform.localScale = scale;

        SpriteRenderer renderer = toolObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 30;

        BoxCollider2D collider = toolObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        if (sprite != null)
        {
            collider.size = sprite.bounds.size;
            collider.offset = sprite.bounds.center;
        }

        toolObject.AddComponent<PreparationPointerTool2D>();
        toolObject.SetActive(false);
    }

    private static void BuildIngredientSlots(Transform parent)
    {
        Transform slotsRoot = FindOrCreateChild(parent, "IngredientSlots");

        BuildIngredientSlot<CuttableIngredient2D>(slotsRoot, "Cutting");
        BuildIngredientSlot<DustWipeIngredient2D>(slotsRoot, "DustWiping");
        BuildIngredientSlot<GrindableIngredient2D>(slotsRoot, "Grinding");
    }

    private static void BuildIngredientSlot<T>(Transform parent, string name) where T : Component
    {
        GameObject slot = new(name);
        slot.transform.SetParent(parent, false);

        SpriteRenderer renderer = slot.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 5;

        PolygonCollider2D collider = slot.AddComponent<PolygonCollider2D>();
        collider.isTrigger = true;

        slot.AddComponent<T>();
        slot.SetActive(false);
    }

    private static void BuildFinishingStages(Transform parent)
    {
        Transform stagesRoot = FindOrCreateChild(parent, "FinishingStages");

        BuildFinishingStage<CookingMixingPot2D>(stagesRoot, "Stir");
        BuildFinishingStage<BasketShakeFinishingStage2D>(stagesRoot, "BasketShake");
        BuildFinishingStage<FryerFinishingStage2D>(stagesRoot, "Fryer");
        BuildFinishingStage<SkewerAssemblyFinishingStage2D>(stagesRoot, "SkewerAssembly");
        BuildFinishingStage<GuidedPlacementFinishingStage2D>(stagesRoot, "GuidedPlacement");
    }

    private static void BuildFinishingStage<T>(Transform parent, string name) where T : Component
    {
        GameObject stage = new(name);
        stage.transform.SetParent(parent, false);
        stage.AddComponent<T>();
        BuildDishResultVisuals(stage.transform);
        stage.SetActive(false);
    }

    // The completed-dish sprite and its name label that DishResultPresenter2D drives - placed
    // here rather than created at runtime so they can be positioned/styled in the prefab.
    private static void BuildDishResultVisuals(Transform parent)
    {
        Transform resultRoot = FindOrCreateChild(parent, "DishResult");

        GameObject dishObject = new("CompletedDish");
        dishObject.transform.SetParent(resultRoot, false);
        dishObject.transform.localPosition = new Vector3(0f, 0.36f, 0f);
        dishObject.transform.localScale = new Vector3(0.78f, 0.78f, 1f);

        SpriteRenderer dishRenderer = dishObject.AddComponent<SpriteRenderer>();
        dishRenderer.sortingOrder = 43;

        GameObject labelObject = BuildWorldText(resultRoot, "CompletedDishLabel", 3f, new Color(0.94f, 0.96f, 0.92f, 1f), 44);
        labelObject.transform.localPosition = new Vector3(0f, -0.55f, 0f);
    }

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing;

        GameObject child = new(name);
        child.transform.SetParent(parent, false);
        return child.transform;
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
