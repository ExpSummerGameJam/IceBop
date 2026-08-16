namespace IceBop.JumpKing
{
    /// <summary>
    /// B. 경사로 - 미끄럽기 때문에 벽/사면을 따라 미끄러지며 바닥으로 추락한다.
    /// 하드 필드 혹은 미끄러운 필드에 닿기 전까지 점프 조작이 불가능하다.
    /// </summary>
    public class SlopeBehaviour : IGroundBehaviour
    {
        public PlatformType Type => PlatformType.Slope;
        public bool AllowsJumpControl => false;

        public void OnLand(JumpKingCharacter character, PlatformSurface surface)
        {
            // 경사로는 "착지"라기보다 "닿음" 취급. 캐릭터 상태를 Sliding으로 전환하는 처리는
            // JumpKingCharacter.OnCollisionEnter2D 쪽에서 AllowsJumpControl 값을 보고 이미 결정한다.
        }

        public void OnPhysicsStep(JumpKingCharacter character, PlatformSurface surface)
        {
            // TODO: 접촉면(경사) 방향을 따라 character.Rb.linearVelocity 를 갱신해 미끄러지는 느낌 구현.
            // 접촉 노멀은 OnCollisionEnter2D/Stay2D의 ContactPoint2D.normal 등에서 얻어와 저장해두고 사용.
            // surface.Data.slideAcceleration 을 가속도로 사용.
        }

        public void OnLeave(JumpKingCharacter character, PlatformSurface surface)
        {
        }
    }
}
