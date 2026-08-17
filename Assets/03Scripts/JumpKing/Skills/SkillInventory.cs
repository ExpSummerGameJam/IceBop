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
        [Tooltip("급속빙결 스킬 애셋. 미끄러운 필드에 착지할 때마다 SlipperyFieldBehaviour.OnLand가 " +
                 "TryActivateFlashFreeze()를 호출해서 충전 횟수를 1씩 소모한다.")]
        [SerializeField] private FlashFreezeSkillSO flashFreezeSkill;

        [Header("디버그")]
        [Tooltip("테스트용. 여기 넣어둔 스킬은 Play 시작하자마자 자동으로 해금 + 충전된다 (Food 안 먹어도 됨). " +
                 "테스트 끝나면 비워두는 걸 권장.")]
        [SerializeField] private SkillSO[] debugUnlockOnStart;
        [SerializeField] private int debugChargeAmount = 99;

        private readonly List<SkillSO> unlockedSkills = new List<SkillSO>();
        private readonly Dictionary<SkillSO, SkillRuntimeState> runtimeStates = new Dictionary<SkillSO, SkillRuntimeState>();

        private JumpKingCharacter character;

        private void Awake()
        {
            character = GetComponent<JumpKingCharacter>();

            foreach (var skill in debugUnlockOnStart)
            {
                AddCharges(skill, debugChargeAmount);
            }
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

        /// <summary>
        /// 음식 섭취 시 FoodItem이 호출. 처음 먹으면 해금과 동시에, 이후로는 먹을 때마다
        /// 사용 횟수(charges)를 amount만큼 늘린다.
        /// </summary>
        public void AddCharges(SkillSO skill, int amount = 1)
        {
            if (skill == null) return;

            if (!IsUnlocked(skill))
            {
                unlockedSkills.Add(skill);
                runtimeStates[skill] = new SkillRuntimeState();
                // TODO: 스킬 해금 UI 알림 등
            }

            runtimeStates[skill].charges += amount;
        }

        /// <summary>해금되어 있고 지금 발동 조건을 만족하는지만 확인한다 (부작용 없음, 상태를 바꾸지 않음).</summary>
        public bool CanActivate(SkillSO skill)
        {
            if (skill == null || !IsUnlocked(skill)) return false;
            return skill.CanActivate(character, runtimeStates[skill]);
        }

        /// <summary>해금 + 발동 조건(충전 횟수 포함) 충족 시에만 실제로 발동하고, 스킬이 알아서 충전을 소모한다.</summary>
        public bool TryActivate(SkillSO skill)
        {
            if (!IsUnlocked(skill)) return false;

            var state = runtimeStates[skill];
            if (!skill.CanActivate(character, state)) return false;

            skill.Activate(character, state);
            return true;
        }

        /// <summary>급속빙결 전용 진입점. 미끄러운 필드 착지 시 SlipperyFieldBehaviour.OnLand가 호출한다.</summary>
        public bool TryActivateFlashFreeze() => TryActivate(flashFreezeSkill);

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
