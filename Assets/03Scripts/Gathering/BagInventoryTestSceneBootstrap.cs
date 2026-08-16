using UnityEngine;

public sealed class BagInventoryTestSceneBootstrap : MonoBehaviour
{
    [SerializeField] private int[] gatherableIds =
    {
        3101, 3102, 3103, 3201,
        3202, 3203, 3204, 3205,
        3206
    };

    [SerializeField] private Vector2 firstGatherablePosition = new(-7f, 1.4f);
    [SerializeField] private Vector2 gatherableSpacing = new(2f, -1.8f);
    [SerializeField] private int gatherablesPerRow = 4;

    private void Awake()
    {
        CreateCamera();
        Transform player = CreatePlayer();
        CreateGround();
        CreateGatherables();

        PhysicalInventory2D inventory = PhysicalInventory2D.FindSceneInventory(player.position);
        if (inventory == null)
            Debug.LogError("BagInventoryTestScene needs a placed PhysicalInventory2D prefab instance.");
    }

    private static void CreateCamera()
    {
        if (Camera.main != null)
            return;

        GameObject cameraObject = new("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 5.2f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.10f, 0.12f, 0.14f, 1f);
    }

    private static Transform CreatePlayer()
    {
        GameObject player = new("BagTest_Player");
        player.transform.position = new Vector3(-8.5f, -2.2f, 0f);
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
        GameObject ground = new("BagTest_Ground");
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
        GameObject root = new($"BagTest_Gatherable_{gatherableId}");
        root.transform.position = position;

        CircleCollider2D trigger = root.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.9f;

        GameObject available = CreateStateVisual(root.transform, "Available", gatherableId, false);
        GameObject depleted = CreateStateVisual(root.transform, "Depleted", gatherableId, true);
        depleted.SetActive(false);

        GatherableObject gatherable = root.AddComponent<GatherableObject>();
        gatherable.Configure(
            gatherableId,
            available,
            depleted,
            available.GetComponent<SpriteRenderer>(),
            depleted.GetComponent<SpriteRenderer>());
    }

    private static GameObject CreateStateVisual(Transform parent, string name, int id, bool depleted)
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
}
