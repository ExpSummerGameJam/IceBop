using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SkewerAssemblyFinishingStage2D : MonoBehaviour, IDishFinishingStage
{
    [Serializable]
    private sealed class SkewerPiece
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public PolygonCollider2D collider;
        public bool placed;
    }

    [Serializable]
    private sealed class SkewerSlot
    {
        public Transform transform;
        public SpriteRenderer guideRenderer;
        public bool occupied;
    }

    [SerializeField] private int requiredPieceCount = 4;

    [SerializeField] private float snapDistance = 0.4f;
    [SerializeField] private float pieceScaleMultiplier = 0.5f;

    [SerializeField] private float hornLength = 2.5f;
    [SerializeField] private float hornWidth = 0.16f;
    [SerializeField] private float hornRotation = -8f;

    [SerializeField] private float slotStartY = -0.68f;
    [SerializeField] private float slotEndY = 0.72f;

    [SerializeField] private float startHorizontalDistance = 1.25f;
    [SerializeField] private float startY = -1.15f;

    [SerializeField] private float guideAlpha = 0.28f;

    private readonly List<SkewerPiece> pieces = new();
    private readonly List<SkewerSlot> slots = new();

    private Camera targetCamera;
    private InventoryView2D inventoryView;
    private IReadOnlyList<CookingIngredientEntry> ingredients;
    private DishDefinition dish;

    private Transform interactionRoot;
    private Transform hornRoot;
    private SpriteRenderer hornRenderer;

    private SkewerPiece draggingPiece;

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
        draggingPiece = null;

        EnsureVisuals();
        RearmPieces();
    }

    private void Update()
    {
        if (completed || presentingResult)
            return;

        if (!TryGetPointerWorldPosition(out Vector2 pointerWorldPosition))
            return;

        if (draggingPiece == null)
        {
            if (GatheringInput.WasPrimaryPressed())
                TryBeginDrag(pointerWorldPosition);

            return;
        }

        UpdateDragging(pointerWorldPosition);
    }

    // Uses a broad-phase overlap query (like every other draggable tool's
    // PreparationPointerTool2D.IsPointerOverTool) instead of asking each piece's own collider
    // directly - a collider that only just became enabled/reactivated this pooled-rig cycle
    // isn't guaranteed to answer OverlapPoint correctly until physics has synced its transform.
    private void TryBeginDrag(Vector2 pointerWorldPosition)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(pointerWorldPosition);

        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            SkewerPiece piece = pieces[i];

            if (piece == null || piece.placed || piece.collider == null)
                continue;

            if (!ContainsCollider(hits, piece.collider))
                continue;

            draggingPiece = piece;

            if (piece.renderer != null)
                piece.renderer.sortingOrder = 70;

            return;
        }
    }

    private static bool ContainsCollider(Collider2D[] hits, Collider2D target)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == target)
                return true;
        }

        return false;
    }

    private void UpdateDragging(Vector2 pointerWorldPosition)
    {
        if (draggingPiece == null || draggingPiece.transform == null || interactionRoot == null)
        {
            draggingPiece = null;
            return;
        }

        Vector3 localPosition = interactionRoot.InverseTransformPoint(
            new Vector3(pointerWorldPosition.x, pointerWorldPosition.y, interactionRoot.position.z));

        localPosition.z = 0f;
        draggingPiece.transform.localPosition = localPosition;

        SkewerSlot closestSlot = FindClosestAvailableSlot(pointerWorldPosition);
        if (closestSlot == null)
            return;

        float distance = Vector2.Distance(pointerWorldPosition, closestSlot.transform.position);
        if (distance <= snapDistance)
            PlacePiece(draggingPiece, closestSlot);
    }

    private SkewerSlot FindClosestAvailableSlot(Vector2 pointerWorldPosition)
    {
        SkewerSlot closest = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < slots.Count; i++)
        {
            SkewerSlot slot = slots[i];

            if (slot == null || slot.occupied || slot.transform == null)
                continue;

            float distance = Vector2.Distance(pointerWorldPosition, slot.transform.position);
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closest = slot;
        }

        return closest;
    }

    private void PlacePiece(SkewerPiece piece, SkewerSlot slot)
    {
        if (piece == null || slot == null || piece.placed || slot.occupied)
            return;

        piece.placed = true;
        slot.occupied = true;
        GameSfxPlayer.Play(GameSfx.ActionSkewer);

        piece.transform.SetParent(slot.transform, true);
        piece.transform.position = slot.transform.position;
        piece.transform.rotation = slot.transform.rotation;
        piece.transform.localPosition = Vector3.zero;
        piece.transform.localRotation = Quaternion.identity;

        if (piece.collider != null)
            piece.collider.enabled = false;

        if (piece.renderer != null)
            piece.renderer.sortingOrder = 55 + placedCount;

        if (slot.guideRenderer != null)
            slot.guideRenderer.gameObject.SetActive(false);

        draggingPiece = null;
        placedCount++;

        if (placedCount >= requiredPieceCount)
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

        GameObject root = new("SkewerAssemblyRoot");
        root.transform.SetParent(transform, false);
        interactionRoot = root.transform;

        CreateHorn();

        int count = Mathf.Max(1, requiredPieceCount);
        CreateSlots(count);
        CreatePieces(count);
    }

    // Rebuilds this cook's piece/slot visuals - repositions the pool built once by
    // EnsureVisuals() and re-skins it for whichever ingredient this cook is using, instead of
    // rebuilding the count-fixed pool from scratch every cook.
    private void RearmPieces()
    {
        CookingIngredientEntry meatIngredient = FindMeatIngredient();
        Sprite sprite = GetPieceSprite(meatIngredient);
        Color color = GetPieceColor(meatIngredient);
        Vector3 scale = GetPieceScale(meatIngredient);

        ApplyHornVisual(FindStickIngredient());

        for (int i = 0; i < slots.Count; i++)
        {
            SkewerSlot slot = slots[i];
            if (slot == null)
                continue;

            slot.occupied = false;

            if (slot.guideRenderer != null)
            {
                slot.guideRenderer.sprite = sprite;
                slot.guideRenderer.color = new Color(color.r, color.g, color.b, guideAlpha);
                slot.guideRenderer.transform.localScale = scale;
                slot.guideRenderer.gameObject.SetActive(true);
            }
        }

        int count = pieces.Count;
        for (int i = 0; i < count; i++)
        {
            SkewerPiece piece = pieces[i];
            if (piece == null || piece.transform == null)
                continue;

            piece.placed = false;
            piece.transform.gameObject.SetActive(true);
            piece.transform.SetParent(interactionRoot, true);
            piece.transform.localPosition = CalculatePieceStartPosition(i, count);
            piece.transform.localRotation = Quaternion.identity;
            piece.transform.localScale = scale;

            if (piece.renderer != null)
            {
                piece.renderer.enabled = true;
                piece.renderer.sprite = sprite;
                piece.renderer.color = color;
                piece.renderer.sortingOrder = 50 + i;
            }

            piece.collider = CookingRuntimeVisuals2D.ApplySpriteFittedCollider(piece.transform.gameObject, sprite);
            if (piece.collider != null)
            {
                piece.collider.enabled = true;
                piece.collider.isTrigger = true;
            }
        }
    }

    private void CreateHorn()
    {
        GameObject hornObject = new("UnicornHorn");
        hornObject.transform.SetParent(interactionRoot, false);
        hornObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        hornObject.transform.localRotation = Quaternion.Euler(0f, 0f, hornRotation);
        hornRoot = hornObject.transform;

        GameObject bodyObject = new("UnicornHornBody");
        bodyObject.transform.SetParent(hornRoot, false);
        hornRenderer = bodyObject.AddComponent<SpriteRenderer>();
        hornRenderer.sortingOrder = 40;
    }

    // The skewer "stick" is a real ingredient (the unicorn horn icicle the pieces get
    // threaded onto) - re-skin it every cook instead of assuming a flat placeholder color.
    private void ApplyHornVisual(CookingIngredientEntry stickIngredient)
    {
        if (hornRenderer == null)
            return;

        Sprite sprite = GetPieceSprite(stickIngredient);
        hornRenderer.sprite = sprite;
        hornRenderer.color = GetPieceColor(stickIngredient);

        float verticalExtent = sprite != null ? Mathf.Max(0.01f, sprite.bounds.size.y) : hornLength;
        float scale = hornLength / verticalExtent;
        hornRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void CreateSlots(int count)
    {
        for (int i = 0; i < count; i++)
        {
            float t = count <= 1 ? 0.5f : i / (float)(count - 1);
            float y = Mathf.Lerp(slotStartY, slotEndY, t);

            GameObject slotObject = new($"SkewerSlot_{i + 1}");
            slotObject.transform.SetParent(hornRoot, false);
            slotObject.transform.localPosition = new Vector3(0f, y, 0f);

            GameObject guideObject = new($"SkewerGuide_{i + 1}");
            guideObject.transform.SetParent(slotObject.transform, false);
            guideObject.transform.localPosition = Vector3.zero;

            SpriteRenderer guideRenderer = guideObject.AddComponent<SpriteRenderer>();
            guideRenderer.sortingOrder = 44;

            slots.Add(new SkewerSlot
            {
                transform = slotObject.transform,
                guideRenderer = guideRenderer,
                occupied = false
            });
        }
    }

    private void CreatePieces(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject pieceObject = new($"DinosaurMeat_{i + 1}");
            pieceObject.transform.SetParent(interactionRoot, false);
            pieceObject.transform.localPosition = CalculatePieceStartPosition(i, count);

            SpriteRenderer renderer = pieceObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 50 + i;

            PolygonCollider2D collider = pieceObject.AddComponent<PolygonCollider2D>();
            collider.isTrigger = true;

            pieces.Add(new SkewerPiece
            {
                transform = pieceObject.transform,
                renderer = renderer,
                collider = collider,
                placed = false
            });
        }
    }

    // The ingredient list order isn't guaranteed to put the piece-ingredient first, so pick
    // the dinosaur-tail entry by its data key rather than assuming index 0. If the key match
    // fails, fall back to any entry that isn't the horn/stick ingredient - never re-use the
    // same entry for both the horn and the meat pieces.
    private CookingIngredientEntry FindMeatIngredient()
    {
        CookingIngredientEntry meat = FindIngredientByKeyContains("dinosaur");
        if (meat != null)
            return meat;

        Debug.LogWarning("SkewerAssemblyFinishingStage2D: no ingredient with a \"dinosaur\" key found - falling back to the first non-horn ingredient.", this);

        CookingIngredientEntry stick = FindIngredientByKeyContains("unicorn");
        return FindIngredientOtherThan(stick) ?? FirstIngredientOrNull();
    }

    private CookingIngredientEntry FindStickIngredient()
    {
        return FindIngredientByKeyContains("unicorn") ?? FirstIngredientOrNull();
    }

    private CookingIngredientEntry FindIngredientOtherThan(CookingIngredientEntry excluded)
    {
        if (ingredients == null)
            return null;

        for (int i = 0; i < ingredients.Count; i++)
        {
            CookingIngredientEntry entry = ingredients[i];
            if (entry != null && entry != excluded)
                return entry;
        }

        return null;
    }

    private CookingIngredientEntry FindIngredientByKeyContains(string keyPart)
    {
        if (ingredients == null)
            return null;

        for (int i = 0; i < ingredients.Count; i++)
        {
            CookingIngredientEntry entry = ingredients[i];
            if (entry?.ingredient != null &&
                !string.IsNullOrEmpty(entry.ingredient.ingredientKey) &&
                entry.ingredient.ingredientKey.IndexOf(keyPart, StringComparison.OrdinalIgnoreCase) >= 0)
                return entry;
        }

        return null;
    }

    private CookingIngredientEntry FirstIngredientOrNull()
    {
        if (ingredients == null)
            return null;

        for (int i = 0; i < ingredients.Count; i++)
        {
            if (ingredients[i] != null)
                return ingredients[i];
        }

        return null;
    }

    private Sprite GetPieceSprite(CookingIngredientEntry entry)
    {
        if (entry == null)
            return InventoryVisualUtility.WhiteSprite;

        Sprite sprite = CookingRuntimeVisuals2D.GetIngredientSprite(entry);
        return sprite != null ? sprite : InventoryVisualUtility.WhiteSprite;
    }

    private Color GetPieceColor(CookingIngredientEntry entry)
    {
        if (entry == null)
            return new Color(0.72f, 0.22f, 0.18f, 1f);

        return CookingRuntimeVisuals2D.GetIngredientColor(entry);
    }

    private Vector3 GetPieceScale(CookingIngredientEntry entry)
    {
        Vector3 sourceScale = entry != null && entry.ingredientScale != Vector3.zero
            ? entry.ingredientScale
            : Vector3.one;

        return sourceScale * Mathf.Max(0.01f, pieceScaleMultiplier);
    }

    private Vector3 CalculatePieceStartPosition(int index, int count)
    {
        if (count <= 1)
            return new Vector3(0f, startY, 0f);

        float t = index / (float)(count - 1);
        float x = Mathf.Lerp(-startHorizontalDistance, startHorizontalDistance, t);
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
        requiredPieceCount = Mathf.Max(1, requiredPieceCount);
        snapDistance = Mathf.Max(0.05f, snapDistance);
        pieceScaleMultiplier = Mathf.Max(0.01f, pieceScaleMultiplier);
        hornLength = Mathf.Max(0.2f, hornLength);
        hornWidth = Mathf.Max(0.02f, hornWidth);
        startHorizontalDistance = Mathf.Max(0.1f, startHorizontalDistance);
        guideAlpha = Mathf.Clamp01(guideAlpha);
    }
}
