using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

namespace Riftbound.EditorTools
{
    /// <summary>
    /// Android build pipeline for the prototype.
    /// Menu: Riftbound > Build Android APK.
    /// Command line (Unity with Android Build Support installed):
    /// <code>
    /// Unity -batchmode -quit -projectPath . -buildTarget Android \
    ///       -executeMethod Riftbound.EditorTools.RiftboundBuild.BuildAndroidCommandLine -logFile build.log
    /// </code>
    /// Output: Builds/Android/RiftboundPrototype.apk (debug-signed, installable with adb or by opening the file).
    /// </summary>
    public static class RiftboundBuild
    {
        public const string ScenePath = "Assets/Riftbound/Scenes/Main.unity";
        public const string ApkPath = "Builds/Android/RiftboundPrototype.apk";
        public const string PackageId = "com.riftbound.prototype";

        [MenuItem("Riftbound/Apply Android Player Settings", priority = 20)]
        public static void ApplyAndroidSettings()
        {
            PlayerSettings.companyName = "Riftbound Prototype";
            PlayerSettings.productName = "Riftbound Prototype";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            // Portrait only.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Android 7.0+ (API 24), latest installed target SDK, 64-bit + 32-bit ARM with IL2CPP
            // (64-bit is mandatory on many recent phones, which is why Mono/ARMv7-only is not used).
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
#else
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageId);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
#endif
            // Plain APK (not an .aab) so it can be side-loaded directly.
            EditorUserBuildSettings.buildAppBundle = false;

            EnsureSceneInBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[Riftbound] Android player settings applied.");
        }

        public static void EnsureSceneInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Riftbound/Build Android APK", priority = 21)]
        public static void BuildAndroidMenu()
        {
            var ok = BuildAndroid(out string message);
            EditorUtility.DisplayDialog("Riftbound Android Build", message, "OK");
            if (ok) EditorUtility.RevealInFinder(ApkPath);
        }

        /// <summary>Entry point for -executeMethod. Exits with code 0 on success, 1 on failure.</summary>
        public static void BuildAndroidCommandLine()
        {
            bool ok;
            string message;
            try
            {
                ok = BuildAndroid(out message);
            }
            catch (Exception e)
            {
                ok = false;
                message = e.ToString();
            }
            Debug.Log("[Riftbound] " + message);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool BuildAndroid(out string message)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                message = "Android Build Support is not installed for this Unity version. Install it from Unity Hub > Installs > (version) > Add modules > Android Build Support (+ OpenJDK + Android SDK & NDK Tools).";
                Debug.LogError("[Riftbound] " + message);
                return false;
            }

            ApplyAndroidSettings();
            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded && File.Exists(ApkPath))
            {
                long size = new FileInfo(ApkPath).Length;
                message = $"APK built: {ApkPath} ({size / (1024f * 1024f):0.0} MB) in {summary.totalTime.TotalSeconds:0}s, {summary.totalWarnings} warnings.";
                Debug.Log("[Riftbound] " + message);
                return true;
            }

            message = $"Build {summary.result}: {summary.totalErrors} errors. See the Console / Editor.log for details.";
            Debug.LogError("[Riftbound] " + message);
            return false;
        }
    }
}
