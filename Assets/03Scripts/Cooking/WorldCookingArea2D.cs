using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class WorldCookingArea2D : MonoBehaviour
{
    [SerializeField] private InventoryCookingStation2D cookingStation;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool hideRendererOnPlay = true;

    private readonly HashSet<Collider2D> playerColliders = new();
    private Collider2D areaCollider;

    public Collider2D AreaCollider
    {
        get
        {
            ResolveReferences();
            return areaCollider;
        }
    }

    public bool HasPlayerInside => playerColliders.Count > 0;

    public void Bind(InventoryCookingStation2D newCookingStation)
    {
        cookingStation = newCookingStation;
        NotifyStation();
    }

    private void Awake()
    {
        ResolveReferences();

        if (areaCollider != null)
            areaCollider.isTrigger = true;

        if (hideRendererOnPlay && TryGetComponent(out Renderer areaRenderer))
            areaRenderer.enabled = false;
    }

    private void Start()
    {
        RefreshPlayerPresence();
    }

    private void Update()
    {
        if (playerColliders.Count == 0)
            return;

        int removedCount = playerColliders.RemoveWhere(IsInvalidPlayerCollider);
        if (removedCount > 0)
            NotifyStation();
    }

    private void OnDisable()
    {
        if (playerColliders.Count == 0)
            return;

        playerColliders.Clear();
        NotifyStation();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || !IsPlayer(other.gameObject) || !playerColliders.Add(other))
            return;

        NotifyStation();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null || !playerColliders.Remove(other))
            return;

        NotifyStation();
    }

    private void RefreshPlayerPresence()
    {
        ResolveReferences();

        if (areaCollider == null)
            return;

        Bounds bounds = areaCollider.bounds;
        Collider2D[] hits = Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f);
        bool changed = false;

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit == areaCollider || !IsPlayer(hit.gameObject))
                continue;

            changed |= playerColliders.Add(hit);
        }

        if (changed)
            NotifyStation();
    }

    private void NotifyStation()
    {
        ResolveReferences();
        cookingStation?.SetCookingAreaPlayerPresence(this, HasPlayerInside);
    }

    private void ResolveReferences()
    {
        if (areaCollider == null)
            areaCollider = GetComponent<Collider2D>();

        if (TryGetComponent(out SpriteRenderer renderer) && renderer.sprite == null)
            renderer.sprite = InventoryVisualUtility.WhiteSprite;

        if (cookingStation == null)
            cookingStation = FindFirstObjectByType<InventoryCookingStation2D>();
    }

    private bool IsInvalidPlayerCollider(Collider2D candidate)
    {
        return candidate == null ||
               !candidate.enabled ||
               !candidate.gameObject.activeInHierarchy ||
               !IsPlayer(candidate.gameObject);
    }

    private bool IsPlayer(GameObject candidate)
    {
        if (candidate == null)
            return false;

        if (candidate.GetComponentInParent<GatheringPlayerMarker>() != null)
            return true;

        return string.IsNullOrEmpty(playerTag) ||
               string.Equals(candidate.tag, playerTag, System.StringComparison.Ordinal);
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
        Gizmos.color = new Color(0.94f, 0.64f, 0.18f, 0.18f);
        Gizmos.DrawCube(bounds.center, bounds.size);
        Gizmos.color = new Color(0.94f, 0.64f, 0.18f, 0.9f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
