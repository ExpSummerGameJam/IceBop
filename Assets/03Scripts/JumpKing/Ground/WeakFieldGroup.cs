using System.Collections.Generic;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 물리적으로 서로 맞닿은(인접한) 약한 바닥(D) 타일들이 공유하는 파손 카운터.
    /// PlatformSurface.BuildWeakGroups()가 씬 시작 시 인접한 타일들을 묶어 그룹을 만든다.
    /// 그룹 내 아무 타일이나 밟아도 카운트가 공유되고, 임계치에 도달하면 그룹 전체가 함께 파손된다.
    /// ScriptableObject가 아닌 순수 런타임 객체다 — 씬을 돌 때마다 새로 만들어지며 애셋에는 저장되지 않는다.
    /// </summary>
    public class WeakFieldGroup
    {
        private readonly List<PlatformSurface> members;
        private int hitCount;

        public bool IsBroken { get; private set; }

        public WeakFieldGroup(List<PlatformSurface> members)
        {
            this.members = members;
        }

        /// <summary>그룹 내 아무 멤버가 착지해도 호출된다.</summary>
        public void RegisterLanding(int breakAfterHits)
        {
            if (IsBroken) return;

            hitCount++;
            if (hitCount >= breakAfterHits)
            {
                Break();
            }
        }

        private void Break()
        {
            IsBroken = true;
            foreach (var member in members)
            {
                member.BreakVisual();
            }
        }

        /// <summary>스크롤 전환 시 그룹 전체를 원상 복구한다.</summary>
        public void Reset()
        {
            hitCount = 0;
            IsBroken = false;
            foreach (var member in members)
            {
                member.ResetVisual();
            }
        }
    }
}
