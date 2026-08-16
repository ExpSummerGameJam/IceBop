using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class IngredientInventory : MonoBehaviour
{
    [Serializable]
    private struct InventoryDebugEntry
    {
        public int ingredientId;
        public int amount;
    }

    public static IngredientInventory Instance { get; private set; }

    public event Action<int, int> AmountChanged;

    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private List<InventoryDebugEntry> debugItems = new();
    [SerializeField] private PhysicalInventory2D physicalInventory;

    public IReadOnlyDictionary<int, int> Items => BuildCounts();

    public static IngredientInventory GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        IngredientInventory existing = FindFirstObjectByType<IngredientInventory>();
        if (existing != null)
            return existing;

        GameObject inventoryObject = new(nameof(IngredientInventory));
        return inventoryObject.AddComponent<IngredientInventory>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        physicalInventory = PhysicalInventory2D.FindSceneInventory(transform.position);
        if (physicalInventory != null)
            physicalInventory.InventoryChanged += SyncDebugItems;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (physicalInventory != null)
            physicalInventory.InventoryChanged -= SyncDebugItems;
    }

    public void Add(int ingredientId, int amount)
    {
        Add(ingredientId, amount, transform.position);
    }

    public void Add(int ingredientId, int amount, Vector3 overflowWorldPosition)
    {
        if (ingredientId <= 0 || amount <= 0)
            return;

        PhysicalInventory2D inventory = PhysicalInventory2D.FindSceneInventory(overflowWorldPosition);
        if (inventory == null)
            return;

        inventory.AddIngredients(ingredientId, amount, overflowWorldPosition);
        SyncDebugItems();
        AmountChanged?.Invoke(ingredientId, GetAmount(ingredientId));
    }

    public bool TryConsume(int ingredientId, int amount)
    {
        if (ingredientId <= 0 || amount <= 0)
            return false;

        PhysicalInventory2D inventory = physicalInventory != null
            ? physicalInventory
            : PhysicalInventory2D.FindSceneInventory(transform.position);

        if (inventory == null)
            return false;

        List<InventoryItem2D> candidates = inventory.Items
            .Where(item => item != null && item.IngredientId == ingredientId)
            .Take(amount)
            .ToList();

        if (candidates.Count < amount)
            return false;

        foreach (InventoryItem2D item in candidates)
            inventory.RemoveItem(item);

        SyncDebugItems();
        AmountChanged?.Invoke(ingredientId, GetAmount(ingredientId));
        return true;
    }

    public int GetAmount(int ingredientId)
    {
        PhysicalInventory2D inventory = physicalInventory != null
            ? physicalInventory
            : PhysicalInventory2D.FindSceneInventory(transform.position);

        return inventory != null ? inventory.CountItems(ingredientId) : 0;
    }

    private void SyncDebugItems()
    {
        debugItems = BuildCounts()
            .OrderBy(pair => pair.Key)
            .Select(pair => new InventoryDebugEntry
            {
                ingredientId = pair.Key,
                amount = pair.Value
            })
            .ToList();
    }

    private Dictionary<int, int> BuildCounts()
    {
        Dictionary<int, int> result = new();
        PhysicalInventory2D inventory = physicalInventory != null
            ? physicalInventory
            : PhysicalInventory2D.FindSceneInventory(transform.position);

        if (inventory == null)
            return result;

        foreach (InventoryItem2D item in inventory.Items)
        {
            if (item == null)
                continue;

            result.TryGetValue(item.IngredientId, out int amount);
            result[item.IngredientId] = amount + 1;
        }

        return result;
    }
}
