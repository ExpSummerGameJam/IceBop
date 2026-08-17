using System;
using UnityEngine;

public abstract class IngredientPreparationBehaviour2D : MonoBehaviour
{
    [SerializeField] private IngredientPreparationKind preparationKind;
    [SerializeField] private bool isPrepared;

    public IngredientPreparationKind PreparationKind => preparationKind;
    public bool IsPrepared => isPrepared;

    /// <summary>The ingredient's sprite after this prep step, if it visually changed (e.g. a cut piece) - null means "unchanged". Read once preparation completes so the finishing stage shows the prepared state instead of the raw ingredient.</summary>
    public virtual Sprite ProcessedSprite => null;

    /// <summary>The ingredient's tint after this prep step, if it visually changed (e.g. ground powder) - null means "unchanged".</summary>
    public virtual Color? ProcessedColor => null;

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
