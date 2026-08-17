using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class CookingFlowController2D : MonoBehaviour
{
    /// <summary>Dish key for the "you made nothing" result. Matches its sprite name in Atlas/Food.</summary>
    private const string FailedDishKey = "failure_mother";

    [SerializeField] private InventoryCookingStation2D cookingStation;
    [SerializeField] private InventoryView2D inventoryView;
    [SerializeField] private Transform stageRig;
    [SerializeField] private Vector3 preparationCenter = new(0f, 0.24f, 0f);
    [SerializeField] private Vector3 toolStartOffset = new(-1.35f, -1.62f, 0f);
    [SerializeField] private float preparedIngredientHeightOffset = 0.6f;
    [SerializeField] private Vector2 boardSize = new(3.55f, 2.18f);
    [SerializeField] private float preparedIngredientScaleMultiplier = 2.5f;
    [SerializeField] private float stageCompleteDelay = 1f;
    [SerializeField] private CookingMethodToolVisual2D[] methodToolVisuals = new CookingMethodToolVisual2D[0];

    private static CookingFlowController2D instance;

    private Coroutine activeRoutine;
    private Action<CookingCraftResult> completionCallback;

    private GameObject activePreparationBoard;
    private GameObject activeStageLabel;
    private GameObject activePreparationTool;
    private GameObject activeIngredientSlot;
    private IDishFinishingStage activeFinishingStage;

    private bool loggedMissingStageRig;

    public static void StartCooking(IReadOnlyList<CookingIngredientEntry> ingredients, Action<DishDefinition> onCompleted)
    {
        StartCookingWithResult(ingredients, result => onCompleted?.Invoke(result?.dish));
    }

    public static void StartCookingWithResult(IReadOnlyList<CookingIngredientEntry> ingredients, Action<CookingCraftResult> onCompleted)
    {
        CookingFlowController2D controller = EnsureInstance();
        controller.BeginCooking(ingredients, onCompleted);
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
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        DeactivateActiveStage();
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

        if (!ResolveStageRig())
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
            GameSfxPlayer.Play(GameSfx.NewCookingStage);
            bool stageCompleted = false;
            preparation.PreparationCompleted += _ => stageCompleted = true;

            yield return new WaitUntil(() => stageCompleted || preparation == null || preparation.IsPrepared);

            // Carry the prepared look (cut piece, ground tint, ...) into the finishing stage
            // instead of re-showing the raw ingredient - must read this before the slot gets
            // reused/re-skinned for the next ingredient.
            if (preparation != null)
            {
                if (preparation.ProcessedSprite != null)
                    entry.ingredientSprite = preparation.ProcessedSprite;

                if (preparation.ProcessedColor.HasValue)
                    entry.ingredientColor = preparation.ProcessedColor.Value;
            }

            yield return new WaitForSeconds(stageCompleteDelay);
            DeactivateActiveStage();
        }

        DishDefinition dish = ResolveDish(ingredients);
        IngredientPreparationKind cookingMethod = ResolveCookingMethod(dish, ingredients);
        IDishFinishingStage finishingStage = CreateFinishingStage(ingredients, dish, cookingMethod, camera);
        GameSfxPlayer.Play(GameSfx.NewCookingStage);

        bool dishPresented = false;
        finishingStage.InteractionCompleted += finishingStage.PlayResultPresentation;
        finishingStage.ResultPresented += () => dishPresented = true;
        finishingStage.InteractionCompleted += () => GameSfxPlayer.Play(GameSfx.DishCompleted);

        yield return new WaitUntil(() => dishPresented);

        // Hold on the finished dish until the player clicks it away. The completion callback
        // is what lets the station go idle again (and its pot reappear), so firing it right
        // after the presentation would snap straight into "ready for the next cook" while the
        // player is still looking at what they just made.
        yield return null;
        yield return new WaitUntil(GatheringInput.WasPrimaryPressed);

        DeactivateActiveStage();

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
        CookingMethodToolVisual2D toolVisual = ResolveToolVisual(entry.preparationKind);
        ActivatePreparationBoard(toolVisual);
        ActivateStageLabel(stageIndex, stageCount, entry);

        return entry.preparationKind switch
        {
            IngredientPreparationKind.DustWiping => ActivateDustWipeStage(entry, camera, toolVisual),
            IngredientPreparationKind.Grinding => ActivateGrindingStage(entry, camera, toolVisual),
            _ => ActivateCuttingStage(entry, camera, toolVisual)
        };
    }

    private CuttableIngredient2D ActivateCuttingStage(CookingIngredientEntry entry, Camera camera, CookingMethodToolVisual2D toolVisual)
    {
        PreparationKnifeTool2D knife = ActivateKnife(camera, toolVisual);
        GameObject ingredient = ActivateIngredientSlot("Cutting", entry);

        CuttableIngredient2D cuttable = ingredient.GetComponent<CuttableIngredient2D>();
        cuttable.BindKnife(knife);
        cuttable.RebuildGuides();
        return cuttable;
    }

    private DustWipeIngredient2D ActivateDustWipeStage(CookingIngredientEntry entry, Camera camera, CookingMethodToolVisual2D toolVisual)
    {
        PreparationPointerTool2D cloth = ActivatePointerTool("Cloth", camera, toolVisual, CookingRuntimeVisuals2D.GetClothSprite());
        GameObject ingredient = ActivateIngredientSlot("DustWiping", entry);

        DustWipeIngredient2D dustWipe = ingredient.GetComponent<DustWipeIngredient2D>();
        dustWipe.BindCloth(cloth);
        dustWipe.Rearm();
        return dustWipe;
    }

    private GrindableIngredient2D ActivateGrindingStage(CookingIngredientEntry entry, Camera camera, CookingMethodToolVisual2D toolVisual)
    {
        // Matches the original placeholder choice: no dedicated grinder-tool sprite exists,
        // so the knife1 sprite is reused for the grinder tool's look (GetGrinderSprite() is the
        // ground-powder visual applied to the ingredient itself, not this tool).
        PreparationPointerTool2D grinder = ActivatePointerTool("Grinder", camera, toolVisual, CookingRuntimeVisuals2D.GetKnife1Sprite());
        GameObject ingredient = ActivateIngredientSlot("Grinding", entry);

        GrindableIngredient2D grindable = ingredient.GetComponent<GrindableIngredient2D>();
        grindable.BindGrindingTool(grinder);
        grindable.Rearm();
        return grindable;
    }

    private GameObject ActivateIngredientSlot(string slotName, CookingIngredientEntry entry)
    {
        Transform slot = GetRigChild($"IngredientSlots/{slotName}");
        if (slot == null)
            return null;

        activeIngredientSlot = slot.gameObject;
        slot.gameObject.SetActive(true);
        slot.position = GetStageCenter() + new Vector3(0f, preparedIngredientHeightOffset, 0f);

        Vector3 sourceScale = entry != null && entry.ingredientScale != Vector3.zero
            ? entry.ingredientScale
            : Vector3.one;
        slot.localScale = sourceScale * Mathf.Max(0.01f, preparedIngredientScaleMultiplier);

        SpriteRenderer renderer = slot.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = CookingRuntimeVisuals2D.GetIngredientSprite(entry);
            renderer.color = CookingRuntimeVisuals2D.GetIngredientColor(entry);
            renderer.enabled = true;
        }

        PolygonCollider2D collider = CookingRuntimeVisuals2D.ApplySpriteFittedCollider(
            slot.gameObject, renderer != null ? renderer.sprite : null);
        if (collider != null)
        {
            collider.enabled = true;
            collider.isTrigger = true;
        }

        return slot.gameObject;
    }

    private void ActivatePreparationBoard(CookingMethodToolVisual2D toolVisual)
    {
        Transform board = GetRigChild("PreparationBoard");
        if (board == null)
            return;

        activePreparationBoard = board.gameObject;
        board.gameObject.SetActive(true);
        board.position = GetStageCenter();

        // The board's BoxCollider2D is only there for touch/tap detection - it must never
        // physically block ingredients (cut-off debris, dropped pieces, etc.), so force it to
        // a trigger every time regardless of what's baked into the prefab.
        BoxCollider2D boardCollider = board.GetComponent<BoxCollider2D>();
        if (boardCollider != null)
            boardCollider.isTrigger = true;

        if (toolVisual != null && toolVisual.preparationBoardSprite != null)
        {
            SpriteRenderer renderer = board.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.sprite = toolVisual.preparationBoardSprite;
        }

        if (toolVisual != null && toolVisual.preparationBoardScale != Vector3.zero)
            board.localScale = toolVisual.preparationBoardScale;
    }

    private void ActivateStageLabel(int stageIndex, int stageCount, CookingIngredientEntry entry)
    {
        Transform label = GetRigChild("StageLabel");
        if (label == null)
            return;

        activeStageLabel = label.gameObject;
        label.gameObject.SetActive(true);

        Vector2 stageSize = GetStageSize();
        label.position = GetStageCenter() + new Vector3(0f, stageSize.y * 0.5f + 0.46f, 0f);

        TMP_Text text = label.GetComponent<TMP_Text>();
        if (text != null)
            text.text = $"{stageIndex + 1}/{stageCount} {GetPreparationActionLabel(entry.preparationKind)}";
    }

    private static string GetPreparationActionLabel(IngredientPreparationKind preparationKind)
    {
        return preparationKind switch
        {
            IngredientPreparationKind.Cutting => "썰어라!",
            IngredientPreparationKind.DustWiping => "닦아라!",
            IngredientPreparationKind.Grinding => "갈아라!",
            _ => preparationKind.ToString()
        };
    }

    private PreparationKnifeTool2D ActivateKnife(Camera camera, CookingMethodToolVisual2D toolVisual)
    {
        Transform knife = GetRigChild("Tools/Knife");
        if (knife == null)
            return null;

        activePreparationTool = knife.gameObject;
        knife.gameObject.SetActive(true);
        knife.position = GetStageCenter() + toolStartOffset;

        if (toolVisual != null && toolVisual.preparationToolScale != Vector3.zero)
            knife.localScale = toolVisual.preparationToolScale;

        Sprite sprite = toolVisual != null && toolVisual.preparationToolSprite != null
            ? toolVisual.preparationToolSprite
            : CookingRuntimeVisuals2D.GetKnife1Sprite();
        Sprite activeSprite = toolVisual != null && toolVisual.preparationToolActiveSprite != null
            ? toolVisual.preparationToolActiveSprite
            : CookingRuntimeVisuals2D.GetKnife2Sprite();

        PreparationKnifeTool2D tool = knife.GetComponent<PreparationKnifeTool2D>();
        tool.Configure(camera, new Vector2(0.92f, 0f), inventoryView);
        tool.ConfigureBladeSprites(sprite, activeSprite);
        return tool;
    }

    private PreparationPointerTool2D ActivatePointerTool(string toolName, Camera camera, CookingMethodToolVisual2D toolVisual, Sprite fallbackSprite)
    {
        Transform toolTransform = GetRigChild($"Tools/{toolName}");
        if (toolTransform == null)
            return null;

        activePreparationTool = toolTransform.gameObject;
        toolTransform.gameObject.SetActive(true);
        toolTransform.position = GetStageCenter() + toolStartOffset;

        if (toolVisual != null && toolVisual.preparationToolScale != Vector3.zero)
            toolTransform.localScale = toolVisual.preparationToolScale;

        Sprite sprite = toolVisual != null && toolVisual.preparationToolSprite != null ? toolVisual.preparationToolSprite : fallbackSprite;
        SpriteRenderer renderer = toolTransform.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.sprite = sprite;

        PreparationPointerTool2D tool = toolTransform.GetComponent<PreparationPointerTool2D>();
        tool.Configure(camera, inventoryView);
        return tool;
    }

    private IDishFinishingStage CreateFinishingStage(
        IReadOnlyList<CookingIngredientEntry> ingredients,
        DishDefinition dish,
        IngredientPreparationKind cookingMethod,
        Camera camera)
    {
        DishFinishingStageKind stageKind = dish != null ? dish.FinishingStageKind : DishFinishingStageKind.Stir;
        string childName = stageKind switch
        {
            DishFinishingStageKind.BasketShake => "BasketShake",
            DishFinishingStageKind.FryerSaltSprinkle => "Fryer",
            DishFinishingStageKind.FryerSaltSqueeze => "Fryer",
            DishFinishingStageKind.SkewerAssembly => "SkewerAssembly",
            DishFinishingStageKind.GuidedPlacement => "GuidedPlacement",
            _ => "Stir",
        };

        Transform stageTransform = GetRigChild($"FinishingStages/{childName}");
        if (stageTransform == null)
            return null;

        stageTransform.gameObject.SetActive(true);
        stageTransform.position = GetStageCenter();

        IDishFinishingStage stage = stageTransform.GetComponent<IDishFinishingStage>();
        activeFinishingStage = stage;
        stage.Configure(camera, ingredients, dish, ResolveToolVisual(cookingMethod), inventoryView);
        return stage;
    }

    private DishDefinition ResolveDish(IReadOnlyList<CookingIngredientEntry> ingredients)
    {
        List<int> ingredientIds = new();
        foreach (CookingIngredientEntry ingredient in ingredients)
            ingredientIds.Add(ingredient.ingredientId);

        if (GatheringDataLookup.TryFindDishForIngredients(ingredientIds, out DishDefinition dish))
            return dish;

        // No recipe matched - the cook produces the authored "failed dish" row, which carries
        // no ability charges. Looked up by key so its sprite/name/finishing stage all come
        // from the Dishes sheet like every other dish.
        if (GatheringDataLookup.TryGetDishByKey(FailedDishKey, out DishDefinition failedDish))
            return failedDish;

        return new DishDefinition
        {
            dishId = 0,
            dishKey = FailedDishKey,
            displayName = "실패한 요리",
            dishSpriteResourcePath = FailedDishKey,
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

    private void DeactivateActiveStage()
    {
        if (activePreparationBoard != null)
            activePreparationBoard.SetActive(false);

        if (activeStageLabel != null)
            activeStageLabel.SetActive(false);

        if (activePreparationTool != null)
            activePreparationTool.SetActive(false);

        if (activeIngredientSlot != null)
            activeIngredientSlot.SetActive(false);

        if (activeFinishingStage is Component finishingComponent && finishingComponent != null)
            finishingComponent.gameObject.SetActive(false);

        activePreparationBoard = null;
        activeStageLabel = null;
        activePreparationTool = null;
        activeIngredientSlot = null;
        activeFinishingStage = null;
    }

    private Camera EnsureCamera()
    {
        ResolveInventoryView();

        if (inventoryView != null && inventoryView.LargeCamera != null)
            return inventoryView.LargeCamera;

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

    private void ResolveInventoryView()
    {
        if (inventoryView != null)
            return;

        ResolveCookingStation();
        inventoryView = cookingStation != null ? cookingStation.GetComponent<InventoryView2D>() : null;

        if (inventoryView == null)
            inventoryView = FindFirstObjectByType<InventoryView2D>();
    }

    private Vector3 GetStageCenter()
    {
        ResolveCookingStation();

        if (cookingStation != null && cookingStation.HasPrepPot)
            return cookingStation.PrepPotCenter;

        return preparationCenter;
    }

    private void ResolveCookingStation()
    {
        if (cookingStation != null)
            return;

        cookingStation = FindFirstObjectByType<InventoryCookingStation2D>();
    }

    private Vector2 GetStageSize()
    {
        return boardSize;
    }

    private bool ResolveStageRig()
    {
        if (stageRig != null)
            return true;

        stageRig = transform.Find("CookingStageRig");
        if (stageRig != null)
            return true;

        if (!loggedMissingStageRig)
        {
            loggedMissingStageRig = true;
            Debug.LogError(
                "CookingFlowController2D needs a placed CookingStageRig prefab instance. " +
                "Build one via IceBop/Build Cooking Stage Rig, then IceBop/Build Placed Inventory And Cooking Areas.",
                this);
        }

        return false;
    }

    private Transform GetRigChild(string path)
    {
        return stageRig != null ? stageRig.Find(path) : null;
    }

    private void OnValidate()
    {
        boardSize.x = Mathf.Max(0.5f, boardSize.x);
        boardSize.y = Mathf.Max(0.5f, boardSize.y);
        preparedIngredientScaleMultiplier = Mathf.Max(0.01f, preparedIngredientScaleMultiplier);
        preparedIngredientHeightOffset = Mathf.Max(0f, preparedIngredientHeightOffset);
        stageCompleteDelay = Mathf.Max(0f, stageCompleteDelay);
    }
}
