#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DarkMagic
{
    [InitializeOnLoad]
    public static class TMPAutoCheck
    {
        private const string PrefKey = "Archenemy.V.TMPChecked";

        static TMPAutoCheck()
        {
            // Run once per project to avoid spamming.
            if (EditorPrefs.GetBool(PrefKey, false)) return;
            EditorPrefs.SetBool(PrefKey, true);

            // If TMP isn't installed, do nothing.
            var tmpType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
            if (tmpType == null) return;

            // If TMP essential resources are missing, Unity will prompt later anyway.
            // We proactively nudge students with a clear message (and open the menu once).
            var settings = Resources.Load("TMP Settings");
            if (settings == null)
            {
                Debug.LogWarning("[U] TextMeshPro Essentials not imported yet. Go to: Window > TextMeshPro > Import TMP Essential Resources. (U uses TMP for all text.)");

                // Attempt to open the import menu (safe even if it fails).
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
                    }
                    catch { /* ignore */ }
                };
            }
        }
    }
    #endif
}
