using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finishing stage for the icicle-fry dishes: shake the fryer basket up and down, then drag
/// the salt shaker over it to season. The "sweet &amp; spicy" variant adds a third step where
/// strawberries appear on top and need to be clicked to pop.
/// </summary>
public sealed class FryerFinishingStage2D : MonoBehaviour, IDishFinishingStage
{
    private enum Step
    {
        Shaking,
        Salting,
        Squeezing,
        Done
    }

    private const float ProgressBarWidth = 1.14f;
    private const float ProgressBarHeight = 0.16f;
    private const float ProgressBarLeftX = -ProgressBarWidth * 0.5f;
    private const float ProgressBarY = -0.9f;

    [SerializeField] private int requiredShakes = 6;
    [SerializeField] private float minShakeDistance = 0.4f;
    [SerializeField] private float shakeNoiseRatio = 1.1f;
    [SerializeField] private float requiredSaltDragDistance = 3.2f;
    [SerializeField] private int strawberryCount = 3;

    private readonly List<SpriteRenderer> ingredientRenderers = new();
    private readonly List<BoxCollider2D> strawberryColliders = new();
    private readonly List<GameObject> strawberries = new();

    private Transform fryerRoot;
    private Transform progressBarRoot;
    private PreparationPointerTool2D fryerHandle;
    private PreparationPointerTool2D saltShaker;
    private BoxCollider2D fryerArea;
    private SpriteRenderer progressFillRenderer;
    private DishResultPresenter2D presenter;
    private AlternatingStrokeTracker shakeTracker;
    private float saltDragProgress;
    private Camera targetCamera;
    private DishDefinition dish;
    private IReadOnlyList<CookingIngredientEntry> ingredients;
    private CookingMethodToolVisual2D toolVisual;
    private Step step;
    private bool includeSqueezeStep;
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
        includeSqueezeStep = newDish != null && newDish.FinishingStageKind == DishFinishingStageKind.FryerSaltSqueeze;
        shakeTracker = new AlternatingStrokeTracker(requiredShakes, minShakeDistance, shakeNoiseRatio, false);

