using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SkewerAssemblyFinishingStage2D : MonoBehaviour, IDishFinishingStage
{
    [Serializable]
    private sealed class SkewerPiece
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public BoxCollider2D collider;
        public bool placed;
    }

    [Serializable]
    private sealed class SkewerSlot
    {
        public Transform transform;
        public SpriteRenderer guideRenderer;
        public bool occupied;
    }

    [SerializeField] private int requiredPieceCount = 4;

    [SerializeField] private float snapDistance = 0.4f;
    [SerializeField] private float pieceScaleMultiplier = 0.5f;

    [SerializeField] private float hornLength = 2.5f;
    [SerializeField] private float hornWidth = 0.16f;
    [SerializeField] private float hornRotation = -8f;

    [SerializeField] private float slotStartY = -0.68f;
    [SerializeField] private float slotEndY = 0.72f;

    [SerializeField] private float startHorizontalDistance = 1.25f;
    [SerializeField] private float startY = -1.15f;

    [SerializeField] private float guideAlpha = 0.28f;

    private readonly List<SkewerPiece> pieces = new();
    private readonly List<SkewerSlot> slots = new();

    private Camera targetCamera;
    private IReadOnlyList<CookingIngredientEntry> ingredients;
    private DishDefinition dish;

    private Transform interactionRoot;
    private Transform hornRoot;

    private SkewerPiece draggingPiece;

    private DishResultPresenter2D presenter;

    private int placedCount;
    private bool completed;
    private bool presentingResult;

    public event Action InteractionCompleted;
    public event Action ResultPresented;

    public void Configure(
        Camera camera,
        IReadOnlyList<CookingIngredientEntry> newIngredients,
        DishDefinition newDish,
        CookingMethodToolVisual2D newToolVisual)
    {
        targetCamera = camera;
        ingredients = newIngredients;
        dish = newDish;

        EnsureVisuals();
    }

    private void Update()
    {
        if (completed || presentingResult)
            return;

        if (!TryGetPointerWorldPosition(out Vector2 pointerWorldPosition))
            return;

        if (draggingPiece == null)
        {
            if (GatheringInput.WasPrimaryPressed())
                TryBeginDrag(pointerWorldPosition);

            return;
        }

        UpdateDragging(pointerWorldPosition);
    }

    private void TryBeginDrag(
        Vector2 pointerWorldPosition)
    {
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            SkewerPiece piece = pieces[i];

            if (piece == null ||
                piece.placed ||
                piece.collider == null)
            {
                continue;
            }

            if (!piece.collider.OverlapPoint(pointerWorldPosition))
                continue;

            draggingPiece = piece;

            if (piece.renderer != null)
                piece.renderer.sortingOrder = 70;

            return;
        }
    }

    private void UpdateDragging(
        Vector2 pointerWorldPosition)
    {
        if (draggingPiece == null ||
            draggingPiece.transform == null ||
            interactionRoot == null)
        {
            draggingPiece = null;
            return;
        }

        Vector3 localPosition =
            interactionRoot.InverseTransformPoint(
                new Vector3(
                    pointerWorldPosition.x,
                    pointerWorldPosition.y,
                    interactionRoot.position.z
                )
            );

        localPosition.z = 0f;

        draggingPiece.transform.localPosition =
            localPosition;

        SkewerSlot closestSlot =
            FindClosestAvailableSlot(
                pointerWorldPosition
            );

        if (closestSlot == null)
            return;

        float distance =
            Vector2.Distance(
                pointerWorldPosition,
                closestSlot.transform.position
            );

        if (distance <= snapDistance)
        {
            PlacePiece(
                draggingPiece,
                closestSlot
            );
        }
    }

    private SkewerSlot FindClosestAvailableSlot(
        Vector2 pointerWorldPosition)
    {
        SkewerSlot closest = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < slots.Count; i++)
        {
            SkewerSlot slot = slots[i];

            if (slot == null ||
                slot.occupied ||
                slot.transform == null)
            {
                continue;
            }

            float distance =
                Vector2.Distance(
                    pointerWorldPosition,
                    slot.transform.position
                );

            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closest = slot;
        }

        return closest;
    }

    private void PlacePiece(
        SkewerPiece piece,
        SkewerSlot slot)
    {
        if (piece == null ||
            slot == null ||
            piece.placed ||
            slot.occupied)
        {
            return;
        }

        piece.placed = true;
        slot.occupied = true;

        piece.transform.SetParent(
            slot.transform,
            true
        );

        piece.transform.position =
            slot.transform.position;

        piece.transform.rotation =
            slot.transform.rotation;

        piece.transform.localPosition =
            Vector3.zero;

        piece.transform.localRotation =
            Quaternion.identity;

        if (piece.collider != null)
            piece.collider.enabled = false;

        if (piece.renderer != null)
            piece.renderer.sortingOrder = 55 + placedCount;

        if (slot.guideRenderer != null)
            slot.guideRenderer.gameObject.SetActive(false);

        draggingPiece = null;

        placedCount++;

        if (placedCount >= requiredPieceCount)
            CompleteInteraction();
    }

    private void CompleteInteraction()
    {
        if (completed)
            return;

        completed = true;
        InteractionCompleted?.Invoke();
    }

    public void PlayResultPresentation()
    {
        if (presentingResult)
            return;

        presentingResult = true;

        if (presenter == null)
        {
            presenter =
                gameObject.AddComponent<DishResultPresenter2D>();

            presenter.Presented +=
                OnResultPresented;
        }

        presenter.Play(
            targetCamera,
            dish,
            ingredients,
            HideInteractiveVisuals
        );
    }

    private void OnResultPresented()
    {
        ResultPresented?.Invoke();
    }

    private void HideInteractiveVisuals()
    {
        if (interactionRoot != null)
            interactionRoot.gameObject.SetActive(false);
    }

    private void EnsureVisuals()
    {
        if (interactionRoot != null)
            return;

        GameObject root =
            new("SkewerAssemblyRoot");

        root.transform.SetParent(
            transform,
            false
        );

        interactionRoot =
            root.transform;

        CreateHorn();

        CookingIngredientEntry meatIngredient =
            FindMeatIngredient();

        CreateSlots(meatIngredient);
        CreatePieces(meatIngredient);
    }

    private void CreateHorn()
    {
        GameObject hornObject =
            new("UnicornHorn");

        hornObject.transform.SetParent(
            interactionRoot,
            false
        );

        hornObject.transform.localPosition =
            new Vector3(
                0f,
                0.05f,
                0f
            );

        hornObject.transform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                hornRotation
            );

        hornRoot =
            hornObject.transform;

        CookingRuntimeVisuals2D.CreateBlock(
            "UnicornHornBody",
            hornRoot,
            Vector3.zero,
            new Vector3(
                hornWidth,
                hornLength,
                1f
            ),
            new Color(
                0.92f,
                0.85f,
                0.98f,
                1f
            ),
            40
        );

        CookingRuntimeVisuals2D.CreateBlock(
            "UnicornHornCore",
            hornRoot,
            Vector3.zero,
            new Vector3(
                hornWidth * 0.42f,
                hornLength * 0.94f,
                1f
            ),
            new Color(
                0.72f,
                0.56f,
                0.92f,
                1f
            ),
            41
        );
    }

    private void CreateSlots(
        CookingIngredientEntry meatIngredient)
    {
        Sprite sprite =
            GetPieceSprite(meatIngredient);

        Color color =
            GetPieceColor(meatIngredient);

        Vector3 scale =
            GetPieceScale(meatIngredient);

        int count =
            Mathf.Max(
                1,
                requiredPieceCount
            );

        for (int i = 0; i < count; i++)
        {
            float t =
                count <= 1
                    ? 0.5f
                    : i / (float)(count - 1);

            float y =
                Mathf.Lerp(
                    slotStartY,
                    slotEndY,
                    t
                );

            GameObject slotObject =
                new($"SkewerSlot_{i + 1}");

            slotObject.transform.SetParent(
                hornRoot,
                false
            );

            slotObject.transform.localPosition =
                new Vector3(
                    0f,
                    y,
                    0f
                );

            GameObject guideObject =
                new($"SkewerGuide_{i + 1}");

            guideObject.transform.SetParent(
                slotObject.transform,
                false
            );

            guideObject.transform.localPosition =
                Vector3.zero;

            guideObject.transform.localScale =
                scale;

            SpriteRenderer guideRenderer =
                guideObject.AddComponent<SpriteRenderer>();

            guideRenderer.sprite =
                sprite;

            guideRenderer.color =
                new Color(
                    color.r,
                    color.g,
                    color.b,
                    guideAlpha
                );

            guideRenderer.sortingOrder =
                44;

            slots.Add(
                new SkewerSlot
                {
                    transform =
                        slotObject.transform,

                    guideRenderer =
                        guideRenderer,

                    occupied =
                        false
                }
            );
        }
    }

    private void CreatePieces(
        CookingIngredientEntry meatIngredient)
    {
        Sprite sprite =
            GetPieceSprite(meatIngredient);

        Color color =
            GetPieceColor(meatIngredient);

        Vector3 scale =
            GetPieceScale(meatIngredient);

        int count =
            Mathf.Max(
                1,
                requiredPieceCount
            );

        for (int i = 0; i < count; i++)
        {
            GameObject pieceObject =
                new($"DinosaurMeat_{i + 1}");

            pieceObject.transform.SetParent(
                interactionRoot,
                false
            );

            pieceObject.transform.localPosition =
                CalculatePieceStartPosition(
                    i,
                    count
                );

            pieceObject.transform.localScale =
                scale;

            SpriteRenderer renderer =
                pieceObject.AddComponent<SpriteRenderer>();

            renderer.sprite =
                sprite;

            renderer.color =
                color;

            renderer.sortingOrder =
                50 + i;

            BoxCollider2D collider =
                pieceObject.AddComponent<BoxCollider2D>();

            collider.isTrigger =
                true;

            if (sprite != null)
            {
                collider.size =
                    sprite.bounds.size;

                collider.offset =
                    sprite.bounds.center;
            }

            pieces.Add(
                new SkewerPiece
                {
                    transform =
                        pieceObject.transform,

                    renderer =
                        renderer,

                    collider =
                        collider,

                    placed =
                        false
                }
            );
        }
    }

    private CookingIngredientEntry FindMeatIngredient()
    {
        if (ingredients == null)
            return null;

        for (int i = 0; i < ingredients.Count; i++)
        {
            if (ingredients[i] != null)
                return ingredients[i];
        }

        return null;
    }

    private Sprite GetPieceSprite(
        CookingIngredientEntry entry)
    {
        if (entry == null)
            return InventoryVisualUtility.WhiteSprite;

        Sprite sprite =
            CookingRuntimeVisuals2D.GetIngredientSprite(entry);

        return sprite != null
            ? sprite
            : InventoryVisualUtility.WhiteSprite;
    }

    private Color GetPieceColor(
        CookingIngredientEntry entry)
    {
        if (entry == null)
        {
            return new Color(
                0.72f,
                0.22f,
                0.18f,
                1f
            );
        }

        return CookingRuntimeVisuals2D
            .GetIngredientColor(entry);
    }

    private Vector3 GetPieceScale(
        CookingIngredientEntry entry)
    {
        Vector3 sourceScale =
            entry != null &&
            entry.ingredientScale != Vector3.zero
                ? entry.ingredientScale
                : Vector3.one;

        return sourceScale *
               Mathf.Max(
                   0.01f,
                   pieceScaleMultiplier
               );
    }

    private Vector3 CalculatePieceStartPosition(
        int index,
        int count)
    {
        if (count <= 1)
        {
            return new Vector3(
                0f,
                startY,
                0f
            );
        }

        float t =
            index /
            (float)(count - 1);

        float x =
            Mathf.Lerp(
                -startHorizontalDistance,
                startHorizontalDistance,
                t
            );

        float y =
            startY -
            (index % 2) * 0.18f;

        return new Vector3(
            x,
            y,
            0f
        );
    }

    private bool TryGetPointerWorldPosition(
        out Vector2 worldPosition)
    {
        worldPosition = default;

        Camera camera =
            targetCamera != null
                ? targetCamera
                : Camera.main;

        if (camera == null)
            return false;

        Vector2 screenPosition =
            GatheringInput.PointerScreenPosition();

        float zDistance =
            Mathf.Abs(
                camera.transform.position.z -
                transform.position.z
            );

        Vector3 result =
            camera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    zDistance
                )
            );

        worldPosition =
            new Vector2(
                result.x,
                result.y
            );

        return true;
    }

    private void OnValidate()
    {
        requiredPieceCount =
            Mathf.Max(
                1,
                requiredPieceCount
            );

        snapDistance =
            Mathf.Max(
                0.05f,
                snapDistance
            );

        pieceScaleMultiplier =
            Mathf.Max(
                0.01f,
                pieceScaleMultiplier
            );

        hornLength =
            Mathf.Max(
                0.2f,
                hornLength
            );

        hornWidth =
            Mathf.Max(
                0.02f,
                hornWidth
            );

        startHorizontalDistance =
            Mathf.Max(
                0.1f,
                startHorizontalDistance
            );

        guideAlpha =
            Mathf.Clamp01(
                guideAlpha
            );
    }
}