using UnityEngine;
using static OneBlock.EditorTools.Art.ArtCanvas;

namespace OneBlock.EditorTools.Art
{
    public enum HazardStyle { Spikes, MudSnake, IceSpikes, Lava, Crystals, CandyCorn, Urchin, Zapper }
    public enum EnemyStyle { Shadow, Scorpion, Penguin, MagmaBlob, Alien, GummyBear, Crab, Drone }

    /// <summary>
    /// Per-world deadly cell props and walking enemies. Gameplay is identical in every world (the hazard
    /// hitbox is the lower 42% of the cell, x 14%..86%); only the look changes.
    /// Hazard frames are 100x100 with the cell floor at y = 30 (pivot 0.5, 0.3) so pits can sink into the ground.
    /// Enemy frames are 100x100, bottom-centre pivot, facing left (the renderer flips them).
    /// </summary>
    public static class HazardArt
    {
        public const int HazardFrames = 3;
        public const int EnemyFrames = 2;
        const float Floor = 30f;
        static readonly Color Ink = Hex("#1A1C2C");

        public static void Generate(EnvironmentArt.WorldPalette p, string dir)
        {
            var hazard = new ArtGenerator.ImportOptions { Pivot = new Vector2(0.5f, Floor / 100f) };
            for (int f = 0; f < HazardFrames; f++)
                ArtGenerator.Save(Hazard(p.Hazards, f), $"{dir}/hazard_{f}", hazard);
            for (int f = 0; f < EnemyFrames; f++)
                ArtGenerator.Save(Enemy(p.Enemies, f), $"{dir}/enemy_{f}", ArtGenerator.ImportOptions.BottomCenter);
        }

        public static string DisplayName(HazardStyle style) => style switch
        {
            HazardStyle.MudSnake => "snake pit",
            HazardStyle.IceSpikes => "ice spikes",
            HazardStyle.Lava => "lava",
            HazardStyle.Crystals => "crystals",
            HazardStyle.CandyCorn => "candy spikes",
            HazardStyle.Urchin => "sea urchin",
            HazardStyle.Zapper => "zapper",
            _ => "spikes"
        };

        public static ArtCanvas Hazard(HazardStyle style, int frame) => style switch
        {
            HazardStyle.MudSnake => MudSnake(frame),
            HazardStyle.IceSpikes => IceSpikes(frame),
            HazardStyle.Lava => Lava(frame),
            HazardStyle.Crystals => Crystals(frame),
            HazardStyle.CandyCorn => CandyCorn(frame),
            HazardStyle.Urchin => Urchin(frame),
            HazardStyle.Zapper => Zapper(frame),
            _ => Spikes()
        };

        public static ArtCanvas Enemy(EnemyStyle style, int frame) => style switch
        {
            EnemyStyle.Scorpion => Scorpion(frame),
            EnemyStyle.Penguin => Penguin(frame),
            EnemyStyle.MagmaBlob => MagmaBlob(frame),
            EnemyStyle.Alien => Alien(frame),
            EnemyStyle.GummyBear => GummyBear(frame),
            EnemyStyle.Crab => Crab(frame),
            EnemyStyle.Drone => Drone(frame),
            _ => Shadow(frame)
        };

        // ================================================================ helpers

        static void EllipseOutlined(ArtCanvas c, float cx, float cy, float rx, float ry, Color fill, float thickness)
        {
            c.Fill(new Rect(cx - rx - thickness - 2, cy - ry - thickness - 2, (rx + thickness + 2) * 2, (ry + thickness + 2) * 2),
                (x, y) => SdEllipse(x, y, cx, cy, rx + thickness, ry + thickness), Ink);
            c.Ellipse(cx, cy, rx, ry, fill);
        }

        /// <summary>A sunken pit: dark rim, then a fill surface whose top sits a little above the floor line.</summary>
        static void Pit(ArtCanvas c, Color rim, Color fill, Color surface, float surfaceY)
        {
            c.RoundRect(new Rect(1, Floor - 18, 98, surfaceY - Floor + 22), 12, rim);
            c.RoundRect(new Rect(5, Floor - 14, 90, surfaceY - Floor + 15), 9, fill);
            c.RoundRect(new Rect(5, surfaceY - 5, 90, 5), 2.5f, surface);
        }

        static void Glint(ArtCanvas c, float x, float y, float size)
        {
            c.Capsule(new Vector2(x - size, y), new Vector2(x + size, y), size * 0.22f, WithAlpha(Color.white, 0.95f));
            c.Capsule(new Vector2(x, y - size), new Vector2(x, y + size), size * 0.22f, WithAlpha(Color.white, 0.95f));
        }

