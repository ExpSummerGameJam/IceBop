using System.Collections;
using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 점프킹 스타일 카메라.
    /// X축: 공중/착지 상관없이 매 프레임 캐릭터를 따라가 항상 화면 중앙에 위치시킨다.
    /// Y축: 평소엔 고정되어 있다가, 캐릭터가 새로운 방(수직 구간)에 착지하는 순간에만 스크롤 이동한다.
    /// 실제로 방(Y)이 바뀌었을 때만 ScrollManager.NotifyScrollChanged()를 호출해
    /// 약한 바닥(D) 초기화 등을 트리거한다 — 같은 방 안에서 착지만 반복해서는 초기화되지 않는다.
    /// </summary>
    public class JumpKingCameraController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private JumpKingCharacter target;

        [Header("방(Room) 세로 크기")]
        [Tooltip("카메라 한 화면이 담당하는 세로 월드 크기. 보통 orthographicSize * 2 로 맞춘다.")]
        [SerializeField] private float roomHeight = 9f;

        [Header("세로 앵커")]
        [Tooltip("0이면 방 정중앙에 캐릭터/바닥이 옴. 값을 키우면 카메라가 위로 당겨져서 " +
                 "바닥이 화면 아래쪽으로 내려가고 위쪽(점프 경로)이 더 넓게 보인다. 범위 -0.5~0.5.")]
        [Range(-0.5f, 0.5f)]
        [SerializeField] private float verticalAnchorRatio = 0.15f;

        [Header("가로 추적")]
        [Tooltip("X축을 얼마나 부드럽게 따라갈지(초). 0에 가까울수록 캐릭터 위치에 즉시 딱 붙는다.")]
        [SerializeField] private float horizontalSmoothTime = 0.08f;

        [Header("전환 연출")]
        [SerializeField] private float scrollDuration = 0.4f;
        [SerializeField] private AnimationCurve scrollCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private int currentRoomIndexY;
        private float xVelocity;
        private Coroutine scrollRoutine;

        private void OnEnable()
        {
            target.OnLanded += HandleLanded;
        }

        private void OnDisable()
        {
            target.OnLanded -= HandleLanded;
        }

        private void Start()
        {
            // 시작 시점엔 연출 없이 캐릭터가 속한 방으로 즉시 고정한다.
            currentRoomIndexY = RoomIndexOf(target.transform.position.y);
            float anchorY = AnchorY(currentRoomIndexY);
            transform.position = new Vector3(target.transform.position.x, anchorY, transform.position.z);
        }

        private void Update()
        {
            // X축은 공중/착지 상관없이 항상 캐릭터를 따라가 화면 중앙에 둔다.
            float smoothedX = Mathf.SmoothDamp(transform.position.x, target.transform.position.x, ref xVelocity, horizontalSmoothTime);
            transform.position = new Vector3(smoothedX, transform.position.y, transform.position.z);
        }

        private void HandleLanded(PlatformSurface surface)
        {
            int newRoomIndexY = RoomIndexOf(target.transform.position.y);
            if (newRoomIndexY == currentRoomIndexY) return; // 같은 방이면 스크롤 불필요

            currentRoomIndexY = newRoomIndexY;

            if (scrollRoutine != null)
            {
                StopCoroutine(scrollRoutine);
            }
            scrollRoutine = StartCoroutine(ScrollYTo(AnchorY(newRoomIndexY)));
        }

        private int RoomIndexOf(float worldY) => Mathf.FloorToInt(worldY / roomHeight);

        private float AnchorY(int roomIndexY)
        {
            float roomBottom = roomIndexY * roomHeight;
            return roomBottom + roomHeight * (0.5f + verticalAnchorRatio);
        }

        private IEnumerator ScrollYTo(float destinationY)
        {
            float startY = transform.position.y;
            float t = 0f;

            while (t < scrollDuration)
            {
                t += Time.deltaTime;
                float eval = scrollCurve.Evaluate(Mathf.Clamp01(t / scrollDuration));
                transform.position = new Vector3(transform.position.x, Mathf.Lerp(startY, destinationY, eval), transform.position.z);
                yield return null;
            }

            transform.position = new Vector3(transform.position.x, destinationY, transform.position.z);
            ScrollManager.NotifyScrollChanged();
        }
    }
}
