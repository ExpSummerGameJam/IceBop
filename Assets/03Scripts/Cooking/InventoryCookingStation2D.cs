using System.Collections.Generic;
using System;
using UnityEngine;

public sealed class InventoryCookingStation2D : MonoBehaviour, IInventoryItemDropReceiver2D
{
    [SerializeField] private PhysicalInventory2D inventory;
    [SerializeField] private InventoryView2D inventoryView;
    [SerializeField] private Collider2D cookingAreaCollider;
    [SerializeField] private bool requirePlayerInCookingArea = true;
    [SerializeField] private int maxIngredients = 4;
    [SerializeField] private Vector2 buttonSize = new(0.86f, 0.28f);

    private readonly List<CookingIngredientEntry> queuedIngredients = new();
    private readonly List<GameObject> queueVisuals = new();
    private readonly HashSet<WorldCookingArea2D> activeCookingAreas = new();

    private Transform visualRoot;
    private Transform potRoot;
    private Transform queueRoot;
    private Transform buttonRoot;
    private BoxCollider2D potCollider;
    private BoxCollider2D cookButtonCollider;
    private SpriteRenderer cookButtonRenderer;
    private TextMesh countLabel;
    private bool isCooking;
    private bool loggedMissingPlacedVisuals;

    public event Action<CookingCraftResult> CookingCompleted;
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
        {
            UpdateLayout();
            UpdateVisibility();
        }
    }

    public void BindCookingArea(Collider2D newCookingAreaCollider)
    {
        cookingAreaCollider = newCookingAreaCollider;

        if (cookingAreaCollider != null)
            cookingAreaCollider.isTrigger = true;

        if (EnsureVisuals())
            UpdateLayout();
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

        if (visualRoot == null || !visualRoot.gameObject.activeSelf)
            return;

        UpdateLayout();
        TryStartCookingFromClick();
    }

    public bool TryReceiveInventoryItem(InventoryItem2D item)
    {
        if (item == null || inventory == null || !CanReceiveDrops)
            return false;

        if (queuedIngredients.Count >= Mathf.Max(1, maxIngredients))
            return false;

        IngredientDefinition definition = item.Definition;
        if (definition == null && !GatheringDataLookup.TryGetIngredient(item.IngredientId, out definition))
            return false;

        CookingIngredientEntry entry = CookingIngredientEntry.FromInventoryItem(item, definition);
        queuedIngredients.Add(entry);

        item.SetDragging(false);
        inventory.RemoveItem(item);

        RebuildQueueVisuals();
        UpdateCookButtonVisual();
        return true;
    }

    private bool CanReceiveDrops =>
        !isCooking &&
        inventoryView != null &&
        inventoryView.IsLargeViewOpen &&
        CanUseCookingStation &&
        queuedIngredients.Count < Mathf.Max(1, maxIngredients);

    private void TryStartCookingFromClick()
    {
        if (queuedIngredients.Count == 0 || isCooking || !CanUseCookingStation || !GatheringInput.WasPrimaryPressed())
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
        if (queuedIngredients.Count == 0 || isCooking || !CanUseCookingStation)
            return;

        isCooking = true;
        List<CookingIngredientEntry> cookingQueue = new(queuedIngredients);
        queuedIngredients.Clear();
        RebuildQueueVisuals();
        UpdateCookButtonVisual();

        if (inventoryView != null)
            inventoryView.SetLargeViewOpen(false);

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

        if (queueRoot == null)
            queueRoot = visualRoot.Find("QueuedIngredients");

        if (countLabel == null)
            countLabel = visualRoot.Find("CountLabel")?.GetComponent<TextMesh>();

        EnsurePlacedSpriteRenderers();

        if (potRoot == null)
            return WarnMissingPlacedVisuals("PrepPot");

        if (buttonRoot == null)
            return WarnMissingPlacedVisuals("CookButton");

        if (queueRoot == null)
            return WarnMissingPlacedVisuals("QueuedIngredients");

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

    private void RebuildQueueVisuals()
    {
        foreach (GameObject visual in queueVisuals)
        {
            if (visual != null)
                Destroy(visual);
        }

        queueVisuals.Clear();

        if (queueRoot == null)
            return;

        for (int i = 0; i < queuedIngredients.Count; i++)
        {
            CookingIngredientEntry entry = queuedIngredients[i];
            GameObject token = new($"QueuedIngredient_{i + 1}_{entry.ingredientId}");
            token.transform.SetParent(queueRoot, false);
            token.transform.localScale = new Vector3(0.22f, 0.22f, 1f);

            SpriteRenderer renderer = token.AddComponent<SpriteRenderer>();
            renderer.sprite = CookingRuntimeVisuals2D.GetIngredientSprite(entry);
            renderer.color = CookingRuntimeVisuals2D.GetIngredientColor(entry);
            renderer.sortingOrder = 41 + i;
            queueVisuals.Add(token);
        }

        UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (inventory == null || visualRoot == null || potRoot == null || buttonRoot == null || queueRoot == null)
            return;

        Rect bagRect = inventory.BagWorldRect;
        Vector3 potPosition = GetCookingAreaCenter(bagRect);
        potRoot.position = potPosition;

        for (int i = 0; i < queueVisuals.Count; i++)
        {
            if (queueVisuals[i] != null)
                queueVisuals[i].transform.localPosition = new Vector3(i * 0.28f, 0f, 0f);
        }

        if (countLabel != null)
        {
            countLabel.text = $"{queuedIngredients.Count}/{Mathf.Max(1, maxIngredients)}";
        }
    }

    private Vector3 GetCookingAreaCenter(Rect bagRect)
    {
        ResolveCookingArea();

        if (cookingAreaCollider != null && cookingAreaCollider.enabled)
            return cookingAreaCollider.bounds.center;

        return new Vector3(bagRect.xMax - 0.62f, bagRect.yMin + 0.58f, transform.position.z);
    }

    private void UpdateVisibility()
    {
        if (visualRoot == null)
            return;

        bool visible = inventoryView != null && inventoryView.IsLargeViewOpen && !isCooking && CanUseCookingStation;
        if (visualRoot.gameObject.activeSelf != visible)
            visualRoot.gameObject.SetActive(visible);
    }

    private void UpdateCookButtonVisual()
    {
        if (cookButtonRenderer == null)
            return;

        bool canCook = queuedIngredients.Count > 0 && !isCooking && CanUseCookingStation;
        cookButtonRenderer.color = canCook
            ? new Color(0.25f, 0.52f, 0.34f, 1f)
            : new Color(0.24f, 0.27f, 0.25f, 1f);
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
