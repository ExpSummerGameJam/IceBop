namespace IceBop.JumpKing
{
    /// <summary>
    /// 점프킹 캐릭터의 이동 상태.
    /// </summary>
    public enum JumpKingMoveState
    {
        Idle,       // 지상에 정지, 점프 조작 가능
        Charging,   // 스페이스 홀드 중, 점프 높이 차지 중
        Airborne,   // 공중에 떠 있는 상태 (점프/낙하 중)
        Sliding,    // 경사로(B)에서 미끄러지며 추락 중, 점프 조작 불가
        Anchored,   // 텅 앵커 등으로 벽에 고정된 상태. 지상 취급, 점프 조작 가능
    }
}
