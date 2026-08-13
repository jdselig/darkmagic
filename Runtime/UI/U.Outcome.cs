using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DarkMagic
{
    public static partial class U
    {
        /// <summary>
        /// Pops a floating outcome number near a world-space target.
        /// It may be awaited or used as fire-and-forget.
        /// </summary>
        public static async Awaitable PopOutcome(
            Transform target,
            int amount,
            Color? color = null,
            int? textSize = null,
            Camera camera = null
        ) => await PopOutcome(target, amount.ToString(), color, textSize, camera);

        /// <summary>
        /// Pops floating text near a world-space target.
        /// It may be awaited or used as fire-and-forget.
        /// </summary>
        public static async Awaitable PopOutcome(
            Transform target,
            string text,
            Color? color = null,
            int? textSize = null,
            Camera camera = null
        )
        {
            EnsureSystem();
            if (target == null) return;

            var cam = camera != null ? camera : Camera.main;
            if (cam == null) cam = Camera.current;
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
#else
            if (cam == null) cam = Object.FindObjectOfType<Camera>();
#endif
            if (cam == null) return;

            var bubble = _outcomePool.GetOrCreate(_sys);
            bubble.Show(
                text ?? "",
                _sys.ResolveFont(),
                textSize ?? UConfig.OutcomeFontSize,
                color ?? UConfig.OutcomeColor
            );

            var worldPosition = ResolveOutcomeWorldPosition(target);
            bubble.SetScreenPosition(cam.WorldToScreenPoint(worldPosition), _sys.Canvas);
            await bubble.AnimateAndRelease(
                UConfig.OutcomeDuration,
                UConfig.OutcomeRisePx,
                UConfig.OutcomeBouncePx,
                UConfig.OutcomeScalePop,
                _outcomePool
            );
        }

        private static Vector3 ResolveOutcomeWorldPosition(Transform target)
        {
            var anchor = target.Find(UConfig.OutcomeAnchorChildName);
            if (anchor != null) return anchor.position;

            var collider2D = target.GetComponentInChildren<Collider2D>();
            if (collider2D != null)
            {
                var bounds = collider2D.bounds;
                return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            }

            var collider = target.GetComponentInChildren<Collider>();
            if (collider != null)
            {
                var bounds = collider.bounds;
                return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            }

            return target.position + Vector3.up * UConfig.OutcomeWorldOffsetY;
        }

        private sealed class OutcomePool
        {
            private readonly Stack<OutcomeBubble> _pool = new();

            public OutcomeBubble GetOrCreate(UISystem system)
            {
                while (_pool.Count > 0)
                {
                    var bubble = _pool.Pop();
                    if (bubble != null) return bubble;
                }

                return new OutcomeBubble(system);
            }

            public void Return(OutcomeBubble bubble)
            {
                if (bubble != null) _pool.Push(bubble);
            }
        }

        private sealed class OutcomeBubble
        {
            private readonly GameObject _gameObject;
            private readonly RectTransform _rectTransform;
            private readonly CanvasGroup _canvasGroup;
            private readonly TMP_Text _text;
            private readonly RectTransform _rootRect;

            public OutcomeBubble(UISystem system)
            {
                _gameObject = new GameObject(
                    "U_Outcome",
                    typeof(RectTransform),
                    typeof(CanvasGroup)
                );
                _rectTransform = _gameObject.GetComponent<RectTransform>();
                _canvasGroup = _gameObject.GetComponent<CanvasGroup>();
                _rectTransform.SetParent(system.Root.transform, false);
                _rootRect = system.Root.GetComponent<RectTransform>();
                _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _rectTransform.sizeDelta = new Vector2(10, 10);
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;

                var textObject = new GameObject("Text", typeof(RectTransform));
                var textRect = (RectTransform)textObject.transform;
                textRect.SetParent(_gameObject.transform, false);
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = Vector2.zero;
                _text = UISystem.AddTMP(
                    textObject,
                    system.ResolveFont(),
                    UConfig.OutcomeFontSize,
                    UConfig.OutcomeColor,
                    TextAlignmentOptions.Center
                );
            }

            public void Show(string text, TMP_FontAsset font, int fontSize, Color color)
            {
                _gameObject.SetActive(true);
                _canvasGroup.alpha = 1f;
                if (font != null) _text.font = font;
                _text.fontSize = fontSize;
                _text.color = color;
                _text.text = text;
                _text.ForceMeshUpdate();

                var preferred = _text.GetPreferredValues(text);
                _text.rectTransform.sizeDelta = new Vector2(
                    Mathf.Min(preferred.x + 8f, 800f),
                    Mathf.Min(preferred.y + 8f, 120f)
                );
            }

            public void SetScreenPosition(Vector3 screenPosition, Canvas canvas)
            {
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootRect,
                    screenPosition,
                    camera,
                    out var local
                )) return;

                var padding = UConfig.OutcomeCanvasPaddingPx;
                var half = _rootRect.rect.size * 0.5f;
                local.x = Mathf.Clamp(local.x, -half.x + padding, half.x - padding);
                local.y = Mathf.Clamp(local.y, -half.y + padding, half.y - padding);
                local += new Vector2(UConfig.OutcomeOffsetX, UConfig.OutcomeOffsetY);
                _rectTransform.anchoredPosition = local;
            }

            public async Awaitable AnimateAndRelease(
                float duration,
                float risePixels,
                float bouncePixels,
                float popScale,
                OutcomePool pool
            )
            {
                var elapsed = 0f;
                var start = _rectTransform.anchoredPosition;
                var baseScale = Vector3.one;
                _rectTransform.localScale = baseScale * popScale;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var progress = Mathf.Clamp01(elapsed / duration);
                    var eased = 1f - Mathf.Pow(1f - progress, 3f);
                    var bounce = Mathf.Sin(progress * Mathf.PI * 2f)
                        * bouncePixels
                        * (1f - progress);
                    _rectTransform.anchoredPosition = start
                        + new Vector2(0f, eased * risePixels + bounce);
                    _rectTransform.localScale = Vector3.Lerp(
                        baseScale * popScale,
                        baseScale,
                        progress
                    );
                    if (progress > 0.65f)
                        _canvasGroup.alpha = Mathf.InverseLerp(1f, 0.65f, progress);

                    await Awaitable.NextFrameAsync();
                }

                _gameObject.SetActive(false);
                _canvasGroup.alpha = 0f;
                _rectTransform.anchoredPosition = start;
                _rectTransform.localScale = baseScale;
                pool.Return(this);
            }
        }
    }
}
