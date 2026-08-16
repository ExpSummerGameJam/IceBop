using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// D. 약한 바닥 - 하드 필드와 동일하게 동작하지만, 밟은 타일 셀이 2번째 착지에 파괴되어 사라진다.
    /// 셀 단위 파손 카운트/복구는 같은 오브젝트에 붙어있는 WeakTilemap이 담당한다
    /// (이 객체는 모든 약한 바닥 타일맵이 공유하는 싱글턴이라 상태를 가질 수 없기 때문).
    /// </summary>
    public class WeakFieldBehaviour : IGroundBehaviour
    {
        public PlatformType Type => PlatformType.Weak;
        public bool AllowsJumpControl => true;

        public void OnLand(JumpKingCharacter character, PlatformSurface surface, Vector2 contactPoint)
        {
            character.Rb.linearVelocity = Vector2.zero; // 하드 필드와 동일하게 착지 즉시 정지
            surface.GetComponent<WeakTilemap>()?.RegisterLanding(contactPoint);
        }

        public void OnPhysicsStep(JumpKingCharacter character, PlatformSurface surface)
        {
        }

        public void OnLeave(JumpKingCharacter character, PlatformSurface surface)
        {
        }

        public void OnSideHit(JumpKingCharacter character, PlatformSurface surface, Vector2 normal)
        {
            // TODO: 필요하면 약한 바닥도 옆면 충돌 처리 추가 (지금은 Unity 기본 물리 반응에 맡김)
        }
    }
}
