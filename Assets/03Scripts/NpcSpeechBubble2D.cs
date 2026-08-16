using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public sealed class NpcSpeechBubble2D : MonoBehaviour
{
    public const string DefaultBubblePrefabResourcePath = "Prefabs/UI/NpcSpeechBubble";

    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 worldOffset = new(0f, 1.35f, 0f);
    [SerializeField] private Vector3 bubblePositionCorrection = new(-0.2f, 0f, 0f);

    [SerializeField] private GameObject bubblePrefab;
    [SerializeField] private string bubblePrefabResourcePath = DefaultBubblePrefabResourcePath;

    [SerializeField] private int wrapCharacters = 18;
    [SerializeField] private float charactersPerSecond = 42f;
    [SerializeField] private float autoHideDelay = 1.15f;

    private readonly Queue<string> queuedLines = new();
    private readonly StringBuilder wrapBuilder = new(128);
    private readonly StringBuilder displayBuilder = new(128);

    private Transform bubbleRoot;
    private Text bubbleText;
    private Coroutine speechRoutine;

    private void Awake()
    {
        if (followTarget == null)
            followTarget = transform;

        EnsureVisuals();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (bubbleRoot == null || followTarget == null)
            return;

        bubbleRoot.position =
            followTarget.position +
            worldOffset +
            bubblePositionCorrection;
    }

    public void Speak(string message)
    {
        Speak(message, autoHideDelay);
    }

    public void Speak(string message, float hideDelay)
    {
        queuedLines.Clear();

        if (!string.IsNullOrWhiteSpace(message))
            queuedLines.Enqueue(message);

        RestartSpeech(hideDelay);
    }

    public void QueueSpeech(IEnumerable<string> lines)
    {
        queuedLines.Clear();

        if (lines != null)
        {
            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    queuedLines.Enqueue(line);
            }
        }

        RestartSpeech(autoHideDelay);
    }

    public void Hide()
    {
        if (speechRoutine != null)
            StopCoroutine(speechRoutine);

        speechRoutine = null;
        queuedLines.Clear();

        SetVisible(false);
    }

    private void RestartSpeech(float hideDelay)
    {
        EnsureVisuals();

        if (bubbleText == null)
            return;

        if (speechRoutine != null)
            StopCoroutine(speechRoutine);

        speechRoutine = StartCoroutine(SpeechRoutine(hideDelay));
    }

    private IEnumerator SpeechRoutine(float hideDelay)
    {
        while (queuedLines.Count > 0)
        {
            string line = queuedLines.Dequeue();

            BuildWrappedText(line, Mathf.Max(4, wrapCharacters));

            displayBuilder.Clear();

            SetVisible(true);
            bubbleText.text = string.Empty;

            float delay = 1f / Mathf.Max(1f, charactersPerSecond);

            for (int i = 0; i < wrapBuilder.Length; i++)
            {
                displayBuilder.Append(wrapBuilder[i]);
                bubbleText.text = displayBuilder.ToString();

                yield return new WaitForSeconds(delay);
            }

            float visibleDelay = Mathf.Max(0f, hideDelay);

            if (visibleDelay > 0f)
                yield return new WaitForSeconds(visibleDelay);
        }

        SetVisible(false);
        speechRoutine = null;
    }

    private void BuildWrappedText(string text, int maxCharacters)
    {
        wrapBuilder.Clear();

        if (string.IsNullOrEmpty(text))
            return;

        int currentLineLength = 0;
        int wordStart = 0;

        while (wordStart < text.Length)
        {
            while (wordStart < text.Length && text[wordStart] == ' ')
                wordStart++;

            if (wordStart >= text.Length)
                break;

            int wordEnd = wordStart;

            while (wordEnd < text.Length && text[wordEnd] != ' ')
                wordEnd++;

            int wordLength = wordEnd - wordStart;

            if (wordLength > maxCharacters)
            {
                if (currentLineLength > 0)
                {
                    wrapBuilder.Append('\n');
                    currentLineLength = 0;
                }

                int index = wordStart;

                while (index < wordEnd)
                {
                    int remaining = wordEnd - index;
                    int take = Mathf.Min(maxCharacters, remaining);

                    for (int i = 0; i < take; i++)
                        wrapBuilder.Append(text[index + i]);

                    index += take;
                    currentLineLength = take;

                    if (index < wordEnd)
                    {
                        wrapBuilder.Append('\n');
                        currentLineLength = 0;
                    }
                }
            }
            else
            {
                int requiredLength = currentLineLength == 0
                    ? wordLength
                    : wordLength + 1;

                if (currentLineLength > 0 &&
                    currentLineLength + requiredLength > maxCharacters)
                {
                    wrapBuilder.Append('\n');
                    currentLineLength = 0;
                }

                if (currentLineLength > 0)
                {
                    wrapBuilder.Append(' ');
                    currentLineLength++;
                }

                for (int i = wordStart; i < wordEnd; i++)
                    wrapBuilder.Append(text[i]);

                currentLineLength += wordLength;
            }

            wordStart = wordEnd + 1;
        }
    }

    private void EnsureVisuals()
    {
        if (bubbleRoot != null)
            return;

        GameObject prefab = ResolveBubblePrefab();

        if (prefab == null)
        {
            Debug.LogError(
                $"NpcSpeechBubble2D: Prefab을 찾을 수 없습니다. Path: {bubblePrefabResourcePath}",
                this
            );

            return;
        }

        GameObject instance = Instantiate(prefab, transform, false);

        bubbleRoot = instance.transform;
        bubbleRoot.localPosition =
            worldOffset +
            bubblePositionCorrection;

        bubbleText = FindBubbleText(instance);

        if (bubbleText == null)
        {
            Debug.LogError(
                "NpcSpeechBubble2D: BubbleText를 찾을 수 없습니다.",
                instance
            );
        }
    }

    private GameObject ResolveBubblePrefab()
    {
        if (bubblePrefab != null)
            return bubblePrefab;

        string resourcePath =
            string.IsNullOrWhiteSpace(bubblePrefabResourcePath)
                ? DefaultBubblePrefabResourcePath
                : bubblePrefabResourcePath.Trim();

        return Resources.Load<GameObject>(resourcePath);
    }

    private Text FindBubbleText(GameObject instance)
    {
        Transform[] children =
            instance.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name != "BubbleText")
                continue;

            Text text = child.GetComponent<Text>();

            if (text != null)
                return text;
        }

        return instance.GetComponentInChildren<Text>(true);
    }

    private void SetVisible(bool visible)
    {
        if (bubbleRoot != null)
            bubbleRoot.gameObject.SetActive(visible);
    }

    private void OnValidate()
    {
        wrapCharacters = Mathf.Max(4, wrapCharacters);
        charactersPerSecond = Mathf.Max(1f, charactersPerSecond);
        autoHideDelay = Mathf.Max(0f, autoHideDelay);
    }
}