using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace DarkMagic
{
    public static partial class U
    {
        /// <summary>
        /// Fade / wipe / dissolve the screen OUT (to TransitionColor).
        /// After this completes, the screen is fully covered.
        /// </summary>
        public static async Awaitable TransitionOut(
            TransitionStyle style = TransitionStyle.Fade,
            float? duration = null,
            Color? color = null,
            CancellationToken cancellationToken = default)
        {
            EnsureSystem();
            await _transition.Ensure();

            await _transition.Play(style, outwards: true, duration ?? UConfig.TransitionDuration, color ?? UConfig.TransitionColor, cancellationToken);
        }

        /// <summary>
        /// Fade / wipe / dissolve the screen IN (from TransitionColor back to gameplay).
        /// After this completes, the overlay is hidden.
        /// </summary>
        public static async Awaitable TransitionIn(
            TransitionStyle style = TransitionStyle.Fade,
            float? duration = null,
            Color? color = null,
            CancellationToken cancellationToken = default)
        {
            EnsureSystem();
            await _transition.Ensure();

            await _transition.Play(style, outwards: false, duration ?? UConfig.TransitionDuration, color ?? UConfig.TransitionColor, cancellationToken);
        }

        /// <summary>
        /// Convenience: transition out, run an async action (eg scene load), then transition in.
        /// </summary>
        public static async Awaitable Transition(
            Func<Awaitable> during,
            TransitionStyle style = TransitionStyle.Fade,
            float? outDuration = null,
            float? inDuration = null,
            Color? color = null,
            CancellationToken cancellationToken = default)
        {
            await TransitionOut(style, outDuration, color, cancellationToken);
            if (during != null) await during();
            await TransitionIn(style, inDuration, color, cancellationToken);
        }

        private static readonly TransitionSystem _transition = new();

        private sealed class TransitionSystem
        {
            private GameObject _go;
            private RectTransform _rt;
            private CanvasGroup _cg;
            private Image _img;
            private Material _mat;
            private Shader _shader;

            public async Awaitable Ensure()
            {
                if (_go != null) return;

                // Create under the UI root so it shares the same canvas and is always above Display.
                _go = new GameObject("U_Transition", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                _rt = _go.GetComponent<RectTransform>();
                _cg = _go.GetComponent<CanvasGroup>();
                _img = _go.GetComponent<Image>();

                _go.transform.SetParent(_sys.Root.transform, false);
                _go.transform.SetAsLastSibling();

                _rt.anchorMin = Vector2.zero;
                _rt.anchorMax = Vector2.one;
                _rt.pivot = new Vector2(0.5f, 0.5f);
                _rt.offsetMin = Vector2.zero;
                _rt.offsetMax = Vector2.zero;

                _cg.alpha = 0f;
                _cg.interactable = false;
                _cg.blocksRaycasts = true;

                _img.raycastTarget = true;
                _img.color = UConfig.TransitionColor;

                // Shader is optional; if not found we can still do Fade.
                _shader = Shader.Find("UI/DarkMagicTransition");

                if (_shader != null)
                {
                    _mat = new Material(_shader) { hideFlags = HideFlags.DontSave };
                    _img.material = _mat;
                }

                _go.SetActive(false);

                // Make sure a frame passes so Unity can initialize the UI objects cleanly.
                await Awaitable.NextFrameAsync();
            }

            public async Awaitable Play(TransitionStyle style, bool outwards, float duration, Color color, CancellationToken ct)
            {
                duration = Mathf.Max(0.001f, duration);

                _go.SetActive(true);
                _go.transform.SetAsLastSibling();
                _img.color = color;

                // Fade uses CanvasGroup alpha (cheapest, most robust).
                if (style == TransitionStyle.Fade || _shader == null || _mat == null)
                {
                    float from = outwards ? 0f : 1f;
                    float to = outwards ? 1f : 0f;

                    _cg.alpha = from;

                    float t = 0f;
                    while (t < duration)
                    {
                        if (ct.IsCancellationRequested) break;

                        t += Time.unscaledDeltaTime;
                        float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                        _cg.alpha = a;

                        await Awaitable.NextFrameAsync(ct);
                    }

                    _cg.alpha = to;

                    if (!outwards)
                        _go.SetActive(false);

                    return;
                }

                // Wipe/dissolve uses shader cutoff.
                _cg.alpha = 1f;

                float mode = style == TransitionStyle.DiagonalWipe ? 0f : 1f;
                _mat.SetFloat("_Mode", mode);
                _mat.SetFloat("_Softness", Mathf.Clamp(UConfig.TransitionSoftness, 0f, 0.5f));
                _mat.SetFloat("_PixelScale", Mathf.Max(1f, UConfig.TransitionPixelScale));
                _mat.SetColor("_Color", color);

                float fromC = outwards ? 0f : 1f;
                float toC = outwards ? 1f : 0f;

                _mat.SetFloat("_Cutoff", fromC);

                float tt = 0f;
                while (tt < duration)
                {
                    if (ct.IsCancellationRequested) break;

                    tt += Time.unscaledDeltaTime;
                    float c = Mathf.Lerp(fromC, toC, Mathf.Clamp01(tt / duration));
                    _mat.SetFloat("_Cutoff", c);

                    await Awaitable.NextFrameAsync(ct);
                }

                _mat.SetFloat("_Cutoff", toC);

                if (!outwards)
                    _go.SetActive(false);
            }
        }
    }
}