        static void AngryEyes(ArtCanvas c, float cx, float eyeY, float spacing, float size, Color white)
        {
            c.Ellipse(cx - spacing, eyeY, size, size * 0.85f, white);
            c.Ellipse(cx + spacing, eyeY, size, size * 0.85f, white);
            c.Circle(cx - spacing - size * 0.3f, eyeY - 1, size * 0.42f, Ink);
            c.Circle(cx + spacing - size * 0.3f, eyeY - 1, size * 0.42f, Ink);
            float b = size * 1.1f;
            c.Polygon(new[] { new Vector2(cx - spacing - b, eyeY + b), new Vector2(cx - spacing + b, eyeY + size * 0.5f),
                new Vector2(cx - spacing + b, eyeY + size * 1.1f), new Vector2(cx - spacing - b, eyeY + b + size * 0.6f) }, Ink);
            c.Polygon(new[] { new Vector2(cx + spacing - b, eyeY + size * 0.5f), new Vector2(cx + spacing + b, eyeY + b),
                new Vector2(cx + spacing + b, eyeY + b + size * 0.6f), new Vector2(cx + spacing - b, eyeY + size * 1.1f) }, Ink);
        }

        // ================================================================ hazards

        static ArtCanvas Spikes()
        {
            var c = new ArtCanvas(100, 100);
            Color red = Hex("#E53935"), light = Hex("#FF6F5E");
            for (int i = 0; i < 3; i++)
            {
                float cx = 17 + i * 33;
                var tri = new[] { new Vector2(cx - 15, Floor + 10), new Vector2(cx + 15, Floor + 10), new Vector2(cx, Floor + 56) };
                c.PolygonOutlined(tri, red, Ink, 2.5f);
                c.Polygon(new[] { new Vector2(cx - 15, Floor + 10), new Vector2(cx, Floor + 10), new Vector2(cx, Floor + 56) }, light);
                c.Circle(cx - 2.5f, Floor + 42, 2, WithAlpha(Color.white, 0.6f));
            }
            c.RoundRect(new Rect(1, Floor, 98, 13), 3, Ink);
            c.RoundRect(new Rect(3, Floor + 2, 94, 9), 2, Hex("#454B5E"));
            c.RoundRect(new Rect(3, Floor + 8, 94, 3), 1, Hex("#5E6579"));
            return c;
        }

        static ArtCanvas MudSnake(int frame)
        {
            var c = new ArtCanvas(100, 100);
            Color mud = Hex("#6B4A2B"), mudDark = Hex("#4A3220"), mudLight = Hex("#8A6440");
            Pit(c, mudDark, mud, mudLight, Floor + 1);
            // Bubbles that pop in turn
            float[] bx = { 18, 80, 30 };
            for (int i = 0; i < 3; i++)
            {
                float r = ((frame + i) % 3) * 2.2f + 1.5f;
                c.Circle(bx[i], Floor - 2, r, mudLight);
                c.Circle(bx[i] - r * 0.3f, Floor - 2 + r * 0.3f, r * 0.3f, WithAlpha(Color.white, 0.35f));
            }

            // Snake rising out of the mud, swaying
            float sway = (frame - 1) * 4f;
            Color scale = Hex("#86C040"), scaleDark = Hex("#557F22"), belly = Hex("#F2D46B");
            var pts = new Vector2[8];
            for (int i = 0; i < pts.Length; i++)
            {
                float t = i / (pts.Length - 1f);
                pts[i] = new Vector2(56 + Mathf.Sin(t * Mathf.PI * 1.6f) * 9f + sway * t, Floor + 2 + t * 30f);
            }
            for (int i = 0; i < pts.Length - 1; i++) c.Capsule(pts[i], pts[i + 1], 9.5f, Ink);
            for (int i = 0; i < pts.Length - 1; i++) c.Capsule(pts[i], pts[i + 1], 7f, i % 2 == 0 ? scale : scaleDark);
            for (int i = 1; i < pts.Length - 1; i++) c.Circle(pts[i].x - 3, pts[i].y, 2.2f, belly);

            // Head looks left at the player
            Vector2 head = pts[pts.Length - 1] + new Vector2(-6, 6);
            EllipseOutlined(c, head.x, head.y, 13, 9.5f, scale, 2.5f);
            c.Ellipse(head.x + 3, head.y + 4, 7, 3, Shade(scale, 0.15f));
            c.Circle(head.x - 4, head.y + 3, 3.4f, Color.white);
            c.Circle(head.x - 5, head.y + 3, 1.8f, Ink);
            c.Circle(head.x - 12, head.y - 1, 1.1f, Ink);
            if (frame != 1)
            {
                Color tongue = Hex("#E53950");
                Vector2 t0 = new Vector2(head.x - 12, head.y - 4);
                c.Capsule(t0, t0 + new Vector2(-8, -1), 1.3f, tongue);
                c.Capsule(t0 + new Vector2(-8, -1), t0 + new Vector2(-12, 2), 1.1f, tongue);
                c.Capsule(t0 + new Vector2(-8, -1), t0 + new Vector2(-12, -4), 1.1f, tongue);
            }
            return c;
        }

