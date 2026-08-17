using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 씬 시작 인트로 연출.
    /// 0) waitForAnyKeyToStart가 켜져 있으면, 스폰 위치에 캐릭터를 완전히 정지시켜둔 채
    ///    아무 키나 눌러야 낙하가 시작된다 (타이틀 화면 등에서 사용). 꺼두면 씬 시작과 동시에 바로 낙하한다.
    /// 1) 캐릭터를 spawnAboveReference(설정 시) 또는 spawnRoomIndex(기본)번째 방 위치로 순간이동시키고,
    ///    회전을 풀어 빙글빙글 돌며 낙하시킨다. 낙하 중에는 floorCollider(맨 아래 바닥)를 제외한 모든
    ///    PlatformSurface와의 충돌을 무시하고, 공기저항/중력을 일시적으로 조정한다.
    /// 2) 바닥에 닿으면 눕는다. 이때부터 스페이스(jumpAction) 입력을 직접 세어,
    ///    1~9번째 입력은 꿈틀(Wiggle) 연출, 10번째 입력에 기상 + 두리번(좌우 반전) 연출을 재생한다.
    /// 3) 연출이 끝나면 회전을 다시 고정하고, 무시했던 모든 발판과의 충돌을 정상 복구하고,
    ///    JumpKingInputController를 다시 켜서 평소 조작으로 돌아간다.
    /// 카메라가 처음 스크롤(방 전환)되는 순간 hideOnFirstScroll(타이틀 로고 등)을 비활성화한다.
    /// </summary>
    public class JumpKingIntroSequence : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private JumpKingCharacter character;
        [SerializeField] private JumpKingInputController inputController;
        [SerializeField] private JumpKingCameraController cameraController;
        [Tooltip("낙하 중 유일하게 충돌을 유지할 맨 아래 바닥 콜라이더.")]
        [SerializeField] private Collider2D floorCollider;
        [Tooltip("웨이크업 입력 카운트에 쓸 액션. 보통 Player/Jump(스페이스)와 동일한 참조를 넣는다.")]
        [SerializeField] private InputActionReference jumpAction;

        [Header("타이틀 화면 연동 (선택)")]
        [Tooltip("켜두면 씬 시작과 동시에 낙하하지 않고, 아무 키나 누를 때까지 캐릭터를 스폰 위치에 " +
                 "완전히 정지시켜둔다 (중력도 안 받음). 타이틀 화면에서 사용.")]
        [SerializeField] private bool waitForAnyKeyToStart;
        [Tooltip("설정하면 이 Transform 바로 위에서 낙하를 시작한다 (예: 타이틀 로고). " +
                 "비워두면 기존처럼 spawnRoomIndex/spawnX 기준으로 스폰한다.")]
        [SerializeField] private Transform spawnAboveReference;
        [SerializeField] private float spawnHeightAboveReference = 3f;
        [Tooltip("카메라가 처음 스크롤(방 전환)되는 순간 비활성화할 오브젝트 (예: 타이틀 로고).")]
        [SerializeField] private GameObject hideOnFirstScroll;

        [Header("낙하 연출")]
        [SerializeField] private int spawnRoomIndex = 4;
        [Tooltip("낙하를 시작할 맵 기준 X좌표. spawnAboveReference를 쓰면 무시된다.")]
        [SerializeField] private float spawnX = -3f;
        [SerializeField] private float fallSpinDegreesPerSecond = 360f;
        [Tooltip("낙하 중에만 적용할 공기저항(Rigidbody2D.linearDamping). 착지하면 원래 값으로 복구된다.")]
        [SerializeField] private float fallLinearDamping = 0f;
        [Tooltip("낙하 중에만 중력에 곱할 배율. 착지하면 원래 값으로 복구된다.")]
        [SerializeField] private float fallGravityMultiplier = 3f;

        [Header("웨이크업")]
        [Tooltip("꿈틀 연출이 나오는 입력 횟수. 이 다음 입력(=이 값+1번째)에 기상한다.")]
        [SerializeField] private int wiggleCount = 9;
        [SerializeField] private float lookAroundFlipInterval = 0.2f;
        [SerializeField] private int lookAroundFlipCount = 4;

        [Header("연출 훅 (선택)")]
        [Tooltip("실제 애니메이션이 준비되면 Animator를 연결해 Wiggle/StandUp 트리거를 쏘는 데 쓴다.")]
        [SerializeField] private Animator animator;

        private Collider2D characterCollider;
        private int wakeUpPresses;
        private bool hasStarted;
        private bool isFalling;
        private bool isAwake;
        private float defaultLinearDamping;
        private float defaultGravityScale;

        private void Awake()
        {
            characterCollider = character.GetComponent<Collider2D>();

            // 카메라 등 다른 오브젝트의 Start()보다 먼저 실행되도록(Awake는 모든 오브젝트에서 Start보다
            // 항상 먼저 돈다) 캐릭터를 스폰 위치로 여기서 순간이동시킨다.
            Vector3 pos = character.transform.position;
            float x;
            float y;

            if (spawnAboveReference != null)
            {
                x = spawnAboveReference.position.x;
                y = spawnAboveReference.position.y + spawnHeightAboveReference;
            }
            else
            {
                x = spawnX;
                y = cameraController.GetRoomCenterY(spawnRoomIndex);
            }

            character.transform.position = new Vector3(x, y, pos.z);

            if (waitForAnyKeyToStart)
            {
                character.Rb.simulated = false; // 키 누르기 전까지 중력을 포함해 물리 시뮬레이션 자체를 정지

                var renderer = character.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = false; // 키 누르기 전까지 캐릭터를 아예 안 보이게
                }
            }
        }

        private void OnEnable()
        {
            ScrollManager.OnScrollChanged += HandleFirstScroll;
        }

        private void OnDisable()
        {
            ScrollManager.OnScrollChanged -= HandleFirstScroll;
        }

        private void Start()
        {
            if (!waitForAnyKeyToStart)
            {
                BeginFall();
            }
        }

        private void Update()
        {
            if (!hasStarted)
            {
                if (AnyKeyPressed())
                {
                    BeginFall();
                }
                return;
            }

            if (isFalling || isAwake) return;
            if (!jumpAction.action.WasPressedThisFrame()) return;

            wakeUpPresses++;

            if (wakeUpPresses <= wiggleCount)
            {
                if (animator != null) animator.SetTrigger("Wiggle");
                // TODO: 실제 꿈틀 연출(스프라이트 애니메이션 등)이 준비되면 여기서 같이 재생.
            }
            else
            {
                StartCoroutine(WakeUpRoutine());
            }
        }

        private bool AnyKeyPressed()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            return false;
        }

        /// <summary>낙하를 실제로 시작한다. waitForAnyKeyToStart가 꺼져 있으면 Start()에서 바로 호출된다.</summary>
        public void BeginFall()
        {
            if (hasStarted) return;
            hasStarted = true;

            character.Rb.simulated = true; // 대기 중 꺼뒀을 수 있으니 다시 켠다

            var renderer = character.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true; // 키를 눌렀으니 이제부터 보이게
            }

            inputController.enabled = false; // 인트로 동안 일반 점프 조작 차단 (내부적으로 액션들도 Disable됨)
            jumpAction.action.Enable(); // 웨이크업 카운트를 위해 우리가 직접 켠다 (위 줄보다 반드시 뒤에 있어야 함)

            character.Rb.constraints = RigidbodyConstraints2D.None; // 회전 허용
            character.Rb.angularVelocity = fallSpinDegreesPerSecond;

            defaultLinearDamping = character.Rb.linearDamping;
            character.Rb.linearDamping = fallLinearDamping; // 낙하 중에만 공기저항을 조정

            defaultGravityScale = character.Rb.gravityScale;
            character.Rb.gravityScale = defaultGravityScale * fallGravityMultiplier; // 낙하 속도 배율

            SetIgnoreAllPlatformsExceptFloor(true);

            character.OnLanded += HandleFallLanded;
            isFalling = true;
        }

        private void HandleFallLanded(PlatformSurface surface)
        {
            if (!isFalling) return;

            character.OnLanded -= HandleFallLanded;
            isFalling = false;

            character.Rb.linearVelocity = Vector2.zero;
            character.Rb.angularVelocity = 0f;
            character.Rb.linearDamping = defaultLinearDamping; // 공기저항 원상 복구
            character.Rb.gravityScale = defaultGravityScale; // 중력 배율 원상 복구
            character.transform.rotation = Quaternion.Euler(0f, 0f, 90f); // 누운 자세
        }

        private IEnumerator WakeUpRoutine()
        {
            isAwake = true;

            if (animator != null) animator.SetTrigger("StandUp");
            character.transform.rotation = Quaternion.identity; // 기상

            var spriteRenderer = character.GetComponentInChildren<SpriteRenderer>();
            for (int i = 0; i < lookAroundFlipCount; i++)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = !spriteRenderer.flipX;
                }
                yield return new WaitForSeconds(lookAroundFlipInterval);
            }

            FinishIntro();
        }

        private void FinishIntro()
        {
            character.Rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 회전 다시 잠금
            SetIgnoreAllPlatformsExceptFloor(false); // 모든 발판과의 충돌 정상 복구
            inputController.enabled = true; // 평소 조작으로 복귀
            enabled = false; // 인트로 스크립트는 더 할 일 없음
        }

        private void SetIgnoreAllPlatformsExceptFloor(bool ignore)
        {
            var surfaces = FindObjectsByType<PlatformSurface>(FindObjectsSortMode.None);
            foreach (var surface in surfaces)
            {
                var col = surface.GetComponent<Collider2D>();
                if (col == null || col == floorCollider) continue;

                Physics2D.IgnoreCollision(characterCollider, col, ignore);
            }
        }

        /// <summary>카메라가 처음 스크롤(방 전환)되는 순간 1회만 실행되어 타이틀 로고 등을 숨긴다.</summary>
        private void HandleFirstScroll()
        {
            if (hideOnFirstScroll != null)
            {
                hideOnFirstScroll.SetActive(false);
            }

            ScrollManager.OnScrollChanged -= HandleFirstScroll;
        }
    }
}
