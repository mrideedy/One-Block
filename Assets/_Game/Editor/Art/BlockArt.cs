using UnityEngine;
using static OneBlock.EditorTools.Art.ArtCanvas;

namespace OneBlock.EditorTools.Art
{
    /// <summary>
    /// Placeable block skins (all cosmetic) and the dashed placement preview. 100x100 = one grid cell.
    /// </summary>
    public static class BlockArt
    {
        const int S = 100;

        public static void Generate()
        {
            var o = ArtGenerator.ImportOptions.Center;
            ArtGenerator.Save(Normal(), "Blocks/block_normal", o);
            ArtGenerator.Save(Bouncy(), "Blocks/block_bouncy", o);
            ArtGenerator.Save(Ice(), "Blocks/block_ice", o);
            ArtGenerator.Save(Explosive(), "Blocks/block_explosive", o);
            ArtGenerator.Save(Ghost(), "Blocks/block_ghost", o);
            ArtGenerator.Save(Stone(), "Blocks/block_stone", o);
            ArtGenerator.Save(Wood(), "Blocks/block_wood", o);
            ArtGenerator.Save(Grass(), "Blocks/block_grass", o);
            ArtGenerator.Save(Crystal(), "Blocks/block_crystal", o);
            ArtGenerator.Save(Galaxy(), "Blocks/block_galaxy", o);
            ArtGenerator.Save(Candy(), "Blocks/block_candy", o);
            ArtGenerator.Save(Coral(), "Blocks/block_coral", o);
            ArtGenerator.Save(Neon(), "Blocks/block_neon", o);
            ArtGenerator.Save(Preview(), "Blocks/block_preview", o);
            ArtGenerator.Save(Preview(Hex("#FF5A5A")), "Blocks/block_preview_blocked", o);
        }

        public static ArtCanvas ForId(string id) => id switch
        {
            "bouncy" => Bouncy(), "ice" => Ice(), "explosive" => Explosive(), "ghost" => Ghost(),
            "stone" => Stone(), "wood" => Wood(), "grass" => Grass(), "crystal" => Crystal(), "galaxy" => Galaxy(),
            "candy" => Candy(), "coral" => Coral(), "neon" => Neon(),
            _ => Normal()
        };

        /// <summary>Glossy bevelled block shared by most skins.</summary>
        static ArtCanvas Bevel(Color baseColor, float radius = 12f, float alpha = 1f)
        {
            var c = new ArtCanvas(S, S);
            Color edge = Shade(baseColor, -0.22f);
            Color light = Shade(baseColor, 0.28f);
            Rect outer = new(1, 1, S - 2, S - 2);
            Rect inner = new(8, 10, S - 16, S - 17);

            c.RoundRect(outer, radius, WithAlpha(edge, alpha));
            // Lit top rim
            c.RoundRect(new Rect(1, 12, S - 2, S - 13), radius, WithAlpha(Shade(baseColor, 0.1f), alpha));
            c.RoundRect(inner, radius * 0.6f, (x, y) =>
            {
                float t = (y - inner.y) / inner.height;
                return WithAlpha(Color.Lerp(baseColor, light, t * 0.45f), alpha);
            });
            // Shine
            c.RoundRect(new Rect(14, S - 22, 26, 7), 3.5f, WithAlpha(Color.white, 0.55f * alpha));
            c.Circle(48, S - 18.5f, 3.5f, WithAlpha(Color.white, 0.45f * alpha));
            return c;
        }

        static ArtCanvas Normal() => Bevel(Hex("#FFC23A"));

        static ArtCanvas Bouncy()
        {
            var c = Bevel(Hex("#2F86F0"));
            // Spring chevrons
            Color line = WithAlpha(Color.white, 0.75f);
            for (int i = 0; i < 2; i++)
            {
                float y = 34 + i * 16;
                c.Capsule(new Vector2(30, y), new Vector2(50, y + 10), 3.2f, line);
                c.Capsule(new Vector2(50, y + 10), new Vector2(70, y), 3.2f, line);
            }
            return c;
        }

