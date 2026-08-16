using UnityEngine;

public static class GatheringInput
{
    public static bool WasInteractPressed()
    {
        if (GamePauseState.IsPaused)
            return false;

        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        pressed |= UnityEngine.InputSystem.Keyboard.current != null
                   && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.E);
#endif

        return pressed;
    }

    public static bool WasInventoryPressed()
    {
        if (GamePauseState.IsPaused)
            return false;

        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        pressed |= UnityEngine.InputSystem.Keyboard.current != null
                   && UnityEngine.InputSystem.Keyboard.current.iKey.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.I);
#endif

        return pressed;
    }

    public static bool WasPrimaryPressed()
    {
        if (GamePauseState.IsPaused)
            return false;

        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        pressed |= UnityEngine.InputSystem.Mouse.current != null
                   && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetMouseButtonDown(0);
#endif

        return pressed;
    }

    public static bool IsPrimaryHeld()
    {
        if (GamePauseState.IsPaused)
            return false;

        bool held = false;

#if ENABLE_INPUT_SYSTEM
        held |= UnityEngine.InputSystem.Mouse.current != null
                && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        held |= Input.GetMouseButton(0);
#endif

        return held;
    }

    public static bool WasPrimaryReleased()
    {
        if (GamePauseState.IsPaused)
            return false;

        bool released = false;

#if ENABLE_INPUT_SYSTEM
        released |= UnityEngine.InputSystem.Mouse.current != null
                    && UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        released |= Input.GetMouseButtonUp(0);
#endif

        return released;
    }

    public static Vector2 PointerScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
            return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }
}
