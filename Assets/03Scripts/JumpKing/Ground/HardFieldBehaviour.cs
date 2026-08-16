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

        public void OnSideHit(JumpKingCharacter character, PlatformSurface surface, Vector2 normal)
        {
            // 착지로는 취급하지 않는다(State/CurrentGround 불변).
            // OnCollisionEnter2D 시점엔 Unity 물리 엔진이 이미 충돌을 해석(Rigidbody2D의 Bounciness로
            // 튕겨내는 것 포함)한 뒤이므로, 여기선 "떨어지는 중" 케이스만 후처리로 덮어쓴다.
            if (character.Rb.linearVelocity.y <= 0f)
            {
                // 떨어지는 중에 옆면에 부딪히면 튕기지 않고 X축 속도만 죽인다. Y(낙하)는 그대로 둬서 계속 떨어진다.
                character.Rb.linearVelocity = new Vector2(0f, character.Rb.linearVelocity.y);
            }
            // 올라가는 중에 부딪힌 경우는 그대로 둔다 — Rigidbody2D의 Bounciness가 이미 튕겨냈다.
        }
    }
}