        static ArtCanvas Ice()
        {
            var c = Bevel(Hex("#8ED3F5"), 14f, 0.92f);
            Color crack = WithAlpha(Color.white, 0.85f);
            c.Capsule(new Vector2(22, 70), new Vector2(40, 52), 1.8f, crack);
            c.Capsule(new Vector2(40, 52), new Vector2(34, 32), 1.8f, crack);
            c.Capsule(new Vector2(40, 52), new Vector2(60, 46), 1.8f, crack);
            c.Capsule(new Vector2(66, 78), new Vector2(78, 60), 1.8f, crack);
            // Frosty corner dots
            c.Circle(76, 28, 5, WithAlpha(Color.white, 0.5f));
            c.Circle(66, 22, 3, WithAlpha(Color.white, 0.4f));
            return c;
        }

        static ArtCanvas Explosive()
        {
            var c = Bevel(Hex("#E8453C"));
            Color dark = Hex("#7A1E1B");
            // Hazard bands
            c.RoundRect(new Rect(8, 44, 84, 12), 2, WithAlpha(dark, 0.55f));
            // Bomb emblem
            c.CircleOutlined(50, 50, 13, Hex("#2A2A36"), Hex("#FFD34D"), 3);
            c.Capsule(new Vector2(58, 60), new Vector2(66, 70), 2.2f, Hex("#FFD34D"));
            c.Circle(68, 72, 3.2f, Hex("#FFF2A8"));
            return c;
        }

        static ArtCanvas Ghost()
        {
            var c = Bevel(Hex("#9B5CF0"), 12f, 0.55f);
            // Dashed bright outline
            DashedOutline(c, new Rect(4, 4, S - 8, S - 8), 12, Hex("#D9B8FF"), 4f, 12f, 8f);
            // Little ghost face
            c.RoundRect(new Rect(38, 50, 7, 12), 3.2f, WithAlpha(Color.white, 0.9f));
            c.RoundRect(new Rect(55, 50, 7, 12), 3.2f, WithAlpha(Color.white, 0.9f));
            return c;
        }

        static ArtCanvas Stone()
        {
            var c = new ArtCanvas(S, S);
            Color mortar = Hex("#55596A");
            Color stone = Hex("#8C92A6");
            c.RoundRect(new Rect(1, 1, S - 2, S - 2), 10, mortar);
            // Brick pattern: 3 rows, offset joints
            float[] rowsY = { 6, 37, 68 };
            for (int r = 0; r < 3; r++)
            {
                float offset = r % 2 == 0 ? 0 : -24;
                for (float x = offset + 6; x < S - 4; x += 48)
                {
                    Rect brick = Rect.MinMaxRect(Mathf.Max(6, x), rowsY[r], Mathf.Min(S - 6, x + 44), rowsY[r] + 26);
                    if (brick.width < 6) continue;
                    c.RoundRect(brick, 4, (px, py) => py > brick.yMax - 6 ? Shade(stone, 0.18f) : stone);
                    c.RoundRect(new Rect(brick.x, brick.y, brick.width, 4), 2, Shade(stone, -0.18f));
                }
            }
            return c;
        }

        static ArtCanvas Wood()
        {
            var c = new ArtCanvas(S, S);
            Color frame = Hex("#6B3F22");
            Color plank = Hex("#B8743E");
            c.RoundRect(new Rect(1, 1, S - 2, S - 2), 10, frame);
            for (int i = 0; i < 3; i++)
            {
                Rect p = new(9, 9 + i * 28, S - 18, 24);
                c.RoundRect(p, 4, (x, y) =>
                {
                    float grain = Mathf.Sin(x * 0.18f + i * 2f + Mathf.Sin(y * 0.5f) * 0.8f);
                    return Color.Lerp(plank, Shade(plank, -0.12f), grain * 0.5f + 0.5f) ;
                });
                c.RoundRect(new Rect(p.x, p.yMax - 4, p.width, 4), 2, Shade(plank, 0.2f));
            }
            // Nails
            foreach (var n in new[] { new Vector2(16, 16), new Vector2(84, 16), new Vector2(16, 84), new Vector2(84, 84) })
                c.Circle(n.x, n.y, 3, Hex("#3A2614"));
            return c;
        }

