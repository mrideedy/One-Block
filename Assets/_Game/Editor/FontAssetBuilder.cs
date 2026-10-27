using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace OneBlock.EditorTools
{
    /// <summary>
    /// Creates the TextMeshPro font asset for Lilita One (OFL, see LilitaOne-OFL.txt) plus an outlined
    /// material used for titles and big numbers.
    /// </summary>
    public static class FontAssetBuilder
    {
        public const string TtfPath = "Assets/_Game/Art/Fonts/LilitaOne-Regular.ttf";
        public const string FontPath = "Assets/_Game/Art/Fonts/LilitaOne SDF.asset";
        public const string OutlineMaterialPath = "Assets/_Game/Art/Fonts/LilitaOne SDF Outline.mat";

        const string Characters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~•×→—’";

        [MenuItem("One Block/Setup/Build Font Assets", priority = 23)]
        public static TMP_FontAsset EnsureFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (font == null)
            {
                Debug.LogError($"[One Block] Font file missing at {TtfPath}");
                return null;
            }

            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = "LilitaOne SDF";
            AssetDatabase.CreateAsset(asset, FontPath);

            var atlas = asset.atlasTextures[0];
            atlas.name = "LilitaOne SDF Atlas";
            AssetDatabase.AddObjectToAsset(atlas, asset);
            asset.material.name = "LilitaOne SDF Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            asset.TryAddCharacters(Characters, out _);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static Material EnsureOutlineMaterial(TMP_FontAsset font)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            bool create = mat == null;
            if (create) mat = new Material(font.material);

            mat.name = "LilitaOne SDF Outline";
            mat.SetTexture(ShaderUtilities.ID_MainTex, font.atlasTexture);
            mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.16f);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.1f, 0.11f, 0.17f, 1f));
            mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.05f, 0.06f, 0.1f, 0.55f));
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.9f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.2f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.05f);

            if (create) AssetDatabase.CreateAsset(mat, OutlineMaterialPath);
            else EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }
    }
}
