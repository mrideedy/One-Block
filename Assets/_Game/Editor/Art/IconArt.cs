using UnityEngine;
using static OneBlock.EditorTools.Art.ArtCanvas;

namespace OneBlock.EditorTools.Art
{
    /// <summary>
    /// App icon art: the hero standing on the golden block against a sunny sky and grass.
    /// Produces the adaptive icon layers (background + foreground kept inside Android's safe zone),
    /// a full legacy/round icon and the 512 px Play Store icon.
    /// </summary>
    public static class IconArt
    {
        public const string Dir = "Icon";
        const int S = 1024;

        public static void Generate()
        {
            var ui = new ArtGenerator.ImportOptions { IsUI = true };
            var background = Background();
            var foreground = Foreground();
            ArtGenerator.Save(background, $"{Dir}/icon_background", ui);
            ArtGenerator.Save(foreground, $"{Dir}/icon_foreground", ui);

            var full = new ArtCanvas(S, S);
            Composite(full, background);
            Composite(full, foreground);
            ArtGenerator.Save(full, $"{Dir}/icon_full", ui);
        }

        /// <summary>The composited icon, for the Play Store export.</summary>
        public static ArtCanvas Full()
        {
            var full = new ArtCanvas(S, S);
            Composite(full, Background());
            Composite(full, Foreground());
            return full;
        }

        static ArtCanvas Background()
        {
            var c = new ArtCanvas(S, S);
            Color skyTop = Hex("#3C8FE6"), skyBottom = Hex("#AEE0F8");
            c.Fill(new Rect(0, 0, S, S), (x, y) => -1f, (x, y) => Color.Lerp(skyBottom, skyTop, Mathf.SmoothStep(0f, 1f, y / S)));

            // Soft sun glow behind where the block sits
            c.Circle(512, 520, 360, WithAlpha(Color.white, 0.12f));
            c.Circle(512, 520, 250, WithAlpha(Color.white, 0.1f));

            // Clouds
            void Cloud(float cx, float cy, float s)
            {
                Color shade = Hex("#DCEFFB");
                c.RoundRect(new Rect(cx - 120 * s, cy - 30 * s, 240 * s, 60 * s), 30 * s, shade);
                c.Circle(cx - 55 * s, cy + 10 * s, 50 * s, shade);
                c.Circle(cx + 30 * s, cy + 25 * s, 65 * s, shade);
                c.RoundRect(new Rect(cx - 115 * s, cy - 22 * s, 230 * s, 52 * s), 26 * s, Color.white);
                c.Circle(cx - 55 * s, cy + 16 * s, 44 * s, Color.white);
                c.Circle(cx + 30 * s, cy + 31 * s, 58 * s, Color.white);
            }
            Cloud(300, 770, 0.62f);
            Cloud(770, 690, 0.5f);

            // Rolling hill and grassy ground
            c.Fill(new Rect(0, 0, S, 420), (x, y) => y - (330 + Mathf.Sin(x * 0.006f + 0.8f) * 40f), Hex("#4E9E6A"));
            Color dirt = Hex("#6A4A3A"), grass = Hex("#5DBE3C"), grassLight = Hex("#8BDB52"), grassDark = Hex("#3C9530");
            c.RoundRect(new Rect(0, 0, S, 250), 0, dirt);
            for (int i = 0; i < 14; i++)
            {
                float x = 30 + (i * 137) % 980, y = 30 + (i * 53) % 150;
                c.RoundRect(new Rect(x, y, 46, 30), 8, Shade(dirt, i % 2 == 0 ? -0.12f : 0.1f));
            }
            c.RoundRect(new Rect(0, 200, S, 90), 0, grassDark);
            c.RoundRect(new Rect(0, 214, S, 76), 0, grass);
            c.RoundRect(new Rect(0, 258, S, 32), 16, grassLight);
            for (int i = 0; i < 12; i++) c.Circle(40 + i * 86, 206, 34, grass);
            return c;
        }

