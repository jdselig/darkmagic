#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// Helps projects import Unity's official TMP shader resources when U needs them.
    /// </summary>
    [InitializeOnLoad]
    internal static class DarkMagicTMPSetup
    {
        private const string SetupMenuPath = "Tools/DarkMagic/Setup UI";
        private const string ImportMenuPath =
            "Window/TextMeshPro/Import TMP Essential Resources";
        private const string ProjectSettingsPath =
            "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private static bool _isImporting;
        private static byte[] _settingsBackup;

        static DarkMagicTMPSetup()
        {
            EditorApplication.delayCall += OfferSetupIfNeeded;
        }

        [MenuItem(SetupMenuPath)]
        public static void SetupUI()
        {
            if (TryGetFontIssue(out _))
            {
                ImportEssentialResources();
                return;
            }

            if (Application.isBatchMode)
            {
                Debug.Log("[DarkMagic/U] The selected TMP font is ready.");
                EditorApplication.Exit(0);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "DarkMagic UI is ready",
                    "The selected TMP font has a working material and shader.",
                    "OK"
                );
            }
        }

        private static void OfferSetupIfNeeded()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OfferSetupIfNeeded;
                return;
            }

            if (!TryGetFontIssue(out var issue))
                return;

            var sessionKey =
                "Archenemy.DarkMagic.TMPSetup.Shown."
                + Hash128.Compute(Application.dataPath).ToString();
            if (SessionState.GetBool(sessionKey, false))
                return;

            SessionState.SetBool(sessionKey, true);
            var importNow = EditorUtility.DisplayDialog(
                "Set up DarkMagic UI",
                "DarkMagic uses Unity's official TextMesh Pro shaders. "
                    + "This project is missing a working shader for its selected font.\n\n"
                    + issue
                    + "\n\nImport TMP Essential Resources now? Unity will add them under Assets/TextMesh Pro.",
                "Import Now",
                "Later"
            );

            if (importNow)
                ImportEssentialResources();
            else
                Debug.LogWarning(BuildHelpMessage(issue));
        }

        private static void ImportEssentialResources()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(TMP_Text).Assembly
            );
            var importPath =
                package != null
                    ? Path.Combine(
                        package.resolvedPath,
                        "Package Resources",
                        "TMP Essential Resources.unitypackage"
                    )
                    : null;

            if (!string.IsNullOrEmpty(importPath) && File.Exists(importPath))
            {
                if (_isImporting)
                    return;

                _isImporting = true;
                AssetDatabase.importPackageCompleted += OnImportCompleted;
                AssetDatabase.importPackageCancelled += OnImportCancelled;
                AssetDatabase.importPackageFailed += OnImportFailed;

                if (File.Exists(ProjectSettingsPath))
                {
                    AssetDatabase.SaveAssets();
                    _settingsBackup = File.ReadAllBytes(ProjectSettingsPath);
                }

                AssetDatabase.ImportPackage(importPath, false);
                return;
            }

            if (EditorApplication.ExecuteMenuItem(ImportMenuPath))
                return;

            if (Application.isBatchMode)
                throw new InvalidOperationException(
                    "DarkMagic could not find Unity's TMP Essential Resources package."
                );

            EditorUtility.DisplayDialog(
                "TMP import command not found",
                "Open Window > TextMeshPro > Import TMP Essential Resources, then run "
                    + SetupMenuPath
                    + " again.",
                "OK"
            );
        }

        private static void OnImportCompleted(string packageName)
        {
            FinishImport();
            AssetDatabase.Refresh();
            Debug.Log("[DarkMagic/U] Imported Unity's TMP Essential Resources.");

            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        private static void OnImportCancelled(string packageName)
        {
            FinishImport();
            Debug.LogWarning("[DarkMagic/U] TMP Essential Resources import was cancelled.");

            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }

        private static void OnImportFailed(string packageName, string errorMessage)
        {
            FinishImport();
            Debug.LogError("[DarkMagic/U] TMP Essential Resources import failed: " + errorMessage);

            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }

        private static void FinishImport()
        {
            if (_settingsBackup != null)
            {
                File.WriteAllBytes(ProjectSettingsPath, _settingsBackup);
                _settingsBackup = null;
                AssetDatabase.Refresh();
            }

            _isImporting = false;
            AssetDatabase.importPackageCompleted -= OnImportCompleted;
            AssetDatabase.importPackageCancelled -= OnImportCancelled;
            AssetDatabase.importPackageFailed -= OnImportFailed;
        }

        internal static bool TryGetFontIssue(out string issue)
        {
            UConfig.ApplyUserOverrides();
            UConfig.ApplyStylePreset();

            var font =
                UConfig.FontAsset
                ?? UConfig.Font
                ?? TMP_Settings.defaultFontAsset;

            if (font == null)
            {
                issue = "No TMP font asset is available.";
                return true;
            }

            var material = font.material;
            if (material == null)
            {
                issue = $"The font '{font.name}' has no material.";
                return true;
            }

            var shader = material.shader;
            if (shader == null)
            {
                issue = $"The font '{font.name}' has no shader.";
                return true;
            }

            if (shader.name.IndexOf("InternalErrorShader", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                issue = $"The font '{font.name}' is using Unity's error shader.";
                return true;
            }

            if (!Application.isBatchMode && !shader.isSupported)
            {
                issue = $"The shader '{shader.name}' is not supported on this editor.";
                return true;
            }

            issue = null;
            return false;
        }

        internal static string BuildHelpMessage(string issue)
        {
            return "[DarkMagic/U] "
                + issue
                + " Choose Tools > DarkMagic > Setup UI and import TMP Essential Resources, "
                + "or assign a working TMP font to UConfig.FontAsset.";
        }
    }

    internal sealed class DarkMagicTMPBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (DarkMagicTMPSetup.TryGetFontIssue(out var issue))
                throw new BuildFailedException(DarkMagicTMPSetup.BuildHelpMessage(issue));
        }
    }
}
#endif
