using UnityEngine;
using DarkMagic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// IInputActionsBridge: optional example showing how to keep using I.GetButton/GetAxis,
/// but have those values come from Input Actions.
/// 
/// Drop this on a GameObject, and it will register providers into I.
/// This is OPTIONAL and can be deleted without affecting I.
/// </summary>
public class IInputActionsBridge : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    public InputStarter starter;

    private void Awake()
    {
        if (starter == null) starter = FindFirstObjectByType<InputStarter>();
        if (starter == null)
        {
            Debug.LogWarning("[V/IInputActionsBridge] No InputStarter found. Add InputStarter to the scene or assign it.");
            enabled = false;
            return;
        }

        // Buttons
        I.Buttons["Jump"] = () => (starter.JumpHeld, starter.JumpDown, I.GetKeyUp(KeyCode.Space)); // up best-effort
        I.Buttons["Fire1"] = () => (starter.FireHeld, starter.FireDown, false);
        I.Buttons["Submit"] = () => (starter.JumpHeld, starter.JumpDown, false);
        I.Buttons["Cancel"] = () => (starter.PauseDown, starter.PauseDown, false);

        // Axes: map Move.x/y to legacy names
        I.Axes["Horizontal"] = () => Mathf.Clamp(starter.Move.x, -1f, 1f);
        I.Axes["Vertical"] = () => Mathf.Clamp(starter.Move.y, -1f, 1f);

        I.AxesRaw["Horizontal"] = () => Snap(starter.Move.x);
        I.AxesRaw["Vertical"] = () => Snap(starter.Move.y);
    }

    private static float Snap(float v)
    {
        if (v > 0.2f) return 1f;
        if (v < -0.2f) return -1f;
        return 0f;
    }
#else
    private void Awake()
    {
        Debug.LogWarning("[V/IInputActionsBridge] ENABLE_INPUT_SYSTEM is not defined. Install/enable the new Input System to use this sample.");
        enabled = false;
    }
#endif
}