using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkMagic
{
    public static partial class U
    {

        internal static TextAlignmentOptions AlignmentForPlacement(Placements placement, TextAlignmentOptions fallback)
        {
            return placement switch
            {
                Placements.TopRight => TextAlignmentOptions.Right,
                Placements.MiddleRight => TextAlignmentOptions.Right,
                Placements.BottomRight => TextAlignmentOptions.Right,

                Placements.TopCenter => TextAlignmentOptions.Center,
                Placements.MiddleCenter => TextAlignmentOptions.Center,
                Placements.BottomCenter => TextAlignmentOptions.Center,

                _ => fallback
            };
        }


        private static Option[] ToOptions(string[] options)
        {
            if (options == null) return Array.Empty<Option>();
            var opts = new Option[options.Length];
            for (int i = 0; i < options.Length; i++) opts[i] = new Option(PreprocessMarkupInline(options[i]));
            return opts;
        }

        // ============================================================
        // Public API (v1)
        // ============================================================

        public static async Awaitable<Result<bool>> PopBanner(string text, Placements placement = Placements.TopCenter, Sizes size = Sizes.FullWidth, int? textSize = null, Color? textColor = null, TextAlignmentOptions? textAlign = null, Color? panelColor = null, int? borderSize = null, Color? borderColor = null, CancellationToken cancellationToken = default)
        {
            EnsureSystem();
            UISystem.EnsureEventSystem();

            text = ClampRichText(text, UConfig.BannerMaxChars);

            text = PreprocessMarkupInline(text);
            var panel = _pool.GetOrCreate(PopupKind.Banner);
            panel.ConfigureBanner(text, placement, size, textSize, textColor, textAlign, panelColor);
            panel.SetBorder(borderSize, borderColor);
            await panel.FadeIn(cancellationToken);

            _stack.Push(panel);

            if (UConfig.TRACE) Debug.Log($"[U] PopBanner: {text}");

            V.Broadcast<DialoguePopped>(text);

            var res = await panel.AwaitConfirmCancel(cancellationToken);

            _stack.Pop();
            await panel.FadeOut(CancellationToken.None);
            panel.ReturnToPool(PopupKind.Banner);

            return new Result<bool>(!res.Cancelled, res.Cancelled);
        }


        public static async Awaitable<Result<bool>> PopBanner(string text, float secondsToLive, Placements placement = Placements.TopCenter, Sizes size = Sizes.FullWidth, int? textSize = null, Color? textColor = null, TextAlignmentOptions? textAlign = null, Color? panelColor = null, int? borderSize = null, Color? borderColor = null, System.Threading.CancellationToken cancellationToken = default)
        {
            EnsureSystem();
            UISystem.EnsureEventSystem();

            text = ClampRichText(text, UConfig.BannerMaxChars);

            text = PreprocessMarkupInline(text);

            var panel = _pool.GetOrCreate(PopupKind.Banner);
            panel.ConfigureBanner(text, placement, size, textSize, textColor, textAlign, panelColor);
            panel.SetBorder(borderSize, borderColor);
            await panel.FadeIn(cancellationToken);

            _stack.Push(panel);

            if (UConfig.TRACE) Debug.Log($"[U] PopBanner (timed {secondsToLive:0.##}s): {text}");

            // Let students hook SFX, etc.
            V.Broadcast<DialoguePopped>(text);

            try
            {
                await Awaitable.WaitForSecondsAsync(secondsToLive, cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                // fall through to cleanup, return canceled
                _stack.Pop();
            await panel.FadeOut(CancellationToken.None);
                panel.ReturnToPool(PopupKind.Banner);
                return Result<bool>.Canceled();
            }

            _stack.Pop();
            await panel.FadeOut(CancellationToken.None);
            panel.ReturnToPool(PopupKind.Banner);

            // Timed banners aren't a "choice": we treat this as OK.
            return Result<bool>.Ok(true);
        }

        // Preferred signature: keeps CancellationToken last so named args like textColor: work nicely.
        public static async Awaitable<Result<bool>> PopDialogue(string text, Placements placement = Placements.BottomCenter, Sizes size = Sizes.FullWidth, int? textSize = null, Color? textColor = null, TextAlignmentOptions? textAlign = null, Color? panelColor = null, int? borderSize = null, Color? borderColor = null, CancellationToken cancellationToken = default)
            => await PopDialogue_Impl(text, cancellationToken, placement, size, textSize, textColor, textAlign, panelColor, borderSize, borderColor);

        // Back-compat signature (older versions had CancellationToken first).
        public static async Awaitable<Result<bool>> PopDialogue(string text, CancellationToken cancellationToken, Placements placement = Placements.BottomCenter, Sizes size = Sizes.FullWidth, int? textSize = null, Color? textColor = null, TextAlignmentOptions? textAlign = null, Color? panelColor = null)
            => await PopDialogue_Impl(text, cancellationToken, placement, size, textSize, textColor, textAlign, panelColor, borderSize: null, borderColor: null);

        private static async Awaitable<Result<bool>> PopDialogue_Impl(string text, CancellationToken cancellationToken, Placements placement, Sizes size, int? textSize, Color? textColor, TextAlignmentOptions? textAlign, Color? panelColor, int? borderSize, Color? borderColor)
        {
            EnsureSystem();

            var pages = PaginateDialogueWithMarkup(text, size, textSize);
            if (pages == null || pages.Count == 0) pages = new List<string> { "" };

            var panel = _pool.GetOrCreate(PopupKind.Dialogue);
            _stack.Push(panel);

            bool didFade = false;






            for (int i = 0; i < pages.Count; i++)
            {
                var pageText = pages[i];
                panel.ConfigureDialogue(pageText, i + 1, pages.Count, placement, size, textSize, textColor, textAlign ?? TextAlignmentOptions.Left, panelColor);

                if (!didFade)
                {
                    didFade = true;
                    panel.SetBorder(borderSize, borderColor);
                    await panel.FadeIn(cancellationToken);
                }




                if (UConfig.TRACE) Debug.Log($"[U] PopDialogue page {i + 1}/{pages.Count}");
                V.Broadcast<DialoguePopped>(pageText);

                var res = await panel.AwaitConfirmCancel(cancellationToken);
                if (res.Cancelled)
                {
                    _stack.Pop();
            await panel.FadeOut(CancellationToken.None);
                    panel.ReturnToPool(PopupKind.Dialogue);
                    return new Result<bool>(false, true);
                }

                // Prevent 'one press' from skipping multiple pages in the same frame.
                if (i < pages.Count - 1)
                    await Awaitable.NextFrameAsync(cancellationToken);
            }

            _stack.Pop();
            await panel.FadeOut(CancellationToken.None);
            panel.ReturnToPool(PopupKind.Dialogue);
            return new Result<bool>(true, false);
        }

        public static async Awaitable<Result<string>> PopChoice(string prompt, int? textSize = null, Color? textColor = null, TextAlignmentOptions? textAlign = null, Color? panelColor = null, int? borderSize = null, Color? borderColor = null, params Option[] options)
            => await PopChoice(prompt, options, cancellationToken: default, placement: Placements.TopCenter, description: null, textSize: textSize, textColor: textColor, textAlign: textAlign, panelColor: panelColor, borderSize: borderSize, borderColor: borderColor);

        public static async Awaitable<Result<string>> PopChoice(string prompt, Option[] options, CancellationToken cancellationToken = default, Placements placement = Placements.TopCenter, Func<string, string> description = null, int? textSize = null, Color? textColor = null, TextAlignmentOptions? textAlign = null, Color? panelColor = null, int? borderSize = null, Color? borderColor = null)
        {
            EnsureSystem();

            if (options == null) options = Array.Empty<Option>();
            if (options.Length == 0)
                return Result<string>.Canceled();

            // Choice is now two panels:
            // 1) Prompt panel (dialogue style, supports pagination)
            // 2) Options panel (tight list under the prompt, right-aligned to the prompt)
            var promptPanel = _pool.GetOrCreate(PopupKind.Dialogue);
            var optionsPanel = _pool.GetOrCreate(PopupKind.Choice);

            // Prompt panel defaults to dialogue-modal width (matches JRPG vibe nicely).
            var pages = PaginateDialogueWithMarkup(prompt ?? "", size: Sizes.Modal, textSizeOverride: textSize);
            int pageCount = Mathf.Max(1, pages.Count);

            _stack.Push(promptPanel);

            if (UConfig.TRACE) Debug.Log($"[U] PopChoice: {prompt} ({options.Length} options)");

            for (int i = 0; i < pageCount; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _stack.Pop();
                    promptPanel.ReturnToPool(PopupKind.Dialogue);
                    optionsPanel.ReturnToPool(PopupKind.Choice);
                    return Result<string>.Canceled();
                }

                promptPanel.ConfigureDialogue(
                    text: pages[Mathf.Clamp(i, 0, pages.Count - 1)],
                    pageIndex: i + 1,
                    pageCount: pageCount,
                    placement: placement,
                    size: Sizes.Modal,
                    textSize: textSize,
                    textColor: textColor,
                    textAlign: textAlign ?? TextAlignmentOptions.Left,
                    panelColor: panelColor
                );
                promptPanel.SetBorder(borderSize ?? UConfig.BorderSize, borderColor ?? UConfig.BorderColor);

                // Only fade in once at the beginning.
                if (i == 0) await promptPanel.FadeIn(cancellationToken);

                // On non-final pages: confirm advances, cancel cancels.
                if (i < pageCount - 1)
                {
                    var step = await promptPanel.AwaitConfirmCancel(cancellationToken);
                    if (step.Cancelled || step.Value == false)
                    {
                        _stack.Pop();
                        await promptPanel.FadeOut(CancellationToken.None);
                        promptPanel.ReturnToPool(PopupKind.Dialogue);
                        optionsPanel.ReturnToPool(PopupKind.Choice);

                        V.Broadcast<ChoiceCanceled>(prompt ?? "Choice");
                        _history.Add($"CANCEL: {prompt}");
                        return Result<string>.Canceled();
                    }

                    // Prevent one press from skipping multiple pages in the same frame.
                    await Awaitable.NextFrameAsync(cancellationToken);
                }
            }

            // Now that the last page is visible, show the options panel beneath it.
            optionsPanel.ConfigureChoiceOptionsOnly(options, placement, description, textSize, textColor, textAlign, panelColor);
            optionsPanel.SetBorder(borderSize ?? UConfig.BorderSize, borderColor ?? UConfig.BorderColor);
            optionsPanel.AlignBelowRightOf(promptPanel, gapPx: 10);
            _stack.Push(optionsPanel);
            await optionsPanel.FadeIn(cancellationToken);

            var res = await optionsPanel.AwaitChoice(cancellationToken);

            _stack.Pop(); // options panel
            _stack.Pop(); // prompt panel

            await optionsPanel.FadeOut(CancellationToken.None);
            optionsPanel.ReturnToPool(PopupKind.Choice);

            await promptPanel.FadeOut(CancellationToken.None);
            promptPanel.ReturnToPool(PopupKind.Dialogue);

            if (res.Cancelled)
            {
                V.Broadcast<ChoiceCanceled>(prompt ?? "Choice");
                _history.Add($"CANCEL: {prompt}");
                return Result<string>.Canceled();
            }

            V.Broadcast<ChoiceMade>(res.Value);
            _history.Add(res.Value);

            return res;
        }


        // Student-first overloads (avoid accidental binding of option strings to textSize)
        public static async Awaitable<Result<string>> PopChoice(
            string prompt,
            string optionA,
            string optionB,
            CancellationToken cancellationToken = default,
            Placements placement = Placements.TopCenter,
            Func<string, string> description = null,
            int? textSize = null,
            Color? textColor = null,
            TextAlignmentOptions? textAlign = null,
            Color? panelColor = null,
            int? borderSize = null,
            Color? borderColor = null)
        {
            return await PopChoice(prompt, new[] { optionA, optionB }, cancellationToken: cancellationToken, placement: placement, description: description, textSize: textSize, textColor: textColor, textAlign: textAlign, panelColor: panelColor, borderSize: borderSize, borderColor: borderColor);
        }

        public static async Awaitable<Result<string>> PopChoice(
            string prompt,
            string optionA,
            string optionB,
            string optionC,
            CancellationToken cancellationToken = default,
            Placements placement = Placements.TopCenter,
            Func<string, string> description = null,
            int? textSize = null,
            Color? textColor = null,
            TextAlignmentOptions? textAlign = null,
            Color? panelColor = null,
            int? borderSize = null,
            Color? borderColor = null)
        {
            return await PopChoice(prompt, new[] { optionA, optionB, optionC }, cancellationToken: cancellationToken, placement: placement, description: description, textSize: textSize, textColor: textColor, textAlign: textAlign, panelColor: panelColor, borderSize: borderSize, borderColor: borderColor);
        }

        public static async Awaitable<Result<string>> PopChoice(string prompt, int? textSize = null, Color? textColor = null, TextAlignmentOptions? textAlign = null, Color? panelColor = null, int? borderSize = null, Color? borderColor = null, params string[] options)
            => await PopChoice(prompt, options, cancellationToken: default, placement: Placements.TopCenter, description: null, textSize: textSize, textColor: textColor, textAlign: textAlign, panelColor: panelColor, borderSize: borderSize, borderColor: borderColor);

        public static async Awaitable<Result<string>> PopChoice(string prompt, string[] options, CancellationToken cancellationToken = default, Placements placement = Placements.TopCenter, Func<string, string> description = null, int? textSize = null, Color? textColor = null, TextAlignmentOptions? textAlign = null, Color? panelColor = null, int? borderSize = null, Color? borderColor = null)
        {
            if (options == null) options = Array.Empty<string>();
            var opts = ToOptions(options);
            return await PopChoice(prompt, opts, cancellationToken: cancellationToken, placement: placement, description: description, textSize: textSize, textColor: textColor, textAlign: textAlign, panelColor: panelColor, borderSize: borderSize, borderColor: borderColor);
        }

        public static async Awaitable<Result<string>> Menu(string title, params string[] options)
            => await PopChoice(title, options);

        public static IDisplayHandle Display(Func<string> text, Placements placement = Placements.TopLeft, Sizes size = Sizes.Inline, Color? background = null, Color? textColor = null, int paddingPx = 8)
        {
            EnsureSystem();
            // Display is non-interactive; no EventSystem needed.
            return _display.Create(text, placement, size, background ?? new Color(0,0,0,0), textColor ?? UConfig.TextColor, paddingPx);
        }

        public static IReadOnlyList<string> History => _history.Items;

        // ============================================================
        // Internal systems
        // ============================================================

        private enum PopupKind { Banner, Dialogue, Choice }

                private static TMP_FontAsset _defaultFontAsset;
        private static TMP_FontAsset DefaultFontAsset
        {
            get
            {
                if (_defaultFontAsset != null) return _defaultFontAsset;
                // Priority: user config -> package fallback -> TMP project default
                _defaultFontAsset = UConfig.FontAsset;
                if (_defaultFontAsset != null) return _defaultFontAsset;
                _defaultFontAsset = (UConfig.StylePreset == UStylePreset.JRPG)
                    ? Resources.Load<TMP_FontAsset>("Default/Default SDF")
                    : Resources.Load<TMP_FontAsset>("Fonts/Liberation/LiberationSans SDF");
                if (_defaultFontAsset != null) return _defaultFontAsset;
                _defaultFontAsset = TMP_Settings.defaultFontAsset;
                if (_defaultFontAsset != null) return _defaultFontAsset;
                return _defaultFontAsset;
            }
        }

private static bool _initialized;
        private static UISystem _sys;
        private static PanelPool _pool;
        private static PanelStack _stack;
        private static DisplaySystem _display;
        private static HistoryRing _history;

        private static void EnsureSystem()
        {
            if (_initialized) return;

            UConfig.ApplyUserOverrides();
            UConfig.ApplyStylePreset();
            _sys = new UISystem();
            _pool = new PanelPool(_sys);
            _stack = new PanelStack();
            _display = new DisplaySystem(_sys);
            _history = new HistoryRing(UConfig.HistoryMax);

            _initialized = true;
        }

        private static string Clamp(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Length <= max) return s;
            return s.Substring(0, max - 1) + "…";
        }


private static string ClampRichText(string s, int maxVisibleChars)
{
    if (string.IsNullOrEmpty(s)) return "";
    if (maxVisibleChars <= 0) return s;

    // Fast path: no tags.
    if (s.IndexOf('<') < 0)
    {
        if (s.Length <= maxVisibleChars) return s;
        return s.Substring(0, Math.Max(0, maxVisibleChars - 1)) + "…";
    }

    int visible = 0;
    bool inTag = false;
    int cut = s.Length;

    for (int i = 0; i < s.Length; i++)
    {
        char c = s[i];

        if (c == '<') { inTag = true; continue; }
        if (inTag)
        {
            if (c == '>') inTag = false;
            continue;
        }

        visible++;
        if (visible >= maxVisibleChars)
        {
            cut = i + 1;
            break;
        }
    }

    if (cut >= s.Length) return s;

    return s.Substring(0, Math.Max(0, cut - 1)) + "…";
}


        private static List<string> Paginate(string text, int maxChars)
        {
            var pages = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                pages.Add("");
                return pages;
            }

            int i = 0;
            while (i < text.Length)
            {
                int len = Math.Min(maxChars, text.Length - i);
                int end = i + len;

                if (end < text.Length)
                {
                    int lastSpace = text.LastIndexOf(' ', end);
                    if (lastSpace > i + (maxChars / 2))
                        end = lastSpace;
                }

                string chunk = text.Substring(i, end - i).Trim();
                if (chunk.Length > 0) pages.Add(chunk);

                i = end;
            }

            if (pages.Count == 0) pages.Add("");
            return pages;
        }

        
