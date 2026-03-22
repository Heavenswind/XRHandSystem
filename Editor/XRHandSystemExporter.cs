using UnityEditor;
using UnityEngine;
using System.IO;

namespace XRHandSystem.Editor
{
    public static class XRHandSystemExporter
    {
        private const string ExportRoot = "Packages/com.xrhandsystem.core";

        [MenuItem("XRHandSystem/Export .unitypackage")]
        public static void Export()
        {
            string savePath = EditorUtility.SaveFilePanel(
                "Export XRHandSystem",
                "",
                "XRHandSystem",
                "unitypackage");

            if (string.IsNullOrEmpty(savePath)) return;

            // Collect all assets under the package
            string[] assetPaths = AssetDatabase.FindAssets("", new[] { ExportRoot })
                is { Length: > 0 } guids
                ? System.Array.ConvertAll(guids, AssetDatabase.GUIDToAssetPath)
                : null;

            if (assetPaths == null || assetPaths.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Export Failed",
                    $"No assets found at {ExportRoot}.\n\nMake sure the package is installed via Package Manager from disk first.",
                    "OK");
                return;
            }

            AssetDatabase.ExportPackage(
                assetPaths,
                savePath,
                ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

            Debug.Log($"[XRHandSystem] Exported to {savePath}");
            EditorUtility.RevealInFinder(savePath);
        }
    }
}
