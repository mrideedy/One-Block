using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace OneBlock.EditorTools
{
    /// <summary>Android player settings the game relies on. Safe to re-run.</summary>
    public static class PlayerSettingsSetup
    {
        [MenuItem("One Block/Setup/Apply Player Settings", priority = 21)]
        public static void Apply()
        {
            // No "Made with Unity" splash: the game plays its own intro (IntroScreen) on the first frame
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.31f, 0.64f, 0.93f);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.oneblock.game");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            AssetDatabase.SaveAssets();
            Debug.Log($"[One Block] Player settings applied. Splash shown: {PlayerSettings.SplashScreen.show}, Unity logo: {PlayerSettings.SplashScreen.showUnityLogo}");
        }
    }
}
