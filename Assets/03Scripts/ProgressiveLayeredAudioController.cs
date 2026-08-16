using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class ProgressiveLayeredAudioController : MonoBehaviour
{
    private const int MaxLayerCount = 3;
    private const string BgmResourcePathPrefix = "Sound/BGM/BGM_";
#if UNITY_EDITOR
    private const string BgmAssetPathPrefix = "Assets/01Resources/Sound/BGM/BGM_";
#endif

    [Header("Layers")]
    [SerializeField] private AudioSource[] layerSources = new AudioSource[MaxLayerCount];
    [SerializeField] private AudioClip[] layerClips = new AudioClip[MaxLayerCount];
    [SerializeField] private bool createMissingSourcesFromClips = true;
    [SerializeField] private bool loadMissingClipsFromBgmFolder = true;

    [Header("Playback")]
    [Range(0, MaxLayerCount)]
    [SerializeField] private int initialActiveLayerCount;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;
    [SerializeField] private double scheduledStartDelay = 0.1d;

    [Header("Settings")]
    [SerializeField] private bool registerSourcesWithSettings = true;
    [SerializeField] private GameAudioChannelType settingsAudioChannel = GameAudioChannelType.BackgroundMusic;

    [Header("Input")]
    [SerializeField] private bool numberKeysSetActiveLayerCount = true;
    [SerializeField] private bool includeNumpadKeys = true;
    [SerializeField] private bool zeroKeyMutesAll = true;

    private int activeLayerCount;
    private readonly float[] layerBaseVolumes = new float[MaxLayerCount];
    private readonly bool[] layerBaseVolumeInitialized = new bool[MaxLayerCount];
    private GameSettingsManager settings;

    public int ActiveLayerCount => activeLayerCount;

    private void Awake()
    {
        activeLayerCount = Mathf.Clamp(initialActiveLayerCount, 0, MaxLayerCount);
        EnsureLayerClipsFromBgmFolder();
        EnsureRuntimeSources();
        ConfigureSources();
        ApplyMuteState();
    }

    private void OnEnable()
    {
        settings = GameSettingsManager.EnsureExists();
        if (settings != null)
        {
            settings.VolumeChanged += OnSettingsVolumeChanged;
            ApplySettingsVolumeToSources();
        }
    }

    private void OnDisable()
    {
        if (settings != null)
            settings.VolumeChanged -= OnSettingsVolumeChanged;
    }

    private void Start()
    {
        if (playOnStart)
            PlayAllSynced();
    }

    private void Update()
    {
        if (GamePauseState.IsPaused)
            return;

        if (!numberKeysSetActiveLayerCount)
            return;

        if (zeroKeyMutesAll && WasNumberKeyPressed(0))
        {
            SetActiveLayerCount(0);
            return;
        }

        for (int number = 1; number <= MaxLayerCount; number++)
        {
            if (!WasNumberKeyPressed(number))
                continue;

            SetActiveLayerCount(number);
            return;
        }
    }

    private void OnValidate()
    {
        EnsureArraySize(ref layerSources);
        EnsureArraySize(ref layerClips);
        initialActiveLayerCount = Mathf.Clamp(initialActiveLayerCount, 0, MaxLayerCount);
        scheduledStartDelay = Math.Max(0d, scheduledStartDelay);
    }

    [ContextMenu("Play All Synced")]
    public void PlayAllSynced()
    {
        EnsureRuntimeSources();
        ConfigureSources();

        double startTime = AudioSettings.dspTime + Math.Max(0d, scheduledStartDelay);

        for (int i = 0; i < MaxLayerCount; i++)
        {
            AudioSource source = layerSources[i];
            if (source == null || source.clip == null)
                continue;

            source.Stop();
            source.timeSamples = 0;
            source.mute = i >= activeLayerCount;
            source.PlayScheduled(startTime);
        }
    }

    public void SetActiveLayerCount(int layerCount)
    {
        activeLayerCount = Mathf.Clamp(layerCount, 0, MaxLayerCount);
        ApplyMuteState();
    }

    public void SetLayerSource(int layerIndex, AudioSource source)
    {
        if (layerIndex < 0 || layerIndex >= MaxLayerCount)
            throw new ArgumentOutOfRangeException(nameof(layerIndex));

        layerSources[layerIndex] = source;
        ConfigureSource(layerSources[layerIndex], layerIndex);
        ApplyMuteState();
    }

    private void EnsureLayerClipsFromBgmFolder()
    {
        if (!loadMissingClipsFromBgmFolder || layerClips == null)
            return;

        for (int i = 0; i < MaxLayerCount; i++)
        {
            if (layerClips[i] != null)
                continue;

            layerClips[i] = LoadBgmClip(i + 1);
        }
    }

    private static AudioClip LoadBgmClip(int stageNumber)
    {
        AudioClip clip = Resources.Load<AudioClip>(BgmResourcePathPrefix + stageNumber);

#if UNITY_EDITOR
        if (clip == null)
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmAssetPathPrefix + stageNumber + ".mp3");
#endif

        return clip;
    }

    private void EnsureRuntimeSources()
    {
        if (!createMissingSourcesFromClips || layerClips == null)
            return;

        for (int i = 0; i < MaxLayerCount; i++)
        {
            if (layerSources[i] != null || layerClips[i] == null)
                continue;

            GameObject sourceObject = new($"AudioLayer_{i + 1}");
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.clip = layerClips[i];
            layerSources[i] = source;
        }
    }

    private void ConfigureSources()
    {
        for (int i = 0; i < MaxLayerCount; i++)
            ConfigureSource(layerSources[i], i);
    }

    private void ConfigureSource(AudioSource source, int layerIndex)
    {
        if (source == null)
            return;

        if (layerClips != null && layerIndex >= 0 && layerIndex < layerClips.Length && layerClips[layerIndex] != null)
            source.clip = layerClips[layerIndex];

        source.playOnAwake = false;
        source.loop = loop;
        source.mute = layerIndex >= activeLayerCount;

        RegisterSourceWithSettings(source, layerIndex);
    }

    private void RegisterSourceWithSettings(AudioSource source, int layerIndex)
    {
        if (!registerSourcesWithSettings || source == null)
            return;

        GameAudioChannel channel = source.GetComponent<GameAudioChannel>();
        if (channel != null)
        {
            channel.SetChannelType(settingsAudioChannel);
            return;
        }

        if (!layerBaseVolumeInitialized[layerIndex])
        {
            layerBaseVolumes[layerIndex] = source.volume;
            layerBaseVolumeInitialized[layerIndex] = true;
        }

        GameSettingsManager currentSettings = settings != null ? settings : GameSettingsManager.EnsureExists();
        if (currentSettings == null)
            return;

        source.volume = layerBaseVolumes[layerIndex] * currentSettings.GetVolume01(settingsAudioChannel);
    }

    private void OnSettingsVolumeChanged(GameAudioChannelType changedChannelType, float volume01)
    {
        if (changedChannelType != settingsAudioChannel)
            return;

        ApplySettingsVolumeToSources();
    }

    private void ApplySettingsVolumeToSources()
    {
        if (!registerSourcesWithSettings)
            return;

        for (int i = 0; i < MaxLayerCount; i++)
        {
            AudioSource source = layerSources[i];
            if (source == null || source.GetComponent<GameAudioChannel>() != null)
                continue;

            RegisterSourceWithSettings(source, i);
        }
    }

    private void ApplyMuteState()
    {
        for (int i = 0; i < MaxLayerCount; i++)
        {
            AudioSource source = layerSources[i];
            if (source == null)
                continue;

            source.mute = i >= activeLayerCount;
        }
    }

    private bool WasNumberKeyPressed(int number)
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
            pressed |= WasInputSystemNumberPressed(keyboard, number);
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(GetAlphaKeyCode(number));

        if (includeNumpadKeys)
            pressed |= Input.GetKeyDown(GetNumpadKeyCode(number));
