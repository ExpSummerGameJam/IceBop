using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CookingMixingPot2D : MonoBehaviour, IDishFinishingStage
{
    private const float RestIngredientScale = 1.5f;

    private static readonly Vector3 LiquidFullLocalPosition = new(0f, 0.03f, 0f);
    private static readonly Vector2 LiquidFullSize = new(1.20f, 0.44f);
    private static readonly Color EmptyLiquidColor = new(0.50f, 0.30f, 0.14f, 0.38f);
    private static readonly Color FullLiquidColor = new(0.78f, 0.48f, 0.20f, 0.92f);

    [SerializeField] private PreparationPointerTool2D ladleTool;
    [SerializeField] private int requiredAlternatingStrokes = 8;
    [SerializeField] private float minHorizontalStrokeDistance = 0.48f;
    [SerializeField] private float verticalNoiseRatio = 1.15f;

    private readonly List<SpriteRenderer> ingredientRenderers = new();
    private readonly List<Vector3> ingredientStartScales = new();

    private Coroutine dropInRoutine;
    private Transform potBody;
    private Collider2D potCollider;
    private Transform contentRoot;
    private SpriteRenderer liquidRenderer;
    private SpriteMask liquidMask;
    private DishResultPresenter2D presenter;
    private AlternatingStrokeTracker strokeTracker;
    private Camera targetCamera;
    private InventoryView2D inventoryView;
    private DishDefinition dish;
    private IReadOnlyList<CookingIngredientEntry> ingredients;
    private CookingMethodToolVisual2D toolVisual;
    private bool mixingCompleted;
    private bool presentingResult;

    public event Action InteractionCompleted;
    public event Action ResultPresented;

    public void Configure(Camera camera, IReadOnlyList<CookingIngredientEntry> newIngredients, DishDefinition newDish)
    {
        Configure(camera, newIngredients, newDish, null, null);
    }

    public void Configure(
        Camera camera,
        IReadOnlyList<CookingIngredientEntry> newIngredients,
        DishDefinition newDish,
        CookingMethodToolVisual2D newToolVisual,
        InventoryView2D newInventoryView = null)
    {
        targetCamera = camera;
        inventoryView = newInventoryView;
        ingredients = newIngredients;
        dish = newDish;
        toolVisual = newToolVisual;
        strokeTracker = new AlternatingStrokeTracker(requiredAlternatingStrokes, minHorizontalStrokeDistance, verticalNoiseRatio, true);
        mixingCompleted = false;
        presentingResult = false;

        if (toolVisual != null && toolVisual.mixingPotScale != Vector3.zero)
            transform.localScale = toolVisual.mixingPotScale;

        EnsureVisuals();
        RebuildIngredientBits();
        ApplyMixVisuals();
    }

    private void Update()
    {
        if (mixingCompleted || presentingResult || ladleTool == null || !ladleTool.HasPointerMovement)
            return;

        ApplyLadleMovement(ladleTool.PreviousPointerWorldPosition, ladleTool.PointerWorldPosition);
    }

    public void PlayResultPresentation()
    {
        if (presentingResult)
            return;

        presentingResult = true;

        if (presenter == null)
        {
            presenter = gameObject.AddComponent<DishResultPresenter2D>();
            presenter.Presented += () => ResultPresented?.Invoke();
        }

        presenter.Play(targetCamera, dish, ingredients, HideInteractiveVisuals);
    }

    private void HideInteractiveVisuals()
    {
        if (potBody != null)
            potBody.gameObject.SetActive(false);

        foreach (SpriteRenderer ingredientRenderer in ingredientRenderers)
        {
            if (ingredientRenderer != null)
                ingredientRenderer.gameObject.SetActive(false);
        }

        if (liquidRenderer != null)
            liquidRenderer.gameObject.SetActive(false);

        if (liquidMask != null)
            liquidMask.gameObject.SetActive(false);
    }

    private void ApplyLadleMovement(Vector2 previousPointerPosition, Vector2 currentPointerPosition)
    {
        if (!IsPointerOverPot(currentPointerPosition))
        {
            strokeTracker.AbandonCurrentStroke();
            return;
        }

        Vector2 delta = currentPointerPosition - previousPointerPosition;
        if (!strokeTracker.ApplyMovement(delta))
            return;

        ApplyMixVisuals();
        EmitBubbles();

        if (strokeTracker.IsComplete && !mixingCompleted)
        {
            mixingCompleted = true;
            InteractionCompleted?.Invoke();
        }
    }

    private void ApplyMixVisuals()
    {
        float progress = strokeTracker != null ? strokeTracker.Progress : 0f;

        if (liquidRenderer != null)
            liquidRenderer.color = Color.Lerp(EmptyLiquidColor, FullLiquidColor, progress);

        if (liquidMask != null)
        {
            // The liquid sprite itself stays fixed at full size; the mask's height grows
            // from the bottom edge upward so the fill reads as rising liquid instead of a
            // stretched blob.
            float maskHeight = Mathf.Max(0.001f, LiquidFullSize.y * progress);
            float bottomY = LiquidFullLocalPosition.y - LiquidFullSize.y * 0.5f;
            liquidMask.transform.localScale = new Vector3(LiquidFullSize.x, maskHeight, 1f);
            liquidMask.transform.localPosition = new Vector3(LiquidFullLocalPosition.x, bottomY + maskHeight * 0.5f, 0f);
        }

        for (int i = 0; i < ingredientRenderers.Count; i++)
        {
            SpriteRenderer renderer = ingredientRenderers[i];
            if (renderer == null)
                continue;

            Color color = renderer.color;
            color.a = Mathf.Lerp(1f, 0.04f, progress);
            renderer.color = color;

            // Position is left to physics now that these bits carry a Rigidbody2D (set by
            // DropInIngredientsPhysics) - directly overwriting localPosition here would fight
            // the physics simulation every FixedUpdate. Scale is safe to lerp directly.
            Vector3 startScale = ingredientStartScales[i];
            renderer.transform.localScale = Vector3.Lerp(startScale, startScale * 0.18f, progress);
        }
    }

    private bool IsPointerOverPot(Vector2 worldPosition)
    {
        if (potCollider == null)
            return Vector2.Distance(worldPosition, transform.position) <= 1.25f;

        return potCollider.bounds.Contains(new Vector3(worldPosition.x, worldPosition.y, potCollider.bounds.center.z));
    }

    private void EmitBubbles()
    {
        if (contentRoot == null)
            return;

        for (int i = 0; i < 4; i++)
        {
            GameObject bubble = new("MixBubble");
            bubble.transform.SetParent(contentRoot, false);
            bubble.transform.localPosition = new Vector3(UnityEngine.Random.Range(-0.44f, 0.44f), UnityEngine.Random.Range(-0.08f, 0.22f), 0f);
            bubble.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.035f, 0.07f);

            SpriteRenderer renderer = bubble.AddComponent<SpriteRenderer>();
            renderer.sprite = InventoryVisualUtility.WhiteSprite;
            renderer.color = new Color(1f, 0.88f, 0.58f, 0.72f);
            renderer.sortingOrder = 42;

            Destroy(bubble, 0.65f);
        }
    }

    private void EnsureVisuals()
    {
        if (potBody == null)
            potBody = transform.Find("PotBody");

        if (potBody != null)
            potBody.gameObject.SetActive(true);

        if (contentRoot == null)
        {
            GameObject root = new("MixingContent");
            root.transform.SetParent(transform, false);
            contentRoot = root.transform;
        }

        if (liquidRenderer == null)
        {
            liquidRenderer = CookingRuntimeVisuals2D.CreateBlock(
                "MixLiquid", contentRoot, LiquidFullLocalPosition,
                new Vector3(LiquidFullSize.x, LiquidFullSize.y, 1f), EmptyLiquidColor, 39);
            liquidRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        if (liquidRenderer != null)
            liquidRenderer.gameObject.SetActive(true);

        if (liquidMask == null)
        {
            GameObject maskObject = new("MixLiquidMask");
            maskObject.transform.SetParent(contentRoot, false);
            liquidMask = maskObject.AddComponent<SpriteMask>();
            liquidMask.sprite = InventoryVisualUtility.WhiteSprite;
        }

        if (liquidMask != null)
            liquidMask.gameObject.SetActive(true);

        EnsureWallTool();
    }

    private void RebuildIngredientBits()
    {
        if (dropInRoutine != null)
        {
            StopCoroutine(dropInRoutine);
            dropInRoutine = null;
        }

        foreach (SpriteRenderer renderer in ingredientRenderers)
        {
            if (renderer != null)
                Destroy(renderer.gameObject);
        }

        ingredientRenderers.Clear();
        ingredientStartScales.Clear();

        if (contentRoot == null || ingredients == null)
            return;

        List<Vector3> spawnPositions = new(ingredients.Count);
        Vector3 restScaleVector = Vector3.one * RestIngredientScale;

        for (int i = 0; i < ingredients.Count; i++)
        {
            CookingIngredientEntry entry = ingredients[i];
            GameObject bit = new($"MixIngredient_{i + 1}_{entry.ingredientId}");
            bit.transform.SetParent(contentRoot, false);
            bit.SetActive(false);

            // Spawn above the pot, slightly randomized so pieces dropped in sequence don't
            // spawn stacked exactly on top of each other - physics then decides where each lands.
            spawnPositions.Add(new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 0.45f + i * 0.05f, 0f));

            SpriteRenderer renderer = bit.AddComponent<SpriteRenderer>();
            renderer.sprite = CookingRuntimeVisuals2D.GetIngredientSprite(entry);
            renderer.color = CookingRuntimeVisuals2D.GetIngredientColor(entry);
            renderer.sortingOrder = 40 + i;

            ingredientRenderers.Add(renderer);
            ingredientStartScales.Add(restScaleVector);
        }

        // Drop ingredients in one at a time from above (real physics, not a scripted lerp)
        // instead of having them all appear at once already arranged in the pot.
        dropInRoutine = StartCoroutine(CookingRuntimeVisuals2D.DropInIngredientsPhysics(
            ingredientRenderers, spawnPositions, RestIngredientScale));
    }

    // The player grabs the pot's own rim (hand-placed "PotBody/PotWall", collider already
    // authored there) and shakes it to stir - no separate ladle prop is built anymore.
    private void EnsureWallTool()
    {
        if (ladleTool != null)
        {
            ladleTool.gameObject.SetActive(true);
            ladleTool.Configure(targetCamera != null ? targetCamera : Camera.main, inventoryView);
            potCollider = ladleTool.GetComponent<Collider2D>();
            return;
        }

        if (potBody == null)
            return;

        Transform wall = potBody.Find("PotWall");
        if (wall == null)
            return;

        ladleTool = wall.GetComponent<PreparationPointerTool2D>();
        if (ladleTool == null)
            ladleTool = wall.gameObject.AddComponent<PreparationPointerTool2D>();

        potCollider = wall.GetComponent<Collider2D>();
        ladleTool.Configure(targetCamera != null ? targetCamera : Camera.main, inventoryView);
    }

    private void OnValidate()
    {
        requiredAlternatingStrokes = Mathf.Max(1, requiredAlternatingStrokes);
        minHorizontalStrokeDistance = Mathf.Max(0.01f, minHorizontalStrokeDistance);
        verticalNoiseRatio = Mathf.Max(0.1f, verticalNoiseRatio);
    }
}
