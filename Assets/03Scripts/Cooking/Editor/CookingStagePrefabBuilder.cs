using System.IO;
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
        GameObject labelObject = new("StageLabel");
        labelObject.transform.SetParent(parent, false);

        TextMesh text = labelObject.AddComponent<TextMesh>();
        text.text = string.Empty;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 34;
        text.characterSize = 0.06f;
        text.color = new Color(0.92f, 0.95f, 0.92f, 1f);

        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sortingOrder = 35;

        labelObject.SetActive(false);
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
        stage.SetActive(false);
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
