using System;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 화면(스크롤) 전환 시점을 브로드캐스트하는 정적 이벤트 허브.
    /// D. 약한 바닥(PlatformSurface)이 이 이벤트를 구독해 파손 상태를 초기화한다.
    /// TODO: 실제 스크롤(카메라 룸 전환) 트리거가 만들어지면 그 지점에서 NotifyScrollChanged() 호출.
    /// </summary>
    public static class ScrollManager
    {
        public static event Action OnScrollChanged;

        public static void NotifyScrollChanged()
        {
            OnScrollChanged?.Invoke();
        }
    }
}
