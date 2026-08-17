using System.Collections.Generic;
using System;
using UnityEngine;

public sealed class InventoryCookingStation2D : MonoBehaviour
{
    [SerializeField] private PhysicalInventory2D inventory;
    [SerializeField] private InventoryView2D inventoryView;
    [SerializeField] private Collider2D cookingAreaCollider;
    [SerializeField] private bool requirePlayerInCookingArea = true;
    [SerializeField] private int maxIngredients = 4;
    [SerializeField] private Vector2 buttonSize = new(0.86f, 0.28f);

    private readonly HashSet<InventoryItem2D> itemsOnPot = new();
    private readonly HashSet<WorldCookingArea2D> activeCookingAreas = new();

    private Transform visualRoot;
    private Transform potRoot;
    private Transform buttonRoot;
    private BoxCollider2D potCollider;
    private BoxCollider2D cookButtonCollider;
    private SpriteRenderer cookButtonRenderer;
    private bool isCooking;
    private bool loggedMissingPlacedVisuals;

    public event Action<CookingCraftResult> CookingCompleted;
    public bool IsCooking => isCooking;
    public IReadOnlyCollection<InventoryItem2D> ItemsOnPot => itemsOnPot;
    public bool HasPrepPot => potRoot != null;
    public Vector3 PrepPotCenter => potCollider != null
        ? (Vector3)potCollider.bounds.center
        : (potRoot != null ? potRoot.position : Vector3.zero);
    public bool IsPlayerInCookingArea
    {
        get
        {
            PruneActiveCookingAreas();
            return activeCookingAreas.Count > 0;
        }
    }

    public bool CanUseCookingStation => !requirePlayerInCookingArea || IsPlayerInCookingArea;

    public void Bind(PhysicalInventory2D newInventory, InventoryView2D newInventoryView)
    {
        inventory = newInventory;
        inventoryView = newInventoryView;
        if (EnsureVisuals())
            UpdateVisibility();
    }

    public void BindCookingArea(Collider2D newCookingAreaCollider)
    {
        cookingAreaCollider = newCookingAreaCollider;

        if (cookingAreaCollider != null)
            cookingAreaCollider.isTrigger = true;

        EnsureVisuals();
    }

    public void SetCookingAreaPlayerPresence(WorldCookingArea2D cookingArea, bool hasPlayerInside)
    {
        if (cookingArea == null)
            return;

        int beforeCount = activeCookingAreas.Count;
        PruneActiveCookingAreas();
        bool changed = hasPlayerInside
            ? activeCookingAreas.Add(cookingArea)
            : activeCookingAreas.Remove(cookingArea);

        if (!changed && activeCookingAreas.Count == beforeCount)
            return;

        UpdateCookButtonVisual();
        UpdateVisibility();
    }

    public void SetItemOnPot(InventoryItem2D item, bool isOnPot)
    {
        if (item == null || isCooking)
            return;

        bool changed = isOnPot
            ? itemsOnPot.Count < Mathf.Max(1, maxIngredients) && itemsOnPot.Add(item)
            : itemsOnPot.Remove(item);

        if (!changed)
            return;

        UpdateCookButtonVisual();
    }

    public void ClearItemsFromArea(InventoryCookingArea2D area)
    {
        if (isCooking || itemsOnPot.Count == 0)
            return;

        itemsOnPot.Clear();
        UpdateCookButtonVisual();
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureVisuals();
    }

    private void Update()
    {
        ResolveReferences();
        ResolveCookingArea();
        if (!EnsureVisuals())
            return;

        UpdateVisibility();
        PruneMissingItemsOnPot();

        if (visualRoot == null || !visualRoot.gameObject.activeSelf)
            return;

        TryStartCookingFromClick();
    }

