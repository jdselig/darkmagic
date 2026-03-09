using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// V: DX-first event bus for Unity, where events are TYPES.
    ///
    /// Define events as empty marker types:
    ///   public sealed class PlayerDamaged : V.Event<int> {}
    ///
    /// Then:
    ///   V.Broadcast<PlayerDamaged>(12);
    ///   this.On<PlayerDamaged>(dmg => ...);
    ///
    /// Supports 0-3 payload values.
    /// </summary>
    public static partial class V
    {
        /// <summary>
        /// Toggle broadcast tracing to the Unity Console (Editor/Dev builds recommended).
        /// Logs event type and how many listeners were invoked.
        /// </summary>
        public static bool Trace = false;

        /// <summary>
        /// Guardrails are DX-first warnings intended to help beginners avoid common pitfalls.
        /// Recommended: leave on for students.
        /// </summary>
        public static bool Guardrails = true;

        // ---------- Event base types (0-3 payloads) ----------

        public abstract class Event { }
        public abstract class Event<T> : Event { }
        public abstract class Event<T1, T2> : Event { }
        public abstract class Event<T1, T2, T3> : Event { }

        // Use this for "no-args" events if you want a payload type.
        public readonly struct Unit
        {
            public static readonly Unit Value = new Unit();
        }

        // -------- Scoped owner helper --------
        public readonly struct Scope
        {
            private readonly UnityEngine.Object _owner;
            internal Scope(UnityEngine.Object owner) => _owner = owner;

            public void On<TEvent>(Action handler) where TEvent : Event => V.On<TEvent>(handler, _owner);
            public void On<TEvent, T>(Action<T> handler) where TEvent : Event<T> => V.On<TEvent, T>(handler, _owner);
            public void On<TEvent, T1, T2>(Action<T1, T2> handler) where TEvent : Event<T1, T2> => V.On<TEvent, T1, T2>(handler, _owner);
            public void On<TEvent, T1, T2, T3>(Action<T1, T2, T3> handler) where TEvent : Event<T1, T2, T3> => V.On<TEvent, T1, T2, T3>(handler, _owner);

            public void Once<TEvent>(Action handler) where TEvent : Event => V.Once<TEvent>(handler, _owner);
            public void Once<TEvent, T>(Action<T> handler) where TEvent : Event<T> => V.Once<TEvent, T>(handler, _owner);
            public void Once<TEvent, T1, T2>(Action<T1, T2> handler) where TEvent : Event<T1, T2> => V.Once<TEvent, T1, T2>(handler, _owner);
            public void Once<TEvent, T1, T2, T3>(Action<T1, T2, T3> handler) where TEvent : Event<T1, T2, T3> => V.Once<TEvent, T1, T2, T3>(handler, _owner);

            public IDisposable OnDisposable<TEvent, T>(Action<T> handler) where TEvent : Event<T> => V.OnDisposable<TEvent, T>(handler, _owner);
        }

        public static Scope With(UnityEngine.Object owner) => new Scope(owner);
        // -------- End scoped helper --------

        // -------- Public API: Broadcast --------

        public static void Broadcast<TEvent>() where TEvent : Event
            => Bus0.Publish(typeof(TEvent));

        /// <summary>
        /// Student-friendly overload: allows `V.Broadcast&lt;MyEvent&gt;(payload)` without specifying the payload type parameter.
        /// Runtime-validates that MyEvent inherits from Event&lt;T&gt; (or Event&lt;T1,T2&gt;/Event&lt;T1,T2,T3&gt;).
        /// </summary>
        public static void Broadcast<TEvent>(object payload) where TEvent : Event
        {
            var baseType = GetEventBase(typeof(TEvent));
            if (baseType == null || !baseType.IsGenericType)
            {
                Warn($"Broadcast<{typeof(TEvent).Name}>(payload) used, but event has no payload type.");
                return;
            }

            var def = baseType.GetGenericTypeDefinition();
            if (def == typeof(Event<>))
            {
                var t = baseType.GetGenericArguments()[0];
                Publish1(typeof(TEvent), t, payload);
                return;
            }

            Warn($"Broadcast<{typeof(TEvent).Name}>(payload) used, but event expects {baseType.GetGenericArguments().Length} payload values.");
        }

        public static void Broadcast<TEvent>(object a, object b) where TEvent : Event
        {
            var baseType = GetEventBase(typeof(TEvent));
            if (baseType == null || !baseType.IsGenericType)
            {
                Warn($"Broadcast<{typeof(TEvent).Name}>(a,b) used, but event has no payload type.");
                return;
            }

            var def = baseType.GetGenericTypeDefinition();
            if (def == typeof(Event<,>))
            {
                var args = baseType.GetGenericArguments();
                Publish2(typeof(TEvent), args[0], args[1], a, b);
                return;
            }

            Warn($"Broadcast<{typeof(TEvent).Name}>(a,b) used, but event expects {baseType.GetGenericArguments().Length} payload values.");
        }

        public static void Broadcast<TEvent>(object a, object b, object c) where TEvent : Event
        {
            var baseType = GetEventBase(typeof(TEvent));
            if (baseType == null || !baseType.IsGenericType)
            {
                Warn($"Broadcast<{typeof(TEvent).Name}>(a,b,c) used, but event has no payload type.");
                return;
            }

            var def = baseType.GetGenericTypeDefinition();
            if (def == typeof(Event<,,>))
            {
                var args = baseType.GetGenericArguments();
                Publish3(typeof(TEvent), args[0], args[1], args[2], a, b, c);
                return;
            }

            Warn($"Broadcast<{typeof(TEvent).Name}>(a,b,c) used, but event expects {baseType.GetGenericArguments().Length} payload values.");
        }

        private static Type GetEventBase(Type eventType)
        {
            // Walk up until we hit V.Event<...> or V.Event
            var t = eventType;
            while (t != null && t != typeof(object))
            {
                if (t.IsGenericType)
                {
                    var def = t.GetGenericTypeDefinition();
                    if (def == typeof(Event<>) || def == typeof(Event<,>) || def == typeof(Event<,,>))
                        return t;
                }


        // ---- Dynamic subscriptions (student-friendly On<TEvent>(payload => ...)) ----












                if (t == typeof(Event))
                    return t;

                t = t.BaseType;
            }

            return null;
        }

        private static void Publish1(Type eventType, Type payloadType, object payload)
        {
            try
            {
                // Call Bus1<T>.Publish(typeof(TEvent), (T)payload)
                var busType = typeof(Bus1<>).MakeGenericType(payloadType);
                var mi = busType.GetMethod("Publish", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                mi.Invoke(null, new object[] { eventType, payload });
            }
            catch (Exception e)
            {
                Warn($"Broadcast<{eventType.Name}>(payload) failed: {e.Message}");
            }
        }

        private static void Publish2(Type eventType, Type t1, Type t2, object a, object b)
        {
            try
            {
                // Bus2<(T1,T2)>.Publish(typeof(TEvent), (ValueTuple<T1,T2>)tuple)
                var tupleType = typeof(ValueTuple<,>).MakeGenericType(t1, t2);
                var tuple = Activator.CreateInstance(tupleType, a, b);

                var busType = typeof(Bus2<>).MakeGenericType(tupleType);
                var mi = busType.GetMethod("Publish", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                mi.Invoke(null, new object[] { eventType, tuple });
            }
            catch (Exception e)
            {
                Warn($"Broadcast<{eventType.Name}>(a,b) failed: {e.Message}");
            }
        }

        private static void Publish3(Type eventType, Type t1, Type t2, Type t3, object a, object b, object c)
        {
            try
            {
                var tupleType = typeof(ValueTuple<,,>).MakeGenericType(t1, t2, t3);
                var tuple = Activator.CreateInstance(tupleType, a, b, c);

                var busType = typeof(Bus3<>).MakeGenericType(tupleType);
                var mi = busType.GetMethod("Publish", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                mi.Invoke(null, new object[] { eventType, tuple });
            }
            catch (Exception e)
            {
                Warn($"Broadcast<{eventType.Name}>(a,b,c) failed: {e.Message}");
            }
        }

        private static void Warn(string msg)
        {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[V] " + msg);
    #endif
        }


        public static void Broadcast<TEvent, T>(T payload) where TEvent : Event<T>
            => Bus1<T>.Publish(typeof(TEvent), payload);

        public static void Broadcast<TEvent, T1, T2>(T1 a, T2 b) where TEvent : Event<T1, T2>
            => Bus2<(T1, T2)>.Publish(typeof(TEvent), (a, b));

        public static void Broadcast<TEvent, T1, T2, T3>(T1 a, T2 b, T3 c) where TEvent : Event<T1, T2, T3>
            => Bus3<(T1, T2, T3)>.Publish(typeof(TEvent), (a, b, c));

        // -------- Public API: On --------

        public static void On<TEvent>(Action handler, UnityEngine.Object owner = null) where TEvent : Event
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var resolvedOwner = owner ?? InferOwner(handler);

            Bus0.Subscribe(typeof(TEvent), handler, resolvedOwner, handler);
        }



        /// <summary>
        /// Student-friendly payload listener with ONE generic arg:
        /// `this.On<MyEvent>(value => ...)` where `MyEvent : V.Event<T>` (or 2/3 args).
        /// IL2CPP-safe: no `dynamic`.
        /// </summary>

    /// <summary>
    /// Student-friendly payload listener with ONE generic arg:
    /// `this.On<MyEvent>(value => ...)` where `MyEvent : V.Event<T>` (or 2/3 args).
    /// IL2CPP-safe: no `dynamic`.
    /// </summary>
    public static void On<TEvent>(Delegate handler, UnityEngine.Object owner = null) where TEvent : Event
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        var resolvedOwner = owner ?? InferOwner(handler);

        var baseType = GetEventBase(typeof(TEvent));
        if (baseType == null || !baseType.IsGenericType)
        {
            Warn($"On<{typeof(TEvent).Name}>(handler) used, but event has no payload type.");
            return;
        }

        var def = baseType.GetGenericTypeDefinition();
        var args = baseType.GetGenericArguments();

        if (def == typeof(Event<>)) { SubscribeDelegate1(typeof(TEvent), args[0], handler, resolvedOwner); return; }
        if (def == typeof(Event<,>)) { SubscribeDelegate2(typeof(TEvent), args[0], args[1], handler, resolvedOwner); return; }
        if (def == typeof(Event<,,>)) { SubscribeDelegate3(typeof(TEvent), args[0], args[1], args[2], handler, resolvedOwner); return; }

        Warn($"On<{typeof(TEvent).Name}>(handler) used, but payload arity is unsupported.");
    }

    /// <summary>
    /// Student-friendly payload listener with ONE generic arg and no helper wrapper:
    /// `this.On<MyEvent>(x => { var v = (T)x; ... })`
    /// Under the hood this uses Action<object> and casts to the event payload type.
    /// IL2CPP-safe. (Note: boxing for value types.)
    /// </summary>
    public static void On<TEvent>(Action<object> handler, UnityEngine.Object owner = null) where TEvent : Event
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        var resolvedOwner = owner ?? InferOwner(handler);

        var baseType = GetEventBase(typeof(TEvent));
        if (baseType == null || !baseType.IsGenericType)
        {
            Warn($"On<{typeof(TEvent).Name}>(Action<object>) used, but event has no payload type.");
            return;
        }

        var def = baseType.GetGenericTypeDefinition();
        var args = baseType.GetGenericArguments();

        if (def == typeof(Event<>))
        {
            var mi = typeof(V).GetMethod(nameof(SubscribeObj1), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            mi = mi.MakeGenericMethod(typeof(TEvent), args[0]);
            mi.Invoke(null, new object[] { handler, resolvedOwner });
            return;
        }

        Warn($"On<{typeof(TEvent).Name}>(Action<object>) requires an Event<T> payload.");
    }

    public static void On<TEvent>(Action<object, object> handler, UnityEngine.Object owner = null) where TEvent : Event
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        var resolvedOwner = owner ?? InferOwner(handler);

        var baseType = GetEventBase(typeof(TEvent));
        if (baseType == null || !baseType.IsGenericType)
        {
            Warn($"On<{typeof(TEvent).Name}>(Action<object,object>) used, but event has no payload type.");
            return;
        }

        var def = baseType.GetGenericTypeDefinition();
        var args = baseType.GetGenericArguments();

        if (def == typeof(Event<,>))
        {
            var mi = typeof(V).GetMethod(nameof(SubscribeObj2), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            mi = mi.MakeGenericMethod(typeof(TEvent), args[0], args[1]);
            mi.Invoke(null, new object[] { handler, resolvedOwner });
            return;
        }

        Warn($"On<{typeof(TEvent).Name}>(Action<object,object>) requires an Event<T1,T2> payload.");
    }

    public static void On<TEvent>(Action<object, object, object> handler, UnityEngine.Object owner = null) where TEvent : Event
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        var resolvedOwner = owner ?? InferOwner(handler);

        var baseType = GetEventBase(typeof(TEvent));
        if (baseType == null || !baseType.IsGenericType)
        {
            Warn($"On<{typeof(TEvent).Name}>(Action<object,object,object>) used, but event has no payload type.");
            return;
        }

        var def = baseType.GetGenericTypeDefinition();
        var args = baseType.GetGenericArguments();

        if (def == typeof(Event<,,>))
        {
            var mi = typeof(V).GetMethod(nameof(SubscribeObj3), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            mi = mi.MakeGenericMethod(typeof(TEvent), args[0], args[1], args[2]);
            mi.Invoke(null, new object[] { handler, resolvedOwner });
            return;
        }

        Warn($"On<{typeof(TEvent).Name}>(Action<object,object,object>) requires an Event<T1,T2,T3> payload.");
    }

    /// <summary>
    /// Student-friendly overload: allows `this.On<MyEvent,TPayload>(payload => ...)` (strongly typed).
    /// </summary>
    public static void On<TEvent, T>(Action<T> handler, UnityEngine.Object owner = null) where TEvent : Event<T>
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var resolvedOwner = owner ?? InferOwner(handler);

            Bus1<T>.Subscribe(typeof(TEvent), handler, resolvedOwner, handler);
        }

        public static void On<TEvent, T1, T2>(Action<T1, T2> handler, UnityEngine.Object owner = null) where TEvent : Event<T1, T2>
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var resolvedOwner = owner ?? InferOwner(handler);

            Action<(T1, T2)> wrapped = t => handler(t.Item1, t.Item2);
            Bus2<(T1, T2)>.Subscribe(typeof(TEvent), wrapped, resolvedOwner, handler);
        }

        public static void On<TEvent, T1, T2, T3>(Action<T1, T2, T3> handler, UnityEngine.Object owner = null) where TEvent : Event<T1, T2, T3>
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var resolvedOwner = owner ?? InferOwner(handler);

            Action<(T1, T2, T3)> wrapped = t => handler(t.Item1, t.Item2, t.Item3);
            Bus3<(T1, T2, T3)>.Subscribe(typeof(TEvent), wrapped, resolvedOwner, handler);
        }

        // -------- Public API: Once --------

        public static void Once<TEvent>(Action handler, UnityEngine.Object owner = null) where TEvent : Event
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var resolvedOwner = owner ?? InferOwner(handler);

            IDisposable sub = null;
            Action wrapped = () =>
            {
                try { handler(); }
                finally { sub?.Dispose(); }
            };

            sub = Bus0.SubscribeDisposable(typeof(TEvent), wrapped, resolvedOwner, handler);
        }










        public static void Once<TEvent>(Delegate handler, UnityEngine.Object owner = null) where TEvent : Event
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var resolvedOwner = owner ?? InferOwner(handler);

            var baseType = GetEventBase(typeof(TEvent));
            if (baseType == null || !baseType.IsGenericType)
            {
                Warn($"Once<{typeof(TEvent).Name}>(handler) used, but event has no payload type.");
                return;
            }

            var def = baseType.GetGenericTypeDefinition();
            var args = baseType.GetGenericArguments();

            if (def == typeof(Event<>)) { SubscribeDelegateOnce1(typeof(TEvent), args[0], handler, resolvedOwner); return; }
            if (def == typeof(Event<,>)) { SubscribeDelegateOnce2(typeof(TEvent), args[0], args[1], handler, resolvedOwner); return; }
            if (def == typeof(Event<,,>)) { SubscribeDelegateOnce3(typeof(TEvent), args[0], args[1], args[2], handler, resolvedOwner); return; }

            Warn($"Once<{typeof(TEvent).Name}>(handler) used, but payload arity is unsupported.");
        }

        // --- cast-friendly overloads (shortest callsite; payload comes in as object/object/object) ---
        public static void Once<TEvent>(Action<object> handler, UnityEngine.Object owner = null) where TEvent : Event
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var resolvedOwner = owner ?? InferOwner(handler);

            var baseType = GetEventBase(typeof(TEvent));
            if (baseType == null || !baseType.IsGenericType)
            {
                Warn($"Once<{typeof(TEvent).Name}>(Action<object>) used, but event has no payload type.");
                return;
            }

            var def = baseType.GetGenericTypeDefinition();
            var args = baseType.GetGenericArguments();

            if (def == typeof(Event<>))
            {
                var mi = typeof(V).GetMethod(nameof(SubscribeObjOnce1), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                mi = mi.MakeGenericMethod(typeof(TEvent), args[0]);
                mi.Invoke(null, new object[] { handler, resolvedOwner });
                return;
            }

            Warn($"Once<{typeof(TEvent).Name}>(Action<object>) requires an Event<T> payload.");
        }

        public static void Once<TEvent>(Action<object, object> handler, UnityEngine.Object owner = null) where TEvent : Event
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var resolvedOwner = owner ?? InferOwner(handler);

            var baseType = GetEventBase(typeof(TEvent));
            if (baseType == null || !baseType.IsGenericType)
            {
                Warn($"Once<{typeof(TEvent).Name}>(Action<object,object>) used, but event has no payload type.");
                return;
            }

            var def = baseType.GetGenericTypeDefinition();
            var args = baseType.GetGenericArguments();

            if (def == typeof(Event<,>))
            {
                var mi = typeof(V).GetMethod(nameof(SubscribeObjOnce2), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                mi = mi.MakeGenericMethod(typeof(TEvent), args[0], args[1]);
                mi.Invoke(null, new object[] { handler, resolvedOwner });
                return;
            }

            Warn($"Once<{typeof(TEvent).Name}>(Action<object,object>) requires an Event<T1,T2> payload.");
        }

        public static void Once<TEvent>(Action<object, object, object> handler, UnityEngine.Object owner = null) where TEvent : Event
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var resolvedOwner = owner ?? InferOwner(handler);

            var baseType = GetEventBase(typeof(TEvent));
            if (baseType == null || !baseType.IsGenericType)
            {
                Warn($"Once<{typeof(TEvent).Name}>(Action<object,object,object>) used, but event has no payload type.");
                return;
            }

            var def = baseType.GetGenericTypeDefinition();
            var args = baseType.GetGenericArguments();

            if (def == typeof(Event<,,>))
            {
                var mi = typeof(V).GetMethod(nameof(SubscribeObjOnce3), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                mi = mi.MakeGenericMethod(typeof(TEvent), args[0], args[1], args[2]);
                mi.Invoke(null, new object[] { handler, resolvedOwner });
                return;
            }

            Warn($"Once<{typeof(TEvent).Name}>(Action<object,object,object>) requires an Event<T1,T2,T3> payload.");
        }

        public static void Once<TEvent, T>(Action<T> handler, UnityEngine.Object owner = null) where TEvent : Event<T>
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var resolvedOwner = owner ?? InferOwner(handler);

            IDisposable sub = null;
            Action<T> wrapped = payload =>
            {
                try { handler(payload); }
                finally { sub?.Dispose(); }
            };

            sub = Bus1<T>.SubscribeDisposable(typeof(TEvent), wrapped, resolvedOwner, handler);
        }

        public static void Once<TEvent, T1, T2>(Action<T1, T2> handler, UnityEngine.Object owner = null) where TEvent : Event<T1, T2>
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var resolvedOwner = owner ?? InferOwner(handler);

            IDisposable sub = null;
            Action<(T1, T2)> wrapped = t =>
            {
                try { handler(t.Item1, t.Item2); }
                finally { sub?.Dispose(); }
            };

            sub = Bus2<(T1, T2)>.SubscribeDisposable(typeof(TEvent), wrapped, resolvedOwner, handler);
        }

        public static void Once<TEvent, T1, T2, T3>(Action<T1, T2, T3> handler, UnityEngine.Object owner = null) where TEvent : Event<T1, T2, T3>
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var resolvedOwner = owner ?? InferOwner(handler);

            IDisposable sub = null;
            Action<(T1, T2, T3)> wrapped = t =>
            {
                try { handler(t.Item1, t.Item2, t.Item3); }
                finally { sub?.Dispose(); }
            };

            sub = Bus3<(T1, T2, T3)>.SubscribeDisposable(typeof(TEvent), wrapped, resolvedOwner, handler);
        }

        public static IDisposable OnDisposable<TEvent, T>(Action<T> handler, UnityEngine.Object owner = null) where TEvent : Event<T>
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var resolvedOwner = owner ?? InferOwner(handler);

            return Bus1<T>.SubscribeDisposable(typeof(TEvent), handler, resolvedOwner, handler);
        }

        // -------- Internals --------

        private static UnityEngine.Object InferOwner(Delegate d) => d.Target as UnityEngine.Object;

        private static bool ShouldWarn =>
    #if UNITY_EDITOR
            Guardrails;
    #else
            Guardrails && Debug.isDebugBuild;
    #endif

        private static string Label(Type evtType) => evtType?.FullName ?? "(event)";

        private sealed class Unsub : IDisposable
        {
            private Action _dispose;
            public Unsub(Action dispose) => _dispose = dispose;
            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }

        private sealed class Sub<TPayload>
        {
            public readonly Action<TPayload> Handler;
            public readonly WeakReference<UnityEngine.Object> Owner; // may be null
            public readonly Delegate Original;

            public Sub(Action<TPayload> handler, UnityEngine.Object owner, Delegate original)
            {
                Handler = handler;
                Original = original;
                Owner = owner != null ? new WeakReference<UnityEngine.Object>(owner) : null;
            }

            public bool IsAlive()
            {
                if (Owner == null) return true; // global listener; never auto-removed
                if (!Owner.TryGetTarget(out var o)) return false;
                return o != null; // Unity destroyed objects compare == null
            }
        }

        private struct OwnerEventKey : IEquatable<OwnerEventKey>
        {
            public Type EventType;
            public int OwnerId;

            public bool Equals(OwnerEventKey other) => EventType == other.EventType && OwnerId == other.OwnerId;
            public override bool Equals(object obj) => obj is OwnerEventKey other && Equals(other);
            public override int GetHashCode() => ((EventType != null ? EventType.GetHashCode() : 0) * 397) ^ OwnerId;
        }

        private struct FrameCounter
        {
            public int Frame;
            public int AddedThisFrame;
        }

        private static void GuardrailChecks(Type evtType, UnityEngine.Object owner, Delegate originalDelegateForWarning,
            Dictionary<Type, List<object>> subsByEvent, Dictionary<OwnerEventKey, FrameCounter> addsPerOwnerPerEvent, HashSet<Type> warnedGlobal)
        {
            if (!ShouldWarn) return;

            string label = Label(evtType);

            // Trap B: global listener (no owner)
            if (owner == null)
            {
                if (warnedGlobal.Add(evtType))
                {
                    Debug.LogWarning(
                        $"[V] Listener added without an owner for {label}.\n" +
                        "This listener may live forever. In MonoBehaviours, prefer: this.On / this.Once."
                    );
                }
                return;
            }

            // Trap A-ish: repeated subscriptions (often from Update)
            int ownerId = owner.GetInstanceID();
            var key = new OwnerEventKey { EventType = evtType, OwnerId = ownerId };

            int frame = Time.frameCount;
            addsPerOwnerPerEvent.TryGetValue(key, out var fc);

            if (fc.Frame != frame)
            {
                fc.Frame = frame;
                fc.AddedThisFrame = 0;
            }

            fc.AddedThisFrame++;
            addsPerOwnerPerEvent[key] = fc;

            if (fc.AddedThisFrame == 3)
            {
                Debug.LogWarning(
                    $"[V] {owner.name} subscribed to {label} multiple times in the same frame.\n" +
                    "Did you call this.On(...) inside Update()? Subscribe once in Awake/Start/OnEnable."
                );
            }

            if (!subsByEvent.TryGetValue(evtType, out var listObj) || listObj.Count == 0)
                return;

            bool sameOwnerExists = false;
            bool exactMethodMatch = false;

            for (int i = listObj.Count - 1; i >= 0; i--)
            {
                var o = listObj[i];
                var ownerField = o.GetType().GetField("Owner");
                var originalField = o.GetType().GetField("Original");

                if (ownerField == null || originalField == null) continue;

                var ownerRef = ownerField.GetValue(o) as WeakReference<UnityEngine.Object>;
                if (ownerRef == null) continue;

                if (!ownerRef.TryGetTarget(out var existingOwner) || existingOwner == null) continue;

                if (existingOwner.GetInstanceID() == ownerId)
                {
                    sameOwnerExists = true;

                    var existingOriginal = originalField.GetValue(o) as Delegate;
                    if (existingOriginal != null && originalDelegateForWarning != null &&
                        existingOriginal.Method == originalDelegateForWarning.Method)
                    {
                        exactMethodMatch = true;
                        break;
                    }
                }
            }

            if (exactMethodMatch)
            {
                Debug.LogWarning(
                    $"[V] Possible duplicate subscription: {owner.name} subscribed again to {label} using the same method.\n" +
                    "If something is firing twice, check that you're not subscribing multiple times (Awake/OnEnable/Start)."
                );
            }
            else if (sameOwnerExists && fc.AddedThisFrame == 1)
            {
                Debug.LogWarning(
                    $"[V] {owner.name} subscribed again to {label}.\n" +
                    "If something is firing twice, check where subscriptions are created. Prefer subscribing once (Awake/Start/OnEnable)."
                );
            }
        }

        // -------- Buses (0-3 payloads) --------



            // ---- Delegate-based payload subscriptions (IL2CPP-safe, no `dynamic`) ----

        private static System.Reflection.MethodInfo GetBusMethod(string methodName, int arity, Type[] genericArgs)
        {
            var busName = arity switch
            {
                1 => "Bus1`1",
                2 => "Bus2`2",
                3 => "Bus3`3",
                _ => null
            };

            if (busName == null) return null;

            var busOpen = typeof(V).GetNestedType(busName, System.Reflection.BindingFlags.NonPublic);
            if (busOpen == null) return null;

            var busClosed = busOpen.MakeGenericType(genericArgs);
            return busClosed.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        }

        private static void SubscribeDelegate1(Type eventType, Type t, Delegate handler, UnityEngine.Object owner)
        {
            var expected = typeof(Action<>).MakeGenericType(t);
            if (!expected.IsAssignableFrom(handler.GetType()))
            {
                Warn($"[V] On<{eventType.Name}> expected Action<{t.Name}> but got {handler.GetType().Name}.");
                return;
            }

            var mi = GetBusMethod("Subscribe", 1, new[] { t });
            if (mi == null) { Warn("[V] Internal error: could not find Bus1<T>.Subscribe"); return; }

            mi.Invoke(null, new object[] { eventType, handler, owner, handler });
        }

        private static void SubscribeDelegate2(Type eventType, Type t1, Type t2, Delegate handler, UnityEngine.Object owner)
        {
            var expected = typeof(Action<,>).MakeGenericType(t1, t2);
            if (!expected.IsAssignableFrom(handler.GetType()))
            {
                Warn($"[V] On<{eventType.Name}> expected Action<{t1.Name},{t2.Name}> but got {handler.GetType().Name}.");
                return;
            }

            var mi = GetBusMethod("Subscribe", 2, new[] { t1, t2 });
            if (mi == null) { Warn("[V] Internal error: could not find Bus2<T1,T2>.Subscribe"); return; }

            mi.Invoke(null, new object[] { eventType, handler, owner, handler });
        }

        private static void SubscribeDelegate3(Type eventType, Type t1, Type t2, Type t3, Delegate handler, UnityEngine.Object owner)
        {
            var expected = typeof(Action<,,>).MakeGenericType(t1, t2, t3);
            if (!expected.IsAssignableFrom(handler.GetType()))
            {
                Warn($"[V] On<{eventType.Name}> expected Action<{t1.Name},{t2.Name},{t3.Name}> but got {handler.GetType().Name}.");
                return;
            }

            var mi = GetBusMethod("Subscribe", 3, new[] { t1, t2, t3 });
            if (mi == null) { Warn("[V] Internal error: could not find Bus3<T1,T2,T3>.Subscribe"); return; }

            mi.Invoke(null, new object[] { eventType, handler, owner, handler });
        }

        private static void SubscribeDelegateOnce1(Type eventType, Type t, Delegate handler, UnityEngine.Object owner)
        {
            var expected = typeof(Action<>).MakeGenericType(t);
            if (!expected.IsAssignableFrom(handler.GetType()))
            {
                Warn($"[V] Once<{eventType.Name}> expected Action<{t.Name}> but got {handler.GetType().Name}.");
                return;
            }

            var mi = GetBusMethod("SubscribeDisposable", 1, new[] { t });
            if (mi == null) { Warn("[V] Internal error: could not find Bus1<T>.SubscribeDisposable"); return; }

            IDisposable disp = null;
            var wrapperType = typeof(OnceWrapper1<>).MakeGenericType(t);
            var wrapper = Activator.CreateInstance(wrapperType, handler, (Action)(() => disp?.Dispose()));
            var wrapped = Delegate.CreateDelegate(expected, wrapper, "Invoke");

            disp = (IDisposable)mi.Invoke(null, new object[] { eventType, wrapped, owner, handler });
        }

        private static void SubscribeDelegateOnce2(Type eventType, Type t1, Type t2, Delegate handler, UnityEngine.Object owner)
        {
            var expected = typeof(Action<,>).MakeGenericType(t1, t2);
            if (!expected.IsAssignableFrom(handler.GetType()))
            {
                Warn($"[V] Once<{eventType.Name}> expected Action<{t1.Name},{t2.Name}> but got {handler.GetType().Name}.");
                return;
            }

            var mi = GetBusMethod("SubscribeDisposable", 2, new[] { t1, t2 });
            if (mi == null) { Warn("[V] Internal error: could not find Bus2<T1,T2>.SubscribeDisposable"); return; }

            IDisposable disp = null;
            var wrapperType = typeof(OnceWrapper2<,>).MakeGenericType(t1, t2);
            var wrapper = Activator.CreateInstance(wrapperType, handler, (Action)(() => disp?.Dispose()));
            var wrapped = Delegate.CreateDelegate(expected, wrapper, "Invoke");

            disp = (IDisposable)mi.Invoke(null, new object[] { eventType, wrapped, owner, handler });
        }

        private static void SubscribeDelegateOnce3(Type eventType, Type t1, Type t2, Type t3, Delegate handler, UnityEngine.Object owner)
        {
            var expected = typeof(Action<,,>).MakeGenericType(t1, t2, t3);
            if (!expected.IsAssignableFrom(handler.GetType()))
            {
                Warn($"[V] Once<{eventType.Name}> expected Action<{t1.Name},{t2.Name},{t3.Name}> but got {handler.GetType().Name}.");
                return;
            }

            var mi = GetBusMethod("SubscribeDisposable", 3, new[] { t1, t2, t3 });
            if (mi == null) { Warn("[V] Internal error: could not find Bus3<T1,T2,T3>.SubscribeDisposable"); return; }

            IDisposable disp = null;
            var wrapperType = typeof(OnceWrapper3<,,>).MakeGenericType(t1, t2, t3);
            var wrapper = Activator.CreateInstance(wrapperType, handler, (Action)(() => disp?.Dispose()));
            var wrapped = Delegate.CreateDelegate(expected, wrapper, "Invoke");

            disp = (IDisposable)mi.Invoke(null, new object[] { eventType, wrapped, owner, handler });
        }

        private sealed class OnceWrapper1<T>
        {
            private readonly Action<T> _inner;
            private readonly Action _dispose;
            public OnceWrapper1(Delegate inner, Action dispose) { _inner = (Action<T>)inner; _dispose = dispose; }
            public void Invoke(T a) { _dispose(); _inner(a); }
        }

        private sealed class OnceWrapper2<T1, T2>
        {
            private readonly Action<T1, T2> _inner;
            private readonly Action _dispose;
            public OnceWrapper2(Delegate inner, Action dispose) { _inner = (Action<T1, T2>)inner; _dispose = dispose; }
            public void Invoke(T1 a, T2 b) { _dispose(); _inner(a, b); }
        }

        private sealed class OnceWrapper3<T1, T2, T3>
        {
            private readonly Action<T1, T2, T3> _inner;
            private readonly Action _dispose;
            public OnceWrapper3(Delegate inner, Action dispose) { _inner = (Action<T1, T2, T3>)inner; _dispose = dispose; }
            public void Invoke(T1 a, T2 b, T3 c) { _dispose(); _inner(a, b, c); }
        }



        // ---- Action<object> subscriptions (IL2CPP-safe, simplest callsite) ----
        private static void SubscribeObj1<TEvent, T>(Action<object> handler, UnityEngine.Object owner) where TEvent : Event<T>
        {
            Action<T> typed = (T v) => handler(v);
            Bus1<T>.Subscribe(typeof(TEvent), typed, owner, handler);
        }

        private static void SubscribeObj2<TEvent, T1, T2>(Action<object, object> handler, UnityEngine.Object owner) where TEvent : Event<T1, T2>
        {
            Action<(T1, T2)> wrapped = t => handler(t.Item1, t.Item2);
            Bus2<(T1, T2)>.Subscribe(typeof(TEvent), wrapped, owner, handler);
        }

        private static void SubscribeObj3<TEvent, T1, T2, T3>(Action<object, object, object> handler, UnityEngine.Object owner) where TEvent : Event<T1, T2, T3>
        {
            Action<(T1, T2, T3)> wrapped = t => handler(t.Item1, t.Item2, t.Item3);
            Bus3<(T1, T2, T3)>.Subscribe(typeof(TEvent), wrapped, owner, handler);
        }

        private static void SubscribeObjOnce1<TEvent, T>(Action<object> handler, UnityEngine.Object owner) where TEvent : Event<T>
            => Once<TEvent, T>((T v) => handler(v), owner);

        private static void SubscribeObjOnce2<TEvent, T1, T2>(Action<object, object> handler, UnityEngine.Object owner) where TEvent : Event<T1, T2>
            => Once<TEvent, T1, T2>((T1 a, T2 b) => handler(a, b), owner);

        private static void SubscribeObjOnce3<TEvent, T1, T2, T3>(Action<object, object, object> handler, UnityEngine.Object owner) where TEvent : Event<T1, T2, T3>
            => Once<TEvent, T1, T2, T3>((T1 a, T2 b, T3 c) => handler(a, b, c), owner);

    static class Bus0
        {
            private static readonly Dictionary<Type, List<Sub<Unit>>> _subs = new();
            private static readonly HashSet<Type> _warnedGlobal = new();
            private static readonly Dictionary<OwnerEventKey, FrameCounter> _addsPerOwnerPerEvent = new();
            private static readonly Dictionary<Type, List<object>> _boxedSubs = new();

            public static void Subscribe(Type evtType, Action handler, UnityEngine.Object owner, Delegate original)
            {
                GuardrailChecks(evtType, owner, original, _boxedSubs, _addsPerOwnerPerEvent, _warnedGlobal);

                if (!_subs.TryGetValue(evtType, out var list))
                {
                    list = new List<Sub<Unit>>(4);
                    _subs[evtType] = list;
                }
                Action<Unit> wrapped = _ => handler();
                var sub = new Sub<Unit>(wrapped, owner, original);
                list.Add(sub);

                if (!_boxedSubs.TryGetValue(evtType, out var boxed))
                {
                    boxed = new List<object>(4);
                    _boxedSubs[evtType] = boxed;
                }
                boxed.Add(sub);
            }

            public static IDisposable SubscribeDisposable(Type evtType, Action handler, UnityEngine.Object owner, Delegate original)
            {
                GuardrailChecks(evtType, owner, original, _boxedSubs, _addsPerOwnerPerEvent, _warnedGlobal);

                if (!_subs.TryGetValue(evtType, out var list))
                {
                    list = new List<Sub<Unit>>(4);
                    _subs[evtType] = list;
                }
                Action<Unit> wrapped = _ => handler();
                var sub = new Sub<Unit>(wrapped, owner, original);
                list.Add(sub);

                if (!_boxedSubs.TryGetValue(evtType, out var boxed))
                {
                    boxed = new List<object>(4);
                    _boxedSubs[evtType] = boxed;
                }
                boxed.Add(sub);

                return new Unsub(() =>
                {
                    if (_subs.TryGetValue(evtType, out var l))
                    {
                        l.Remove(sub);
                        if (l.Count == 0) _subs.Remove(evtType);
                    }
                    if (_boxedSubs.TryGetValue(evtType, out var b))
                    {
                        b.Remove(sub);
                        if (b.Count == 0) _boxedSubs.Remove(evtType);
                    }
                });
            }

            public static void Publish(Type evtType)
            {
                if (!_subs.TryGetValue(evtType, out var list) || list.Count == 0)
                {
                    if (Trace) Debug.Log($"[V] Broadcast {Label(evtType)} (0 listeners)");
                    return;
                }

                int invoked = 0;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var s = list[i];
                    if (!s.IsAlive())
                    {
                        list.RemoveAt(i);
                        continue;
                    }

                    try
                    {
                        s.Handler(Unit.Value);
                        invoked++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                if (Trace)
                    Debug.Log($"[V] Broadcast {Label(evtType)} ({invoked} listener{(invoked == 1 ? "" : "s")})");

                if (list.Count == 0)
                    _subs.Remove(evtType);
            }
        }

        private static class Bus1<T>
        {
            private static readonly Dictionary<Type, List<Sub<T>>> _subs = new();
            private static readonly HashSet<Type> _warnedGlobal = new();
            private static readonly Dictionary<OwnerEventKey, FrameCounter> _addsPerOwnerPerEvent = new();
            private static readonly Dictionary<Type, List<object>> _boxedSubs = new();

            public static void Subscribe(Type evtType, Action<T> handler, UnityEngine.Object owner, Delegate original)
            {
                GuardrailChecks(evtType, owner, original, _boxedSubs, _addsPerOwnerPerEvent, _warnedGlobal);

                if (!_subs.TryGetValue(evtType, out var list))
                {
                    list = new List<Sub<T>>(4);
                    _subs[evtType] = list;
                }
                var sub = new Sub<T>(handler, owner, original);
                list.Add(sub);

                if (!_boxedSubs.TryGetValue(evtType, out var boxed))
                {
                    boxed = new List<object>(4);
                    _boxedSubs[evtType] = boxed;
                }
                boxed.Add(sub);
            }

            public static IDisposable SubscribeDisposable(Type evtType, Action<T> handler, UnityEngine.Object owner, Delegate original)
            {
                GuardrailChecks(evtType, owner, original, _boxedSubs, _addsPerOwnerPerEvent, _warnedGlobal);

                if (!_subs.TryGetValue(evtType, out var list))
                {
                    list = new List<Sub<T>>(4);
                    _subs[evtType] = list;
                }
                var sub = new Sub<T>(handler, owner, original);
                list.Add(sub);

                if (!_boxedSubs.TryGetValue(evtType, out var boxed))
                {
                    boxed = new List<object>(4);
                    _boxedSubs[evtType] = boxed;
                }
                boxed.Add(sub);

                return new Unsub(() =>
                {
                    if (_subs.TryGetValue(evtType, out var l))
                    {
                        l.Remove(sub);
                        if (l.Count == 0) _subs.Remove(evtType);
                    }
                    if (_boxedSubs.TryGetValue(evtType, out var b))
                    {
                        b.Remove(sub);
                        if (b.Count == 0) _boxedSubs.Remove(evtType);
                    }
                });
            }

            public static void Publish(Type evtType, T payload)
            {
                if (!_subs.TryGetValue(evtType, out var list) || list.Count == 0)
                {
                    if (Trace) Debug.Log($"[V] Broadcast {Label(evtType)} (0 listeners)");
                    return;
                }

                int invoked = 0;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var s = list[i];
                    if (!s.IsAlive())
                    {
                        list.RemoveAt(i);
                        continue;
                    }

                    try
                    {
                        s.Handler(payload);
                        invoked++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                if (Trace)
                    Debug.Log($"[V] Broadcast {Label(evtType)} ({invoked} listener{(invoked == 1 ? "" : "s")})");

                if (list.Count == 0)
                    _subs.Remove(evtType);
            }
        }

        private static class Bus2<TTuple>
        {
            private static readonly Dictionary<Type, List<Sub<TTuple>>> _subs = new();
            private static readonly HashSet<Type> _warnedGlobal = new();
            private static readonly Dictionary<OwnerEventKey, FrameCounter> _addsPerOwnerPerEvent = new();
            private static readonly Dictionary<Type, List<object>> _boxedSubs = new();

            public static void Subscribe(Type evtType, Action<TTuple> handler, UnityEngine.Object owner, Delegate original)
            {
                GuardrailChecks(evtType, owner, original, _boxedSubs, _addsPerOwnerPerEvent, _warnedGlobal);

                if (!_subs.TryGetValue(evtType, out var list))
                {
                    list = new List<Sub<TTuple>>(4);
                    _subs[evtType] = list;
                }
                var sub = new Sub<TTuple>(handler, owner, original);
                list.Add(sub);

                if (!_boxedSubs.TryGetValue(evtType, out var boxed))
                {
                    boxed = new List<object>(4);
                    _boxedSubs[evtType] = boxed;
                }
                boxed.Add(sub);
            }

            public static IDisposable SubscribeDisposable(Type evtType, Action<TTuple> handler, UnityEngine.Object owner, Delegate original)
            {
                GuardrailChecks(evtType, owner, original, _boxedSubs, _addsPerOwnerPerEvent, _warnedGlobal);

                if (!_subs.TryGetValue(evtType, out var list))
                {
                    list = new List<Sub<TTuple>>(4);
                    _subs[evtType] = list;
                }
                var sub = new Sub<TTuple>(handler, owner, original);
                list.Add(sub);

                if (!_boxedSubs.TryGetValue(evtType, out var boxed))
                {
                    boxed = new List<object>(4);
                    _boxedSubs[evtType] = boxed;
                }
                boxed.Add(sub);

                return new Unsub(() =>
                {
                    if (_subs.TryGetValue(evtType, out var l))
                    {
                        l.Remove(sub);
                        if (l.Count == 0) _subs.Remove(evtType);
                    }
                    if (_boxedSubs.TryGetValue(evtType, out var b))
                    {
                        b.Remove(sub);
                        if (b.Count == 0) _boxedSubs.Remove(evtType);
                    }
                });
            }

            public static void Publish(Type evtType, TTuple payload)
            {
                if (!_subs.TryGetValue(evtType, out var list) || list.Count == 0)
                {
                    if (Trace) Debug.Log($"[V] Broadcast {Label(evtType)} (0 listeners)");
                    return;
                }

                int invoked = 0;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var s = list[i];
                    if (!s.IsAlive())
                    {
                        list.RemoveAt(i);
                        continue;
                    }

                    try
                    {
                        s.Handler(payload);
                        invoked++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                if (Trace)
                    Debug.Log($"[V] Broadcast {Label(evtType)} ({invoked} listener{(invoked == 1 ? "" : "s")})");

                if (list.Count == 0)
                    _subs.Remove(evtType);
            }
        }

        private static class Bus3<TTuple>
        {
            private static readonly Dictionary<Type, List<Sub<TTuple>>> _subs = new();
            private static readonly HashSet<Type> _warnedGlobal = new();
            private static readonly Dictionary<OwnerEventKey, FrameCounter> _addsPerOwnerPerEvent = new();
            private static readonly Dictionary<Type, List<object>> _boxedSubs = new();

            public static void Subscribe(Type evtType, Action<TTuple> handler, UnityEngine.Object owner, Delegate original)
            {
                GuardrailChecks(evtType, owner, original, _boxedSubs, _addsPerOwnerPerEvent, _warnedGlobal);

                if (!_subs.TryGetValue(evtType, out var list))
                {
                    list = new List<Sub<TTuple>>(4);
                    _subs[evtType] = list;
                }
                var sub = new Sub<TTuple>(handler, owner, original);
                list.Add(sub);

                if (!_boxedSubs.TryGetValue(evtType, out var boxed))
                {
                    boxed = new List<object>(4);
                    _boxedSubs[evtType] = boxed;
                }
                boxed.Add(sub);
            }

            public static IDisposable SubscribeDisposable(Type evtType, Action<TTuple> handler, UnityEngine.Object owner, Delegate original)
            {
                GuardrailChecks(evtType, owner, original, _boxedSubs, _addsPerOwnerPerEvent, _warnedGlobal);

                if (!_subs.TryGetValue(evtType, out var list))
                {
                    list = new List<Sub<TTuple>>(4);
                    _subs[evtType] = list;
                }
                var sub = new Sub<TTuple>(handler, owner, original);
                list.Add(sub);

                if (!_boxedSubs.TryGetValue(evtType, out var boxed))
                {
                    boxed = new List<object>(4);
                    _boxedSubs[evtType] = boxed;
                }
                boxed.Add(sub);

                return new Unsub(() =>
                {
                    if (_subs.TryGetValue(evtType, out var l))
                    {
                        l.Remove(sub);
                        if (l.Count == 0) _subs.Remove(evtType);
                    }
                    if (_boxedSubs.TryGetValue(evtType, out var b))
                    {
                        b.Remove(sub);
                        if (b.Count == 0) _boxedSubs.Remove(evtType);
                    }
                });
            }

            public static void Publish(Type evtType, TTuple payload)
            {
                if (!_subs.TryGetValue(evtType, out var list) || list.Count == 0)
                {
                    if (Trace) Debug.Log($"[V] Broadcast {Label(evtType)} (0 listeners)");
                    return;
                }

                int invoked = 0;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var s = list[i];
                    if (!s.IsAlive())
                    {
                        list.RemoveAt(i);
                        continue;
                    }

                    try
                    {
                        s.Handler(payload);
                        invoked++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                if (Trace)
                    Debug.Log($"[V] Broadcast {Label(evtType)} ({invoked} listener{(invoked == 1 ? "" : "s")})");

                if (list.Count == 0)
                    _subs.Remove(evtType);
            }
        }
    }
}
