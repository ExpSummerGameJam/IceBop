namespace IceBop.JumpKing
{
    /// <summary>
    /// 스킬 1개 x 캐릭터 1명 기준으로 필요한 실행 중(런타임) 상태.
    /// SkillInventory가 해금된 SkillSO마다 하나씩 생성해 들고 있는다.
    /// SkillSO(애셋) 자체에는 상태를 저장하지 않는다 (모든 사용자가 같은 애셋 인스턴스를 공유하기 때문).
    /// </summary>
    public class SkillRuntimeState
    {
        public bool usedThisAirtime;   // 악셀 점프: 공중에 뜬 뒤 1회 제한용
        public float cooldownTimer;    // 필요 시 재사용 쿨다운
        public bool isActive;          // 텅 앵커처럼 지속시간이 있는 스킬의 활성 여부
    }
}
