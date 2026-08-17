using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryInfoTapController2D : MonoBehaviour
{
    private const string UnknownLabel = "???";

    [SerializeField] private InventoryView2D inventoryView;
    [SerializeField] private InventoryCookingStation2D cookingStation;
    [SerializeField] private GameObject infoTapRoot;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI infoText;

    private readonly List<int> potIngredientIds = new();

    private void Awake()
    {
        DishDiscoveryTracker.EnsureInitialized();
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();

        if (infoTapRoot == null)
            return;

        if (inventoryView == null || !inventoryView.IsLargeViewOpen)
        {
            infoTapRoot.SetActive(false);
            return;
        }

        if (TryGetHoveredIngredient(out IngredientDefinition hovered))
        {
            ShowIngredientInfo(hovered);
            return;
        }

        if (cookingStation != null && cookingStation.ItemsOnPot.Count > 0)
        {
            ShowPotPreview();
            return;
        }

        infoTapRoot.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (inventoryView == null)
            inventoryView = GetComponentInParent<InventoryView2D>();

        if (cookingStation == null)
            cookingStation = GetComponentInParent<InventoryCookingStation2D>();

        if (infoTapRoot != null && iconImage != null && typeText != null && infoText != null)
            return;

        Transform canvas = transform.Find("InventoryViewCanvas");
        Transform infoTap = canvas != null ? canvas.Find("InfoTap") : null;
        if (infoTap == null)
            return;

        infoTapRoot = infoTap.gameObject;

        if (iconImage == null)
        {
            Transform icon = infoTap.Find("Icon");
            iconImage = icon != null ? icon.GetComponent<Image>() : null;
        }

        if (typeText == null)
        {
            Transform type = infoTap.Find("Type");
            typeText = type != null ? type.GetComponent<TextMeshProUGUI>() : null;
        }

        if (infoText == null)
        {
            Transform info = infoTap.Find("Info");
            infoText = info != null ? info.GetComponent<TextMeshProUGUI>() : null;
        }
    }

    private bool TryGetHoveredIngredient(out IngredientDefinition definition)
    {
        definition = null;

        if (inventoryView == null || !inventoryView.TryGetLargeViewPointerWorldPosition(out Vector2 worldPosition))
            return false;

        Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition);
        InventoryItem2D bestItem = null;
        int bestSortingOrder = int.MinValue;

        foreach (Collider2D hit in hits)
        {
            InventoryItem2D item = hit.GetComponentInParent<InventoryItem2D>();
            if (item == null)
                continue;

            SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
            int sortingOrder = renderer != null ? renderer.sortingOrder : 0;
            if (bestItem != null && sortingOrder < bestSortingOrder)
                continue;

            bestItem = item;
            bestSortingOrder = sortingOrder;
        }

        if (bestItem == null)
            return false;

        definition = bestItem.Definition ?? GatheringDataLookup.GetIngredientOrFallback(bestItem.IngredientId);
        return definition != null;
    }

    private void ShowIngredientInfo(IngredientDefinition definition)
    {
        infoTapRoot.SetActive(true);
        SetIcon(CookingRuntimeVisuals2D.GetIngredientSprite(definition));

        string typeLabel = definition.categoryKey;
        if (GatheringDataLookup.TryGetIngredientCategory(definition.categoryKey, out IngredientCategoryDefinition category) &&
            !string.IsNullOrWhiteSpace(category.displayName))
            typeLabel = category.displayName;

        SetText(typeText, typeLabel);
        SetText(infoText, definition.description);
    }

    private void ShowPotPreview()
    {
        infoTapRoot.SetActive(true);

        potIngredientIds.Clear();
        foreach (InventoryItem2D item in cookingStation.ItemsOnPot)
        {
            if (item != null)
                potIngredientIds.Add(item.IngredientId);
        }

        bool hasDish = GatheringDataLookup.TryFindDishForIngredients(potIngredientIds, out DishDefinition dish);
        bool discovered = hasDish && DishDiscoveryTracker.HasDiscovered(dish.dishId);

        if (!discovered)
        {
            SetIconEnabled(false);
            SetText(typeText, UnknownLabel);
            SetText(infoText, UnknownLabel);
            return;
        }

        SetIcon(CookingRuntimeVisuals2D.GetDishSprite(dish, null));
        SetText(typeText, !string.IsNullOrWhiteSpace(dish.abilityEffect) ? dish.abilityEffect : dish.displayName);
        SetText(infoText, dish.memo);
    }

    private void SetIcon(Sprite sprite)
    {
        if (iconImage == null)
            return;

        SetIconEnabled(true);
        iconImage.sprite = sprite;
    }

    private void SetIconEnabled(bool value)
    {
        if (iconImage != null)
            iconImage.enabled = value;
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value ?? string.Empty;
    }
}
