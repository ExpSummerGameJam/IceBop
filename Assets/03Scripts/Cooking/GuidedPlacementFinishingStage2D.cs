using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GuidedPlacementFinishingStage2D : MonoBehaviour, IDishFinishingStage
{
    [Serializable]
    private sealed class PlacementItem
    {
        public CookingIngredientEntry ingredient;
        public GameObject gameObject;
        public Transform transform;
        public SpriteRenderer renderer;
        public PolygonCollider2D collider;

        public GameObject guideObject;
        public Transform guide;
        public SpriteRenderer guideRenderer;

        public Vector3 targetLocalPosition;

        public bool placed;
        public bool active;
    }

    [SerializeField] private int maxPoolSize = 4;
    [SerializeField] private float snapDistance = 0.35f;
    [SerializeField] private float ingredientScaleMultiplier = 0.55f;
    [SerializeField] private float guideAlpha = 0.3f;

    [SerializeField] private float targetHorizontalSpacing = 0.78f;
    [SerializeField] private float targetVerticalSpacing = 0.62f;

    [SerializeField] private float startHorizontalRange = 1.35f;
    [SerializeField] private float startY = -1.25f;

    private readonly List<PlacementItem> placementItems = new();

    private Camera targetCamera;
    private InventoryView2D inventoryView;
    private IReadOnlyList<CookingIngredientEntry> ingredients;
    private DishDefinition dish;

    private Transform interactionRoot;
    private PlacementItem draggingItem;

    private DishResultPresenter2D presenter;

    private int placedCount;
    private int activeItemCount;
    private bool completed;
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
        placedCount = 0;
        completed = false;
        presentingResult = false;
        draggingItem = null;

        EnsureVisuals();
        RearmItems();
    }

    private void Update()
    {
        if (completed || presentingResult)
            return;

        if (!TryGetPointerWorldPosition(out Vector2 pointerWorldPosition))
            return;

        if (draggingItem == null)
        {
            if (GatheringInput.WasPrimaryPressed())
                TryBeginDrag(pointerWorldPosition);

            return;
        }

        UpdateDragging(pointerWorldPosition);
    }

    private void TryBeginDrag(Vector2 pointerWorldPosition)
    {
        for (int i = placementItems.Count - 1; i >= 0; i--)
        {
            PlacementItem item = placementItems[i];

            if (item == null || !item.active || item.placed || item.collider == null)
                continue;

            if (!item.collider.OverlapPoint(pointerWorldPosition))
                continue;

            draggingItem = item;

            if (item.renderer != null)
                item.renderer.sortingOrder = 70;

            return;
        }
    }

    private void UpdateDragging(Vector2 pointerWorldPosition)
    {
        if (draggingItem == null || draggingItem.transform == null || interactionRoot == null)
        {
            draggingItem = null;
            return;
        }

        Vector3 localPosition = interactionRoot.InverseTransformPoint(
            new Vector3(pointerWorldPosition.x, pointerWorldPosition.y, interactionRoot.position.z));

        localPosition.z = 0f;
        draggingItem.transform.localPosition = localPosition;

        float distance = Vector2.Distance(
            new Vector2(localPosition.x, localPosition.y),
            new Vector2(draggingItem.targetLocalPosition.x, draggingItem.targetLocalPosition.y));

        if (distance <= snapDistance)
            PlaceDraggingItem();
    }

    private void PlaceDraggingItem()
    {
        if (draggingItem == null)
            return;

        PlacementItem item = draggingItem;

        item.transform.localPosition = item.targetLocalPosition;
        item.placed = true;

        if (item.collider != null)
            item.collider.enabled = false;

        if (item.renderer != null)
            item.renderer.sortingOrder = 55;

        if (item.guideRenderer != null)
            item.guideRenderer.gameObject.SetActive(false);

        draggingItem = null;
        placedCount++;

        if (placedCount >= activeItemCount)
            CompleteInteraction();
    }

    private void CompleteInteraction()
    {
        if (completed)
            return;

        completed = true;
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
            presenter.Presented += OnResultPresented;
        }

        presenter.Play(targetCamera, dish, ingredients, HideInteractiveVisuals);
    }

    private void OnResultPresented()
    {
        ResultPresented?.Invoke();
    }

    private void HideInteractiveVisuals()
    {
        if (interactionRoot != null)
            interactionRoot.gameObject.SetActive(false);
    }

    private void EnsureVisuals()
    {
        if (interactionRoot != null)
        {
            interactionRoot.gameObject.SetActive(true);
            return;
        }

        GameObject root = new("GuidedPlacementRoot");
        root.transform.SetParent(transform, false);
        interactionRoot = root.transform;

        CreateBowl();

        int poolSize = Mathf.Max(1, maxPoolSize);
        for (int i = 0; i < poolSize; i++)
            CreatePlacementItem(i);
    }

    // Reskins/repositions/activates as many pooled items as this cook's ingredient list needs
    // (built once, to poolSize, by EnsureVisuals) and deactivates the rest, instead of
    // rebuilding a variable-count hierarchy from scratch every cook.
    private void RearmItems()
    {
        int count = ingredients != null ? Mathf.Min(ingredients.Count, placementItems.Count) : 0;
        activeItemCount = count;

        for (int i = 0; i < placementItems.Count; i++)
        {
            PlacementItem item = placementItems[i];
            if (item == null)
                continue;

            if (i >= count)
            {
                item.active = false;
                item.placed = false;
                if (item.gameObject != null)
                    item.gameObject.SetActive(false);
                if (item.guideObject != null)
                    item.guideObject.SetActive(false);
                continue;
            }

            CookingIngredientEntry entry = ingredients[i];
            item.ingredient = entry;
            item.active = true;
            item.placed = false;

            Vector3 targetPosition = CalculateTargetPosition(i, count);
            Vector3 startPosition = CalculateStartPosition(i, count);
            item.targetLocalPosition = targetPosition;

            Sprite sprite = CookingRuntimeVisuals2D.GetIngredientSprite(entry);
            if (sprite == null)
                sprite = InventoryVisualUtility.WhiteSprite;

            Color ingredientColor = CookingRuntimeVisuals2D.GetIngredientColor(entry);
            Vector3 sourceScale = entry != null && entry.ingredientScale != Vector3.zero ? entry.ingredientScale : Vector3.one;
            Vector3 finalScale = sourceScale * Mathf.Max(0.01f, ingredientScaleMultiplier);

            if (item.guideObject != null)
                item.guideObject.SetActive(true);
            if (item.guide != null)
            {
                item.guide.localPosition = targetPosition;
                item.guide.localScale = finalScale;
            }
            if (item.guideRenderer != null)
            {
                item.guideRenderer.sprite = sprite;
                item.guideRenderer.color = new Color(ingredientColor.r, ingredientColor.g, ingredientColor.b, guideAlpha);
            }

            if (item.gameObject != null)
                item.gameObject.SetActive(true);
            if (item.transform != null)
            {
                item.transform.localPosition = startPosition;
                item.transform.localScale = finalScale;
            }
            if (item.renderer != null)
            {
                item.renderer.sprite = sprite;
                item.renderer.color = ingredientColor;
                item.renderer.sortingOrder = 50 + i;
            }
            item.collider = CookingRuntimeVisuals2D.ApplySpriteFittedCollider(item.gameObject, sprite);
            if (item.collider != null)
            {
                item.collider.enabled = true;
                item.collider.isTrigger = true;
            }
        }
    }

    private void CreateBowl()
    {
        CookingRuntimeVisuals2D.CreateBlock(
            "BingsuBowlBack", interactionRoot, new Vector3(0f, -0.15f, 0f),
            new Vector3(2.25f, 1.05f, 1f), new Color(0.78f, 0.88f, 0.96f, 1f), 30);

        CookingRuntimeVisuals2D.CreateBlock(
            "BingsuBowlFront", interactionRoot, new Vector3(0f, -0.48f, 0f),
            new Vector3(2.05f, 0.52f, 1f), new Color(0.62f, 0.76f, 0.88f, 1f), 58);
    }

    private void CreatePlacementItem(int index)
    {
        GameObject guideObject = new($"PlacementGuide_{index + 1}");
        guideObject.transform.SetParent(interactionRoot, false);

        SpriteRenderer guideRenderer = guideObject.AddComponent<SpriteRenderer>();
        guideRenderer.sortingOrder = 42;
        guideObject.SetActive(false);

        GameObject ingredientObject = new($"PlacementIngredient_{index + 1}");
        ingredientObject.transform.SetParent(interactionRoot, false);

        SpriteRenderer renderer = ingredientObject.AddComponent<SpriteRenderer>();

        PolygonCollider2D collider = ingredientObject.AddComponent<PolygonCollider2D>();
        collider.isTrigger = true;

        ingredientObject.SetActive(false);

        placementItems.Add(new PlacementItem
        {
            ingredient = null,
            gameObject = ingredientObject,
            transform = ingredientObject.transform,
            renderer = renderer,
            collider = collider,

            guideObject = guideObject,
            guide = guideObject.transform,
            guideRenderer = guideRenderer,

            targetLocalPosition = Vector3.zero,
            placed = false,
            active = false
        });
    }

    private Vector3 CalculateTargetPosition(int index, int count)
    {
        int columnCount = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(count)), 1, 4);
        int rowCount = Mathf.CeilToInt(count / (float)columnCount);

        int column = index % columnCount;
        int row = index / columnCount;

        float totalWidth = (columnCount - 1) * targetHorizontalSpacing;
        float x = column * targetHorizontalSpacing - totalWidth * 0.5f;

        float totalHeight = (rowCount - 1) * targetVerticalSpacing;
        float y = 0.28f + totalHeight * 0.5f - row * targetVerticalSpacing;

        return new Vector3(x, y, 0f);
    }

    private Vector3 CalculateStartPosition(int index, int count)
    {
        if (count <= 1)
            return new Vector3(0f, startY, 0f);

        float normalized = index / (float)(count - 1);
        float x = Mathf.Lerp(-startHorizontalRange, startHorizontalRange, normalized);
        float y = startY - (index % 2) * 0.18f;

        return new Vector3(x, y, 0f);
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
        Vector3 result = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
        worldPosition = new Vector2(result.x, result.y);
        return true;
    }

    private void OnValidate()
    {
        maxPoolSize = Mathf.Max(1, maxPoolSize);
        snapDistance = Mathf.Max(0.05f, snapDistance);
        ingredientScaleMultiplier = Mathf.Max(0.01f, ingredientScaleMultiplier);
        guideAlpha = Mathf.Clamp01(guideAlpha);
        targetHorizontalSpacing = Mathf.Max(0.1f, targetHorizontalSpacing);
        targetVerticalSpacing = Mathf.Max(0.1f, targetVerticalSpacing);
        startHorizontalRange = Mathf.Max(0.1f, startHorizontalRange);
    }
}
