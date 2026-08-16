using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>A. 하드 필드 - 일반적인 바닥. 착지 시 점프 조작이 가능하다.</summary>
    public class HardFieldBehaviour : IGroundBehaviour
    {
        public PlatformType Type => PlatformType.Hard;
        public bool AllowsJumpControl => true;

        public void OnLand(JumpKingCharacter character, PlatformSurface surface)
        {
            character.Rb.linearVelocity = Vector2.zero; // 관성 없음: 착지 즉시 정지
        }

        public void OnPhysicsStep(JumpKingCharacter character, PlatformSurface surface)
        {
            // 특별한 처리 없음
        }

        public void OnLeave(JumpKingCharacter character, PlatformSurface surface)
        {
        }
    }
}
