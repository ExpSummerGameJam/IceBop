using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GuidedPlacementFinishingStage2D : MonoBehaviour, IDishFinishingStage
{
    [Serializable]
    private sealed class PlacementItem
    {
        public CookingIngredientEntry ingredient;
        public Transform transform;
        public SpriteRenderer renderer;
        public BoxCollider2D collider;

        public Transform guide;
        public SpriteRenderer guideRenderer;

        public Vector3 targetLocalPosition;

        public bool placed;
    }

    [SerializeField] private float snapDistance = 0.35f;
    [SerializeField] private float ingredientScaleMultiplier = 0.55f;
    [SerializeField] private float guideAlpha = 0.3f;

    [SerializeField] private float targetHorizontalSpacing = 0.78f;
    [SerializeField] private float targetVerticalSpacing = 0.62f;

    [SerializeField] private float startHorizontalRange = 1.35f;
    [SerializeField] private float startY = -1.25f;

    private readonly List<PlacementItem> placementItems = new();

    private Camera targetCamera;
    private IReadOnlyList<CookingIngredientEntry> ingredients;
    private DishDefinition dish;

    private Transform interactionRoot;
    private PlacementItem draggingItem;

    private DishResultPresenter2D presenter;

    private int placedCount;
    private bool completed;
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

        EnsureVisuals();
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

            if (item == null ||
                item.placed ||
                item.collider == null)
            {
                continue;
            }

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
        if (draggingItem == null ||
            draggingItem.transform == null ||
            interactionRoot == null)
        {
            draggingItem = null;
            return;
        }

        Vector3 localPosition =
            interactionRoot.InverseTransformPoint(
                new Vector3(
                    pointerWorldPosition.x,
                    pointerWorldPosition.y,
                    interactionRoot.position.z
                )
            );

        localPosition.z = 0f;

        draggingItem.transform.localPosition = localPosition;

        float distance = Vector2.Distance(
            new Vector2(localPosition.x, localPosition.y),
            new Vector2(
                draggingItem.targetLocalPosition.x,
                draggingItem.targetLocalPosition.y
            )
        );

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

        if (placedCount >= placementItems.Count)
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

        presenter.Play(
            targetCamera,
            dish,
            ingredients,
            HideInteractiveVisuals
        );
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
            return;

        GameObject root = new("GuidedPlacementRoot");
        root.transform.SetParent(transform, false);

        interactionRoot = root.transform;

        CreateBowl();

        if (ingredients == null)
            return;

        int count = ingredients.Count;

        for (int i = 0; i < count; i++)
        {
            CookingIngredientEntry entry = ingredients[i];

            if (entry == null)
                continue;

            Vector3 targetPosition =
                CalculateTargetPosition(
                    placementItems.Count,
                    count
                );

            Vector3 startPosition =
                CalculateStartPosition(
                    placementItems.Count,
                    count
                );

            CreatePlacementItem(
                entry,
                startPosition,
                targetPosition,
                placementItems.Count
            );
        }
    }

    private void CreateBowl()
    {
        CookingRuntimeVisuals2D.CreateBlock(
            "BingsuBowlBack",
            interactionRoot,
            new Vector3(0f, -0.15f, 0f),
            new Vector3(2.25f, 1.05f, 1f),
            new Color(0.78f, 0.88f, 0.96f, 1f),
            30
        );

        CookingRuntimeVisuals2D.CreateBlock(
            "BingsuBowlFront",
            interactionRoot,
            new Vector3(0f, -0.48f, 0f),
            new Vector3(2.05f, 0.52f, 1f),
            new Color(0.62f, 0.76f, 0.88f, 1f),
            58
        );
    }

    private void CreatePlacementItem(
        CookingIngredientEntry entry,
        Vector3 startPosition,
        Vector3 targetPosition,
        int index)
    {
        Sprite sprite =
            CookingRuntimeVisuals2D.GetIngredientSprite(entry);

        if (sprite == null)
            sprite = InventoryVisualUtility.WhiteSprite;

        Color ingredientColor =
            CookingRuntimeVisuals2D.GetIngredientColor(entry);

        Vector3 sourceScale =
            entry.ingredientScale != Vector3.zero
                ? entry.ingredientScale
                : Vector3.one;

        Vector3 finalScale =
            sourceScale *
            Mathf.Max(0.01f, ingredientScaleMultiplier);

        GameObject guideObject =
            new($"PlacementGuide_{index + 1}_{entry.ingredientId}");

        guideObject.transform.SetParent(
            interactionRoot,
            false
        );

        guideObject.transform.localPosition =
            targetPosition;

        guideObject.transform.localScale =
            finalScale;

        SpriteRenderer guideRenderer =
            guideObject.AddComponent<SpriteRenderer>();

        guideRenderer.sprite = sprite;

        guideRenderer.color = new Color(
            ingredientColor.r,
            ingredientColor.g,
            ingredientColor.b,
            guideAlpha
        );

        guideRenderer.sortingOrder = 42;

        GameObject ingredientObject =
            new($"PlacementIngredient_{index + 1}_{entry.ingredientId}");

        ingredientObject.transform.SetParent(
            interactionRoot,
            false
        );

        ingredientObject.transform.localPosition =
            startPosition;

        ingredientObject.transform.localScale =
            finalScale;

        SpriteRenderer renderer =
            ingredientObject.AddComponent<SpriteRenderer>();

        renderer.sprite = sprite;
        renderer.color = ingredientColor;
        renderer.sortingOrder = 50 + index;

        BoxCollider2D collider =
            ingredientObject.AddComponent<BoxCollider2D>();

        collider.isTrigger = true;

        if (sprite != null)
        {
            collider.size = sprite.bounds.size;
            collider.offset = sprite.bounds.center;
        }

        PlacementItem item =
            new PlacementItem
            {
                ingredient = entry,
                transform = ingredientObject.transform,
                renderer = renderer,
                collider = collider,

                guide = guideObject.transform,
                guideRenderer = guideRenderer,

                targetLocalPosition = targetPosition,
                placed = false
            };

        placementItems.Add(item);
    }

    private Vector3 CalculateTargetPosition(
        int index,
        int count)
    {
        int columnCount =
            Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Sqrt(count)),
                1,
                4
            );

        int rowCount =
            Mathf.CeilToInt(
                count / (float)columnCount
            );

        int column =
            index % columnCount;

        int row =
            index / columnCount;

        float totalWidth =
            (columnCount - 1) *
            targetHorizontalSpacing;

        float x =
            column *
            targetHorizontalSpacing -
            totalWidth *
            0.5f;

        float totalHeight =
            (rowCount - 1) *
            targetVerticalSpacing;

        float y =
            0.28f +
            totalHeight *
            0.5f -
            row *
            targetVerticalSpacing;

        return new Vector3(
            x,
            y,
            0f
        );
    }

    private Vector3 CalculateStartPosition(
        int index,
        int count)
    {
        if (count <= 1)
        {
            return new Vector3(
                0f,
                startY,
                0f
            );
        }

        float normalized =
            index /
            (float)(count - 1);

        float x =
            Mathf.Lerp(
                -startHorizontalRange,
                startHorizontalRange,
                normalized
            );

        float y =
            startY -
            (index % 2) * 0.18f;

        return new Vector3(
            x,
            y,
            0f
        );
    }

    private bool TryGetPointerWorldPosition(
        out Vector2 worldPosition)
    {
        worldPosition = default;

        Camera camera =
            targetCamera != null
                ? targetCamera
                : Camera.main;

        if (camera == null)
            return false;

        Vector2 screenPosition =
            GatheringInput.PointerScreenPosition();

        float zDistance =
            Mathf.Abs(
                camera.transform.position.z -
                transform.position.z
            );

        Vector3 result =
            camera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    zDistance
                )
            );

        worldPosition =
            new Vector2(
                result.x,
                result.y
            );

        return true;
    }

    private void OnValidate()
    {
        snapDistance =
            Mathf.Max(
                0.05f,
                snapDistance
            );

        ingredientScaleMultiplier =
            Mathf.Max(
                0.01f,
                ingredientScaleMultiplier
            );

        guideAlpha =
            Mathf.Clamp01(
                guideAlpha
            );

        targetHorizontalSpacing =
            Mathf.Max(
                0.1f,
                targetHorizontalSpacing
            );

        targetVerticalSpacing =
            Mathf.Max(
                0.1f,
                targetVerticalSpacing
            );

        startHorizontalRange =
            Mathf.Max(
                0.1f,
                startHorizontalRange
            );
    }
}