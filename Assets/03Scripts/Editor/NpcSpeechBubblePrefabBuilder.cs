using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class NpcSpeechBubblePrefabBuilder
{
    private const string PrefabAssetPath = "Assets/Resources/Prefabs/UI/NpcSpeechBubble.prefab";
    private const string PrefabFolderPath = "Assets/Resources/Prefabs/UI";
    private const int UiLayer = 5;
    private const float PreviewScale = 0.0045f;

    [MenuItem("Tools/IceBop/UI/Rebuild NPC Speech Bubble Prefab")]
    public static void RebuildPrefab()
    {
        EnsureFolder(PrefabFolderPath);

        GameObject root = CreateSpeechBubbleObject();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabAssetPath, out bool success);
        Object.DestroyImmediate(root);

        if (!success || prefab == null)
        {
            Debug.LogError($"Failed to save NPC speech bubble prefab at {PrefabAssetPath}.");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Rebuilt NPC speech bubble prefab: {PrefabAssetPath}");
    }

    [InitializeOnLoadMethod]
    private static void CreatePrefabIfMissing()
    {
        if (File.Exists(PrefabAssetPath))
            return;

        RebuildPrefab();
    }

    private static GameObject CreateSpeechBubbleObject()
    {
        GameObject root = new("NPC_SpeechBubble", typeof(RectTransform), typeof(Canvas));
        root.layer = UiLayer;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        ConfigureCenteredRect(rootRect);
        rootRect.localScale = Vector3.one * PreviewScale;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 120;

        RectTransform panel = CreateRect("BubblePanel", rootRect);
        panel.sizeDelta = new Vector2(118f, 56f);
        AddImage(panel, new Color(0.92f, 0.95f, 0.98f, 1f));

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(27, 27, 19, 19);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter sizeFitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform background = CreateRect("BubbleBackground", panel);
        ConfigureStretchRect(background);
        background.offsetMin = Vector2.one * 5f;
        background.offsetMax = -Vector2.one * 5f;
        AddImage(background, new Color(0.02f, 0.02f, 0.025f, 0.94f));
        background.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        RectTransform tailBorder = CreateRect("BubbleTailBorder", panel);
        tailBorder.localRotation = Quaternion.Euler(0f, 0f, 45f);
        tailBorder.sizeDelta = new Vector2(33f, 33f);
        tailBorder.anchoredPosition = new Vector2(-28.32f, -35f);
        AddImage(tailBorder, new Color(0.92f, 0.95f, 0.98f, 1f));
        tailBorder.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        RectTransform tail = CreateRect("BubbleTail", panel);
        tail.localRotation = Quaternion.Euler(0f, 0f, 45f);
        tail.sizeDelta = new Vector2(28f, 28f);
        tail.anchoredPosition = new Vector2(-28.32f, -35f);
        AddImage(tail, new Color(0.02f, 0.02f, 0.025f, 0.94f));
        tail.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        RectTransform textRect = CreateRect("BubbleText", panel);
        textRect.sizeDelta = new Vector2(64f, 18f);
        Text text = textRect.gameObject.AddComponent<Text>();
        text.font = ResolveDefaultFont();
        text.fontSize = 48;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        text.raycastTarget = false;
        text.color = Color.white;

        LayoutElement textLayout = textRect.gameObject.AddComponent<LayoutElement>();
        textLayout.minWidth = 64f;
        textLayout.minHeight = 18f;

        return root;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = UiLayer;
        rect.SetParent(parent, false);
        ConfigureCenteredRect(rect);
        return rect;
    }

    private static void ConfigureCenteredRect(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void ConfigureStretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static Image AddImage(RectTransform rect, Color color)
    {
        rect.gameObject.AddComponent<CanvasRenderer>();
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Font ResolveDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return font;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);

        string folderName = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            AssetDatabase.CreateFolder(parent, folderName);
    }
}
