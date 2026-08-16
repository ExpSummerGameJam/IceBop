using UnityEngine;
using UnityEngine.InputSystem;

namespace IceBop.JumpKing
{
    /// <summary>
    /// 스킬 발동 입력을 처리한다. 지금은 "버튼 1개를 누르면 우선순위 순서대로
    /// CanActivate를 만족하는 첫 스킬을 발동"하는 컨텍스트 방식으로 잡아두었다.
    /// TODO: 스킬이 늘어나면 스킬별 개별 키 바인딩이 필요할 수도 있음 (기획 확정 필요).
    /// </summary>
    [RequireComponent(typeof(SkillInventory))]
    public class SkillInputController : MonoBehaviour
    {
        [SerializeField] private InputActionReference skillAction;
        [Tooltip("버튼을 눌렀을 때 이 순서대로 CanActivate를 검사해 첫 번째로 가능한 스킬을 발동한다.")]
        [SerializeField] private SkillSO[] skillsInPriorityOrder;

        private SkillInventory inventory;

        private void Awake()
        {
            inventory = GetComponent<SkillInventory>();
        }

        private void OnEnable() => skillAction.action.Enable();
        private void OnDisable() => skillAction.action.Disable();

        private void Update()
        {
            if (global::GamePauseState.IsPaused)
                return;

            if (!skillAction.action.WasPressedThisFrame()) return;

            foreach (var skill in skillsInPriorityOrder)
            {
                if (inventory.TryActivate(skill)) break;
            }
        }
    }
}
