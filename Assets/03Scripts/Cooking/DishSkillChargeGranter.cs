using IceBop.JumpKing;
using UnityEngine;

/// <summary>
/// Grants skill charges to the player whenever a dish is cooked, using the per-ability charge
/// counts authored on the dish row (anchorTongueCharges / rapidFreezeCharges / accelJumpCharges
/// in CookingData.xlsx "Dishes").
/// </summary>
[DisallowMultipleComponent]
public sealed class DishSkillChargeGranter : MonoBehaviour
{
    [Header("Skills")]
    [SerializeField] private SkillSO anchorTongueSkill;
    [SerializeField] private SkillSO rapidFreezeSkill;
    [SerializeField] private SkillSO accelJumpSkill;

    [Header("Target")]
    [Tooltip("Leave empty to find the player's SkillInventory in the scene on first grant.")]
    [SerializeField] private SkillInventory skillInventory;

    private void OnEnable()
    {
        CookingSystem.DishCrafted += OnDishCrafted;
    }

    private void OnDisable()
    {
        CookingSystem.DishCrafted -= OnDishCrafted;
    }

    private void OnDishCrafted(CookingCraftResult result)
    {
        DishDefinition dish = result != null ? result.dish : null;
        if (dish == null)
            return;

        GrantCharges(anchorTongueSkill, dish.anchorTongueCharges);
        GrantCharges(rapidFreezeSkill, dish.rapidFreezeCharges);
        GrantCharges(accelJumpSkill, dish.accelJumpCharges);
    }

    private void GrantCharges(SkillSO skill, int amount)
    {
        if (skill == null || amount <= 0)
            return;

        if (!ResolveSkillInventory())
            return;

        // AddCharges also unlocks the skill the first time, so a dish that grants charges for
        // a not-yet-unlocked ability makes it usable rather than silently doing nothing.
        skillInventory.AddCharges(skill, amount);
    }

    private bool ResolveSkillInventory()
    {
        if (skillInventory != null)
            return true;

        skillInventory = FindFirstObjectByType<SkillInventory>();
        return skillInventory != null;
    }
}
