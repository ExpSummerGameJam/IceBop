using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public sealed class GameAudioChannel : MonoBehaviour
{
    [SerializeField] private GameAudioChannelType channelType = GameAudioChannelType.SoundEffect;
    [SerializeField, Range(0f, 1f)] private float baseVolume = 1f;
    [SerializeField] private bool readInitialAudioSourceVolume = true;

    private AudioSource audioSource;
    private GameSettingsManager settings;

    public GameAudioChannelType ChannelType => channelType;

    public void Configure(GameAudioChannelType newChannelType, float newBaseVolume)
    {
        channelType = newChannelType;
        baseVolume = Mathf.Clamp01(newBaseVolume);
        readInitialAudioSourceVolume = false;
        ApplyVolume();
    }

    public void SetChannelType(GameAudioChannelType newChannelType)
    {
        if (channelType == newChannelType)
            return;

        channelType = newChannelType;
        ApplyVolume();
    }

    public void SetBaseVolume(float newBaseVolume)
    {
        baseVolume = Mathf.Clamp01(newBaseVolume);
        readInitialAudioSourceVolume = false;
        ApplyVolume();
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (readInitialAudioSourceVolume)
            baseVolume = audioSource.volume;
    }

    private void OnEnable()
    {
        settings = GameSettingsManager.EnsureExists();
        if (settings == null)
            return;

        settings.VolumeChanged += OnVolumeChanged;
        ApplyVolume();
    }

    private void OnDisable()
    {
        if (settings != null)
            settings.VolumeChanged -= OnVolumeChanged;
    }

    private void OnValidate()
    {
        baseVolume = Mathf.Clamp01(baseVolume);
    }

    private void OnVolumeChanged(GameAudioChannelType changedChannelType, float volume01)
    {
        if (changedChannelType != channelType)
            return;

        ApplyVolume();
    }

    private void ApplyVolume()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        GameSettingsManager currentSettings = settings != null ? settings : GameSettingsManager.EnsureExists();
        if (currentSettings == null)
            return;

        audioSource.volume = baseVolume * currentSettings.GetVolume01(channelType);
    }
}
