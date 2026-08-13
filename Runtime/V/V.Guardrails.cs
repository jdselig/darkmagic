using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkMagic
{
    public static partial class V
    {
        private static bool ShouldWarn =>
#if UNITY_EDITOR
            Guardrails;
#else
            Guardrails && Debug.isDebugBuild;
#endif

        private struct OwnerEventKey : IEquatable<OwnerEventKey>
        {
            public Type EventType;
            public int OwnerId;

            public bool Equals(OwnerEventKey other) =>
                EventType == other.EventType && OwnerId == other.OwnerId;

            public override bool Equals(object obj) =>
                obj is OwnerEventKey other && Equals(other);

            public override int GetHashCode() =>
                ((EventType != null ? EventType.GetHashCode() : 0) * 397) ^ OwnerId;
        }

        private struct FrameCounter
        {
            public int Frame;
            public int AddedThisFrame;
        }

        private static void GuardrailChecks(
            Type eventType,
            UnityEngine.Object owner,
            Delegate originalDelegate,
            Dictionary<Type, List<object>> subscriptions,
            Dictionary<OwnerEventKey, FrameCounter> additions,
            HashSet<Type> warnedGlobal
        )
        {
            if (!ShouldWarn) return;

            var label = Label(eventType);
            if (owner == null)
            {
                if (warnedGlobal.Add(eventType))
                {
                    Debug.LogWarning(
                        $"[V] Listener added without an owner for {label}.\n"
                            + "This listener may live forever. In MonoBehaviours, prefer: this.On / this.Once."
                    );
                }
                return;
            }

            var ownerId = GetOwnerKey(owner);
            var key = new OwnerEventKey { EventType = eventType, OwnerId = ownerId };
            var frame = Time.frameCount;
            additions.TryGetValue(key, out var count);

            if (count.Frame != frame)
            {
                count.Frame = frame;
                count.AddedThisFrame = 0;
            }

            count.AddedThisFrame++;
            additions[key] = count;

            if (count.AddedThisFrame == 3)
            {
                Debug.LogWarning(
                    $"[V] {owner.name} subscribed to {label} multiple times in the same frame.\n"
                        + "Did you call this.On(...) inside Update()? Subscribe once in Awake/Start/OnEnable."
                );
            }

            if (!subscriptions.TryGetValue(eventType, out var entries) || entries.Count == 0)
                return;

            var sameOwnerExists = false;
            var exactMethodMatch = false;

            for (var index = entries.Count - 1; index >= 0; index--)
            {
                var entry = entries[index];
                var ownerField = entry.GetType().GetField("Owner");
                var originalField = entry.GetType().GetField("Original");
                if (ownerField == null || originalField == null) continue;

                var ownerReference = ownerField.GetValue(entry)
                    as WeakReference<UnityEngine.Object>;
                if (ownerReference == null) continue;
                if (!ownerReference.TryGetTarget(out var existingOwner) || existingOwner == null)
                    continue;
                if (GetOwnerKey(existingOwner) != ownerId) continue;

                sameOwnerExists = true;
                var existingOriginal = originalField.GetValue(entry) as Delegate;
                if (
                    existingOriginal != null
                    && originalDelegate != null
                    && existingOriginal.Method == originalDelegate.Method
                )
                {
                    exactMethodMatch = true;
                    break;
                }
            }

            if (exactMethodMatch)
            {
                Debug.LogWarning(
                    $"[V] Possible duplicate subscription: {owner.name} subscribed again to {label} using the same method.\n"
                        + "If something is firing twice, check that you're not subscribing multiple times (Awake/OnEnable/Start)."
                );
            }
            else if (sameOwnerExists && count.AddedThisFrame == 1)
            {
                Debug.LogWarning(
                    $"[V] {owner.name} subscribed again to {label}.\n"
                        + "If something is firing twice, check where subscriptions are created. Prefer subscribing once (Awake/Start/OnEnable)."
                );
            }
        }
    }
}
