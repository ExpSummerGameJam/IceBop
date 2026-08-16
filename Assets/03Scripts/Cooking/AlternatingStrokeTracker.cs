using UnityEngine;

/// <summary>
/// Counts completed back-and-forth strokes along one axis (e.g. shaking left-right or
/// up-down). Shared by the ladle-stir, basket-shake, and fryer-shake finishing stages.
/// </summary>
public sealed class AlternatingStrokeTracker
{
    private readonly int requiredStrokes;
    private readonly float minStrokeDistance;
    private readonly float axisNoiseRatio;
    private readonly bool horizontalAxis;

    private int currentStrokeDirection;
    private int lastCompletedStrokeDirection;
    private float currentStrokeDistance;
    private int completedStrokes;

    public AlternatingStrokeTracker(int requiredStrokes, float minStrokeDistance, float axisNoiseRatio, bool horizontalAxis)
    {
        this.requiredStrokes = Mathf.Max(1, requiredStrokes);
        this.minStrokeDistance = Mathf.Max(0.01f, minStrokeDistance);
        this.axisNoiseRatio = Mathf.Max(0.1f, axisNoiseRatio);
        this.horizontalAxis = horizontalAxis;
    }

    public int RequiredStrokes => requiredStrokes;
    public int CompletedStrokes => completedStrokes;
    public float Progress => Mathf.Clamp01((float)completedStrokes / requiredStrokes);
    public bool IsComplete => completedStrokes >= requiredStrokes;

    /// <returns>true if this movement completed a new stroke.</returns>
    public bool ApplyMovement(Vector2 delta)
    {
        float primary = horizontalAxis ? delta.x : delta.y;
        float secondary = horizontalAxis ? delta.y : delta.x;

        if (Mathf.Abs(primary) <= 0.001f || Mathf.Abs(secondary) > Mathf.Abs(primary) * axisNoiseRatio)
            return false;

        int direction = primary > 0f ? 1 : -1;
        if (direction != currentStrokeDirection)
        {
            currentStrokeDirection = direction;
            currentStrokeDistance = 0f;
        }

        currentStrokeDistance += Mathf.Abs(primary);
        if (currentStrokeDistance < minStrokeDistance || direction == lastCompletedStrokeDirection)
            return false;

        lastCompletedStrokeDirection = direction;
        currentStrokeDistance = 0f;
        completedStrokes = Mathf.Min(requiredStrokes, completedStrokes + 1);
        return true;
    }

    public void Reset()
    {
        currentStrokeDirection = 0;
        lastCompletedStrokeDirection = 0;
        currentStrokeDistance = 0f;
        completedStrokes = 0;
    }
}
