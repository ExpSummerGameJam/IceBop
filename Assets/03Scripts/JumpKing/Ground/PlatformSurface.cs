using System.Collections.Generic;
using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 씬에 배치되는 개별 발판(콜라이더)에 붙이는 컴포넌트.
    /// 공유 설정(PlatformDataSO)을 참조한다.
    /// 약한 바닥(D)은 서로 맞닿은 타일끼리 자동으로 WeakFieldGroup을 이뤄 파손 카운트/파손 자체를 공유한다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlatformSurface : MonoBehaviour
    {
        [SerializeField] private PlatformDataSO data;

        private static readonly List<PlatformSurface> weakSurfaces = new List<PlatformSurface>();
        private static bool weakGroupsBuilt;

        private Collider2D col;
        private WeakFieldGroup group;
        private bool isBroken;

        public PlatformType Type => data.type;
        public PlatformDataSO Data => data;
        public bool IsBroken => isBroken;

        private void Awake()
        {
            col = GetComponent<Collider2D>();
            if (Type == PlatformType.Weak)
            {
                weakSurfaces.Add(this);
            }
        }

        private void Start()
        {
            // 모든 오브젝트의 Awake가 끝난 뒤 호출되는 게 보장되므로, 이 시점에 한 번만
            // 씬에 있는 모든 약한 바닥을 인접 여부로 묶어 그룹을 만든다.
            if (Type == PlatformType.Weak && !weakGroupsBuilt)
            {
                BuildWeakGroups();
                weakGroupsBuilt = true;
            }
        }

        private void OnEnable()
        {
            ScrollManager.OnScrollChanged += ResetSurface;
        }

        private void OnDisable()
        {
            ScrollManager.OnScrollChanged -= ResetSurface;
        }

        private void OnDestroy()
        {
            weakSurfaces.Remove(this);
        }

        /// <summary>캐릭터가 착지했을 때 호출된다. 연결된 약한 바닥 그룹 전체가 파손 카운트를 공유한다.</summary>
        public void RegisterLanding()
        {
            group?.RegisterLanding(data.breakAfterHits);
            // TODO: 파손 전 경고 연출(금 가는 스프라이트 등)은 그룹 카운트가 breakAfterHits - 1일 때 처리
        }

        /// <summary>그룹이 임계치에 도달하면 그룹의 모든 멤버에서 호출된다.</summary>
        internal void BreakVisual()
        {
            isBroken = true;
            col.enabled = false;
            // TODO: 파손 스프라이트/파티클, 사운드
        }

        /// <summary>스크롤 전환으로 그룹이 초기화될 때 그룹의 모든 멤버에서 호출된다.</summary>
        internal void ResetVisual()
        {
            isBroken = false;
            col.enabled = true;
            // TODO: 콜라이더/렌더러 원상 복구
        }

        /// <summary>스크롤(화면 전환) 시 ScrollManager가 브로드캐스트하면 그룹 전체를 초기화한다.</summary>
        private void ResetSurface()
        {
            group?.Reset();
        }

        /// <summary>씬에 있는 모든 약한 바닥을 콜라이더 인접 여부로 묶어 WeakFieldGroup을 만든다 (flood-fill).</summary>
        private static void BuildWeakGroups()
        {
            var visited = new HashSet<PlatformSurface>();

            foreach (var surface in weakSurfaces)
            {
                if (visited.Contains(surface)) continue;

                var cluster = new List<PlatformSurface>();
                var queue = new Queue<PlatformSurface>();
                queue.Enqueue(surface);
                visited.Add(surface);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    cluster.Add(current);

                    foreach (var other in weakSurfaces)
                    {
                        if (visited.Contains(other)) continue;
                        if (IsAdjacent(current, other))
                        {
                            visited.Add(other);
                            queue.Enqueue(other);
                        }
                    }
                }

                var group = new WeakFieldGroup(cluster);
                foreach (var member in cluster)
                {
                    member.group = group;
                }
            }
        }

        /// <summary>두 타일의 콜라이더 경계가 살짝의 여유(epsilon)를 두고 맞닿아 있는지 검사한다.</summary>
        private static bool IsAdjacent(PlatformSurface a, PlatformSurface b)
        {
            const float epsilon = 0.05f;
            Bounds boundsA = a.col.bounds;
            boundsA.Expand(epsilon);
            return boundsA.Intersects(b.col.bounds);
        }
    }
}