        static ArtCanvas IceSpikes(int frame)
        {
            var c = new ArtCanvas(100, 100);
            Color ice = Hex("#BDEBFF"), shade = Hex("#79C3EA"), deep = Hex("#4F9CCB");
            c.Ellipse(50, Floor + 4, 50, 10, Color.white);
            var spikes = new[] { (22f, 48f, 13f), (52f, 60f, 16f), (80f, 44f, 12f) };
            foreach (var (cx, h, hw) in spikes)
            {
                var tri = new[] { new Vector2(cx - hw, Floor + 4), new Vector2(cx + hw, Floor + 4), new Vector2(cx + 2, Floor + h) };
                c.PolygonOutlined(tri, ice, Ink, 2.5f);
                c.Polygon(new[] { new Vector2(cx + 1, Floor + 4), new Vector2(cx + hw, Floor + 4), new Vector2(cx + 2, Floor + h) }, shade);
                c.Polygon(new[] { new Vector2(cx + hw * 0.4f, Floor + 4), new Vector2(cx + hw, Floor + 4), new Vector2(cx + 2, Floor + h * 0.45f) }, deep);
            }
            // Snow drift over the base
            c.Ellipse(20, Floor + 5, 20, 7, Color.white);
            c.Ellipse(60, Floor + 4, 26, 6, Color.white);
            c.Ellipse(90, Floor + 5, 14, 6, Hex("#E6F4FF"));
            var glint = spikes[frame % spikes.Length];
            Glint(c, glint.Item1 - 3, Floor + glint.Item2 * 0.62f, 5f);
            return c;
        }

        static ArtCanvas Lava(int frame)
        {
            var c = new ArtCanvas(100, 100);
            Color rock = Hex("#2E2429"), lava = Hex("#FF6A1A"), hot = Hex("#FFB02E"), core = Hex("#FFE36B");
            c.RoundRect(new Rect(1, Floor - 22, 98, 28), 12, rock);
            c.RoundRect(new Rect(5, Floor - 18, 90, 21), 9, (x, y) => Color.Lerp(Hex("#D9331A"), lava, Mathf.InverseLerp(Floor - 18, Floor + 3, y)));
            c.RoundRect(new Rect(5, Floor - 3, 90, 6), 3, hot);
            // Crust islands drifting
            float drift = frame * 6f;
            c.RoundRect(new Rect(14 + drift, Floor - 9, 14, 5), 2.5f, WithAlpha(rock, 0.8f));
            c.RoundRect(new Rect(62 - drift * 0.5f, Floor - 14, 10, 4), 2, WithAlpha(rock, 0.7f));

            // Bubbles and splashes (reach the top of the hitbox so the danger reads clearly)
            switch (frame)
            {
                case 0:
                    c.Circle(30, Floor + 12, 7, lava); c.Circle(30, Floor + 14, 4, core);
                    c.Circle(72, Floor + 26, 5, lava); c.Circle(76, Floor + 38, 3, hot);
                    break;
                case 1:
                    c.Fill(new Rect(40, Floor, 22, 36), (x, y) => SdEllipse(x, y, 51, Floor + 16, 8, 18), lava);
                    c.Ellipse(51, Floor + 18, 4, 11, core);
                    c.Circle(24, Floor + 34, 4, hot); c.Circle(80, Floor + 12, 6, lava);
                    break;
                default:
                    c.Circle(68, Floor + 12, 8, lava); c.Circle(68, Floor + 15, 4.5f, core);
                    c.Circle(46, Floor + 40, 4, lava); c.Circle(40, Floor + 30, 2.5f, hot);
                    break;
            }
            // Rim stones
            c.Ellipse(4, Floor + 2, 8, 6, rock);
            c.Ellipse(96, Floor + 2, 8, 6, rock);
            return c;
        }

