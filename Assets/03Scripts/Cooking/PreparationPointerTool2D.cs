using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PreparationPointerTool2D : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private InventoryView2D inventoryView;
    [SerializeField] private int draggingSortingOrder = 50;

    private SpriteRenderer toolRenderer;
    private Vector2 grabOffset;
    private int sortingOrderBeforeDrag;

    public bool IsDragging { get; private set; }
    public Vector2 PointerWorldPosition { get; private set; }
    public Vector2 PreviousPointerWorldPosition { get; private set; }

    public bool HasPointerMovement =>
        IsDragging && (PointerWorldPosition - PreviousPointerWorldPosition).sqrMagnitude > 0.000001f;

    public void Configure(Camera newTargetCamera, InventoryView2D newInventoryView = null)
    {
        targetCamera = newTargetCamera;
        inventoryView = newInventoryView;
        PointerWorldPosition = transform.position;
        PreviousPointerWorldPosition = PointerWorldPosition;
    }

    private void Awake()
    {
        ResolveComponents();
        PointerWorldPosition = transform.position;
        PreviousPointerWorldPosition = PointerWorldPosition;
    }

    private void OnDisable()
    {
        EndDrag();
    }

    private void Update()
    {
        if (GamePauseState.IsPaused)
        {
            EndDrag();
            return;
        }

        if (!IsDragging && GatheringInput.WasPrimaryPressed())
            TryBeginDrag();

        if (IsDragging && GatheringInput.IsPrimaryHeld())
            DragToPointer();

        if (IsDragging && GatheringInput.WasPrimaryReleased())
            EndDrag();
    }

    private void TryBeginDrag()
    {
        if (!TryGetPointerWorldPosition(out Vector2 pointerWorldPosition))
            return;

        if (!IsPointerOverTool(pointerWorldPosition))
            return;

        ResolveComponents();
        IsDragging = true;
        grabOffset = (Vector2)transform.position - pointerWorldPosition;
        sortingOrderBeforeDrag = toolRenderer.sortingOrder;
        toolRenderer.sortingOrder = draggingSortingOrder;
        PointerWorldPosition = pointerWorldPosition;
        PreviousPointerWorldPosition = PointerWorldPosition;
    }

    private void DragToPointer()
    {
        if (!TryGetPointerWorldPosition(out Vector2 pointerWorldPosition))
            return;

        PreviousPointerWorldPosition = PointerWorldPosition;
        PointerWorldPosition = pointerWorldPosition;
        transform.position = pointerWorldPosition + grabOffset;
    }

    private void EndDrag()
    {
        if (!IsDragging)
            return;

        IsDragging = false;

        if (toolRenderer != null)
            toolRenderer.sortingOrder = sortingOrderBeforeDrag;
    }

    private bool IsPointerOverTool(Vector2 pointerWorldPosition)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(pointerWorldPosition);
        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit.transform.IsChildOf(transform))
                return true;
        }

        return false;
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
        Vector3 pointerWorld = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
        worldPosition = pointerWorld;
        return true;
    }

    private void ResolveComponents()
    {
        if (toolRenderer == null)
            toolRenderer = GetComponent<SpriteRenderer>();
    }
}
