using UnityEngine;
using UnityEngine.InputSystem;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 입력을 읽어 JumpKingCharacter에 전달하는 컨트롤러.
    /// 스페이스 홀드 = 점프 높이 차지, 방향키 = 점프 방향, 스페이스를 떼면 점프 실행.
    /// New Input System(Player/Move, Player/Jump 액션)을 사용한다.
    /// </summary>
    [RequireComponent(typeof(JumpKingCharacter))]
    public class JumpKingInputController : MonoBehaviour
    {
        [Header("Input Actions (Player/Move, Player/Jump 참조)")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;

        [Header("차지 튜닝")]
        [SerializeField] private float maxChargeSeconds = 1.2f;
        [SerializeField] private AnimationCurve chargeCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("방향 튜닝")]
        [Tooltip("방향키 좌우 입력에 곱해지는 값. 1보다 작을수록 옆으로 덜 날아감 (세로 성분은 항상 1로 고정).")]
        [Range(0f, 1f)]
        [SerializeField] private float horizontalSensitivity = 0.6f;

        [Header("스프라이트")]
        [Tooltip("비워두면 자식에서 자동으로 찾는다. 기본 스프라이트가 왼쪽을 보고 있다고 가정.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private JumpKingCharacter character;
        private float chargeTimer;
        private bool isCharging;

        /// <summary>차지하는 동안 감지된 마지막 좌우 입력값. 스페이스보다 방향키를 먼저 떼도 방향이 씹히지 않도록 버퍼링한다.</summary>
        private float chargeHorizontal;

        /// <summary>0~1 차지 비율. 애니메이션/UI에서 참조.</summary>
        public float ChargeRatio01 { get; private set; }
        public bool IsCharging => isCharging;

        private void Awake()
        {
            character = GetComponent<JumpKingCharacter>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            moveAction.action.Enable();
            jumpAction.action.Enable();
        }

        private void OnDisable()
        {
            moveAction.action.Disable();
            jumpAction.action.Disable();
        }

        private void Update()
        {
            UpdateFacing();

            if (jumpAction.action.WasPressedThisFrame() && character.CanControlJump)
            {
                BeginCharge();
            }

            if (isCharging)
            {
                chargeTimer += Time.deltaTime;
                ChargeRatio01 = chargeCurve.Evaluate(Mathf.Clamp01(chargeTimer / maxChargeSeconds));

                float rawHorizontal = moveAction.action.ReadValue<Vector2>().x;
                if (Mathf.Abs(rawHorizontal) > 0.01f)
                {
                    chargeHorizontal = rawHorizontal;
                }
            }

            if (jumpAction.action.WasReleasedThisFrame() && isCharging)
            {
                ReleaseJump();
            }
        }

        /// <summary>
        /// 이동은 없지만, 방향키를 누르면 캐릭터가 그 방향을 바라보도록 스프라이트를 뒤집는다.
        /// 땅/공중 상관없이 매 프레임 갱신한다.
        /// </summary>
        private void UpdateFacing()
        {
            if (spriteRenderer == null) return;

            float horizontal = moveAction.action.ReadValue<Vector2>().x;
            if (Mathf.Abs(horizontal) > 0.01f)
            {
                spriteRenderer.flipX = horizontal > 0f;
            }
        }

        private void BeginCharge()
        {
            isCharging = true;
            chargeTimer = 0f;
            ChargeRatio01 = 0f;
            chargeHorizontal = 0f;
            // TODO: 차지 시작 연출을 위한 이벤트가 필요하면 여기서 발행 (JumpKingAnimationDriver가 구독)
        }

        private void ReleaseJump()
        {
            isCharging = false;
            Vector2 direction = ReadDirection();
            character.ExecuteJump(direction, ChargeRatio01);
            ChargeRatio01 = 0f;
        }

        /// <summary>
        /// 차지하는 동안 감지됐던 좌우 입력(chargeHorizontal)으로 점프 방향을 결정한다.
        /// 릴리즈 순간의 값만 보지 않기 때문에, 방향키를 스페이스보다 살짝 먼저 떼도 방향이 반영된다.
        /// 수직(높이) 성분은 항상 위쪽으로 고정해, 방향키만 눌러도 "옆으로만 날아가는" 현상을 막는다.
        /// </summary>
        private Vector2 ReadDirection()
        {
            return new Vector2(chargeHorizontal * horizontalSensitivity, 1f);
        }
    }
}
