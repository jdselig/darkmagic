using System;
using System.Threading;
using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// W: "V-style async" sugar for Unity Awaitables.
    /// Goals:
    /// - student-first DX
    /// - sensible defaults (auto-cancel when MonoBehaviour is destroyed)
    /// - small surface area
    /// - optional trace + guardrails (Editor/Dev builds only)
    /// </summary>
    public static class W
    {
        /// <summary>Logs when W.Run starts/finishes/cancels/fails.</summary>
        public static bool Trace = false;

        /// <summary>Warnings that help beginners avoid common async footguns.</summary>
        public static bool Guardrails = true;

        private static bool AllowedNow =>
    #if UNITY_EDITOR
            true;
    #else
            Debug.isDebugBuild;
    #endif

        internal static bool TraceNow => Trace && AllowedNow;
        internal static bool WarnNow => Guardrails && AllowedNow;

        // ------------ Owner token helpers ------------

        public static CancellationToken TokenFor(UnityEngine.Object owner)
        {
            if (owner is MonoBehaviour mb)
                return mb.destroyCancellationToken;

            return Application.exitCancellationToken;
        }

        public readonly struct Scope
        {
            private readonly UnityEngine.Object _owner;
            internal Scope(UnityEngine.Object owner) => _owner = owner;

            public CancellationToken Token => TokenFor(_owner);

            // ----- Wait helpers -----

            public Awaitable NextFrame() => Awaitable.NextFrameAsync(Token);
            public Awaitable EndOfFrame() => Awaitable.EndOfFrameAsync(Token);
            public Awaitable FixedUpdate() => Awaitable.FixedUpdateAsync(Token);
            public Awaitable Seconds(float seconds) => Awaitable.WaitForSecondsAsync(seconds, Token);

            public async Awaitable Until(Func<bool> predicate)
            {
                if (predicate == null) throw new ArgumentNullException(nameof(predicate));
                while (!predicate())
                    await Awaitable.NextFrameAsync(Token);
            }

            public async Awaitable While(Func<bool> predicate)
            {
                if (predicate == null) throw new ArgumentNullException(nameof(predicate));
                while (predicate())
                    await Awaitable.NextFrameAsync(Token);
            }

            // ----- Runners -----

            public void Run(Func<Awaitable> routine, string name = null)
                => W.Run(_owner, _ => routine(), name);

            public void Run(Func<CancellationToken, Awaitable> routine, string name = null)
                => W.Run(_owner, routine, name);
        }

        /// <summary>
        /// Returns a W.Scope for clean calls like:
        ///   await this.W().Seconds(1);
        ///   this.W().Run(async () => { ... });
        /// </summary>
        public static Scope With(UnityEngine.Object owner) => new Scope(owner);

        // ------------ Fire-and-forget runners ------------

        public static void Run(UnityEngine.Object owner, Func<CancellationToken, Awaitable> routine, string name = null)
        {
            if (routine == null) throw new ArgumentNullException(nameof(routine));

            var token = TokenFor(owner);
            Guardrail_RunMaybeInUpdate(owner, name);

            _ = RunInternal(owner, routine, token, name);
        }

        private static async Awaitable RunInternal(UnityEngine.Object owner, Func<CancellationToken, Awaitable> routine, CancellationToken token, string name)
        {
            var label = string.IsNullOrWhiteSpace(name) ? (routine.Method?.Name ?? "Routine") : name;
            float t0 = Time.realtimeSinceStartup;

            if (TraceNow)
                Debug.Log($"[W] ▶ {label} ({OwnerName(owner)})");

            V.Broadcast<V.WStarted, V.WInfo>(new V.WInfo(owner, label, 0f, null));

            try
            {
                await routine(token);

                float dt = Time.realtimeSinceStartup - t0;

                if (TraceNow)
                    Debug.Log($"[W] ■ {label} ({OwnerName(owner)})  {dt:0.###}s");

                V.Broadcast<V.WFinished, V.WInfo>(new V.WInfo(owner, label, dt, null));
            }
            catch (OperationCanceledException)
            {
                float dt = Time.realtimeSinceStartup - t0;

                if (TraceNow)
                    Debug.Log($"[W] ⏹ {label} CANCELED ({OwnerName(owner)})  {dt:0.###}s");

                V.Broadcast<V.WCanceled, V.WInfo>(new V.WInfo(owner, label, dt, null));
            }
            catch (Exception ex)
            {
                float dt = Time.realtimeSinceStartup - t0;

                Debug.LogException(ex);

                V.Broadcast<V.WFailed, V.WInfo>(new V.WInfo(owner, label, dt, ex));
            }
        }

        private static string OwnerName(UnityEngine.Object owner)
        {
            if (owner == null) return "(no owner)";
            return owner.name;
        }

        // ------------ Guardrails ------------

        private struct RunKey : IEquatable<RunKey>
        {
            public int OwnerId;
            public string Name;

            public bool Equals(RunKey other) => OwnerId == other.OwnerId && Name == other.Name;
            public override bool Equals(object obj) => obj is RunKey other && Equals(other);
            public override int GetHashCode() => (OwnerId * 397) ^ (Name != null ? Name.GetHashCode() : 0);
        }

        private struct FrameCounter
        {
            public int Frame;
            public int Count;
        }

        private static readonly System.Collections.Generic.Dictionary<RunKey, FrameCounter> _runsPerFrame = new();

        private static void Guardrail_RunMaybeInUpdate(UnityEngine.Object owner, string name)
        {
            if (!WarnNow) return;
            if (owner == null) return;

            int id = owner.GetInstanceID();
            var key = new RunKey { OwnerId = id, Name = name ?? "" };

            int frame = Time.frameCount;
            _runsPerFrame.TryGetValue(key, out var fc);

            if (fc.Frame != frame)
            {
                fc.Frame = frame;
                fc.Count = 0;
            }

            fc.Count++;
            _runsPerFrame[key] = fc;

            if (fc.Count == 3)
            {
                Debug.LogWarning(
                    $"[W] {owner.name} started the same async routine multiple times in one frame.\n" +
                    "Did you call Run(...) inside Update()? Start async work once (Awake/Start/OnEnable) or gate it with a flag."
                );
            }
        }
    

        // -----------------------------
        // Combinators + sugar (moved from W)
        // -----------------------------

        /// <summary>Run an action after an awaitable completes.</summary>
        public static async Awaitable Then(this Awaitable awaitable, Action then)
        {
            await awaitable;
            then?.Invoke();
        }

        /// <summary>
        /// Wait for ALL awaitables to complete (sequentially awaits each).
        /// </summary>
        public static async Awaitable All(params Awaitable[] awaitables)
        {
            if (awaitables == null) throw new ArgumentNullException(nameof(awaitables));
            for (int i = 0; i < awaitables.Length; i++)
                await awaitables[i];
        }

        /// <summary>
        /// Wait for ANY awaitable to complete.
        /// Returns the index of the awaitable that completed first.
        /// </summary>
        public static async Awaitable<int> Any(params Awaitable[] awaitables)
        {
            if (awaitables == null) throw new ArgumentNullException(nameof(awaitables));
            if (awaitables.Length == 0) throw new ArgumentException("Need at least one awaitable.", nameof(awaitables));

            var done = new bool[awaitables.Length];
            var exs = new Exception[awaitables.Length];

            for (int i = 0; i < awaitables.Length; i++)
            {
                int idx = i;
                _ = Runner(idx, awaitables[idx], done, exs);
            }

            while (true)
            {
                for (int i = 0; i < done.Length; i++)
                {
                    if (!done[i]) continue;
                    if (exs[i] != null) throw exs[i];
                    return i;
                }
                await Awaitable.NextFrameAsync();
            }

            static async Awaitable Runner(int idx, Awaitable a, bool[] doneArr, Exception[] exArr)
            {
                try { await a; }
                catch (Exception ex) { exArr[idx] = ex; }
                finally { doneArr[idx] = true; }
            }
        }

        /// <summary>
        /// Returns true if the awaitable completes before the timeout. If timeout triggers, returns false and optionally calls onTimeout.
        /// </summary>
        public static async Awaitable<bool> Timeout(this Awaitable awaitable, UnityEngine.Object owner, float seconds, Action onTimeout = null)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(TokenFor(owner));
            var timeout = Awaitable.WaitForSecondsAsync(seconds, cts.Token);

            int winner = await Any(awaitable, timeout);

            if (winner == 0)
            {
                // awaitable finished first
                cts.Cancel();
                return true;
            }

            // timeout finished first
            onTimeout?.Invoke();
            return false;
        }

        /// <summary>
        /// Fire-and-forget helper that swallows OperationCanceledException and logs other exceptions.
        /// </summary>
        public static void Forget(this Awaitable awaitable, string label = null)
        {
            _ = ForgetRunner(awaitable, label);

            static async Awaitable ForgetRunner(Awaitable a, string lbl)
            {
                try { await a; }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    if (string.IsNullOrEmpty(lbl)) Debug.LogException(ex);
                    else Debug.LogException(new Exception($"[{lbl}] {ex.Message}", ex));
                }
            }
        }

        public static void Forget(this System.Threading.Tasks.Task task, string label = null)
        {
            _ = ForgetTask(task, label);

            static async System.Threading.Tasks.Task ForgetTask(System.Threading.Tasks.Task t, string lbl)
            {
                try { await t; }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    if (string.IsNullOrEmpty(lbl)) Debug.LogException(ex);
                    else Debug.LogException(new Exception($"[{lbl}] {ex.Message}", ex));
                }
            }
        }
}
}
