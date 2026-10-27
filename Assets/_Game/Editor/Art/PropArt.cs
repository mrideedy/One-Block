using UnityEngine;
using static OneBlock.EditorTools.Art.ArtCanvas;

namespace OneBlock.EditorTools.Art
{
    /// <summary>Obstacles and collectibles.</summary>
    public static class PropArt
    {
        static readonly Color Ink = Hex("#1A1C2C");

        public static void Generate()
        {
            var bottom = ArtGenerator.ImportOptions.BottomCenter;
            var center = ArtGenerator.ImportOptions.Center;

            ArtGenerator.Save(Spikes(), "Props/spikes", bottom);
            ArtGenerator.Save(Enemy(0), "Props/enemy_0", bottom);
            ArtGenerator.Save(Enemy(1), "Props/enemy_1", bottom);
            ArtGenerator.Save(MovingPlatform(), "Props/moving_platform", center);
            ArtGenerator.Save(Flag(), "Props/flag", new ArtGenerator.ImportOptions { Pivot = new Vector2(0.1f, 0f) });

            float[] spin = { 1f, 0.72f, 0.28f, 0.72f };
            for (int i = 0; i < spin.Length; i++)
                ArtGenerator.Save(Coin(64, spin[i], i == 3), $"Props/coin_{i}", center);
            ArtGenerator.Save(Star(64), "Props/star", center);
            ArtGenerator.Save(Heart(64), "Props/heart", center);
        }

        static ArtCanvas Spikes()
        {
            var c = new ArtCanvas(100, 64);
            Color red = Hex("#E53935"), light = Hex("#FF6F5E");
            // Three spikes on a dark base plate
            for (int i = 0; i < 3; i++)
            {
                float cx = 17 + i * 33;
                var tri = new[] { new Vector2(cx - 15, 10), new Vector2(cx + 15, 10), new Vector2(cx, 56) };
                c.PolygonOutlined(tri, red, Ink, 2.5f);
                c.Polygon(new[] { new Vector2(cx - 15, 10), new Vector2(cx, 10), new Vector2(cx, 56) }, light);
                c.Circle(cx - 2.5f, 42, 2, WithAlpha(Color.white, 0.6f));
            }
            c.RoundRect(new Rect(1, 0, 98, 13), 3, Ink);
            c.RoundRect(new Rect(3, 2, 94, 9), 2, Hex("#454B5E"));
            c.RoundRect(new Rect(3, 8, 94, 3), 1, Hex("#5E6579"));
            return c;
        }

        static ArtCanvas Enemy(int frame)
        {
            var c = new ArtCanvas(100, 100);
            float squash = frame == 0 ? 0 : 5;
            Rect body = new(12 - squash * 0.5f, 2, 76 + squash, 74 - squash);
            c.RoundRectOutlined(body, 14, Hex("#2B2F45"), Ink, 3);
            c.RoundRect(new Rect(body.x + 6, body.yMax - 16, body.width - 12, 8), 4, Hex("#3B4160"));
            float eyeY = body.y + body.height * 0.52f;
            // Angry eyes with slanted brows
            c.Ellipse(36, eyeY, 10, 8, Color.white);
            c.Ellipse(64, eyeY, 10, 8, Color.white);
            c.Circle(39, eyeY - 1, 4, Ink);
            c.Circle(61, eyeY - 1, 4, Ink);
            c.Polygon(new[] { new Vector2(22, eyeY + 12), new Vector2(46, eyeY + 4), new Vector2(46, eyeY + 10), new Vector2(22, eyeY + 18) }, Ink);
            c.Polygon(new[] { new Vector2(54, eyeY + 4), new Vector2(78, eyeY + 12), new Vector2(78, eyeY + 18), new Vector2(54, eyeY + 10) }, Ink);
            // Feet
            c.RoundRect(new Rect(22, 0, 18, 7), 3, Ink);
            c.RoundRect(new Rect(60, 0, 18, 7), 3, Ink);
            return c;
        }