        static ArtCanvas Crystals(int frame)
        {
            var c = new ArtCanvas(100, 100);
            Color rock = Hex("#5E5A7C"), crystal = Hex("#C86BFF"), light = Hex("#EBC2FF"), dark = Hex("#8A3FCF");
            float pulse = frame == 1 ? 0.34f : frame == 2 ? 0.22f : 0.12f;
            c.Ellipse(50, Floor + 26, 48, 30, WithAlpha(crystal, pulse));
            var shards = new[] { (26f, -14f, 44f), (50f, 4f, 58f), (74f, 16f, 42f) };
            foreach (var (cx, lean, h) in shards)
            {
                var poly = new[] { new Vector2(cx - 9, Floor + 4), new Vector2(cx + 9, Floor + 4), new Vector2(cx + 7 + lean * 0.3f, Floor + h - 10), new Vector2(cx + lean * 0.35f, Floor + h), new Vector2(cx - 8 + lean * 0.3f, Floor + h - 12) };
                c.PolygonOutlined(poly, crystal, Ink, 2.5f);
                c.Polygon(new[] { poly[0], new Vector2(cx, Floor + 4), new Vector2(cx + lean * 0.35f, Floor + h), poly[4] }, light);
                c.Polygon(new[] { new Vector2(cx + 3, Floor + 4), poly[1], poly[2] }, dark);
            }
            c.Ellipse(50, Floor + 3, 48, 8, rock);
            c.Ellipse(40, Floor + 5, 18, 4, Shade(rock, 0.15f));
            Glint(c, shards[frame].Item1 - 2, Floor + shards[frame].Item3 * 0.7f, 4.5f);
            return c;
        }

        static ArtCanvas CandyCorn(int frame)
        {
            var c = new ArtCanvas(100, 100);
            Color yellow = Hex("#FFD23F"), orange = Hex("#FF8A3D"), cream = Hex("#FFF6E0");
            for (int i = 0; i < 3; i++)
            {
                float cx = 17 + i * 33, baseY = Floor + 6, top = Floor + 54;
                Vector2 l = new(cx - 15, baseY), r = new(cx + 15, baseY), t = new(cx, top);
                c.PolygonOutlined(new[] { l, r, t }, yellow, Ink, 2.5f);
                // Bands: orange middle, cream tip
                float Lerp(float a, float b, float k) => a + (b - a) * k;
                float m1 = 0.36f, m2 = 0.7f;
                c.Polygon(new[] { new Vector2(Lerp(l.x, t.x, m1), Lerp(baseY, top, m1)), new Vector2(Lerp(r.x, t.x, m1), Lerp(baseY, top, m1)),
                    new Vector2(Lerp(r.x, t.x, m2), Lerp(baseY, top, m2)), new Vector2(Lerp(l.x, t.x, m2), Lerp(baseY, top, m2)) }, orange);
                c.Polygon(new[] { new Vector2(Lerp(l.x, t.x, m2), Lerp(baseY, top, m2)), new Vector2(Lerp(r.x, t.x, m2), Lerp(baseY, top, m2)), t }, cream);
                c.Capsule(new Vector2(cx - 7, baseY + 6), new Vector2(cx - 3, baseY + 30), 1.6f, WithAlpha(Color.white, 0.5f));
            }
            // Pink icing base
            c.RoundRect(new Rect(1, Floor, 98, 12), 6, Hex("#E0619F"));
            c.RoundRect(new Rect(3, Floor + 3, 94, 8), 4, Hex("#FF8CC3"));
            Glint(c, 17 + frame * 33 + 2, Floor + 40, 4.5f);
            return c;
        }

        static ArtCanvas Urchin(int frame)
        {
            var c = new ArtCanvas(100, 100);
            Color body = Hex("#4A2A6B"), spine = Hex("#6E3FA0"), tip = Hex("#C99BFF");
            // Sand mound
            c.Ellipse(50, Floor + 3, 46, 8, Hex("#E8D29A"));
            float cx = 50, cy = Floor + 18;
            int n = 13;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI * (0.02f + 0.96f * i / (n - 1f));
                float len = (i % 2 == 0 ? 30f : 23f) + Mathf.Sin(frame * 2.1f + i) * 2.5f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                c.Capsule(new Vector2(cx, cy), new Vector2(cx, cy) + dir * len, 3.6f, Ink);
                c.Capsule(new Vector2(cx, cy), new Vector2(cx, cy) + dir * (len - 1), 2.2f, spine);
                c.Circle(cx + dir.x * (len - 2), cy + dir.y * (len - 2), 1.8f, tip);
            }
            EllipseOutlined(c, cx, cy, 20, 15, body, 2.5f);
            c.Ellipse(cx - 6, cy + 7, 7, 3.5f, Shade(body, 0.2f));
            // Grumpy little face
            c.Circle(cx - 7, cy + 1, 3.2f, Color.white);
            c.Circle(cx + 5, cy + 1, 3.2f, Color.white);
            c.Circle(cx - 8, cy, 1.7f, Ink);
            c.Circle(cx + 4, cy, 1.7f, Ink);
            c.Capsule(new Vector2(cx - 5, cy - 7), new Vector2(cx + 3, cy - 7), 1.2f, Ink);
            return c;
        }

