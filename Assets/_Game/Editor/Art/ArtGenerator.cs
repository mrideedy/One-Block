using System.IO;
using UnityEditor;
using UnityEngine;

namespace OneBlock.EditorTools.Art
{
    /// <summary>
    /// Menu entry that regenerates every sprite in the game from code.
    /// Output: Assets/_Game/Art/Generated/... (safe to delete and regenerate).
    /// </summary>
    public static class ArtGenerator
    {
        public const string Root = "Assets/_Game/Art/Generated";
        public const int PixelsPerUnit = 100;

        [MenuItem("One Block/Art/Generate All Art", priority = 0)]
        public static void GenerateAll()
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                CharacterArt.Generate();
                BlockArt.Generate();
                EnvironmentArt.Generate();
                PropArt.Generate();
                UIArt.Generate();
                FxArt.Generate();
                BundleArt.Generate();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ApplyPendingImportSettings();
            Debug.Log("[One Block] Art generated.");
        }

        // ---------------------------------------------------------------- saving

        struct PendingImport
        {
            public string Path;
            public ImportOptions Options;
        }

        static readonly System.Collections.Generic.List<PendingImport> Pending = new();

        public class ImportOptions
        {
            public Vector2 Pivot = new Vector2(0.5f, 0.5f);
            public Vector4 Border = Vector4.zero;       // 9-slice (L,B,R,T) in pixels
            public bool Repeat;                         // tileable backgrounds
            public int PixelsPerUnit = ArtGenerator.PixelsPerUnit;
            public bool IsUI;

            public static ImportOptions Center => new ImportOptions();
            public static ImportOptions BottomCenter => new ImportOptions { Pivot = new Vector2(0.5f, 0f) };
            public static ImportOptions BottomLeft => new ImportOptions { Pivot = Vector2.zero };
        }

        public static void Save(ArtCanvas canvas, string relativePath, ImportOptions options = null)
        {
            string assetPath = $"{Root}/{relativePath}.png";
            string fullPath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllBytes(fullPath, canvas.EncodePng());
            Pending.Add(new PendingImport { Path = assetPath, Options = options ?? ImportOptions.Center });
        }

        static void ApplyPendingImportSettings()
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var p in Pending)
                {
                    if (AssetImporter.GetAtPath(p.Path) is not TextureImporter ti) continue;
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.spritePixelsPerUnit = p.Options.PixelsPerUnit;
                    ti.mipmapEnabled = false;
                    ti.alphaIsTransparency = true;
                    ti.filterMode = FilterMode.Bilinear;
                    // Backgrounds repeat horizontally only; vertical repeat bleeds a line along the top edge
                    ti.wrapModeU = p.Options.Repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                    ti.wrapModeV = TextureWrapMode.Clamp;
                    ti.textureCompression = TextureImporterCompression.Uncompressed; // atlased + compressed at build time

                    var settings = new TextureImporterSettings();
                    ti.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = p.Options.Pivot;
                    settings.spriteBorder = p.Options.Border;
                    settings.spriteMeshType = p.Options.Repeat || p.Options.Border != Vector4.zero
                        ? SpriteMeshType.FullRect
                        : SpriteMeshType.Tight;
                    settings.spriteGenerateFallbackPhysicsShape = false;
                    ti.SetTextureSettings(settings);
                    ti.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                Pending.Clear();
            }
        }
    }
}
