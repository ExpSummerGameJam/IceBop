using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class PhysicalInventory2D : MonoBehaviour
{
    [SerializeField] private Vector2 bagInnerSize = new(4f, 3f);
    [SerializeField] private float dropYOffset = 0.35f;
    [SerializeField] private float spawnJitter = 0.16f;
    [SerializeField] private float itemGravityScale = 1f;
    [SerializeField] private float overflowMargin = 0.35f;
    [SerializeField] private float overflowCheckDelay = 0.45f;
    [SerializeField] private Collider2D bagBoundsCollider;
    [SerializeField] private Collider2D[] bagBoundaryColliders = new Collider2D[0];
    [SerializeField] private Transform itemRoot;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private InventoryView2D inventoryView;

    private readonly List<InventoryItem2D> items = new();
    private readonly Dictionary<InventoryItem2D, float> overflowCheckTimes = new();
    private Vector3 lastOverflowWorldPosition;
    private bool loggedMissingBagColliders;

    public static PhysicalInventory2D Instance { get; private set; }

    public event Action InventoryChanged;

    public IReadOnlyList<InventoryItem2D> Items => items;
    public Vector2 BagInnerSize => BagWorldRect.size;
    public Transform DropPoint => dropPoint;
    public Vector3 DropWorldPosition => GetDropWorldPosition();

    public Rect BagViewWorldRect
    {
        get
        {
            if (TryGetBagViewBounds(out Bounds bounds))
                return Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);

            return BagWorldRect;
        }
    }

    public Rect BagWorldRect
    {
        get
        {
            if (TryGetBagBounds(out Bounds bounds))
                return Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);

            Vector2 center = transform.position;
            return new Rect(center - bagInnerSize * 0.5f, bagInnerSize);
        }
    }

    public static PhysicalInventory2D FindSceneInventory(Vector3? overflowWorldPosition = null)
    {
        if (Instance != null)
        {
            if (overflowWorldPosition.HasValue)
                Instance.SetOverflowWorldPosition(overflowWorldPosition.Value);

            return Instance;
        }

        PhysicalInventory2D existing = FindFirstObjectByType<PhysicalInventory2D>();
        if (existing != null)
        {
            if (overflowWorldPosition.HasValue)
                existing.SetOverflowWorldPosition(overflowWorldPosition.Value);

            return existing;
        }

        Debug.LogError("PhysicalInventory2D was not found in the scene. Place the PhysicalInventory2D prefab in the scene before playing.");
        return null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        lastOverflowWorldPosition = FindPlayerPosition(transform.position);
        ResolveSceneReferences();
        BindView();
        WarnIfBagCollidersMissing();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void FixedUpdate()
    {
        Rect bagRect = BagWorldRect;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            InventoryItem2D item = items[i];
            if (item == null)
            {
                items.RemoveAt(i);
                continue;
            }

            if (item.IsDragging)
                continue;

            if (overflowCheckTimes.TryGetValue(item, out float checkTime) && Time.time < checkTime)
                continue;

            Vector2 center = item.Bounds.center;
            bool leftOrRightOverflow = center.x < bagRect.xMin - overflowMargin || center.x > bagRect.xMax + overflowMargin;
            bool bottomOverflow = center.y < bagRect.yMin - overflowMargin;

            if (leftOrRightOverflow || bottomOverflow)
                OverflowItem(item);
        }
    }

    public int AddIngredients(int ingredientId, int count, Vector3 overflowWorldPosition)
    {
        SetOverflowWorldPosition(overflowWorldPosition);

        int added = 0;
        for (int i = 0; i < count; i++)
        {
            if (TryAddIngredient(ingredientId, overflowWorldPosition))
                added++;
        }

        return added;
    }

    public bool TryAddIngredient(int ingredientId, Vector3 overflowWorldPosition)
    {
        SetOverflowWorldPosition(overflowWorldPosition);

        IngredientDefinition definition = GatheringDataLookup.GetIngredientOrFallback(ingredientId);
        if (definition == null)
        {
            Debug.LogWarning($"Ingredient data not found. ingredientId = {ingredientId}");
            return false;
        }

        InventoryItem2D item = CreateInventoryItem(definition);
        items.Add(item);
        DelayOverflowCheck(item, overflowCheckDelay);
        InventoryChanged?.Invoke();
        return true;
    }

    public int CountItems(int ingredientId)
    {
        return items.Count(item => item != null && item.IngredientId == ingredientId);
    }

    public bool RemoveItem(InventoryItem2D item)
    {
        if (item == null)
            return false;

        bool removed = items.Remove(item);
        overflowCheckTimes.Remove(item);

        if (removed)
        {
            Destroy(item.gameObject);
            InventoryChanged?.Invoke();
        }

        return removed;
    }

    public void DelayOverflowCheck(InventoryItem2D item, float delay)
    {
        if (item == null)
            return;

        overflowCheckTimes[item] = Time.time + Mathf.Max(0f, delay);
    }

    public void SetOverflowWorldPosition(Vector3 worldPosition)
    {
        lastOverflowWorldPosition = worldPosition;
    }

    public void RegisterView(InventoryView2D view)
    {
        inventoryView = view;
    }

    private InventoryItem2D CreateInventoryItem(IngredientDefinition definition)
    {
        ResolveSceneReferences();

        Transform parent = itemRoot != null ? itemRoot : transform;
        Vector3 spawnPosition = GetDropWorldPosition();

        GameObject itemObject = new();
        itemObject.transform.SetParent(parent, false);
        itemObject.transform.position = spawnPosition + new Vector3(UnityEngine.Random.Range(-spawnJitter, spawnJitter), 0f, 0f);
        itemObject.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-12f, 12f));

        InventoryItem2D item = itemObject.AddComponent<InventoryItem2D>();
        item.Configure(definition, itemGravityScale);
        return item;
    }

    private void OverflowItem(InventoryItem2D item)
    {
        if (item == null)
            return;

        int ingredientId = item.IngredientId;
        items.Remove(item);
        overflowCheckTimes.Remove(item);

        Vector3 spawnPosition = FindPlayerPosition(lastOverflowWorldPosition);
        WorldIngredientPickup2D.Spawn(ingredientId, spawnPosition, true);

        Destroy(item.gameObject);
        InventoryChanged?.Invoke();
    }

    private void ResolveSceneReferences()
    {
        if (itemRoot == null)
            itemRoot = transform.Find("Items");

        if (dropPoint == null)
            dropPoint = transform.Find("DropPoint");

        if (!HasAssignedBagBoundaryColliders())
            bagBoundaryColliders = FindBagBoundaryColliders();

        EnsurePlacedAreaSprites();
    }

    private void BindView()
    {
        if (inventoryView == null)
            inventoryView = GetComponent<InventoryView2D>();

        if (inventoryView != null)
            inventoryView.Bind(this);
    }

    private Vector3 GetDropWorldPosition()
    {
        if (dropPoint != null)
            return dropPoint.position + new Vector3(0f, dropYOffset, 0f);

        Rect bagRect = BagWorldRect;
        return new Vector3(bagRect.center.x, bagRect.yMax + dropYOffset, transform.position.z);
    }

    private bool TryGetBagBounds(out Bounds bounds)
    {
        if (IsUsableBoundsCollider(bagBoundsCollider))
        {
            bounds = bagBoundsCollider.bounds;
            return true;
        }

        if (!HasAssignedBagBoundaryColliders())
            bagBoundaryColliders = FindBagBoundaryColliders();

        bounds = default;
        bool hasBounds = false;

        foreach (Collider2D boundaryCollider in bagBoundaryColliders)
        {
            if (!IsUsableBoundsCollider(boundaryCollider))
                continue;

            if (!hasBounds)
            {
                bounds = boundaryCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(boundaryCollider.bounds);
            }
        }

        return hasBounds;
    }

    private bool TryGetBagViewBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        EncapsulateBagViewBounds(bagBoundsCollider, ref bounds, ref hasBounds);

        if (!HasAssignedBagBoundaryColliders())
            bagBoundaryColliders = FindBagBoundaryColliders();

        foreach (Collider2D boundaryCollider in bagBoundaryColliders)
            EncapsulateBagViewBounds(boundaryCollider, ref bounds, ref hasBounds);

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (!IsBagViewObject(renderer.transform))
                continue;

            EncapsulateBounds(renderer.bounds, ref bounds, ref hasBounds);
        }

        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
        {
            if (!IsBagViewObject(collider.transform))
                continue;

            EncapsulateBagViewBounds(collider, ref bounds, ref hasBounds);
        }

        return hasBounds;
    }

    private void EncapsulateBagViewBounds(Collider2D collider, ref Bounds bounds, ref bool hasBounds)
    {
        if (collider == null || !collider.enabled)
            return;

        EncapsulateBounds(collider.bounds, ref bounds, ref hasBounds);
    }

    private void EncapsulateBounds(Bounds candidate, ref Bounds bounds, ref bool hasBounds)
    {
        if (candidate.size.x <= 0f || candidate.size.y <= 0f)
            return;

        if (!hasBounds)
        {
            bounds = candidate;
            hasBounds = true;
        }
        else
        {
            bounds.Encapsulate(candidate);
        }
    }

    private bool IsBagViewObject(Transform candidate)
    {
        if (candidate == null)
            return false;

        if (itemRoot != null && candidate.IsChildOf(itemRoot))
            return false;

        if (candidate.GetComponentInParent<InventoryItem2D>() != null)
            return false;

        return candidate.GetComponentInParent<Canvas>() == null;
    }

    private Collider2D[] FindBagBoundaryColliders()
    {
        List<Collider2D> colliders = new();

        foreach (Collider2D candidate in GetComponentsInChildren<Collider2D>(true))
        {
            if (IsBagBoundaryCollider(candidate))
                colliders.Add(candidate);
        }

        return colliders.ToArray();
    }

    private bool HasAssignedBagBoundaryColliders()
    {
        if (bagBoundaryColliders == null)
            return false;

        foreach (Collider2D boundaryCollider in bagBoundaryColliders)
        {
            if (boundaryCollider != null)
                return true;
        }

        return false;
    }

    private bool IsBagBoundaryCollider(Collider2D candidate)
    {
        if (candidate == null || !candidate.enabled || candidate.isTrigger)
            return false;

        if (candidate.GetComponentInParent<InventoryItem2D>() != null)
            return false;

        return itemRoot == null || !candidate.transform.IsChildOf(itemRoot);
    }

    private static bool IsUsableBoundsCollider(Collider2D collider)
    {
        if (collider == null || !collider.enabled)
            return false;

        Bounds bounds = collider.bounds;
        return bounds.size.x > 0f && bounds.size.y > 0f;
    }

    private void EnsurePlacedAreaSprites()
    {
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer != null && renderer.sprite == null)
                renderer.sprite = InventoryVisualUtility.WhiteSprite;
        }
    }

    private void WarnIfBagCollidersMissing()
    {
        if (loggedMissingBagColliders || TryGetBagBounds(out _))
            return;

        loggedMissingBagColliders = true;
        Debug.LogWarning(
            "PhysicalInventory2D has no bag colliders assigned or found under the bag object. " +
            "Pre-place the bag wall colliders in the scene/prefab; runtime wall generation is disabled.",
            this);
    }

    private static Vector3 FindPlayerPosition(Vector3 fallback)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            return player.transform.position;

        GatheringPlayerMarker marker = FindFirstObjectByType<GatheringPlayerMarker>();
        return marker != null ? marker.transform.position : fallback;
    }

    private void Reset()
    {
        itemRoot = transform.Find("Items");
        dropPoint = transform.Find("DropPoint");
        inventoryView = GetComponent<InventoryView2D>();
        bagBoundaryColliders = FindBagBoundaryColliders();
    }

    private void OnValidate()
    {
        bagInnerSize.x = Mathf.Max(0.5f, bagInnerSize.x);
        bagInnerSize.y = Mathf.Max(0.5f, bagInnerSize.y);
        overflowCheckDelay = Mathf.Max(0f, overflowCheckDelay);
    }

    private void OnDrawGizmosSelected()
    {
        Rect bagRect = BagWorldRect;
        Vector3 center = new(bagRect.center.x, bagRect.center.y, transform.position.z);
        Vector3 size = new(bagRect.size.x, bagRect.size.y, 0.05f);

        Gizmos.color = new Color(0.92f, 0.68f, 0.34f, 0.18f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(0.92f, 0.68f, 0.34f, 0.9f);
        Gizmos.DrawWireCube(center, size);
    }
}
