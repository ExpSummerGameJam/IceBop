using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryView2D : MonoBehaviour
{
    [SerializeField] private PhysicalInventory2D inventory;
    [SerializeField] private Camera miniCamera;
    [SerializeField] private Camera largeCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RawImage miniView;
    [SerializeField] private RawImage largeView;
    [SerializeField] private InventoryDragController2D dragController;
    [SerializeField] private InventoryCookingStation2D cookingStation;
    [SerializeField] private Vector2 miniViewSize = new(192f, 144f);
    [SerializeField] private Vector2 largeViewSize = new(1920f, 1080f);
    [SerializeField] private Vector2 miniViewMargin = new(16f, 16f);
    [SerializeField] private float inventoryCameraMargin = 0.35f;
    [SerializeField] private float miniDropPointPadding = 0.25f;
    [SerializeField] private float miniBagScale = 1.25f;
    [SerializeField] private bool renderLargeViewAsCameraOverlay;
    [SerializeField] private float largeCameraDepthOffset = 1f;
    [SerializeField] private Vector3 largeCameraPositionCorrection = Vector3.zero;
    [SerializeField] private Color miniTint = Color.white;
    [SerializeField] private Color largeTint = Color.white;

    private RenderTexture miniTexture;
    private RenderTexture largeTexture;
    private bool largeCameraBasePositionCaptured;
    private const float MinimumCameraSize = 0.1f;

    public bool IsLargeViewOpen { get; private set; }
    public RectTransform LargeViewport => largeView != null ? largeView.rectTransform : null;
    public Camera LargeCamera => largeCamera;

    private bool IsLargeCameraOverlay => false;

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<PhysicalInventory2D>();

        if (inventory != null)
            Bind(inventory);
    }

    private void Update()
    {
        if (GatheringInput.WasInventoryPressed())
            SetLargeViewOpen(!IsLargeViewOpen);
    }

    private void OnDestroy()
    {
        ReleaseRenderTexture(ref miniTexture);
        ReleaseRenderTexture(ref largeTexture);
    }

    public void Bind(PhysicalInventory2D newInventory)
    {
        inventory = newInventory;
        inventory.RegisterView(this);
        EnsureCameras();
        EnsureCanvasViews();
        ApplyViewLayout();
        DisableUnusedOutsideView();
        EnsureDragController();
        EnsureCookingStation();
        SetLargeViewOpen(IsLargeViewOpen);
    }

    public void SetLargeViewOpen(bool open)
    {
        IsLargeViewOpen = open;

        if (miniView != null)
            miniView.gameObject.SetActive(!open);

        if (miniCamera != null)
            miniCamera.enabled = !open;

        if (largeView != null)
        {
            largeView.gameObject.SetActive(open && !IsLargeCameraOverlay);
            largeView.enabled = !IsLargeCameraOverlay;
            largeView.raycastTarget = !IsLargeCameraOverlay;
        }

        if (largeCamera != null)
            ConfigureLargeCameraOutput(largeCamera);
    }

    private void EnsureCameras()
    {
        miniCamera = ResolveInventoryCamera("MiniInventoryCamera", miniCamera, miniViewSize, true);
        largeCamera = ResolveInventoryCamera("LargeInventoryCamera", largeCamera, largeViewSize, false);
    }

    private Camera ResolveInventoryCamera(string cameraName, Camera camera, Vector2 viewSize, bool isMiniCamera)
    {
        if (camera == null)
        {
            Transform existing = transform.Find(cameraName);
            if (existing != null)
                camera = existing.GetComponent<Camera>();
        }

        if (camera == null)
        {
            Debug.LogError($"InventoryView2D needs a placed {cameraName} child camera.", this);
            return null;
        }

        camera.transform.rotation = Quaternion.identity;
        camera.orthographic = true;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 50f;

        Vector2Int textureSize = GetTextureSize(viewSize);
        ApplyInventoryCameraFrame(camera, textureSize, isMiniCamera);

        if (!isMiniCamera && !largeCameraBasePositionCaptured)
        {
            largeCameraBasePositionCaptured = true;
            camera.transform.position += largeCameraPositionCorrection;
        }

        if (!isMiniCamera && IsLargeCameraOverlay)
        {
            ReleaseRenderTexture(ref largeTexture);
            ConfigureLargeCameraOutput(camera);
            return camera;
        }

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);

        RenderTexture texture = isMiniCamera ? miniTexture : largeTexture;
        if (texture == null || texture.width != textureSize.x || texture.height != textureSize.y)
        {
            if (isMiniCamera)
            {
                ReleaseRenderTexture(ref miniTexture);
                miniTexture = CreateRenderTexture(textureSize);
                miniTexture.Create();
                texture = miniTexture;
            }
            else
            {
                ReleaseRenderTexture(ref largeTexture);
                largeTexture = CreateRenderTexture(textureSize);
                largeTexture.Create();
                texture = largeTexture;
            }
        }

        camera.targetTexture = texture;
        camera.enabled = isMiniCamera ? !IsLargeViewOpen : IsLargeViewOpen;
        return camera;
    }

    private void ConfigureLargeCameraOutput(Camera camera)
    {
        if (camera == null)
            return;

        if (!IsLargeCameraOverlay)
        {
            camera.enabled = IsLargeViewOpen;
            return;
        }

        camera.targetTexture = null;
        camera.clearFlags = CameraClearFlags.Depth;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.depth = ResolveLargeCameraDepth();
        camera.enabled = IsLargeViewOpen;
    }

    private float ResolveLargeCameraDepth()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || mainCamera == largeCamera)
            return largeCameraDepthOffset;

        return mainCamera.depth + Mathf.Max(0.01f, largeCameraDepthOffset);
    }

    private void EnsureCanvasViews()
    {
        if (canvas == null)
        {
            Transform existing = transform.Find("InventoryViewCanvas");
            if (existing != null)
                canvas = existing.GetComponent<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError("InventoryView2D needs a placed InventoryViewCanvas child.", this);
            return;
        }

        miniView = ResolveRawImage("MiniInventoryView", miniView, miniTexture, miniTint);
        largeView = ResolveRawImage("LargeInventoryView", largeView, IsLargeCameraOverlay ? null : largeTexture, largeTint);
    }

    private RawImage ResolveRawImage(string viewName, RawImage view, Texture texture, Color tint)
    {
        if (view == null)
        {
            Transform existing = canvas.transform.Find(viewName);
            if (existing != null)
                view = existing.GetComponent<RawImage>();
        }

        if (view == null)
        {
            Debug.LogError($"InventoryView2D needs a placed {viewName} RawImage under InventoryViewCanvas.", this);
            return null;
        }

        view.texture = texture;
        view.color = tint;
        view.raycastTarget = true;
        return view;
    }

    private void EnsureDragController()
    {
        if (dragController == null)
            dragController = GetComponent<InventoryDragController2D>();

        if (dragController == null)
        {
            Debug.LogError("InventoryView2D needs a placed InventoryDragController2D component on the inventory object.", this);
            return;
        }

        if (largeCamera == null || largeView == null)
            return;

        dragController.Bind(inventory, this, largeCamera, largeView.rectTransform);
    }

    public bool TryGetLargeViewPointerWorldPosition(out Vector2 worldPosition)
    {
        worldPosition = default;

        if (!IsLargeViewOpen || largeCamera == null)
            return false;

        Vector2 screenPosition = GatheringInput.PointerScreenPosition();

        if (IsLargeCameraOverlay)
            return TryGetCameraScreenWorldPosition(largeCamera, screenPosition, out worldPosition);

        return TryGetViewportWorldPosition(largeCamera, LargeViewport, screenPosition, out worldPosition);
    }

    private void LateUpdate()
    {
        ApplyViewLayout();
        UpdateInventoryCameraFrames();
    }

    private void ApplyViewLayout()
    {
        ApplyAbsoluteViewSize(miniView, miniViewSize);
        ApplyAbsoluteViewSize(largeView, largeViewSize);

        if (miniView != null)
            miniView.rectTransform.anchoredPosition = miniViewMargin;
    }

    private void UpdateInventoryCameraFrames()
    {
        ApplyInventoryCameraFrame(miniCamera, GetTextureSize(miniViewSize), true);
        ApplyInventoryCameraFrame(largeCamera, GetTextureSize(largeViewSize), false);

        if (largeCamera != null && IsLargeCameraOverlay)
            largeCamera.depth = ResolveLargeCameraDepth();
    }

    private void ApplyInventoryCameraFrame(Camera camera, Vector2Int textureSize, bool includeDropPoint)
    {
        if (camera == null)
            return;

        Rect bagRect = includeDropPoint ? GetMiniInventoryViewWorldRect() : GetInventoryViewWorldRect();
        float frameScale = includeDropPoint ? miniBagScale : 1f;
        camera.orthographicSize = CalculateInventoryCameraSize(textureSize, bagRect, frameScale);
    }

    private Rect GetMiniInventoryViewWorldRect()
    {
        Rect bagRect = GetInventoryViewWorldRect();

        if (inventory != null)
            return EncapsulatePoint(bagRect, inventory.DropWorldPosition, miniDropPointPadding);

        Rect fallbackRect = inventory != null ? inventory.BagWorldRect : bagRect;
        Vector2 fallbackDropPoint = new(fallbackRect.center.x, fallbackRect.yMax);
        return EncapsulatePoint(bagRect, fallbackDropPoint, miniDropPointPadding);
    }

    private Rect GetInventoryViewWorldRect()
    {
        return inventory != null
            ? inventory.BagViewWorldRect
            : new Rect((Vector2)transform.position - new Vector2(2f, 1.5f), new Vector2(4f, 3f));
    }

    private float CalculateInventoryCameraSize(Vector2Int textureSize, Rect bagRect, float frameScale)
    {
        float aspect = textureSize.x / Mathf.Max(1f, textureSize.y);
        float margin = Mathf.Max(0f, inventoryCameraMargin);
        float halfHeight = bagRect.height * 0.5f + margin;
        float halfWidthAsHeight = (bagRect.width * 0.5f + margin) / Mathf.Max(0.01f, aspect);
        float cameraSize = Mathf.Max(halfHeight, halfWidthAsHeight);
        return Mathf.Max(MinimumCameraSize, cameraSize / Mathf.Max(0.01f, frameScale));
    }

    private static Rect EncapsulatePoint(Rect rect, Vector2 point, float padding)
    {
        float safePadding = Mathf.Max(0f, padding);
        return Rect.MinMaxRect(
            Mathf.Min(rect.xMin, point.x - safePadding),
            Mathf.Min(rect.yMin, point.y - safePadding),
            Mathf.Max(rect.xMax, point.x + safePadding),
            Mathf.Max(rect.yMax, point.y + safePadding));
    }

    private static bool TryGetCameraScreenWorldPosition(Camera camera, Vector2 screenPosition, out Vector2 worldPosition)
    {
        worldPosition = default;

        if (camera == null || !camera.pixelRect.Contains(screenPosition))
            return false;

        float zDistance = Mathf.Abs(camera.transform.position.z);
        Vector3 cameraWorld = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
        worldPosition = cameraWorld;
        return true;
    }

    private static bool TryGetViewportWorldPosition(Camera camera, RectTransform viewport, Vector2 screenPosition, out Vector2 worldPosition)
    {
        worldPosition = default;

        if (camera == null || viewport == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPosition, null, out Vector2 localPoint))
            return false;

        Rect rect = viewport.rect;
        Vector2 normalized = new(
            (localPoint.x - rect.xMin) / rect.width,
            (localPoint.y - rect.yMin) / rect.height);

        if (normalized.x < 0f || normalized.x > 1f || normalized.y < 0f || normalized.y > 1f)
            return false;

        float zDistance = Mathf.Abs(camera.transform.position.z);
        Vector3 cameraWorld = camera.ViewportToWorldPoint(new Vector3(normalized.x, normalized.y, zDistance));
        worldPosition = cameraWorld;
        return true;
    }

    private static void ApplyAbsoluteViewSize(RawImage view, Vector2 size)
    {
        if (view == null)
            return;

        Vector2 safeSize = GetSafeViewSize(size);
        RectTransform rect = view.rectTransform;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, safeSize.x);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, safeSize.y);
    }

    private static Vector2Int GetTextureSize(Vector2 size)
    {
        Vector2 safeSize = GetSafeViewSize(size);
        return new Vector2Int(Mathf.RoundToInt(safeSize.x), Mathf.RoundToInt(safeSize.y));
    }

    private static Vector2 GetSafeViewSize(Vector2 size)
    {
        return new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
    }

    private void OnValidate()
    {
        inventoryCameraMargin = Mathf.Max(0f, inventoryCameraMargin);
        miniDropPointPadding = Mathf.Max(0f, miniDropPointPadding);
        miniBagScale = Mathf.Max(0.01f, miniBagScale);
        largeCameraDepthOffset = Mathf.Max(0.01f, largeCameraDepthOffset);
    }

    private static RenderTexture CreateRenderTexture(Vector2Int size)
    {
        return new RenderTexture(size.x, size.y, 16, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    private void EnsureCookingStation()
    {
        if (cookingStation == null)
            cookingStation = GetComponent<InventoryCookingStation2D>();

        if (cookingStation == null)
        {
            Debug.LogError("InventoryView2D needs a placed InventoryCookingStation2D component on the inventory object.", this);
            return;
        }

        cookingStation.Bind(inventory, this);
    }

    private void DisableUnusedOutsideView()
    {
        Transform outsideCameraTransform = transform.Find("BagOutsideCamera");
        if (outsideCameraTransform != null)
            outsideCameraTransform.gameObject.SetActive(false);

        Transform outsideViewTransform = canvas != null ? canvas.transform.Find("BagOutsideView") : null;
        if (outsideViewTransform != null)
            outsideViewTransform.gameObject.SetActive(false);
    }

    private static void ReleaseRenderTexture(ref RenderTexture texture)
    {
        if (texture == null)
            return;

        texture.Release();
        Destroy(texture);
        texture = null;
    }
}
