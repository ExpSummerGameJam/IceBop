using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>필드에 배치되는 음식. 플레이어가 닿으면 지정된 스킬을 해금한다.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class FoodItem : MonoBehaviour
    {
        [SerializeField] private SkillSO unlockSkill;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var inventory = other.GetComponent<SkillInventory>();
            if (inventory == null) return;

            inventory.Unlock(unlockSkill);
            // TODO: 획득 연출/사운드 재생 후 Destroy(gameObject);
        }
    }
}
