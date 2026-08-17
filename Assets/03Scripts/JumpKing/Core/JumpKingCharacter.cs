using System;
using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 점프킹 캐릭터의 물리/상태를 소유하는 "모델" 역할.
    /// 입력은 여기서 직접 읽지 않는다 (JumpKingInputController가 입력을 읽고 이 클래스를 호출한다).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class JumpKingCharacter : MonoBehaviour
    {
        [Header("점프 튜닝")]
        [SerializeField] private float minJumpPower = 4f;
        [SerializeField] private float maxJumpPower = 14f;

        [Header("충돌 반발력")]
        [Tooltip("옆면 충돌 시 Unity 물리 엔진이 직접 튕겨내는 정도. 0=안 튕김, 1=완전 탄성. " +
                 "이 값은 캐릭터 전체에 균일하게 적용된다 (발판 종류별로 다르게 줄 수 없음).")]
        [Range(0f, 1f)]
        [SerializeField] private float bounciness = 0.6f;

        public Rigidbody2D Rb { get; private set; }
        public JumpKingMoveState State { get; private set; } = JumpKingMoveState.Idle;
        public PlatformSurface CurrentSurface { get; private set; }
        public IGroundBehaviour CurrentGround { get; private set; }

        /// <summary>지금 점프 차지/발사 조작이 가능한 상태인지. 텅 앵커로 고정된 상태에서도 가능하다.</summary>
        public bool CanControlJump =>
            (State == JumpKingMoveState.Idle && CurrentGround != null && CurrentGround.AllowsJumpControl) ||
            State == JumpKingMoveState.Anchored;

        /// <summary>착지 시 발생. 인자는 착지한 발판.</summary>
        public event Action<PlatformSurface> OnLanded;

        /// <summary>공중으로 진입할 때(점프 실행/낙하 시작) 발생.</summary>
        public event Action OnAirborne;

        private float defaultGravityScale;
        private float anchorTimer;

        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            defaultGravityScale = Rb.gravityScale;
            // 빠른 낙하/점프 속도에서 얇은 바닥 콜라이더를 그냥 통과(터널링)하는 걸 막기 위해
            // 인스펙터 설정과 무관하게 항상 연속 충돌 감지를 강제한다.
            Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            // 정지/미끄러짐은 우리 스크립트가 직접 제어하므로 마찰은 0으로 강제한다.
            // 옆면 충돌 시 튕겨나가는 건 Unity 물리 엔진의 Bounciness에 맡긴다.
            Rb.sharedMaterial = new PhysicsMaterial2D(name + "_Physics") { friction = 0f, bounciness = bounciness };
        }

        private void FixedUpdate()
        {
            if (CurrentGround != null && State != JumpKingMoveState.Airborne)
            {
                CurrentGround.OnPhysicsStep(this, CurrentSurface);
            }

            if (State == JumpKingMoveState.Anchored)
            {
                anchorTimer -= Time.fixedDeltaTime;
                if (anchorTimer <= 0f)
                {
                    SetAnchored(false);
                }
            }
        }

        /// <summary>
        /// JumpKingInputController가 스페이스를 뗄 때 호출한다.
        /// direction: 방향키로 결정된 방향, chargeRatio01: 0~1 차지 비율.
        /// </summary>
        public void ExecuteJump(Vector2 direction, float chargeRatio01)
        {
            // TODO: 미끄러운 필드(C)에서 발사하는 경우, 기존 관성을 그대로 덮어쓸지
            // 합산할지는 기획에 맞춰 결정 필요. 지금은 항상 덮어씀.
            float power = Mathf.Lerp(minJumpPower, maxJumpPower, chargeRatio01);
            Rb.linearVelocity = direction.normalized * power;
            EnterAirborne();
        }

        public void EnterAirborne()
        {
            State = JumpKingMoveState.Airborne;
            CurrentGround = null;
            CurrentSurface = null;
            Rb.gravityScale = defaultGravityScale; // 앵커로 중력을 꺼뒀을 수도 있으니 항상 복구
            OnAirborne?.Invoke();
        }

        /// <summary>
        /// 텅 앵커 스킬에서 벽에 고정시킬 때 사용. anchored == true면 duration초 뒤 자동으로 풀린다
        /// (그 전에 점프하면 ExecuteJump -> EnterAirborne이 먼저 상태를 Airborne으로 바꿔 자동 해제와 무관해진다).
        /// </summary>
        public void SetAnchored(bool anchored, float duration = 0f)
        {
            if (anchored)
            {
                State = JumpKingMoveState.Anchored;
                Rb.linearVelocity = Vector2.zero;
                Rb.gravityScale = 0f;
                anchorTimer = duration;
            }
            else
            {
                Rb.gravityScale = defaultGravityScale;
                if (State == JumpKingMoveState.Anchored)
                {
                    State = JumpKingMoveState.Airborne;
                }
            }
        }

        [Header("충돌 판정")]
        [Tooltip("충돌 노멀과 위쪽(Vector2.up)의 내적이 이 값을 넘어야 '착지'로 취급한다. " +
                 "그보다 낮으면(옆면/아랫면) OnSideHit으로 처리된다.")]
        [SerializeField] private float landingNormalThreshold = 0.5f;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var surface = collision.collider.GetComponent<PlatformSurface>();
            if (surface == null) return;

            bool isTopHit = TryGetMostUpwardContact(collision, out Vector2 normal, out Vector2 point);

            if (!isTopHit)
            {
                GroundBehaviourFactory.Get(surface.Type).OnSideHit(this, surface, normal);
                return;
            }

            CurrentSurface = surface;
            CurrentGround = GroundBehaviourFactory.Get(surface.Type);
            State = CurrentGround.AllowsJumpControl ? JumpKingMoveState.Idle : JumpKingMoveState.Sliding;

            CurrentGround.OnLand(this, surface, point);
            OnLanded?.Invoke(surface);
        }

        /// <summary>
        /// L자 코너처럼 여러 발판이 동시에 접촉하는 상황에서는, OnCollisionEnter2D가 호출되는
        /// 그 순간 하필 이 발판의 접촉 노멀이 코너 때문에 애매하게(옆면처럼) 잡혀 착지가 무산될 수 있다.
        /// 아직 다른 곳에 착지하지 못한 상태라면, 접촉이 유지되는 동안 매 스텝 재시도해서
        /// 뒤늦게라도 위쪽 노멀이 확인되면 착지 처리한다.
        /// </summary>
        private void OnCollisionStay2D(Collision2D collision)
        {
            if (CurrentSurface != null) return; // 이미 다른 곳에 착지해 있으면 재검사 불필요

            // 막 점프해서 위로 솟구치는 중이면 재착지 처리를 하지 않는다.
            // 점프 직후에도 콜라이더가 물리적으로 아직 안 떨어진 스텝이 있는데, 이 가드 없이는
            // 방금 준 점프 속도를 여기서 다시 0으로 죽여버려 점프가 거의 안 되는 것처럼 보인다.
            if (Rb.linearVelocity.y > 0f) return;

            var surface = collision.collider.GetComponent<PlatformSurface>();
            if (surface == null) return;
            if (!TryGetMostUpwardContact(collision, out _, out Vector2 point)) return;

            CurrentSurface = surface;
            CurrentGround = GroundBehaviourFactory.Get(surface.Type);
            State = CurrentGround.AllowsJumpControl ? JumpKingMoveState.Idle : JumpKingMoveState.Sliding;

            CurrentGround.OnLand(this, surface, point);
            OnLanded?.Invoke(surface);
        }

        /// <summary>
        /// 접촉점이 여러 개일 수 있으므로(모서리 등) 그중 가장 위쪽에 가까운 접촉(노멀+지점)을 찾는다.
        /// 그 노멀조차 기준(landingNormalThreshold)을 못 넘으면 착지가 아니라 옆면/아랫면 충돌로 판정한다.
        /// </summary>
        private bool TryGetMostUpwardContact(Collision2D collision, out Vector2 bestNormal, out Vector2 bestPoint)
        {
            ContactPoint2D contact = collision.GetContact(0);
            bestNormal = contact.normal;
            bestPoint = contact.point;
            float bestDot = Vector2.Dot(bestNormal, Vector2.up);

            for (int i = 1; i < collision.contactCount; i++)
            {
                contact = collision.GetContact(i);
                float dot = Vector2.Dot(contact.normal, Vector2.up);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestNormal = contact.normal;
                    bestPoint = contact.point;
                }
            }

            return bestDot > landingNormalThreshold;
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            var surface = collision.collider.GetComponent<PlatformSurface>();
            if (surface == null || surface != CurrentSurface) return;

            CurrentGround?.OnLeave(this, surface);
            CurrentSurface = null;
            CurrentGround = null;
        }
    }
}