        static ArtCanvas Zapper(int frame)
        {
            var c = new ArtCanvas(100, 100);
            Color metal = Hex("#2E2548"), cyan = Hex("#3DF2FF"), magenta = Hex("#FF3DCB");
            c.RoundRect(new Rect(1, Floor, 98, 10), 4, Ink);
            c.RoundRect(new Rect(3, Floor + 2, 94, 6), 3, metal);
            foreach (float px in new[] { 12f, 88f })
            {
                c.RoundRectOutlined(new Rect(px - 6, Floor + 6, 12, 36), 4, metal, Ink, 2.5f);
                c.CircleOutlined(px, Floor + 44, 6, cyan, Ink, 2.5f);
                c.Circle(px - 1.5f, Floor + 45.5f, 2, Color.white);
            }
            // Electric arc: jagged polyline that changes each frame
            float[][] jitter =
            {
                new[] { 0f, 8f, -6f, 10f, -8f, 5f, 0f },
                new[] { 0f, -7f, 9f, -4f, 7f, -9f, 0f },
                new[] { 0f, 5f, 10f, -8f, -3f, 8f, 0f },
            };
            var j = jitter[frame % jitter.Length];
            var pts = new Vector2[j.Length];
            for (int i = 0; i < j.Length; i++) pts[i] = new Vector2(Mathf.Lerp(12, 88, i / (j.Length - 1f)), Floor + 30 + j[i]);
            for (int i = 0; i < pts.Length - 1; i++) c.Capsule(pts[i], pts[i + 1], 6f, WithAlpha(magenta, 0.35f));
            for (int i = 0; i < pts.Length - 1; i++) c.Capsule(pts[i], pts[i + 1], 2.6f, cyan);
            for (int i = 0; i < pts.Length - 1; i++) c.Capsule(pts[i], pts[i + 1], 1.1f, Color.white);
            // Second, fainter arc
            for (int i = 0; i < pts.Length - 1; i++)
                c.Capsule(pts[i] + new Vector2(0, -12 - j[i] * 0.5f), pts[i + 1] + new Vector2(0, -12 - j[i + 1] * 0.5f), 1.6f, WithAlpha(magenta, 0.8f));
            return c;
        }

        // ================================================================ enemies

        static ArtCanvas Shadow(int frame)
        {
            var c = new ArtCanvas(100, 100);
            float squash = frame == 0 ? 0 : 5;
            Rect body = new(12 - squash * 0.5f, 2, 76 + squash, 74 - squash);
            c.RoundRectOutlined(body, 14, Hex("#2B2F45"), Ink, 3);
            c.RoundRect(new Rect(body.x + 6, body.yMax - 16, body.width - 12, 8), 4, Hex("#3B4160"));
            float eyeY = body.y + body.height * 0.52f;
            c.Ellipse(36, eyeY, 10, 8, Color.white);
            c.Ellipse(64, eyeY, 10, 8, Color.white);
            c.Circle(39, eyeY - 1, 4, Ink);
            c.Circle(61, eyeY - 1, 4, Ink);
            c.Polygon(new[] { new Vector2(22, eyeY + 12), new Vector2(46, eyeY + 4), new Vector2(46, eyeY + 10), new Vector2(22, eyeY + 18) }, Ink);
            c.Polygon(new[] { new Vector2(54, eyeY + 4), new Vector2(78, eyeY + 12), new Vector2(78, eyeY + 18), new Vector2(54, eyeY + 10) }, Ink);
            c.RoundRect(new Rect(22, 0, 18, 7), 3, Ink);
            c.RoundRect(new Rect(60, 0, 18, 7), 3, Ink);
            return c;
        }

