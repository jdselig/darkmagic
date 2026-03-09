using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkMagic
{
    public static partial class U
    {
        /// <summary>
        /// U.Flow: tiny helper for nested menus (push/pop) with automatic Back behavior.
        ///
        /// Two modes:
        /// A) Run(...) executes actions immediately (student-simple).
        /// B) Pick&lt;T&gt;(...) returns a decision payload (more advanced).
        /// </summary>
        public static class Flow
        {
            public sealed class Menu
            {
                public string Title;

                /// <summary>
                /// Optional description resolver for this menu. If provided, U shows a description panel under the list.
                /// </summary>
                public Func<string, string> Description = null;

                internal readonly List<Entry> Entries = new List<Entry>();

                public Menu(string title) { Title = title; }

                public Menu Add(string label, Func<Awaitable> action) => Add(new Option(label), action);
                public Menu Add(Option option, Func<Awaitable> action)
                {
                    Entries.Add(Entry.MakeAction(option, action));
                    return this;
                }

                public Menu AddSubmenu(string label, Menu submenu) => AddSubmenu(new Option(label), submenu);
                public Menu AddSubmenu(Option option, Menu submenu)
                {
                    Entries.Add(Entry.MakeSubmenu(option, submenu));
                    return this;
                }

                public Menu AddSubmenu(string label, Action<Menu> build) => AddSubmenu(new Option(label), build);
                public Menu AddSubmenu(Option option, Action<Menu> build)
                {
                    var m = new Menu(option.Label);
                    build?.Invoke(m);
                    return AddSubmenu(option, m);
                }

                /// <summary>
                /// Add a selectable decision leaf that returns a payload when using Pick&lt;T&gt;.
                /// </summary>
                public Menu AddSelect<T>(string label, T payload) => AddSelect(new Option(label), payload);
                public Menu AddSelect<T>(Option option, T payload)
                {
                    Entries.Add(Entry.MakeSelect(option, payload));
                    return this;
                }
            }

            internal readonly struct Entry
            {
                public readonly Option Option;
                public readonly Func<Awaitable> ActionFn;
                public readonly Menu Submenu;
                public readonly object Payload;

                private Entry(Option option, Func<Awaitable> actionFn, Menu submenu, object payload)
                {
                    Option = option;
                    ActionFn = actionFn;
                    Submenu = submenu;
                    Payload = payload;
                }

                public static Entry MakeAction(Option option, Func<Awaitable> action) => new Entry(option, action, null, null);
                public static Entry MakeSubmenu(Option option, Menu submenu) => new Entry(option, null, submenu, null);
                public static Entry MakeSelect(Option option, object payload) => new Entry(option, null, null, payload);

                public bool IsAction => ActionFn != null;
                public bool IsSubmenu => Submenu != null;
                public bool IsSelect => Payload != null;
            }

            public readonly struct Decision<T>
            {
                public readonly string Label;
                public readonly IReadOnlyList<string> Path; // root -> leaf labels
                public readonly T Payload;

                public Decision(string label, IReadOnlyList<string> path, T payload)
                {
                    Label = label;
                    Path = path;
                    Payload = payload;
                }
            }

            /// <summary>
            /// Run a menu tree until the user cancels out of the root.
            /// Selecting an action executes it immediately (classic JRPG feel).
            /// </summary>
            public static async Awaitable Run(Menu root)
            {
                if (root == null) return;

                var stack = new Stack<Menu>();
                stack.Push(root);

                while (stack.Count > 0)
                {
                    var current = stack.Peek();

                    var opts = new List<Option>();
                    for (int i = 0; i < current.Entries.Count; i++)
                        opts.Add(current.Entries[i].Option);

                    bool isSub = stack.Count > 1;
                    if (isSub) opts.Add(new Option("Back"));

                    var pick = await U.PopChoice(current.Title, opts.ToArray(), description: current.Description);

                    if (pick.Cancelled)
                    {
                        if (isSub) stack.Pop();
                        else break;
                        continue;
                    }

                    if (isSub && pick.Value == "Back")
                    {
                        stack.Pop();
                        continue;
                    }

                    var found = FindEntry(current, pick.Value);
                    if (!found.HasValue) continue;

                    var e = found.Value;

                    if (e.IsSubmenu)
                    {
                        stack.Push(e.Submenu);
                        continue;
                    }

                    if (e.IsAction)
                    {
                        await e.ActionFn();
                        continue;
                    }

                    // Select entries do nothing in Run mode by default (they matter for Pick<T>).
                }
            }

            /// <summary>
            /// Advanced: returns a decision payload (leaf) so you can execute later (e.g., battle command resolution).
            ///
            /// Behavior:
            /// - Submenus push/pop like Run()
            /// - Action entries (Add) execute immediately and remain in the menu
            /// - Select entries (AddSelect<T>) return Result.Ok(Decision<T>)
            /// </summary>
            public static async Awaitable<Result<Decision<T>>> Pick<T>(Menu root)
            {
                if (root == null) return Result<Decision<T>>.Canceled();

                var stack = new Stack<Menu>();
                stack.Push(root);

                var path = new List<string>();

                while (stack.Count > 0)
                {
                    var current = stack.Peek();

                    var opts = new List<Option>();
                    for (int i = 0; i < current.Entries.Count; i++)
                        opts.Add(current.Entries[i].Option);

                    bool isSub = stack.Count > 1;
                    if (isSub) opts.Add(new Option("Back"));

                    var pick = await U.PopChoice(current.Title, opts.ToArray(), description: current.Description);

                    if (pick.Cancelled)
                    {
                        if (isSub)
                        {
                            stack.Pop();
                            if (path.Count > 0) path.RemoveAt(path.Count - 1);
                        }
                        else return Result<Decision<T>>.Canceled();

                        continue;
                    }

                    if (isSub && pick.Value == "Back")
                    {
                        stack.Pop();
                        if (path.Count > 0) path.RemoveAt(path.Count - 1);
                        continue;
                    }

                    var found = FindEntry(current, pick.Value);
                    if (!found.HasValue) continue;

                    var e = found.Value;

                    if (e.IsSubmenu)
                    {
                        stack.Push(e.Submenu);
                        path.Add(e.Option.Label);
                        continue;
                    }

                    if (e.IsAction)
                    {
                        await e.ActionFn();
                        continue;
                    }

                    if (e.IsSelect)
                    {
                        if (e.Payload is T payload)
                        {
                            var fullPath = new List<string>(path) { e.Option.Label };
                            return Result<Decision<T>>.Ok(new Decision<T>(e.Option.Label, fullPath, payload));
                        }

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.LogWarning($"[U.Flow] Pick<{typeof(T).Name}> selected '{e.Option.Label}', but payload type was {e.Payload?.GetType().Name ?? "null"}.");
    #endif
                    }
                }

                return Result<Decision<T>>.Canceled();
            }

            private static Entry? FindEntry(Menu current, string label)
            {
                for (int i = 0; i < current.Entries.Count; i++)
                {
                    if (current.Entries[i].Option.Label == label)
                        return current.Entries[i];
                }

                return null;
            }
        }
    }
}
