using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// A. 텅 앵커 - 혀를 길게 늘려 잠시 벽에 고정된다.
    /// 고정 중에는 바닥에 착지한 것처럼 점프 조작이 가능하다 (벽에 붙어서 점프하는 느낌).
    /// </summary>
    [CreateAssetMenu(menuName = "JumpKing/Skills/Tongue Anchor", fileName = "Skill_TongueAnchor")]
    public class TongueAnchorSkillSO : SkillSO
    {
        [SerializeField] private float anchorDuration = 1.5f;
        [SerializeField] private float anchorRange = 2f;

        public override bool CanActivate(JumpKingCharacter character, SkillRuntimeState state)
        {
            // TODO: 사거리(anchorRange) 내에 앵커 가능한 벽(Collider)이 있는지 체크 필요
            return character.State == JumpKingMoveState.Airborne && !state.isActive;
        }

        public override void Activate(JumpKingCharacter character, SkillRuntimeState state)
        {
            // TODO: 사거리 내 벽 탐색 -> 캐릭터를 벽에 붙임 -> character.SetAnchored(true)
            // anchorDuration 경과, 또는 재점프 입력 시 character.SetAnchored(false) 로 해제
            state.isActive = true;
        }

        public override void OnLanded(SkillRuntimeState state)
        {
            state.isActive = false;
        }
    }
}