        static ArtCanvas Scorpion(int frame)
        {
            var c = new ArtCanvas(100, 100);
            Color shell = Hex("#D08A3E"), dark = Hex("#9C5E24"), light = Hex("#F0B267");
            float step = frame == 0 ? 0 : 3;
            // Authored small, then stretched to fill the enemy hitbox (it's a low, wide creature)
            const float sx = 0.94f, sy = 1.35f, sr = 1.15f;
            Vector2 P(float x, float y) => new Vector2(52 + (x - 48) * sx, y * sy);
            // Legs
            for (int i = 0; i < 3; i++)
            {
                float x = 40 + i * 13;
                float lift = (i + frame) % 2 == 0 ? step : 0;
                c.Capsule(P(x, 16), P(x - 6, 1.5f + lift), 3.5f * sr, Ink);
                c.Capsule(P(x, 16), P(x - 6, 1.5f + lift), 2f * sr, dark);
            }
            // Tail arching over the back with the stinger pointing forward (left)
            var tail = new[] { P(76, 20), P(85, 30), P(87, 43), P(82, 54), P(70, 58 + step * 0.4f) };
            for (int i = 0; i < tail.Length - 1; i++) c.Capsule(tail[i], tail[i + 1], (8.5f - i * 0.8f) * sr, Ink);
            for (int i = 0; i < tail.Length - 1; i++) c.Capsule(tail[i], tail[i + 1], (6f - i * 0.8f) * sr, i % 2 == 0 ? shell : dark);
            var sting = tail[tail.Length - 1];
            c.PolygonOutlined(new[] { sting + new Vector2(2, 6), sting + new Vector2(2, -6), sting + new Vector2(-15, -8 + step) }, Hex("#3A2A20"), Ink, 2f);
            // Body
            var bodyC = P(56, 20);
            EllipseOutlined(c, bodyC.x, bodyC.y, 26 * sx, 12 * sy, shell, 2.5f);
            c.Ellipse(bodyC.x + 2, bodyC.y + 8, 18 * sx, 4 * sy, light);
            for (int i = 0; i < 3; i++) c.Capsule(P(46 + i * 12, 10), P(46 + i * 12, 30), 1.5f, dark);
            // Claws
            float pinch = frame == 0 ? 0 : 4;
            c.CapsuleOutlined(P(34, 22), P(20, 30), 4 * sr, shell, Ink, 2.5f);
            c.CapsuleOutlined(P(20, 32), P(4, 40 - pinch), 4.2f * sr, shell, Ink, 2.5f);
            c.CapsuleOutlined(P(20, 29), P(6, 22 + pinch), 3.4f * sr, shell, Ink, 2.5f);
            var joint = P(20, 31);
            EllipseOutlined(c, joint.x, joint.y, 7, 7, shell, 2f);
            // Eyes on the front
            var eye = P(34, 28);
            c.Circle(eye.x, eye.y, 5.5f, Color.white);
            c.Circle(eye.x - 1.5f, eye.y, 3f, Ink);
            c.Capsule(eye + new Vector2(-6, 8), eye + new Vector2(5, 4), 1.6f, Ink);
            return c;
        }

        static ArtCanvas Penguin(int frame)
        {
            var c = new ArtCanvas(100, 100);
            Color navy = Hex("#2B2F45"), belly = Hex("#F4F6FF"), orange = Hex("#FF9A2E");
            float tilt = frame == 0 ? -3 : 3;
            // Feet
            c.Ellipse(40 + tilt, 4, 10, 4.5f, orange);
            c.Ellipse(62 + tilt, 4, 10, 4.5f, orange);
            // Body
            c.Fill(new Rect(10, 0, 80, 84), (x, y) => SdEllipse(x - tilt * (y / 80f), y, 51, 38, 30, 36) - 3, Ink);
            c.Fill(new Rect(10, 0, 80, 84), (x, y) => SdEllipse(x - tilt * (y / 80f), y, 51, 38, 30, 36), navy);
            c.Fill(new Rect(20, 0, 60, 70), (x, y) => SdEllipse(x - tilt * (y / 80f), y, 46, 32, 20, 27), belly);
            // Flippers
            c.CapsuleOutlined(new Vector2(78 + tilt, 44), new Vector2(86 + tilt, 24), 5, navy, Ink, 2.5f);
            // Face (looking left)
            float hx = 42 + tilt * 0.8f;
            c.Circle(hx - 4, 56, 5, Color.white);
            c.Circle(hx + 10, 56, 5, Color.white);
            c.Circle(hx - 6, 55, 2.6f, Ink);
            c.Circle(hx + 8, 55, 2.6f, Ink);
            c.Capsule(new Vector2(hx - 11, 64), new Vector2(hx - 1, 61), 1.6f, Ink);
            c.Capsule(new Vector2(hx + 3, 61), new Vector2(hx + 13, 64), 1.6f, Ink);
            c.PolygonOutlined(new[] { new Vector2(hx + 2, 50), new Vector2(hx + 2, 43), new Vector2(hx - 12, 46) }, orange, Ink, 2f);
            // Scarf
            c.RoundRect(new Rect(24 + tilt, 38, 56, 7), 3.5f, Hex("#E53935"));
            return c;
        }

