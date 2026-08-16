using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class InventoryItem2D : MonoBehaviour
{
    private readonly List<Vector2> physicsShapeBuffer = new();

    private Rigidbody2D itemRigidbody;
    private PolygonCollider2D itemCollider;
    private SpriteRenderer itemRenderer;
    private RigidbodyType2D bodyTypeBeforeDrag;
    private float gravityScaleBeforeDrag;

    public int IngredientId { get; private set; }
    public IngredientDefinition Definition { get; private set; }
    public Rigidbody2D Rigidbody => itemRigidbody;
    public Collider2D Collider => itemCollider;
    public Bounds Bounds => itemCollider != null ? itemCollider.bounds : new Bounds(transform.position, Vector3.zero);
    public bool IsDragging { get; private set; }

    private void Awake()
    {
        EnsureComponents();
    }

    public void Configure(IngredientDefinition definition, float gravityScale)
    {
        EnsureComponents();

        Definition = definition;
        IngredientId = definition.ingredientId;
        gameObject.name = $"InventoryItem_{definition.ingredientKey}_{definition.ingredientId}";

        Sprite sprite = InventoryVisualUtility.GetIngredientSprite(definition);

        itemRenderer.sprite = sprite != null ? sprite : InventoryVisualUtility.WhiteSprite;
        itemRenderer.color = sprite != null ? Color.white : InventoryVisualUtility.ColorFromId(definition.ingredientId);
        itemRenderer.sortingOrder = 10;
        ApplyPhysicalSize(sprite);

        itemRigidbody.bodyType = RigidbodyType2D.Dynamic;
        itemRigidbody.gravityScale = gravityScale;
        itemRigidbody.linearDamping = 0.4f;
        itemRigidbody.angularDamping = 0.2f;
        itemRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void SetDragging(bool value)
    {
        EnsureComponents();

        if (IsDragging == value)
            return;

        IsDragging = value;

        if (value)
        {
            bodyTypeBeforeDrag = itemRigidbody.bodyType;
            gravityScaleBeforeDrag = itemRigidbody.gravityScale;
            itemRigidbody.linearVelocity = Vector2.zero;
            itemRigidbody.angularVelocity = 0f;
            itemRigidbody.bodyType = RigidbodyType2D.Kinematic;
            itemRigidbody.gravityScale = 0f;
        }
        else
        {
            itemRigidbody.bodyType = bodyTypeBeforeDrag;
            itemRigidbody.gravityScale = gravityScaleBeforeDrag;
            itemRigidbody.linearVelocity = Vector2.zero;
            itemRigidbody.angularVelocity = 0f;
        }
    }

    public void MoveDragged(Vector2 worldPosition)
    {
        if (itemRigidbody == null)
            transform.position = worldPosition;
        else
            itemRigidbody.MovePosition(worldPosition);
    }

    public void Launch(Vector2 force)
    {
        EnsureComponents();
        SetDragging(false);
        itemRigidbody.AddForce(force, ForceMode2D.Impulse);
        itemRigidbody.AddTorque(Random.Range(-0.8f, 0.8f), ForceMode2D.Impulse);
    }

    private void EnsureComponents()
    {
        if (itemRigidbody == null)
            itemRigidbody = GetComponent<Rigidbody2D>();

        if (itemCollider == null)
            itemCollider = GetComponent<PolygonCollider2D>();

        if (itemRenderer == null)
            itemRenderer = GetComponent<SpriteRenderer>();
    }

    private void ApplyPhysicalSize(Sprite sprite)
    {
        transform.localScale = Vector3.one;

        Sprite sizeSprite = sprite != null ? sprite : InventoryVisualUtility.WhiteSprite;
        if (sizeSprite == null || sizeSprite.bounds.size.x <= 0f || sizeSprite.bounds.size.y <= 0f)
        {
            ApplyBoundsRectangleShape(new Bounds(Vector3.zero, new Vector3(0.55f, 0.55f, 0f)));
            return;
        }

        int shapeCount = sizeSprite.GetPhysicsShapeCount();
        if (shapeCount <= 0)
        {
            ApplyBoundsRectangleShape(sizeSprite.bounds);
            return;
        }

        itemCollider.pathCount = shapeCount;
        for (int i = 0; i < shapeCount; i++)
        {
            sizeSprite.GetPhysicsShape(i, physicsShapeBuffer);
            itemCollider.SetPath(i, physicsShapeBuffer);
        }
    }

    private void ApplyBoundsRectangleShape(Bounds bounds)
    {
        Vector2 min = bounds.min;
        Vector2 max = bounds.max;

        itemCollider.pathCount = 1;
        itemCollider.SetPath(0, new[]
        {
            new Vector2(min.x, min.y),
            new Vector2(max.x, min.y),
            new Vector2(max.x, max.y),
            new Vector2(min.x, max.y),
        });
    }
}
