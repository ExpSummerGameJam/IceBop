using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class InventoryScenePlacementBuilder
{
    private const string InventoryPrefabPath = "Assets/01Resources/Prefabs/Gathering/PhysicalInventory2D.prefab";
    private const string WhiteSquarePath = "Assets/01Resources/Sprites/Generated/WhiteSquare.png";
    private const string CookingIntegrationScenePath = "Assets/00Scenes/CookingIntegrationTestScene.unity";
    private const string BagInventoryScenePath = "Assets/00Scenes/BagInventoryTestScene.unity";
    private static readonly Vector2 MiniViewSize = new(192f, 144f);
    private static readonly Vector2 LargeViewSize = new(1920f, 1080f);
    private static readonly Vector2 ViewMargin = new(16f, 16f);
    private const float InventoryCameraMargin = 0.35f;
    private const float LargeCameraDepthOffset = 1f;

    [MenuItem("IceBop/Build Placed Inventory And Cooking Areas")]
    public static void ApplyScenePlacement()
    {
        Sprite blockSprite = EnsureWhiteSquareSprite();
        BuildInventoryPrefab(blockSprite);
        EnsureScenePlacement(CookingIntegrationScenePath, blockSprite, true);
        EnsureScenePlacement(BagInventoryScenePath, blockSprite, false);
        AssetDatabase.SaveAssets();
    }

    private static void BuildInventoryPrefab(Sprite blockSprite)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(InventoryPrefabPath);

        try
        {
            ConfigureInventoryPrefabRoot(root, blockSprite);
            PrefabUtility.SaveAsPrefabAsset(root, InventoryPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureInventoryPrefabRoot(GameObject root, Sprite blockSprite)
    {
        root.name = "PhysicalInventory2D";
        root.transform.localPosition = new Vector3(10000f, 10000f, 0f);
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        PhysicalInventory2D inventory = GetOrAddComponent<PhysicalInventory2D>(root);
        InventoryView2D view = GetOrAddComponent<InventoryView2D>(root);
        InventoryDragController2D dragController = GetOrAddComponent<InventoryDragController2D>(root);
        InventoryCookingStation2D cookingStation = GetOrAddComponent<InventoryCookingStation2D>(root);

        Transform items = FindOrCreateChild(root.transform, "Items");
        ConfigureTransform(items, Vector3.zero, Vector3.one);

        Transform dropPoint = FindOrCreateChild(root.transform, "DropPoint");
        ConfigureTransform(dropPoint, new Vector3(0f, 1.55f, 0f), Vector3.one);

        Transform panel = FindOrCreateChild(root.transform, "BagBackPanel");
        ConfigureBlock(panel, new Vector3(0f, 0f, 0f), new Vector3(4.25f, 3.05f, 1f), blockSprite, new Color(0.18f, 0.16f, 0.13f, 1f), -4);
        BoxCollider2D panelCollider = GetOrAddComponent<BoxCollider2D>(panel.gameObject);
        panelCollider.isTrigger = true;
        panelCollider.size = Vector2.one;

        BoxCollider2D bottomWall = ConfigureWall(root.transform, "BagBottomWall", new Vector3(0f, -1.62f, 0f), new Vector3(4.35f, 0.18f, 1f), blockSprite);
        BoxCollider2D leftWall = ConfigureWall(root.transform, "BagLeftWall", new Vector3(-2.12f, 0f, 0f), new Vector3(0.18f, 3.15f, 1f), blockSprite);
        BoxCollider2D rightWall = ConfigureWall(root.transform, "BagRightWall", new Vector3(2.12f, 0f, 0f), new Vector3(0.18f, 3.15f, 1f), blockSprite);

        Transform overflowTrigger = FindOrCreateChild(root.transform, "OverflowGroundTrigger");
        ConfigureTransform(overflowTrigger, new Vector3(0f, -1.95f, 0f), Vector3.one);
        BoxCollider2D overflowTriggerCollider = GetOrAddComponent<BoxCollider2D>(overflowTrigger.gameObject);
        overflowTriggerCollider.isTrigger = true;
        overflowTriggerCollider.size = new Vector2(4.6f, 0.4f);
        InventoryOverflowTrigger2D overflowTriggerComponent = GetOrAddComponent<InventoryOverflowTrigger2D>(overflowTrigger.gameObject);

        DestroyChildIfExists(root.transform, "CookingDropArea");

        Camera miniCamera = ConfigureInventoryCamera(root.transform, "MiniInventoryCamera");
        Camera largeCamera = ConfigureInventoryCamera(root.transform, "LargeInventoryCamera");
        DestroyChildIfExists(root.transform, "BagOutsideCamera");
        miniCamera.enabled = true;
        largeCamera.enabled = false;
        largeCamera.clearFlags = CameraClearFlags.SolidColor;
        largeCamera.depth = LargeCameraDepthOffset;
        Canvas canvas = ConfigureInventoryCanvas(root.transform);
        RawImage miniView = ConfigureRawImage(canvas.transform, "MiniInventoryView", MiniViewSize, Vector2.zero, Vector2.zero, Vector2.zero, ViewMargin);
        RawImage largeView = ConfigureRawImage(canvas.transform, "LargeInventoryView", LargeViewSize, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        DestroyChildIfExists(canvas.transform, "BagOutsideView");
        largeView.enabled = false;
        largeView.raycastTarget = false;
        largeView.gameObject.SetActive(false);

        (Transform pot, BoxCollider2D potCollider) = ConfigureCookingStationVisuals(root.transform, blockSprite);
        InventoryCookingArea2D cookingArea = GetOrAddComponent<InventoryCookingArea2D>(pot.gameObject);

        SerializedObject inventoryObject = new(inventory);
        inventoryObject.FindProperty("bagBoundsCollider").objectReferenceValue = panelCollider;
        SerializedProperty boundaryColliders = inventoryObject.FindProperty("bagBoundaryColliders");
        boundaryColliders.arraySize = 3;
        boundaryColliders.GetArrayElementAtIndex(0).objectReferenceValue = bottomWall;
        boundaryColliders.GetArrayElementAtIndex(1).objectReferenceValue = leftWall;
        boundaryColliders.GetArrayElementAtIndex(2).objectReferenceValue = rightWall;
        inventoryObject.FindProperty("itemRoot").objectReferenceValue = items;
        inventoryObject.FindProperty("dropPoint").objectReferenceValue = dropPoint;
        inventoryObject.FindProperty("inventoryView").objectReferenceValue = view;
        inventoryObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject overflowTriggerObject = new(overflowTriggerComponent);
        overflowTriggerObject.FindProperty("inventory").objectReferenceValue = inventory;
        overflowTriggerObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject viewObject = new(view);
        viewObject.FindProperty("inventory").objectReferenceValue = inventory;
        viewObject.FindProperty("miniCamera").objectReferenceValue = miniCamera;
        viewObject.FindProperty("largeCamera").objectReferenceValue = largeCamera;
        viewObject.FindProperty("canvas").objectReferenceValue = canvas;
        viewObject.FindProperty("miniView").objectReferenceValue = miniView;
        viewObject.FindProperty("largeView").objectReferenceValue = largeView;
        viewObject.FindProperty("dragController").objectReferenceValue = dragController;
        viewObject.FindProperty("cookingStation").objectReferenceValue = cookingStation;
        viewObject.FindProperty("miniViewSize").vector2Value = MiniViewSize;
        viewObject.FindProperty("largeViewSize").vector2Value = LargeViewSize;
        viewObject.FindProperty("miniViewMargin").vector2Value = ViewMargin;
        viewObject.FindProperty("inventoryCameraMargin").floatValue = InventoryCameraMargin;
        viewObject.FindProperty("renderLargeViewAsCameraOverlay").boolValue = false;
        viewObject.FindProperty("largeCameraDepthOffset").floatValue = LargeCameraDepthOffset;
        viewObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject dragObject = new(dragController);
        dragObject.FindProperty("inventory").objectReferenceValue = inventory;
        dragObject.FindProperty("inventoryView").objectReferenceValue = view;
        dragObject.FindProperty("inventoryCamera").objectReferenceValue = largeCamera;
        dragObject.FindProperty("viewport").objectReferenceValue = largeView.rectTransform;
        dragObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject stationObject = new(cookingStation);
        stationObject.FindProperty("inventory").objectReferenceValue = inventory;
        stationObject.FindProperty("inventoryView").objectReferenceValue = view;
        stationObject.FindProperty("cookingAreaCollider").objectReferenceValue = potCollider;
        stationObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject cookingAreaObject = new(cookingArea);
        cookingAreaObject.FindProperty("cookingStation").objectReferenceValue = cookingStation;
        cookingAreaObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureScenePlacement(string scenePath, Sprite blockSprite, bool includeCookingStage)
    {
        EditorSceneManager.OpenScene(scenePath);

        PhysicalInventory2D inventory = Object.FindFirstObjectByType<PhysicalInventory2D>();
        if (inventory == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "PhysicalInventory2D";
            instance.transform.position = new Vector3(10000f, 10000f, 0f);
            inventory = instance.GetComponent<PhysicalInventory2D>();
        }

        if (includeCookingStage)
            ConfigureCookingIntegrationScene(blockSprite, inventory);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static void ConfigureCookingIntegrationScene(Sprite blockSprite, PhysicalInventory2D inventory)
    {
        CookingFlowController2D controller = Object.FindFirstObjectByType<CookingFlowController2D>();
        if (controller == null)
            controller = new GameObject("CookingFlowController").AddComponent<CookingFlowController2D>();

        CookingStageArea2D stageArea = Object.FindFirstObjectByType<CookingStageArea2D>();
        if (stageArea == null)
        {
            GameObject areaObject = new("CookingStageArea");
            areaObject.transform.position = new Vector3(0f, 0.24f, 0f);
            areaObject.transform.localScale = new Vector3(3.55f, 2.18f, 1f);
            stageArea = areaObject.AddComponent<CookingStageArea2D>();
        }

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(stageArea.gameObject);
        renderer.enabled = true;
        renderer.sprite = blockSprite;
        renderer.color = new Color(0.24f, 0.52f, 0.72f, 0.18f);
        renderer.sortingOrder = -6;

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(stageArea.gameObject);
        collider.isTrigger = true;
        collider.size = Vector2.one;

        SerializedObject stageAreaObject = new(stageArea);
        stageAreaObject.FindProperty("cookingFlowController").objectReferenceValue = controller;
        stageAreaObject.ApplyModifiedPropertiesWithoutUndo();

        WorldCookingArea2D worldCookingArea = Object.FindFirstObjectByType<WorldCookingArea2D>();
        if (worldCookingArea == null)
        {
            GameObject areaObject = new("WorldCookingArea");
            areaObject.transform.position = new Vector3(-8.4f, -2.2f, 0f);
            areaObject.transform.localScale = new Vector3(2.25f, 1.45f, 1f);
            GetOrAddComponent<SpriteRenderer>(areaObject);
            GetOrAddComponent<BoxCollider2D>(areaObject);
            worldCookingArea = areaObject.AddComponent<WorldCookingArea2D>();
        }

        SpriteRenderer worldAreaRenderer = GetOrAddComponent<SpriteRenderer>(worldCookingArea.gameObject);
        worldAreaRenderer.enabled = true;
        worldAreaRenderer.sprite = blockSprite;
        worldAreaRenderer.color = new Color(0.94f, 0.64f, 0.18f, 0.22f);
        worldAreaRenderer.sortingOrder = -4;

        BoxCollider2D worldAreaCollider = GetOrAddComponent<BoxCollider2D>(worldCookingArea.gameObject);
        worldAreaCollider.isTrigger = true;
        worldAreaCollider.size = Vector2.one;

        SerializedObject worldAreaObject = new(worldCookingArea);
        worldAreaObject.FindProperty("cookingStation").objectReferenceValue = inventory != null
            ? inventory.GetComponent<InventoryCookingStation2D>()
            : null;
        worldAreaObject.ApplyModifiedPropertiesWithoutUndo();

        CookingIntegrationTestSceneBootstrap bootstrap = Object.FindFirstObjectByType<CookingIntegrationTestSceneBootstrap>();
        if (bootstrap == null)
            return;

        SerializedObject bootstrapObject = new(bootstrap);
        bootstrapObject.FindProperty("inventory").objectReferenceValue = inventory;
        bootstrapObject.FindProperty("cookingStageArea").objectReferenceValue = stageArea;
        bootstrapObject.FindProperty("cookingFlowController").objectReferenceValue = controller;
        bootstrapObject.FindProperty("worldCookingArea").objectReferenceValue = worldCookingArea;
        bootstrapObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite EnsureWhiteSquareSprite()
    {
        string folder = Path.GetDirectoryName(WhiteSquarePath)?.Replace("\\", "/");
        if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
        {
            string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            string child = Path.GetFileName(folder);
            AssetDatabase.CreateFolder(parent, child);
        }

        if (!File.Exists(WhiteSquarePath))
        {
            Texture2D texture = new(16, 16, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(WhiteSquarePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(WhiteSquarePath);
        }

        TextureImporter importer = AssetImporter.GetAtPath(WhiteSquarePath) as TextureImporter;
        if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spritePixelsPerUnit != 16f))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSquarePath);
    }

    private static BoxCollider2D ConfigureWall(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Sprite blockSprite)
    {
        Transform wall = FindOrCreateChild(parent, name);
        ConfigureBlock(wall, localPosition, localScale, blockSprite, new Color(0.48f, 0.34f, 0.20f, 1f), -3);

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(wall.gameObject);
        collider.isTrigger = false;
        collider.size = Vector2.one;
        return collider;
    }

    private static void ConfigureBlock(Transform transform, Vector3 localPosition, Vector3 localScale, Sprite blockSprite, Color color, int sortingOrder)
    {
        ConfigureTransform(transform, localPosition, localScale);

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(transform.gameObject);
        renderer.enabled = true;
        renderer.sprite = blockSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private static Camera ConfigureInventoryCamera(Transform parent, string name)
    {
        Transform cameraTransform = FindOrCreateChild(parent, name);
        ConfigureTransform(cameraTransform, new Vector3(0f, 0f, -10f), Vector3.one);

        Camera camera = GetOrAddComponent<Camera>(cameraTransform.gameObject);
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 50f;
        return camera;
    }

    private static Canvas ConfigureInventoryCanvas(Transform parent)
    {
        RectTransform canvasTransform = FindOrCreateRectChild(parent, "InventoryViewCanvas");
        canvasTransform.localRotation = Quaternion.identity;
        canvasTransform.localScale = Vector3.one;
        canvasTransform.anchorMin = Vector2.zero;
        canvasTransform.anchorMax = Vector2.zero;
        canvasTransform.pivot = Vector2.zero;
        canvasTransform.anchoredPosition = Vector2.zero;
        canvasTransform.sizeDelta = Vector2.zero;

        Canvas canvas = GetOrAddComponent<Canvas>(canvasTransform.gameObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasTransform.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GetOrAddComponent<GraphicRaycaster>(canvasTransform.gameObject);
        return canvas;
    }

    private static RawImage ConfigureRawImage(
        Transform parent,
        string name,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition)
    {
        RectTransform rectTransform = FindOrCreateRectChild(parent, name);
        RawImage rawImage = GetOrAddComponent<RawImage>(rectTransform.gameObject);
        rawImage.color = Color.white;
        rawImage.raycastTarget = true;

        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;
        rectTransform.sizeDelta = size;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        return rawImage;
    }

    private static (Transform pot, BoxCollider2D potCollider) ConfigureCookingStationVisuals(Transform parent, Sprite blockSprite)
    {
        Transform root = FindOrCreateChild(parent, "InventoryCookingStation");
        ConfigureTransform(root, Vector3.zero, Vector3.one);

        Transform pot = FindOrCreateChild(root, "PrepPot");
        ConfigureTransform(pot, new Vector3(1.38f, -1.00f, 0f), new Vector3(1.0326f, 1f, 1f));
        BoxCollider2D potCollider = GetOrAddComponent<BoxCollider2D>(pot.gameObject);
        potCollider.isTrigger = true;
        potCollider.size = new Vector2(0.92f, 0.72f);

        ConfigureBlock(FindOrCreateChild(pot, "PotShadow"), new Vector3(0f, -0.09f, 0f), new Vector3(1.02f, 0.44f, 1f), blockSprite, new Color(0.16f, 0.15f, 0.14f, 1f), 34);
        ConfigureBlock(FindOrCreateChild(pot, "PotBody"), Vector3.zero, new Vector3(0.92f, 0.56f, 1f), blockSprite, new Color(0.26f, 0.29f, 0.31f, 1f), 35);
        ConfigureBlock(FindOrCreateChild(pot, "PotLiquid"), new Vector3(0f, 0.09f, 0f), new Vector3(0.74f, 0.20f, 1f), blockSprite, new Color(0.66f, 0.42f, 0.20f, 1f), 36);
        ConfigureBlock(FindOrCreateChild(pot, "PotRim"), new Vector3(0f, 0.28f, 0f), new Vector3(1.04f, 0.16f, 1f), blockSprite, new Color(0.46f, 0.49f, 0.51f, 1f), 37);

        Transform button = FindOrCreateChild(root, "CookButton");
        ConfigureTransform(button, new Vector3(1.38f, -1.57f, 0f), Vector3.one);
        BoxCollider2D buttonCollider = GetOrAddComponent<BoxCollider2D>(button.gameObject);
        buttonCollider.isTrigger = true;
        buttonCollider.size = new Vector2(0.86f, 0.28f);

        ConfigureBlock(FindOrCreateChild(button, "ButtonBody"), Vector3.zero, new Vector3(0.86f, 0.28f, 1f), blockSprite, new Color(0.25f, 0.45f, 0.34f, 1f), 38);
        ConfigureText(FindOrCreateChild(button, "ButtonLabel"), "Cook", new Vector3(0f, -0.012f, 0f), 32, 0.032f, new Color(0.92f, 0.96f, 0.92f, 1f), 39);

        DestroyChildIfExists(root, "QueuedIngredients");
        DestroyChildIfExists(root, "CountLabel");

        return (pot, potCollider);
    }

    private static void ConfigureText(Transform transform, string text, Vector3 localPosition, int fontSize, float characterSize, Color color, int sortingOrder)
    {
        ConfigureTransform(transform, localPosition, Vector3.one);

        TextMesh label = GetOrAddComponent<TextMesh>(transform.gameObject);
        label.text = text;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = fontSize;
        label.characterSize = characterSize;
        label.color = color;

        MeshRenderer renderer = GetOrAddComponent<MeshRenderer>(transform.gameObject);
        renderer.sortingOrder = sortingOrder;
    }

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing;

        GameObject child = new(name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static RectTransform FindOrCreateRectChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing is RectTransform existingRect)
            return existingRect;

        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
            existing = null;
        }

        GameObject child = new(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static void DestroyChildIfExists(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);
    }

    private static void ConfigureTransform(Transform transform, Vector3 localPosition, Vector3 localScale)
    {
        transform.localPosition = localPosition;
        transform.localRotation = Quaternion.identity;
        transform.localScale = localScale;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
            component = gameObject.AddComponent<T>();

        return component;
    }
}
