using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// C. 악셀 점프 - 공중에서 스페이스를 한 번 더 눌러 점프를 차지하고 다시 점프한다.
    /// 실제 차지/방향/발사는 JumpKingInputController가 일반 점프와 동일한 방식으로 처리하고,
    /// 여기서는 "공중에 뜬 뒤 딱 한 번만 사용 가능하다"는 조건과 사용 여부만 관리한다.
    /// </summary>
    [CreateAssetMenu(menuName = "JumpKing/Skills/Accel Jump", fileName = "Skill_AccelJump")]
    public class AccelJumpSkillSO : SkillSO
    {
        public override bool CanActivate(JumpKingCharacter character, SkillRuntimeState state)
        {
            return character.State == JumpKingMoveState.Airborne && !state.usedThisAirtime;
        }

        public override void Activate(JumpKingCharacter character, SkillRuntimeState state)
        {
            // 실제 점프 실행은 JumpKingInputController.ReleaseJump()가 character.ExecuteJump()로 처리한다.
            // 여기서는 "이번 공중에서 사용함" 표시만 담당한다.
            state.usedThisAirtime = true;
        }

        public override void OnAirborneEnter(SkillRuntimeState state)
        {
            state.usedThisAirtime = false; // 새로 공중에 뜨면 다시 사용 가능
        }
    }
}
