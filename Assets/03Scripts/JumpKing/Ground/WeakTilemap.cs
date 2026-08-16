using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace IceBop.JumpKing
{
    /// <summary>
    /// D. 약한 바닥 타일맵 전용 컴포넌트.
    /// 밟은 칸과 4방향으로 맞닿아 이어진(flood-fill) 칸들이 하나의 그룹으로 묶여
    /// 밟힌 횟수를 공유한다 — 그룹 중 아무 칸이나 밟아도 그룹 전체의 스프라이트가 함께 바뀐다.
    /// 1번 밟으면 그룹 전체가 금 간 타일(crackedTile)로, breakAfterHits번째 밟으면 그룹 전체가 사라진다
    /// (Tilemap에서 지워지면 TilemapCollider2D도 자동으로 그 칸의 충돌을 없앤다).
    /// 카메라 스크롤(ScrollManager.OnScrollChanged) 시 지금까지 바뀐 칸을 전부 원래 타일로 함께 복구한다.
    /// PlatformSurface와 같은 오브젝트에 붙어 breakAfterHits 설정값을 그대로 가져다 쓴다.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    [RequireComponent(typeof(PlatformSurface))]
    public class WeakTilemap : MonoBehaviour
    {
        [SerializeField] private TileBase crackedTile;

        private static readonly Vector3Int[] Neighbors4 =
        {
            Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right
        };

        private Tilemap tilemap;
        private PlatformSurface surface;
        private readonly Dictionary<Vector3Int, int> hitCounts = new Dictionary<Vector3Int, int>();
        private readonly Dictionary<Vector3Int, TileBase> originalTiles = new Dictionary<Vector3Int, TileBase>();

        private void Awake()
        {
            tilemap = GetComponent<Tilemap>();
            surface = GetComponent<PlatformSurface>();
        }

        private void OnEnable()
        {
            ScrollManager.OnScrollChanged += RestoreAll;
        }

        private void OnDisable()
        {
            ScrollManager.OnScrollChanged -= RestoreAll;
        }

        /// <summary>캐릭터가 이 타일맵의 특정 지점(월드 좌표)에 착지했을 때 호출된다.</summary>
        public void RegisterLanding(Vector2 worldContactPoint)
        {
            Vector3Int cell = tilemap.WorldToCell(worldContactPoint);
            if (!tilemap.HasTile(cell)) return; // 이미 파손되어 사라진 칸

            HashSet<Vector3Int> group = FloodFillGroup(cell);

            int currentCount = hitCounts.TryGetValue(cell, out int existing) ? existing : 0;
            int newCount = currentCount + 1;
            bool breaking = newCount >= surface.Data.breakAfterHits;

            foreach (var member in group)
            {
                if (!originalTiles.ContainsKey(member))
                {
                    originalTiles[member] = tilemap.GetTile(member);
                }

                hitCounts[member] = newCount;
                tilemap.SetTile(member, breaking ? null : crackedTile);
            }
        }

        /// <summary>밟은 칸과 4방향으로 이어진(맞닿은) 살아있는 칸들을 전부 찾는다.</summary>
        private HashSet<Vector3Int> FloodFillGroup(Vector3Int start)
        {
            var group = new HashSet<Vector3Int> { start };
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                foreach (var dir in Neighbors4)
                {
                    Vector3Int next = current + dir;
                    if (group.Contains(next)) continue;
                    if (!tilemap.HasTile(next)) continue;

                    group.Add(next);
                    queue.Enqueue(next);
                }
            }

            return group;
        }

        /// <summary>스크롤 전환 시 지금까지 변경된 모든 칸을 원래 타일로 함께 복구한다.</summary>
        private void RestoreAll()
        {
            if (originalTiles.Count == 0) return;

            foreach (var cellAndTile in originalTiles)
            {
                tilemap.SetTile(cellAndTile.Key, cellAndTile.Value);
            }

            originalTiles.Clear();
            hitCounts.Clear();
        }
    }
}
