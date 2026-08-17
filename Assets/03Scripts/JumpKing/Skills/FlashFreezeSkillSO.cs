using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// B. 급속빙결 - 해금되어 있으면 미끄러운 필드 착지가 하드 필드처럼 즉시 정지한다.
    /// 버튼으로 발동하는 스킬이 아니라 해금 즉시 적용되는 패시브라서, 실제 효과는
    /// SkillInventory.HasFlashFreeze를 SlipperyFieldBehaviour.OnLand가 조회하는 방식으로 구현되어 있다.
    /// CanActivate/Activate는 이 스킬에서는 사용되지 않는다(스킬 버튼 우선순위 목록에 넣지 않을 것).
    /// </summary>
    [CreateAssetMenu(menuName = "JumpKing/Skills/Flash Freeze", fileName = "Skill_FlashFreeze")]
    public class FlashFreezeSkillSO : SkillSO
    {
        public override bool CanActivate(JumpKingCharacter character, SkillRuntimeState state) => false;

        public override void Activate(JumpKingCharacter character, SkillRuntimeState state)
        {
        }
    }
}
