using UnityEngine;
using DarkMagic;

/// <summary>
/// WConfig: One obvious place to configure W.
/// Import this sample via Package Manager → V → Samples → Import "WConfig".
/// </summary>
public static class WConfig
{
    public const bool TRACE = true;
    public const bool GUARDRAILS = true;

    private static bool AllowedNow =>
#if UNITY_EDITOR
        true;
#else
        Debug.isDebugBuild;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        W.Trace = TRACE && AllowedNow;
        W.Guardrails = GUARDRAILS && AllowedNow;

        if (W.Trace)
            Debug.Log("<color=#7CFFB2><b>[W]</b> Trace enabled (WConfig)</color>");
    }
}