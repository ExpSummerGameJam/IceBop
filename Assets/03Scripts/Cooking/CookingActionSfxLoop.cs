using UnityEngine;

/// <summary>
/// Keeps an action SFX looping for as long as the player is actively performing that cooking
/// action (cutting, stirring, shaking, ...). Stages call <see cref="Notify"/> every frame the
/// action is happening; the loop stops on its own once they stop calling it.
/// </summary>
public sealed class CookingActionSfxLoop
{
    private readonly float silenceGraceSeconds;

    private AudioSource source;
    private GameSfx currentSfx;
    private float lastNotifiedTime;
    private bool playing;

    public CookingActionSfxLoop(float silenceGraceSeconds = 0.12f)
    {
        this.silenceGraceSeconds = Mathf.Max(0.01f, silenceGraceSeconds);
    }

    /// <summary>Call every frame the action is being performed.</summary>
    public void Notify(Transform owner, GameSfx sfx)
    {
        lastNotifiedTime = Time.unscaledTime;

        if (playing && currentSfx == sfx)
            return;

        if (!EnsureSource(owner))
            return;

        AudioClip clip = GameSfxPlayer.GetClip(sfx);
        if (clip == null)
            return;

        currentSfx = sfx;
        source.clip = clip;
        source.volume = GameSfxPlayer.GetSoundEffectVolume01();
        source.Play();
        playing = true;
    }

    /// <summary>Call every frame (from the stage's Update) so the loop can time itself out.</summary>
    public void Tick()
    {
        if (!playing)
            return;

        if (Time.unscaledTime - lastNotifiedTime <= silenceGraceSeconds)
        {
            if (source != null)
                source.volume = GameSfxPlayer.GetSoundEffectVolume01();

            return;
        }

        Stop();
    }

    public void Stop()
    {
        playing = false;

        if (source != null)
            source.Stop();
    }

    private bool EnsureSource(Transform owner)
    {
        if (source != null)
            return true;

        if (owner == null)
            return false;

        GameObject sourceObject = new("CookingActionSfx");
        sourceObject.transform.SetParent(owner, false);

        source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;

        return true;
    }
}
