using System.Collections.Generic;

namespace IceBop.JumpKing
{
    /// <summary>
    /// PlatformType -> IGroundBehaviour 매핑.
    /// 구현체가 무상태(stateless)이므로 타입당 인스턴스 하나를 재사용한다.
    /// </summary>
    public static class GroundBehaviourFactory
    {
        private static readonly Dictionary<PlatformType, IGroundBehaviour> behaviours =
            new Dictionary<PlatformType, IGroundBehaviour>
            {
                { PlatformType.Hard, new HardFieldBehaviour() },
                { PlatformType.Slope, new SlopeBehaviour() },
                { PlatformType.Slippery, new SlipperyFieldBehaviour() },
                { PlatformType.Weak, new WeakFieldBehaviour() },
            };

        public static IGroundBehaviour Get(PlatformType type) => behaviours[type];
    }
}
