namespace IceBop.JumpKing
{
    /// <summary>
    /// 발판 종류. 종류마다 IGroundBehaviour 구현체가 하나씩 대응된다.
    /// </summary>
    public enum PlatformType
    {
        Hard,       // A. 하드 필드 - 일반 바닥. 착지 시 점프 조작 가능
        Slope,      // B. 경사로 - 미끄러지며 추락. 하드/미끄러운 필드에 닿기 전까지 점프 불가
        Slippery,   // C. 미끄러운 필드 - 점프 조작은 가능하지만 관성에 따라 미끄러짐
        Weak,       // D. 약한 바닥 - 하드 필드와 동일하나 2번째 착지에 파괴됨 (스크롤 전환 시 초기화)
    }
}