        static ArtCanvas MagmaBlob(int frame)
        {
            var c = new ArtCanvas(100, 100);
            Color rock = Hex("#3A2E33"), glow = Hex("#FF7A1A"), hot = Hex("#FFD23F");
            float squash = frame == 0 ? 0 : 5;
            float w = 38 + squash * 0.6f, h = 36 - squash;
            ArtCanvas.Sdf blob = (x, y) => Mathf.Max(SdEllipse(x, y, 50, 6, w, h + 30), 2 - y);
            c.Ellipse(50, 30, w + 10, h + 4, WithAlpha(glow, 0.2f));
            c.Fill(new Rect(0, 0, 100, 100), (x, y) => blob(x, y) - 3, Ink);
            c.Fill(new Rect(0, 0, 100, 100), blob, rock);
            // Glowing cracks
            c.Capsule(new Vector2(30, 14), new Vector2(40, 30), 2.2f, glow);
            c.Capsule(new Vector2(40, 30), new Vector2(36, 44 - squash), 1.8f, glow);
            c.Capsule(new Vector2(66, 10), new Vector2(72, 28), 2.2f, glow);
            c.Capsule(new Vector2(72, 28), new Vector2(64, 38 - squash), 1.8f, hot);
            // Flame crest
            float flick = frame == 0 ? 0 : 4;
            c.Fill(new Rect(30, 30, 40, 50), (x, y) => SdEllipse(x, y, 50, 58 - squash + flick * 0.5f, 10, 14 + flick), glow);
            c.Fill(new Rect(30, 30, 40, 50), (x, y) => SdEllipse(x, y, 50, 55 - squash, 5, 8 + flick * 0.5f), hot);
            AngryEyes(c, 50, 30 - squash * 0.5f, 12, 7, hot);
            return c;
        }

        static ArtCanvas Alien(int frame)
        {
            var c = new ArtCanvas(100, 100);
            Color slime = Hex("#7BE35A"), dark = Hex("#48A83A"), light = Hex("#B5F58F");
            float squash = frame == 0 ? 0 : 5;
            float w = 34 + squash * 0.7f, h = 40 - squash;
            ArtCanvas.Sdf blob = (x, y) => Mathf.Max(SdEllipse(x, y, 50, 4, w, h + 20), 2 - y);
            // Antenna
            float sway = frame == 0 ? -3 : 3;
            c.Capsule(new Vector2(50, 50 - squash), new Vector2(50 + sway, 74 - squash), 3.4f, Ink);
            c.Capsule(new Vector2(50, 50 - squash), new Vector2(50 + sway, 74 - squash), 1.8f, dark);
            c.CircleOutlined(50 + sway, 76 - squash, 5, Hex("#FFD23F"), Ink, 2);
            c.Fill(new Rect(0, 0, 100, 100), (x, y) => blob(x, y) - 3, Ink);
            c.Fill(new Rect(0, 0, 100, 100), blob, slime);
            c.Ellipse(38, 44 - squash, 10, 5, light);
            // Drips at the base
            c.Circle(20, 6, 4, dark);
            c.Circle(80, 5, 3.5f, dark);
            // One big eye looking left
            c.CircleOutlined(48, 30 - squash * 0.5f, 13, Color.white, Ink, 2.5f);
            c.Circle(43, 29 - squash * 0.5f, 6.5f, Hex("#6A4FD8"));
            c.Circle(42, 29 - squash * 0.5f, 3.4f, Ink);
            c.Circle(46, 33 - squash * 0.5f, 2, Color.white);
            c.Capsule(new Vector2(34, 46 - squash), new Vector2(60, 42 - squash), 2, Ink);
            return c;
        }

        static ArtCanvas GummyBear(int frame)
        {
            var c = new ArtCanvas(100, 100);
            Color gummy = Hex("#FF4F6D"), light = Hex("#FF9DB0"), dark = Hex("#D22E50");
            float bob = frame == 0 ? 0 : 3;
            float armLift = frame == 0 ? 0 : 6;
            // Legs
            EllipseOutlined(c, 38, 9, 10, 8, gummy, 2.5f);
            EllipseOutlined(c, 64, 9, 10, 8, gummy, 2.5f);
            // Body
            EllipseOutlined(c, 51, 28 + bob * 0.3f, 22, 20, gummy, 2.5f);
            c.Ellipse(51, 26, 13, 12, light);
            // Arms (raised, menacing)
            c.CapsuleOutlined(new Vector2(32, 34), new Vector2(20, 42 + armLift), 6, gummy, Ink, 2.5f);
            c.CapsuleOutlined(new Vector2(70, 34), new Vector2(82, 40 + armLift * 0.6f), 6, gummy, Ink, 2.5f);
            // Head
            float hy = 58 + bob;
            EllipseOutlined(c, 30, hy + 16, 7, 7, gummy, 2.5f);
            EllipseOutlined(c, 68, hy + 16, 7, 7, gummy, 2.5f);
            EllipseOutlined(c, 49, hy, 22, 18, gummy, 2.5f);
            c.Ellipse(42, hy + 9, 9, 4, WithAlpha(Color.white, 0.45f));
            c.Ellipse(44, hy - 6, 9, 6, light);
            c.Circle(44, hy - 3, 2.6f, dark);
            AngryEyes(c, 48, hy + 3, 9, 4.5f, Color.white);
            return c;
        }

