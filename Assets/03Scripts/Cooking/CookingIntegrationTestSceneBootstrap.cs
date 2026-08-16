using UnityEngine;

public sealed class CookingIntegrationTestSceneBootstrap : MonoBehaviour
{
    [SerializeField] private PhysicalInventory2D inventory;
    [SerializeField] private CookingStageArea2D cookingStageArea;
    [SerializeField] private CookingFlowController2D cookingFlowController;
    [SerializeField] private WorldCookingArea2D worldCookingArea;

    [SerializeField] private int[] gatherableIds =
    {
        3101, 3102, 3103, 3201,
        3202, 3203, 3204, 3205,
        3206
    };

    [SerializeField] private int[] startingIngredientIds =
    {
        1101, 1203, 1201
    };

    [SerializeField] private Vector2 firstGatherablePosition = new(-7.25f, 1.35f);
    [SerializeField] private Vector2 gatherableSpacing = new(2.05f, -1.75f);
    [SerializeField] private int gatherablesPerRow = 4;
    [SerializeField] private bool openInventoryOnStart = true;

    private void Awake()
    {
        Camera camera = CreateCamera();
        Transform player = CreatePlayer();
        CreateGround();
        CreateGatherables();

        PhysicalInventory2D sceneInventory = ResolveInventory(player.position);
        SeedInventory(sceneInventory, player.position);
        EnsureWorldCookingArea(sceneInventory, player.position);

        InventoryView2D view = sceneInventory != null ? sceneInventory.GetComponent<InventoryView2D>() : null;
        if (view != null)
            view.SetLargeViewOpen(openInventoryOnStart);

        RequireCookingStageArea();
        EnsureCookingFlowController();
        CreateSceneLabel(camera);
    }

