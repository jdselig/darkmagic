using System;

namespace DarkMagic
{
    /// <summary>
    /// Convenience wrappers for state-change events via V.
    /// </summary>
    public static class StateExtensions
    {
        public static void OnStateChanged<TGroup>(this UnityEngine.Object self, Action<V.StateChange> handler)
            => V.On<V.StateChanged<TGroup>, V.StateChange>(handler, self);

        public static void OnceStateChanged<TGroup>(this UnityEngine.Object self, Action<V.StateChange> handler)
            => V.Once<V.StateChanged<TGroup>, V.StateChange>(handler, self);
    }
}
