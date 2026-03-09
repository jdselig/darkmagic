using System.Collections.Generic;
using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// Tiny logging helpers. Styled, student-friendly.
    /// </summary>
    public static class Logs
    {
        // Colors (hex) for Unity rich text.
        private const string _blue = "#001f5b";     // navy
        private const string _orange = "#b45309";   // dark orange
        private const string _red = "#7f1d1d";      // dark red

        private static readonly HashSet<string> _once = new HashSet<string>();

        /// <summary>Bold navy log.</summary>
        public static void L(object message, Object context = null) => Log(message, context);

        /// <summary>Alias for L(). Bold navy.</summary>
        public static void Log(object message, Object context = null)
            => Styled(_blue, message, context);

        /// <summary>Bold dark-orange warning.</summary>
        public static void Warn(object message, Object context = null)
            => Styled(_orange, message, context);

        /// <summary>Bold dark-red error.</summary>
        public static void Error(object message, Object context = null)
            => Styled(_red, message, context, isError: true);

        /// <summary>
        /// Logs once per unique string key (message.ToString()).
        /// Prefixes with "SINGLE:".
        /// </summary>
        public static void LogOnce(object message, Object context = null)
        {
            var key = message?.ToString() ?? "null";
            if (_once.Contains(key)) return;
            _once.Add(key);
            Styled(_blue, "SINGLE: " + key, context);
        }

        /// <summary>Alias for LogOnce().</summary>
        public static void Single(object message, Object context = null) => LogOnce(message, context);

        private static void Styled(string hex, object message, Object context, bool isError = false)
        {
            var txt = message?.ToString() ?? "null";
            var formatted = $"<color={hex}><b>{txt}</b></color>";
            if (context != null)
            {
                if (isError) Debug.LogError(formatted, context);
                else Debug.Log(formatted, context);
            }
            else
            {
                if (isError) Debug.LogError(formatted);
                else Debug.Log(formatted);
            }
        }
    }
}
