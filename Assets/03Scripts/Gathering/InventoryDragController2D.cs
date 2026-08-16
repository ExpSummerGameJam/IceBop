using UnityEngine;

public sealed class InventoryDragController2D : MonoBehaviour
{
    [SerializeField] private PhysicalInventory2D inventory;
    [SerializeField] private InventoryView2D inventoryView;
    [SerializeField] private Camera inventoryCamera;
    [SerializeField] private RectTransform viewport;

    private InventoryCookingStation2D cookingStation;
    private InventoryItem2D draggedItem;
    private Vector2 grabOffset;

    public void Bind(PhysicalInventory2D newInventory, InventoryView2D newInventoryView, Camera newCamera, RectTransform newViewport)
    {
        inventory = newInventory;
        inventoryView = newInventoryView;
        inventoryCamera = newCamera;
        viewport = newViewport;
    }

    private void Update()
    {
        if (GamePauseState.IsPaused)
        {
            CancelDraggedItem();
            return;
        }

        if (inventoryView == null || !inventoryView.IsLargeViewOpen)
        {
            ReleaseDraggedItem();
            return;
        }

        if (cookingStation == null)
            cookingStation = GetComponent<InventoryCookingStation2D>();

        if (cookingStation != null && cookingStation.IsCooking)
        {
            CancelDraggedItem();
            return;
        }

        if (GatheringInput.WasPrimaryPressed())
            TryBeginDrag();

        if (draggedItem != null && GatheringInput.IsPrimaryHeld())
        {
            if (TryGetPointerWorldPosition(out Vector2 pointerWorldPosition))
                draggedItem.MoveDragged(pointerWorldPosition + grabOffset);
        }

        if (GatheringInput.WasPrimaryReleased())
            ReleaseDraggedItem();
    }

    private void CancelDraggedItem()
    {
        if (draggedItem == null)
            return;

        InventoryItem2D canceledItem = draggedItem;
        draggedItem = null;
        canceledItem.SetDragging(false);
    }

    private void TryBeginDrag()
    {
        if (!TryGetPointerWorldPosition(out Vector2 pointerWorldPosition))
            return;

        Collider2D[] hits = Physics2D.OverlapPointAll(pointerWorldPosition);
        InventoryItem2D bestItem = null;
        int bestSortingOrder = int.MinValue;

        foreach (Collider2D hit in hits)
        {
            InventoryItem2D item = hit.GetComponentInParent<InventoryItem2D>();
            if (item == null)
                continue;

            SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
            int sortingOrder = renderer != null ? renderer.sortingOrder : 0;
            if (bestItem != null && sortingOrder < bestSortingOrder)
                continue;

            bestItem = item;
            bestSortingOrder = sortingOrder;
        }

        if (bestItem == null)
            return;

        draggedItem = bestItem;
        grabOffset = (Vector2)draggedItem.transform.position - pointerWorldPosition;
        draggedItem.SetDragging(true);
    }

    private void ReleaseDraggedItem()
    {
        if (draggedItem == null)
            return;

        InventoryItem2D releasedItem = draggedItem;
        draggedItem = null;

        releasedItem.SetDragging(false);
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
}
