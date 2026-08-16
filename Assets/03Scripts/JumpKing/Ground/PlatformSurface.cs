using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 씬에 배치되는 개별 발판(콜라이더)에 붙이는 컴포넌트.
    /// 공유 설정(PlatformDataSO)을 참조하고, 이 인스턴스만의 런타임 상태(파손 여부 등)를 들고 있다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlatformSurface : MonoBehaviour
    {
        [SerializeField] private PlatformDataSO data;

        private int hitCount;
        private bool isBroken;

        public PlatformType Type => data.type;
        public PlatformDataSO Data => data;
        public bool IsBroken => isBroken;

        private void OnEnable()
        {
            ScrollManager.OnScrollChanged += ResetSurface;
        }

        private void OnDisable()
        {
            ScrollManager.OnScrollChanged -= ResetSurface;
        }

        /// <summary>
        /// 캐릭터가 착지했을 때 호출된다. D. 약한 바닥의 파손 카운트를 여기서 처리한다.
        /// </summary>
        public void RegisterLanding()
        {
            hitCount++;
            if (hitCount >= data.breakAfterHits)
            {
                Break();
            }
            // TODO: 파손 전 경고 연출(금 가는 스프라이트 등)은 hitCount == breakAfterHits - 1 시점에 처리
        }

        private void Break()
        {
            isBroken = true;
            // TODO: 콜라이더 비활성화, 파손 스프라이트/파티클, 사운드
        }

        /// <summary>
        /// 스크롤(화면 전환) 시 ScrollManager가 브로드캐스트하면 파손 상태를 초기화한다.
        /// </summary>
        private void ResetSurface()
        {
            hitCount = 0;
            isBroken = false;
            // TODO: 콜라이더/렌더러 원상 복구
        }
    }
}
