using System;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 화면(스크롤) 전환 시점을 브로드캐스트하는 정적 이벤트 허브.
    /// D. 약한 바닥(WeakTilemap)이 OnScrollChanged를 구독해 파손 상태를 초기화한다.
    /// </summary>
    public static class ScrollManager
    {
        /// <summary>스크롤(방 전환)이 일어날 때마다 발생.</summary>
        public static event Action OnScrollChanged;

        /// <summary>카메라가 맨 아래(시작) 방으로 돌아왔을 때만 발생. 과일/채집물 등 "시작 지점 복귀 시 초기화"용.</summary>
        public static event Action OnReturnedToStartRoom;

        public static void NotifyScrollChanged()
        {
            OnScrollChanged?.Invoke();
        }

        public static void NotifyReturnedToStartRoom()
        {
            OnReturnedToStartRoom?.Invoke();
        }
    }
}
