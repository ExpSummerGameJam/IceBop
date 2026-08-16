using System.Collections;
using UnityEngine;

public sealed class NpcSpeechBubbleTestSceneBootstrap : MonoBehaviour
{
    [SerializeField] private Vector2[] npcPositions =
    {
        new(-3.4f, -1.1f),
        new(0f, -0.95f),
        new(3.4f, -1.1f)
    };

    [SerializeField] private string[] testLines =
    {
        "여긴 바람이 세다.",
        "말풍선이 NPC 머리 위를 따라간다.",
        "긴 대사도 자동으로 줄바꿈되어야 한다.",
        "Space로 다음 대사를 확인한다."
    };

    [SerializeField] private float firstSpeechDelay = 0.35f;
    [SerializeField] private float speechInterval = 2.8f;

    private NpcSpeechBubble2D[] speechBubbles;
    private int activeNpcIndex;
    private int nextLineIndex;
    private Coroutine loopRoutine;

    private void Awake()
    {
        CreateCamera();
        CreateEnvironment();
        CreateNpcs();
    }

    private void Start()
    {
        loopRoutine = StartCoroutine(AutoSpeechRoutine());
    }

    private void Update()
    {
        if (WasSpacePressed())
            SpeakNextLine();

        if (WasNumberPressed(1))
            SpeakFromNpc(0);

        if (WasNumberPressed(2))
            SpeakFromNpc(1);

        if (WasNumberPressed(3))
            SpeakFromNpc(2);
    }

    private IEnumerator AutoSpeechRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, firstSpeechDelay));

        while (true)
        {
            SpeakNextLine();
            yield return new WaitForSeconds(Mathf.Max(0.1f, speechInterval));
        }
    }

    private void SpeakNextLine()
    {
        if (speechBubbles == null || speechBubbles.Length == 0)
            return;

        SpeakFromNpc(activeNpcIndex);
        activeNpcIndex = (activeNpcIndex + 1) % speechBubbles.Length;
    }

    private void SpeakFromNpc(int npcIndex)
    {
        if (speechBubbles == null || npcIndex < 0 || npcIndex >= speechBubbles.Length)
            return;

        for (int i = 0; i < speechBubbles.Length; i++)
        {
            if (speechBubbles[i] != null)
                speechBubbles[i].Hide();
        }

        string line = GetNextLine();
        speechBubbles[npcIndex].Speak(line);
    }

    private string GetNextLine()
    {
        if (testLines == null || testLines.Length == 0)
            return "대사 테스트";

        string line = testLines[nextLineIndex % testLines.Length];
        nextLineIndex++;
        return line;
    }

    private void CreateCamera()
    {
        if (Camera.main != null)
            return;

        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
    }

    private void CreateEnvironment()
    {
        CreateBlock("BackPlate", new Vector3(0f, 0f, 1f), new Vector3(9.8f, 5.9f, 1f), new Color(0.12f, 0.15f, 0.18f, 1f), -10);
        CreateBlock("SnowGround", new Vector3(0f, -2.05f, 0f), new Vector3(9.5f, 0.42f, 1f), new Color(0.78f, 0.86f, 0.92f, 1f), -2);
        CreateBlock("GroundShadow", new Vector3(0f, -2.22f, 0f), new Vector3(9.5f, 0.14f, 1f), new Color(0.22f, 0.25f, 0.28f, 1f), -1);
    }

    private void CreateNpcs()
    {
        int count = Mathf.Max(1, npcPositions != null ? npcPositions.Length : 0);
        speechBubbles = new NpcSpeechBubble2D[count];

        for (int i = 0; i < count; i++)
        {
            Vector2 position = npcPositions != null && i < npcPositions.Length
                ? npcPositions[i]
                : new Vector2(i * 1.8f, -1.1f);

            speechBubbles[i] = CreateNpc(i, position);
        }
    }

    private NpcSpeechBubble2D CreateNpc(int index, Vector2 position)
    {
        GameObject npc = new($"SpeechBubbleTest_NPC_{index + 1}");
        npc.transform.position = position;

        Color bodyColor = index switch
        {
            1 => new Color(0.72f, 0.36f, 0.32f, 1f),
            2 => new Color(0.34f, 0.62f, 0.48f, 1f),
            _ => new Color(0.35f, 0.50f, 0.78f, 1f)
        };

        CreateBlock("Body", npc.transform, new Vector3(0f, 0.28f, 0f), new Vector3(0.58f, 0.82f, 1f), bodyColor, 10);
        CreateBlock("Face", npc.transform, new Vector3(0f, 0.86f, 0f), new Vector3(0.48f, 0.42f, 1f), new Color(0.93f, 0.82f, 0.68f, 1f), 11);
        CreateBlock("EyeL", npc.transform, new Vector3(-0.1f, 0.9f, 0f), new Vector3(0.04f, 0.04f, 1f), Color.black, 12);
        CreateBlock("EyeR", npc.transform, new Vector3(0.1f, 0.9f, 0f), new Vector3(0.04f, 0.04f, 1f), Color.black, 12);

        return npc.AddComponent<NpcSpeechBubble2D>();
    }

    private static SpriteRenderer CreateBlock(string name, Vector3 position, Vector3 scale, Color color, int sortingOrder)
    {
        GameObject block = new(name);
        block.transform.position = position;
        block.transform.localScale = scale;
        return ConfigureBlockRenderer(block, color, sortingOrder);
    }

    private static SpriteRenderer CreateBlock(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder)
    {
        GameObject block = new(name);
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localPosition;
        block.transform.localScale = localScale;
        return ConfigureBlockRenderer(block, color, sortingOrder);
    }

    private static SpriteRenderer ConfigureBlockRenderer(GameObject block, Color color, int sortingOrder)
    {
        SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = InventoryVisualUtility.WhiteSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static bool WasSpacePressed()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        pressed |= UnityEngine.InputSystem.Keyboard.current != null &&
                   UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.Space);
#endif

        return pressed;
    }

    private static bool WasNumberPressed(int number)
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            pressed |= number switch
            {
                1 => keyboard.digit1Key.wasPressedThisFrame,
                2 => keyboard.digit2Key.wasPressedThisFrame,
                3 => keyboard.digit3Key.wasPressedThisFrame,
                _ => false
            };
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= number switch
        {
            1 => Input.GetKeyDown(KeyCode.Alpha1),
            2 => Input.GetKeyDown(KeyCode.Alpha2),
            3 => Input.GetKeyDown(KeyCode.Alpha3),
            _ => false
        };
#endif

        return pressed;
    }

    private void OnValidate()
    {
        firstSpeechDelay = Mathf.Max(0f, firstSpeechDelay);
        speechInterval = Mathf.Max(0.1f, speechInterval);
    }
}
