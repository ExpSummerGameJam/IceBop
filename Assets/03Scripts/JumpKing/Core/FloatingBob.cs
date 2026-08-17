using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 사인파로 위아래를 오가며 둥실둥실 떠 있는 것처럼 보이게 하는 범용 컴포넌트.
    /// 타이틀 로고 등에 붙여서 쓴다. Awake 시점의 localPosition을 중심으로 위아래로만 움직인다.
    /// </summary>
    public class FloatingBob : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.3f;
        [SerializeField] private float cyclesPerSecond = 0.5f;

        private Vector3 basePosition;

        private void Awake()
        {
            basePosition = transform.localPosition;
        }

        private void Update()
        {
            float offset = Mathf.Sin(Time.time * cyclesPerSecond * Mathf.PI * 2f) * amplitude;
            transform.localPosition = basePosition + new Vector3(0f, offset, 0f);
        }
    }
}
