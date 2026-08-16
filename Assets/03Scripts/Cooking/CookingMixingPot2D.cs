using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class CookingMixingPot2D : MonoBehaviour
{
    [SerializeField] private PreparationPointerTool2D ladleTool;
    [SerializeField] private int requiredAlternatingStrokes = 8;
    [SerializeField] private float minHorizontalStrokeDistance = 0.48f;
    [SerializeField] private float verticalNoiseRatio = 1.15f;
    [SerializeField] private float finishTravelDuration = 0.65f;
    [SerializeField] private float returnTravelDuration = 0.75f;

    private readonly List<SpriteRenderer> ingredientRenderers = new();
    private readonly List<Vector3> ingredientStartPositions = new();
    private readonly List<Vector3> ingredientStartScales = new();

    private BoxCollider2D potCollider;
    private Transform contentRoot;
    private SpriteRenderer liquidRenderer;
    private SpriteRenderer dishRenderer;
    private TextMesh dishLabel;
    private Camera targetCamera;
    private DishDefinition dish;
    private IReadOnlyList<CookingIngredientEntry> ingredients;
    private CookingMethodToolVisual2D toolVisual;
    private GameObject potVisualInstance;
    private float mixProgress;
    private int currentStrokeDirection;
    private int lastCompletedStrokeDirection;
    private float currentStrokeDistance;
    private bool mixingCompleted;
    private bool presentingResult;

    public event Action<CookingMixingPot2D> MixingCompleted;
    public event Action<CookingMixingPot2D> DishPresented;

    public void Configure(Camera camera, IReadOnlyList<CookingIngredientEntry> newIngredients, DishDefinition newDish)
    {
        Configure(camera, newIngredients, newDish, null);
    }

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

        if (toolVisual != null && toolVisual.mixingPotScale != Vector3.zero)
            transform.localScale = toolVisual.mixingPotScale;

        EnsureVisuals();
        ApplyMixVisuals();
    }

    private void Awake()
    {
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

        StartCoroutine(ResultPresentationRoutine());
    }

    private void ApplyLadleMovement(Vector2 previousPointerPosition, Vector2 currentPointerPosition)
    {
        if (!IsPointerOverPot(currentPointerPosition))
        {
            currentStrokeDirection = 0;
            currentStrokeDistance = 0f;
            return;
        }

        Vector2 delta = currentPointerPosition - previousPointerPosition;
        if (Mathf.Abs(delta.x) <= 0.001f || Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * verticalNoiseRatio)
            return;

        int direction = delta.x > 0f ? 1 : -1;
        if (direction != currentStrokeDirection)
        {
            currentStrokeDirection = direction;
            currentStrokeDistance = 0f;
        }

        currentStrokeDistance += Mathf.Abs(delta.x);
        if (currentStrokeDistance < minHorizontalStrokeDistance || direction == lastCompletedStrokeDirection)
            return;

        CompleteMixStroke(direction);
        currentStrokeDistance = 0f;
    }

    private void CompleteMixStroke(int direction)
    {
        lastCompletedStrokeDirection = direction;
        mixProgress = Mathf.Clamp01(mixProgress + 1f / Mathf.Max(1, requiredAlternatingStrokes));
        ApplyMixVisuals();
        EmitBubbles();

        if (mixProgress >= 1f && !mixingCompleted)
        {
            mixingCompleted = true;
            MixingCompleted?.Invoke(this);
        }
    }

    private void ApplyMixVisuals()
    {
        float progress = Mathf.Clamp01(mixProgress);

        if (liquidRenderer != null)
        {
            liquidRenderer.transform.localScale = new Vector3(
                Mathf.Lerp(0.32f, 1.20f, progress),
                Mathf.Lerp(0.08f, 0.44f, progress),
                1f);
            liquidRenderer.transform.localPosition = new Vector3(0f, Mathf.Lerp(-0.20f, 0.03f, progress), 0f);
            liquidRenderer.color = Color.Lerp(new Color(0.50f, 0.30f, 0.14f, 0.38f), new Color(0.78f, 0.48f, 0.20f, 0.92f), progress);
        }

        for (int i = 0; i < ingredientRenderers.Count; i++)
        {
            SpriteRenderer renderer = ingredientRenderers[i];
            if (renderer == null)
                continue;

            Color color = renderer.color;
            color.a = Mathf.Lerp(1f, 0.04f, progress);
            renderer.color = color;

            Vector3 startPosition = ingredientStartPositions[i];
            Vector3 startScale = ingredientStartScales[i];
            renderer.transform.localPosition = Vector3.Lerp(startPosition, new Vector3(startPosition.x * 0.35f, -0.08f, 0f), progress);
            renderer.transform.localScale = Vector3.Lerp(startScale, startScale * 0.18f, progress);
        }
    }

    private bool IsPointerOverPot(Vector2 worldPosition)
    {
        if (potCollider == null)
            return Vector2.Distance(worldPosition, transform.position) <= 1.25f;

        return potCollider.bounds.Contains(new Vector3(worldPosition.x, worldPosition.y, potCollider.bounds.center.z));
    }

    private IEnumerator ResultPresentationRoutine()
    {
        presentingResult = true;

        if (ladleTool != null)
            ladleTool.gameObject.SetActive(false);

        Vector3 startPosition = transform.position;
        Vector3 offscreenPosition = GetRightOffscreenPosition(startPosition);
        yield return MoveTo(startPosition, offscreenPosition, finishTravelDuration);

        ShowDishResult();
        yield return MoveTo(offscreenPosition, startPosition, returnTravelDuration);

        DishPresented?.Invoke(this);
    }

    private IEnumerator MoveTo(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            t = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.position = to;
    }

    private Vector3 GetRightOffscreenPosition(Vector3 fallback)
    {
        Camera camera = targetCamera != null ? targetCamera : Camera.main;
        if (camera == null)
            return fallback + Vector3.right * 5f;

        float zDistance = Mathf.Abs(camera.transform.position.z - fallback.z);
        Vector3 rightEdge = camera.ViewportToWorldPoint(new Vector3(1.2f, 0.5f, zDistance));
        return new Vector3(rightEdge.x + 1.2f, fallback.y, fallback.z);
    }

    private void ShowDishResult()
    {
        foreach (SpriteRenderer ingredientRenderer in ingredientRenderers)
        {
            if (ingredientRenderer != null)
                ingredientRenderer.gameObject.SetActive(false);
        }

        if (liquidRenderer != null)
            liquidRenderer.gameObject.SetActive(false);

        if (dishRenderer == null)
        {
            GameObject dishObject = new("CompletedDish");
            dishObject.transform.SetParent(transform, false);
            dishObject.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            dishObject.transform.localScale = new Vector3(0.78f, 0.78f, 1f);
            dishRenderer = dishObject.AddComponent<SpriteRenderer>();
            dishRenderer.sortingOrder = 43;
        }

        dishRenderer.sprite = CookingRuntimeVisuals2D.GetDishSprite(dish, ingredients);
        dishRenderer.color = Color.white;
        dishRenderer.gameObject.SetActive(true);

        if (dishLabel == null)
        {
            GameObject labelObject = new("CompletedDishLabel");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, -0.96f, 0f);
            dishLabel = labelObject.AddComponent<TextMesh>();
            dishLabel.anchor = TextAnchor.MiddleCenter;
            dishLabel.alignment = TextAlignment.Center;
            dishLabel.fontSize = 34;
            dishLabel.characterSize = 0.055f;
            dishLabel.color = new Color(0.94f, 0.96f, 0.92f, 1f);

            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sortingOrder = 44;
        }

        dishLabel.text = dish != null && !string.IsNullOrWhiteSpace(dish.displayName)
            ? dish.displayName
            : "Completed Dish";
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
        if (potCollider == null)
        {
            potCollider = GetComponent<BoxCollider2D>();
            if (potCollider == null)
                potCollider = gameObject.AddComponent<BoxCollider2D>();

            potCollider.isTrigger = true;
            potCollider.size = new Vector2(2.15f, 1.45f);
            potCollider.offset = new Vector2(0f, -0.04f);
        }

        if (contentRoot == null)
        {
            GameObject root = new("MixingContent");
            root.transform.SetParent(transform, false);
            contentRoot = root.transform;
        }

        if (potVisualInstance == null && toolVisual != null && toolVisual.mixingPotPrefab != null)
        {
            potVisualInstance = Instantiate(toolVisual.mixingPotPrefab, transform, false);
            potVisualInstance.name = "MixPotVisual";
        }

        if (potVisualInstance == null && transform.Find("MixPotBody") == null)
        {
            CookingRuntimeVisuals2D.CreateBlock("MixPotShadow", transform, new Vector3(0f, -0.26f, 0f), new Vector3(2.20f, 0.76f, 1f), new Color(0.13f, 0.12f, 0.11f, 1f), 35);
            SpriteRenderer potBody = CookingRuntimeVisuals2D.CreateBlock("MixPotBody", transform, new Vector3(0f, -0.15f, 0f), new Vector3(2.02f, 1.02f, 1f), new Color(0.24f, 0.27f, 0.29f, 1f), 36);
            if (toolVisual != null && toolVisual.mixingPotBodySprite != null)
                potBody.sprite = toolVisual.mixingPotBodySprite;

            CookingRuntimeVisuals2D.CreateBlock("MixPotRim", transform, new Vector3(0f, 0.39f, 0f), new Vector3(2.28f, 0.24f, 1f), new Color(0.48f, 0.51f, 0.53f, 1f), 41);
        }

        if (liquidRenderer == null)
        {
            liquidRenderer = CookingRuntimeVisuals2D.CreateBlock("MixLiquid", contentRoot, new Vector3(0f, -0.20f, 0f), new Vector3(0.32f, 0.08f, 1f), new Color(0.50f, 0.30f, 0.14f, 0.38f), 39);
        }

        EnsureIngredientBits();
        EnsureLadle();
    }

    private void EnsureIngredientBits()
    {
        if (ingredientRenderers.Count > 0 || contentRoot == null || ingredients == null)
            return;

        for (int i = 0; i < ingredients.Count; i++)
        {
            CookingIngredientEntry entry = ingredients[i];
            GameObject bit = new($"MixIngredient_{i + 1}_{entry.ingredientId}");
            bit.transform.SetParent(contentRoot, false);

            float angle = ingredients.Count <= 1 ? 0f : (Mathf.PI * 2f * i) / ingredients.Count;
            Vector3 localPosition = new(Mathf.Cos(angle) * 0.46f, Mathf.Sin(angle) * 0.12f + 0.15f, 0f);
            Vector3 localScale = Vector3.one * 0.24f;
            bit.transform.localPosition = localPosition;
            bit.transform.localScale = localScale;

            SpriteRenderer renderer = bit.AddComponent<SpriteRenderer>();
            renderer.sprite = CookingRuntimeVisuals2D.GetIngredientSprite(entry);
            renderer.color = CookingRuntimeVisuals2D.GetIngredientColor(entry);
            renderer.sortingOrder = 40 + i;

            ingredientRenderers.Add(renderer);
            ingredientStartPositions.Add(localPosition);
            ingredientStartScales.Add(localScale);
        }
    }

    private void EnsureLadle()
    {
        if (ladleTool != null)
            return;

        GameObject ladle = toolVisual != null && toolVisual.mixingToolPrefab != null
            ? Instantiate(toolVisual.mixingToolPrefab, transform, false)
            : new GameObject("MixingLadle");

        ladle.name = "MixingLadle";
        ladle.transform.SetParent(transform, false);
        ladle.transform.localPosition = new Vector3(-1.18f, -1.05f, 0f);
        if (toolVisual != null && toolVisual.mixingToolScale != Vector3.zero)
            ladle.transform.localScale = toolVisual.mixingToolScale;
        else if (toolVisual == null || toolVisual.mixingToolPrefab == null)
            ladle.transform.localScale = new Vector3(0.72f, 0.72f, 1f);

        SpriteRenderer renderer = ladle.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = ladle.AddComponent<SpriteRenderer>();

        if (toolVisual != null && toolVisual.mixingLadleSprite != null)
            renderer.sprite = toolVisual.mixingLadleSprite;
        else if (renderer.sprite == null)
            renderer.sprite = CookingRuntimeVisuals2D.GetLadleSprite();

        renderer.sortingOrder = 45;

        BoxCollider2D collider = ladle.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = ladle.AddComponent<BoxCollider2D>();

        collider.isTrigger = true;
        if (renderer.sprite != null)
        {
            collider.size = renderer.sprite.bounds.size;
            collider.offset = renderer.sprite.bounds.center;
        }

        ladleTool = ladle.GetComponent<PreparationPointerTool2D>();
        if (ladleTool == null)
            ladleTool = ladle.AddComponent<PreparationPointerTool2D>();

        ladleTool.Configure(targetCamera != null ? targetCamera : Camera.main);
    }

    private void OnValidate()
    {
        requiredAlternatingStrokes = Mathf.Max(1, requiredAlternatingStrokes);
        minHorizontalStrokeDistance = Mathf.Max(0.01f, minHorizontalStrokeDistance);
        verticalNoiseRatio = Mathf.Max(0.1f, verticalNoiseRatio);
        finishTravelDuration = Mathf.Max(0.01f, finishTravelDuration);
        returnTravelDuration = Mathf.Max(0.01f, returnTravelDuration);
    }
}
