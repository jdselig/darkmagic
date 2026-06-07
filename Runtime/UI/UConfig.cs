using System.Linq;
using TMPro;
using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// UConfig: defaults for U (code-first UI).
    ///
    /// Recommended: copy the Config sample's Config folder into Assets/ and edit there.
    /// </summary>
    public enum UStylePreset
    {
        JRPG,
        Liberation,
    }

    public static class UConfig
    {
        // Tracing (auto off in builds)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool TRACE = false;
#else
        public static bool TRACE = false;
#endif

        // Style preset (student-friendly look + feel defaults)
        public static UStylePreset StylePreset = UStylePreset.JRPG;

        // Font
        public static TMP_FontAsset Font = null; // optional override. If null, U will use TMP default font asset if available.

        // Colors
        public static Color PanelColor = new Color(0f, 0f, 0f, 0.77f);
        public static int BorderSize = 6;
        public static Color BorderColor = Color.white;
        public static bool PopFadeIn = true;
        public static bool PopFadeOut = true;
        public static float PopFadeDuration = 0.12f;

        // Transitions (scene-style wipes)
        public static Color TransitionColor = Color.black;
        public static float TransitionDuration = 0.35f;
        public static float TransitionSoftness = 0.08f; // diagonal edge softness
        public static float TransitionPixelScale = 120f; // higher = smaller pixels

        public static Color TextColor = Color.white;
        public static TMP_FontAsset FontAsset = null;

        // Font sizes (U defaults). Tune these if UI feels too small/large.

        // Outcome numbers (JRPG combat text)
        public static int OutcomeFontSize = 36;
        public static Color OutcomeColor = Color.white;
        public static float OutcomeDuration = 0.8f;
        public static float OutcomeRisePx = 70f;
        public static float OutcomeBouncePx = 14f;
        public static float OutcomeScalePop = 1.15f;
        public static float OutcomeCanvasPaddingPx = 12f;
        public static float OutcomeOffsetX = 32f; // pixels (canvas space) added after world->canvas conversion
        public static float OutcomeOffsetY = 28f; // pixels (canvas space) added after world->canvas conversion
        public static string OutcomeAnchorChildName = "OutcomeAnchor";
        public static float OutcomeWorldOffsetY = 1.0f; // world units added to target.position before projection
        public static int BodyFontSize = 34;
        public static int DialogueFontSize = 34; // default body size for dialogue/choice prompt
        public static int TitleFontSize = 44;
        public static int ChoiceFontSize = 36;
        public static int DescFontSize = 34;
        public static int PageFontSize = 32;

        // Highlight styles
        public static Color SelectedTextColor = new Color(1f, 0.84f, 0.2f, 1f); // gold-ish
        public static Color SelectedColor = new Color(1f, 1f, 1f, 0.12f);

        // Layout sizing (as % of screen)
        public static float BannerHeightPct = 0.07f;
        public static float DialogueHeightPct = 0.34f;
        public static float ModalWidthPct = 0.85f;
        public static float ModalHeightPct = 0.85f;

        // Pagination
        public static int DialogueMaxCharsPerPage = 220;
        public static int DialoguePaddingX = 60;
        public static int DialoguePaddingY = 24;
        public static int DialoguePaddingTop = 45; // top padding for dialogue text
        public static int BannerPaddingX = 40;
        public static int BannerPaddingY = 12;
        public static int BannerFontSize = 34;
        public static float DialogueLineSpacing = 56f;
        public static float DisplayLineSpacing = 22f;
        public static float BannerLineSpacing = 20f;
        public static float ChoiceLineSpacing = 18f;

        public static int BannerMaxChars = 90;

        // History
        public static int HistoryMax = 50;

        // Display HUD outer margin from screen edge (pixels)
        public static float DisplayMarginPx = 10f;
        public static int DisplayPaddingPx = 10;

        // CanvasScaler reference resolution (used when U creates the U_Root canvas)
        public static Vector2 ReferenceResolution = new Vector2(1920, 1080);

        // Choice layout spacing
        public static float ChoiceSpacingPx = 16f;
        public static float ChoiceLabelSpacingPx = 12f;

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

        // Target filtering (global predicate). If provided, only targets passing this are selectable by default.
        public static System.Func<Transform, bool> TargetIsTargetable = null;

        // Targeting (U.Target)
        public static string TargetMarkerGlyph = "^"; // ASCII-safe marker (avoid missing glyphs in SDF fonts)
        public static Color TargetMarkerColor = new Color(1f, 0.88235295f, 0.03137255f, 1f); // #FFE108
        public static float TargetMarkerScaleY = -1f; // flip caret marker downward by default
        public static TMP_FontAsset TargetMarkerFont = null; // optional override; defaults to the same font used by U
        public static Vector2 TargetMarkerScreenOffset = new Vector2(0, 40); // extra screen-space offset (pixels)
        public static float TargetMarkerWorldOffsetY = 1.0f; // world units added if no anchors/colliders
        public static string TargetAnchorChildName = "TargetAnchor";
        public static GameObject TargetMarkerPrefab = null; // optional UI prefab override (must have RectTransform)
        public static Sprite TargetMarkerSprite = null; // optional sprite marker override (added as Image)

        /// <summary>
        /// U will pull matching public static fields from it at startup and apply them to UConfig.
        /// This lets students edit settings in Assets/ without touching the package.
        /// </summary>
        // Internal baseline defaults (used to detect whether user explicitly changed values)
        private static readonly Color _defaultPanelColor = new Color(0f, 0f, 0f, 0.77f);
        private static readonly Color _jrpgPanelColor = new Color(
            11f / 255f,
            42f / 255f,
            122f / 255f,
            0.93f
        ); // FFVI-ish menu blue, 93% opacity

        /// <summary>
        /// Apply the chosen StylePreset. Runs after user overrides load,
        /// but only overwrites values that still match package defaults.
        /// </summary>
        public static void ApplyStylePreset()
        {
            // Font: only set if user didn't explicitly provide one
            if (FontAsset == null && Font == null)
            {
                if (StylePreset == UStylePreset.JRPG)
                {
                    FontAsset = Resources.Load<TMP_FontAsset>("Default/Default SDF");
                }
                else if (StylePreset == UStylePreset.Liberation)
                {
                    FontAsset = Resources.Load<TMP_FontAsset>(
                        "Fonts/Liberation/LiberationSans SDF"
                    );
                }
            }

            // Panel color: only overwrite if still at baseline default
            if (PanelColor == _defaultPanelColor && StylePreset == UStylePreset.JRPG)
            {
                PanelColor = _jrpgPanelColor;
            }
        }

        // Students: copy Samples~/Config/UConfig.cs into Assets/Config/UConfig.cs and edit values there.
        // That file defines a DarkMagic.UConfigUser class which overrides these defaults.
        public static void ApplyUserOverrides()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            // In non-dev builds, we skip reflection-based overrides for speed and AOT friendliness.
            return;
#endif
            var t = System.Type.GetType("DarkMagic.UConfigUser, Assembly-CSharp");
            if (t == null)
                t = System.Type.GetType("DarkMagic.UConfigUser");
            if (t == null)
                return;

            try
            {
                var flags =
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
                foreach (var f in t.GetFields(flags))
                {
                    var dst = typeof(UConfig).GetField(f.Name, flags);
                    if (dst == null)
                        continue;
                    if (dst.FieldType != f.FieldType)
                        continue;
                    dst.SetValue(null, f.GetValue(null));
                }
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    "[DarkMagic/U] Failed to apply UConfigUser overrides: " + ex.Message
                );
#endif
            }
        }
    }
}