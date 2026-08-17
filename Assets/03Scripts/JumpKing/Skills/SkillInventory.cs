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
        [Tooltip("급속빙결 스킬 애셋. 해금되어 있으면 미끄러운 필드 착지가 하드 필드처럼 즉시 정지한다 " +
                 "(SlipperyFieldBehaviour.OnLand가 HasFlashFreeze를 조회).")]
        [SerializeField] private FlashFreezeSkillSO flashFreezeSkill;

        private readonly List<SkillSO> unlockedSkills = new List<SkillSO>();
        private readonly Dictionary<SkillSO, SkillRuntimeState> runtimeStates = new Dictionary<SkillSO, SkillRuntimeState>();

        private JumpKingCharacter character;

        /// <summary>급속빙결이 해금되어 있는지. 미끄러운 필드 착지 처리에서 조회한다.</summary>
        public bool HasFlashFreeze => flashFreezeSkill != null && IsUnlocked(flashFreezeSkill);

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

        /// <summary>해금되어 있고 지금 발동 조건을 만족하는지만 확인한다 (부작용 없음, 상태를 바꾸지 않음).</summary>
        public bool CanActivate(SkillSO skill)
        {
            if (skill == null || !IsUnlocked(skill)) return false;
            return skill.CanActivate(character, runtimeStates[skill]);
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
