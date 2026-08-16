using UnityEngine;

public enum GatherableDropMode
{
    DataIngredient = 0,
    RandomFromConfiguredIngredients = 1,
    AllConfiguredIngredients = 2
}

public sealed class GatherableObject : MonoBehaviour
{
    [SerializeField] private int gatherableId;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool startGathered;
    [SerializeField] private bool disableTriggerAfterGather = true;
    [SerializeField] private bool autoApplySprites = true;
    [SerializeField] private GatherableDropMode dropMode = GatherableDropMode.DataIngredient;
    [SerializeField] private int configuredDropIngredientIdA;
    [SerializeField] private int configuredDropIngredientIdB;
    [SerializeField] private GameObject availableState;
    [SerializeField] private GameObject depletedState;
    [SerializeField] private SpriteRenderer availableSpriteRenderer;
    [SerializeField] private SpriteRenderer depletedSpriteRenderer;

    private bool gathered;
    private bool playerInRange;
    private Transform currentPlayer;
    private Rigidbody2D triggerRigidbody2D;

    public int GatherableId => gatherableId;
    public GatherableDropMode DropMode => dropMode;
    public int ConfiguredDropIngredientIdA => configuredDropIngredientIdA;
    public int ConfiguredDropIngredientIdB => configuredDropIngredientIdB;
    public bool IsGathered => gathered;
    public bool IsPlayerInRange => playerInRange;

    public void Configure(
        int newGatherableId,
        GameObject newAvailableState,
        GameObject newDepletedState,
        SpriteRenderer newAvailableSpriteRenderer,
        SpriteRenderer newDepletedSpriteRenderer)
    {
        gatherableId = newGatherableId;
        availableState = newAvailableState;
        depletedState = newDepletedState;
        availableSpriteRenderer = newAvailableSpriteRenderer;
        depletedSpriteRenderer = newDepletedSpriteRenderer;
        ApplySpritesFromData();
        ApplyVisualState(gathered);
    }

    public void ConfigureDropIngredients(
        GatherableDropMode newDropMode,
        int newConfiguredDropIngredientIdA,
        int newConfiguredDropIngredientIdB)
    {
        dropMode = newDropMode;
        configuredDropIngredientIdA = newConfiguredDropIngredientIdA;
        configuredDropIngredientIdB = newConfiguredDropIngredientIdB;
    }

    private void Awake()
    {
        EnsureTriggerRigidbody2D();
        gathered = startGathered;
        ApplySpritesFromData();
        ApplyVisualState(gathered);

        if (!HasTriggerCollider())
            Debug.LogWarning($"GatherableObject needs a trigger Collider or Collider2D. object = {name}", this);
    }

    private void Update()
    {
        if (gathered || !playerInRange)
            return;

        if (GatheringInput.WasInteractPressed())
            Gather();
    }

    public bool Gather()
    {
        if (gathered)
            return false;

        if (!GatheringDataLookup.TryGetGatherable(gatherableId, out GatherableDefinition definition))
        {
            Debug.LogWarning($"Gatherable data not found. gatherableId = {gatherableId}", this);
            return false;
        }

        if (definition.SafeDropCount <= 0)
        {
            Debug.LogWarning($"Gatherable dropCount must be greater than 0. gatherableId = {gatherableId}", this);
            return false;
        }

        Vector3 overflowPosition = currentPlayer != null ? currentPlayer.position : transform.position;
        PhysicalInventory2D inventory = PhysicalInventory2D.FindSceneInventory(overflowPosition);
        if (inventory == null)
            return false;

        int addedCount = AddDrops(inventory, definition, overflowPosition);

        if (addedCount <= 0)
            return false;

        if (definition.canGatherOnce)
            SetGathered(true);

        return true;
    }

    private int AddDrops(PhysicalInventory2D inventory, GatherableDefinition definition, Vector3 overflowPosition)
    {
        if (dropMode == GatherableDropMode.DataIngredient)
            return inventory.AddIngredients(definition.ingredientId, definition.SafeDropCount, overflowPosition);

        int configuredCount = GetConfiguredDropIngredientCount();
        if (configuredCount <= 0)
        {
            Debug.LogWarning($"Configured drop ingredients are empty. gatherableId = {gatherableId}", this);
            return 0;
        }

        if (dropMode == GatherableDropMode.AllConfiguredIngredients)
        {
            int addedTotal = 0;
            if (configuredDropIngredientIdA > 0)
                addedTotal += inventory.AddIngredients(configuredDropIngredientIdA, definition.SafeDropCount, overflowPosition);

            if (configuredDropIngredientIdB > 0)
                addedTotal += inventory.AddIngredients(configuredDropIngredientIdB, definition.SafeDropCount, overflowPosition);

            return addedTotal;
        }

        int addedCount = 0;
        for (int i = 0; i < definition.SafeDropCount; i++)
        {
            int ingredientId = GetRandomConfiguredDropIngredientId(configuredCount);
            if (inventory.TryAddIngredient(ingredientId, overflowPosition))
                addedCount++;
        }

        return addedCount;
    }

