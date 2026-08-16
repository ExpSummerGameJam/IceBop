using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class InventoryOverflowTrigger2D : MonoBehaviour
{
    [SerializeField] private PhysicalInventory2D inventory;

    private void Awake()
    {
        ResolveReferences();

        if (TryGetComponent(out Collider2D triggerCollider))
            triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
            return;

        InventoryItem2D item = other.GetComponentInParent<InventoryItem2D>();
        if (item == null)
            return;

        ResolveReferences();
        inventory?.OverflowItem(item);
    }

    private void ResolveReferences()
    {
        if (inventory == null)
            inventory = GetComponentInParent<PhysicalInventory2D>();
    }

    private void OnValidate()
    {
        if (TryGetComponent(out Collider2D triggerCollider))
            triggerCollider.isTrigger = true;
    }
}
