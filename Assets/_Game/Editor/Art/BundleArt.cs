using UnityEngine;
using static OneBlock.EditorTools.Art.ArtCanvas;

namespace OneBlock.EditorTools.Art
{
    /// <summary>Shop cards for ad bundles: the world thumbnail with the bundle's hero and block standing in it.</summary>
    public static class BundleArt
    {
        public static void Generate()
        {
            foreach (var b in CosmeticDefs.Bundles)
            {
                var world = System.Array.Find(EnvironmentArt.All, w => w.Id == b.World);
                var hero = System.Array.Find(CharacterArt.All, s => s.Id == b.Character);
                var c = EnvironmentArt.ThumbnailFor(world);

                // Darken the lower half slightly so the hero and block read against busy ground
                for (int y = 0; y < 90; y++)
                for (int x = 0; x < c.Width; x++)
                    if (c.Get(x, y).a > 0) c.Blend(x, y, Color.black, 0.22f * (1f - y / 90f));

                StampScaled(c, BlockArt.ForId(b.Block), 190, 97, 0.62f);
                StampScaled(c, CharacterArt.IdleFrame(hero), 62, 96, 0.8f);
                ArtGenerator.Save(c, $"UI/Bundles/{b.Id}", new ArtGenerator.ImportOptions { IsUI = true });
            }
        }

        /// <summary>Box-filtered resampled stamp (straight alpha).</summary>
        static void StampScaled(ArtCanvas dst, ArtCanvas src, int ox, int oy, float scale)
        {
            int w = Mathf.RoundToInt(src.Width * scale), h = Mathf.RoundToInt(src.Height * scale);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int dx = ox + x, dy = oy + y;
                if (dx < 0 || dy < 0 || dx >= dst.Width || dy >= dst.Height) continue;
                float sx0 = x / scale, sy0 = y / scale, sx1 = (x + 1) / scale, sy1 = (y + 1) / scale;
                Color sum = Color.clear;
                int n = 0;
                for (int sy = Mathf.FloorToInt(sy0); sy < Mathf.CeilToInt(sy1); sy++)
                for (int sx = Mathf.FloorToInt(sx0); sx < Mathf.CeilToInt(sx1); sx++)
                {
                    if (sx >= src.Width || sy >= src.Height) continue;
                    Color s = src.Get(sx, sy);
                    sum += new Color(s.r * s.a, s.g * s.a, s.b * s.a, s.a);
                    n++;
                }
                if (n == 0) continue;
                sum /= n;
                if (sum.a > 0.001f) dst.Blend(dx, dy, new Color(sum.r / sum.a, sum.g / sum.a, sum.b / sum.a, 1f), sum.a);
            }
        }
    }
}
