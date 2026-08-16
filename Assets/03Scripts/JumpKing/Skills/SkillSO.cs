using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 스킬의 데이터(이름/아이콘/튜닝값)와 행동(발동 로직)을 함께 담는 ScriptableObject 베이스.
    /// 주의: SO 애셋은 모두가 공유하는 하나의 인스턴스이므로,
    /// 여기에 실행 중 상태(쿨다운, 사용 여부 등)를 필드로 저장하면 안 된다.
    /// 그런 상태는 인자로 전달되는 SkillRuntimeState(캐릭터가 소유)에 저장한다.
    /// </summary>
    public abstract class SkillSO : ScriptableObject
    {
        [Header("공통")]
        public string skillName;
        public Sprite icon;
        [TextArea] public string description;

        /// <summary>지금 이 스킬을 발동할 수 있는 상태인지.</summary>
        public abstract bool CanActivate(JumpKingCharacter character, SkillRuntimeState state);

        /// <summary>실제 발동 처리.</summary>
        public abstract void Activate(JumpKingCharacter character, SkillRuntimeState state);

        /// <summary>공중 진입 시점 훅 (예: 악셀 점프의 사용 여부 초기화).</summary>
        public virtual void OnAirborneEnter(SkillRuntimeState state) { }

        /// <summary>착지 시점 훅.</summary>
        public virtual void OnLanded(SkillRuntimeState state) { }
    }
}
