using UnityEngine;
using static OneBlock.EditorTools.Art.ArtCanvas;

namespace OneBlock.EditorTools.Art
{
    /// <summary>Particle sprites for death effects plus shop icons for trails and effects.</summary>
    public static class FxArt
    {
        static readonly Color Ink = Hex("#1A1C2C");

        public static void Generate()
        {
            var center = ArtGenerator.ImportOptions.Center;
            ArtGenerator.Save(Spark(), "FX/fx_spark", center);
            ArtGenerator.Save(Puff(), "FX/fx_puff", center);
            ArtGenerator.Save(Skull(96), "FX/fx_skull", center);
            ArtGenerator.Save(Square(), "FX/fx_square", center);
            ArtGenerator.Save(Flash(), "FX/fx_flash", center);

            var icon = new ArtGenerator.ImportOptions { IsUI = true };
            ArtGenerator.Save(NoneIcon(), "UI/Icons/shop_none", icon);
            foreach (var t in CosmeticDefs.Trails)
                ArtGenerator.Save(TrailIcon(t.Colors), $"UI/Icons/shop_{t.Id}", icon);
            foreach (var e in CosmeticDefs.Effects)
                ArtGenerator.Save(EffectIcon(e.Style, e.Palette), $"UI/Icons/shop_{e.Id}", icon);
        }

        static ArtCanvas Spark()
        {
            var c = new ArtCanvas(48, 48);
            var diamond = new[] { new Vector2(24, 2), new Vector2(32, 24), new Vector2(24, 46), new Vector2(16, 24) };
            c.Polygon(diamond, Color.white);
            return c;
        }

        static ArtCanvas Puff()
        {
            var c = new ArtCanvas(64, 64);
            c.Circle(32, 32, 26, WithAlpha(Color.white, 0.95f));
            c.Circle(25, 38, 9, WithAlpha(Color.white, 1f));
            return c;
        }

        static ArtCanvas Square()
        {
            var c = new ArtCanvas(24, 24);
            c.RoundRect(new Rect(2, 2, 20, 20), 3, Color.white);
            return c;
        }

        static ArtCanvas Flash()
        {
            var c = new ArtCanvas(128, 128);
            c.Fill(new Rect(0, 0, 128, 128), (x, y) => SdCircle(x, y, 64, 64, 60), (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(64, 64)) / 60f;
                return WithAlpha(Color.white, Mathf.Clamp01(1f - d * d));
            });
            return c;
        }

        public static ArtCanvas Skull(int size)
        {
            var c = new ArtCanvas(size, size);
            float s = size / 96f;
            Color bone = Hex("#F4F1E8"), shade = Hex("#CFC9B8");
            Rect cranium = new(18 * s, 34 * s, 60 * s, 52 * s);
            Rect jaw = new(30 * s, 12 * s, 36 * s, 28 * s);
            c.RoundRect(new Rect(cranium.x - 3 * s, cranium.y - 3 * s, cranium.width + 6 * s, cranium.height + 6 * s), 26 * s, Ink);
            c.RoundRect(new Rect(jaw.x - 3 * s, jaw.y - 3 * s, jaw.width + 6 * s, jaw.height + 6 * s), 9 * s, Ink);
            c.RoundRect(jaw, 7 * s, shade);
            c.RoundRect(cranium, 24 * s, bone);
            c.Ellipse(36 * s, 56 * s, 9 * s, 10 * s, Ink);
            c.Ellipse(60 * s, 56 * s, 9 * s, 10 * s, Ink);
            c.Polygon(new[] { new Vector2(48 * s, 46 * s), new Vector2(43 * s, 36 * s), new Vector2(53 * s, 36 * s) }, Ink);
            for (int i = 0; i < 3; i++) c.RoundRect(new Rect((36 + i * 9) * s, 14 * s, 3 * s, 12 * s), 1.5f * s, Ink);
            return c;
        }

