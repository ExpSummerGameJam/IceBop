namespace IceBop.JumpKing
{
    /// <summary>
    /// C. 미끄러운 필드 - 하드 필드처럼 점프 조작이 가능하지만, 착지 시 관성을 유지한 채 미끄러진다.
    /// </summary>
    public class SlipperyFieldBehaviour : IGroundBehaviour
    {
        public PlatformType Type => PlatformType.Slippery;
        public bool AllowsJumpControl => true;

        public void OnLand(JumpKingCharacter character, PlatformSurface surface)
        {
            // 하드 필드와 달리 착지 시 수평 속도를 0으로 만들지 않는다 (관성 유지).
        }

        public void OnPhysicsStep(JumpKingCharacter character, PlatformSurface surface)
        {
            // TODO: surface.Data.slipperyDamping 값으로 매 스텝 수평 속도를 서서히 감쇠.
            // 급속빙결(FlashFreezeSkillSO) 발동 시 이 관성을 즉시 0으로 덮어씀.
        }

        public void OnLeave(JumpKingCharacter character, PlatformSurface surface)
        {
        }
    }
}
