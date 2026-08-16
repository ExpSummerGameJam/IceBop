namespace IceBop.JumpKing
{
    /// <summary>
    /// 발판 종류별 물리 처리를 캡슐화하는 전략(Strategy) 인터페이스.
    /// 구현체는 여러 발판 인스턴스가 공유하므로 상태를 갖지 않는 것을 원칙으로 한다.
    /// 인스턴스별 상태(파손 카운트 등)가 필요하면 PlatformSurface 쪽에 저장한다.
    /// </summary>
    public interface IGroundBehaviour
    {
        PlatformType Type { get; }

        /// <summary>이 발판 위에서 점프 차지/발사 조작이 가능한지.</summary>
        bool AllowsJumpControl { get; }

        /// <summary>착지 순간 1회 호출.</summary>
        void OnLand(JumpKingCharacter character, PlatformSurface surface);

        /// <summary>착지해 있는 동안 매 FixedUpdate 호출 (경사로 미끄러짐, 관성 감쇠 등).</summary>
        void OnPhysicsStep(JumpKingCharacter character, PlatformSurface surface);

        /// <summary>이 발판을 벗어나는 순간 1회 호출.</summary>
        void OnLeave(JumpKingCharacter character, PlatformSurface surface);
    }
}