    private int GetConfiguredDropIngredientCount()
    {
        int count = 0;
        if (configuredDropIngredientIdA > 0)
            count++;

        if (configuredDropIngredientIdB > 0)
            count++;

        return count;
    }

    private int GetRandomConfiguredDropIngredientId(int configuredCount)
    {
        if (configuredCount <= 1)
            return configuredDropIngredientIdA > 0 ? configuredDropIngredientIdA : configuredDropIngredientIdB;

        return Random.value < 0.5f ? configuredDropIngredientIdA : configuredDropIngredientIdB;
    }

    public bool ApplySpritesFromData()
    {
        if (!autoApplySprites)
            return false;

        if (!GatheringDataLookup.TryGetGatherable(gatherableId, out GatherableDefinition definition))
            return false;

        ApplySprite(availableSpriteRenderer, definition.availableSpriteResourcePath);
        ApplySprite(depletedSpriteRenderer, definition.depletedSpriteResourcePath);
        return true;
    }

    public void SetGathered(bool value)
    {
        gathered = value;
        playerInRange = false;
        ApplyVisualState(gathered);
    }

    private void ApplyVisualState(bool isGathered)
    {
        if (availableState != null)
            availableState.SetActive(!isGathered);
        else if (availableSpriteRenderer != null)
            availableSpriteRenderer.enabled = !isGathered;

        if (depletedState != null)
            depletedState.SetActive(isGathered);
        else if (depletedSpriteRenderer != null)
            depletedSpriteRenderer.enabled = isGathered;

        if (disableTriggerAfterGather)
            SetTriggerCollidersEnabled(!isGathered);
    }

    private static void ApplySprite(SpriteRenderer targetRenderer, string resourcePath)
    {
        if (targetRenderer == null || string.IsNullOrWhiteSpace(resourcePath))
            return;

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
            targetRenderer.sprite = sprite;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other.gameObject))
        {
            playerInRange = true;
            currentPlayer = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other.gameObject))
        {
            playerInRange = false;
            if (currentPlayer == other.transform)
                currentPlayer = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other.gameObject))
        {
            playerInRange = true;
            currentPlayer = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other.gameObject))
        {
            playerInRange = false;
            if (currentPlayer == other.transform)
                currentPlayer = null;
        }
    }

    private bool IsPlayer(GameObject candidate)
    {
        if (candidate.GetComponentInParent<GatheringPlayerMarker>() != null)
            return true;

        return string.IsNullOrEmpty(playerTag) || string.Equals(candidate.tag, playerTag, System.StringComparison.Ordinal);
    }

    private void Reset()
    {
        EnsureTriggerRigidbody2D();
        availableSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (availableSpriteRenderer != null)
            availableState = availableSpriteRenderer.gameObject;

        foreach (Collider2D triggerCollider in GetComponentsInChildren<Collider2D>(true))
            triggerCollider.isTrigger = true;

        foreach (Collider triggerCollider in GetComponentsInChildren<Collider>(true))
            triggerCollider.isTrigger = true;
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        ApplySpritesFromData();
        ApplyVisualState(startGathered);
    }

    private bool HasTriggerCollider()
    {
        foreach (Collider2D candidate in GetComponentsInChildren<Collider2D>(true))
        {
            if (candidate.isTrigger)
                return true;
        }

        foreach (Collider candidate in GetComponentsInChildren<Collider>(true))
        {
            if (candidate.isTrigger)
                return true;
        }

        return false;
    }

    private void EnsureTriggerRigidbody2D()
    {
        if (!HasTriggerCollider2D())
            return;

        if (triggerRigidbody2D == null)
            triggerRigidbody2D = GetComponent<Rigidbody2D>();

        if (triggerRigidbody2D == null)
            triggerRigidbody2D = gameObject.AddComponent<Rigidbody2D>();

        triggerRigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        triggerRigidbody2D.gravityScale = 0f;
        triggerRigidbody2D.freezeRotation = true;
    }

    private bool HasTriggerCollider2D()
    {
        foreach (Collider2D candidate in GetComponentsInChildren<Collider2D>(true))
        {
            if (candidate.isTrigger)
                return true;
        }

        return false;
    }

    private void SetTriggerCollidersEnabled(bool enabled)
    {
        foreach (Collider2D triggerCollider in GetComponentsInChildren<Collider2D>(true))
        {
            if (triggerCollider.isTrigger)
                triggerCollider.enabled = enabled;
        }

        foreach (Collider triggerCollider in GetComponentsInChildren<Collider>(true))
        {
            if (triggerCollider.isTrigger)
                triggerCollider.enabled = enabled;
        }
    }
}
