using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// D. 약한 바닥 - 하드 필드와 동일하게 동작하지만, 2번째 착지에 파괴되어 사라진다.
    /// 파손 카운트는 이 행동 객체가 아니라 인스턴스별 상태를 가진 PlatformSurface에 저장한다
    /// (이 객체는 모든 약한 바닥 타일이 공유하는 싱글턴이기 때문).
    /// </summary>
    public class WeakFieldBehaviour : IGroundBehaviour
    {
        public PlatformType Type => PlatformType.Weak;
        public bool AllowsJumpControl => true;

        public void OnLand(JumpKingCharacter character, PlatformSurface surface)
        {
            // TODO: 하드 필드와 동일하게 수평/수직 속도 정지
            surface.RegisterLanding();
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
