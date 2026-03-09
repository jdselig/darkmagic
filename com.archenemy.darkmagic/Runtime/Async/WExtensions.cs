using System;
using System.Threading;
using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// Student-first async helpers.
    /// Two styles:
    /// 1) Scoped: await this.W().Seconds(1);
    /// 2) Direct: await this.Seconds(1);
    /// </summary>
    public static class WExtensions
    {
        // ----- Scope -----
        public static DarkMagic.W.Scope W(this UnityEngine.Object self) => DarkMagic.W.With(self);

        // ----- Direct wait helpers -----
        public static Awaitable NextFrame(this UnityEngine.Object self) => Awaitable.NextFrameAsync(DarkMagic.W.TokenFor(self));
        public static Awaitable EndOfFrame(this UnityEngine.Object self) => Awaitable.EndOfFrameAsync(DarkMagic.W.TokenFor(self));
        public static Awaitable FixedUpdate(this UnityEngine.Object self) => Awaitable.FixedUpdateAsync(DarkMagic.W.TokenFor(self));
        public static Awaitable Seconds(this UnityEngine.Object self, float seconds) => Awaitable.WaitForSecondsAsync(seconds, DarkMagic.W.TokenFor(self));

        public static async Awaitable Until(this UnityEngine.Object self, Func<bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var token = DarkMagic.W.TokenFor(self);
            while (!predicate())
                await Awaitable.NextFrameAsync(token);
        }

        public static async Awaitable While(this UnityEngine.Object self, Func<bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var token = DarkMagic.W.TokenFor(self);
            while (predicate())
                await Awaitable.NextFrameAsync(token);
        }

        // ----- Direct runners -----
        public static void Run(this UnityEngine.Object self, Func<Awaitable> routine, string name = null)
            => DarkMagic.W.Run(self, _ => routine(), name);

        public static void Run(this UnityEngine.Object self, Func<CancellationToken, Awaitable> routine, string name = null)
            => DarkMagic.W.Run(self, routine, name);
    }
}
