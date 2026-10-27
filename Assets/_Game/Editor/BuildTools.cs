using System.Collections.Generic;
using System.IO;
using OneBlock.EditorTools.Art;
using OneBlock.Services;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OneBlock.EditorTools
{
    /// <summary>
    /// Store builds: release player settings, app icons, a test APK and the signed release App Bundle,
    /// with pre-flight checks so nothing embarrassing ships (test ads, debug signing, dev builds).
    /// </summary>
    public static class BuildTools
    {
        public const string BuildsDir = "Builds";
        const string IconDir = "Assets/_Game/Art/Generated/" + IconArt.Dir;

        // ------------------------------------------------------------------ settings

        [MenuItem("One Block/Build/Apply Release Settings", priority = 40)]
        public static void ApplyReleaseSettings()
        {
            PlayerSettingsSetup.Apply();
            AdsSetup.Apply();
            ApplyIcons();

            var android = NamedBuildTarget.Android;
            PlayerSettings.productName = "One Block";
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            // 32-bit included so older phones can install; Play ships each device only its own slice
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;   // highest installed
            PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.Low);          // ad SDKs rely on reflection
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.Android.optimizedFramePacing = true;
            PlayerSettings.SetIl2CppCompilerConfiguration(android, Il2CppCompilerConfiguration.Release);   // Master only for store builds
            PlayerSettings.muteOtherAudioSources = false;
            PlayerSettings.runInBackground = false;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1.0") PlayerSettings.bundleVersion = "1.0.0";
            AssetDatabase.SaveAssets();
            Debug.Log($"[One Block] Release settings applied. Version {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode}).");
        }

        [MenuItem("One Block/Build/Generate App Icon", priority = 41)]
        public static void GenerateIcon()
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                IconArt.Generate();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ApplyIcons();
        }

        public static void ApplyIcons()
        {
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>($"{IconDir}/icon_background.png");
            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>($"{IconDir}/icon_foreground.png");
            var full = AssetDatabase.LoadAssetAtPath<Texture2D>($"{IconDir}/icon_full.png");
            if (background == null || foreground == null || full == null)
            {
                Debug.LogWarning("[One Block] Icon textures missing: run One Block/Build/Generate App Icon.");
                return;
            }

            var android = NamedBuildTarget.Android;
            var adaptive = PlayerSettings.GetPlatformIcons(android, AndroidPlatformIconKind.Adaptive);
            foreach (var icon in adaptive)
            {
                icon.SetTexture(background, 0);
                icon.SetTexture(foreground, 1);
            }
            PlayerSettings.SetPlatformIcons(android, AndroidPlatformIconKind.Adaptive, adaptive);

            foreach (var kind in new[] { AndroidPlatformIconKind.Round, AndroidPlatformIconKind.Legacy })
            {
                var icons = PlayerSettings.GetPlatformIcons(android, kind);
                foreach (var icon in icons) icon.SetTexture(full);
                PlayerSettings.SetPlatformIcons(android, kind, icons);
            }
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { full }, IconKind.Any);
            Debug.Log("[One Block] App icons assigned.");
        }

        // ------------------------------------------------------------------ builds

        [MenuItem("One Block/Build/Android Test APK (install on your phone)", priority = 60)]
        public static void BuildTestApkMenu() => BuildTestApk();

        public static BuildReport BuildTestApk()
        {
            // Phone test builds always use Google's test ads: tapping your own live ads can get the AdMob account banned
            var ads = AdsSetup.EnsureConfig();
            bool wasTest = ads.useTestAds;
            ads.useTestAds = true;
            EditorUtility.SetDirty(ads);
            try
            {
                ApplyReleaseSettings();
                EditorUserBuildSettings.buildAppBundle = false;
                string path = Path.Combine(BuildsDir, $"OneBlock-{PlayerSettings.bundleVersion}-test.apk");
                return Build(path);
            }
            finally
            {
                ads.useTestAds = wasTest;
                EditorUtility.SetDirty(ads);
                AssetDatabase.SaveAssets();
                AdsSetup.Apply();
            }
        }

        [MenuItem("One Block/Build/Android Release App Bundle (.aab)", priority = 61)]
        public static void BuildReleaseMenu()
        {
            var problems = PreflightRelease();
            if (problems.Count > 0)
            {
                string message = "Before uploading to Google Play:\n\n• " + string.Join("\n• ", problems);
                bool blocking = problems.Exists(p => p.StartsWith("BLOCKING"));
                if (blocking)
                {
                    EditorUtility.DisplayDialog("One Block release", message, "OK");
                    return;
                }
                if (!EditorUtility.DisplayDialog("One Block release", message + "\n\nBuild anyway?", "Build", "Cancel")) return;
            }
            BuildRelease();
        }

        public static BuildReport BuildRelease()
        {
            ApplyReleaseSettings();
            PlayerSettings.Android.bundleVersionCode += 1;   // every Play upload needs a higher version code
            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Master);
            string path = Path.Combine(BuildsDir, $"OneBlock-{PlayerSettings.bundleVersion}-{PlayerSettings.Android.bundleVersionCode}.aab");
            try
            {
                return Build(path);
            }
            finally
            {
                PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
            }
        }

        /// <summary>Things that must (BLOCKING) or should be fixed before a Play Store upload.</summary>
        public static List<string> PreflightRelease()
        {
            var problems = new List<string>();
            bool keystore = PlayerSettings.Android.useCustomKeystore && File.Exists(PlayerSettings.Android.keystoreName);
            if (!keystore)
                problems.Add("BLOCKING: no release keystore. Project Settings > Player > Android > Publishing Settings > Keystore Manager > Create New.");
            else if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
                problems.Add("BLOCKING: keystore passwords are empty for this Unity session. Re-enter them in Publishing Settings.");

            var ads = AdsSetup.EnsureConfig();
            if (ads.useTestAds) problems.Add("AdsConfig still uses TEST ads (fine for internal testing, no revenue in production).");
            if (ads.androidAppId == AdsConfig.TestAppId) problems.Add("AdsConfig has Google's test App ID. Paste your own AdMob App ID.");
            if (PlayerSettings.bundleVersion == "0.1.0") problems.Add("Version is still 0.1.0.");
            return problems;
        }

        static BuildReport Build(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.GameScenePath },
                locationPathName = path,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            string sizeMb = File.Exists(path) ? (new FileInfo(path).Length / 1048576f).ToString("0.0") : "?";
            Debug.Log($"[One Block] Build {s.result}: {path} ({sizeMb} MB) in {s.totalTime.TotalMinutes:0.0} min, {s.totalErrors} errors, {s.totalWarnings} warnings.");
            File.WriteAllText(Path.Combine(BuildsDir, "last_build.txt"),
                $"{s.result}\n{path}\n{sizeMb} MB\n{s.totalTime.TotalMinutes:0.0} min\nerrors {s.totalErrors}\nwarnings {s.totalWarnings}\n");
            return report;
        }
    }
}
