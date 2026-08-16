using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class WorldIngredientPickup2D : MonoBehaviour
{
    [SerializeField] private int ingredientId;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float pickupRadius = 1.1f;

    private Rigidbody2D itemRigidbody;
    private BoxCollider2D itemCollider;
    private SpriteRenderer itemRenderer;
    private Transform playerInRange;

    public int IngredientId => ingredientId;

    public static WorldIngredientPickup2D Spawn(int ingredientId, Vector3 position, bool launch)
    {
        GameObject itemObject = new($"WorldIngredient_{ingredientId}");
        itemObject.transform.position = position;
        WorldIngredientPickup2D pickup = itemObject.AddComponent<WorldIngredientPickup2D>();
        pickup.Configure(ingredientId, launch);
        return pickup;
    }

    private void Awake()
    {
        EnsureComponents();
        ApplyDefinition(false);
    }

    private void Update()
    {
        if (playerInRange == null)
            return;

        if (GatheringInput.WasInteractPressed())
            TryPickup();
    }

    public void Configure(int newIngredientId, bool launch)
    {
        ingredientId = newIngredientId;
        ApplyDefinition(launch);
    }

    public void SetPlayerInRange(Transform player, bool inRange)
    {
        if (inRange)
        {
            playerInRange = player;
        }
        else if (playerInRange == player)
        {
            playerInRange = null;
        }
    }

    private void TryPickup()
    {
        Vector3 overflowPosition = playerInRange != null ? playerInRange.position : transform.position;
        PhysicalInventory2D inventory = PhysicalInventory2D.FindSceneInventory(overflowPosition);
        if (inventory == null)
            return;

        if (inventory.TryAddIngredient(ingredientId, overflowPosition))
            Destroy(gameObject);
    }

    private void ApplyDefinition(bool launch)
    {
        EnsureComponents();

        IngredientDefinition definition = GatheringDataLookup.GetIngredientOrFallback(ingredientId);
        if (definition == null)
            return;

        gameObject.name = $"WorldIngredient_{definition.ingredientKey}_{definition.ingredientId}";

        Sprite sprite = InventoryVisualUtility.GetIngredientSprite(definition);

        itemRenderer.sprite = sprite != null ? sprite : InventoryVisualUtility.WhiteSprite;
        itemRenderer.color = sprite != null ? Color.white : InventoryVisualUtility.ColorFromId(definition.ingredientId);
        itemRenderer.sortingOrder = 5;
        ApplyPhysicalSize(sprite);
        EnsurePickupTrigger();

        if (launch)
        {
            Vector2 force = new(
                Random.Range(-definition.WorldDropForce, definition.WorldDropForce),
                definition.WorldDropForce);
            itemRigidbody.AddForce(force, ForceMode2D.Impulse);
            itemRigidbody.AddTorque(Random.Range(-0.8f, 0.8f), ForceMode2D.Impulse);
        }
    }

    private void EnsureComponents()
    {
        if (itemRigidbody == null)
            itemRigidbody = GetComponent<Rigidbody2D>();

        if (itemCollider == null)
            itemCollider = GetComponent<BoxCollider2D>();

        if (itemRenderer == null)
            itemRenderer = GetComponent<SpriteRenderer>();

        itemRigidbody.bodyType = RigidbodyType2D.Dynamic;
        itemRigidbody.gravityScale = 1f;
        itemRigidbody.linearDamping = 0.25f;
        itemRigidbody.angularDamping = 0.1f;
        itemRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        itemCollider.isTrigger = false;
    }

    private void ApplyPhysicalSize(Sprite sprite)
    {
        Sprite sizeSprite = sprite != null ? sprite : InventoryVisualUtility.WhiteSprite;
        if (sizeSprite == null || sizeSprite.bounds.size.x <= 0f || sizeSprite.bounds.size.y <= 0f)
        {
            transform.localScale = Vector3.one;
            itemCollider.size = new Vector2(0.55f, 0.55f);
            return;
        }

        Vector2 spriteSize = sizeSprite.bounds.size;
        transform.localScale = Vector3.one;
        itemCollider.size = spriteSize;
    }

    private void EnsurePickupTrigger()
    {
        Transform trigger = transform.Find("PickupTrigger");
        if (trigger == null)
        {
            GameObject triggerObject = new("PickupTrigger");
            triggerObject.transform.SetParent(transform, false);
            trigger = triggerObject.transform;
        }

        CircleCollider2D triggerCollider = trigger.GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
            triggerCollider = trigger.gameObject.AddComponent<CircleCollider2D>();

        triggerCollider.isTrigger = true;
        triggerCollider.radius = Mathf.Max(0.1f, pickupRadius);
        trigger.localScale = new Vector3(
            InverseScale(transform.localScale.x),
            InverseScale(transform.localScale.y),
            1f);

        WorldIngredientPickupTrigger2D triggerRelay = trigger.GetComponent<WorldIngredientPickupTrigger2D>();
        if (triggerRelay == null)
            triggerRelay = trigger.gameObject.AddComponent<WorldIngredientPickupTrigger2D>();

        triggerRelay.Bind(this, playerTag);
    }

    private static float InverseScale(float value)
    {
        return Mathf.Approximately(value, 0f) ? 1f : 1f / Mathf.Abs(value);
    }
}
