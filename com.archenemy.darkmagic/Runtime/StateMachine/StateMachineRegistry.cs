using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// Internal registry mapping UnityEngine.Object -> StateMachine.
    /// Keeps machines per owner without requiring manual setup/teardown.
    /// </summary>
    public static class StateMachineRegistry
    {
        private static readonly Dictionary<int, Entry> _machines = new();

        private sealed class Entry
        {
            public readonly WeakReference<UnityEngine.Object> Owner;
            public readonly StateMachine Machine;

            public Entry(UnityEngine.Object owner)
            {
                Owner = new WeakReference<UnityEngine.Object>(owner);
                Machine = new StateMachine(owner);
            }

            public bool TryGetOwner(out UnityEngine.Object owner)
            {
                owner = null;
                return Owner.TryGetTarget(out owner) && owner != null;
            }
        }

        public static StateMachine For(UnityEngine.Object owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));

            int id = owner.GetInstanceID();

            if (_machines.TryGetValue(id, out var entry))
            {
                if (entry.TryGetOwner(out var stillAlive) && stillAlive == owner)
                    return entry.Machine;

                _machines.Remove(id);
            }

            CleanupSome();

            var e = new Entry(owner);
            _machines[id] = e;
            return e.Machine;
        }

        private static int _cleanupCursor = 0;

        private static void CleanupSome()
        {
            if (_machines.Count == 0) return;

            int toCheck = Math.Min(8, _machines.Count);
            var keys = new List<int>(_machines.Keys);

            for (int i = 0; i < toCheck; i++)
            {
                _cleanupCursor = (_cleanupCursor + 1) % keys.Count;
                int key = keys[_cleanupCursor];

                if (!_machines.TryGetValue(key, out var entry))
                    continue;

                if (!entry.TryGetOwner(out _))
                    _machines.Remove(key);
            }
        }
    }
}
