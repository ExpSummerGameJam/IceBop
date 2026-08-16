using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The final "cooking" stage after every ingredient has been prepared. Which mechanic
/// runs (stir, shake, skewer, guided placement, ...) depends on the dish being made;
/// CookingFlowController2D only depends on this contract, not the concrete stage type.
/// </summary>
public interface IDishFinishingStage
{
    /// <summary>Raised once the player has finished the interactive part (e.g. enough stirs/shakes/placements).</summary>
    event Action InteractionCompleted;

    /// <summary>Raised once the completed-dish presentation animation has finished playing.</summary>
    event Action ResultPresented;

    void Configure(Camera camera, IReadOnlyList<CookingIngredientEntry> ingredients, DishDefinition dish, CookingMethodToolVisual2D toolVisual);

    void PlayResultPresentation();
}
