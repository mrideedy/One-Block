using OneBlock.Core;
using UnityEditor;
using UnityEngine;

namespace OneBlock.EditorTools
{
    /// <summary>Paths and helpers for the project's core assets. Safe to re-run.</summary>
    public static class ProjectSetup
    {
        public const string DataDir = "Assets/_Game/Data";
        public const string ScenesDir = "Assets/_Game/Scenes";
        public const string ConfigPath = DataDir + "/GameConfig.asset";
        public const string GameScenePath = ScenesDir + "/Game.unity";

        [MenuItem("One Block/Setup/Create Core Assets", priority = 20)]
        public static GameConfig EnsureConfig()
        {
            EnsureFolder(DataDir);
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
                AssetDatabase.SaveAssets();
            }
            return config;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