        static ArtCanvas Foreground()
        {
            var c = new ArtCanvas(S, S);
            // Everything important stays inside the centre ~62% so any launcher mask keeps it

            // Shadow on the grass
            c.Ellipse(512, 292, 170, 24, WithAlpha(Color.black, 0.18f));

            // The golden block (same look as the in-game block, drawn large)
            Rect block = new(512 - 132, 288, 264, 250);
            Color gold = Hex("#FFC23A"), edge = Shade(gold, -0.24f), light = Shade(gold, 0.28f);
            c.RoundRect(new Rect(block.x - 10, block.y - 10, block.width + 20, block.height + 20), 46, Hex("#1A1C2C"));
            c.RoundRect(block, 38, edge);
            c.RoundRect(new Rect(block.x, block.y + 30, block.width, block.height - 30), 38, Shade(gold, 0.08f));
            Rect inner = new(block.x + 20, block.y + 28, block.width - 40, block.height - 48);
            c.RoundRect(inner, 24, (x, y) => Color.Lerp(gold, light, (y - inner.y) / inner.height * 0.5f));
            c.RoundRect(new Rect(block.x + 34, block.yMax - 60, 86, 22), 11, WithAlpha(Color.white, 0.6f));
            c.Circle(block.x + 144, block.yMax - 49, 11, WithAlpha(Color.white, 0.5f));

            // The hero standing on top
            var hero = CharacterArt.IdleFrame(CharacterArt.Default);
            StampBilinear(c, hero, 500, block.yMax - 6, 2.3f);

            // Little sparkles
            void Sparkle(float x, float y, float r)
            {
                c.Capsule(new Vector2(x - r, y), new Vector2(x + r, y), r * 0.18f, Color.white);
                c.Capsule(new Vector2(x, y - r), new Vector2(x, y + r), r * 0.18f, Color.white);
            }
            Sparkle(318, 660, 30);
            Sparkle(716, 580, 24);
            return c;
        }

        /// <summary>Bilinear-scaled stamp anchored at bottom-centre (straight alpha).</summary>
        internal static void StampBilinear(ArtCanvas dst, ArtCanvas src, float centreX, float bottomY, float scale)
        {
            int w = Mathf.RoundToInt(src.Width * scale), h = Mathf.RoundToInt(src.Height * scale);
            int ox = Mathf.RoundToInt(centreX - w * 0.5f), oy = Mathf.RoundToInt(bottomY);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int dx = ox + x, dy = oy + y;
                if (dx < 0 || dy < 0 || dx >= dst.Width || dy >= dst.Height) continue;
                float sx = (x + 0.5f) / scale - 0.5f, sy = (y + 0.5f) / scale - 0.5f;
                int x0 = Mathf.FloorToInt(sx), y0 = Mathf.FloorToInt(sy);
                float fx = sx - x0, fy = sy - y0;
                Color Sample(int px, int py)
                {
                    if (px < 0 || py < 0 || px >= src.Width || py >= src.Height) return Color.clear;
                    Color s = src.Get(px, py);
                    return new Color(s.r * s.a, s.g * s.a, s.b * s.a, s.a);
                }
                Color a = Color.Lerp(Sample(x0, y0), Sample(x0 + 1, y0), fx);
                Color b = Color.Lerp(Sample(x0, y0 + 1), Sample(x0 + 1, y0 + 1), fx);
                Color p = Color.Lerp(a, b, fy);
                if (p.a > 0.001f) dst.Blend(dx, dy, new Color(p.r / p.a, p.g / p.a, p.b / p.a, 1f), p.a);
            }
        }

        static void Composite(ArtCanvas dst, ArtCanvas src)
        {
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                Color s = src.Get(x, y);
                if (s.a > 0f) dst.Blend(x, y, new Color(s.r, s.g, s.b, 1f), s.a);
            }
        }
    }
}
