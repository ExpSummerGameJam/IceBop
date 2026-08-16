using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class CookingFlowController2D : MonoBehaviour
{
    [SerializeField] private Collider2D stageAreaCollider;
    [SerializeField] private Vector3 preparationCenter = new(0f, 0.24f, 0f);
    [SerializeField] private Vector3 toolStartOffset = new(-1.35f, -1.62f, 0f);
    [SerializeField] private Vector2 boardSize = new(3.55f, 2.18f);
    [SerializeField] private float preparedIngredientScaleMultiplier = 5f;
    [SerializeField] private float stageCompleteDelay = 0.45f;
    [SerializeField] private CookingMethodToolVisual2D[] methodToolVisuals = new CookingMethodToolVisual2D[0];

    private static CookingFlowController2D instance;

    private Coroutine activeRoutine;
    private GameObject activeStageRoot;
    private Action<CookingCraftResult> completionCallback;

    public static void StartCooking(IReadOnlyList<CookingIngredientEntry> ingredients, Action<DishDefinition> onCompleted)
    {
        StartCookingWithResult(ingredients, result => onCompleted?.Invoke(result?.dish));
    }

    public static void StartCookingWithResult(IReadOnlyList<CookingIngredientEntry> ingredients, Action<CookingCraftResult> onCompleted)
    {
        CookingFlowController2D controller = EnsureInstance();
        controller.BeginCooking(ingredients, onCompleted);
    }

    public void BindStageArea(Collider2D newStageAreaCollider)
    {
        stageAreaCollider = newStageAreaCollider;

        if (stageAreaCollider != null)
            stageAreaCollider.isTrigger = true;
    }

    private static CookingFlowController2D EnsureInstance()
    {
        if (instance != null)
            return instance;

        CookingFlowController2D existing = FindFirstObjectByType<CookingFlowController2D>();
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject controllerObject = new("CookingFlowController");
        instance = controllerObject.AddComponent<CookingFlowController2D>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void BeginCooking(IReadOnlyList<CookingIngredientEntry> ingredients, Action<CookingCraftResult> onCompleted)
    {
        ResolveStageArea();

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        DestroyActiveStage();
        completionCallback = onCompleted;
        activeRoutine = StartCoroutine(CookingRoutine(new List<CookingIngredientEntry>(ingredients)));
    }

    private IEnumerator CookingRoutine(List<CookingIngredientEntry> ingredients)
    {
        if (ingredients == null || ingredients.Count == 0)
        {
            completionCallback?.Invoke(null);
            activeRoutine = null;
            yield break;
        }

        Camera camera = EnsureCamera();

        for (int i = 0; i < ingredients.Count; i++)
        {
            CookingIngredientEntry entry = ingredients[i];
            IngredientPreparationBehaviour2D preparation = CreatePreparationStage(entry, i, ingredients.Count, camera);
            bool stageCompleted = false;
            preparation.PreparationCompleted += _ => stageCompleted = true;

            yield return new WaitUntil(() => stageCompleted || preparation == null || preparation.IsPrepared);
            yield return new WaitForSeconds(stageCompleteDelay);
            DestroyActiveStage();
        }

        DishDefinition dish = ResolveDish(ingredients);
        IngredientPreparationKind cookingMethod = ResolveCookingMethod(dish, ingredients);
        CookingMixingPot2D mixingPot = CreateMixingStage(ingredients, dish, cookingMethod, camera);

        bool dishPresented = false;
        mixingPot.MixingCompleted += pot => pot.PlayResultPresentation();
        mixingPot.DishPresented += _ => dishPresented = true;

        yield return new WaitUntil(() => dishPresented);

        CookingCraftResult result = CookingCraftResult.Create(dish, ingredients, cookingMethod);
        CookingSystem.NotifyDishCrafted(result);
        completionCallback?.Invoke(result);
        activeRoutine = null;
    }

    private IngredientPreparationBehaviour2D CreatePreparationStage(
        CookingIngredientEntry entry,
        int stageIndex,
        int stageCount,
        Camera camera)
    {
        activeStageRoot = new GameObject($"PreparationStage_{stageIndex + 1}_{entry.ingredientId}");

        CookingMethodToolVisual2D toolVisual = ResolveToolVisual(entry.preparationKind);
        CreatePreparationBoard(toolVisual);

        CreateStageLabel(stageIndex, stageCount, entry);

        return entry.preparationKind switch
        {
            IngredientPreparationKind.DustWiping => CreateDustWipeStage(entry, camera, toolVisual),
            IngredientPreparationKind.Grinding => CreateGrindingStage(entry, camera, toolVisual),
            _ => CreateCuttingStage(entry, camera, toolVisual)
        };
    }

    private CuttableIngredient2D CreateCuttingStage(CookingIngredientEntry entry, Camera camera, CookingMethodToolVisual2D toolVisual)
    {
        PreparationKnifeTool2D knife = CreateKnife(camera, toolVisual);
        GameObject ingredient = CreatePreparedIngredientObject(entry, "CuttingIngredient");

        CuttableIngredient2D cuttable = ingredient.AddComponent<CuttableIngredient2D>();
        cuttable.BindKnife(knife);
        cuttable.RebuildGuides();
        return cuttable;
    }

    private DustWipeIngredient2D CreateDustWipeStage(CookingIngredientEntry entry, Camera camera, CookingMethodToolVisual2D toolVisual)
    {
        PreparationPointerTool2D cloth = CreatePointerTool("Handkerchief", CookingRuntimeVisuals2D.GetClothSprite(), Vector3.one, camera, toolVisual);
        GameObject ingredient = CreatePreparedIngredientObject(entry, "DustWipeIngredient");

        DustWipeIngredient2D dustWipe = ingredient.AddComponent<DustWipeIngredient2D>();
        dustWipe.BindCloth(cloth);
        return dustWipe;
    }

    private GrindableIngredient2D CreateGrindingStage(CookingIngredientEntry entry, Camera camera, CookingMethodToolVisual2D toolVisual)
    {
        PreparationPointerTool2D grinder = CreatePointerTool("Grinder", CookingRuntimeVisuals2D.GetGrinderSprite(), new Vector3(0.66f, 0.66f, 1f), camera, toolVisual);
        GameObject ingredient = CreatePreparedIngredientObject(entry, "GrindingIngredient");

        GrindableIngredient2D grindable = ingredient.AddComponent<GrindableIngredient2D>();
        grindable.BindGrindingTool(grinder);
        return grindable;
    }

    private GameObject CreatePreparedIngredientObject(CookingIngredientEntry entry, string name)
    {
        GameObject ingredient = new($"{name}_{entry.ingredientId}");
        ingredient.transform.SetParent(activeStageRoot.transform, false);
        ingredient.transform.localPosition = GetStageCenter();
        Vector3 sourceScale = entry != null && entry.ingredientScale != Vector3.zero
            ? entry.ingredientScale
            : Vector3.one;
        ingredient.transform.localScale = sourceScale * Mathf.Max(0.01f, preparedIngredientScaleMultiplier);

        SpriteRenderer renderer = ingredient.AddComponent<SpriteRenderer>();
        renderer.sprite = CookingRuntimeVisuals2D.GetIngredientSprite(entry);
        renderer.color = CookingRuntimeVisuals2D.GetIngredientColor(entry);
        renderer.sortingOrder = 5;

        BoxCollider2D collider = ingredient.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = renderer.sprite.bounds.size;
        collider.offset = renderer.sprite.bounds.center;

        return ingredient;
    }

    private GameObject CreatePreparationBoard(CookingMethodToolVisual2D toolVisual)
    {
        if (toolVisual != null && toolVisual.preparationBoardPrefab != null)
        {
            GameObject board = Instantiate(toolVisual.preparationBoardPrefab, activeStageRoot.transform, false);
            board.name = "PreparationBoard";
            board.transform.localPosition = GetStageCenter();

            if (toolVisual.preparationBoardScale != Vector3.zero)
                board.transform.localScale = toolVisual.preparationBoardScale;

            ApplyPreparationBoardSprite(board, toolVisual.preparationBoardSprite);
            return board;
        }

        GameObject defaultBoard = CookingRuntimeVisuals2D.CreateBoard(GetStageCenter(), GetStageSize(), -1);
        defaultBoard.transform.SetParent(activeStageRoot.transform, true);

        if (toolVisual != null)
        {
            ApplyPreparationBoardSprite(defaultBoard, toolVisual.preparationBoardSprite);

            if (toolVisual.preparationBoardScale != Vector3.zero)
                defaultBoard.transform.localScale = toolVisual.preparationBoardScale;
        }

        return defaultBoard;
    }

    private static void ApplyPreparationBoardSprite(GameObject board, Sprite sprite)
    {
        if (board == null || sprite == null)
            return;

        SpriteRenderer renderer = board.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = board.AddComponent<SpriteRenderer>();

        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = -1;
    }

    private PreparationKnifeTool2D CreateKnife(Camera camera, CookingMethodToolVisual2D toolVisual)
    {
        Sprite sprite = toolVisual != null && toolVisual.preparationToolSprite != null
            ? toolVisual.preparationToolSprite
            : CookingRuntimeVisuals2D.GetKnifeSprite();

        GameObject knife = CreatePreparationToolObject(
            "PreparationKnife",
            toolVisual,
            GetStageCenter() + toolStartOffset,
            new Vector3(0.75f, 0.75f, 1f));

        bool forceSprite = toolVisual == null || toolVisual.preparationToolPrefab == null || toolVisual.preparationToolSprite != null;
        SpriteRenderer renderer = EnsureToolRenderer(knife, sprite, 30, forceSprite);

        BoxCollider2D collider = knife.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = knife.AddComponent<BoxCollider2D>();

        collider.isTrigger = true;
        if (renderer.sprite != null)
        {
            collider.size = renderer.sprite.bounds.size;
            collider.offset = renderer.sprite.bounds.center;
        }

        PreparationKnifeTool2D tool = knife.GetComponent<PreparationKnifeTool2D>();
        if (tool == null)
            tool = knife.AddComponent<PreparationKnifeTool2D>();

        tool.Configure(camera, new Vector2(0.92f, 0f));
        return tool;
    }

    private PreparationPointerTool2D CreatePointerTool(string name, Sprite fallbackSprite, Vector3 fallbackScale, Camera camera, CookingMethodToolVisual2D toolVisual)
    {
        Sprite sprite = toolVisual != null && toolVisual.preparationToolSprite != null
            ? toolVisual.preparationToolSprite
            : fallbackSprite;

        GameObject toolObject = CreatePreparationToolObject(
            $"Preparation{name}",
            toolVisual,
            GetStageCenter() + toolStartOffset,
            fallbackScale);

        bool forceSprite = toolVisual == null || toolVisual.preparationToolPrefab == null || toolVisual.preparationToolSprite != null;
        SpriteRenderer renderer = EnsureToolRenderer(toolObject, sprite, 30, forceSprite);

        BoxCollider2D collider = toolObject.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = toolObject.AddComponent<BoxCollider2D>();

        collider.isTrigger = true;
        if (renderer.sprite != null)
        {
            collider.size = renderer.sprite.bounds.size;
            collider.offset = renderer.sprite.bounds.center;
        }

        PreparationPointerTool2D tool = toolObject.GetComponent<PreparationPointerTool2D>();
        if (tool == null)
            tool = toolObject.AddComponent<PreparationPointerTool2D>();

        tool.Configure(camera);
        return tool;
    }

    private GameObject CreatePreparationToolObject(
        string name,
        CookingMethodToolVisual2D toolVisual,
        Vector3 localPosition,
        Vector3 fallbackScale)
    {
        GameObject toolObject = toolVisual != null && toolVisual.preparationToolPrefab != null
            ? Instantiate(toolVisual.preparationToolPrefab, activeStageRoot.transform, false)
            : new GameObject(name);

        toolObject.name = name;
        toolObject.transform.SetParent(activeStageRoot.transform, false);
        toolObject.transform.localPosition = localPosition;

        if (toolVisual != null && toolVisual.preparationToolScale != Vector3.zero)
            toolObject.transform.localScale = toolVisual.preparationToolScale;
        else if (toolVisual == null || toolVisual.preparationToolPrefab == null)
            toolObject.transform.localScale = fallbackScale;

        return toolObject;
    }

    private static SpriteRenderer EnsureToolRenderer(GameObject toolObject, Sprite sprite, int sortingOrder, bool forceSprite)
    {
        SpriteRenderer renderer = toolObject.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = toolObject.AddComponent<SpriteRenderer>();

        if (forceSprite || renderer.sprite == null)
            renderer.sprite = sprite;

        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private CookingMixingPot2D CreateMixingStage(
        IReadOnlyList<CookingIngredientEntry> ingredients,
        DishDefinition dish,
        IngredientPreparationKind cookingMethod,
        Camera camera)
    {
        activeStageRoot = new GameObject("FinalMixingStage");
        activeStageRoot.transform.position = GetStageCenter();

        CookingMixingPot2D mixingPot = activeStageRoot.AddComponent<CookingMixingPot2D>();
        mixingPot.Configure(camera, ingredients, dish, ResolveToolVisual(cookingMethod));
        return mixingPot;
    }

    private void CreateStageLabel(int stageIndex, int stageCount, CookingIngredientEntry entry)
    {
        GameObject labelObject = new("PreparationStageLabel");
        labelObject.transform.SetParent(activeStageRoot.transform, false);
        Vector2 stageSize = GetStageSize();
        labelObject.transform.localPosition = GetStageCenter() + new Vector3(0f, stageSize.y * 0.5f + 0.46f, 0f);

        TextMesh text = labelObject.AddComponent<TextMesh>();
        text.text = $"{stageIndex + 1}/{stageCount} {entry.preparationKind}";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 34;
        text.characterSize = 0.06f;
        text.color = new Color(0.92f, 0.95f, 0.92f, 1f);

        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sortingOrder = 35;
    }

    private DishDefinition ResolveDish(IReadOnlyList<CookingIngredientEntry> ingredients)
    {
        List<int> ingredientIds = new();
        foreach (CookingIngredientEntry ingredient in ingredients)
            ingredientIds.Add(ingredient.ingredientId);

        if (GatheringDataLookup.TryFindDishForIngredients(ingredientIds, out DishDefinition dish))
            return dish;

        return new DishDefinition
        {
            dishId = 0,
            dishKey = "mixed_dish",
            displayName = "Mixed Dish",
            requiredIngredientIds = string.Join(",", ingredientIds)
        };
    }

    private IngredientPreparationKind ResolveCookingMethod(DishDefinition dish, IReadOnlyList<CookingIngredientEntry> ingredients)
    {
        if (dish != null && dish.HasCookingMethod)
            return dish.CookingMethod;

        if (ingredients == null || ingredients.Count == 0)
            return IngredientPreparationKind.Cutting;

        Dictionary<IngredientPreparationKind, int> counts = new();
        IngredientPreparationKind fallback = ingredients[0] != null
            ? ingredients[0].preparationKind
            : IngredientPreparationKind.Cutting;

        foreach (CookingIngredientEntry ingredient in ingredients)
        {
            if (ingredient == null)
                continue;

            counts.TryGetValue(ingredient.preparationKind, out int count);
            counts[ingredient.preparationKind] = count + 1;
        }

        IngredientPreparationKind bestKind = fallback;
        int bestCount = -1;
        foreach (KeyValuePair<IngredientPreparationKind, int> pair in counts)
        {
            if (pair.Value <= bestCount)
                continue;

            bestKind = pair.Key;
            bestCount = pair.Value;
        }

        return bestKind;
    }

    private CookingMethodToolVisual2D ResolveToolVisual(IngredientPreparationKind cookingMethod)
    {
        if (methodToolVisuals == null)
            return null;

        foreach (CookingMethodToolVisual2D visual in methodToolVisuals)
        {
            if (visual != null && visual.cookingMethod == cookingMethod)
                return visual;
        }

        return null;
    }

    private void DestroyActiveStage()
    {
        if (activeStageRoot == null)
            return;

        Destroy(activeStageRoot);
        activeStageRoot = null;
    }

    private static Camera EnsureCamera()
    {
        if (Camera.main != null)
            return Camera.main;

        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.2f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.10f, 0.12f, 0.14f, 1f);
        return camera;
    }

    private Vector3 GetStageCenter()
    {
        ResolveStageArea();

        if (stageAreaCollider != null && stageAreaCollider.enabled)
            return stageAreaCollider.bounds.center;

        return preparationCenter;
    }

    private Vector2 GetStageSize()
    {
        ResolveStageArea();

        if (stageAreaCollider != null && stageAreaCollider.enabled)
            return stageAreaCollider.bounds.size;

        return boardSize;
    }

    private void ResolveStageArea()
    {
        if (stageAreaCollider != null)
            return;

        CookingStageArea2D stageArea = FindFirstObjectByType<CookingStageArea2D>();
        if (stageArea != null)
            stageAreaCollider = stageArea.AreaCollider;
    }

    private void OnValidate()
    {
        boardSize.x = Mathf.Max(0.5f, boardSize.x);
        boardSize.y = Mathf.Max(0.5f, boardSize.y);
        preparedIngredientScaleMultiplier = Mathf.Max(0.01f, preparedIngredientScaleMultiplier);
        stageCompleteDelay = Mathf.Max(0f, stageCompleteDelay);
    }
}
