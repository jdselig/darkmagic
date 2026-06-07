using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkMagic
{
    public static partial class U
    {
        public enum TargetMode
        {
            Single,
            All,   // always return all
            UpTo,  // choose up to N, confirm early (DONE)
            Exact  // choose exactly N
        }

        public sealed class TargetRules
        {
            // Input + UX
            public bool AllowMouseRaycast = false;
            public LayerMask RaycastMask = ~0;
            public float RaycastMaxDistance = 500f;

            public string DoneLabel = "DONE";
            public string AllLabel = "ALL";

            // Behavior
            public bool AllowAll = false;     // include ALL entry (single-or-all style)
            public bool AllowSelf = true;     // allow selecting the caller if present in list (mostly informational)
            public bool ExcludeNulls = true;

            // Visuals
            public Vector2 MarkerScreenOffset = new Vector2(-30, 20);
            public int MarkerFontSize = 36;

            public static TargetRules Default => new TargetRules();
        }

        public static class Target
        {
            // ----------------------------
            // Single
            // ----------------------------

            /// <summary>
            /// Single target selection (List overload to avoid generic ambiguity when T=Transform).
            /// </summary>
            public static async Awaitable<Result<Transform>> Select(
                List<Transform> targets,
                Func<Transform, bool> filter = null,
                TargetRules rules = null,
                Camera camera = null,
                CancellationToken cancellationToken = default)
                => await Select((IReadOnlyList<Transform>)targets, filter, rules, camera, cancellationToken);

            /// <summary>
            /// Single target selection from the direct children of a group transform.
            /// </summary>
            public static async Awaitable<Result<Transform>> Select(
                Transform group,
                Func<Transform, bool> filter = null,
                TargetRules rules = null,
                Camera camera = null,
                CancellationToken cancellationToken = default)
            {
                if (group == null) return Result<Transform>.Canceled();
                var list = new List<Transform>(group.childCount);
                for (int i = 0; i < group.childCount; i++)
                    list.Add(group.GetChild(i));
                return await Select(list, filter, rules, camera, cancellationToken);
            }

            public static async Awaitable<Result<Transform>> Select(
                IReadOnlyList<Transform> targets,
                Func<Transform, bool> filter = null,
                TargetRules rules = null,
                Camera camera = null,
                CancellationToken cancellationToken = default)
            {
                var res = await SelectMany(
                    targets,
                    mode: TargetMode.Single,
                    count: 1,
                    filter: filter,
                    rules: rules,
                    camera: camera,
                    cancellationToken: cancellationToken);

                if (res.Cancelled || res.Value == null || res.Value.Count == 0)
                    return Result<Transform>.Canceled();

                return Result<Transform>.Ok(res.Value[0]);
            }

            public static async Awaitable<Result<T>> Select<T>(
                IReadOnlyList<T> targets,
                Func<T, Transform> getTransform = null,
                Func<T, bool> filter = null,
                TargetRules rules = null,
                Camera camera = null,
                CancellationToken cancellationToken = default)
            {
                if (targets == null) return Result<T>.Canceled();

                getTransform ??= InferGetTransform<T>();
                if (getTransform == null) return Result<T>.Canceled();

                // map indices
                var mapped = new List<(T item, Transform tr)>(targets.Count);
                for (int i = 0; i < targets.Count; i++)
                {
                    var it = targets[i];
                    if (filter != null && !filter(it)) continue;

                    var tr = getTransform(it);
                    if (tr == null) continue;
                    mapped.Add((it, tr));
                }

                var trList = mapped.Select(m => m.tr).ToList();
                var picked = await Select(trList, filter: null, rules: rules, camera: camera, cancellationToken: cancellationToken);
                if (picked.Cancelled) return Result<T>.Canceled();

                // map back by transform reference
                for (int i = 0; i < mapped.Count; i++)
                    if (mapped[i].tr == picked.Value)
                        return Result<T>.Ok(mapped[i].item);

                return Result<T>.Canceled();
            }

            // ----------------------------
            // Many
            // ----------------------------

            public static async Awaitable<Result<IReadOnlyList<Transform>>> SelectMany(
                IReadOnlyList<Transform> targets,
                bool all = false,
                Func<Transform, bool> filter = null,
                TargetRules rules = null,
                Camera camera = null,
                CancellationToken cancellationToken = default)
            {
                rules ??= TargetRules.Default;
                rules.AllowAll = all;
                return await SelectMany(targets, TargetMode.UpTo, count: int.MaxValue, filter: filter, rules: rules, camera: camera, cancellationToken: cancellationToken);
            }

            public static async Awaitable<Result<IReadOnlyList<Transform>>> SelectMany(
                IReadOnlyList<Transform> targets,
                int max,
                Func<Transform, bool> filter = null,
                TargetRules rules = null,
                Camera camera = null,
                CancellationToken cancellationToken = default)
            {
                return await SelectMany(targets, TargetMode.UpTo, count: Mathf.Max(1, max), filter: filter, rules: rules, camera: camera, cancellationToken: cancellationToken);
            }

            public static async Awaitable<Result<IReadOnlyList<Transform>>> SelectMany(
                IReadOnlyList<Transform> targets,
                TargetMode mode,
                int count = 1,
                Func<Transform, bool> filter = null,
                TargetRules rules = null,
                Camera camera = null,
                CancellationToken cancellationToken = default)
            {
                EnsureSystem();
                rules ??= TargetRules.Default;

                if (targets == null || targets.Count == 0)
                    return Result<IReadOnlyList<Transform>>.Canceled();

                camera ??= Camera.main;
                if (camera == null) camera = Camera.current;
                if (camera == null)
                {
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
                    camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
#else
                    camera = UnityEngine.Object.FindObjectOfType<Camera>();
#endif
                }
                if (camera == null)
                    return Result<IReadOnlyList<Transform>>.Canceled();

                // Filter list (global + per-call)
                var list = new List<Transform>(targets.Count);
                for (int i = 0; i < targets.Count; i++)
                {
                    var t = targets[i];
                    if (rules.ExcludeNulls && t == null) continue;

                    if (UConfig.TargetIsTargetable != null && !UConfig.TargetIsTargetable(t))
                        continue;

                    if (filter != null && !filter(t))
                        continue;

                    list.Add(t);
                }

                if (list.Count == 0)
                    return Result<IReadOnlyList<Transform>>.Canceled();

                // Special modes
                if (mode == TargetMode.All)
                    return Result<IReadOnlyList<Transform>>.Ok(list);

                // Arrow UI marker
                var marker = CreateDefaultMarker(rules.MarkerFontSize);
                marker.gameObject.SetActive(true);

                // Pseudo entries:
                // ALL: index = -2
                // DONE: index = list.Count
                int index = 0;
                bool showAll = rules.AllowAll && list.Count > 1;
                bool showDone = (mode == TargetMode.UpTo || mode == TargetMode.Exact) && count > 1;

                if (showAll) index = -2;

                // Selected set
                var selected = new List<Transform>(Mathf.Min(count, list.Count));

                UpdateMarker();

                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        UnityEngine.Object.Destroy(marker.gameObject);
                        return Result<IReadOnlyList<Transform>>.Canceled();
                    }

                    // optional mouse hover/raycast
                    if (rules.AllowMouseRaycast)
                        TryUpdateIndexFromMouseRaycast();

                    if (InputLeft())
                    {
                        index = PrevIndex(index);
                        UpdateMarker();
                    }
                    if (InputRight())
                    {
                        index = NextIndex(index);
                        UpdateMarker();
                    }

                    if (InputConfirm())
                    {
                        if (showAll && index == -2)
                        {
                            UnityEngine.Object.Destroy(marker.gameObject);
                            return Result<IReadOnlyList<Transform>>.Ok(list);
                        }

                        if (showDone && index == list.Count)
                        {
                            // Done
                            if (mode == TargetMode.Exact && selected.Count != count)
                            {
                                // not enough selected; ignore
                            }
                            else
                            {
                                UnityEngine.Object.Destroy(marker.gameObject);
                                return Result<IReadOnlyList<Transform>>.Ok(selected.Count == 0 ? list.Take(1).ToList() : selected);
                            }
                        }
                        else
                        {
                            var t = list[Mathf.Clamp(index, 0, list.Count - 1)];

                            if (mode == TargetMode.Single || count == 1)
                            {
                                UnityEngine.Object.Destroy(marker.gameObject);
                                return Result<IReadOnlyList<Transform>>.Ok(new List<Transform> { t });
                            }

                            // toggle selection
                            if (selected.Contains(t))
                                selected.Remove(t);
                            else
                            {
                                if (mode == TargetMode.Exact)
                                {
                                    if (selected.Count < count) selected.Add(t);
                                    if (selected.Count >= count)
                                    {
                                        UnityEngine.Object.Destroy(marker.gameObject);
                                        return Result<IReadOnlyList<Transform>>.Ok(selected);
                                    }
                                }
                                else // UpTo
                                {
                                    if (selected.Count < count) selected.Add(t);
                                    // else ignore (max reached)
                                }
                            }
                        }
                    }

                    if (InputCancel())
                    {
                        // if selecting many and something is selected, undo last pick; else cancel
                        if (selected.Count > 0 && (mode == TargetMode.UpTo || mode == TargetMode.Exact) && count > 1)
                        {
                            selected.RemoveAt(selected.Count - 1);
                        }
                        else
                        {
                            UnityEngine.Object.Destroy(marker.gameObject);
                            return Result<IReadOnlyList<Transform>>.Canceled();
                        }
                    }

                    await Awaitable.NextFrameAsync(cancellationToken);
                }

                // -----------------
                // local helpers
                // -----------------

                int PrevIndex(int i)
                {
                    if (showDone)
                    {
                        // cycle among ALL, 0..n-1, DONE
                        if (showAll)
                        {
                            if (i == -2) return list.Count; // ALL -> DONE
                            if (i == 0) return -2;          // 0 -> ALL
                            if (i == list.Count) return list.Count - 1; // DONE -> last
                            return i - 1;
                        }
                        else
                        {
                            if (i == 0) return list.Count;  // 0 -> DONE
                            if (i == list.Count) return list.Count - 1;
                            return i - 1;
                        }
                    }
                    else
                    {
                        if (showAll)
                        {
                            if (i == -2) return list.Count - 1;
                            if (i == 0) return -2;
                            return i - 1;
                        }
                        else
                        {
                            return (i - 1 + list.Count) % list.Count;
                        }
                    }
                }

                int NextIndex(int i)
                {
                    if (showDone)
                    {
                        if (showAll)
                        {
                            if (i == -2) return 0;
                            if (i == list.Count) return -2;
                            if (i == list.Count - 1) return list.Count;
                            return i + 1;
                        }
                        else
                        {
                            if (i == list.Count) return 0;
                            if (i == list.Count - 1) return list.Count;
                            return i + 1;
                        }
                    }
                    else
                    {
                        if (showAll)
                        {
                            if (i == -2) return 0;
                            if (i == list.Count - 1) return -2;
                            return i + 1;
                        }
                        else
                        {
                            return (i + 1) % list.Count;
                        }
                    }
                }

                void UpdateMarker()
                {
                    if (camera == null) return;

                    var tmp = marker.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        if (showAll && index == -2) tmp.text = rules.AllLabel;
                        else if (showDone && index == list.Count) tmp.text = rules.DoneLabel;
                        else tmp.text = UConfig.TargetMarkerGlyph ?? ">";
                    }

                    if (showAll && index == -2)
                    {
                        marker.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.78f, 0);
                        return;
                    }

                    if (showDone && index == list.Count)
                    {
                        marker.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.15f, 0);
                        return;
                    }

                    var tr = list[Mathf.Clamp(index, 0, list.Count - 1)];
                    if (tr == null) return;

                    var worldPos = tr.position;
                    // Anchor priority: TargetAnchor -> OutcomeAnchor -> Collider top-center -> pivot + world offset
                    var a = tr.Find(UConfig.TargetAnchorChildName);
                    if (a == null && !string.IsNullOrEmpty(UConfig.OutcomeAnchorChildName)) a = tr.Find(UConfig.OutcomeAnchorChildName);
                    if (a != null) worldPos = a.position;
                    else
                    {
                        var c2d = tr.GetComponentInChildren<Collider2D>();
                        if (c2d != null)
                        {
                            var b = c2d.bounds;
                            worldPos = new Vector3(b.center.x, b.max.y, b.center.z);
                        }
                        else
                        {
                            var c = tr.GetComponentInChildren<Collider>();
                            if (c != null)
                            {
                                var b = c.bounds;
                                worldPos = new Vector3(b.center.x, b.max.y, b.center.z);
                            }
                            else
                            {
                                worldPos += Vector3.up * UConfig.TargetMarkerWorldOffsetY;
                            }
                        }
                    }

                    var screen = camera.WorldToScreenPoint(worldPos);
                    marker.position = screen + (Vector3)rules.MarkerScreenOffset + (Vector3)UConfig.TargetMarkerScreenOffset;
                }

                void TryUpdateIndexFromMouseRaycast()
                {
                    // If cursor over a target collider, make that the current target.
                    // 2D + 3D supported.
                    var mp = (Vector3)UnityEngine.Input.mousePosition;

                    // 2D raycast
                    var ray = camera.ScreenPointToRay(mp);
                    // 3D
                    if (Physics.Raycast(ray, out var hit3, rules.RaycastMaxDistance, rules.RaycastMask))
                    {
                        var h = hit3.transform;
                        if (h != null)
                        {
                            var idx = list.IndexOf(h);
                            if (idx >= 0)
                            {
                                index = idx;
                                UpdateMarker();
                                return;
                            }
                            // try parent
                            for (int p = 0; p < 4 && h.parent != null; p++)
                            {
                                h = h.parent;
                                idx = list.IndexOf(h);
                                if (idx >= 0)
                                {
                                    index = idx;
                                    UpdateMarker();
                                    return;
                                }
                            }
                        }
                    }

                    // 2D raycast
                    var hit2 = Physics2D.GetRayIntersection(ray, rules.RaycastMaxDistance, rules.RaycastMask);
                    if (hit2.collider != null)
                    {
                        var h = hit2.transform;
                        var idx = list.IndexOf(h);
                        if (idx >= 0)
                        {
                            index = idx;
                            UpdateMarker();
                            return;
                        }
                        // try parent
                        for (int p = 0; p < 4 && h.parent != null; p++)
                        {
                            h = h.parent;
                            idx = list.IndexOf(h);
                            if (idx >= 0)
                            {
                                index = idx;
                                UpdateMarker();
                                return;
                            }
                        }
                    }
                }
            }

            private static Func<T, Transform> InferGetTransform<T>()
            {
                // For Component types, default getTransform to .transform
                if (typeof(Component).IsAssignableFrom(typeof(T)))
                    return (T t) => (t as Component)?.transform;
                return null;
            }

            private static RectTransform CreateDefaultMarker(int fontSize)
            {
                // If provided, use a prefab override (must include a RectTransform).
                if (UConfig.TargetMarkerPrefab != null)
                {
                    var go = UnityEngine.Object.Instantiate(UConfig.TargetMarkerPrefab);
                    go.name = "U_TargetMarker";
                    var prt = go.GetComponent<RectTransform>();
                    if (prt == null) prt = go.AddComponent<RectTransform>();
                    prt.SetParent(_sys.Root.transform, false);
                    return prt;
                }

                var rt = UISystem.CreateRect("U_TargetMarker", _sys.Root.transform);
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(120, 50);

                // If provided, use a sprite override.
                if (UConfig.TargetMarkerSprite != null)
                {
                    var img = rt.gameObject.AddComponent<Image>();
                    img.sprite = UConfig.TargetMarkerSprite;
                img.color = UConfig.TargetMarkerColor;
                img.rectTransform.localScale = new Vector3(1f, UConfig.TargetMarkerScaleY, 1f);
                    img.preserveAspect = true;
                    img.color = UConfig.TextColor;
                    return rt;
                }

                // Default: simple TMP glyph (ASCII-safe).
                var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
                var font = UConfig.TargetMarkerFont != null ? UConfig.TargetMarkerFont : (_sys != null ? _sys.ResolveFont() : null);
                if (font == null && UConfig.Font != null) font = UConfig.Font;
                if (font != null) t.font = font;
                t.text = UConfig.TargetMarkerGlyph ?? ">";
                t.fontSize = fontSize;
                t.color = UConfig.TargetMarkerColor;
                t.alignment = TextAlignmentOptions.Center;
                t.rectTransform.localScale = new Vector3(1f, UConfig.TargetMarkerScaleY, 1f);

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
        }
    }
}