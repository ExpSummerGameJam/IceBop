using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class InventoryCookingArea2D : MonoBehaviour, IInventoryItemDropReceiver2D
{
    [SerializeField] private InventoryCookingStation2D cookingStation;
    [SerializeField] private bool hideRendererOnPlay = true;

    private Collider2D areaCollider;

    public Collider2D AreaCollider
    {
        get
        {
            ResolveReferences();
            return areaCollider;
        }
    }

    private void Awake()
    {
        ResolveReferences();

        if (areaCollider != null)
            areaCollider.isTrigger = true;

        if (hideRendererOnPlay && TryGetComponent(out Renderer areaRenderer))
            areaRenderer.enabled = false;

        if (cookingStation != null)
            cookingStation.BindCookingArea(areaCollider);
    }

    public bool TryReceiveInventoryItem(InventoryItem2D item)
    {
        ResolveReferences();

        if (cookingStation == null)
            return false;

        return cookingStation.TryReceiveInventoryItem(item);
    }

    private void ResolveReferences()
    {
        if (areaCollider == null)
            areaCollider = GetComponent<Collider2D>();

        if (TryGetComponent(out SpriteRenderer renderer) && renderer.sprite == null)
            renderer.sprite = InventoryVisualUtility.WhiteSprite;

        if (cookingStation == null)
            cookingStation = GetComponentInParent<InventoryCookingStation2D>();

        if (cookingStation == null)
            cookingStation = FindFirstObjectByType<InventoryCookingStation2D>();
    }

    private void OnValidate()
    {
        if (areaCollider == null)
            areaCollider = GetComponent<Collider2D>();

        if (areaCollider != null)
            areaCollider.isTrigger = true;
    }

    private void OnDrawGizmos()
    {
        Collider2D targetCollider = areaCollider != null ? areaCollider : GetComponent<Collider2D>();
        if (targetCollider == null)
            return;

        Bounds bounds = targetCollider.bounds;
        Gizmos.color = new Color(0.2f, 0.78f, 0.42f, 0.18f);
        Gizmos.DrawCube(bounds.center, bounds.size);
        Gizmos.color = new Color(0.2f, 0.78f, 0.42f, 0.9f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
