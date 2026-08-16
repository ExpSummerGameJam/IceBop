using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// "같은 종류의 발판"이 공유하는 설정값만 담는 ScriptableObject.
    /// 예: HardFieldData 애셋 1개를 하드 필드 타일 100개가 함께 참조.
    /// 개별 타일 하나만의 런타임 상태(파손 여부 등)는 여기 두지 않고 PlatformSurface(인스턴스)에 둔다.
    /// </summary>
    [CreateAssetMenu(menuName = "JumpKing/Platform Data", fileName = "PlatformData")]
    public class PlatformDataSO : ScriptableObject
    {
        [Header("공통")]
        public PlatformType type;

        [Header("C. 미끄러운 필드 전용")]
        [Tooltip("관성이 감쇠하는 정도. 0에 가까울수록 오래 미끄러짐")]
        public float slipperyDamping = 0.5f;

        [Header("B. 경사로 전용")]
        [Tooltip("경사면을 따라 미끄러질 때 가속도")]
        public float slideAcceleration = 9.8f;

        [Header("D. 약한 바닥 전용")]
        [Tooltip("몇 번째 착지에 파괴되는지 (기본 2)")]
        public int breakAfterHits = 2;
    }
}
