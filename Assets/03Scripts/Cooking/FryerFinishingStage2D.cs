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
    [SerializeField] private int requiredSaltShakes = 6;
    [SerializeField] private float minSaltShakeDistance = 0.28f;
    [SerializeField] private float saltShakeNoiseRatio = 1.1f;
    [SerializeField] private int strawberryCount = 3;
    [SerializeField] private Color rockSaltColor = new(0.82f, 0.78f, 0.74f, 1f);
    [SerializeField] private int saltEmitCountPerDrag = 5;

    private readonly List<SpriteRenderer> ingredientRenderers = new();
    private readonly List<GameObject> strawberryPool = new();
    private readonly List<BoxCollider2D> strawberryPoolColliders = new();
    private readonly List<GameObject> strawberries = new();
    private readonly List<BoxCollider2D> strawberryColliders = new();

    private Coroutine dropInRoutine;
    private Transform fryerRoot;
    private Transform progressBarRoot;
    private PreparationPointerTool2D fryerHandle;
    private PreparationPointerTool2D saltShaker;
    private ParticleSystem saltParticles;
    private Collider2D fryerArea;
    private SpriteRenderer progressFillRenderer;
    private DishResultPresenter2D presenter;
    private AlternatingStrokeTracker shakeTracker;
    private AlternatingStrokeTracker saltShakeTracker;
    private Camera targetCamera;
    private InventoryView2D inventoryView;
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
        CookingMethodToolVisual2D newToolVisual,
        InventoryView2D newInventoryView)
    {
        targetCamera = camera;
        inventoryView = newInventoryView;
        ingredients = newIngredients;
        dish = newDish;
        toolVisual = newToolVisual;
        includeSqueezeStep = newDish != null && newDish.FinishingStageKind == DishFinishingStageKind.FryerSaltSqueeze;
        shakeTracker = new AlternatingStrokeTracker(requiredShakes, minShakeDistance, shakeNoiseRatio, false);
        saltShakeTracker = new AlternatingStrokeTracker(requiredSaltShakes, minSaltShakeDistance, saltShakeNoiseRatio, false);
        step = Step.Shaking;
        presentingResult = false;

        EnsureVisuals();
        RebuildIngredientBits();
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

    // Salting isn't a wipe-across-the-food motion - the shaker is held above the fryer and
    // shaken up and down, same as the fryer-shake step, and each completed shake showers
    // salt down into the basket. Progress is purely how much it's been shaken above the
    // fryer, not how far it's been dragged across any particular spot.
    private void UpdateSalting()
    {
        if (saltShaker == null || !saltShaker.HasPointerMovement)
            return;

        if (!IsOverFryerArea(saltShaker.PointerWorldPosition))
            return;

        Vector2 delta = saltShaker.PointerWorldPosition - saltShaker.PreviousPointerWorldPosition;
        if (!saltShakeTracker.ApplyMovement(delta))
            return;

        EmitSalt();
        ApplyProgressVisual();

        if (saltShakeTracker.IsComplete)
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
                strawberry.SetActive(false);

            break;
        }

        ApplyProgressVisual();

        if (strawberryColliders.Count == 0)
            CompleteInteraction();
    }

    private void BeginSalting()
    {
        step = Step.Salting;

        // Only stop the fryer from being dragged around - the basket itself (and its
        // salting-area collider, checked by IsOverFryerArea) must stay visible/in place
        // for the rest of the finishing stage, not disappear once shaking is done.
        if (fryerHandle != null)
            fryerHandle.enabled = false;

        EnsureSaltShaker();
        ApplyProgressVisual();
    }

    private void BeginSqueezing()
    {
        step = Step.Squeezing;

        if (saltShaker != null)
            saltShaker.gameObject.SetActive(false);

        RearmStrawberries();
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

        foreach (GameObject strawberry in strawberryPool)
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
        if (inventoryView != null)
            return inventoryView.TryGetLargeViewPointerWorldPosition(out worldPosition);

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
            Step.Salting => 0.4f + saltShakeTracker.Progress * (includeSqueezeStep ? 0.3f : 0.6f),
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
            fryerRoot = transform.Find("Fryer");

        if (fryerRoot != null)
        {
            fryerRoot.gameObject.SetActive(true);
            EnsureFryerHandle();
        }

        if (progressBarRoot == null)
        {
            GameObject barRoot = new("FryerProgressBar");
            barRoot.transform.SetParent(transform, false);
            progressBarRoot = barRoot.transform;

            CookingRuntimeVisuals2D.CreateBlock("FryerProgressTrack", progressBarRoot, new Vector3(0f, ProgressBarY, 0f), new Vector3(ProgressBarWidth, ProgressBarHeight, 1f), new Color(0.2f, 0.2f, 0.2f, 0.6f), 38);
            progressFillRenderer = CookingRuntimeVisuals2D.CreateBlock("FryerProgressFill", progressBarRoot, new Vector3(ProgressBarLeftX, ProgressBarY, 0f), new Vector3(0.02f, ProgressBarHeight, 1f), new Color(0.95f, 0.6f, 0.3f, 1f), 39);
        }

        progressBarRoot.gameObject.SetActive(true);
    }

    // The player grabs the whole hand-placed "Fryer" body (parent of the "PotWall" rim art)
    // and shakes it - no separate procedural handle prop is built anymore.
    private void EnsureFryerHandle()
    {
        if (fryerRoot == null)
            return;

        // The hand-authored EdgeCollider2D on the child "PotWall" is a thin rim outline
        // (edgeRadius 0), so OverlapPoint only registers a hit within a hair of the exact
        // outline. Add a filled trigger collider on the parent body object itself, sized to
        // its sprite, so the whole visible fryer basket is grabbable and the salting-drag area
        // check has a real bounds to test against.
        BoxCollider2D dragCollider = fryerRoot.GetComponent<BoxCollider2D>();
        if (dragCollider == null)
            dragCollider = fryerRoot.gameObject.AddComponent<BoxCollider2D>();

        dragCollider.isTrigger = true;
        SpriteRenderer bodyRenderer = fryerRoot.GetComponent<SpriteRenderer>();
        if (bodyRenderer != null && bodyRenderer.sprite != null)
        {
            dragCollider.size = bodyRenderer.sprite.bounds.size;
            dragCollider.offset = bodyRenderer.sprite.bounds.center;
        }

        fryerArea = dragCollider;

        fryerHandle = fryerRoot.GetComponent<PreparationPointerTool2D>();
        if (fryerHandle == null)
            fryerHandle = fryerRoot.gameObject.AddComponent<PreparationPointerTool2D>();

        fryerHandle.gameObject.SetActive(true);
        fryerHandle.enabled = true;
        fryerHandle.Configure(targetCamera != null ? targetCamera : Camera.main, inventoryView);
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

        if (fryerRoot == null || ingredients == null)
            return;

        List<Vector3> spawnPositions = new(ingredients.Count);

        for (int i = 0; i < ingredients.Count; i++)
        {
            CookingIngredientEntry entry = ingredients[i];
            GameObject bit = new($"FryerIngredient_{i + 1}_{entry.ingredientId}");
            bit.transform.SetParent(fryerRoot, false);
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

    private void EnsureSaltShaker()
    {
        if (saltShaker != null)
        {
            saltShaker.gameObject.SetActive(true);
            saltShaker.Configure(targetCamera != null ? targetCamera : Camera.main, inventoryView);
            return;
        }

        GameObject shaker = new("SaltShaker");
        shaker.transform.SetParent(transform, false);
        shaker.transform.localPosition = new Vector3(-1.3f, 1.0f, 0f);
        shaker.transform.localScale = new Vector3(0.4f, 0.55f, 1f);

        SpriteRenderer renderer = shaker.AddComponent<SpriteRenderer>();
        renderer.sprite = CookingRuntimeVisuals2D.GetSaltyIceSprite();
        renderer.color = Color.white;
        renderer.sortingOrder = 46;

        BoxCollider2D collider = shaker.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = renderer.sprite != null ? renderer.sprite.bounds.size : Vector2.one;

        saltShaker = shaker.AddComponent<PreparationPointerTool2D>();
        saltShaker.Configure(targetCamera != null ? targetCamera : Camera.main, inventoryView);

        EnsureSaltParticles();
    }

    // Coarse, chunky rock-salt bits scattered from the shaker while it's dragged over the
    // fryer basket - distinct from the fine GrindableIngredient2D powder look.
    private void EnsureSaltParticles()
    {
        if (saltParticles != null)
            return;

        Transform existing = transform.Find("RockSaltParticles");
        GameObject particlesObject = existing != null ? existing.gameObject : new GameObject("RockSaltParticles");
        particlesObject.transform.SetParent(transform, false);
        saltParticles = particlesObject.GetComponent<ParticleSystem>();
        if (saltParticles == null)
            saltParticles = particlesObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = saltParticles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
        main.startColor = new ParticleSystem.MinMaxGradient(rockSaltColor);
        main.gravityModifier = 0.65f;

        ParticleSystem.EmissionModule emission = saltParticles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = saltParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.18f;

        ParticleSystemRenderer particleRenderer = saltParticles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sortingOrder = 45;
    }

    private void EmitSalt()
    {
        if (saltParticles == null || saltShaker == null)
            return;

        saltParticles.transform.position = saltShaker.transform.position;
        saltParticles.Emit(Mathf.Max(1, saltEmitCountPerDrag));
    }

    private void EnsureStrawberryPool()
    {
        if (strawberryPool.Count > 0)
            return;

        int count = Mathf.Max(1, strawberryCount);
        for (int i = 0; i < count; i++)
        {
            GameObject strawberry = new($"PopStrawberry_{i + 1}");
            strawberry.transform.SetParent(transform, false);
            strawberry.transform.localScale = Vector3.one * 0.28f;

            SpriteRenderer renderer = strawberry.AddComponent<SpriteRenderer>();
            renderer.sprite = CookingRuntimeVisuals2D.GetStrawberryIceSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = 47;

            BoxCollider2D collider = strawberry.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = renderer.sprite != null ? renderer.sprite.bounds.size : Vector2.one;

            strawberry.SetActive(false);
            strawberryPool.Add(strawberry);
            strawberryPoolColliders.Add(collider);
        }
    }

    private void RearmStrawberries()
    {
        EnsureStrawberryPool();

        strawberries.Clear();
        strawberryColliders.Clear();

        int count = strawberryPool.Count;
        for (int i = 0; i < count; i++)
        {
            GameObject strawberry = strawberryPool[i];
            BoxCollider2D collider = strawberryPoolColliders[i];
            if (strawberry == null || collider == null)
                continue;

            float spread = count <= 1 ? 0f : Mathf.Lerp(-0.55f, 0.55f, i / (float)(count - 1));
            strawberry.transform.localPosition = new Vector3(spread, 0.7f, 0f);
            collider.enabled = true;
            strawberry.SetActive(true);

            strawberries.Add(strawberry);
            strawberryColliders.Add(collider);
        }
    }

    private void OnValidate()
    {
        requiredShakes = Mathf.Max(1, requiredShakes);
        minShakeDistance = Mathf.Max(0.01f, minShakeDistance);
        shakeNoiseRatio = Mathf.Max(0.1f, shakeNoiseRatio);
        requiredSaltShakes = Mathf.Max(1, requiredSaltShakes);
        minSaltShakeDistance = Mathf.Max(0.01f, minSaltShakeDistance);
        saltShakeNoiseRatio = Mathf.Max(0.1f, saltShakeNoiseRatio);
        strawberryCount = Mathf.Max(1, strawberryCount);
        saltEmitCountPerDrag = Mathf.Max(1, saltEmitCountPerDrag);
    }
}
