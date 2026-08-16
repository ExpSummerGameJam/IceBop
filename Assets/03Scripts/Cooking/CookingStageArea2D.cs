using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class CookingStageArea2D : MonoBehaviour
{
    [SerializeField] private CookingFlowController2D cookingFlowController;
    [SerializeField] private bool hideRendererOnPlay = true;

    private Collider2D areaCollider;

    public Collider2D AreaCollider
    {
        get
        {
            ResolveReferences();
            return areaCollider;
        }
    }

    private void Awake()
    {
        ResolveReferences();

        if (areaCollider != null)
            areaCollider.isTrigger = true;

        if (hideRendererOnPlay && TryGetComponent(out Renderer areaRenderer))
            areaRenderer.enabled = false;

        if (cookingFlowController != null)
            cookingFlowController.BindStageArea(areaCollider);
    }

    private void ResolveReferences()
    {
        if (areaCollider == null)
            areaCollider = GetComponent<Collider2D>();

        if (TryGetComponent(out SpriteRenderer renderer) && renderer.sprite == null)
            renderer.sprite = InventoryVisualUtility.WhiteSprite;

        if (cookingFlowController == null)
            cookingFlowController = FindFirstObjectByType<CookingFlowController2D>();
    }

    private void OnValidate()
    {
        if (areaCollider == null)
            areaCollider = GetComponent<Collider2D>();

        if (areaCollider != null)
            areaCollider.isTrigger = true;
    }

    private void OnDrawGizmos()
    {
        Collider2D targetCollider = areaCollider != null ? areaCollider : GetComponent<Collider2D>();
        if (targetCollider == null)
            return;

        Bounds bounds = targetCollider.bounds;
        Gizmos.color = new Color(0.24f, 0.62f, 0.92f, 0.18f);
        Gizmos.DrawCube(bounds.center, bounds.size);
        Gizmos.color = new Color(0.24f, 0.62f, 0.92f, 0.9f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
