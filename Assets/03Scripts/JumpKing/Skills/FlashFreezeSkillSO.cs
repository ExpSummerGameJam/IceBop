using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// B. 급속빙결 - 충전 횟수가 남아있으면 미끄러운 필드 착지가 하드 필드처럼 즉시 정지하고, 1회 소모된다.
    /// 버튼으로 발동하는 스킬이 아니라 착지 시점에 자동으로 시도되는 패시브라서, SkillInputController의
    /// 우선순위 목록에는 넣지 않는다 — 대신 SlipperyFieldBehaviour.OnLand가
    /// SkillInventory.TryActivateFlashFreeze()를 통해 이 CanActivate/Activate를 호출한다.
    /// </summary>
    [CreateAssetMenu(menuName = "JumpKing/Skills/Flash Freeze", fileName = "Skill_FlashFreeze")]
    public class FlashFreezeSkillSO : SkillSO
    {
        public override bool CanActivate(JumpKingCharacter character, SkillRuntimeState state)
        {
            return state.charges > 0;
        }

        public override void Activate(JumpKingCharacter character, SkillRuntimeState state)
        {
            state.charges--; // 미끄러운 필드 착지할 때마다 1회 소모
            character.Rb.linearVelocity = Vector2.zero;
        }
    }
}
