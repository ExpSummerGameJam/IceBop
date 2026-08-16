using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>B. 급속빙결 - 미끄러운 필드에서 모든 관성을 무시하고 즉시 정지한다.</summary>
    [CreateAssetMenu(menuName = "JumpKing/Skills/Flash Freeze", fileName = "Skill_FlashFreeze")]
    public class FlashFreezeSkillSO : SkillSO
    {
        public override bool CanActivate(JumpKingCharacter character, SkillRuntimeState state)
        {
            return character.CurrentGround != null && character.CurrentGround.Type == PlatformType.Slippery;
        }

        public override void Activate(JumpKingCharacter character, SkillRuntimeState state)
        {
            // TODO: character.Rb.linearVelocity = Vector2.zero; (SlipperyFieldBehaviour의 관성 감쇠를 즉시 무시)
        }
    }
}
