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

        public Rigidbody2D Rb { get; private set; }
        public JumpKingMoveState State { get; private set; } = JumpKingMoveState.Idle;
        public PlatformSurface CurrentSurface { get; private set; }
        public IGroundBehaviour CurrentGround { get; private set; }

        /// <summary>지금 점프 차지/발사 조작이 가능한 상태인지.</summary>
        public bool CanControlJump =>
            State == JumpKingMoveState.Idle &&
            CurrentGround != null &&
            CurrentGround.AllowsJumpControl;

        /// <summary>착지 시 발생. 인자는 착지한 발판.</summary>
        public event Action<PlatformSurface> OnLanded;

        /// <summary>공중으로 진입할 때(점프 실행/낙하 시작) 발생.</summary>
        public event Action OnAirborne;

        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (CurrentGround != null && State != JumpKingMoveState.Airborne)
            {
                CurrentGround.OnPhysicsStep(this, CurrentSurface);
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

        /// <summary>악셀 점프 등 "공중에서의 추가 점프"를 위한 진입점.</summary>
        public void ExecuteAirJump(Vector2 direction, float power)
        {
            // TODO: Rb.linearVelocity = direction.normalized * power;
            // 호출 전 사용 가능 여부 확인은 SkillSO.CanActivate(AccelJumpSkillSO)에서 처리됨.
        }

        public void EnterAirborne()
        {
            State = JumpKingMoveState.Airborne;
            CurrentGround = null;
            CurrentSurface = null;
            OnAirborne?.Invoke();
        }

        /// <summary>텅 앵커 스킬 등에서 벽에 고정시킬 때 사용.</summary>
        public void SetAnchored(bool anchored)
        {
            // TODO: anchored == true  -> State = Anchored, 속도 0으로 고정, 위치를 벽면에 붙임
            //       anchored == false -> Airborne으로 복귀 (재점프 입력 대기)
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var surface = collision.collider.GetComponent<PlatformSurface>();
            if (surface == null || surface.IsBroken) return;

            CurrentSurface = surface;
            CurrentGround = GroundBehaviourFactory.Get(surface.Type);
            State = CurrentGround.AllowsJumpControl ? JumpKingMoveState.Idle : JumpKingMoveState.Sliding;

            CurrentGround.OnLand(this, surface);
            OnLanded?.Invoke(surface);
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
