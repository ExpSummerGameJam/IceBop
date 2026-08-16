using System.Collections;
using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 16x16 타일 단위의 "방(room)" 카메라.
    /// X축은 startRoomReference.x에 완전히 고정되어 절대 움직이지 않는다.
    /// Y축만 캐릭터가 속한 방 정중앙을 비추도록 고정되어 있다가,
    /// 캐릭터가 그 방 경계를 넘는 순간(공중/착지 상관없이) 위/아래 방으로 스크롤 이동한다.
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
                 "타일 수 * 타일 크기 그대로다(16타일이면 간격 16, 겹치거나 비는 구간 없이 딱 이어붙음). " +
                 "카메라 Orthographic Size는 이 값의 절반으로 맞출 것.")]
        [SerializeField] private int roomSizeInTiles = 16;

        [Header("방 그리드 기준점")]
        [Tooltip("X는 카메라가 고정될 좌표, Y는 첫 방의 아래쪽 경계 좌표(월드). 예: (-3,-4)면 카메라는 X=-3에 항상 고정되고 " +
                 "Y는 -4를 아래쪽 경계로 하는 방(중심 4)부터 시작.")]
        [SerializeField] private Vector2 startRoomReference = new Vector2(-3f, -4f);

        [Header("전환 연출")]
        [SerializeField] private float scrollDuration = 0.4f;
        [SerializeField] private AnimationCurve scrollCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>방과 방 사이의 실제 간격 = 카메라 뷰포트 세로 크기. 겹침/빈틈 없이 정확히 타일 수만큼.</summary>
        private float RoomWorldSize => roomSizeInTiles * tileSize;

        /// <summary>방(0)의 아래쪽 경계 Y = startRoomReference.y 그대로. 보정 없음.</summary>
        private float GridOriginY => startRoomReference.y;

        private int currentRoomIndexY;
        private Coroutine scrollRoutine;

        private void Start()
        {
            currentRoomIndexY = RoomIndexOf(target.transform.position.y);
            // 스크롤 연출 없이 즉시 그 방 위치에서 시작한다.
            transform.position = RoomCenterPosition(currentRoomIndexY);
        }

        private void LateUpdate()
        {
            // 캐릭터의 물리 이동이 끝난 뒤(LateUpdate)에 방 경계를 넘었는지 매 프레임 검사한다.
            // 착지 여부와 상관없이, 방 경계를 넘는 즉시 스크롤을 시작한다. X는 항상 고정이라 검사하지 않는다.
            int newRoomIndexY = RoomIndexOf(target.transform.position.y);
            if (newRoomIndexY == currentRoomIndexY) return;

            currentRoomIndexY = newRoomIndexY;

            if (scrollRoutine != null)
            {
                StopCoroutine(scrollRoutine);
            }
            scrollRoutine = StartCoroutine(ScrollTo(RoomCenterPosition(currentRoomIndexY)));
        }

        private int RoomIndexOf(float worldY)
        {
            return Mathf.FloorToInt((worldY - GridOriginY) / RoomWorldSize);
        }

        private Vector3 RoomCenterPosition(int roomIndexY)
        {
            float y = GridOriginY + (roomIndexY + 0.5f) * RoomWorldSize;
            return new Vector3(startRoomReference.x, y, transform.position.z);
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