#endif

        return pressed;
    }

#if ENABLE_INPUT_SYSTEM
    private bool WasInputSystemNumberPressed(UnityEngine.InputSystem.Keyboard keyboard, int number)
    {
        return number switch
        {
            0 => keyboard.digit0Key.wasPressedThisFrame ||
                 includeNumpadKeys && keyboard.numpad0Key.wasPressedThisFrame,
            1 => keyboard.digit1Key.wasPressedThisFrame ||
                 includeNumpadKeys && keyboard.numpad1Key.wasPressedThisFrame,
            2 => keyboard.digit2Key.wasPressedThisFrame ||
                 includeNumpadKeys && keyboard.numpad2Key.wasPressedThisFrame,
            3 => keyboard.digit3Key.wasPressedThisFrame ||
                 includeNumpadKeys && keyboard.numpad3Key.wasPressedThisFrame,
            4 => keyboard.digit4Key.wasPressedThisFrame ||
                 includeNumpadKeys && keyboard.numpad4Key.wasPressedThisFrame,
            5 => keyboard.digit5Key.wasPressedThisFrame ||
                 includeNumpadKeys && keyboard.numpad5Key.wasPressedThisFrame,
            6 => keyboard.digit6Key.wasPressedThisFrame ||
                 includeNumpadKeys && keyboard.numpad6Key.wasPressedThisFrame,
            7 => keyboard.digit7Key.wasPressedThisFrame ||
                 includeNumpadKeys && keyboard.numpad7Key.wasPressedThisFrame,
            8 => keyboard.digit8Key.wasPressedThisFrame ||
                 includeNumpadKeys && keyboard.numpad8Key.wasPressedThisFrame,
            9 => keyboard.digit9Key.wasPressedThisFrame ||
                 includeNumpadKeys && keyboard.numpad9Key.wasPressedThisFrame,
            _ => false
        };
    }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
    private static KeyCode GetAlphaKeyCode(int number)
    {
        return number switch
        {
            0 => KeyCode.Alpha0,
            1 => KeyCode.Alpha1,
            2 => KeyCode.Alpha2,
            3 => KeyCode.Alpha3,
            4 => KeyCode.Alpha4,
            5 => KeyCode.Alpha5,
            6 => KeyCode.Alpha6,
            7 => KeyCode.Alpha7,
            8 => KeyCode.Alpha8,
            9 => KeyCode.Alpha9,
            _ => KeyCode.None
        };
    }

    private static KeyCode GetNumpadKeyCode(int number)
    {
        return number switch
        {
            0 => KeyCode.Keypad0,
            1 => KeyCode.Keypad1,
            2 => KeyCode.Keypad2,
            3 => KeyCode.Keypad3,
            4 => KeyCode.Keypad4,
            5 => KeyCode.Keypad5,
            6 => KeyCode.Keypad6,
            7 => KeyCode.Keypad7,
            8 => KeyCode.Keypad8,
            9 => KeyCode.Keypad9,
            _ => KeyCode.None
        };
    }
#endif

    private static void EnsureArraySize<T>(ref T[] array)
    {
        if (array == null)
        {
            array = new T[MaxLayerCount];
            return;
        }

        if (array.Length != MaxLayerCount)
            Array.Resize(ref array, MaxLayerCount);
    }
}
