using UnityEngine;

public sealed class WorldIngredientPickupTrigger2D : MonoBehaviour
{
    private WorldIngredientPickup2D owner;
    private string playerTag;

    public void Bind(WorldIngredientPickup2D pickupOwner, string pickupPlayerTag)
    {
        owner = pickupOwner;
        playerTag = pickupPlayerTag;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other.gameObject))
            owner?.SetPlayerInRange(other.transform, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other.gameObject))
            owner?.SetPlayerInRange(other.transform, false);
    }

    private bool IsPlayer(GameObject candidate)
    {
        if (candidate.GetComponentInParent<GatheringPlayerMarker>() != null)
            return true;

        return string.IsNullOrEmpty(playerTag) || string.Equals(candidate.tag, playerTag, System.StringComparison.Ordinal);
    }
}
