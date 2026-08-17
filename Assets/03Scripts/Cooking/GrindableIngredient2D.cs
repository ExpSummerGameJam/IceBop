using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class GrindableIngredient2D : IngredientPreparationBehaviour2D
{
    [SerializeField] private SpriteRenderer ingredientRenderer;
    [SerializeField] private PreparationPointerTool2D grindingTool;
    [SerializeField] private ParticleSystem powderParticles;
    [SerializeField] private int requiredAlternatingStrokes = 8;
    [SerializeField] private float minVerticalStrokeDistance = 0.42f;
    [SerializeField] private float horizontalNoiseRatio = 1.15f;
    [SerializeField] private float remainingScaleY = 0.001f;
    [SerializeField] private int powderEmitCount = 12;
    [SerializeField] private Color groundColor = new(0.86f, 0.72f, 0.44f, 1f);
    [SerializeField] private SpriteMask visibleMask;
    [SerializeField] private SpriteRenderer groundRenderer;
    [SerializeField] private Vector2 groundLocalOffset = new(0f, -0.72f);
    [SerializeField] private Vector2 groundFinalScale = new(1.12f, 0.18f);

    private PolygonCollider2D ingredientCollider;
    private Bounds sourceBounds;
    private float progress;
    private int currentStrokeDirection;
    private int lastCompletedStrokeDirection;
    private int grindStartSide;
    private float currentStrokeDistance;

    public override Color? ProcessedColor => Color.Lerp(Color.white, groundColor, Mathf.Clamp01(progress) * 0.55f);

    public void BindGrindingTool(PreparationPointerTool2D newGrindingTool)
    {
        grindingTool = newGrindingTool;
    }

    /// <summary>Resets this instance for reuse against a (possibly different) ingredient sprite - call after re-skinning the SpriteRenderer for a new cook. Awake() only runs once per GameObject lifetime, so pooled reuse needs this instead.</summary>
    public void Rearm()
    {
        ResetPreparationState();

        // Fitting the slot's collider to this cook's sprite can replace the component, and in
        // play mode the old one only reads as null at end of frame - so drop the cached
        // reference and let ResolveComponents pick up the current one.
        ingredientCollider = null;
        ResolveComponents();

        sourceBounds = ingredientRenderer != null && ingredientRenderer.sprite != null
            ? ingredientRenderer.sprite.bounds
            : new Bounds(Vector3.zero, new Vector3(3f, 2f, 0f));

        progress = 0f;
        currentStrokeDirection = 0;
        lastCompletedStrokeDirection = 0;
        grindStartSide = 0;
        currentStrokeDistance = 0f;

        if (ingredientRenderer != null)
        {
            ingredientRenderer.enabled = true;
            ingredientRenderer.color = Color.white;
        }

        EnsureVisibleMask();
        EnsureGroundVisual();
        EnsurePowderParticles();
        ApplyProgressVisuals();
    }

    private void Awake()
    {
        SetPreparationKind(IngredientPreparationKind.Grinding);
        ResolveComponents();
        sourceBounds = ingredientRenderer != null && ingredientRenderer.sprite != null
            ? ingredientRenderer.sprite.bounds
            : new Bounds(Vector3.zero, new Vector3(3f, 2f, 0f));
        EnsureVisibleMask();
        EnsureGroundVisual();
        EnsurePowderParticles();
        ApplyProgressVisuals();
    }

    private void Update()
    {
        if (IsPrepared || grindingTool == null || !grindingTool.HasPointerMovement)
            return;

        ApplyGrindingMovement(grindingTool.PreviousPointerWorldPosition, grindingTool.PointerWorldPosition);
    }

    private void ApplyGrindingMovement(Vector2 previousPointerPosition, Vector2 currentPointerPosition)
    {
        if (!IsPointerOverIngredient(currentPointerPosition))
        {
            currentStrokeDirection = 0;
            currentStrokeDistance = 0f;
            return;
        }

        Vector2 delta = currentPointerPosition - previousPointerPosition;
        if (Mathf.Abs(delta.y) <= 0.001f || Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * horizontalNoiseRatio)
            return;

        int direction = delta.y > 0f ? 1 : -1;
        if (direction != currentStrokeDirection)
        {
            currentStrokeDirection = direction;
            currentStrokeDistance = 0f;
        }

        currentStrokeDistance += Mathf.Abs(delta.y);
        if (currentStrokeDistance < minVerticalStrokeDistance || direction == lastCompletedStrokeDirection)
            return;

        CompleteGrindingStroke(direction, previousPointerPosition);
        currentStrokeDistance = 0f;
    }

    private void CompleteGrindingStroke(int direction, Vector2 strokeStartWorldPosition)
    {
        if (grindStartSide == 0)
            grindStartSide = ResolveGrindStartSide(strokeStartWorldPosition);

        lastCompletedStrokeDirection = direction;
        progress = Mathf.Clamp01(progress + 1f / Mathf.Max(1, requiredAlternatingStrokes));
        ApplyProgressVisuals();
        EmitPowder();

        if (progress >= 1f)
            TryMarkPrepared();
    }

    private void ApplyProgressVisuals()
    {
        ApplyMaskVisual();
        ApplyGroundVisual();

        if (ingredientRenderer != null)
            ingredientRenderer.color = Color.Lerp(Color.white, groundColor, progress * 0.55f);

        UpdatePowderPosition();
    }

    private void ApplyMaskVisual()
    {
        EnsureVisibleMask();

        if (visibleMask == null)
            return;

        float visibleRatio = Mathf.Lerp(1f, Mathf.Max(0.001f, remainingScaleY), progress);
        float visibleHeight = Mathf.Max(0.001f, sourceBounds.size.y * visibleRatio);
        int startSide = grindStartSide == 0 ? 1 : grindStartSide;
        float localY = startSide > 0
            ? sourceBounds.min.y + visibleHeight * 0.5f
            : sourceBounds.max.y - visibleHeight * 0.5f;

        visibleMask.transform.localPosition = new Vector3(sourceBounds.center.x, localY, 0f);
        visibleMask.transform.localScale = new Vector3(sourceBounds.size.x + 0.02f, visibleHeight + 0.02f, 1f);

        if (ingredientRenderer != null)
            ingredientRenderer.enabled = progress < 0.995f;
    }

    private void ApplyGroundVisual()
    {
        EnsureGroundVisual();

        if (groundRenderer == null)
            return;

        float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
        groundRenderer.transform.localPosition = new Vector3(
            sourceBounds.center.x + groundLocalOffset.x,
            sourceBounds.min.y + groundLocalOffset.y,
            0f);
        groundRenderer.transform.localScale = new Vector3(
            Mathf.Lerp(0.24f, groundFinalScale.x, easedProgress),
            Mathf.Lerp(0.04f, groundFinalScale.y, easedProgress),
            1f);

        Color color = groundColor;
        color.a = Mathf.Lerp(0f, groundColor.a, easedProgress);
        groundRenderer.color = color;
        groundRenderer.enabled = progress > 0.001f;
    }

    private void EmitPowder()
    {
        if (powderParticles == null)
            return;

        UpdatePowderPosition();
        powderParticles.Emit(Mathf.Max(1, powderEmitCount));
    }

    private bool IsPointerOverIngredient(Vector2 worldPosition)
    {
        if (ingredientCollider != null)
            return ingredientCollider.bounds.Contains(new Vector3(worldPosition.x, worldPosition.y, ingredientCollider.bounds.center.z));

        return Vector2.Distance(worldPosition, transform.position) <= Mathf.Max(sourceBounds.extents.x, sourceBounds.extents.y);
    }

    private int ResolveGrindStartSide(Vector2 strokeStartWorldPosition)
    {
        Vector3 localPosition = transform.InverseTransformPoint(strokeStartWorldPosition);
        return localPosition.y >= sourceBounds.center.y ? 1 : -1;
    }

    private void EnsurePowderParticles()
    {
        if (powderParticles == null)
        {
            Transform existing = transform.Find("PowderParticles");
            if (existing != null)
                powderParticles = existing.GetComponent<ParticleSystem>();
        }

        if (powderParticles == null)
        {
            GameObject particles = new("PowderParticles");
            particles.transform.SetParent(transform, false);
            powderParticles = particles.AddComponent<ParticleSystem>();
        }

        ParticleSystem.MainModule main = powderParticles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.72f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.78f, 0.63f, 0.35f, 0.85f));
        main.gravityModifier = 0.35f;

        ParticleSystem.EmissionModule emission = powderParticles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = powderParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.28f;

        ParticleSystemRenderer renderer = powderParticles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = ingredientRenderer != null ? ingredientRenderer.sortingOrder + 1 : 6;

        UpdatePowderPosition();
    }

    private void EnsureVisibleMask()
    {
        if (ingredientRenderer == null)
            return;

        if (visibleMask == null)
        {
            Transform existing = transform.Find("GrindingVisibleMask");
            if (existing != null)
                visibleMask = existing.GetComponent<SpriteMask>();
        }

        if (visibleMask == null)
        {
            GameObject maskObject = new("GrindingVisibleMask");
            maskObject.transform.SetParent(transform, false);
            visibleMask = maskObject.AddComponent<SpriteMask>();
        }

        visibleMask.sprite = InventoryVisualUtility.WhiteSprite;
        visibleMask.isCustomRangeActive = true;
        visibleMask.frontSortingLayerID = ingredientRenderer.sortingLayerID;
        visibleMask.frontSortingOrder = ingredientRenderer.sortingOrder + 1;
        visibleMask.backSortingLayerID = ingredientRenderer.sortingLayerID;
        visibleMask.backSortingOrder = ingredientRenderer.sortingOrder - 1;
        ingredientRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }

    private void EnsureGroundVisual()
    {
        if (groundRenderer == null)
        {
            Transform existing = transform.Find("GroundIngredient");
            if (existing != null)
                groundRenderer = existing.GetComponent<SpriteRenderer>();
        }

        if (groundRenderer == null)
        {
            GameObject ground = new("GroundIngredient");
            ground.transform.SetParent(transform, false);
            groundRenderer = ground.AddComponent<SpriteRenderer>();
        }

        groundRenderer.sprite = ingredientRenderer != null && ingredientRenderer.sprite != null
            ? ingredientRenderer.sprite
            : InventoryVisualUtility.WhiteSprite;
        groundRenderer.sortingLayerID = ingredientRenderer != null ? ingredientRenderer.sortingLayerID : 0;
        groundRenderer.sortingOrder = ingredientRenderer != null ? ingredientRenderer.sortingOrder + 1 : 6;
        groundRenderer.maskInteraction = SpriteMaskInteraction.None;
    }

    private void UpdatePowderPosition()
    {
        if (powderParticles == null)
            return;

        float halfHeight = sourceBounds.extents.y * transform.lossyScale.y;
        powderParticles.transform.position = transform.position + Vector3.down * (halfHeight + 0.08f);
        powderParticles.transform.rotation = Quaternion.identity;
        powderParticles.transform.localScale = Vector3.one;
    }

    private void ResolveComponents()
    {
        if (ingredientRenderer == null)
            ingredientRenderer = GetComponent<SpriteRenderer>();

        if (ingredientCollider == null)
            ingredientCollider = GetComponent<PolygonCollider2D>();
    }

    private void OnValidate()
    {
        requiredAlternatingStrokes = Mathf.Max(1, requiredAlternatingStrokes);
        minVerticalStrokeDistance = Mathf.Max(0.01f, minVerticalStrokeDistance);
        horizontalNoiseRatio = Mathf.Max(0.1f, horizontalNoiseRatio);
        remainingScaleY = Mathf.Clamp(remainingScaleY, 0.001f, 1f);
        powderEmitCount = Mathf.Max(1, powderEmitCount);
        groundFinalScale.x = Mathf.Max(0.01f, groundFinalScale.x);
        groundFinalScale.y = Mathf.Max(0.01f, groundFinalScale.y);
    }
}
