using System;

namespace DarkMagic
{
    public static partial class U
    {
        public enum Placements
        {
            TopLeft,
            TopCenter,
            TopRight,

            MiddleLeft,
            MiddleCenter,
            MiddleRight,

            BottomLeft,
            BottomCenter,
            BottomRight
        }

        public enum Sizes
        {
            Inline,
            FullWidth,
            Modal,
            Default
        }

        public readonly struct Option
        {
            public readonly string Label;
            public readonly UnityEngine.Sprite Icon;

            public Option(string label, UnityEngine.Sprite icon = null)
            {
                Label = label;
                Icon = icon;
            }

            public static implicit operator Option(string label) => new Option(label);
        }

        public readonly struct Result<T>
        {
            public readonly bool Cancelled;
            public readonly T Value;

            public Result(T value, bool cancelled)
            {
                Value = value;
                Cancelled = cancelled;
            }

            public static Result<T> Ok(T value) => new Result<T>(value, false);
            public static Result<T> Canceled() => new Result<T>(default, true);
        }

        // V-events students can hook into for SFX etc.
        public class DialoguePopped : V.Event<string> { }
        public class ChoiceMade : V.Event<string> { }          // selected label
        public class ChoiceCanceled : V.Event<string> { }       // context/title
    }
}