using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Plays the numbered one-shot SFX from Assets/01Resources/Sound/SFX, routed through the
/// SoundEffect volume setting. Clips are addressed by <see cref="GameSfx"/> rather than by
/// their (Korean, space- and paren-containing) file names.
/// </summary>
public static class GameSfxPlayer
{
    private const string SfxResourceFolder = "Sound/SFX";
#if UNITY_EDITOR
    private const string SfxAssetFolder = "Assets/01Resources/Sound/SFX";
#endif

    private static readonly Dictionary<GameSfx, string> FileNamesBySfx = new()
    {
        { GameSfx.WeakLandingSnow, "1-1) 약한 착지 (눈)" },
        { GameSfx.WeakLandingIce, "1-2) 약한 착지 (얼음)" },
        { GameSfx.HardLanding, "2) 강한 착지" },
        { GameSfx.BigFall, "3) 대형 추락" },
        { GameSfx.AirFall, "4) 공중 추락" },
        { GameSfx.ActionCut, "5) 썰기 행동" },
        { GameSfx.ActionGrind, "6) 갈기 행동" },
        { GameSfx.ActionWipe, "7) 닦이 행동" },
        { GameSfx.BoilLimit, "8) 끓이기 한도" },
        { GameSfx.DishCompleted, "9) 요리 완성시" },
        { GameSfx.NewCookingStage, "10) 새로운 조리과정 돌입" },
        { GameSfx.ActionStir, "11) 섞어라 행동" },
        { GameSfx.ActionFry, "12) 튀겨라 행동" },
        { GameSfx.ActionSkewer, "13) 꽂아라 행동" },
        { GameSfx.ActionPlace, "14) 올려라 행동" },
        { GameSfx.CookingAreaAmbience, "15) 조리구역내 배경효과음" },
        { GameSfx.ActionSprinkle, "16) 뿌려라 행동" },
        { GameSfx.Button, "17) 버튼" },
    };

    private static readonly Dictionary<GameSfx, AudioClip> ClipCache = new();

    private static AudioSource oneShotSource;

    public static void Play(GameSfx sfx, float volumeScale = 1f)
    {
        AudioClip clip = GetClip(sfx);
        if (clip == null)
            return;

        AudioSource source = EnsureOneShotSource();
        if (source == null)
            return;

        source.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * GetSoundEffectVolume01());
    }

    public static AudioClip GetClip(GameSfx sfx)
    {
        if (ClipCache.TryGetValue(sfx, out AudioClip cached) && cached != null)
            return cached;

        if (!FileNamesBySfx.TryGetValue(sfx, out string fileName))
            return null;

        AudioClip clip = Resources.Load<AudioClip>($"{SfxResourceFolder}/{fileName}");

#if UNITY_EDITOR
        if (clip == null)
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxAssetFolder}/{fileName}.mp3");
#endif

        if (clip != null)
            ClipCache[sfx] = clip;

        return clip;
    }

    public static float GetSoundEffectVolume01()
    {
        GameSettingsManager settings = GameSettingsManager.EnsureExists();
        return settings != null ? settings.GetVolume01(GameAudioChannelType.SoundEffect) : 1f;
    }

    // One shared 2D source that survives scene loads, so a one-shot fired by an object that is
    // about to be deactivated (a finished cooking stage, a consumed pickup) still plays out.
    private static AudioSource EnsureOneShotSource()
    {
        if (oneShotSource != null)
            return oneShotSource;

        GameObject sourceObject = new("GameSfxPlayer");
        Object.DontDestroyOnLoad(sourceObject);

        oneShotSource = sourceObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f;

        return oneShotSource;
    }
}
