using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkMagic
{
    public static partial class U
    {
        public static class Target
        {
            /// <summary>
            /// Final-Fantasy-style target selection (cycle through a list, show an arrow by the current target).
            /// No raycasts. Great for turn-based RPG prototyping.
            /// </summary>
            public static async Awaitable<Result<T>> Single<T>(
                IReadOnlyList<T> targets,
                System.Func<T, Transform> getTransform,
                Camera camera = null,
                CancellationToken cancellationToken = default)
            {
                EnsureSystem();

                if (targets == null || targets.Count == 0)
                    return Result<T>.Canceled();

                camera ??= Camera.main;

                // Arrow UI
                var arrow = CreateArrow();
                arrow.gameObject.SetActive(true);

                int index = 0;
                UpdateArrow();

                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        UnityEngine.Object.Destroy(arrow.gameObject);
                        return Result<T>.Canceled();
                    }

                    if (InputLeft())
                    {
                        index = (index - 1 + targets.Count) % targets.Count;
                        UpdateArrow();
                    }
                    if (InputRight())
                    {
                        index = (index + 1) % targets.Count;
                        UpdateArrow();
                    }

                    if (UIPanel_InputConfirm())
                    {
                        var chosen = targets[index];
                        UnityEngine.Object.Destroy(arrow.gameObject);
                        return Result<T>.Ok(chosen);
                    }

                    if (UIPanel_InputCancel())
                    {
                        UnityEngine.Object.Destroy(arrow.gameObject);
                        return Result<T>.Canceled();
                    }

                    await Awaitable.NextFrameAsync(cancellationToken);
                }

                void UpdateArrow()
                {
                    var t = getTransform(targets[index]);
                    if (t == null || camera == null)
                        return;

                    var screen = camera.WorldToScreenPoint(t.position);
                    // place arrow slightly left of the target's screen position
                    arrow.position = screen + new Vector3(-30, 20, 0);
                }
            }



            /// <summary>
            /// Select either a single target OR "All" (enemy party).
            /// Cycle targets with Left/Right (or A/D). Includes an "ALL" entry first.
            /// </summary>
            public static async Awaitable<Result<IReadOnlyList<T>>> Party<T>(
                IReadOnlyList<T> targets,
                System.Func<T, Transform> getTransform,
                Camera camera = null,
                CancellationToken cancellationToken = default,
                string allLabel = "ALL")
            {
                EnsureSystem();

                if (targets == null || targets.Count == 0)
                    return Result<IReadOnlyList<T>>.Canceled();

                camera ??= Camera.main;

                var arrow = CreateArrow();
                arrow.gameObject.SetActive(true);

                // index -1 means ALL, 0..n-1 = target
                int index = targets.Count > 1 ? -1 : 0;
                UpdateArrow();

                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        UnityEngine.Object.Destroy(arrow.gameObject);
                        return Result<IReadOnlyList<T>>.Canceled();
                    }

                    if (InputLeft() || InputRight())
                    {
                        if (targets.Count > 1)
                        {
                            // rotate through ALL + each target
                            index++;
                            if (index >= targets.Count) index = -1;
                        }
                        else index = 0;

                        UpdateArrow();
                    }

                    if (UIPanel_InputConfirm())
                    {
                        UnityEngine.Object.Destroy(arrow.gameObject);

                        if (index == -1)
                            return Result<IReadOnlyList<T>>.Ok(targets);

                        return Result<IReadOnlyList<T>>.Ok(new List<T> { targets[index] });
                    }

                    if (UIPanel_InputCancel())
                    {
                        UnityEngine.Object.Destroy(arrow.gameObject);
                        return Result<IReadOnlyList<T>>.Canceled();
                    }

                    await Awaitable.NextFrameAsync(cancellationToken);
                }

                void UpdateArrow()
                {
                    if (camera == null) return;

                    var tmp = arrow.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = (index == -1) ? allLabel : "▶";

                    if (index == -1)
                    {
                        // Put ALL indicator near the top center of screen.
                        arrow.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.78f, 0);
                        return;
                    }

                    var tr = getTransform(targets[index]);
                    if (tr == null) return;

                    var screen = camera.WorldToScreenPoint(tr.position);
                    arrow.position = screen + new Vector3(-30, 20, 0);
                }
            }

            private static RectTransform CreateArrow()
            {
                var rt = UISystem.CreateRect("U_TargetArrow", _sys.Root.transform);
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(40, 40);

                var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
                if (UConfig.Font != null) t.font = UConfig.Font;
                t.text = "▶";
                t.fontSize = 36;
                t.color = UConfig.TextColor;
                t.alignment = TextAlignmentOptions.Center;

                return rt;
            }

            private static bool InputLeft()
            {
                foreach (var k in UConfig.LeftKeys)
                    if (I.GetKeyDown(k)) return true;
                return false;
            }

            private static bool InputRight()
            {
                foreach (var k in UConfig.RightKeys)
                    if (I.GetKeyDown(k)) return true;
                return false;
            }

            // reuse confirm/cancel rules (copied from UIPanel)
            private static bool UIPanel_InputConfirm()
            {
                if (I.GetMouseButtonDown(0)) return true;
                foreach (var k in UConfig.ConfirmKeys)
                    if (I.GetKeyDown(k)) return true;
                if (!string.IsNullOrEmpty(UConfig.ConfirmButtonName) && I.GetButtonDown(UConfig.ConfirmButtonName))
                    return true;
                return false;
            }

            private static bool UIPanel_InputCancel()
            {
                if (I.GetMouseButtonDown(1)) return true;
                foreach (var k in UConfig.CancelKeys)
                    if (I.GetKeyDown(k)) return true;
                if (!string.IsNullOrEmpty(UConfig.CancelButtonName) && I.GetButtonDown(UConfig.CancelButtonName))
                    return true;
                return false;
            }
        }
    }
}
