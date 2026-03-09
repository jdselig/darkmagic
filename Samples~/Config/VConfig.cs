using UnityEngine;
using DarkMagic;

/// <summary>
/// VConfig: One obvious place to configure V.
///
/// Suggested student workflow:
/// - Package Manager → V → Samples → Import "VConfig"
/// - Toggle Trace here
/// - Add new event TYPES below (one class per event)
/// </summary>
public static class VConfig
{
    public const bool TRACE = true;

    private static bool TraceAllowedNow =>
#if UNITY_EDITOR
        true;
#else
        Debug.isDebugBuild;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        V.Trace = TRACE && TraceAllowedNow;

        if (V.Trace)
            Debug.Log("<color=#7CFFB2><b>[V]</b> Trace enabled (VConfig)</color>");
    }
}

// --------------------
// Define your events as TYPES.
// --------------------

public sealed class GameStart : V.Event { }

public sealed class PlayerDamaged : V.Event<int> { }

// Example (commented out): custom game types
// public sealed class ItemStolen : V.Event<Item, Character, Character> { }