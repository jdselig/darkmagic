#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DarkMagic
{
    [InitializeOnLoad]
    public static class VWelcome
    {
        private const string PrefKey = "Archenemy.V.WelcomeShown";

        static VWelcome()
        {
            // Log only once per project.
            if (EditorPrefs.GetBool(PrefKey, false))
                return;

            EditorPrefs.SetBool(PrefKey, true);

            Debug.Log(
                "<color=#FFD36E><b>📣 V Event Bus installed</b></color>\n" +
                "To get started fast:\n" +
                "<b>Package Manager → V Event Bus → Samples → Import “VConfig”</b>\n" +
                "Then edit <b>VConfig.cs</b> to add events + toggle Trace."
            );
        }
    }
    #endif
}