        static ArtCanvas NoneIcon()
        {
            var c = new ArtCanvas(128, 128);
            c.Fill(new Rect(0, 0, 128, 128), (x, y) =>
            {
                float ring = Mathf.Abs(SdCircle(x, y, 64, 64, 40)) - 8;
                float slash = SdCapsule(x, y, new Vector2(36, 36), new Vector2(92, 92), 8);
                return Mathf.Min(ring, slash);
            }, WithAlpha(Color.white, 0.8f));
            return c;
        }

        static ArtCanvas TrailIcon(Color[] colors)
        {
            var c = new ArtCanvas(160, 128);
            // Tapered streak with the colour gradient flowing left
            c.Fill(new Rect(0, 30, 160, 70), (x, y) =>
            {
                float t = x / 150f;
                float half = Mathf.Lerp(3f, 24f, t);
                return Mathf.Max(Mathf.Abs(y - 64) - half, x - 150);
            }, (x, y) =>
            {
                float t = 1f - Mathf.Clamp01(x / 150f);
                return WithAlpha(Sample(colors, t), Mathf.Lerp(1f, 0.15f, t * t));
            });
            // Speed lines
            for (int i = 0; i < 3; i++)
            {
                float y = 40 + i * 24;
                c.Capsule(new Vector2(18 + i * 10, y), new Vector2(60 + i * 10, y), 2.5f, WithAlpha(Sample(colors, 0.4f), 0.5f));
            }
            return c;
        }

        static ArtCanvas EffectIcon(OneBlock.Data.DeathEffectStyle style, Color[] palette)
        {
            var c = new ArtCanvas(128, 128);
            var rng = new System.Random((int)style * 31 + 7);
            switch (style)
            {
                case OneBlock.Data.DeathEffectStyle.Explosion:
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i / 10f * Mathf.PI * 2f;
                        var tip = new Vector2(64 + Mathf.Cos(a) * 58, 64 + Mathf.Sin(a) * 58);
                        var l = new Vector2(64 + Mathf.Cos(a + 0.3f) * 22, 64 + Mathf.Sin(a + 0.3f) * 22);
                        var r = new Vector2(64 + Mathf.Cos(a - 0.3f) * 22, 64 + Mathf.Sin(a - 0.3f) * 22);
                        c.Polygon(new[] { l, tip, r }, palette[2 + i % 2]);
                    }
                    c.Circle(64, 64, 30, palette[1]);
                    c.Circle(64, 64, 16, palette[0]);
                    break;
                case OneBlock.Data.DeathEffectStyle.Poof:
                    var puffs = new[] { new Vector3(44, 58, 26), new Vector3(78, 62, 30), new Vector3(60, 84, 24), new Vector3(62, 44, 22) };
                    foreach (var p in puffs) c.Circle(p.x, p.y - 3, p.z + 3, palette[2]);
                    foreach (var p in puffs) c.Circle(p.x, p.y, p.z, palette[0]);
                    break;
                case OneBlock.Data.DeathEffectStyle.Skull:
                    c.Stamp(Skull(112), 8, 8);
                    break;
                case OneBlock.Data.DeathEffectStyle.Confetti:
                    for (int i = 0; i < 16; i++)
                    {
                        float x = 14 + (float)rng.NextDouble() * 100, y = 14 + (float)rng.NextDouble() * 100;
                        float a = (float)rng.NextDouble() * Mathf.PI;
                        var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 8;
                        c.Capsule(new Vector2(x, y) - d, new Vector2(x, y) + d, 4, palette[i % palette.Length]);
                    }
                    break;
            }
            return c;
        }

        public static Color Sample(Color[] colors, float t)
        {
            if (colors.Length == 1) return colors[0];
            float f = Mathf.Clamp01(t) * (colors.Length - 1);
            int i = Mathf.Min(Mathf.FloorToInt(f), colors.Length - 2);
            return Color.Lerp(colors[i], colors[i + 1], f - i);
        }
    }
}
