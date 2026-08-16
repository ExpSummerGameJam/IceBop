using System.Collections.Generic;
using UnityEngine;

public sealed class InventoryDragController2D : MonoBehaviour
{
    [SerializeField] private PhysicalInventory2D inventory;
    [SerializeField] private InventoryView2D inventoryView;
    [SerializeField] private Camera inventoryCamera;
    [SerializeField] private RectTransform viewport;

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
        if (inventoryView == null || !inventoryView.IsLargeViewOpen)
        {
            ReleaseDraggedItem();
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
        inventory?.DelayOverflowCheck(draggedItem, 0.4f);
    }

    private void ReleaseDraggedItem()
    {
        if (draggedItem == null)
            return;

        InventoryItem2D releasedItem = draggedItem;
        draggedItem = null;

        if (TryDropOnReceiver(releasedItem))
            return;

        releasedItem.SetDragging(false);
        inventory?.DelayOverflowCheck(releasedItem, 0.4f);
    }

    private bool TryDropOnReceiver(InventoryItem2D item)
    {
        if (item == null)
            return false;

        Bounds bounds = item.Bounds;
        Collider2D[] hits = Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f);
        HashSet<IInventoryItemDropReceiver2D> checkedReceivers = new();

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.transform.IsChildOf(item.transform))
                continue;

            IInventoryItemDropReceiver2D receiver = hit.GetComponentInParent<IInventoryItemDropReceiver2D>();
            if (receiver == null || !checkedReceivers.Add(receiver))
                continue;

            if (receiver.TryReceiveInventoryItem(item))
                return true;
        }

        return false;
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
