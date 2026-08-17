using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Shared "slide the stage off-screen, reveal the finished dish, slide back" animation used
/// by every finishing stage (stir, shake, fry, skewer, guided placement).
/// </summary>
public sealed class DishResultPresenter2D : MonoBehaviour
{
    private const string DishLabelChildPath = "CompletedDishLabel";

    [SerializeField] private float finishTravelDuration = 0.65f;
    [SerializeField] private float returnTravelDuration = 0.75f;
    [SerializeField] private TMP_Text dishLabel;

    private SpriteRenderer dishRenderer;
    private bool presenting;
    private bool loggedMissingVisuals;

    public event Action Presented;

    // If the finishing-stage GameObject gets deactivated mid-presentation (e.g. a new cook
    // starts before this one finished presenting), the coroutine dies with it but the flag
    // must still reset - otherwise a pooled/reused presenter would refuse to ever Play() again.
    private void OnDisable()
    {
        presenting = false;
    }

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

        presenting = false;
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
        // The dish sprite itself is still built here - only the name label is a placed object.
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

        ResolveDishLabel();
        if (dishLabel == null)
            return;

        dishLabel.gameObject.SetActive(true);
        dishLabel.text = dish != null && !string.IsNullOrWhiteSpace(dish.displayName)
            ? dish.displayName
            : "Completed Dish";
    }

    private void ResolveDishLabel()
    {
        if (dishLabel != null)
            return;

        Transform labelTransform = transform.Find(DishLabelChildPath);
        if (labelTransform != null)
            dishLabel = labelTransform.GetComponent<TMP_Text>();

        if (dishLabel != null || loggedMissingVisuals)
            return;

        loggedMissingVisuals = true;
        Debug.LogError(
            $"DishResultPresenter2D needs a placed \"{DishLabelChildPath}\" (TextMeshPro) object " +
            "under this finishing stage. Rebuild the rig via IceBop/Build Cooking Stage Rig.",
            this);
    }

    private void OnValidate()
    {
        finishTravelDuration = Mathf.Max(0.01f, finishTravelDuration);
        returnTravelDuration = Mathf.Max(0.01f, returnTravelDuration);
    }
}
