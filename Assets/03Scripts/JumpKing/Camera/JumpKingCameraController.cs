using System.Collections;
using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 16x16 타일 단위의 "방(room)" 카메라.
    /// X/Y 모두 캐릭터가 속한 방 정중앙을 비추도록 고정되어 있다가,
    /// 캐릭터가 그 방 경계를 넘는 순간(공중/착지 상관없이) 옆/위/아래 방으로 스크롤 이동한다.
    /// 실제로 방이 바뀌었을 때만 ScrollManager.NotifyScrollChanged()를 호출해
    /// 약한 바닥(D) 초기화 등을 트리거한다.
    /// </summary>
    public class JumpKingCameraController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private JumpKingCharacter target;

        [Header("방(Room) 크기")]
        [Tooltip("타일 하나의 월드 유닛 크기. 프로젝트 Grid의 Cell Size와 맞출 것 (현재 1).")]
        [SerializeField] private float tileSize = 1f;
        [Tooltip("방 한 변에 들어가는 타일 수(16이면 16x16). 방과 방 사이의 실제 간격(=카메라 뷰포트 크기)은 " +
                 "(타일 수 - 1) * 타일 크기다. 예: 16타일이면 간격은 15 — 카메라 Orthographic Size는 이 값의 절반으로 맞출 것.")]
        [SerializeField] private int roomSizeInTiles = 16;

        [Header("방 그리드 기준점")]
        [Tooltip("첫 방의 X 중앙, Y 맨 아래 경계 좌표(월드). 예: (-3,-4)면 첫 방은 X=-3에 고정되고 아래쪽 경계가 Y=-4.")]
        [SerializeField] private Vector2 startRoomReference = new Vector2(-3f, -4f);

        [Header("전환 연출")]
        [SerializeField] private float scrollDuration = 0.4f;
        [SerializeField] private AnimationCurve scrollCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>방과 방 사이의 실제 간격 = 카메라 뷰포트 한 변의 크기.</summary>
        private float RoomSpacing => (roomSizeInTiles - 1) * tileSize;

        /// <summary>방(0,0)의 중앙. X는 이미 중앙 좌표로 주어졌으니 그대로, Y는 "아래쪽 경계"이므로 반 칸(RoomSpacing/2) 위로 보정.</summary>
        private Vector2 FirstRoomCenter => new Vector2(
            startRoomReference.x,
            startRoomReference.y + RoomSpacing * 0.5f);

        /// <summary>방(0,0)의 왼쪽 아래 모서리.</summary>
        private Vector2 GridOrigin => FirstRoomCenter - new Vector2(RoomSpacing, RoomSpacing) * 0.5f;

        private Vector2Int currentRoomIndex;
        private Coroutine scrollRoutine;

        private void Start()
        {
            currentRoomIndex = RoomIndexOf(target.transform.position);
            // 시작 시점에도 즉시 스냅하지 않고, 인스펙터에 배치해둔 카메라 위치에서
            // 캐릭터가 속한 방 중앙으로 스크롤 연출이 한 번 재생되게 한다.
            scrollRoutine = StartCoroutine(ScrollTo(RoomToWorldCenter(currentRoomIndex)));
        }

        private void LateUpdate()
        {
            // 캐릭터의 물리 이동이 끝난 뒤(LateUpdate)에 방 경계를 넘었는지 매 프레임 검사한다.
            // 착지 여부와 상관없이, 방 경계를 넘는 즉시 스크롤을 시작한다.
            Vector2Int newRoomIndex = RoomIndexOf(target.transform.position);
            if (newRoomIndex == currentRoomIndex) return;

            currentRoomIndex = newRoomIndex;

            if (scrollRoutine != null)
            {
                StopCoroutine(scrollRoutine);
            }
            scrollRoutine = StartCoroutine(ScrollTo(RoomToWorldCenter(currentRoomIndex)));
        }

        private Vector2Int RoomIndexOf(Vector3 worldPosition)
        {
            Vector2 origin = GridOrigin;
            return new Vector2Int(
                Mathf.FloorToInt((worldPosition.x - origin.x) / RoomSpacing),
                Mathf.FloorToInt((worldPosition.y - origin.y) / RoomSpacing));
        }

        private Vector3 RoomToWorldCenter(Vector2Int roomIndex)
        {
            Vector2 origin = GridOrigin;
            float x = origin.x + (roomIndex.x + 0.5f) * RoomSpacing;
            float y = origin.y + (roomIndex.y + 0.5f) * RoomSpacing;
            return new Vector3(x, y, transform.position.z);
        }

        private IEnumerator ScrollTo(Vector3 destination)
        {
            Vector3 start = transform.position;
            float t = 0f;

            while (t < scrollDuration)
            {
                t += Time.deltaTime;
                float eval = scrollCurve.Evaluate(Mathf.Clamp01(t / scrollDuration));
                transform.position = Vector3.Lerp(start, destination, eval);
                yield return null;
            }

            transform.position = destination;
            ScrollManager.NotifyScrollChanged();
        }
    }
}
