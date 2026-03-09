using System;

namespace DarkMagic
{
    /// <summary>
    /// Optional V events emitted by W.Run.
    /// </summary>
    public static partial class V
    {
        public readonly struct WInfo
        {
            public readonly UnityEngine.Object Owner;
            public readonly string Name;
            public readonly float Seconds;
            public readonly Exception Exception;

            public WInfo(UnityEngine.Object owner, string name, float seconds, Exception exception)
            {
                Owner = owner;
                Name = name;
                Seconds = seconds;
                Exception = exception;
            }

            public override string ToString()
                => $"{Name} ({Owner?.name ?? "(no owner)"}) {Seconds:0.###}s";
        }

        public sealed class WStarted : Event<WInfo> { }
        public sealed class WFinished : Event<WInfo> { }
        public sealed class WCanceled : Event<WInfo> { }
        public sealed class WFailed : Event<WInfo> { }
    }
}