    private static Camera CreateCamera()
    {
        if (Camera.main != null)
            return Camera.main;

        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.2f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.10f, 0.12f, 0.14f, 1f);
        return camera;
    }

    private static Transform CreatePlayer()
    {
        GameObject player = new("IntegrationTest_Player");
        player.tag = "Player";
        player.transform.position = new Vector3(-8.4f, -2.2f, 0f);
        player.AddComponent<GatheringPlayerMarker>();

        Rigidbody2D rigidbody = player.AddComponent<Rigidbody2D>();
        rigidbody.gravityScale = 0f;
        rigidbody.freezeRotation = true;

        CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
        collider.radius = 0.38f;

        SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = InventoryVisualUtility.WhiteSprite;
        renderer.color = new Color(0.18f, 0.62f, 0.95f, 1f);
        renderer.sortingOrder = 20;
        player.transform.localScale = new Vector3(0.72f, 0.72f, 1f);

        player.AddComponent<BagTestPlayerController2D>();
        return player.transform;
    }

    private static void CreateGround()
    {
        GameObject ground = new("IntegrationTest_Ground");
        ground.transform.position = new Vector3(0f, -3.25f, 0f);
        ground.transform.localScale = new Vector3(18f, 0.35f, 1f);

        SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
        renderer.sprite = InventoryVisualUtility.WhiteSprite;
        renderer.color = new Color(0.36f, 0.31f, 0.25f, 1f);
        renderer.sortingOrder = -5;

        BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
    }

    private void CreateGatherables()
    {
        for (int i = 0; i < gatherableIds.Length; i++)
        {
            int column = i % Mathf.Max(1, gatherablesPerRow);
            int row = i / Mathf.Max(1, gatherablesPerRow);
            Vector2 position = firstGatherablePosition + new Vector2(gatherableSpacing.x * column, gatherableSpacing.y * row);
            CreateGatherable(gatherableIds[i], position);
        }
    }

    private static void CreateGatherable(int gatherableId, Vector2 position)
    {
        GameObject root = new($"IntegrationTest_Gatherable_{gatherableId}");
        root.transform.position = position;

        CircleCollider2D trigger = root.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.9f;

        GameObject available = CreateGatherableStateVisual(root.transform, "Available", gatherableId, false);
        GameObject depleted = CreateGatherableStateVisual(root.transform, "Depleted", gatherableId, true);
        depleted.SetActive(false);

        GatherableObject gatherable = root.AddComponent<GatherableObject>();
        gatherable.Configure(
            gatherableId,
            available,
            depleted,
            available.GetComponent<SpriteRenderer>(),
            depleted.GetComponent<SpriteRenderer>());
    }

    private static GameObject CreateGatherableStateVisual(Transform parent, string name, int id, bool depleted)
    {
        GameObject visual = new(name);
        visual.transform.SetParent(parent, false);
        visual.transform.localScale = depleted
            ? new Vector3(0.55f, 0.35f, 1f)
            : new Vector3(0.75f, 0.75f, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = InventoryVisualUtility.WhiteSprite;
        renderer.color = depleted
            ? new Color(0.28f, 0.27f, 0.25f, 1f)
            : InventoryVisualUtility.ColorFromId(id);
        renderer.sortingOrder = 10;

        return visual;
    }

    private PhysicalInventory2D ResolveInventory(Vector3 overflowWorldPosition)
    {
        if (inventory == null)
            inventory = PhysicalInventory2D.FindSceneInventory(overflowWorldPosition);
        else
            inventory.SetOverflowWorldPosition(overflowWorldPosition);

        if (inventory == null)
            Debug.LogError("CookingIntegrationTestScene needs a placed PhysicalInventory2D prefab instance.");

        return inventory;
    }

    private void RequireCookingStageArea()
    {
        if (cookingStageArea == null)
            cookingStageArea = FindFirstObjectByType<CookingStageArea2D>();

        if (cookingStageArea == null)
            Debug.LogError("CookingIntegrationTestScene needs a placed CookingStageArea2D object.");
    }

    private void SeedInventory(PhysicalInventory2D inventory, Vector3 overflowWorldPosition)
    {
        if (inventory == null)
            return;

        foreach (int ingredientId in startingIngredientIds)
            inventory.TryAddIngredient(ingredientId, overflowWorldPosition);
    }

    private void EnsureCookingFlowController()
    {
        if (cookingFlowController != null)
            return;

        cookingFlowController = FindFirstObjectByType<CookingFlowController2D>();
        if (cookingFlowController != null)
            return;

        cookingFlowController = new GameObject("CookingFlowController").AddComponent<CookingFlowController2D>();
    }

    private void EnsureWorldCookingArea(PhysicalInventory2D sceneInventory, Vector3 playerPosition)
    {
        if (worldCookingArea == null)
            worldCookingArea = FindFirstObjectByType<WorldCookingArea2D>();

        InventoryCookingStation2D station = sceneInventory != null
            ? sceneInventory.GetComponent<InventoryCookingStation2D>()
            : FindFirstObjectByType<InventoryCookingStation2D>();

        if (worldCookingArea == null)
            worldCookingArea = CreateWorldCookingArea(playerPosition);

        if (station != null)
            worldCookingArea.Bind(station);
    }

    private static WorldCookingArea2D CreateWorldCookingArea(Vector3 playerPosition)
    {
        GameObject areaObject = new("WorldCookingArea");
        areaObject.transform.position = new Vector3(playerPosition.x, playerPosition.y, 0f);
        areaObject.transform.localScale = new Vector3(2.25f, 1.45f, 1f);

        SpriteRenderer renderer = areaObject.AddComponent<SpriteRenderer>();
        renderer.sprite = InventoryVisualUtility.WhiteSprite;
        renderer.color = new Color(0.94f, 0.64f, 0.18f, 0.22f);
        renderer.sortingOrder = -4;

        BoxCollider2D collider = areaObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        return areaObject.AddComponent<WorldCookingArea2D>();
    }

    private static void CreateSceneLabel(Camera camera)
    {
        if (camera == null)
            return;

        GameObject label = new("IntegrationTest_Label");
        label.transform.position = new Vector3(0f, 4.45f, 0f);

        TextMesh text = label.AddComponent<TextMesh>();
        text.text = "Cooking Integration Test";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 42;
        text.characterSize = 0.075f;
        text.color = new Color(0.92f, 0.95f, 0.92f, 1f);

        MeshRenderer renderer = label.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sortingOrder = 20;
    }
}
