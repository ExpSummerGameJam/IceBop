using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finishing stage for dishes like the frog salad: shake the basket left-right a number of
/// times to toss the ingredients together.
/// </summary>
public sealed class BasketShakeFinishingStage2D : MonoBehaviour, IDishFinishingStage
{
    private const float ProgressBarWidth = 1.14f;
    private const float ProgressBarHeight = 0.16f;
    private const float ProgressBarLeftX = -ProgressBarWidth * 0.5f;
    private const float ProgressBarY = -0.72f;

    [SerializeField] private int requiredShakes = 6;
    [SerializeField] private float minShakeDistance = 0.42f;
    [SerializeField] private float verticalNoiseRatio = 1.1f;

    private readonly List<SpriteRenderer> ingredientRenderers = new();

    private Coroutine dropInRoutine;
    private Transform basketRoot;
    private Transform progressBarRoot;
    private PreparationPointerTool2D basketHandle;
    private SpriteRenderer progressFillRenderer;
    private DishResultPresenter2D presenter;
    private AlternatingStrokeTracker strokeTracker;
    private Camera targetCamera;
    private InventoryView2D inventoryView;
    private DishDefinition dish;
    private IReadOnlyList<CookingIngredientEntry> ingredients;
    private CookingMethodToolVisual2D toolVisual;
    private bool interactionCompleted;
    private bool presentingResult;

    public event Action InteractionCompleted;
    public event Action ResultPresented;

    public void Configure(
        Camera camera,
        IReadOnlyList<CookingIngredientEntry> newIngredients,
        DishDefinition newDish,
        CookingMethodToolVisual2D newToolVisual,
        InventoryView2D newInventoryView)
    {
        targetCamera = camera;
        inventoryView = newInventoryView;
        ingredients = newIngredients;
        dish = newDish;
        toolVisual = newToolVisual;
        strokeTracker = new AlternatingStrokeTracker(requiredShakes, minShakeDistance, verticalNoiseRatio, true);
        interactionCompleted = false;
        presentingResult = false;

        EnsureVisuals();
        RebuildIngredientBits();
        ApplyProgressVisual();
    }

    private void Update()
    {
        if (interactionCompleted || presentingResult || basketHandle == null || !basketHandle.HasPointerMovement)
            return;

        Vector2 delta = basketHandle.PointerWorldPosition - basketHandle.PreviousPointerWorldPosition;
        if (!strokeTracker.ApplyMovement(delta))
            return;

        ApplyProgressVisual();

        if (strokeTracker.IsComplete && !interactionCompleted)
        {
            interactionCompleted = true;
            InteractionCompleted?.Invoke();
        }
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
        if (basketRoot != null)
            basketRoot.gameObject.SetActive(false);

        if (progressBarRoot != null)
            progressBarRoot.gameObject.SetActive(false);
    }

    private void ApplyProgressVisual()
    {
        if (progressFillRenderer == null)
            return;

        float width = Mathf.Max(0.02f, ProgressBarWidth * strokeTracker.Progress);
        progressFillRenderer.transform.localScale = new Vector3(width, ProgressBarHeight, 1f);
        progressFillRenderer.transform.localPosition = new Vector3(ProgressBarLeftX + width * 0.5f, ProgressBarY, 0f);
    }

    private void EnsureVisuals()
    {
        if (basketRoot == null)
            basketRoot = transform.Find("BasketBody");

        if (basketRoot != null)
        {
            basketRoot.gameObject.SetActive(true);
            EnsureBasketHandle();
        }

        if (progressBarRoot == null)
        {
            GameObject barRoot = new("BasketProgressBar");
            barRoot.transform.SetParent(transform, false);
            progressBarRoot = barRoot.transform;

            CookingRuntimeVisuals2D.CreateBlock("BasketProgressTrack", progressBarRoot, new Vector3(0f, ProgressBarY, 0f), new Vector3(ProgressBarWidth, ProgressBarHeight, 1f), new Color(0.2f, 0.2f, 0.2f, 0.6f), 38);
            progressFillRenderer = CookingRuntimeVisuals2D.CreateBlock("BasketProgressFill", progressBarRoot, new Vector3(ProgressBarLeftX, ProgressBarY, 0f), new Vector3(0.02f, ProgressBarHeight, 1f), new Color(0.95f, 0.75f, 0.35f, 1f), 39);
        }

        progressBarRoot.gameObject.SetActive(true);
    }

    // The player grabs the whole hand-placed "BasketBody" (parent of the "PotWall" rim art)
    // and shakes it - no separate procedural handle prop is built anymore.
    private void EnsureBasketHandle()
    {
        if (basketRoot == null)
            return;

        // The hand-authored EdgeCollider2D on the child "PotWall" is a thin rim outline
        // (edgeRadius 0), so OverlapPoint only registers a hit within a hair of the exact
        // outline. Add a filled trigger collider on the parent "BasketBody" itself, sized to
        // its sprite, so the whole visible basket is grabbable. PreparationPointerTool2D goes
        // on the same object as its required Collider2D.
        BoxCollider2D dragCollider = basketRoot.GetComponent<BoxCollider2D>();
        if (dragCollider == null)
            dragCollider = basketRoot.gameObject.AddComponent<BoxCollider2D>();

        dragCollider.isTrigger = true;
        SpriteRenderer bodyRenderer = basketRoot.GetComponent<SpriteRenderer>();
        if (bodyRenderer != null && bodyRenderer.sprite != null)
        {
            dragCollider.size = bodyRenderer.sprite.bounds.size;
            dragCollider.offset = bodyRenderer.sprite.bounds.center;
        }

        basketHandle = basketRoot.GetComponent<PreparationPointerTool2D>();
        if (basketHandle == null)
            basketHandle = basketRoot.gameObject.AddComponent<PreparationPointerTool2D>();

        basketHandle.gameObject.SetActive(true);
        basketHandle.Configure(targetCamera != null ? targetCamera : Camera.main, inventoryView);
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

        if (basketRoot == null || ingredients == null)
            return;

        List<Vector3> spawnPositions = new(ingredients.Count);

        for (int i = 0; i < ingredients.Count; i++)
        {
            CookingIngredientEntry entry = ingredients[i];
            GameObject bit = new($"BasketIngredient_{i + 1}_{entry.ingredientId}");
            bit.transform.SetParent(basketRoot, false);
            bit.SetActive(false);

            // Spawn above the basket, slightly randomized so pieces dropped in sequence don't
            // spawn stacked exactly on top of each other - physics then decides where each lands.
            spawnPositions.Add(new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 1.1f + i * 0.1f, 0f));

            SpriteRenderer renderer = bit.AddComponent<SpriteRenderer>();
            renderer.sprite = CookingRuntimeVisuals2D.GetIngredientSprite(entry);
            renderer.color = CookingRuntimeVisuals2D.GetIngredientColor(entry);
            renderer.sortingOrder = 40 + i;

            ingredientRenderers.Add(renderer);
        }

        dropInRoutine = StartCoroutine(CookingRuntimeVisuals2D.DropInIngredientsPhysics(
            ingredientRenderers, spawnPositions, 1.5f));
    }

    private void OnValidate()
    {
        requiredShakes = Mathf.Max(1, requiredShakes);
        minShakeDistance = Mathf.Max(0.01f, minShakeDistance);
        verticalNoiseRatio = Mathf.Max(0.1f, verticalNoiseRatio);
    }
}
