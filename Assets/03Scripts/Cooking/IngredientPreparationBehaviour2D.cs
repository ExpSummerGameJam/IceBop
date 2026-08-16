using System;
using UnityEngine;

public abstract class IngredientPreparationBehaviour2D : MonoBehaviour
{
    [SerializeField] private IngredientPreparationKind preparationKind;
    [SerializeField] private bool isPrepared;

    public IngredientPreparationKind PreparationKind => preparationKind;
    public bool IsPrepared => isPrepared;

    public event Action<IngredientPreparationBehaviour2D> PreparationCompleted;

    protected void SetPreparationKind(IngredientPreparationKind kind)
    {
        preparationKind = kind;
    }

    protected void ResetPreparationState()
    {
        isPrepared = false;
    }

    protected bool TryMarkPrepared()
    {
        if (isPrepared)
            return false;

        isPrepared = true;
        PreparationCompleted?.Invoke(this);
        return true;
    }
}
