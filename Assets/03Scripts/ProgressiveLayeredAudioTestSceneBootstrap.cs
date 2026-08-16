using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProgressiveLayeredAudioTestSceneBootstrap : MonoBehaviour
{
    private const int LayerCount = 9;
    private const int SampleRate = 44100;
    private const float Bpm = 120f;
    private const float LoopSeconds = 8f;
    private static readonly float[] ArpNotes = { 329.63f, 392f, 493.88f, 659.25f };
    private static readonly float[] LeadNotes = { 659.25f, 739.99f, 783.99f, 587.33f };

    [SerializeField, Range(0, LayerCount)] private int startingLayerCount = 1;
    [SerializeField, Range(0f, 1f)] private float layerVolume = 0.22f;
    [SerializeField] private bool createCameraIfMissing = true;
    [SerializeField] private bool showOverlay = true;

    private ProgressiveLayeredAudioController controller;

    private void Awake()
    {
        if (createCameraIfMissing)
            EnsureCamera();

        controller = CreateController();
        controller.SetActiveLayerCount(startingLayerCount);
    }

    private void Update()
    {
        if (controller != null && WasRestartPressed())
            controller.PlayAllSynced();
    }

    private void OnGUI()
    {
        if (!showOverlay || controller == null)
            return;

        const float margin = 24f;
        const float width = 430f;
        const float height = 158f;

        GUI.Box(new Rect(margin, margin, width, height), GUIContent.none);
        GUI.Label(new Rect(margin + 16f, margin + 14f, width - 32f, 24f), "Progressive Layered Audio Test");
        GUI.Label(new Rect(margin + 16f, margin + 42f, width - 32f, 22f), "1-9: enable first N layers   0: mute all   R: restart synced");
        GUI.Label(new Rect(margin + 16f, margin + 68f, width - 32f, 22f), $"Active layers: {controller.ActiveLayerCount}");

        Color oldColor = GUI.color;
        for (int i = 0; i < LayerCount; i++)
        {
            bool isActive = i < controller.ActiveLayerCount;
            Rect rect = new(margin + 16f + i * 44f, margin + 102f, 34f, 34f);

            GUI.color = isActive ? new Color(0.2f, 0.85f, 0.56f, 1f) : new Color(0.24f, 0.27f, 0.32f, 1f);
            GUI.Box(rect, (i + 1).ToString());
        }

        GUI.color = oldColor;
    }

    private ProgressiveLayeredAudioController CreateController()
    {
        GameObject controllerObject = new("ProgressiveLayeredAudioController");
        controllerObject.transform.SetParent(transform, false);

        ProgressiveLayeredAudioController layeredController = controllerObject.AddComponent<ProgressiveLayeredAudioController>();

        for (int i = 0; i < LayerCount; i++)
        {
            GameObject layerObject = new($"TestAudioLayer_{i + 1}");
            layerObject.transform.SetParent(controllerObject.transform, false);

            AudioSource source = layerObject.AddComponent<AudioSource>();
            source.clip = CreateTestClip(i + 1);
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = layerVolume;

            layeredController.SetLayerSource(i, source);
        }

        return layeredController;
    }

    private static void EnsureCamera()
    {
        if (Camera.main != null)
            return;

        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;

        cameraObject.AddComponent<AudioListener>();
    }

    private static AudioClip CreateTestClip(int layerNumber)
    {
        int sampleCount = Mathf.RoundToInt(SampleRate * LoopSeconds);
        float[] data = new float[sampleCount];

        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            float time = sampleIndex / (float)SampleRate;
            float beat = time * Bpm / 60f;
            data[sampleIndex] = GenerateLayerSample(layerNumber, sampleIndex, time, beat) * 0.68f;
        }

        AudioClip clip = AudioClip.Create($"Generated_Test_Layer_{layerNumber:00}", sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float GenerateLayerSample(int layerNumber, int sampleIndex, float time, float beat)
    {
        return layerNumber switch
        {
            1 => Kick(beat, time),
            2 => Hat(beat, sampleIndex),
            3 => Bass(beat, time),
            4 => Pad(time),
            5 => Arp(beat, time),
            6 => Snare(beat, sampleIndex),
            7 => CounterPulse(beat, time),
            8 => Lead(beat, time),
            9 => Shimmer(time),
            _ => 0f
        };
    }

    private static float Kick(float beat, float time)
    {
        float phase = Mathf.Repeat(beat, 1f);
        float envelope = Mathf.Exp(-phase * 9.5f);
        float frequency = 44f + 84f * Mathf.Exp(-phase * 11f);
        return Mathf.Sin(Tau * frequency * time) * envelope * 0.95f;
    }

    private static float Hat(float beat, int sampleIndex)
    {
        float phase = Mathf.Repeat(beat, 0.5f);
        float envelope = Mathf.Exp(-phase * 42f);
        return Noise(sampleIndex) * envelope * 0.32f;
    }

    private static float Bass(float beat, float time)
    {
        float phase = Mathf.Repeat(beat, 2f);
        float envelope = phase < 1.45f ? Mathf.Exp(-phase * 0.45f) : 0f;
        float frequency = Mathf.FloorToInt(beat / 4f) % 2 == 0 ? 82.41f : 98f;
        return Mathf.Sin(Tau * frequency * time) * envelope * 0.7f;
    }

    private static float Pad(float time)
    {
        float swell = 0.55f + 0.45f * Mathf.Sin(Tau * 0.125f * time);
        return (
            Mathf.Sin(Tau * 164.81f * time) +
            Mathf.Sin(Tau * 196f * time) +
            Mathf.Sin(Tau * 246.94f * time)) * swell * 0.12f;
    }

    private static float Arp(float beat, float time)
    {
        int noteIndex = Mathf.FloorToInt(beat * 2f) % ArpNotes.Length;
        float phase = Mathf.Repeat(beat, 0.5f);
        float envelope = phase < 0.42f ? Mathf.Exp(-phase * 4f) : 0f;
        return Mathf.Sin(Tau * ArpNotes[noteIndex] * time) * envelope * 0.36f;
    }

    private static float Snare(float beat, int sampleIndex)
    {
        float phase = Mathf.Repeat(beat + 1f, 2f);
        float envelope = phase < 0.22f ? Mathf.Exp(-phase * 18f) : 0f;
        return Noise(sampleIndex) * envelope * 0.5f;
    }

    private static float CounterPulse(float beat, float time)
    {
        float phase = Mathf.Repeat(beat + 0.5f, 1f);
        float envelope = phase < 0.32f ? Mathf.Exp(-phase * 7f) : 0f;
        return Mathf.Sin(Tau * 261.63f * time) * envelope * 0.42f;
    }

    private static float Lead(float beat, float time)
    {
        int noteIndex = Mathf.FloorToInt(beat / 2f) % LeadNotes.Length;
        float phase = Mathf.Repeat(beat, 2f);
        float envelope = phase < 1.7f ? 0.45f + 0.55f * Mathf.Exp(-phase * 1.2f) : 0f;
        return Mathf.Sin(Tau * LeadNotes[noteIndex] * time) * envelope * 0.24f;
    }

    private static float Shimmer(float time)
    {
        float mod = 0.5f + 0.5f * Mathf.Sin(Tau * 0.375f * time);
        return (
            Mathf.Sin(Tau * 987.77f * time) +
            Mathf.Sin(Tau * 1318.51f * time)) * mod * 0.08f;
    }

    private static float Noise(int sampleIndex)
    {
        float value = Mathf.Sin(sampleIndex * 12.9898f) * 43758.5453f;
        return Mathf.Repeat(value, 1f) * 2f - 1f;
    }

    private static bool WasRestartPressed()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        pressed |= UnityEngine.InputSystem.Keyboard.current != null
                   && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.R);
#endif

        return pressed;
    }

    private static float Tau => Mathf.PI * 2f;
}
