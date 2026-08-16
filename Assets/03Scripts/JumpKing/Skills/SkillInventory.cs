using System.Collections.Generic;
using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 캐릭터가 해금한 스킬 목록과, 스킬별 런타임 상태(SkillRuntimeState)를 관리한다.
    /// </summary>
    [RequireComponent(typeof(JumpKingCharacter))]
    public class SkillInventory : MonoBehaviour
    {
        private readonly List<SkillSO> unlockedSkills = new List<SkillSO>();
        private readonly Dictionary<SkillSO, SkillRuntimeState> runtimeStates = new Dictionary<SkillSO, SkillRuntimeState>();

        private JumpKingCharacter character;

        private void Awake()
        {
            character = GetComponent<JumpKingCharacter>();
        }

        private void OnEnable()
        {
            character.OnAirborne += HandleAirborne;
            character.OnLanded += HandleLanded;
        }

        private void OnDisable()
        {
            character.OnAirborne -= HandleAirborne;
            character.OnLanded -= HandleLanded;
        }

        public bool IsUnlocked(SkillSO skill) => unlockedSkills.Contains(skill);

        /// <summary>음식 섭취 시 FoodItem이 호출.</summary>
        public void Unlock(SkillSO skill)
        {
            if (skill == null || IsUnlocked(skill)) return;

            unlockedSkills.Add(skill);
            runtimeStates[skill] = new SkillRuntimeState();
            // TODO: 스킬 해금 UI 알림 등
        }

        /// <summary>해금 + 발동 조건 충족 시에만 실제로 발동한다.</summary>
        public bool TryActivate(SkillSO skill)
        {
            if (!IsUnlocked(skill)) return false;

            var state = runtimeStates[skill];
            if (!skill.CanActivate(character, state)) return false;

            skill.Activate(character, state);
            return true;
        }

        private void HandleAirborne()
        {
            foreach (var kv in runtimeStates)
            {
                kv.Key.OnAirborneEnter(kv.Value);
            }
        }

        private void HandleLanded(PlatformSurface surface)
        {
            foreach (var kv in runtimeStates)
            {
                kv.Key.OnLanded(kv.Value);
            }
        }
    }
}
