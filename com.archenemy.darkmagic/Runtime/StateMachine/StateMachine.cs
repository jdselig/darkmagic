using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// StateMachine: a tiny, per-Object state machine with a "V-style" DX.
    /// - No hierarchy, no graphs
    /// - States are types (usually nested under S.* groups)
    /// - Simple enter/exit/change hooks
    /// - Optional locks + guardrail warnings (Editor/Dev builds only)
    /// </summary>
    public sealed class StateMachine
    {
        // Global toggles (typically set via sample SConfig.cs)
        public static bool Trace = false;
        public static bool Warnings = true;

        private static bool WarningsAllowedNow =>
    #if UNITY_EDITOR
            true;
    #else
            Debug.isDebugBuild;
    #endif

        private readonly WeakReference<UnityEngine.Object> _ownerRef;

        private Type _current;
        private Type _lockedTo;   // when set, only this state can be entered
        private bool _locked;     // when true, no transitions allowed (unless forced)

        private readonly Dictionary<Type, List<Action>> _onEnter = new();
        private readonly Dictionary<Type, List<Action>> _onExit = new();
        private readonly List<Action<Type, Type>> _onChange = new();

        internal StateMachine(UnityEngine.Object owner)
        {
            _ownerRef = new WeakReference<UnityEngine.Object>(owner);
        }

        public UnityEngine.Object Owner
        {
            get
            {
                if (_ownerRef.TryGetTarget(out var o)) return o;
                return null;
            }
        }

        public Type Current => _current;
        public string CurrentName => _current?.FullName ?? "(none)";

        public bool HasState => _current != null;

        // -------- Convenience aliases (student-friendly) --------
        public bool Go<TState>() => Enter<TState>();
        public bool In<TState>() => Is<TState>();

        // -------- State queries --------

        public bool Is<TState>() => _current == typeof(TState);

        // -------- Setup helpers --------

        /// <summary>
        /// Sets an initial state only if none is set yet.
        /// Useful for beginners who forget to initialize.
        /// </summary>
        public bool StartIn<TState>()
        {
            if (_current != null)
            {
                Warn($"StartIn<{typeof(TState).Name}> called but state is already {_current.Name}. Ignored.");
                return false;
            }
            return Enter<TState>();
        }

        // -------- Hooks --------

        public void OnChange(Action<Type, Type> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _onChange.Add(handler);
            Guardrail_SubscribeMaybeInUpdate();
        }

        public void OnEnter<TState>(Action handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var t = typeof(TState);
            if (!_onEnter.TryGetValue(t, out var list))
            {
                list = new List<Action>(2);
                _onEnter[t] = list;
            }
            list.Add(handler);
            Guardrail_SubscribeMaybeInUpdate();
        }

        public void OnExit<TState>(Action handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var t = typeof(TState);
            if (!_onExit.TryGetValue(t, out var list))
            {
                list = new List<Action>(2);
                _onExit[t] = list;
            }
            list.Add(handler);
            Guardrail_SubscribeMaybeInUpdate();
        }

        // Aliases (optional sugar)
        public void When<TState>(Action handler) => OnEnter<TState>(handler);
        public void Unless<TState>(Action handler) => OnExit<TState>(handler);

        // -------- Locks --------

        public void Lock() => _locked = true;

        public void Unlock()
        {
            _locked = false;
            _lockedTo = null;
        }

        public bool IsLocked => _locked || _lockedTo != null;

        /// <summary>
        /// Lock state machine so ONLY the given state is allowed.
        /// Optionally forces entry immediately (recommended for "Stunned", "Cutscene", etc).
        /// </summary>
        public bool LockTo<TState>(bool forceEnterNow = true)
        {
            _lockedTo = typeof(TState);
            _locked = false;
            if (forceEnterNow)
                return ForceEnter<TState>();
            return true;
        }

        // -------- Transitions --------

        /// <summary>
        /// Enter a new state.
        /// Returns true if a transition occurred.
        /// Default behavior:
        /// - same-state enter: warn + no-op
        /// - locked: warn + no-op
        /// </summary>
        public bool Enter<TState>() => Enter(typeof(TState), force: false, allowReenter: false);

        /// <summary>
        /// Like Enter, but returns false when blocked.
        /// Provided mainly for clarity when teaching.
        /// </summary>
        public bool TryEnter<TState>() => Enter<TState>();

        /// <summary>
        /// Ignores locks. Still no-ops if you're already in that state.
        /// </summary>
        public bool ForceEnter<TState>() => Enter(typeof(TState), force: true, allowReenter: false);

        /// <summary>
        /// Re-fires OnEnter for the current state without changing state.
        /// If you're not currently in TState, it behaves like Enter.
        /// </summary>
        public bool Reenter<TState>()
        {
            var t = typeof(TState);
            if (_current == t)
            {
                InvokeEnter(t);
                if (Trace) Log($"Reenter {t.Name}");
                return true;
            }
            return Enter(t, force: false, allowReenter: false);
        }

        private bool Enter(Type next, bool force, bool allowReenter)
        {
            var owner = Owner;
            if (owner == null)
                return false; // owner destroyed

            if (!force)
            {
                if (_locked)
                {
                    Warn($"Transition blocked (Lock): {_current?.Name ?? "(none)"} -> {next.Name}");
                    return false;
                }

                if (_lockedTo != null && next != _lockedTo)
                {
                    Warn($"Transition blocked (LockTo<{_lockedTo.Name}>): {_current?.Name ?? "(none)"} -> {next.Name}");
                    return false;
                }
            }

            if (!allowReenter && _current == next)
            {
                Warn($"Enter<{next.Name}> called while already in {next.Name}. No-op. Use Reenter<{next.Name}>() if you meant to retrigger.");
                return false;
            }

            var from = _current;

            if (from != null)
                InvokeExit(from);

            _current = next;

            // Local change hook
            InvokeChange(from, next);

            // Optional V broadcast (global + group-specific)
            BroadcastChange(owner, from, next);

            InvokeEnter(next);

            if (Trace) Log($"{(from?.Name ?? "(none)")} -> {next.Name}");
            return true;
        }

        private void InvokeEnter(Type t)
        {
            if (_onEnter.TryGetValue(t, out var list))
                InvokeList(list);
        }

        private void InvokeExit(Type t)
        {
            if (_onExit.TryGetValue(t, out var list))
                InvokeList(list);
        }

        private void InvokeChange(Type from, Type to)
        {
            for (int i = 0; i < _onChange.Count; i++)
            {
                try { _onChange[i]?.Invoke(from, to); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        private static void InvokeList(List<Action> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                try { list[i]?.Invoke(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        private static void Log(string msg) => Debug.Log($"[S] {msg}");

        private static void Warn(string msg)
        {
            if (!Warnings || !WarningsAllowedNow) return;
            Debug.LogWarning($"[S] {msg}");
        }

        private int _frame = -1;
        private int _subsThisFrame = 0;

        private void Guardrail_SubscribeMaybeInUpdate()
        {
            if (!Warnings || !WarningsAllowedNow) return;

            int frame = Time.frameCount;
            if (_frame != frame)
            {
                _frame = frame;
                _subsThisFrame = 0;
            }

            _subsThisFrame++;
            if (_subsThisFrame == 6)
            {
                Debug.LogWarning(
                    $"[S] Many state subscriptions were added for {Owner?.name ?? "(owner)"} in a single frame.\n" +
                    "Did you call S.OnEnter/OnExit/OnChange inside Update()? Subscribe once in Awake/Start/OnEnable."
                );
            }
        }

        private void BroadcastChange(UnityEngine.Object owner, Type from, Type to)
        {
            // Global
            V.Broadcast<V.StateChanged, V.StateChange>(new V.StateChange(owner, from, to, group: null));

            // Group-specific: infer from declaring type (e.g. S.Player.Falling -> S.Player)
            var group = to?.DeclaringType;
            if (group != null)
            {
                // Broadcast to a group-specific V event: V.StateChanged<S.Player>
                var method = typeof(StateMachine).GetMethod(nameof(BroadcastGroup), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var generic = method.MakeGenericMethod(group);
                generic.Invoke(null, new object[] { owner, from, to, group });
            }
        }

        private static void BroadcastGroup<TGroup>(UnityEngine.Object owner, Type from, Type to, Type group)
        {
            V.Broadcast<V.StateChanged<TGroup>, V.StateChange>(new V.StateChange(owner, from, to, group));
        }
    }
}
