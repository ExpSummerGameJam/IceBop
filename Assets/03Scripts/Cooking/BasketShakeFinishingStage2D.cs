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

    private Transform basketRoot;
    private Transform progressBarRoot;
    private PreparationPointerTool2D basketHandle;
    private SpriteRenderer progressFillRenderer;
    private DishResultPresenter2D presenter;
    private AlternatingStrokeTracker strokeTracker;
    private Camera targetCamera;
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
        CookingMethodToolVisual2D newToolVisual)
    {
        targetCamera = camera;
        ingredients = newIngredients;
        dish = newDish;
        toolVisual = newToolVisual;
        strokeTracker = new AlternatingStrokeTracker(requiredShakes, minShakeDistance, verticalNoiseRatio, true);

        EnsureVisuals();
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
        {
            GameObject root = new("BasketHandle");
            root.transform.SetParent(transform, false);
            basketRoot = root.transform;

            CookingRuntimeVisuals2D.CreateBlock("BasketShadow", transform, new Vector3(0f, -0.32f, 0f), new Vector3(1.55f, 0.28f, 1f), new Color(0.1f, 0.09f, 0.08f, 0.4f), 34);
            CookingRuntimeVisuals2D.CreateBlock("BasketBody", basketRoot, Vector3.zero, new Vector3(1.5f, 0.9f, 1f), new Color(0.62f, 0.42f, 0.22f, 1f), 36);
            CookingRuntimeVisuals2D.CreateBlock("BasketRim", basketRoot, new Vector3(0f, 0.42f, 0f), new Vector3(1.62f, 0.16f, 1f), new Color(0.48f, 0.32f, 0.16f, 1f), 37);

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.62f, 1.0f);

            basketHandle = root.AddComponent<PreparationPointerTool2D>();
            basketHandle.Configure(targetCamera != null ? targetCamera : Camera.main);

            EnsureIngredientBits();
        }

        if (progressBarRoot == null)
        {
            GameObject barRoot = new("BasketProgressBar");
            barRoot.transform.SetParent(transform, false);
            progressBarRoot = barRoot.transform;

            CookingRuntimeVisuals2D.CreateBlock("BasketProgressTrack", progressBarRoot, new Vector3(0f, ProgressBarY, 0f), new Vector3(ProgressBarWidth, ProgressBarHeight, 1f), new Color(0.2f, 0.2f, 0.2f, 0.6f), 38);
            progressFillRenderer = CookingRuntimeVisuals2D.CreateBlock("BasketProgressFill", progressBarRoot, new Vector3(ProgressBarLeftX, ProgressBarY, 0f), new Vector3(0.02f, ProgressBarHeight, 1f), new Color(0.95f, 0.75f, 0.35f, 1f), 39);
        }
    }

    private void EnsureIngredientBits()
    {
        if (ingredientRenderers.Count > 0 || basketRoot == null || ingredients == null)
            return;

        for (int i = 0; i < ingredients.Count; i++)
        {
            CookingIngredientEntry entry = ingredients[i];
            GameObject bit = new($"BasketIngredient_{i + 1}_{entry.ingredientId}");
            bit.transform.SetParent(basketRoot, false);

            float spread = ingredients.Count <= 1 ? 0f : Mathf.Lerp(-0.5f, 0.5f, i / (float)(ingredients.Count - 1));
            bit.transform.localPosition = new Vector3(spread, 0.18f, 0f);
            bit.transform.localScale = Vector3.one * 0.3f;

            SpriteRenderer renderer = bit.AddComponent<SpriteRenderer>();
            renderer.sprite = CookingRuntimeVisuals2D.GetIngredientSprite(entry);
            renderer.color = CookingRuntimeVisuals2D.GetIngredientColor(entry);
            renderer.sortingOrder = 40 + i;

            ingredientRenderers.Add(renderer);
        }
    }

    private void OnValidate()
    {
        requiredShakes = Mathf.Max(1, requiredShakes);
        minShakeDistance = Mathf.Max(0.01f, minShakeDistance);
        verticalNoiseRatio = Mathf.Max(0.1f, verticalNoiseRatio);
    }
}