// ----------------------------
// Markup preprocessing
// ----------------------------
// TMP already supports rich text tags like <b>, <i>, <size=..>, <color=#..>.
// We add two extra "pseudo-html" helpers:
//   <br/>  -> newline
//   <pbr/> -> forced page break (dialogue pagination)
//
// We also support a tiny convenience color syntax:
//   <color=Colors.gold> ... </color>
//   <color=U.Colors.gold> ... </color>
//
// Which gets converted into a TMP hex color tag:
//   <color=#FFD700> ... </color>
private static readonly System.Text.RegularExpressions.Regex _rxBr =
    new System.Text.RegularExpressions.Regex(@"<\s*br\s*/?\s*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

private static readonly System.Text.RegularExpressions.Regex _rxPbr =
    new System.Text.RegularExpressions.Regex(@"<\s*pbr\s*/?\s*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

private static readonly System.Text.RegularExpressions.Regex _rxColorOpen =
    new System.Text.RegularExpressions.Regex(@"<\s*color\s*=\s*([^>\s]+)\s*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

private static string PreprocessMarkupInline(string raw)
{
    if (string.IsNullOrEmpty(raw)) return raw ?? "";

    // Normalize newlines and convert <br/>
    string t = raw.Replace("\r\n", "\n").Replace("\r", "\n");
    t = _rxBr.Replace(t, "\n");

    // Convert convenience color tokens like Colors.gold
    t = _rxColorOpen.Replace(t, m =>
    {
        string val = m.Groups[1].Value ?? "";
        string lowered = val.Trim();

        // If it's already a hex color or a named TMP color, leave it alone.
        if (lowered.StartsWith("#")) return m.Value;
        if (lowered.IndexOf("Colors.", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Extract the part after the last dot.
            int dot = lowered.LastIndexOf('.');
            string name = dot >= 0 ? lowered.Substring(dot + 1) : lowered;

            if (TryResolveNamedColor(name, out var col))
            {
                string hex = "#" + ColorUtility.ToHtmlStringRGB(col);
                return $"<color={hex}>";
            }


        }

        return m.Value;
    });

    // If someone put <pbr/> in a banner or other non-paginated text,
    // treat it as a blank-line separator.
    t = _rxPbr.Replace(t, "\n\n");

    return t;
}

private static string PreprocessMarkup(string raw, bool allowPageBreaks)
{
    var s = PreprocessMarkupInline(raw);

    if (!allowPageBreaks)
    {
        // For non-paginated contexts (HUD displays), treat page breaks as simple newlines.
        s = _rxPbr.Replace(s, "\n");
    }

    return s;
}


private static string[] SplitByForcedPageBreaks(string raw)
{
    if (raw == null) return new[] { "" };

    // Convert <br/> and color tokens first, BUT keep <pbr/> for splitting.
    string t = raw.Replace("\r\n", "\n").Replace("\r", "\n");
    t = _rxBr.Replace(t, "\n");
    t = _rxColorOpen.Replace(t, m =>
    {
        string val = m.Groups[1].Value ?? "";
        string lowered = val.Trim();

        if (lowered.StartsWith("#")) return m.Value;
        if (lowered.IndexOf("Colors.", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            int dot = lowered.LastIndexOf('.');
            string name = dot >= 0 ? lowered.Substring(dot + 1) : lowered;

            if (TryResolveNamedColor(name, out var col))
            {
                string hex = "#" + ColorUtility.ToHtmlStringRGB(col);
                return $"<color={hex}>";
            }
        }

        return m.Value;
    });

    // Now split on <pbr/>
    return _rxPbr.Split(t);
}

private static bool TryResolveNamedColor(string name, out Color color)
{
    color = default;

    if (string.IsNullOrWhiteSpace(name)) return false;

    switch (name.Trim().ToLowerInvariant())
    {
        case "gold":
            color = UConfig.SelectedTextColor;
            return true;

        case "white":
            color = Color.white;
            return true;

        case "black":
            color = Color.black;
            return true;

        case "red":
            color = Color.red;
            return true;

        case "green":
            color = Color.green;
            return true;

        case "blue":
            color = Color.blue;
            return true;

        case "cyan":
            color = Color.cyan;
            return true;

        case "magenta":
            color = Color.magenta;
            return true;

        case "yellow":
            color = Color.yellow;
            return true;
    }

    return false;
}

private static List<string> PaginateDialogueWithMarkup(string raw, Sizes size, int? textSizeOverride)
{
    var segments = SplitByForcedPageBreaks(raw);
    var pages = new List<string>();

    foreach (var seg in segments)
    {
        string cleaned = PreprocessMarkupInline(seg);

        // If the segment is empty, still force a page.
        if (string.IsNullOrEmpty(cleaned))
        {
            pages.Add("");
            continue;
        }

        var segPages = PaginateDialogue(cleaned, size, textSizeOverride);
        if (segPages == null || segPages.Count == 0) pages.Add(cleaned);
        else pages.AddRange(segPages);
    }

    if (pages.Count == 0) pages.Add("");
    return pages;
}

private static List<string> PaginateDialogue(string text, Sizes size, int? textSizeOverride)
        {
            // TMP-aware pagination using GetPreferredValues so pages fit the current box.
            // Still falls back to DialogueMaxCharsPerPage if TMP isn't available for some reason.
            if (string.IsNullOrEmpty(text)) return new List<string> { "" };

            int fontSize = textSizeOverride ?? UConfig.BodyFontSize;

            // Dialogue boxes always use DialogueHeightPct; width depends on size.
            float widthPct = size switch
            {
                Sizes.Inline => 0.5f,
                Sizes.FullWidth => 1f,
                Sizes.Modal => UConfig.ModalWidthPct,
                _ => 1f
            };

            float boxW = Mathf.Max(64f, Screen.width * widthPct - (UConfig.DialoguePaddingX * 2));
            float boxH = Mathf.Max(64f, Screen.height * UConfig.DialogueHeightPct - (UConfig.DialoguePaddingY * 2));

            var font = DefaultFontAsset;
            if (font == null)
            {
                // fallback: character chunking
                return ChunkByChars(text, UConfig.DialogueMaxCharsPerPage);
            }

            // Create (or reuse) a hidden TMP text for measurement.
            TMP_Text measurer = _paginationMeasurer;
            if (measurer == null)
            {
                var go = new GameObject("U_TMP_PaginationMeasurer");
                go.hideFlags = HideFlags.HideAndDontSave;
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.raycastTarget = false;
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.overflowMode = TextOverflowModes.Overflow;
                measurer = tmp;
                _paginationMeasurer = tmp;
            }

            measurer.font = font;
            measurer.fontSize = fontSize;
            measurer.lineSpacing = UConfig.DialogueLineSpacing;
            measurer.alignment = TextAlignmentOptions.TopLeft;
            measurer.textWrappingMode = TextWrappingModes.Normal;
            measurer.overflowMode = TextOverflowModes.Overflow;

            // Build pages by greedily adding words until preferred height exceeds box.
            var tokenized = (text ?? "").Replace("\n", " \n ");
            var words = tokenized.Split(new[] { ' ' }, StringSplitOptions.None);
            var pages = new List<string>();
            var sb = new System.Text.StringBuilder();
            int i = 0;

            while (i < words.Length)
            {
                sb.Clear();
                float lastGoodHeight = 0f;
                int lastGoodIndex = i;

                // If a single "word" is enormous (no spaces), we still need progress.
                // We'll chunk it by chars if it won't fit.
                bool progressed = false;

                for (int j = i; j < words.Length; j++)
                {
                    string tok = words[j];
                    if (tok == "\n")
                    {
                        sb.Append('\n');
                    }
                    else
                    {
                        if (sb.Length > 0)
                        {
                            // Avoid leading spaces right after a newline.
                            if (sb[sb.Length - 1] != '\n') sb.Append(' ');
                        }
                        sb.Append(tok);
                    }

                    measurer.text = sb.ToString();
                    measurer.ForceMeshUpdate();

                    var pref = measurer.GetPreferredValues(measurer.text, boxW, 0f);
                    float h = pref.y;

                    if (h <= boxH)
                    {
                        lastGoodHeight = h;
                        lastGoodIndex = j + 1;
                        progressed = true;
                    }
                    else
                    {
                        break;
                    }
                }

                if (!progressed)
                {
                    // Can't fit even one word. Chunk by chars as a last resort.
                    string remaining = string.Join(" ", words, i, words.Length - i);
                    var chunks = ChunkByChars(remaining, Mathf.Max(20, UConfig.DialogueMaxCharsPerPage / 2));
                    pages.AddRange(chunks);
                    break;
                }

                string page = string.Join(" ", words, i, lastGoodIndex - i);
                pages.Add(page);

                i = lastGoodIndex;
            }

            if (pages.Count == 0) pages.Add(text);
            return pages;
        }

        private static TMP_Text _paginationMeasurer;

        private static List<string> ChunkByChars(string text, int maxChars)
        {
            var pages = new List<string>();
            int idx = 0;
            while (idx < text.Length)
            {
                int len = Mathf.Min(maxChars, text.Length - idx);
                pages.Add(text.Substring(idx, len));
                idx += len;
            }
            if (pages.Count == 0) pages.Add(text);
            return pages;
        }


        // ============================================================
        // UISystem
        // ============================================================

        private sealed class UISystem
        {
            public readonly GameObject Root;
            public readonly Canvas Canvas;
            public Vector2 CanvasSize => ((RectTransform)Root.transform).rect.size;

            public UISystem()
            {
                // Reuse if already present (helps when domain-reload settings cause statics/objects to desync).
                var existing = GameObject.Find("U_Root");
                if (existing != null && existing.GetComponent<Canvas>() != null)
                {
                    Root = existing;
                    if (Root.GetComponent<RectTransform>() == null) Root.AddComponent<RectTransform>();
                    Canvas = Root.GetComponent<Canvas>();
                    this.EnsureCanvasScaler();
                    if (Canvas == null) Canvas = Root.AddComponent<Canvas>();
                }
                else
                {
                    Root = new GameObject("U_Root", typeof(RectTransform));
                    UnityEngine.Object.DontDestroyOnLoad(Root);

                    Canvas = Root.AddComponent<Canvas>();
                    Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    Canvas.sortingOrder = 6000;

                    this.EnsureCanvasScaler();
                    Root.AddComponent<GraphicRaycaster>();
                }

                // Ensure required components exist
                if (Root.GetComponent<CanvasScaler>() == null)
                {
                    this.EnsureCanvasScaler();
                }
                if (Root.GetComponent<GraphicRaycaster>() == null)
                {
                    Root.AddComponent<GraphicRaycaster>();
                }

                Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Canvas.sortingOrder = 6000;

                // EventSystem is created lazily only when interactive UI is shown.
            }


            private void EnsureCanvasScaler()
            {
                var scaler = Root.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler == null) scaler = Root.AddComponent<UnityEngine.UI.CanvasScaler>();

                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = UConfig.ReferenceResolution;
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            private static bool _eventSystemEnsured;

            public static void EnsureEventSystem()
            {
                if (_eventSystemEnsured) return;
                _eventSystemEnsured = true;

                if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
                    return;

                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();

                // Prefer the new Input System UI module if available.
                var isuiType =
                    Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem") ??
                    Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem.UI");

                if (isuiType != null)
                {
                    es.AddComponent(isuiType);
                }
                else
                {
                    // Fallback: legacy module (will throw if project is set to "Input System Package (New)" only).
                    es.AddComponent<StandaloneInputModule>();
                    Debug.LogWarning("[U] Using StandaloneInputModule (legacy). If your project uses the new Input System only, install/enable the Input System package UI module (InputSystemUIInputModule).");
                }

                UnityEngine.Object.DontDestroyOnLoad(es);
            }

            public TMP_FontAsset ResolveFont()
            {
                return DefaultFontAsset;
            }

            public static RectTransform CreateRect(string name, Transform parent)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return go.GetComponent<RectTransform>();
            }

            public static TMP_Text AddTMP(GameObject go, TMP_FontAsset font, int size, Color color, TextAlignmentOptions align)
            {
                var t = go.AddComponent<TextMeshProUGUI>();
                if (font != null) t.font = font;
                t.fontSize = size;
                t.color = color;
                t.alignment = align;
                t.textWrappingMode = TextWrappingModes.Normal;
                t.raycastTarget = false;
                return t;
            }
        }

        // ============================================================
        // UIPanel
        // ============================================================

        private sealed class UIPanel
        {
            private readonly UISystem _sys;
            private readonly GameObject _go;
            private readonly RectTransform _rt;

            private Image _bg;
            private CanvasGroup _cg;
            private Image _borderTop, _borderBottom, _borderLeft, _borderRight;
            private int? _overrideBorderSize;
            private Color? _overrideBorderColor;

            private Color? _overridePanelColor;
            private TMP_Text _text;
            private int? _overrideTextSize;
            private Color? _overrideTextColor;
            private TextAlignmentOptions? _overrideTextAlign;

            private TMP_Text _title;
            private readonly List<ChoiceRow> _rows = new List<ChoiceRow>();
            private int _selected;

            private TMP_Text _page;

            private TMP_Text _desc;
            private Func<string, string> _descFn;

            public UIPanel(UISystem sys)
            {
                _sys = sys;
                _go = new GameObject("U_Panel", typeof(RectTransform));
                _go.transform.SetParent(sys.Root.transform, false);
                _go.transform.SetAsLastSibling();
                _rt = _go.GetComponent<RectTransform>();

                _bg = _go.AddComponent<Image>();
                _bg.color = UConfig.PanelColor;
                 _cg = _go.AddComponent<CanvasGroup>();
                 _cg.alpha = 1f;
                 _cg.interactable = true;
                 _cg.blocksRaycasts = true;


                _go.SetActive(false);
            }

            public void ReturnToPool(PopupKind kind)
            {
                _go.SetActive(false);
                _pool.Return(kind, this);
            }

            public void ConfigureBanner(string text, Placements placement, Sizes size, int? textSize, Color? textColor, TextAlignmentOptions? textAlign, Color? panelColor)
            {
                EnsureText();

                // Always reset to defaults first (panels are pooled).
                _overrideTextSize = textSize;
                _overrideTextColor = textColor;
                _overrideTextAlign = textAlign;
                _overridePanelColor = panelColor;

                _go.name = "U_Banner";
                _go.SetActive(true);
                _bg.color = _overridePanelColor ?? UConfig.PanelColor;

                _text.rectTransform.offsetMin = new Vector2(UConfig.BannerPaddingX, UConfig.BannerPaddingY);
                _text.rectTransform.offsetMax = new Vector2(-UConfig.BannerPaddingX, -UConfig.BannerPaddingY);
                _text.lineSpacing = UConfig.BannerLineSpacing;

                _text.font = _sys.ResolveFont();
                _text.fontSize = UConfig.BannerFontSize;
                _text.color = UConfig.TextColor;
                _text.alignment = U.AlignmentForPlacement(placement, fallback: TextAlignmentOptions.Center);

                if (_overrideTextSize.HasValue) _text.fontSize = _overrideTextSize.Value;
                if (_overrideTextColor.HasValue) _text.color = _overrideTextColor.Value;
                if (_overrideTextAlign.HasValue) _text.alignment = _overrideTextAlign.Value;

                _text.text = text;

                ApplyPlacement(placement, widthPct: size == Sizes.FullWidth ? 1f : 0.5f, heightPct: UConfig.BannerHeightPct);
                ClearChoice();
                ClearPage();
            }

            public void ConfigureDialogue(string text, int pageIndex, int pageCount, Placements placement, Sizes size, int? textSize, Color? textColor, TextAlignmentOptions? textAlign, Color? panelColor)
            {
                EnsureText();

                // Always reset to defaults first (panels are pooled, so we must not "leak" prior overrides).
                _overrideTextSize = textSize;
                _overrideTextColor = textColor;
                _overrideTextAlign = textAlign;
                _overridePanelColor = panelColor;

                _go.name = "U_Dialogue";
                _go.SetActive(true);

                _bg.color = _overridePanelColor ?? UConfig.PanelColor;

                // Dialogue padding + readable line spacing.
                _text.rectTransform.offsetMin = new Vector2(UConfig.DialoguePaddingX, UConfig.DialoguePaddingY);
                _text.rectTransform.offsetMax = new Vector2(-UConfig.DialoguePaddingX, -UConfig.DialoguePaddingY);
                _text.lineSpacing = UConfig.DialogueLineSpacing;

                // Reset typography to config defaults, then apply per-call overrides.
                _text.font = _sys.ResolveFont();
                _text.fontSize = UConfig.DialogueFontSize;
                _text.color = UConfig.TextColor;
                _text.alignment = U.AlignmentForPlacement(placement, fallback: TextAlignmentOptions.Left);

                if (_overrideTextSize.HasValue) _text.fontSize = _overrideTextSize.Value;
                if (_overrideTextColor.HasValue) _text.color = _overrideTextColor.Value;
                if (_overrideTextAlign.HasValue) _text.alignment = _overrideTextAlign.Value;

                _text.text = text;

                ApplyPlacement(placement, widthPct: size switch { Sizes.Inline => 0.5f, Sizes.FullWidth => 1f, Sizes.Modal => UConfig.ModalWidthPct, _ => 1f }, heightPct: UConfig.DialogueHeightPct);

                EnsurePage();
                _page.text = pageCount > 1 ? $"{pageIndex}/{pageCount}" : "";
                ClearChoice();
            }

            public void ConfigureChoice(string prompt, string[] options, Placements placement, int? textSize, Color? textColor, TextAlignmentOptions? textAlign, Color? panelColor)
            {
                var opts = new Option[options.Length];
                for (int i = 0; i < options.Length; i++) opts[i] = new Option(PreprocessMarkupInline(options[i]));
                ConfigureChoice(prompt, opts, placement, null, textSize, textColor, textAlign, panelColor);
            }

            public void ConfigureChoice(string prompt, Option[] options, Placements placement, Func<string, string> description, int? textSize, Color? textColor, TextAlignmentOptions? textAlign, Color? panelColor)
            {
                _go.name = "U_Choice";
                _go.SetActive(true);

                _overrideTextSize = textSize;
                _overrideTextColor = textColor;
                _overrideTextAlign = textAlign;

                ClearText();
                EnsureTitle();

                // Reset pooled title to defaults first (avoid leaking dialogue overrides).
                _title.font = _sys.ResolveFont();
                _title.fontSize = UConfig.ChoiceFontSize;
                _title.color = UConfig.TextColor;
                _title.alignment = U.AlignmentForPlacement(placement, fallback: TextAlignmentOptions.Left);


                BuildChoiceRows(options);
                _title.text = prompt ?? "Choose";
                if (_overrideTextSize.HasValue) _title.fontSize = _overrideTextSize.Value;
                if (_overrideTextColor.HasValue) _title.color = _overrideTextColor.Value;
                _title.alignment = _overrideTextAlign ?? U.AlignmentForPlacement(placement, fallback: TextAlignmentOptions.Left);

                _descFn = description;
                if (_descFn != null) EnsureDesc(); else ClearDesc();

                ApplyPlacement(placement, widthPct: UConfig.ModalWidthPct, heightPct: UConfig.ModalHeightPct);
                ClearPage();
                UpdateDesc();
            }


            // Options-only choice panel (used by PopChoice, where prompt is a separate dialogue/banner).
            public void ConfigureChoiceOptionsOnly(Option[] options, Placements placement, Func<string, string> description, int? textSize, Color? textColor, TextAlignmentOptions? textAlign, Color? panelColor)
            {
                _go.name = "U_ChoiceOptions";
                _go.SetActive(true);

                _overrideTextSize = textSize;
                _overrideTextColor = textColor;
                _overrideTextAlign = textAlign;
                _descFn = description;

                ClearText();
                ClearPage();
                ClearDesc();

                // Panel base look
                _overridePanelColor = panelColor;
                _bg.color = _overridePanelColor ?? UConfig.PanelColor;

                // Build options WITHOUT a title, and fit to content.
                BuildChoiceRowsOptionsOnly(options ?? Array.Empty<Option>());

                // Tight size based on content (students don't want a giant slab here).
                FitToChoiceContent(maxWidthPct: 0.9f);

                // We'll position relative to another panel (AlignBelowRightOf), so for now just center-ish.
                ApplyPlacement(placement, widthPct: _rt.rect.width / Mathf.Max(1f, _sys.CanvasSize.x), heightPct: _rt.rect.height / Mathf.Max(1f, _sys.CanvasSize.y));
            }

            public void AlignBelowRightOf(UIPanel other, float gapPx)
            {
                if (other == null) return;

                var corners = new Vector3[4];
                other._rt.GetWorldCorners(corners);

                float rightX = corners[2].x; // top-right
                float bottomY = corners[0].y; // bottom-left

                _rt.anchorMin = Vector2.zero;
                _rt.anchorMax = Vector2.zero;
                _rt.pivot = new Vector2(1, 1);

                _rt.position = new Vector3(rightX, bottomY - gapPx, 0);
            }

            private void BuildChoiceRowsOptionsOnly(Option[] options)
            {
                // Clear existing rows/title (and any prior desc).
                foreach (var r in _rows) r.Destroy();
                _rows.Clear();
                if (_title != null) { UnityEngine.Object.Destroy(_title.gameObject); _title = null; }

                // Remove old Options container(s) if they exist (best-effort cleanup).
                for (int i = _go.transform.childCount - 1; i >= 0; i--)
                {
                    var child = _go.transform.GetChild(i);
                    if (child != null && child.name == "Options") UnityEngine.Object.Destroy(child.gameObject);
                }

                var content = UISystem.CreateRect("Options", _go.transform);
                content.anchorMin = new Vector2(0, 0);
                content.anchorMax = new Vector2(1, 1);
                content.offsetMin = new Vector2(18, 18);
                content.offsetMax = new Vector2(-18, -18);

                var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.childControlHeight = true;
                vlg.childControlWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.spacing = Mathf.Max(UConfig.ChoiceSpacingPx, 18);

                for (int i = 0; i < options.Length; i++)
                {
                    var rowGo = new GameObject($"Option_{i}", typeof(RectTransform));
                    rowGo.transform.SetParent(content, false);
                    var rt = rowGo.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(0, 52);

                    var bg = rowGo.AddComponent<Image>();
                    bg.color = new Color(1, 1, 1, 0f);

                    Image iconImg = null;
                    if (options[i].Icon != null)
                    {
                        var iconRt = UISystem.CreateRect("Icon", rowGo.transform);
                        iconRt.anchorMin = new Vector2(0, 0.5f);
                        iconRt.anchorMax = new Vector2(0, 0.5f);
                        iconRt.pivot = new Vector2(0, 0.5f);
                        iconRt.anchoredPosition = new Vector2(10, 0);
                        iconRt.sizeDelta = new Vector2(28, 28);
                        iconImg = iconRt.gameObject.AddComponent<Image>();
                        iconImg.sprite = options[i].Icon;
                        iconImg.preserveAspect = true;
                    }

                    var labelRt = UISystem.CreateRect("Label", rowGo.transform);
                    labelRt.anchorMin = new Vector2(0, 0);
                    labelRt.anchorMax = new Vector2(1, 1);
                    labelRt.offsetMin = new Vector2(iconImg != null ? 48 : 12, 6);
                    labelRt.offsetMax = new Vector2(-12, -6);

                    var label = UISystem.AddTMP(labelRt.gameObject, _sys.ResolveFont(), _overrideTextSize ?? UConfig.ChoiceFontSize, _overrideTextColor ?? UConfig.TextColor, _overrideTextAlign ?? TextAlignmentOptions.Left);
                    label.text = options[i].Label;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.overflowMode = TextOverflowModes.Overflow;
                    label.lineSpacing = UConfig.ChoiceLineSpacing;

                // Ensure the row is tall enough for the current font, so labels don't overlap.
                // Ensure the row is tall enough for the current font, so labels don't overlap.
label.ForceMeshUpdate();
int fs = _overrideTextSize ?? UConfig.ChoiceFontSize;
int rowH = Mathf.CeilToInt(Mathf.Max(label.preferredHeight, fs * 1.2f) + 20f);
rt.sizeDelta = new Vector2(rt.sizeDelta.x, rowH);
var le = rowGo.AddComponent<LayoutElement>();
le.minHeight = rowH;
le.preferredHeight = rowH;
_rows.Add(new ChoiceRow(rowGo, bg, iconImg, label, options[i].Label));
                }

                _selected = 0;
                UpdateChoiceVisuals();
            }

            private void FitToChoiceContent(float maxWidthPct)
            {
                if (_rows.Count == 0) return;

                float maxW = 0f;
                for (int i = 0; i < _rows.Count; i++)
                {
                    var t = _rows[i].LabelText;
                    if (t == null) continue;
                    var pref = t.GetPreferredValues(t.text, 0, 0);
                    maxW = Mathf.Max(maxW, pref.x);
                }

                // icon(0 or 28) + paddings
                float iconW = 28f;
                float padX = 18f + 18f + 12f + 12f; // content + label paddings (rough)
                float w = Mathf.Min(_sys.CanvasSize.x * Mathf.Clamp01(maxWidthPct), maxW + iconW + padX);

                float gap = Mathf.Max(UConfig.ChoiceSpacingPx, 18);
                float rowsH = 0f;
                for (int i = 0; i < _rows.Count; i++)
                {
                    var rt = _rows[i].Go != null ? _rows[i].Go.GetComponent<RectTransform>() : null;
                    rowsH += rt != null ? rt.sizeDelta.y : 52f;
                }
                float h = 18f + 18f + rowsH + ((_rows.Count - 1) * gap) + 10f; // extra breathing room

                _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            }

            public async Awaitable<Result<bool>> AwaitConfirmCancel(CancellationToken ct)
            {
                while (true)
                {
                    if (ct.IsCancellationRequested) return new Result<bool>(false, true);

                    if (InputConfirm()) return new Result<bool>(true, false);
                    if (InputCancel()) return new Result<bool>(false, true);

                    await Awaitable.NextFrameAsync(ct);
                }
            }

            public async Awaitable<Result<string>> AwaitChoice(CancellationToken ct)
            {
                _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, _rows.Count - 1));
                UpdateChoiceVisuals();
                UpdateDesc();

                while (true)
                {
                    if (ct.IsCancellationRequested) return Result<string>.Canceled();

                    if (_rows.Count > 0)
                    {
                        if (InputUp())
                        {
                            _selected = (_selected - 1 + _rows.Count) % _rows.Count;
                            UpdateChoiceVisuals();
                            UpdateDesc();
                        }
                        else if (InputDown())
                        {
                            _selected = (_selected + 1) % _rows.Count;
                            UpdateChoiceVisuals();
                            UpdateDesc();
                        }
                    }

                    if (InputConfirm())
                    {
                        if (_rows.Count == 0) return Result<string>.Canceled();
                        var value = _rows[_selected].Label;
                        return Result<string>.Ok(value);
                    }

                    if (InputCancel())
                        return Result<string>.Canceled();

                    await Awaitable.NextFrameAsync(ct);
                }
            }

            private void ApplyPlacement(Placements placement, float widthPct, float heightPct)
            {
                var cs = _sys.CanvasSize;
                float w = cs.x * Mathf.Clamp01(widthPct);
                float h = cs.y * Mathf.Clamp01(heightPct);

                _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);

                Vector2 anchor = placement switch
                {
                    Placements.TopLeft => new Vector2(0, 1),
                    Placements.TopCenter => new Vector2(0.5f, 1),
                    Placements.TopRight => new Vector2(1, 1),

                    Placements.MiddleLeft => new Vector2(0, 0.5f),
                    Placements.MiddleCenter => new Vector2(0.5f, 0.5f),
                    Placements.MiddleRight => new Vector2(1, 0.5f),

                    Placements.BottomLeft => new Vector2(0, 0),
                    Placements.BottomCenter => new Vector2(0.5f, 0),
                    Placements.BottomRight => new Vector2(1, 0),
                    _ => new Vector2(0.5f, 0.5f)
                };

                _rt.anchorMin = anchor;
                _rt.anchorMax = anchor;
                _rt.pivot = anchor;

                float pad = UConfig.DisplayMarginPx;
                Vector2 offset = anchor switch
                {
                    var a when a == new Vector2(0,1) => new Vector2(pad, -pad),
                    var a when a == new Vector2(0.5f,1) => new Vector2(0, -pad),
                    var a when a == new Vector2(1,1) => new Vector2(-pad, -pad),

                    var a when a == new Vector2(0,0.5f) => new Vector2(pad, 0),
                    var a when a == new Vector2(0.5f,0.5f) => Vector2.zero,
                    var a when a == new Vector2(1,0.5f) => new Vector2(-pad, 0),

                    var a when a == new Vector2(0,0) => new Vector2(pad, pad),
                    var a when a == new Vector2(0.5f,0) => new Vector2(0, pad),
                    var a when a == new Vector2(1,0) => new Vector2(-pad, pad),
                    _ => Vector2.zero
                };

                _rt.anchoredPosition = offset;
            }

            private void EnsureBorder()
            {
                if (_borderTop != null) return;

                _borderTop = MakeBorderSide("U_Border_Top");
                _borderBottom = MakeBorderSide("U_Border_Bottom");
                _borderLeft = MakeBorderSide("U_Border_Left");
                _borderRight = MakeBorderSide("U_Border_Right");

                LayoutBorder();
                SetBorder(_overrideBorderSize, _overrideBorderColor);
            }

            private Image MakeBorderSide(string name)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(_go.transform, false);
                var img = go.AddComponent<Image>();
                img.raycastTarget = false;
                img.color = UConfig.BorderColor;
                return img;
            }

            private void LayoutBorder()
            {
                int px = _overrideBorderSize ?? UConfig.BorderSize;
                if (px < 0) px = 0;

                // Top
                SetupSide(_borderTop.rectTransform, anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1), pivot: new Vector2(0.5f, 1),
                    offsetMin: new Vector2(0, -px), offsetMax: new Vector2(0, 0));
                // Bottom
                SetupSide(_borderBottom.rectTransform, anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 0), pivot: new Vector2(0.5f, 0),
                    offsetMin: new Vector2(0, 0), offsetMax: new Vector2(0, px));
                // Left
                SetupSide(_borderLeft.rectTransform, anchorMin: new Vector2(0, 0), anchorMax: new Vector2(0, 1), pivot: new Vector2(0, 0.5f),
                    offsetMin: new Vector2(0, 0), offsetMax: new Vector2(px, 0));
                // Right
                SetupSide(_borderRight.rectTransform, anchorMin: new Vector2(1, 0), anchorMax: new Vector2(1, 1), pivot: new Vector2(1, 0.5f),
                    offsetMin: new Vector2(-px, 0), offsetMax: new Vector2(0, 0));
            }

            private static void SetupSide(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
            {
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot = pivot;
                rt.offsetMin = offsetMin;
                rt.offsetMax = offsetMax;
            }

            public void SetBorder(int? borderSize, Color? borderColor)
            {
                _overrideBorderSize = borderSize;
                _overrideBorderColor = borderColor;
                EnsureBorder();

                int px = borderSize ?? UConfig.BorderSize;
                if (px <= 0)
                {
                    _borderTop.gameObject.SetActive(false);
                    _borderBottom.gameObject.SetActive(false);
                    _borderLeft.gameObject.SetActive(false);
                    _borderRight.gameObject.SetActive(false);
                    return;
                }

                _borderTop.gameObject.SetActive(true);
                _borderBottom.gameObject.SetActive(true);
                _borderLeft.gameObject.SetActive(true);
                _borderRight.gameObject.SetActive(true);

                var c = borderColor ?? UConfig.BorderColor;
                _borderTop.color = c;
                _borderBottom.color = c;
                _borderLeft.color = c;
                _borderRight.color = c;
                LayoutBorder();
            }

            public async Awaitable FadeIn(CancellationToken ct)
            {
                if (!UConfig.PopFadeIn) { _cg.alpha = 1f; _cg.interactable = true; _cg.blocksRaycasts = true; return; }
                float d = Mathf.Max(0.001f, UConfig.PopFadeDuration);
                _cg.alpha = 0f;
                _cg.interactable = false;
                _cg.blocksRaycasts = false;
                float t = 0f;
                while (t < d)
                {
                    t += Time.unscaledDeltaTime;
                    _cg.alpha = Mathf.Clamp01(t / d);
                    await Awaitable.NextFrameAsync(ct);
                }
                _cg.alpha = 1f;
                _cg.interactable = true;
                _cg.blocksRaycasts = true;
            }

            public async Awaitable FadeOut(CancellationToken ct)
            {
                if (!UConfig.PopFadeOut) { _cg.alpha = 0f; _cg.interactable = false; _cg.blocksRaycasts = false; return; }
                float d = Mathf.Max(0.001f, UConfig.PopFadeDuration);
                _cg.interactable = false;
                _cg.blocksRaycasts = false;
                float t = 0f;
                while (t < d)
                {
                    t += Time.unscaledDeltaTime;
                    _cg.alpha = 1f - Mathf.Clamp01(t / d);
                    await Awaitable.NextFrameAsync(ct);
                }
                _cg.alpha = 0f;
            }

            private void EnsureText()
            {
                if (_text != null) return;

                var font = _sys.ResolveFont();

                var textGo = UISystem.CreateRect("Text", _go.transform);
                textGo.anchorMin = new Vector2(0, 0);
                textGo.anchorMax = new Vector2(1, 1);
                textGo.offsetMin = new Vector2(18, 14);
                textGo.offsetMax = new Vector2(-18, -14);

                _text = UISystem.AddTMP(textGo.gameObject, font, UConfig.BodyFontSize , UConfig.TextColor, TextAlignmentOptions.Center);
            }

            private void ClearText()
            {
                if (_text != null) UnityEngine.Object.Destroy(_text.gameObject);
                _text = null;
            }

            private void EnsureTitle()
            {
                if (_title != null) return;

                var font = _sys.ResolveFont();

                var titleGo = UISystem.CreateRect("Title", _go.transform);
                titleGo.anchorMin = new Vector2(0, 1);
                titleGo.anchorMax = new Vector2(1, 1);
                titleGo.pivot = new Vector2(0.5f, 1);
                titleGo.offsetMin = new Vector2(18, -52);
                titleGo.offsetMax = new Vector2(-18, -12);

                _title = UISystem.AddTMP(titleGo.gameObject, font, UConfig.TitleFontSize, UConfig.TextColor, TextAlignmentOptions.Left);
            }

            private void ClearChoice()
            {
                foreach (var r in _rows)
                    r.Destroy();
                _rows.Clear();

                if (_title != null) { UnityEngine.Object.Destroy(_title.gameObject); _title = null; }
            }

            private void BuildChoiceRows(Option[] options)
            {
                ClearChoice();
                EnsureTitle();

                var content = UISystem.CreateRect("Options", _go.transform);
                content.anchorMin = new Vector2(0, 0);
                content.anchorMax = new Vector2(1, 1);
                content.offsetMin = new Vector2(18, 18);
                content.offsetMax = new Vector2(-18, -70);

                var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.childControlHeight = true;
                vlg.childControlWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.spacing = UConfig.ChoiceSpacingPx;

                for (int i = 0; i < options.Length; i++)
                {
                    var rowGo = new GameObject($"Option_{i}", typeof(RectTransform));
                    rowGo.transform.SetParent(content, false);
                    var rt = rowGo.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(0, 40);

                    var bg = rowGo.AddComponent<Image>();
                    bg.color = new Color(1,1,1,0f);

                    // Optional icon
                    Image iconImg = null;
                    if (options[i].Icon != null)
                    {
                        var iconRt = UISystem.CreateRect("Icon", rowGo.transform);
                        iconRt.anchorMin = new Vector2(0, 0.5f);
                        iconRt.anchorMax = new Vector2(0, 0.5f);
                        iconRt.pivot = new Vector2(0, 0.5f);
                        iconRt.anchoredPosition = new Vector2(10, 0);
                        iconRt.sizeDelta = new Vector2(28, 28);
                        iconImg = iconRt.gameObject.AddComponent<Image>();
                        iconImg.sprite = options[i].Icon;
                        iconImg.preserveAspect = true;
                    }


                    var labelRt = UISystem.CreateRect("Label", rowGo.transform);
                    labelRt.anchorMin = new Vector2(0, 0);
                    labelRt.anchorMax = new Vector2(1, 1);
                     labelRt.offsetMin = new Vector2(iconImg != null ? 48 : 12, 6);
                    labelRt.offsetMax = new Vector2(-12, -6);

                    var label = UISystem.AddTMP(labelRt.gameObject, _sys.ResolveFont(), _overrideTextSize ?? UConfig.ChoiceFontSize, _overrideTextColor ?? UConfig.TextColor, _overrideTextAlign ?? TextAlignmentOptions.Left);
                    label.text = options[i].Label;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.overflowMode = TextOverflowModes.Overflow;
                    label.lineSpacing = UConfig.ChoiceLineSpacing;

                    _rows.Add(new ChoiceRow(rowGo, bg, iconImg, label, options[i].Label));
                }

                _selected = 0;

                UpdateChoiceVisuals();
            }

            private void UpdateChoiceVisuals()
            {
                for (int i = 0; i < _rows.Count; i++)
                    _rows[i].SetSelected(i == _selected);
            }


            private void EnsureDesc()
            {
                if (_desc != null) return;

                var font = _sys.ResolveFont();

                var descGo = UISystem.CreateRect("Description", _go.transform);
                descGo.anchorMin = new Vector2(0, 0);
                descGo.anchorMax = new Vector2(1, 0);
                descGo.pivot = new Vector2(0.5f, 0);
                descGo.offsetMin = new Vector2(18, 12);
                descGo.offsetMax = new Vector2(-18, 72);
                descGo.sizeDelta = new Vector2(0, 56);

                var bg = descGo.gameObject.AddComponent<Image>();
                bg.color = new Color(1, 1, 1, 0.06f);

                var textRt = UISystem.CreateRect("DescText", descGo);
                textRt.anchorMin = new Vector2(0, 0);
                textRt.anchorMax = new Vector2(1, 1);
                textRt.offsetMin = new Vector2(10, 6);
                textRt.offsetMax = new Vector2(-10, -6);

                _desc = UISystem.AddTMP(textRt.gameObject, font, UConfig.DescFontSize , new Color(1,1,1,0.85f), TextAlignmentOptions.Left);
            }

            private void ClearDesc()
            {
                if (_desc != null)
                {
                    UnityEngine.Object.Destroy(_desc.transform.parent.gameObject); // Description rect
                    _desc = null;
                }
                _descFn = null;
            }

            private void UpdateDesc()
            {
                if (_desc == null || _descFn == null || _rows.Count == 0) return;
                var label = _rows[Mathf.Clamp(_selected, 0, _rows.Count - 1)].Label;
                string d = null;
                try { d = _descFn(label); } catch { d = null; }

                _desc.text = string.IsNullOrEmpty(d) ? "" : d;
            }

            private void EnsurePage()
            {
                if (_page != null) return;

                var font = _sys.ResolveFont();

                var pageGo = UISystem.CreateRect("Page", _go.transform);
                pageGo.anchorMin = new Vector2(1, 0);
                pageGo.anchorMax = new Vector2(1, 0);
                pageGo.pivot = new Vector2(1, 0);
                pageGo.anchoredPosition = new Vector2(-12, 10);
                pageGo.sizeDelta = new Vector2(120, 30);

                _page = UISystem.AddTMP(pageGo.gameObject, font, UConfig.PageFontSize , new Color(1,1,1,0.7f), TextAlignmentOptions.Right);
            }

            private void ClearPage()
            {
                if (_page != null) { UnityEngine.Object.Destroy(_page.gameObject); _page = null; }
            }

            private static bool InputConfirm()
            {
                if (I.GetMouseButtonDown(0)) return true;

                foreach (var k in UConfig.ConfirmKeys)
                    if (I.GetKeyDown(k)) return true;

                if (!string.IsNullOrEmpty(UConfig.ConfirmButtonName) && I.GetButtonDown(UConfig.ConfirmButtonName))
                    return true;

                return false;
            }

            private static bool InputCancel()
            {
                if (I.GetMouseButtonDown(1)) return true;

                foreach (var k in UConfig.CancelKeys)
                    if (I.GetKeyDown(k)) return true;

                if (!string.IsNullOrEmpty(UConfig.CancelButtonName) && I.GetButtonDown(UConfig.CancelButtonName))
                    return true;

                return false;
            }

            private static bool InputUp()
            {
                foreach (var k in UConfig.UpKeys)
                    if (I.GetKeyDown(k)) return true;
                return false;
            }

            private static bool InputDown()
            {
                foreach (var k in UConfig.DownKeys)
                    if (I.GetKeyDown(k)) return true;
                return false;
            }

            private readonly struct ChoiceRow
            {
                public readonly GameObject Go;
                public readonly Image Bg;
                public readonly Image Icon;
                public readonly TMP_Text LabelText;
                public readonly string Label;

                public ChoiceRow(GameObject go, Image bg, Image icon, TMP_Text labelText, string label)
                {
                    Go = go;
                    Bg = bg;
                    Icon = icon;
                    LabelText = labelText;
                    Label = label;
                }

                public void SetSelected(bool selected)
                {
                    if (Bg != null) Bg.color = selected ? UConfig.SelectedColor : new Color(1,1,1,0f);
                    if (LabelText != null) LabelText.color = selected ? UConfig.SelectedTextColor : UConfig.TextColor;
}

                public void Destroy()
                {
                    if (Go != null) UnityEngine.Object.Destroy(Go);
                }
            }
        }

        private sealed class PanelPool
        {
            private readonly UISystem _sys;
            private readonly Dictionary<PopupKind, Stack<UIPanel>> _p = new Dictionary<PopupKind, Stack<UIPanel>>();

            public PanelPool(UISystem sys)
            {
                _sys = sys;
                _p[PopupKind.Banner] = new Stack<UIPanel>();
                _p[PopupKind.Dialogue] = new Stack<UIPanel>();
                _p[PopupKind.Choice] = new Stack<UIPanel>();
            }

            public UIPanel GetOrCreate(PopupKind kind)
            {
                var st = _p[kind];
                if (st.Count > 0) return st.Pop();
                return new UIPanel(_sys);
            }

            public void Return(PopupKind kind, UIPanel panel)
            {
                _p[kind].Push(panel);
            }
        }

        private sealed class PanelStack
        {
            private readonly Stack<UIPanel> _s = new Stack<UIPanel>();
            public void Push(UIPanel p) => _s.Push(p);
            public UIPanel Pop() => _s.Pop();
        }

        public interface IDisplayHandle : IDisposable
        {
            void Show();
            void Hide();
            void SetText(Func<string> text);
        }

        private sealed class DisplaySystem
        {
            private readonly UISystem _sys;

            public DisplaySystem(UISystem sys) { _sys = sys; }

            public IDisplayHandle Create(Func<string> text, Placements placement, Sizes size, Color bg, Color fg, int paddingPx)
                => new DisplayHandle(_sys, text, placement, size, bg, fg, paddingPx);

            private sealed class DisplayHandle : IDisplayHandle
            {
                private readonly UISystem _sys;
                private readonly GameObject _go;
                private readonly RectTransform _rt;
                private readonly Image _bg;
                private readonly TMP_Text _text;
                private Func<string> _source;
                private bool _visible = true;
                private float _nextTick;

                public DisplayHandle(UISystem sys, Func<string> source, Placements placement, Sizes size, Color bg, Color fg, int paddingPx)
                {
                    _source = source ?? (() => "");
                    _sys = sys;

                    _go = new GameObject("U_Display", typeof(RectTransform));
                    _go.transform.SetParent(sys.Root.transform, false);
                    // Keep HUD displays behind modal Pop panels.
                    _go.transform.SetAsFirstSibling();
                    _rt = _go.GetComponent<RectTransform>();

                    _bg = _go.AddComponent<Image>();
                    _bg.color = bg;

                    var textRt = UISystem.CreateRect("Text", _go.transform);
                    textRt.anchorMin = new Vector2(0, 0);
                    textRt.anchorMax = new Vector2(1, 1);
                    textRt.offsetMin = new Vector2(paddingPx, paddingPx);
                    textRt.offsetMax = new Vector2(-paddingPx, -paddingPx);

                    _text = UISystem.AddTMP(textRt.gameObject, sys.ResolveFont(), UConfig.BodyFontSize, fg, U.AlignmentForPlacement(placement, fallback: TextAlignmentOptions.Left));
                    _text.richText = true;
                    _text.textWrappingMode = TextWrappingModes.Normal;
                    _text.lineSpacing = UConfig.DisplayLineSpacing;

                    ApplyPlacement(placement, size);
                    Tick(true);

                    URunner.Ensure();
                    URunner.OnUpdate += Update;
                }

                public void SetText(Func<string> text)
                {
                    _source = text ?? (() => "");
                    Tick(true);
                }

                public void Show()
                {
                    _visible = true;
                    _go.SetActive(true);
                    Tick(true);
                }

                public void Hide()
                {
                    _visible = false;
                    _go.SetActive(false);
                }

                public void Dispose()
                {
                    URunner.OnUpdate -= Update;
                    if (_go != null) UnityEngine.Object.Destroy(_go);
                }

                private void Update()
                {
                    if (!_visible) return;
                    if (Time.unscaledTime < _nextTick) return;
                    _nextTick = Time.unscaledTime + 0.1f;
                    Tick(false);
                }

                                private void Tick(bool force)
                {
                    string raw;
                    try { raw = _source(); }
                    catch { raw = "ERR"; }

                    var s = U.PreprocessMarkup(raw, allowPageBreaks: false);

                    if (force || _text.text != s)
                        _text.text = s;
                }


                private void ApplyPlacement(Placements placement, Sizes size)
                {
                    float widthPct = size == Sizes.FullWidth ? 1f : 0.35f;

                    float w = _sys.CanvasSize.x * Mathf.Clamp01(widthPct);

                    // Auto-size height to fit current text (helps multi-line displays not get clipped).
                    // Clamp to a sane max so HUDs don't become giant novels.
                    float innerW = Mathf.Max(0f, w - (UConfig.DisplayPaddingPx * 2f));
                    string preview = _source != null ? (_source() ?? "") : "";
                    var pref = _text.GetPreferredValues(preview, innerW, 0);
                    float h = Mathf.Clamp(pref.y + (UConfig.DisplayPaddingPx * 2f), 24f, _sys.CanvasSize.y * 0.3f);

                    _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                    _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);

                    Vector2 anchor = placement switch
                    {
                        Placements.TopLeft => new Vector2(0, 1),
                        Placements.TopCenter => new Vector2(0.5f, 1),
                        Placements.TopRight => new Vector2(1, 1),

                        Placements.MiddleLeft => new Vector2(0, 0.5f),
                        Placements.MiddleCenter => new Vector2(0.5f, 0.5f),
                        Placements.MiddleRight => new Vector2(1, 0.5f),

                        Placements.BottomLeft => new Vector2(0, 0),
                        Placements.BottomCenter => new Vector2(0.5f, 0),
                        Placements.BottomRight => new Vector2(1, 0),
                        _ => new Vector2(0, 1)
                    };


                    _rt.anchorMin = anchor;
                    _rt.anchorMax = anchor;
                    _rt.pivot = anchor;

                    float pad = UConfig.DisplayMarginPx;
                    Vector2 offset = anchor switch
                    {
                        var a when a == new Vector2(0,1) => new Vector2(pad, -pad),
                        var a when a == new Vector2(0.5f,1) => new Vector2(0, -pad),
                        var a when a == new Vector2(1,1) => new Vector2(-pad, -pad),

                        var a when a == new Vector2(0,0.5f) => new Vector2(pad, 0),
                        var a when a == new Vector2(0.5f,0.5f) => Vector2.zero,
                        var a when a == new Vector2(1,0.5f) => new Vector2(-pad, 0),

                        var a when a == new Vector2(0,0) => new Vector2(pad, pad),
                        var a when a == new Vector2(0.5f,0) => new Vector2(0, pad),
                        var a when a == new Vector2(1,0) => new Vector2(-pad, pad),
                        _ => Vector2.zero
                    };

                    _rt.anchoredPosition = offset;
                }
            }
        }

        private sealed class HistoryRing
        {
            private readonly int _max;
            private readonly List<string> _items = new List<string>();

            public HistoryRing(int max) { _max = Mathf.Max(1, max); }

            public IReadOnlyList<string> Items => _items;

            public void Add(string s)
            {
                if (string.IsNullOrEmpty(s)) return;
                _items.Add(s);
                if (_items.Count > _max) _items.RemoveAt(0);
            }
        }

        private sealed class URunner : MonoBehaviour
        {
            public static event Action OnUpdate;
            private static URunner _inst;

            public static void Ensure()
            {
                if (_inst != null) return;
                var go = new GameObject("U_Runner");
                DontDestroyOnLoad(go);
                _inst = go.AddComponent<URunner>();
            }

            private void Update() => OnUpdate?.Invoke();
        }


    }
}