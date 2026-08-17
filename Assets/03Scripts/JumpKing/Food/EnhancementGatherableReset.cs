using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 카메라가 맨 아래(시작) 방으로 돌아오면(ScrollManager.OnReturnedToStartRoom),
    /// 지정된 컨테이너들(stage1/2/3, frog/octopus/feather 타일맵 등) 아래에 배치된
    /// 모든 GatherableObject를 상호작용하기 전 상태로 되돌린다.
    /// EnhancementGatherable_* 뿐 아니라 Gatherable_* 등 어떤 GatherableObject 프리팹이든 동일하게 동작한다 —
    /// 이름과 달리 Enhancement 전용이 아니라 범용이다.
    /// 실제 상태 초기화는 GatherableObject.SetGathered(false)가 이미 처리하므로 그대로 재사용한다.
    /// </summary>
    public class EnhancementGatherableReset : MonoBehaviour
    {
        [Tooltip("이 아래에 배치된 GatherableObject를 전부 초기화 대상으로 삼는다. " +
                 "stage1/2/3, frog/octopus/feather 등 타일맵을 여기에 전부 연결하면 된다.")]
        [SerializeField] private Transform[] stageContainers;

        private void OnEnable()
        {
            ScrollManager.OnReturnedToStartRoom += ResetAll;
        }

        private void OnDisable()
        {
            ScrollManager.OnReturnedToStartRoom -= ResetAll;
        }

        private void ResetAll()
        {
            foreach (var container in stageContainers)
            {
                if (container == null) continue;

                foreach (var gatherable in container.GetComponentsInChildren<GatherableObject>(true))
                {
                    gatherable.SetGathered(false);
                }
            }
        }
    }
}