        static ArtCanvas Grass()
        {
            var c = new ArtCanvas(S, S);
            Color dirt = Hex("#6A4A3A");
            c.RoundRect(new Rect(1, 1, S - 2, S - 2), 10, dirt);
            for (int i = 0; i < 9; i++)
            {
                float x = 10 + Hash(i, 3, 7) * 76, y = 8 + Hash(i, 9, 7) * 50;
                c.RoundRect(new Rect(x, y, 9, 7), 2, Shade(dirt, Hash(i, 1, 2) > 0.5f ? -0.15f : 0.12f));
            }
            c.RoundRect(new Rect(1, 64, S - 2, 35), 10, Hex("#5DBE3C"));
            c.RoundRect(new Rect(1, 86, S - 2, 13), 6, Hex("#86D852"));
            for (int i = 0; i < 6; i++)
                c.Ellipse(9 + i * 16.5f, 66, 7, 6, Hex("#5DBE3C"));
            return c;
        }

        static ArtCanvas Crystal()
        {
            var c = Bevel(Hex("#3AA8F2"), 12f);
            Color facet = WithAlpha(Color.white, 0.35f);
            c.Polygon(new[] { new Vector2(12, 14), new Vector2(50, 50), new Vector2(12, 86) }, facet);
            c.Polygon(new[] { new Vector2(88, 14), new Vector2(50, 50), new Vector2(88, 86) }, WithAlpha(Hex("#0D4F9E"), 0.35f));
            return c;
        }

        static ArtCanvas Galaxy()
        {
            var c = new ArtCanvas(S, S);
            c.RoundRect(new Rect(1, 1, S - 2, S - 2), 12, Hex("#8B5CF6"));
            Rect inner = new(7, 7, S - 14, S - 14);
            c.RoundRect(inner, 8, (x, y) =>
            {
                float t = (x + y) / (2f * S);
                Color a = Hex("#2A1466"), b = Hex("#4A1F8F");
                float swirl = Mathf.Sin((x - 50) * 0.06f + (y - 50) * 0.04f) * 0.5f + 0.5f;
                return Color.Lerp(Color.Lerp(a, b, t), Hex("#7A3FD1"), swirl * 0.35f);
            });
            for (int i = 0; i < 14; i++)
            {
                float x = 12 + Hash(i, 5, 11) * 76, y = 12 + Hash(i, 8, 11) * 76;
                float r = 0.8f + Hash(i, 2, 11) * 1.8f;
                c.Circle(x, y, r, WithAlpha(Color.white, 0.6f + Hash(i, 4, 1) * 0.4f));
            }
            return c;
        }

        static ArtCanvas Candy()
        {
            var c = Bevel(Hex("#FF7FB6"));
            // Icing drip across the top and sprinkles
            c.RoundRect(new Rect(1, 70, S - 2, 29), 12, Color.white);
            for (int i = 0; i < 4; i++)
            {
                float x = 16 + i * 22.5f, depth = i % 2 == 0 ? 22 : 12;
                c.Capsule(new Vector2(x, 76), new Vector2(x, 76 - depth), 7, Color.white);
            }
            c.RoundRect(new Rect(14, 84, 22, 6), 3, WithAlpha(Hex("#FFD6EA"), 0.9f));
            var cols = new[] { Hex("#7FE3D0"), Hex("#FFD23F"), Hex("#8A6CFF"), Hex("#FF5A5A") };
            for (int i = 0; i < 8; i++)
            {
                float x = 16 + Hash(i, 1, 21) * 66, y = 16 + Hash(i, 2, 21) * 34;
                float a = Hash(i, 3, 21) * Mathf.PI;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 4f;
                c.Capsule(new Vector2(x, y) - d, new Vector2(x, y) + d, 2.2f, cols[i % cols.Length]);
            }
            return c;
        }

