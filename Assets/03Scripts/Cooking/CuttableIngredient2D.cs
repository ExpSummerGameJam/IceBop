using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class CuttableIngredient2D : IngredientPreparationBehaviour2D
{
    [Serializable]
    public sealed class CutLine
    {
        public Vector2 localStart;
        public Vector2 localEnd;

        [NonSerialized] public bool Completed;
        [NonSerialized] public bool HasProgress;
        [NonSerialized] public bool Sliced;
        [NonSerialized] public float MinProgress;
        [NonSerialized] public float MaxProgress;

        public float Coverage => HasProgress ? MaxProgress - MinProgress : 0f;

        public void ResetProgress()
        {
            Completed = false;
            HasProgress = false;
            Sliced = false;
            MinProgress = 0f;
            MaxProgress = 0f;
        }
    }

    [SerializeField] private SpriteRenderer ingredientRenderer;
    [SerializeField] private PreparationKnifeTool2D knife;
    [SerializeField] private CutLine[] cutLines = Array.Empty<CutLine>();
    [SerializeField] private Transform guideRoot;
    [SerializeField] private SpriteRenderer remainingRenderer;
    [SerializeField] private int defaultCutLineCount = 1;
    [SerializeField] private float defaultCutLineInset = 0.12f;
    [SerializeField] private int dotsPerLine = 12;
    [SerializeField] private Vector2 dotSize = new(0.075f, 0.075f);
    [SerializeField] private float cutTolerance = 0.18f;
    [SerializeField] private float requiredCoverage = 0.82f;
    [SerializeField] private float lineFollowDotThreshold = 0.55f;
    [SerializeField] private float axisAlignmentTolerance = 0.04f;
    [SerializeField] private Color pendingLineColor = new(1f, 1f, 1f, 0.85f);
    [SerializeField] private Color filledLineColor = new(0.1f, 0.95f, 0.55f, 1f);
    [SerializeField] private bool hideGuidesWhenCompleted = true;
    [SerializeField] private float cutPieceGravityScale = 0.8f;
    [SerializeField] private float cutPieceImpulse = 0.5f;
    [SerializeField] private float cutPieceTorque = 0.6f;

    private readonly List<List<SpriteRenderer>> guideDots = new();
    private readonly List<GameObject> generatedPieces = new();
    private Sprite sourceSprite;
    private Bounds sourceBounds;
    private bool segmentInitialized;
    private bool verticalCuts = true;
    private float mainSegmentMin;
    private float mainSegmentMax;
    private int cutPieceIndex;
    private PolygonCollider2D rootCollider;

    public IReadOnlyList<CutLine> CutLines => cutLines;

    public override Sprite ProcessedSprite =>
        remainingRenderer != null && remainingRenderer.enabled && remainingRenderer.sprite != null
            ? remainingRenderer.sprite
            : sourceSprite;

    public void BindKnife(PreparationKnifeTool2D newKnife)
    {
        knife = newKnife;
    }

    public void RebuildGuides()
    {
        // Drop the cached collider first: fitting the slot's collider to a new sprite can
        // replace the component outright, and in play mode the old one doesn't read as null
        // until end of frame - so without this, ResolveComponents would keep the doomed
        // collider and every cut-progress reshape this cook would go nowhere.
        rootCollider = null;
        ResolveComponents();

        // Force the default cut lines to be recomputed from whatever sprite is currently on
        // ingredientRenderer - this instance may be a pooled slot being reused for a different
        // ingredient than last time, so the previous cook's cut-line geometry can't be kept.
        cutLines = Array.Empty<CutLine>();
        EnsureDefaultCutLines();
        InitializeSourceSprite();
        ResetCutLineProgress();
        ResetMainSegment();
        ClearGeneratedPieces();
        DestroyRemainingRenderer();
        BuildGuideVisuals();
        UpdateAllGuideColors();
        ResetPreparationState();

        if (ingredientRenderer != null)
        {
            ingredientRenderer.sprite = sourceSprite;
            ingredientRenderer.enabled = true;
        }
    }

    private void Awake()
    {
        SetPreparationKind(IngredientPreparationKind.Cutting);
        ResolveComponents();
        EnsureDefaultCutLines();
        InitializeSourceSprite();
        ResetCutLineProgress();
        ResetMainSegment();
        BuildGuideVisuals();
        UpdateAllGuideColors();
    }

    // Cut-off debris pieces are parented to this slot's PARENT (IngredientSlots), not to this
    // slot itself, so CookingFlowController2D deactivating just this slot's GameObject at the
    // end of the prep stage doesn't touch them - left uncleaned, they'd keep sitting around
    // (and colliding with) whatever the finishing stage spawns next. Clear them here instead,
    // since OnDisable reliably fires when the slot is SetActive(false).
    private void OnDisable()
    {
        ClearGeneratedPieces();
    }

    private void Update()
    {
        if (IsPrepared)
            return;

        PreparationKnifeTool2D activeKnife = knife != null ? knife : PreparationKnifeTool2D.CurrentDraggingKnife;
        if (activeKnife == null || !activeKnife.HasPointerMovement)
            return;

        ApplyPointerMovement(activeKnife, activeKnife.PreviousPointerWorldPosition, activeKnife.PointerWorldPosition);
    }

    private void ApplyPointerMovement(PreparationKnifeTool2D activeKnife, Vector2 previousPointerPosition, Vector2 currentPointerPosition)
    {
        bool changed = false;
        List<CutLine> completedLines = null;

        foreach (CutLine line in cutLines)
        {
            if (line == null || line.Completed)
                continue;

            if (TryAdvanceLine(line, previousPointerPosition, currentPointerPosition, out bool completedNow))
            {
                changed = true;

                if (completedNow)
                {
                    completedLines ??= new List<CutLine>();
                    completedLines.Add(line);
                }
            }
        }

        if (!changed)
            return;

        activeKnife.SetCutting(true);
        UpdateAllGuideColors();

        if (completedLines != null)
        {
            foreach (CutLine completedLine in completedLines)
                SliceCompletedLine(completedLine);
        }

        if (AreAllLinesCompleted())
            CompleteCutting();
    }

    private bool TryAdvanceLine(
        CutLine line,
        Vector2 previousPointerPosition,
        Vector2 currentPointerPosition,
        out bool completedNow)
    {
        completedNow = false;

        Vector2 worldStart = transform.TransformPoint(line.localStart);
        Vector2 worldEnd = transform.TransformPoint(line.localEnd);
        Vector2 lineVector = worldEnd - worldStart;
        float lineLength = lineVector.magnitude;
        if (lineLength <= 0.001f)
            return false;

        Vector2 pointerMovement = currentPointerPosition - previousPointerPosition;
        if (pointerMovement.sqrMagnitude <= 0.000001f)
            return false;

        float lineFollow = Mathf.Abs(Vector2.Dot(pointerMovement.normalized, lineVector / lineLength));
        if (lineFollow < lineFollowDotThreshold)
            return false;

        float previousDistance = DistancePointToSegment(previousPointerPosition, worldStart, worldEnd, out float previousT);
        float currentDistance = DistancePointToSegment(currentPointerPosition, worldStart, worldEnd, out float currentT);
        bool previousNear = previousDistance <= cutTolerance;
        bool currentNear = currentDistance <= cutTolerance;
        if (!previousNear && !currentNear)
            return false;

        float coveragePadding = Mathf.Clamp01(cutTolerance / lineLength);
        if (previousNear && currentNear)
        {
            AddCutInterval(
                line,
                Mathf.Min(previousT, currentT) - coveragePadding,
                Mathf.Max(previousT, currentT) + coveragePadding);
        }
        else
        {
            float t = currentNear ? currentT : previousT;
            AddCutInterval(line, t - coveragePadding, t + coveragePadding);
        }

        if (!line.Completed && line.Coverage >= requiredCoverage)
        {
            line.Completed = true;
            completedNow = true;
        }

        return true;
    }

    private static void AddCutInterval(CutLine line, float minT, float maxT)
    {
        minT = Mathf.Clamp01(minT);
        maxT = Mathf.Clamp01(maxT);

        if (!line.HasProgress)
        {
            line.HasProgress = true;
            line.MinProgress = minT;
            line.MaxProgress = maxT;
            return;
        }

        line.MinProgress = Mathf.Min(line.MinProgress, minT);
        line.MaxProgress = Mathf.Max(line.MaxProgress, maxT);
    }

    private void SliceCompletedLine(CutLine line)
    {
        if (line == null || line.Sliced)
            return;

        line.Sliced = true;

        if (sourceSprite == null || !TryGetAxisCut(line, out bool lineIsVertical, out float cutCoordinate))
            return;

        if (!segmentInitialized)
            ResetMainSegment();

        if (lineIsVertical != verticalCuts)
            return;

        if (cutCoordinate <= mainSegmentMin + axisAlignmentTolerance ||
            cutCoordinate >= mainSegmentMax - axisAlignmentTolerance)
            return;

        bool keepLower = ShouldKeepLowerSegment(cutCoordinate);
        float droppedMin = keepLower ? cutCoordinate : mainSegmentMin;
        float droppedMax = keepLower ? mainSegmentMax : cutCoordinate;

        if (droppedMax - droppedMin > axisAlignmentTolerance)
            CreatePhysicsCutPiece(droppedMin, droppedMax);

        if (keepLower)
            mainSegmentMax = cutCoordinate;
        else
            mainSegmentMin = cutCoordinate;

        UpdateRemainingMainVisual();
    }

    private bool ShouldKeepLowerSegment(float cutCoordinate)
    {
        float anchor = 0f;
        if (anchor < mainSegmentMin || anchor > mainSegmentMax)
        {
            float lowerLength = cutCoordinate - mainSegmentMin;
            float upperLength = mainSegmentMax - cutCoordinate;
            return lowerLength >= upperLength;
        }

        if (anchor < cutCoordinate - axisAlignmentTolerance)
            return true;

        if (anchor > cutCoordinate + axisAlignmentTolerance)
            return false;

        return cutCoordinate - mainSegmentMin >= mainSegmentMax - cutCoordinate;
    }

    private void CompleteCutting()
    {
        if (!TryMarkPrepared())
            return;

        if (hideGuidesWhenCompleted && guideRoot != null)
            guideRoot.gameObject.SetActive(false);

        ApplyPhysicsToRemainingPiece();
    }

    // Once every cut is done, the leftover center piece should scatter/tumble like the
    // already-sliced-off debris pieces do, instead of sitting perfectly still.
    private void ApplyPhysicsToRemainingPiece()
    {
        SpriteRenderer target = remainingRenderer != null && remainingRenderer.enabled
            ? remainingRenderer
            : ingredientRenderer;

        if (target == null || target.sprite == null)
            return;

        GameObject pieceObject = target.gameObject;

        PolygonCollider2D pieceCollider = CookingRuntimeVisuals2D.ApplySpriteFittedCollider(pieceObject, target.sprite);

        Rigidbody2D rigidbody = pieceObject.GetComponent<Rigidbody2D>();
        if (rigidbody == null)
            rigidbody = pieceObject.AddComponent<Rigidbody2D>();

        rigidbody.bodyType = RigidbodyType2D.Dynamic;
        rigidbody.gravityScale = cutPieceGravityScale;
        rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rigidbody.linearDamping = 0.6f;
        rigidbody.angularDamping = 0.5f;

        Vector2 impulseDirection = new(UnityEngine.Random.Range(-0.4f, 0.4f), 0.5f);
        rigidbody.AddForce(impulseDirection.normalized * cutPieceImpulse, ForceMode2D.Impulse);
        rigidbody.AddTorque(UnityEngine.Random.Range(-cutPieceTorque, cutPieceTorque), ForceMode2D.Impulse);

        IgnoreCollisionsWithKnifeAndPieces(pieceCollider);
    }

    // Cut-off pieces (and the final remaining piece) shouldn't physically bump into each
    // other or the knife - they're purely a visual "scatter" effect, not a puzzle about
    // pieces jostling each other, so every solid piece collider ignores every other one.
    private void IgnoreCollisionsWithKnifeAndPieces(Collider2D newCollider)
    {
        if (newCollider == null)
            return;

        if (knife != null)
        {
            Collider2D knifeCollider = knife.GetComponent<Collider2D>();
            if (knifeCollider != null)
                Physics2D.IgnoreCollision(newCollider, knifeCollider, true);
        }

        foreach (GameObject piece in generatedPieces)
        {
            if (piece == null)
                continue;

            Collider2D pieceCollider = piece.GetComponent<Collider2D>();
            if (pieceCollider != null && pieceCollider != newCollider)
                Physics2D.IgnoreCollision(newCollider, pieceCollider, true);
        }

        if (remainingRenderer != null)
        {
            Collider2D remainingCollider = remainingRenderer.GetComponent<Collider2D>();
            if (remainingCollider != null && remainingCollider != newCollider)
                Physics2D.IgnoreCollision(newCollider, remainingCollider, true);
        }
    }

    private void CreatePhysicsCutPiece(float pieceMin, float pieceMax)
    {
        Sprite pieceSprite = CreateSegmentSprite(pieceMin, pieceMax, false);
        if (pieceSprite == null)
            return;

        Vector3 localCenter = GetSegmentLocalCenter(pieceMin, pieceMax);
        GameObject piece = new($"CutPiece_{++cutPieceIndex}");
        piece.transform.SetParent(transform.parent, true);
        piece.transform.position = transform.TransformPoint(localCenter);
        piece.transform.rotation = transform.rotation;
        piece.transform.localScale = transform.lossyScale;

        SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
        renderer.sprite = pieceSprite;
        renderer.color = ingredientRenderer.color;
        renderer.sortingLayerID = ingredientRenderer.sortingLayerID;
        renderer.sortingOrder = ingredientRenderer.sortingOrder + 1;

        PolygonCollider2D pieceCollider = CookingRuntimeVisuals2D.ApplySpriteFittedCollider(piece, pieceSprite);

        Rigidbody2D rigidbody = piece.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Dynamic;
        rigidbody.gravityScale = cutPieceGravityScale;
        rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rigidbody.linearDamping = 0.6f;
        rigidbody.angularDamping = 0.5f;

        Vector2 impulseDirection = verticalCuts
            ? new Vector2(Mathf.Sign(localCenter.x), 0.45f).normalized
            : new Vector2(0.35f, Mathf.Sign(localCenter.y)).normalized;

        if (impulseDirection.sqrMagnitude <= 0.0001f)
            impulseDirection = verticalCuts ? Vector2.right : Vector2.up;

        rigidbody.AddForce(impulseDirection * cutPieceImpulse, ForceMode2D.Impulse);
        rigidbody.AddTorque(UnityEngine.Random.Range(-cutPieceTorque, cutPieceTorque), ForceMode2D.Impulse);

        IgnoreCollisionsWithKnifeAndPieces(pieceCollider);
        generatedPieces.Add(piece);
    }

    private void UpdateRemainingMainVisual()
    {
        if (sourceSprite == null || mainSegmentMax - mainSegmentMin <= axisAlignmentTolerance)
        {
            if (ingredientRenderer != null)
                ingredientRenderer.enabled = false;

            if (remainingRenderer != null)
                remainingRenderer.enabled = false;

            return;
        }

        Sprite remainingSprite = CreateSegmentSprite(mainSegmentMin, mainSegmentMax, false);
        if (remainingSprite == null)
            return;

        EnsureRemainingRenderer();
        remainingRenderer.sprite = remainingSprite;
        remainingRenderer.color = ingredientRenderer.color;
        remainingRenderer.sortingLayerID = ingredientRenderer.sortingLayerID;
        remainingRenderer.sortingOrder = ingredientRenderer.sortingOrder;
        remainingRenderer.transform.localPosition = GetSegmentLocalCenter(mainSegmentMin, mainSegmentMax);
        remainingRenderer.transform.localRotation = Quaternion.identity;
        remainingRenderer.transform.localScale = Vector3.one;
        remainingRenderer.enabled = true;

        if (ingredientRenderer != null)
            ingredientRenderer.enabled = false;

        UpdateRootCollider();
    }

    private void EnsureRemainingRenderer()
    {
        if (remainingRenderer != null)
            return;

        Transform existing = transform.Find("RemainingIngredient");
        if (existing != null)
            remainingRenderer = existing.GetComponent<SpriteRenderer>();

        if (remainingRenderer != null)
            return;

        GameObject visual = new("RemainingIngredient");
        visual.transform.SetParent(transform, false);
        remainingRenderer = visual.AddComponent<SpriteRenderer>();
    }

    private Sprite CreateSegmentSprite(float segmentMin, float segmentMax, bool anchoredToMain)
    {
        if (sourceSprite == null)
            return null;

        Rect sourceRect = sourceSprite.rect;
        float pixelsPerUnit = sourceSprite.pixelsPerUnit;

        // Derived from the sprite's own current rect/bounds rather than trusting the
        // import-time pixelsPerUnit: atlas packing can rescale the packed pixel footprint
        // (padding, downscaling), so the pixels-per-unit ratio must be recomputed from the
        // sprite's actual current rect to avoid cropping the wrong region of the atlas page.
        float xPixelsPerUnit = sourceRect.width / Mathf.Max(0.0001f, sourceBounds.size.x);
        float yPixelsPerUnit = sourceRect.height / Mathf.Max(0.0001f, sourceBounds.size.y);

        if (verticalCuts)
        {
            float xMin = sourceRect.x + Mathf.Round((segmentMin - sourceBounds.min.x) * xPixelsPerUnit);
            float xMax = sourceRect.x + Mathf.Round((segmentMax - sourceBounds.min.x) * xPixelsPerUnit);
            xMin = Mathf.Clamp(xMin, sourceRect.x, sourceRect.xMax - 1f);
            xMax = Mathf.Clamp(xMax, xMin + 1f, sourceRect.xMax);
            Vector2 pivot = anchoredToMain
                ? new Vector2(-segmentMin / Mathf.Max(0.001f, segmentMax - segmentMin), -sourceBounds.min.y / Mathf.Max(0.001f, sourceBounds.size.y))
                : new Vector2(0.5f, 0.5f);
            return Sprite.Create(sourceSprite.texture, new Rect(xMin, sourceRect.y, xMax - xMin, sourceRect.height),
                pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
        }

        float yMin = sourceRect.y + Mathf.Round((segmentMin - sourceBounds.min.y) * yPixelsPerUnit);
        float yMax = sourceRect.y + Mathf.Round((segmentMax - sourceBounds.min.y) * yPixelsPerUnit);
        yMin = Mathf.Clamp(yMin, sourceRect.y, sourceRect.yMax - 1f);
        yMax = Mathf.Clamp(yMax, yMin + 1f, sourceRect.yMax);
        Vector2 horizontalPivot = anchoredToMain
            ? new Vector2(-sourceBounds.min.x / Mathf.Max(0.001f, sourceBounds.size.x), -segmentMin / Mathf.Max(0.001f, segmentMax - segmentMin))
            : new Vector2(0.5f, 0.5f);
        return Sprite.Create(sourceSprite.texture, new Rect(sourceRect.x, yMin, sourceRect.width, yMax - yMin),
            horizontalPivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
    }

    private Vector3 GetSegmentLocalCenter(float segmentMin, float segmentMax)
    {
        return verticalCuts
            ? new Vector3((segmentMin + segmentMax) * 0.5f, sourceBounds.center.y, 0f)
            : new Vector3(sourceBounds.center.x, (segmentMin + segmentMax) * 0.5f, 0f);
    }

    private bool TryGetAxisCut(CutLine line, out bool lineIsVertical, out float cutCoordinate)
    {
        lineIsVertical = true;
        cutCoordinate = 0f;

        Vector2 delta = line.localEnd - line.localStart;
        if (delta.sqrMagnitude <= 0.0001f)
            return false;

        bool isVertical = Mathf.Abs(delta.x) <= axisAlignmentTolerance;
        bool isHorizontal = Mathf.Abs(delta.y) <= axisAlignmentTolerance;
        if (!isVertical && !isHorizontal)
            return false;

        lineIsVertical = isVertical;
        cutCoordinate = isVertical
            ? (line.localStart.x + line.localEnd.x) * 0.5f
            : (line.localStart.y + line.localEnd.y) * 0.5f;
        return true;
    }

    private void ResetMainSegment()
    {
        if (sourceSprite == null)
        {
            segmentInitialized = false;
            return;
        }

        verticalCuts = DetermineCutAxis();
        mainSegmentMin = verticalCuts ? sourceBounds.min.x : sourceBounds.min.y;
        mainSegmentMax = verticalCuts ? sourceBounds.max.x : sourceBounds.max.y;
        segmentInitialized = true;
        UpdateRootCollider();
    }

    private bool DetermineCutAxis()
    {
        foreach (CutLine line in cutLines)
        {
            if (line != null && TryGetAxisCut(line, out bool lineIsVertical, out _))
                return lineIsVertical;
        }

        return true;
    }

    private void UpdateRootCollider()
    {
        if (rootCollider == null || sourceSprite == null)
            return;

        Vector3 localCenter = GetSegmentLocalCenter(mainSegmentMin, mainSegmentMax);
        Vector2 size = verticalCuts
            ? new Vector2(Mathf.Max(0.01f, mainSegmentMax - mainSegmentMin), sourceBounds.size.y)
            : new Vector2(sourceBounds.size.x, Mathf.Max(0.01f, mainSegmentMax - mainSegmentMin));

        // The remaining segment shrinks as it's cut, so this stays a plain rectangle around
        // the current remaining range rather than the sprite's authored physics shape (which
        // only describes the original, uncut ingredient).
        Vector2 half = size * 0.5f;
        Vector2 center = localCenter;
        rootCollider.pathCount = 1;
        rootCollider.SetPath(0, new[]
        {
            new Vector2(center.x - half.x, center.y - half.y),
            new Vector2(center.x + half.x, center.y - half.y),
            new Vector2(center.x + half.x, center.y + half.y),
            new Vector2(center.x - half.x, center.y + half.y),
        });
    }

    private bool AreAllLinesCompleted()
    {
        if (cutLines == null || cutLines.Length == 0)
            return false;

        foreach (CutLine line in cutLines)
        {
            if (line == null || !line.Completed)
                return false;
        }

        return true;
    }

    private void BuildGuideVisuals()
    {
        if (guideRoot == null)
        {
            Transform existing = transform.Find("CutGuides");
            if (existing != null)
                guideRoot = existing;
        }

        if (guideRoot == null)
        {
            GameObject root = new("CutGuides");
            root.transform.SetParent(transform, false);
            guideRoot = root.transform;
        }

        guideRoot.gameObject.SetActive(true);
        ClearGuideVisuals();
        guideDots.Clear();

        for (int lineIndex = 0; lineIndex < cutLines.Length; lineIndex++)
        {
            CutLine line = cutLines[lineIndex];
            List<SpriteRenderer> dots = new();
            guideDots.Add(dots);

            float lineLength = Vector2.Distance(line.localStart, line.localEnd);
            int count = Mathf.Clamp(Mathf.CeilToInt(lineLength / 0.18f) + 1, 2, Mathf.Max(2, dotsPerLine));
            float spacing = count > 1 ? lineLength / (count - 1) : lineLength;
            Vector2 safeDotSize = new(
                Mathf.Min(dotSize.x, spacing * 0.42f),
                Mathf.Min(dotSize.y, spacing * 0.42f));

            for (int dotIndex = 0; dotIndex < count; dotIndex++)
            {
                float t = dotIndex / (float)(count - 1);
                GameObject dot = new($"CutDot_{lineIndex + 1}_{dotIndex + 1}");
                dot.transform.SetParent(guideRoot, false);
                dot.transform.localPosition = Vector2.Lerp(line.localStart, line.localEnd, t);
                dot.transform.localScale = new Vector3(safeDotSize.x, safeDotSize.y, 1f);

                SpriteRenderer renderer = dot.AddComponent<SpriteRenderer>();
                renderer.sprite = InventoryVisualUtility.WhiteSprite;
                renderer.color = pendingLineColor;
                renderer.sortingLayerID = ingredientRenderer.sortingLayerID;
                renderer.sortingOrder = ingredientRenderer.sortingOrder + 2;
                dots.Add(renderer);
            }
        }
    }

    private void ClearGuideVisuals()
    {
        if (guideRoot == null)
            return;

        for (int i = guideRoot.childCount - 1; i >= 0; i--)
            DestroyChild(guideRoot.GetChild(i).gameObject);
    }

    private void UpdateAllGuideColors()
    {
        for (int lineIndex = 0; lineIndex < guideDots.Count; lineIndex++)
        {
            CutLine line = lineIndex < cutLines.Length ? cutLines[lineIndex] : null;
            List<SpriteRenderer> dots = guideDots[lineIndex];

            for (int dotIndex = 0; dotIndex < dots.Count; dotIndex++)
            {
                SpriteRenderer dot = dots[dotIndex];
                if (dot == null)
                    continue;

                float t = dots.Count <= 1 ? 0.5f : dotIndex / (float)(dots.Count - 1);
                bool filled = line != null && (line.Completed ||
                                               line.HasProgress && t >= line.MinProgress && t <= line.MaxProgress);
                dot.color = filled ? filledLineColor : pendingLineColor;
            }
        }
    }

    private void EnsureDefaultCutLines()
    {
        if (cutLines != null && cutLines.Length > 0)
            return;

        Bounds bounds = ingredientRenderer != null && ingredientRenderer.sprite != null
            ? ingredientRenderer.sprite.bounds
            : new Bounds(Vector3.zero, new Vector3(4f, 2f, 0f));

        int lineCount = Mathf.Max(1, defaultCutLineCount);
        cutLines = new CutLine[lineCount];
        for (int i = 0; i < cutLines.Length; i++)
        {
            float x = Mathf.Lerp(bounds.min.x, bounds.max.x, (i + 1f) / (cutLines.Length + 1f));
            float inset = Mathf.Min(defaultCutLineInset, bounds.size.y * 0.18f);
            cutLines[i] = new CutLine
            {
                localStart = new Vector2(x, bounds.min.y + inset),
                localEnd = new Vector2(x, bounds.max.y - inset)
            };
        }
    }

    private void ResetCutLineProgress()
    {
        if (cutLines == null)
            return;

        foreach (CutLine line in cutLines)
            line?.ResetProgress();
    }

    private void InitializeSourceSprite()
    {
        if (ingredientRenderer == null)
            return;

        sourceSprite = ingredientRenderer.sprite;
        if (sourceSprite != null)
            sourceBounds = sourceSprite.bounds;
    }

    private void ResolveComponents()
    {
        if (ingredientRenderer == null)
            ingredientRenderer = GetComponent<SpriteRenderer>();

        if (rootCollider == null)
            rootCollider = GetComponent<PolygonCollider2D>();
    }

    private void ClearGeneratedPieces()
    {
        foreach (GameObject piece in generatedPieces)
        {
            if (piece != null)
                DestroyChild(piece);
        }

        generatedPieces.Clear();
        cutPieceIndex = 0;
    }

    private void DestroyRemainingRenderer()
    {
        if (remainingRenderer == null)
            return;

        DestroyChild(remainingRenderer.gameObject);
        remainingRenderer = null;
    }

    private static void DestroyChild(GameObject child)
    {
        if (child == null)
            return;

        if (Application.isPlaying)
            Destroy(child);
        else
            DestroyImmediate(child);
    }

    private static float DistancePointToSegment(Vector2 point, Vector2 start, Vector2 end, out float t)
    {
        Vector2 segment = end - start;
        float denominator = Vector2.Dot(segment, segment);
        if (denominator <= 0.000001f)
        {
            t = 0f;
            return Vector2.Distance(point, start);
        }

        t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / denominator);
        return Vector2.Distance(point, start + segment * t);
    }

    private void OnValidate()
    {
        dotsPerLine = Mathf.Max(2, dotsPerLine);
        defaultCutLineCount = Mathf.Max(1, defaultCutLineCount);
        defaultCutLineInset = Mathf.Max(0f, defaultCutLineInset);
        dotSize.x = Mathf.Max(0.01f, dotSize.x);
        dotSize.y = Mathf.Max(0.01f, dotSize.y);
        cutTolerance = Mathf.Max(0.01f, cutTolerance);
        requiredCoverage = Mathf.Clamp01(requiredCoverage);
        lineFollowDotThreshold = Mathf.Clamp01(lineFollowDotThreshold);
        axisAlignmentTolerance = Mathf.Max(0.001f, axisAlignmentTolerance);
        cutPieceGravityScale = Mathf.Max(0f, cutPieceGravityScale);
        cutPieceImpulse = Mathf.Max(0f, cutPieceImpulse);
        cutPieceTorque = Mathf.Max(0f, cutPieceTorque);
    }
}
