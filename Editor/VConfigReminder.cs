#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// One-time editor reminder to import the Config sample.
    /// This never runs in builds.
    /// </summary>
    [InitializeOnLoad]
    internal static class DarkMagicConfigReminder
    {
        private const string PrefKey = "Archenemy.DarkMagic.ConfigReminder.Shown";

        static DarkMagicConfigReminder()
        {
            // Only show once per project (until user deletes EditorPrefs key).
            if (EditorPrefs.GetBool(PrefKey, false))
                return;

            // If any config already exists in Assets, don't nag.
            if (HasAnyConfigInAssets())
            {
                EditorPrefs.SetBool(PrefKey, true);
                return;
            }

            EditorPrefs.SetBool(PrefKey, true);

            Debug.Log(
                "<b>[DarkMagic]</b> Quick setup tip:\n" +
                "1) Window → Package Manager → DarkMagic → Samples → Import <b>Config</b>\n" +
                "2) Edit or move the imported config scripts under <b>Assets</b>.\n" +
                "DarkMagic also works without custom config, so import this only when you want overrides."
            );
        }

        private static bool HasAnyConfigInAssets()
        {
            // Avoid disk scans. AssetDatabase is much faster and respects Unity's indexing.
            // We look for a user-level config class the student would copy into Assets/Config.
            // Common: Assets/Config/UConfig.cs etc.
            if (AssetDatabase.FindAssets("t:Script UConfig").Length > 0) return true;
            if (AssetDatabase.FindAssets("t:Script VConfig").Length > 0) return true;
            if (AssetDatabase.FindAssets("t:Script SConfig").Length > 0) return true;
            if (AssetDatabase.FindAssets("t:Script WConfig").Length > 0) return true;
            if (AssetDatabase.FindAssets("t:Script IConfig").Length > 0) return true;
            if (AssetDatabase.FindAssets("t:Script StatConfig").Length > 0) return true;

            return false;
        }
    }
}
#endif
