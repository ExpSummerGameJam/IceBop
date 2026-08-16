using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared "slide the stage off-screen, reveal the finished dish, slide back" animation used
/// by every finishing stage (stir, shake, fry, skewer, guided placement).
/// </summary>
public sealed class DishResultPresenter2D : MonoBehaviour
{
    [SerializeField] private float finishTravelDuration = 0.65f;
    [SerializeField] private float returnTravelDuration = 0.75f;

    private SpriteRenderer dishRenderer;
    private TextMesh dishLabel;
    private bool presenting;

    public event Action Presented;

    public void Play(Camera camera, DishDefinition dish, IReadOnlyList<CookingIngredientEntry> ingredients, Action hideInteractiveVisuals)
    {
        if (presenting)
            return;

        StartCoroutine(PresentRoutine(camera, dish, ingredients, hideInteractiveVisuals));
    }

    private IEnumerator PresentRoutine(
        Camera camera,
        DishDefinition dish,
        IReadOnlyList<CookingIngredientEntry> ingredients,
        Action hideInteractiveVisuals)
    {
        presenting = true;
        hideInteractiveVisuals?.Invoke();

        Vector3 startPosition = transform.position;
        Vector3 offscreenPosition = GetRightOffscreenPosition(camera, startPosition);
        yield return MoveTo(startPosition, offscreenPosition, finishTravelDuration);

        ShowDishResult(dish, ingredients);
        yield return MoveTo(offscreenPosition, startPosition, returnTravelDuration);

        Presented?.Invoke();
    }

    private IEnumerator MoveTo(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            t = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.position = to;
    }

    private Vector3 GetRightOffscreenPosition(Camera camera, Vector3 fallback)
    {
        Camera targetCamera = camera != null ? camera : Camera.main;
        if (targetCamera == null)
            return fallback + Vector3.right * 5f;

        float zDistance = Mathf.Abs(targetCamera.transform.position.z - fallback.z);
        Vector3 rightEdge = targetCamera.ViewportToWorldPoint(new Vector3(1.2f, 0.5f, zDistance));
        return new Vector3(rightEdge.x + 1.2f, fallback.y, fallback.z);
    }

    private void ShowDishResult(DishDefinition dish, IReadOnlyList<CookingIngredientEntry> ingredients)
    {
        if (dishRenderer == null)
        {
            GameObject dishObject = new("CompletedDish");
            dishObject.transform.SetParent(transform, false);
            dishObject.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            dishObject.transform.localScale = new Vector3(0.78f, 0.78f, 1f);
            dishRenderer = dishObject.AddComponent<SpriteRenderer>();
            dishRenderer.sortingOrder = 43;
        }

        dishRenderer.sprite = CookingRuntimeVisuals2D.GetDishSprite(dish, ingredients);
        dishRenderer.color = Color.white;
        dishRenderer.gameObject.SetActive(true);

        if (dishLabel == null)
        {
            GameObject labelObject = new("CompletedDishLabel");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, -0.96f, 0f);
            dishLabel = labelObject.AddComponent<TextMesh>();
            dishLabel.anchor = TextAnchor.MiddleCenter;
            dishLabel.alignment = TextAlignment.Center;
            dishLabel.fontSize = 34;
            dishLabel.characterSize = 0.055f;
            dishLabel.color = new Color(0.94f, 0.96f, 0.92f, 1f);

            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sortingOrder = 44;
        }

        dishLabel.text = dish != null && !string.IsNullOrWhiteSpace(dish.displayName)
            ? dish.displayName
            : "Completed Dish";
    }

    private void OnValidate()
    {
        finishTravelDuration = Mathf.Max(0.01f, finishTravelDuration);
        returnTravelDuration = Mathf.Max(0.01f, returnTravelDuration);
    }
}
