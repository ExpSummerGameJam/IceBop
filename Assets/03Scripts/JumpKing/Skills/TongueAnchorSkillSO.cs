using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// A. 텅 앵커 - 혀를 늘려 잠시 몸을 공중에 고정시킨다.
    /// 주변 anchorRange 안에 발판(타일)이 있을 때 Q(SkillInputController의 skillAction)를 누르면 즉시 고정되고,
    /// 고정 중에는 바닥에 착지한 것처럼 점프 조작이 가능하다. anchorDuration이 지나면 자동으로 풀린다.
    /// </summary>
    [CreateAssetMenu(menuName = "JumpKing/Skills/Tongue Anchor", fileName = "Skill_TongueAnchor")]
    public class TongueAnchorSkillSO : SkillSO
    {
        [Tooltip("고정이 자동으로 풀리기까지 걸리는 시간(초). 그 전에 점프하면 그 즉시 풀린다.")]
        [SerializeField] private float anchorDuration = 1.5f;
        [Tooltip("이 범위 안에 발판이 있어야 고정을 시도할 수 있다.")]
        [SerializeField] private float anchorRange = 2f;
        [Tooltip("사거리 판정에 사용할 레이어. 발판 콜라이더가 속한 레이어로 좁혀서 설정할 것.")]
        [SerializeField] private LayerMask platformLayerMask = ~0;

        public override bool CanActivate(JumpKingCharacter character, SkillRuntimeState state)
        {
            if (state.charges <= 0) return false;
            if (character.State != JumpKingMoveState.Airborne) return false;
            return Physics2D.OverlapCircle(character.transform.position, anchorRange, platformLayerMask) != null;
        }

        public override void Activate(JumpKingCharacter character, SkillRuntimeState state)
        {
            state.charges--; // 벽에 붙을 때마다 1회 소모
            character.SetAnchored(true, anchorDuration);
        }
    }
}
