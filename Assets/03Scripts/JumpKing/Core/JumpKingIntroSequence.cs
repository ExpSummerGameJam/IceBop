using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 씬 시작 인트로 연출.
    /// 1) 캐릭터를 spawnRoomIndex(기본 14)번째 방 위치로 순간이동시키고, 회전을 풀어 빙글빙글 돌며 낙하시킨다.
    ///    낙하 중에는 floorCollider(맨 아래 바닥)를 제외한 모든 PlatformSurface와의 충돌을 무시하고,
    ///    공기저항(linearDamping)을 일시적으로 높여 너무 빨리 떨어지지 않게 한다.
    /// 2) 바닥에 닿으면 눕는다. 이때부터 스페이스(jumpAction) 입력을 직접 세어,
    ///    1~9번째 입력은 꿈틀(Wiggle) 연출, 10번째 입력에 기상 + 두리번(좌우 반전) 연출을 재생한다.
    /// 3) 연출이 끝나면 회전을 다시 고정하고, 무시했던 모든 발판과의 충돌을 정상 복구하고,
    ///    JumpKingInputController를 다시 켜서 평소 조작으로 돌아간다.
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

        [Header("낙하 연출")]
        [SerializeField] private int spawnRoomIndex = 4;
        [Tooltip("낙하를 시작할 맵 기준 X좌표.")]
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
        private bool isFalling;
        private bool isAwake;
        private float defaultLinearDamping;
        private float defaultGravityScale;

        private void Awake()
        {
            characterCollider = character.GetComponent<Collider2D>();

            // JumpKingCameraController.Start()보다 먼저 실행되도록(Awake는 모든 오브젝트에서 Start보다 항상 먼저 돈다)
            // 캐릭터를 spawnRoomIndex 위치로 여기서 순간이동시킨다.
            float spawnY = cameraController.GetRoomCenterY(spawnRoomIndex);
            Vector3 pos = character.transform.position;
            character.transform.position = new Vector3(spawnX, spawnY, pos.z);
        }

        private void Start()
        {
            inputController.enabled = false; // 인트로 동안 일반 점프 조작 차단 (내부적으로 액션들도 Disable됨)
            jumpAction.action.Enable(); // 웨이크업 카운트를 위해 우리가 직접 켠다 (위 줄보다 반드시 뒤에 있어야 함)

            character.Rb.constraints = RigidbodyConstraints2D.None; // 회전 허용
            character.Rb.angularVelocity = fallSpinDegreesPerSecond;

            defaultLinearDamping = character.Rb.linearDamping;
            character.Rb.linearDamping = fallLinearDamping; // 낙하 중에만 공기저항을 높여 너무 빨리 안 떨어지게

            defaultGravityScale = character.Rb.gravityScale;
            character.Rb.gravityScale = defaultGravityScale * fallGravityMultiplier; // 낙하 속도를 배로 빠르게

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

        private void Update()
        {
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
    }
}