        EnsureVisuals();
        ApplyProgressVisual();
    }

    private void Update()
    {
        if (presentingResult)
            return;

        switch (step)
        {
            case Step.Shaking:
                UpdateShaking();
                break;
            case Step.Salting:
                UpdateSalting();
                break;
            case Step.Squeezing:
                UpdateSqueezing();
                break;
        }
    }

    private void UpdateShaking()
    {
        if (fryerHandle == null || !fryerHandle.HasPointerMovement)
            return;

        Vector2 delta = fryerHandle.PointerWorldPosition - fryerHandle.PreviousPointerWorldPosition;
        if (!shakeTracker.ApplyMovement(delta))
            return;

        ApplyProgressVisual();

        if (shakeTracker.IsComplete)
            BeginSalting();
    }

    private void UpdateSalting()
    {
        if (saltShaker == null || !saltShaker.HasPointerMovement)
            return;

        if (!IsOverFryerArea(saltShaker.PointerWorldPosition))
            return;

        float moved = Vector2.Distance(saltShaker.PointerWorldPosition, saltShaker.PreviousPointerWorldPosition);
        saltDragProgress = Mathf.Min(requiredSaltDragDistance, saltDragProgress + moved);
        ApplyProgressVisual();

        if (saltDragProgress >= requiredSaltDragDistance)
        {
            if (includeSqueezeStep)
                BeginSqueezing();
            else
                CompleteInteraction();
        }
    }

    private void UpdateSqueezing()
    {
        if (!GatheringInput.WasPrimaryPressed() || !TryGetPointerWorldPosition(out Vector2 pointerWorldPosition))
            return;

        for (int i = strawberryColliders.Count - 1; i >= 0; i--)
        {
            BoxCollider2D collider = strawberryColliders[i];
            if (collider == null || !collider.OverlapPoint(pointerWorldPosition))
                continue;

            GameObject strawberry = strawberries[i];
            strawberries.RemoveAt(i);
            strawberryColliders.RemoveAt(i);
            if (strawberry != null)
                Destroy(strawberry);

            break;
        }

        ApplyProgressVisual();

        if (strawberryColliders.Count == 0)
            CompleteInteraction();
    }

    private void BeginSalting()
    {
        step = Step.Salting;

        if (fryerHandle != null)
            fryerHandle.gameObject.SetActive(false);

        EnsureSaltShaker();
        ApplyProgressVisual();
    }

    private void BeginSqueezing()
    {
        step = Step.Squeezing;

        if (saltShaker != null)
            saltShaker.gameObject.SetActive(false);

        EnsureStrawberries();
        ApplyProgressVisual();
    }

    private void CompleteInteraction()
    {
        if (step == Step.Done)
            return;

        step = Step.Done;
        InteractionCompleted?.Invoke();
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
        if (fryerRoot != null)
            fryerRoot.gameObject.SetActive(false);

        if (saltShaker != null)
            saltShaker.gameObject.SetActive(false);

        if (progressBarRoot != null)
            progressBarRoot.gameObject.SetActive(false);

        foreach (GameObject strawberry in strawberries)
        {
            if (strawberry != null)
                strawberry.SetActive(false);
        }
    }

    private bool IsOverFryerArea(Vector2 worldPosition)
    {
        if (fryerArea == null)
            return true;

        return fryerArea.bounds.Contains(new Vector3(worldPosition.x, worldPosition.y, fryerArea.bounds.center.z));
    }

    private bool TryGetPointerWorldPosition(out Vector2 worldPosition)
    {
        worldPosition = default;
        Camera camera = targetCamera != null ? targetCamera : Camera.main;
        if (camera == null)
            return false;

        Vector2 screenPosition = GatheringInput.PointerScreenPosition();
        float zDistance = Mathf.Abs(camera.transform.position.z - transform.position.z);
        worldPosition = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
        return true;
    }

    private void ApplyProgressVisual()
    {
        if (progressFillRenderer == null)
            return;

        float progress = step switch
        {
            Step.Shaking => shakeTracker.Progress * 0.4f,
            Step.Salting => 0.4f + Mathf.Clamp01(saltDragProgress / Mathf.Max(0.01f, requiredSaltDragDistance)) * (includeSqueezeStep ? 0.3f : 0.6f),
            Step.Squeezing => 0.7f + (1f - (float)strawberryColliders.Count / Mathf.Max(1, strawberryCount)) * 0.3f,
            _ => 1f
        };

        float width = Mathf.Max(0.02f, ProgressBarWidth * progress);
        progressFillRenderer.transform.localScale = new Vector3(width, ProgressBarHeight, 1f);
        progressFillRenderer.transform.localPosition = new Vector3(ProgressBarLeftX + width * 0.5f, ProgressBarY, 0f);
    }

    private void EnsureVisuals()
    {
        if (fryerRoot == null)
        {
            GameObject root = new("FryerHandle");
            root.transform.SetParent(transform, false);
            fryerRoot = root.transform;

            CookingRuntimeVisuals2D.CreateBlock("FryerShadow", transform, new Vector3(0f, -0.34f, 0f), new Vector3(1.7f, 0.3f, 1f), new Color(0.08f, 0.07f, 0.06f, 0.4f), 34);
            CookingRuntimeVisuals2D.CreateBlock("FryerBasketBody", fryerRoot, Vector3.zero, new Vector3(1.65f, 0.85f, 1f), new Color(0.35f, 0.3f, 0.24f, 1f), 36);
            CookingRuntimeVisuals2D.CreateBlock("FryerBasketRim", fryerRoot, new Vector3(0f, 0.4f, 0f), new Vector3(1.78f, 0.14f, 1f), new Color(0.5f, 0.42f, 0.32f, 1f), 37);

            fryerArea = root.AddComponent<BoxCollider2D>();
            fryerArea.isTrigger = true;
            fryerArea.size = new Vector2(1.78f, 1.0f);

            fryerHandle = root.AddComponent<PreparationPointerTool2D>();
            fryerHandle.Configure(targetCamera != null ? targetCamera : Camera.main);

            EnsureIngredientBits();
        }

        if (progressBarRoot == null)
        {
            GameObject barRoot = new("FryerProgressBar");
            barRoot.transform.SetParent(transform, false);
            progressBarRoot = barRoot.transform;

            CookingRuntimeVisuals2D.CreateBlock("FryerProgressTrack", progressBarRoot, new Vector3(0f, ProgressBarY, 0f), new Vector3(ProgressBarWidth, ProgressBarHeight, 1f), new Color(0.2f, 0.2f, 0.2f, 0.6f), 38);
            progressFillRenderer = CookingRuntimeVisuals2D.CreateBlock("FryerProgressFill", progressBarRoot, new Vector3(ProgressBarLeftX, ProgressBarY, 0f), new Vector3(0.02f, ProgressBarHeight, 1f), new Color(0.95f, 0.6f, 0.3f, 1f), 39);
        }
    }

    private void EnsureIngredientBits()
    {
        if (ingredientRenderers.Count > 0 || fryerRoot == null || ingredients == null)
            return;

        for (int i = 0; i < ingredients.Count; i++)
        {
            CookingIngredientEntry entry = ingredients[i];
            GameObject bit = new($"FryerIngredient_{i + 1}_{entry.ingredientId}");
            bit.transform.SetParent(fryerRoot, false);

            float spread = ingredients.Count <= 1 ? 0f : Mathf.Lerp(-0.5f, 0.5f, i / (float)(ingredients.Count - 1));
            bit.transform.localPosition = new Vector3(spread, 0.14f, 0f);
            bit.transform.localScale = Vector3.one * 0.3f;

            SpriteRenderer renderer = bit.AddComponent<SpriteRenderer>();
            renderer.sprite = CookingRuntimeVisuals2D.GetIngredientSprite(entry);
            renderer.color = CookingRuntimeVisuals2D.GetIngredientColor(entry);
            renderer.sortingOrder = 40 + i;

            ingredientRenderers.Add(renderer);
        }
    }

    private void EnsureSaltShaker()
    {
        if (saltShaker != null)
        {
            saltShaker.gameObject.SetActive(true);
            return;
        }

        GameObject shaker = new("SaltShaker");
        shaker.transform.SetParent(transform, false);
        shaker.transform.localPosition = new Vector3(-1.3f, 1.0f, 0f);
        shaker.transform.localScale = new Vector3(0.4f, 0.55f, 1f);

        SpriteRenderer renderer = shaker.AddComponent<SpriteRenderer>();
        renderer.color = new Color(0.85f, 0.86f, 0.88f, 1f);
        renderer.sprite = InventoryVisualUtility.WhiteSprite;
        renderer.sortingOrder = 46;

        BoxCollider2D collider = shaker.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        saltShaker = shaker.AddComponent<PreparationPointerTool2D>();
        saltShaker.Configure(targetCamera != null ? targetCamera : Camera.main);
    }

    private void EnsureStrawberries()
    {
        int count = Mathf.Max(1, strawberryCount);
        for (int i = 0; i < count; i++)
        {
            GameObject strawberry = new($"PopStrawberry_{i + 1}");
            strawberry.transform.SetParent(transform, false);
            float spread = count <= 1 ? 0f : Mathf.Lerp(-0.55f, 0.55f, i / (float)(count - 1));
            strawberry.transform.localPosition = new Vector3(spread, 0.7f, 0f);
            strawberry.transform.localScale = Vector3.one * 0.28f;

            SpriteRenderer renderer = strawberry.AddComponent<SpriteRenderer>();
            renderer.sprite = InventoryVisualUtility.WhiteSprite;
            renderer.color = new Color(0.86f, 0.16f, 0.24f, 1f);
            renderer.sortingOrder = 47;

            BoxCollider2D collider = strawberry.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;

            strawberries.Add(strawberry);
            strawberryColliders.Add(collider);
        }
    }

    private void OnValidate()
    {
        requiredShakes = Mathf.Max(1, requiredShakes);
        minShakeDistance = Mathf.Max(0.01f, minShakeDistance);
        shakeNoiseRatio = Mathf.Max(0.1f, shakeNoiseRatio);
        requiredSaltDragDistance = Mathf.Max(0.1f, requiredSaltDragDistance);
        strawberryCount = Mathf.Max(1, strawberryCount);
    }
}
