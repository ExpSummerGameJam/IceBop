using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class FixedResolution1920x1080 : MonoBehaviour
{
    [Header("Resolution")]
    [SerializeField] private Vector2Int referenceResolution = new(1920, 1080);
    [SerializeField] private bool setScreenResolutionInPlayMode = true;
    [SerializeField] private FullScreenMode fullScreenMode = FullScreenMode.Windowed;

    [Header("Camera")]
    [SerializeField] private bool applyToAllCameras = true;
    [SerializeField] private Camera[] targetCameras = new Camera[0];

    [Header("UI")]
    [SerializeField] private bool applyToAllCanvases = true;
    [SerializeField] private Canvas[] targetCanvases = new Canvas[0];
    [SerializeField] private bool addCanvasScalerIfMissing = true;
    [SerializeField] private CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
    [Range(0f, 1f)]
    [SerializeField] private float matchWidthOrHeight = 0.5f;
    [SerializeField] private bool stretchRootCanvasRect = true;

    private int lastScreenWidth;
    private int lastScreenHeight;
    private int lastCameraCount;
    private int lastCanvasCount;
    private Vector2Int lastReferenceResolution;

    private float ReferenceAspect =>
        referenceResolution.y <= 0 ? 16f / 9f : referenceResolution.x / (float)referenceResolution.y;

    private void OnEnable()
    {
        ApplyNow();
    }

    private void Start()
    {
        ApplyNow();
    }

    private void LateUpdate()
    {
        int currentCameraCount = GetCurrentCameraCount();
        int currentCanvasCount = GetCurrentCanvasCount();

        if (Screen.width == lastScreenWidth &&
            Screen.height == lastScreenHeight &&
            currentCameraCount == lastCameraCount &&
            currentCanvasCount == lastCanvasCount &&
            referenceResolution == lastReferenceResolution)
            return;

        ApplyNow();
    }

    private void OnValidate()
    {
        ClampReferenceResolution();
    }

    [ContextMenu("Apply Fixed Resolution")]
    public void ApplyNow()
    {
        ClampReferenceResolution();
        ApplyScreenResolution();
        ApplyCameraRects();
        ApplyCanvasSettings();

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastCameraCount = GetCurrentCameraCount();
        lastCanvasCount = GetCurrentCanvasCount();
        lastReferenceResolution = referenceResolution;
    }

    private void ApplyScreenResolution()
    {
        if (!Application.isPlaying || !setScreenResolutionInPlayMode)
            return;

        if (Screen.width == referenceResolution.x && Screen.height == referenceResolution.y)
            return;

        Screen.SetResolution(referenceResolution.x, referenceResolution.y, fullScreenMode);
    }

    private void ApplyCameraRects()
    {
        Rect cameraRect = CalculateCameraRect();

        if (applyToAllCameras)
        {
            foreach (Camera targetCamera in Camera.allCameras)
                ApplyCameraRect(targetCamera, cameraRect);

            return;
        }

        if (targetCameras == null)
            return;

        foreach (Camera targetCamera in targetCameras)
            ApplyCameraRect(targetCamera, cameraRect);
    }

    private Rect CalculateCameraRect()
    {
        float screenWidth = Mathf.Max(1, Screen.width);
        float screenHeight = Mathf.Max(1, Screen.height);
        float windowAspect = screenWidth / screenHeight;
        float targetAspect = ReferenceAspect;

        if (Mathf.Approximately(windowAspect, targetAspect))
            return new Rect(0f, 0f, 1f, 1f);

        if (windowAspect > targetAspect)
        {
            float width = targetAspect / windowAspect;
            return new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }

        float height = windowAspect / targetAspect;
        return new Rect(0f, (1f - height) * 0.5f, 1f, height);
    }

    private static void ApplyCameraRect(Camera targetCamera, Rect cameraRect)
    {
        if (targetCamera == null)
            return;

        if (targetCamera.targetTexture != null)
        {
            targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
            return;
        }

        targetCamera.rect = cameraRect;
    }

    private void ApplyCanvasSettings()
    {
        if (applyToAllCanvases)
        {
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                ApplyCanvasSettings(canvas);

            return;
        }

        if (targetCanvases == null)
            return;

        foreach (Canvas canvas in targetCanvases)
            ApplyCanvasSettings(canvas);
    }

    private void ApplyCanvasSettings(Canvas canvas)
    {
        if (canvas == null)
            return;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null && addCanvasScalerIfMissing)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(referenceResolution.x, referenceResolution.y);
            scaler.screenMatchMode = screenMatchMode;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
        }

        if (!stretchRootCanvasRect || canvas.renderMode == RenderMode.WorldSpace)
            return;

        RectTransform rectTransform = canvas.GetComponent<RectTransform>();
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private void ClampReferenceResolution()
    {
        referenceResolution.x = Mathf.Max(1, referenceResolution.x);
        referenceResolution.y = Mathf.Max(1, referenceResolution.y);
    }

    private int GetCurrentCameraCount()
    {
        if (applyToAllCameras)
            return Camera.allCamerasCount;

        return targetCameras != null ? targetCameras.Length : 0;
    }

    private int GetCurrentCanvasCount()
    {
        if (applyToAllCanvases)
            return FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

        return targetCanvases != null ? targetCanvases.Length : 0;
    }
}
