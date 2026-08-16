using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// C. 악셀 점프 - 공중에서 한 번 더 점프한다. 공중에 뜬 뒤 딱 한 번만 사용 가능하다.
    /// </summary>
    [CreateAssetMenu(menuName = "JumpKing/Skills/Accel Jump", fileName = "Skill_AccelJump")]
    public class AccelJumpSkillSO : SkillSO
    {
        [SerializeField] private float power = 8f;

        public override bool CanActivate(JumpKingCharacter character, SkillRuntimeState state)
        {
            return character.State == JumpKingMoveState.Airborne && !state.usedThisAirtime;
        }

        public override void Activate(JumpKingCharacter character, SkillRuntimeState state)
        {
            state.usedThisAirtime = true;
            // TODO: 방향을 어디서 받을지 결정 필요. JumpKingInputController가 마지막으로 읽은 방향키 값을
            // 재사용하거나(권장), 별도 파라미터 없이 항상 수직으로 고정하는 방법도 있음.
            character.ExecuteAirJump(Vector2.up, power);
        }

        public override void OnAirborneEnter(SkillRuntimeState state)
        {
            state.usedThisAirtime = false; // 새로 공중에 뜨면 다시 사용 가능
        }
    }
}
