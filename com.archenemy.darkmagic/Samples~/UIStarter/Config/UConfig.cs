using TMPro;
using UnityEngine;
using DarkMagic;

/// <summary>
/// UConfig: defaults for U (code-first UI).
/// 
/// Copy this Config folder into Assets/ and edit freely.
/// </summary>
public static class UConfig
{
    // Tracing (auto off in builds)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static bool TRACE = false;
#else
    public static bool TRACE = false;
#endif

    // Font
    public static TMP_FontAsset Font = null; // optional override. If null, U will use TMP default font asset if available.

    // Colors
    public static Color PanelColor = new Color(0f, 0f, 0f, 0.72f);
    public static Color BorderColor = new Color(1f, 1f, 1f, 0.18f);
    public static Color TextColor = Color.white;
    public static Color SelectedColor = new Color(1f, 1f, 1f, 0.12f);

    // Layout sizing (as % of screen)
    public static float BannerHeightPct = 0.07f;
    public static float DialogueHeightPct = 0.25f;
    public static float ModalWidthPct = 0.85f;
    public static float ModalHeightPct = 0.85f;

    // Pagination
    public static int DialogueMaxCharsPerPage = 220;
    public static int BannerMaxChars = 90;

    // History
    public static int HistoryMax = 50;

    // Input (defaults: Enter/Space/Mouse0/Gamepad South confirm; Esc/Backspace/Mouse1/Gamepad East cancel)
    public static KeyCode[] ConfirmKeys = { KeyCode.Return, KeyCode.Space };
    public static KeyCode[] CancelKeys = { KeyCode.Escape, KeyCode.Backspace };

    // Named buttons via I (optional, can be overridden by IConfig)
    public static string ConfirmButtonName = "Submit";
    public static string CancelButtonName = "Cancel";

    // Choice navigation keys
    public static KeyCode[] UpKeys = { KeyCode.UpArrow, KeyCode.W };
    public static KeyCode[] DownKeys = { KeyCode.DownArrow, KeyCode.S };

    // Targeting navigation keys
    public static KeyCode[] LeftKeys = { KeyCode.LeftArrow, KeyCode.A };
    public static KeyCode[] RightKeys = { KeyCode.RightArrow, KeyCode.D };
}