        static ArtCanvas Coral()
        {
            var c = Bevel(Hex("#FF7A6B"));
            Color pore = Hex("#D9534A");
            for (int i = 0; i < 10; i++)
            {
                float x = 16 + Hash(i, 4, 33) * 68, y = 14 + Hash(i, 5, 33) * 66;
                float r = 3 + Hash(i, 6, 33) * 4;
                c.Circle(x, y, r, pore);
                c.Circle(x - r * 0.3f, y + r * 0.3f, r * 0.45f, Shade(pore, -0.2f));
            }
            // A little bubble
            c.Fill(new Rect(66, 58, 22, 22), (x, y) => Mathf.Abs(SdCircle(x, y, 77, 69, 8)) - 1.5f, WithAlpha(Color.white, 0.85f));
            c.Circle(74, 72, 2, Color.white);
            return c;
        }

        static ArtCanvas Neon()
        {
            var c = new ArtCanvas(S, S);
            Color cyan = Hex("#3DF2FF"), magenta = Hex("#FF3DCB");
            c.RoundRect(new Rect(1, 1, S - 2, S - 2), 12, Hex("#120C24"));
            c.Fill(new Rect(0, 0, S, S), (x, y) => Mathf.Abs(SdRoundRect(x, y, new Rect(7, 7, S - 14, S - 14), 8)) - 4f, WithAlpha(cyan, 0.35f));
            c.Fill(new Rect(0, 0, S, S), (x, y) => Mathf.Abs(SdRoundRect(x, y, new Rect(7, 7, S - 14, S - 14), 8)) - 1.8f, cyan);
            c.Fill(new Rect(0, 0, S, S), (x, y) => Mathf.Abs(SdRoundRect(x, y, new Rect(7, 7, S - 14, S - 14), 8)) - 0.6f, Color.white);
            // Magenta diamond core
            var diamond = new[] { new Vector2(50, 26), new Vector2(74, 50), new Vector2(50, 74), new Vector2(26, 50) };
            c.Polygon(diamond, WithAlpha(magenta, 0.3f));
            c.Fill(new Rect(20, 20, 60, 60), (x, y) => Mathf.Abs(Mathf.Abs(x - 50) + Mathf.Abs(y - 50) - 20f) / 1.414f - 1.6f, magenta);
            c.RoundRect(new Rect(14, S - 20, 20, 4), 2, WithAlpha(Color.white, 0.4f));
            return c;
        }

        static ArtCanvas Preview(Color? tint = null)
        {
            var c = new ArtCanvas(S, S);
            Color col = tint ?? Color.white;
            c.RoundRect(new Rect(6, 6, S - 12, S - 12), 10, WithAlpha(col, 0.14f));
            DashedOutline(c, new Rect(6, 6, S - 12, S - 12), 10, WithAlpha(col, 0.95f), 4.5f, 14f, 9f);
            return c;
        }

        /// <summary>Draws a dashed stroke centred on a rounded rectangle's edge.</summary>
        public static void DashedOutline(ArtCanvas c, Rect r, float radius, Color color, float thickness, float dash, float gap)
        {
            float period = dash + gap;
            c.Fill(new Rect(r.x - thickness, r.y - thickness, r.width + thickness * 2, r.height + thickness * 2), (x, y) =>
            {
                float d = Mathf.Abs(SdRoundRect(x, y, r, radius)) - thickness * 0.5f;
                // Arc-length approximation via perimeter parameter around the rect centre
                float px = x - r.center.x, py = y - r.center.y;
                float hw = r.width * 0.5f, hh = r.height * 0.5f;
                float s;
                if (Mathf.Abs(px) / hw > Mathf.Abs(py) / hh)
                    s = px > 0 ? hh + py : 3 * hh + 2 * hw - py;          // right / left sides
                else
                    s = py > 0 ? 2 * hh + hw - px : 4 * hh + 3 * hw + px; // top / bottom sides
                float m = Mathf.Repeat(s + dash * 0.5f, period);
                float dashD = m < dash ? 0f : Mathf.Min(m - dash, period - m);
                return Mathf.Max(d, dashD - 0.01f);
            }, color);
        }
    }
}
