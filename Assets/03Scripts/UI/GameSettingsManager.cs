using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameSettingsManager : MonoBehaviour
{
    private const string BackgroundVolumeKey = "IceBop.Settings.BackgroundVolume";
    private const string SoundEffectVolumeKey = "IceBop.Settings.SoundEffectVolume";
    private const string FullScreenKey = "IceBop.Settings.FullScreen";

    private const float DefaultVolume = 0.8f;

    public static GameSettingsManager Instance { get; private set; }

    private bool initialized;

    public float BackgroundVolume01 { get; private set; } = DefaultVolume;
    public float SoundEffectVolume01 { get; private set; } = DefaultVolume;
    public bool IsFullScreen { get; private set; }

    public event Action<GameAudioChannelType, float> VolumeChanged;
    public event Action<bool> FullScreenChanged;

    public static GameSettingsManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameSettingsManager[] managers = FindObjectsByType<GameSettingsManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (managers.Length > 0)
        {
            managers[0].Initialize();
            return managers[0];
        }

        Debug.LogError("GameSettingsManager is missing. Place PauseSettingsMenuRuntime prefab in the first loaded scene.");
        return null;
    }

    public float GetVolume01(GameAudioChannelType channelType)
    {
        return channelType == GameAudioChannelType.BackgroundMusic
            ? BackgroundVolume01
            : SoundEffectVolume01;
    }

    public void SetBackgroundVolumePercent(float volumePercent)
    {
        SetVolume(GameAudioChannelType.BackgroundMusic, PercentTo01(volumePercent));
    }

    public void SetSoundEffectVolumePercent(float volumePercent)
    {
        SetVolume(GameAudioChannelType.SoundEffect, PercentTo01(volumePercent));
    }

    public void SetVolume(GameAudioChannelType channelType, float volume01)
    {
        volume01 = Mathf.Clamp01(volume01);

        if (channelType == GameAudioChannelType.BackgroundMusic)
        {
            if (Mathf.Approximately(BackgroundVolume01, volume01))
                return;

            BackgroundVolume01 = volume01;
            PlayerPrefs.SetFloat(BackgroundVolumeKey, BackgroundVolume01);
        }
        else
        {
            if (Mathf.Approximately(SoundEffectVolume01, volume01))
                return;

            SoundEffectVolume01 = volume01;
            PlayerPrefs.SetFloat(SoundEffectVolumeKey, SoundEffectVolume01);
        }

        PlayerPrefs.Save();
        VolumeChanged?.Invoke(channelType, volume01);
    }

    public void SetFullScreen(bool isFullScreen)
    {
        if (IsFullScreen == isFullScreen)
            return;

        IsFullScreen = isFullScreen;
        PlayerPrefs.SetInt(FullScreenKey, IsFullScreen ? 1 : 0);
        PlayerPrefs.Save();

        ApplyFullScreenMode();
        FullScreenChanged?.Invoke(IsFullScreen);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Initialize();
    }

    private void Initialize()
    {
        if (Instance != null && Instance != this)
            return;

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (initialized)
            return;

        initialized = true;
        LoadSettings();
        ApplyFullScreenMode();
    }

    private void LoadSettings()
    {
        BackgroundVolume01 = PlayerPrefs.GetFloat(BackgroundVolumeKey, DefaultVolume);
        SoundEffectVolume01 = PlayerPrefs.GetFloat(SoundEffectVolumeKey, DefaultVolume);

        bool defaultFullScreen = Screen.fullScreenMode == FullScreenMode.FullScreenWindow ||
                                 Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen ||
                                 Screen.fullScreen;
        IsFullScreen = PlayerPrefs.GetInt(FullScreenKey, defaultFullScreen ? 1 : 0) == 1;
    }

    private void ApplyFullScreenMode()
    {
        FullScreenMode mode = IsFullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        if (Screen.fullScreenMode == mode)
            return;

        Screen.SetResolution(Screen.width, Screen.height, mode);
    }

    private static float PercentTo01(float volumePercent)
    {
        return Mathf.Clamp(volumePercent, 0f, 100f) / 100f;
    }
}
