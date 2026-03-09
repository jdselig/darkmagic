using System;
using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// Flavor C: convenience extension methods.
    /// They bind listener lifetime to the UnityEngine.Object instance ("this") automatically.
    /// </summary>
    public static class VExtensions
    {
        // ---------- LISTEN / REACT ----------

        public static void On<TEvent>(this UnityEngine.Object self, Action handler)
            where TEvent : V.Event
            => V.On<TEvent>(handler, self);


        public static void On<TEvent>(this UnityEngine.Object self, Delegate handler)
            where TEvent : V.Event
            => V.On<TEvent>(handler, self);


        public static void On<TEvent>(this UnityEngine.Object self, Action<object> handler)
            where TEvent : V.Event
            => V.On<TEvent>(handler, self);

        public static void On<TEvent>(this UnityEngine.Object self, Action<object, object> handler)
            where TEvent : V.Event
            => V.On<TEvent>(handler, self);

        public static void On<TEvent>(this UnityEngine.Object self, Action<object, object, object> handler)
            where TEvent : V.Event
            => V.On<TEvent>(handler, self);
        public static void On<TEvent, T>(this UnityEngine.Object self, Action<T> handler)
            where TEvent : V.Event<T>
            => V.On<TEvent, T>(handler, self);

        public static void On<TEvent, T1, T2>(this UnityEngine.Object self, Action<T1, T2> handler)
            where TEvent : V.Event<T1, T2>
            => V.On<TEvent, T1, T2>(handler, self);

        public static void On<TEvent, T1, T2, T3>(this UnityEngine.Object self, Action<T1, T2, T3> handler)
            where TEvent : V.Event<T1, T2, T3>
            => V.On<TEvent, T1, T2, T3>(handler, self);

        // Alias: React
        public static void React<TEvent>(this UnityEngine.Object self, Action handler)
            where TEvent : V.Event
            => V.On<TEvent>(handler, self);

        public static void React<TEvent, T>(this UnityEngine.Object self, Action<T> handler)
            where TEvent : V.Event<T>
            => V.On<TEvent, T>(handler, self);

        public static void React<TEvent, T1, T2>(this UnityEngine.Object self, Action<T1, T2> handler)
            where TEvent : V.Event<T1, T2>
            => V.On<TEvent, T1, T2>(handler, self);

        public static void React<TEvent, T1, T2, T3>(this UnityEngine.Object self, Action<T1, T2, T3> handler)
            where TEvent : V.Event<T1, T2, T3>
            => V.On<TEvent, T1, T2, T3>(handler, self);

        // ---------- ONCE ----------

        public static void Once<TEvent>(this UnityEngine.Object self, Action handler)
            where TEvent : V.Event
            => V.Once<TEvent>(handler, self);


        public static void Once<TEvent>(this UnityEngine.Object self, Delegate handler)
            where TEvent : V.Event
            => V.Once<TEvent>(handler, self);
        public static void Once<TEvent, T>(this UnityEngine.Object self, Action<T> handler)
            where TEvent : V.Event<T>
            => V.Once<TEvent, T>(handler, self);

        public static void Once<TEvent, T1, T2>(this UnityEngine.Object self, Action<T1, T2> handler)
            where TEvent : V.Event<T1, T2>
            => V.Once<TEvent, T1, T2>(handler, self);

        public static void Once<TEvent, T1, T2, T3>(this UnityEngine.Object self, Action<T1, T2, T3> handler)
            where TEvent : V.Event<T1, T2, T3>
            => V.Once<TEvent, T1, T2, T3>(handler, self);

        // Optional: disposable versions (thin wrappers)
        public static IDisposable OnDisposable<TEvent, T>(this UnityEngine.Object self, Action<T> handler)
            where TEvent : V.Event<T>
            => V.OnDisposable<TEvent, T>(handler, self);

        // ---------- BROADCAST / EMIT / YELL ----------

        public static void Broadcast<TEvent>(this UnityEngine.Object self)
            where TEvent : V.Event
            => V.Broadcast<TEvent>();

        public static void Broadcast<TEvent, T>(this UnityEngine.Object self, T payload)
            where TEvent : V.Event<T>
            => V.Broadcast<TEvent, T>(payload);

        public static void Broadcast<TEvent, T1, T2>(this UnityEngine.Object self, T1 a, T2 b)
            where TEvent : V.Event<T1, T2>
            => V.Broadcast<TEvent, T1, T2>(a, b);

        public static void Broadcast<TEvent, T1, T2, T3>(this UnityEngine.Object self, T1 a, T2 b, T3 c)
            where TEvent : V.Event<T1, T2, T3>
            => V.Broadcast<TEvent, T1, T2, T3>(a, b, c);

        // Alias: Emit
        public static void Emit<TEvent>(this UnityEngine.Object self) where TEvent : V.Event => V.Broadcast<TEvent>();
        public static void Emit<TEvent, T>(this UnityEngine.Object self, T payload) where TEvent : V.Event<T> => V.Broadcast<TEvent, T>(payload);
        public static void Emit<TEvent, T1, T2>(this UnityEngine.Object self, T1 a, T2 b) where TEvent : V.Event<T1, T2> => V.Broadcast<TEvent, T1, T2>(a, b);
        public static void Emit<TEvent, T1, T2, T3>(this UnityEngine.Object self, T1 a, T2 b, T3 c) where TEvent : V.Event<T1, T2, T3> => V.Broadcast<TEvent, T1, T2, T3>(a, b, c);

        // Alias: Yell 😄
        public static void Yell<TEvent>(this UnityEngine.Object self) where TEvent : V.Event => V.Broadcast<TEvent>();
        public static void Yell<TEvent, T>(this UnityEngine.Object self, T payload) where TEvent : V.Event<T> => V.Broadcast<TEvent, T>(payload);
        public static void Yell<TEvent, T1, T2>(this UnityEngine.Object self, T1 a, T2 b) where TEvent : V.Event<T1, T2> => V.Broadcast<TEvent, T1, T2>(a, b);
        public static void Yell<TEvent, T1, T2, T3>(this UnityEngine.Object self, T1 a, T2 b, T3 c) where TEvent : V.Event<T1, T2, T3> => V.Broadcast<TEvent, T1, T2, T3>(a, b, c);

        // Student-friendly: payload listener shorthand (no extra generic args)


    }
}
