using OneBlock.Services;
using UnityEditor;
using UnityEngine;

namespace OneBlock.EditorTools
{
    /// <summary>Creates the AdsConfig asset and copies its AdMob app id into the Google Mobile Ads settings.</summary>
    public static class AdsSetup
    {
        public const string ConfigPath = ProjectSetup.DataDir + "/AdsConfig.asset";
        const string GmaSettingsPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";

        public static AdsConfig EnsureConfig()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<AdsConfig>(ConfigPath);
            if (cfg != null) return cfg;
            cfg = ScriptableObject.CreateInstance<AdsConfig>();
            AssetDatabase.CreateAsset(cfg, ConfigPath);
            AssetDatabase.SaveAssets();
            return cfg;
        }

        [MenuItem("One Block/Setup/Apply Ads Settings", priority = 23)]
        public static void Apply()
        {
            var cfg = EnsureConfig();
            string appId = string.IsNullOrWhiteSpace(cfg.androidAppId) || cfg.useTestAds ? AdsConfig.TestAppId : cfg.androidAppId.Trim();

            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(GmaSettingsPath);
            if (settings == null)
            {
                // The settings type is internal to the plugin; create it by name
                settings = ScriptableObject.CreateInstance("GoogleMobileAdsSettings");
                if (settings == null)
                {
                    Debug.LogWarning("[One Block] Google Mobile Ads package not installed; skipping AdMob settings.");
                    return;
                }
                System.IO.Directory.CreateDirectory("Assets/GoogleMobileAds/Resources");
                AssetDatabase.CreateAsset(settings, GmaSettingsPath);
            }

            var so = new SerializedObject(settings);
            var prop = so.FindProperty("adMobAndroidAppId");
            if (prop == null)
            {
                Debug.LogWarning("[One Block] GoogleMobileAdsSettings has no adMobAndroidAppId field; set the app id in Assets > Google Mobile Ads > Settings.");
                return;
            }
            prop.stringValue = appId;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[One Block] AdMob Android app id set to {appId}{(cfg.useTestAds ? " (TEST)" : "")}.");
        }
    }
}
