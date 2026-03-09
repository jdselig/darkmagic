using UnityEngine;
using DarkMagic;

/// <summary>
/// IConfig: optional overrides for I's default Buttons/Axes.
/// Import via Package Manager → V → Samples → Import "Config".
/// </summary>
public static class IConfig
{
    public const bool TRACE = false;
    public const bool WARN_ON_FALLBACK = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        I.Trace = TRACE;
        I.WarnOnFallback = WARN_ON_FALLBACK;

        // Example: override Jump to use Z instead of Space (works regardless of input backend).
        // I.Buttons["Jump"] = () => (
        //     I.GetKey(KeyCode.Z),
        //     I.GetKeyDown(KeyCode.Z),
        //     I.GetKeyUp(KeyCode.Z)
        // );

        // Example: custom axis (raw) using Q/E as left/right:
        // I.AxesRaw["Horizontal"] = () =>
        // {
        //     float left = I.GetKey(KeyCode.Q) ? 1f : 0f;
        //     float right = I.GetKey(KeyCode.E) ? 1f : 0f;
        //     return Mathf.Clamp(right - left, -1f, 1f);
        // };
    }
}