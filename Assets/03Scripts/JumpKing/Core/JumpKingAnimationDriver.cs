using UnityEngine;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 캐릭터 상태/차지 비율을 Animator 파라미터로 반영한다.
    /// 예정 파라미터: "IsCharging"(bool), "ChargeRatio"(float),
    /// "VerticalVelocity"(float), "Jump"(trigger), "Land"(trigger).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(JumpKingCharacter))]
    [RequireComponent(typeof(JumpKingInputController))]
    public class JumpKingAnimationDriver : MonoBehaviour
    {
        private Animator animator;
        private JumpKingCharacter character;
        private JumpKingInputController input;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            character = GetComponent<JumpKingCharacter>();
            input = GetComponent<JumpKingInputController>();
        }

        private void OnEnable()
        {
            character.OnLanded += HandleLanded;
            character.OnAirborne += HandleAirborne;
        }

        private void OnDisable()
        {
            character.OnLanded -= HandleLanded;
            character.OnAirborne -= HandleAirborne;
        }

        private void Update()
        {
            // TODO: animator.SetBool("IsCharging", input.IsCharging);
            // TODO: animator.SetFloat("ChargeRatio", input.ChargeRatio01);
            // TODO: animator.SetFloat("VerticalVelocity", character.Rb.linearVelocity.y);
        }

        private void HandleLanded(PlatformSurface surface)
        {
            // TODO: animator.SetTrigger("Land");
        }

        private void HandleAirborne()
        {
            // TODO: animator.SetTrigger("Jump");
        }
    }
}
