using System;
using UnityEngine;

public static class GamePauseState
{
    private static float timeScaleBeforePause = 1f;

    public static bool IsPaused { get; private set; }

    public static event Action<bool> PauseChanged;

    public static void SetPaused(bool paused)
    {
        if (IsPaused == paused)
            return;

        if (paused)
        {
            timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
        }

        IsPaused = paused;
        PauseChanged?.Invoke(IsPaused);
    }
}
