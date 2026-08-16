using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class InventoryCookingArea2D : MonoBehaviour
{
    [SerializeField] private InventoryCookingStation2D cookingStation;

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

        if (cookingStation != null)
            cookingStation.BindCookingArea(areaCollider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        NotifyItemPresence(other, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        NotifyItemPresence(other, false);
    }

    private void OnDisable()
    {
        cookingStation?.ClearItemsFromArea(this);
    }

    private void NotifyItemPresence(Collider2D other, bool isOnPot)
    {
        if (other == null)
            return;

        InventoryItem2D item = other.GetComponentInParent<InventoryItem2D>();
        if (item == null)
            return;

        ResolveReferences();
        cookingStation?.SetItemOnPot(item, isOnPot);
    }

    private void ResolveReferences()
    {
        if (areaCollider == null)
            areaCollider = GetComponent<Collider2D>();

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
