using System.IO;
using OneBlock.DevTools;
using OneBlock.EditorTools.Art;
using UnityEditor;
using UnityEngine;

namespace OneBlock.EditorTools
{
    /// <summary>
    /// Google Play listing graphics: 512 px store icon, 1024×500 feature graphic and 1920×1080 screenshots,
    /// all written to StoreAssets/ next to the project (outside Assets, so they never ship in the build).
    /// </summary>
    public static class StoreAssetsTools
    {
        public const string Root = "StoreAssets";
        public const string ShotsDir = "Temp/StoreShots";

        [MenuItem("One Block/Build/Export Store Icon (512)", priority = 80)]
        public static void ExportIcon()
        {
            var full = IconArt.Full();
            var tex = new Texture2D(full.Width, full.Height, TextureFormat.RGBA32, false);
            for (int y = 0; y < full.Height; y++)
            for (int x = 0; x < full.Width; x++)
            {
                var c = full.Get(x, y);
                tex.SetPixel(x, y, new Color(c.r, c.g, c.b, 1f));
            }
            tex.Apply();
            WritePng(Downscale(tex, 512, 512), Path.Combine(Root, "icon_512.png"));
            Object.DestroyImmediate(tex);
        }

        /// <summary>Play Mode only: captures the current game view (world + UI) as a store screenshot.</summary>
        public static void Screenshot(string name) => GameCapture.Capture(Path.Combine(Root, "screenshots", name + ".png"));

        /// <summary>
        /// Play Mode only, on the main menu: hides everything but the logo, renders at 2× and downsamples
        /// to the 1024×500 feature graphic.
        /// </summary>
        public static void FeatureGraphic(float logoScale = 0.85f)
        {
            var menu = GameObject.Find("MainMenu");
            if (menu == null) { Debug.LogWarning("[Store] MainMenu not found"); return; }
            var hidden = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in menu.transform)
            {
                if (child.name == "Logo" || !child.gameObject.activeSelf) continue;
                child.gameObject.SetActive(false);
                hidden.Add(child.gameObject);
            }
            var logo = menu.transform.Find("Logo") as RectTransform;
            Vector3 scale = logo != null ? logo.localScale : Vector3.one;
            if (logo != null) logo.localScale = scale * logoScale;

            string temp = Path.Combine("Temp", "feature_2x.png");
            GameCapture.Capture(temp, 2048, 1000);

            if (logo != null) logo.localScale = scale;
            foreach (var go in hidden) go.SetActive(true);

            var big = new Texture2D(2, 2);
            big.LoadImage(File.ReadAllBytes(temp));
            WritePng(Downscale(big, 1024, 500), Path.Combine(Root, "feature_graphic_1024x500.png"));
            Object.DestroyImmediate(big);
        }

        /// <summary>
        /// Play Mode only: records <paramref name="count"/> candidate frames every <paramref name="interval"/> seconds
        /// of gameplay into Temp/StoreShots/{prefix}_NN.png, then writes {prefix}_sheet.png (4 columns, row-major)
        /// so the best moments can be picked with <see cref="Keep"/>.
        /// </summary>
        public static void Record(string prefix, int count = 12, float interval = 2.5f, float minDistance = 0f)
        {
            ShotRecorder.Capture = (p, i) => GameCapture.Capture(Path.Combine(ShotsDir, $"{p}_{i:00}.png"));
            ShotRecorder.Finished = ContactSheet;
            var go = new GameObject("[ShotRecorder]");
            var r = go.AddComponent<ShotRecorder>();
            r.Prefix = prefix;
            r.Count = count;
            r.Interval = interval;
            r.MinDistance = minDistance;
        }

        /// <summary>Copies a recorded candidate into StoreAssets/screenshots under its final name.</summary>
        public static void Keep(string prefix, int index, string name)
        {
            string dst = Path.Combine(Root, "screenshots", name + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            File.Copy(Path.Combine(ShotsDir, $"{prefix}_{index:00}.png"), dst, true);
        }

        internal static void ContactSheet(string prefix, int count)
        {
            const int tw = 480, th = 270, cols = 4;
            int rows = Mathf.CeilToInt(count / (float)cols);
            var sheet = new Texture2D(tw * cols, th * rows, TextureFormat.RGB24, false);
            for (int i = 0; i < count; i++)
            {
                string path = Path.Combine(ShotsDir, $"{prefix}_{i:00}.png");
                if (!File.Exists(path)) continue;
                var src = new Texture2D(2, 2);
                src.LoadImage(File.ReadAllBytes(path));
                var small = Downscale(src, tw, th);
                int cx = i % cols, cy = rows - 1 - i / cols;
                sheet.SetPixels(cx * tw, cy * th, tw, th, small.GetPixels());
                Object.DestroyImmediate(src);
                Object.DestroyImmediate(small);
            }
            sheet.Apply();
            WritePng(sheet, Path.Combine(ShotsDir, $"{prefix}_sheet.png"));
        }

        static Texture2D Downscale(Texture2D src, int width, int height)
        {
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            src.filterMode = FilterMode.Bilinear;
            // Halve repeatedly for a clean box-filtered result
            Texture current = src;
            RenderTexture step = null;
            int w = src.width, h = src.height;
            while (w / 2 >= width && h / 2 >= height)
            {
                w /= 2;
                h /= 2;
                var next = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                next.filterMode = FilterMode.Bilinear;
                Graphics.Blit(current, next);
                if (step != null) RenderTexture.ReleaseTemporary(step);
                step = next;
                current = next;
            }
            Graphics.Blit(current, rt);
            if (step != null) RenderTexture.ReleaseTemporary(step);

            RenderTexture.active = rt;
            var dst = new Texture2D(width, height, TextureFormat.RGB24, false);
            dst.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            dst.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return dst;
        }

        static void WritePng(Texture2D tex, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log($"[Store] Wrote {path}");
        }
    }
}
