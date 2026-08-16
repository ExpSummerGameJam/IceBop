using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PreparationKnifeTool2D : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 bladeLocalOffset = new(0.65f, 0f);
    [SerializeField] private int draggingSortingOrder = 50;

    private Collider2D toolCollider;
    private SpriteRenderer toolRenderer;
    private Vector2 grabOffset;
    private int sortingOrderBeforeDrag;
    private Sprite defaultBladeSprite;
    private Sprite activeBladeSprite;
    private bool isCutting;

    public static PreparationKnifeTool2D CurrentDraggingKnife { get; private set; }

    public bool IsDragging { get; private set; }
    public Vector2 BladeWorldPosition { get; private set; }
    public Vector2 PreviousBladeWorldPosition { get; private set; }
    public Vector2 PointerWorldPosition { get; private set; }
    public Vector2 PreviousPointerWorldPosition { get; private set; }

    public bool HasBladeMovement =>
        IsDragging && (BladeWorldPosition - PreviousBladeWorldPosition).sqrMagnitude > 0.000001f;

    public bool HasPointerMovement =>
        IsDragging && (PointerWorldPosition - PreviousPointerWorldPosition).sqrMagnitude > 0.000001f;

    public void Configure(Camera newTargetCamera, Vector2 newBladeLocalOffset)
    {
        targetCamera = newTargetCamera;
        bladeLocalOffset = newBladeLocalOffset;
        UpdateBladePosition();
        PreviousBladeWorldPosition = BladeWorldPosition;
        PointerWorldPosition = BladeWorldPosition;
        PreviousPointerWorldPosition = PointerWorldPosition;
    }

    public void ConfigureBladeSprites(Sprite newDefaultSprite, Sprite newActiveSprite)
    {
        defaultBladeSprite = newDefaultSprite;
        activeBladeSprite = newActiveSprite;
        isCutting = false;
        ApplyBladeSprite();
    }

    public void SetCutting(bool cutting)
    {
        if (isCutting == cutting)
            return;

        isCutting = cutting;
        ApplyBladeSprite();
    }

    private void ApplyBladeSprite()
    {
        ResolveComponents();

        if (toolRenderer == null)
            return;

        Sprite sprite = isCutting && activeBladeSprite != null ? activeBladeSprite : defaultBladeSprite;
        if (sprite != null)
            toolRenderer.sprite = sprite;
    }

    private void Awake()
    {
        ResolveComponents();
        UpdateBladePosition();
        PreviousBladeWorldPosition = BladeWorldPosition;
        PointerWorldPosition = BladeWorldPosition;
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
        CurrentDraggingKnife = this;
        grabOffset = (Vector2)transform.position - pointerWorldPosition;
        sortingOrderBeforeDrag = toolRenderer.sortingOrder;
        toolRenderer.sortingOrder = draggingSortingOrder;
        PointerWorldPosition = pointerWorldPosition;
        PreviousPointerWorldPosition = PointerWorldPosition;
        UpdateBladePosition();
        PreviousBladeWorldPosition = BladeWorldPosition;
    }

    private void DragToPointer()
    {
        if (!TryGetPointerWorldPosition(out Vector2 pointerWorldPosition))
            return;

        PreviousPointerWorldPosition = PointerWorldPosition;
        PointerWorldPosition = pointerWorldPosition;
        PreviousBladeWorldPosition = BladeWorldPosition;
        transform.position = pointerWorldPosition + grabOffset;
        UpdateBladePosition();
    }

    private void EndDrag()
    {
        if (!IsDragging)
            return;

        IsDragging = false;

        if (CurrentDraggingKnife == this)
            CurrentDraggingKnife = null;

        if (toolRenderer != null)
            toolRenderer.sortingOrder = sortingOrderBeforeDrag;

        UpdateBladePosition();
        PreviousBladeWorldPosition = BladeWorldPosition;
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

    private void UpdateBladePosition()
    {
        BladeWorldPosition = transform.TransformPoint(bladeLocalOffset);
    }

    private void ResolveComponents()
    {
        if (toolCollider == null)
            toolCollider = GetComponent<Collider2D>();

        if (toolRenderer == null)
            toolRenderer = GetComponent<SpriteRenderer>();
    }
}
