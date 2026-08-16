using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class NpcSpeechBubble2D : MonoBehaviour
{
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 worldOffset = new(0f, 1.35f, 0f);
    [SerializeField] private int wrapCharacters = 18;
    [SerializeField] private float charactersPerSecond = 42f;
    [SerializeField] private float autoHideDelay = 1.15f;
    [SerializeField] private float characterSize = 0.07f;
    [SerializeField] private int fontSize = 48;
    [SerializeField] private int sortingOrder = 120;
    [SerializeField] private Color backgroundColor = new(0.02f, 0.02f, 0.025f, 0.94f);
    [SerializeField] private Color borderColor = new(0.92f, 0.95f, 0.98f, 1f);
    [SerializeField] private Color textColor = Color.white;

    private readonly Queue<string> queuedLines = new();
    private Transform bubbleRoot;
    private SpriteRenderer borderRenderer;
    private SpriteRenderer backgroundRenderer;
    private SpriteRenderer tailRenderer;
    private TextMesh textMesh;
    private Coroutine speechRoutine;
    private string wrappedText = string.Empty;

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

        bubbleRoot.position = followTarget.position + worldOffset;
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

        if (speechRoutine != null)
            StopCoroutine(speechRoutine);

        speechRoutine = StartCoroutine(SpeechRoutine(hideDelay));
    }

    private IEnumerator SpeechRoutine(float hideDelay)
    {
        while (queuedLines.Count > 0)
        {
            string line = queuedLines.Dequeue();
            wrappedText = WrapText(line, Mathf.Max(4, wrapCharacters));
            ApplyBubbleSize(wrappedText);
            SetVisible(true);

            int characterCount = wrappedText.Length;
            float safeCharactersPerSecond = Mathf.Max(1f, charactersPerSecond);
            for (int i = 0; i <= characterCount; i++)
            {
                textMesh.text = wrappedText.Substring(0, i);
                yield return new WaitForSeconds(1f / safeCharactersPerSecond);
            }

            float visibleDelay = Mathf.Max(0f, hideDelay);
            if (visibleDelay > 0f)
                yield return new WaitForSeconds(visibleDelay);
        }

        SetVisible(false);
        speechRoutine = null;
    }

    private void EnsureVisuals()
    {
        if (bubbleRoot != null)
            return;

        bubbleRoot = new GameObject("NPC_SpeechBubble").transform;
        bubbleRoot.SetParent(transform, false);
        bubbleRoot.localPosition = worldOffset;
        bubbleRoot.localRotation = Quaternion.identity;
        bubbleRoot.localScale = Vector3.one;

        borderRenderer = CreateRenderer("BubbleBorder", bubbleRoot, new Vector3(0f, 0f, 0.02f), borderColor, sortingOrder);
        backgroundRenderer = CreateRenderer("BubbleBackground", bubbleRoot, new Vector3(0f, 0f, 0.01f), backgroundColor, sortingOrder + 1);
        tailRenderer = CreateRenderer("BubbleTail", bubbleRoot, new Vector3(0f, -0.32f, 0.015f), backgroundColor, sortingOrder + 1);
        tailRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        GameObject textObject = new("BubbleText");
        textObject.transform.SetParent(bubbleRoot, false);
        textObject.transform.localPosition = new Vector3(0f, 0.01f, -0.01f);

        textMesh = textObject.AddComponent<TextMesh>();
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.color = textColor;

        MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
        if (textRenderer != null)
            textRenderer.sortingOrder = sortingOrder + 2;
    }

    private SpriteRenderer CreateRenderer(string name, Transform parent, Vector3 localPosition, Color color, int order)
    {
        GameObject visual = new(name);
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = localPosition;

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = InventoryVisualUtility.WhiteSprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    private void ApplyBubbleSize(string text)
    {
        string[] lines = text.Split('\n');
        int longestLine = 1;
        foreach (string line in lines)
            longestLine = Mathf.Max(longestLine, line.Length);

        float width = Mathf.Clamp(longestLine * characterSize * 0.62f + 0.44f, 1.15f, 3.9f);
        float height = Mathf.Clamp(lines.Length * characterSize * 1.55f + 0.34f, 0.52f, 2.2f);

        borderRenderer.transform.localScale = new Vector3(width + 0.08f, height + 0.08f, 1f);
        backgroundRenderer.transform.localScale = new Vector3(width, height, 1f);
        tailRenderer.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
        tailRenderer.transform.localPosition = new Vector3(-width * 0.24f, -height * 0.5f, 0.015f);
    }

    private static string WrapText(string text, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        string[] words = text.Split(' ');
        if (words.Length <= 1)
            return WrapLongToken(text, maxCharacters);

        List<string> lines = new();
        string currentLine = string.Empty;
        foreach (string word in words)
        {
            if (string.IsNullOrWhiteSpace(word))
                continue;

            string candidate = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
            if (candidate.Length <= maxCharacters)
            {
                currentLine = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(currentLine))
                lines.Add(currentLine);

            currentLine = word.Length > maxCharacters ? WrapLongToken(word, maxCharacters) : word;
        }

        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        return string.Join("\n", lines);
    }

    private static string WrapLongToken(string text, int maxCharacters)
    {
        List<string> lines = new();
        for (int i = 0; i < text.Length; i += maxCharacters)
        {
            int length = Mathf.Min(maxCharacters, text.Length - i);
            lines.Add(text.Substring(i, length));
        }

        return string.Join("\n", lines);
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
        characterSize = Mathf.Max(0.01f, characterSize);
        fontSize = Mathf.Max(8, fontSize);
    }
}