    private void TryStartCookingFromClick()
    {
        if (itemsOnPot.Count == 0 || isCooking || !CanUseCookingStation || !GatheringInput.WasPrimaryPressed())
            return;

        if (!TryGetPointerWorldPosition(out Vector2 worldPosition))
            return;

        Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition);
        foreach (Collider2D hit in hits)
        {
            if (hit == cookButtonCollider)
            {
                StartCooking();
                return;
            }
        }
    }

    private void StartCooking()
    {
        if (itemsOnPot.Count == 0 || isCooking || !CanUseCookingStation)
            return;

        isCooking = true;

        List<CookingIngredientEntry> cookingQueue = new();
        List<InventoryItem2D> consumedItems = new(itemsOnPot);
        itemsOnPot.Clear();

        foreach (InventoryItem2D item in consumedItems)
        {
            if (item == null)
                continue;

            IngredientDefinition definition = item.Definition;
            if (definition == null && !GatheringDataLookup.TryGetIngredient(item.IngredientId, out definition))
                continue;

            cookingQueue.Add(CookingIngredientEntry.FromInventoryItem(item, definition));
            inventory.RemoveItem(item);
        }

        UpdateCookButtonVisual();
        UpdateVisibility();
        CookingFlowController2D.StartCookingWithResult(cookingQueue, OnCookingCompleted);
    }

    private void OnCookingCompleted(CookingCraftResult result)
    {
        isCooking = false;
        UpdateCookButtonVisual();
        UpdateVisibility();
        CookingCompleted?.Invoke(result);
    }

    private bool EnsureVisuals()
    {
        ResolveCookingArea();

        if (visualRoot == null)
            visualRoot = transform.Find("InventoryCookingStation");

        if (visualRoot == null)
            return WarnMissingPlacedVisuals("InventoryCookingStation");

        if (potRoot == null)
            potRoot = visualRoot.Find("PrepPot");

        if (potCollider == null)
            potCollider = potRoot != null ? potRoot.GetComponent<BoxCollider2D>() : null;

        if (potCollider != null)
            potCollider.isTrigger = true;

        if (buttonRoot == null)
            buttonRoot = visualRoot.Find("CookButton");

        if (cookButtonRenderer == null)
            cookButtonRenderer = buttonRoot != null ? buttonRoot.Find("ButtonBody")?.GetComponent<SpriteRenderer>() : null;

        if (cookButtonCollider == null)
            cookButtonCollider = buttonRoot != null ? buttonRoot.GetComponent<BoxCollider2D>() : null;

        if (cookButtonCollider != null)
        {
            cookButtonCollider.isTrigger = true;
            cookButtonCollider.size = buttonSize;
        }

        EnsurePlacedSpriteRenderers();

        if (potRoot == null)
            return WarnMissingPlacedVisuals("PrepPot");

        if (buttonRoot == null)
            return WarnMissingPlacedVisuals("CookButton");

        if (cookButtonCollider == null)
            return WarnMissingPlacedVisuals("CookButton BoxCollider2D");

        UpdateCookButtonVisual();
        return true;
    }

    private bool WarnMissingPlacedVisuals(string missingName)
    {
        if (!loggedMissingPlacedVisuals)
        {
            loggedMissingPlacedVisuals = true;
            Debug.LogError(
                $"InventoryCookingStation2D needs a placed {missingName} object. " +
                "Place the cooking station visuals under the inventory prefab instead of creating them at runtime.",
                this);
        }

        return false;
    }

    private void EnsurePlacedSpriteRenderers()
    {
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer != null && renderer.sprite == null)
                renderer.sprite = InventoryVisualUtility.WhiteSprite;
        }
    }

    private void PruneMissingItemsOnPot()
    {
        if (itemsOnPot.Count == 0)
            return;

        if (itemsOnPot.RemoveWhere(item => item == null) > 0)
            UpdateCookButtonVisual();
    }

    private void UpdateVisibility()
    {
        if (visualRoot == null)
            return;

        // isCooking stays true until the player clicks the finished dish away, so this also
        // covers "hide the pot while a completed dish is still being shown".
        bool visible = inventoryView != null && inventoryView.IsLargeViewOpen && !isCooking && CanUseCookingStation;
        if (visualRoot.gameObject.activeSelf != visible)
            visualRoot.gameObject.SetActive(visible);
    }

    private void UpdateCookButtonVisual()
    {
        if (cookButtonRenderer == null)
            return;

        bool canCook = itemsOnPot.Count > 0 && !isCooking && CanUseCookingStation;
        cookButtonRenderer.gameObject.SetActive(canCook);
    }

    private bool TryGetPointerWorldPosition(out Vector2 worldPosition)
    {
        if (inventoryView == null)
        {
            worldPosition = default;
            return false;
        }

        return inventoryView.TryGetLargeViewPointerWorldPosition(out worldPosition);
    }

    private void ResolveReferences()
    {
        if (inventory == null)
            inventory = GetComponent<PhysicalInventory2D>();

        if (inventoryView == null)
            inventoryView = GetComponent<InventoryView2D>();
    }

    private void ResolveCookingArea()
    {
        if (cookingAreaCollider != null)
            return;

        InventoryCookingArea2D area = GetComponentInChildren<InventoryCookingArea2D>(true);
        if (area != null)
            cookingAreaCollider = area.AreaCollider;
    }

    private void PruneActiveCookingAreas()
    {
        if (activeCookingAreas.Count == 0)
            return;

        activeCookingAreas.RemoveWhere(area => area == null || !area.isActiveAndEnabled);
    }

    private void OnValidate()
    {
        maxIngredients = Mathf.Max(1, maxIngredients);
        buttonSize.x = Mathf.Max(0.1f, buttonSize.x);
        buttonSize.y = Mathf.Max(0.1f, buttonSize.y);
    }
}