        static ArtCanvas Crab(int frame)
        {
            var c = new ArtCanvas(100, 100);
            Color shell = Hex("#FF6B4A"), dark = Hex("#D24A2E"), light = Hex("#FFA285");
            float open = frame == 0 ? 0 : 5;
            float step = frame == 0 ? 0 : 2;
            // Legs
            for (int i = 0; i < 3; i++)
            {
                float lift = (i + frame) % 2 == 0 ? step : 0;
                c.Capsule(new Vector2(36 - i * 4, 14), new Vector2(18 - i * 5, 2 + lift), 3.4f, Ink);
                c.Capsule(new Vector2(36 - i * 4, 14), new Vector2(18 - i * 5, 2 + lift), 2f, dark);
                c.Capsule(new Vector2(64 + i * 4, 14), new Vector2(82 + i * 5, 2 + lift), 3.4f, Ink);
                c.Capsule(new Vector2(64 + i * 4, 14), new Vector2(82 + i * 5, 2 + lift), 2f, dark);
            }
            // Claws raised
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 arm0 = new(50 + side * 24, 26), arm1 = new(50 + side * 34, 46);
                c.CapsuleOutlined(arm0, arm1, 4, shell, Ink, 2.5f);
                Vector2 claw = arm1 + new Vector2(0, 10);
                c.Fill(new Rect(claw.x - 14, claw.y - 14, 28, 30),
                    (x, y) => Mathf.Max(SdCircle(x, y, claw.x, claw.y, 11) - 2.5f, -(SdPolygon(x, y, new[] { claw + new Vector2(0, 2), claw + new Vector2(-6 - open, 16), claw + new Vector2(6 + open, 16) }) - 0.5f)), Ink);
                c.Fill(new Rect(claw.x - 14, claw.y - 14, 28, 30),
                    (x, y) => Mathf.Max(SdCircle(x, y, claw.x, claw.y, 11), -SdPolygon(x, y, new[] { claw + new Vector2(0, 0), claw + new Vector2(-8 - open, 16), claw + new Vector2(8 + open, 16) })), shell);
            }
            // Body
            EllipseOutlined(c, 50, 22, 28, 15, shell, 2.5f);
            c.Ellipse(46, 28, 16, 5, light);
            // Eye stalks
            foreach (float ex in new[] { 42f, 58f })
            {
                c.Capsule(new Vector2(ex, 30), new Vector2(ex, 44), 2.4f, Ink);
                c.CircleOutlined(ex, 46, 5.5f, Color.white, Ink, 2);
                c.Circle(ex - 2, 46, 2.6f, Ink);
            }
            c.Capsule(new Vector2(44, 17), new Vector2(56, 17), 1.4f, Ink);
            return c;
        }

        static ArtCanvas Drone(int frame)
        {
            var c = new ArtCanvas(100, 100);
            Color hull = Hex("#241C3E"), trim = Hex("#3A3160"), cyan = Hex("#3DF2FF"), magenta = Hex("#FF3DCB");
            float hover = frame == 0 ? 0 : 3;
            float y0 = 10 + hover;
            // Thruster glow below
            c.Ellipse(50, y0 - 4, 18, 6, WithAlpha(magenta, 0.35f));
            c.Ellipse(50, y0 - 2, 9, 3, WithAlpha(Color.white, 0.6f));
            // Rotors
            float blade = frame == 0 ? 30 : 14;
            foreach (float rx in new[] { 20f, 80f })
            {
                c.Capsule(new Vector2(rx, y0 + 54), new Vector2(rx, y0 + 64), 2.6f, Ink);
                c.Capsule(new Vector2(rx - blade * 0.5f, y0 + 66), new Vector2(rx + blade * 0.5f, y0 + 66), 2.8f, WithAlpha(Hex("#BFC6D6"), 0.9f));
                c.Circle(rx, y0 + 66, 3.4f, Ink);
            }
            c.Capsule(new Vector2(20, y0 + 54), new Vector2(80, y0 + 54), 3.2f, Ink);
            // Hull
            c.RoundRectOutlined(new Rect(12, y0, 76, 54), 20, hull, Ink, 3);
            c.RoundRect(new Rect(20, y0 + 38, 60, 9), 4.5f, trim);
            // Visor with single eye looking left
            c.RoundRect(new Rect(16, y0 + 12, 56, 22), 11, Ink);
            c.RoundRect(new Rect(18, y0 + 14, 52, 18), 9, Hex("#0B061F"));
            c.Circle(34, y0 + 23, 8, WithAlpha(cyan, 0.35f));
            c.Circle(34, y0 + 23, 4.6f, cyan);
            c.Circle(33, y0 + 24, 1.8f, Color.white);
            c.RoundRect(new Rect(56, y0 + 19, 9, 8), 2.5f, magenta);
            // Antenna light
            c.CircleOutlined(74, y0 + 54, 3.6f, frame == 0 ? magenta : trim, Ink, 1.5f);
            return c;
        }
    }
}
