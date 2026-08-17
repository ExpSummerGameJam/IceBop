using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class DustWipeIngredient2D : IngredientPreparationBehaviour2D
{
    [SerializeField] private SpriteRenderer ingredientRenderer;
    [SerializeField] private PreparationPointerTool2D clothTool;
    [SerializeField] private Transform dustRoot;
    [SerializeField] private int defaultDustCount = 8;
    [SerializeField] private Vector2 dustPatchSize = new(0.07f, 0.05f);
    [SerializeField] private float wipeRadius = 0.32f;
    [SerializeField] private Color dustColor = new(0.36f, 0.34f, 0.31f, 0.92f);

    private readonly List<SpriteRenderer> dustPatches = new();

    public void BindCloth(PreparationPointerTool2D newClothTool)
    {
        clothTool = newClothTool;
    }

    /// <summary>Resets this instance for reuse against a (possibly different) ingredient sprite - call after re-skinning the SpriteRenderer for a new cook. Safe even if Awake() hasn't run yet (e.g. a pooled slot activated for the first time).</summary>
    public void Rearm()
    {
        ResetPreparationState();
        ResolveRenderer();
        EnsureDustRoot();
        ClearDustPatches();
        BuildDefaultDustPatches();
    }

    private void EnsureDustRoot()
    {
        if (dustRoot == null)
        {
            Transform existing = transform.Find("DustPatches");
            if (existing != null)
                dustRoot = existing;
        }

        if (dustRoot == null)
        {
            GameObject root = new("DustPatches");
            root.transform.SetParent(transform, false);
            dustRoot = root.transform;
        }
    }

    private void ClearDustPatches()
    {
        if (dustRoot != null)
        {
            for (int i = dustRoot.childCount - 1; i >= 0; i--)
                Destroy(dustRoot.GetChild(i).gameObject);
        }

        dustPatches.Clear();
    }

    private void Awake()
    {
        SetPreparationKind(IngredientPreparationKind.DustWiping);
        ResolveRenderer();
        EnsureDustPatches();
    }

    private void Update()
    {
        if (IsPrepared || clothTool == null || !clothTool.IsDragging)
            return;

        WipeAt(clothTool.PointerWorldPosition);
    }

    private void WipeAt(Vector2 worldPosition)
    {
        bool changed = false;

        for (int i = dustPatches.Count - 1; i >= 0; i--)
        {
            SpriteRenderer dust = dustPatches[i];
            if (dust == null)
            {
                dustPatches.RemoveAt(i);
                continue;
            }

            float radius = Mathf.Max(wipeRadius, Mathf.Max(dust.bounds.extents.x, dust.bounds.extents.y));
            if (Vector2.Distance(worldPosition, dust.transform.position) > radius)
                continue;

            GameObject dustObject = dust.gameObject;
            dustPatches.RemoveAt(i);
            Destroy(dustObject);
            changed = true;
        }

        if (changed && dustPatches.Count == 0)
            TryMarkPrepared();
    }

    private void EnsureDustPatches()
    {
        EnsureDustRoot();
        CollectDustPatches();

        if (dustPatches.Count == 0)
            BuildDefaultDustPatches();
    }

    private void CollectDustPatches()
    {
        dustPatches.Clear();

        if (dustRoot == null)
            return;

        foreach (SpriteRenderer dust in dustRoot.GetComponentsInChildren<SpriteRenderer>(true))
            dustPatches.Add(dust);
    }

    private void BuildDefaultDustPatches()
    {
        Bounds bounds = ingredientRenderer != null && ingredientRenderer.sprite != null
            ? ingredientRenderer.sprite.bounds
            : new Bounds(Vector3.zero, new Vector3(3f, 2f, 0f));

        Vector2[] positions =
        {
            new(-0.34f, 0.24f), new(-0.08f, 0.32f), new(0.28f, 0.22f), new(0.42f, -0.04f),
            new(0.08f, -0.20f), new(-0.28f, -0.12f), new(-0.46f, 0.02f), new(0.20f, 0.02f)
        };

        int count = Mathf.Max(1, defaultDustCount);
        for (int i = 0; i < count; i++)
        {
            Vector2 normalized = positions[i % positions.Length];
            GameObject dust = new($"Dust_{i + 1}");
            dust.transform.SetParent(dustRoot, false);
            dust.transform.localPosition = new Vector3(
                normalized.x * bounds.size.x,
                normalized.y * bounds.size.y,
                0f);
            dust.transform.localScale = new Vector3(
                dustPatchSize.x * Random.Range(0.75f, 1.35f),
                dustPatchSize.y * Random.Range(0.75f, 1.35f),
                1f);
            dust.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-18f, 18f));

            SpriteRenderer renderer = dust.AddComponent<SpriteRenderer>();
            renderer.sprite = InventoryVisualUtility.WhiteSprite;
            renderer.color = dustColor;
            renderer.sortingLayerID = ingredientRenderer.sortingLayerID;
            renderer.sortingOrder = ingredientRenderer.sortingOrder + 2;
            dustPatches.Add(renderer);
        }
    }

    private void ResolveRenderer()
    {
        if (ingredientRenderer == null)
            ingredientRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnValidate()
    {
        defaultDustCount = Mathf.Max(1, defaultDustCount);
        dustPatchSize.x = Mathf.Max(0.01f, dustPatchSize.x);
        dustPatchSize.y = Mathf.Max(0.01f, dustPatchSize.y);
        wipeRadius = Mathf.Max(0.01f, wipeRadius);
    }
}