        static ArtCanvas MovingPlatform()
        {
            var c = new ArtCanvas(300, 44);
            c.RoundRectOutlined(new Rect(1, 1, 298, 42), 10, Hex("#4B5266"), Ink, 3);
            c.RoundRect(new Rect(6, 28, 288, 10), 5, Hex("#6A7389"));
            for (int i = 0; i < 4; i++)
            {
                float x = 30 + i * 80;
                c.Circle(x, 16, 4.5f, Hex("#2F3445"));
                c.Circle(x - 1, 17, 1.8f, Hex("#7F889E"));
            }
            return c;
        }

        static ArtCanvas Flag()
        {
            var c = new ArtCanvas(80, 140);
            c.RoundRect(new Rect(6, 0, 10, 136), 4, Ink);
            c.RoundRect(new Rect(8, 0, 6, 134), 3, Hex("#5E6579"));
            c.Circle(11, 134, 5, Hex("#FFC23A"));
            var flag = new[] { new Vector2(14, 128), new Vector2(76, 106), new Vector2(14, 84) };
            c.PolygonOutlined(flag, Hex("#E53935"), Ink, 2.5f);
            c.Polygon(new[] { new Vector2(14, 106), new Vector2(76, 106), new Vector2(14, 84) }, Hex("#C62828"));
            return c;
        }

        public static ArtCanvas Coin(int size, float widthScale, bool backFace)
        {
            var c = new ArtCanvas(size, size);
            float s = size / 64f;
            float cx = size * 0.5f, cy = size * 0.5f;
            float rx = 26 * s * widthScale, ry = 28 * s;
            Color rim = Hex("#E39B12"), gold = Hex("#FFC928"), inner = Hex("#FFD84D");
            c.Ellipse(cx, cy, rx + 2.5f * s, ry + 2.5f * s, Hex("#8A5A08"));
            c.Ellipse(cx, cy, rx, ry, rim);
            c.Ellipse(cx - 1.5f * s * widthScale, cy + 1 * s, rx - 3 * s * widthScale, ry - 3 * s, gold);
            if (widthScale > 0.4f)
            {
                c.Ellipse(cx, cy, rx * 0.62f, ry * 0.62f, rim);
                c.Ellipse(cx, cy + 0.8f * s, rx * 0.55f, ry * 0.55f, backFace ? Shade(inner, -0.06f) : inner);
                // Shine streak
                c.RoundRect(new Rect(cx - rx * 0.55f, cy + ry * 0.1f, 5 * s * widthScale, ry * 0.5f), 2.5f * s * widthScale,
                    WithAlpha(Color.white, 0.7f));
            }
            return c;
        }

        public static ArtCanvas Star(int size)
        {
            var c = new ArtCanvas(size, size);
            float s = size / 64f;
            var pts = StarPoints(size * 0.5f, size * 0.47f, 29 * s, 13 * s);
            c.PolygonOutlined(pts, Hex("#FFD23F"), Hex("#C27C0E"), 2.5f * s);
            var inner = StarPoints(size * 0.5f - 2 * s, size * 0.47f + 3 * s, 16 * s, 7 * s);
            c.Polygon(inner, WithAlpha(Color.white, 0.35f));
            return c;
        }

        public static Vector2[] StarPoints(float cx, float cy, float outer, float inner)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? outer : inner;
                pts[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }
            return pts;
        }

        public static ArtCanvas Heart(int size)
        {
            var c = new ArtCanvas(size, size);
            float s = size / 64f;
            Color red = Hex("#E53935");
            ArtCanvas.Sdf heart = (x, y) =>
            {
                float d1 = SdCircle(x, y, 22 * s, 38 * s, 15 * s);
                float d2 = SdCircle(x, y, 42 * s, 38 * s, 15 * s);
                float d3 = SdPolygon(x, y, new[] { new Vector2(8.5f * s, 33 * s), new Vector2(55.5f * s, 33 * s), new Vector2(32 * s, 8 * s) });
                return Mathf.Min(d1, Mathf.Min(d2, d3));
            };
            c.Fill(new Rect(0, 0, size, size), (x, y) => heart(x, y) - 2.5f * s, Hex("#8E1B1B"));
            c.Fill(new Rect(0, 0, size, size), heart, red);
            c.Ellipse(20 * s, 42 * s, 5 * s, 3.5f * s, WithAlpha(Color.white, 0.6f));
            return c;
        }
    }
}
