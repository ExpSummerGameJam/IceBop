using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 씬에 배치되는 발판(콜라이더)에 붙이는 컴포넌트. 공유 설정(PlatformDataSO)을 참조한다.
    /// 약한 바닥(D)의 타일 셀 단위 파손/복구는 같은 오브젝트의 WeakTilemap이 별도로 담당한다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlatformSurface : MonoBehaviour
    {
        [SerializeField] private PlatformDataSO data;

        public PlatformType Type => data.type;
        public PlatformDataSO Data => data;
    }
}
