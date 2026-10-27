using System.IO;
using UnityEditor;
using UnityEngine;
using static OneBlock.EditorTools.Art.ArtCanvas;

namespace OneBlock.EditorTools.Art
{
    /// <summary>
    /// Candidate store icons, built to survive the 48 px browse grid: one subject, cropped large,
    /// on a ground colour that is not sky blue. Exports each at 512 plus a contact sheet that shows
    /// them at the sizes users actually see (96/64/48, on light and dark).
    /// Nothing here ships: output lands in StoreAssets/icon_variants/.
    /// </summary>
    public static class IconVariants
    {
        const int S = 1024;
        static readonly Color Ink = Hex("#1A1C2C");

        [MenuItem("One Block/Build/Export Icon Variants", priority = 81)]
        public static void Export()
        {
            string dir = Path.Combine(StoreAssetsTools.Root, "icon_variants");
            Directory.CreateDirectory(dir);

            var a = BigHero();
            var b = BlockMark();
            var c = TapMoment();

            Write(Downsample(a, 512, 512), Path.Combine(dir, "A_big_hero_512.png"));
            Write(Downsample(b, 512, 512), Path.Combine(dir, "B_block_mark_512.png"));
            Write(Downsample(c, 512, 512), Path.Combine(dir, "C_tap_moment_512.png"));
            Write(Sheet(a, b, c), Path.Combine(dir, "contact_sheet.png"));

            Debug.Log($"[One Block] Icon variants written to {dir}");
        }

        // ---------------------------------------------------------------- A: character forward

        /// <summary>The hero huge and centre stage, standing on a block that runs off the bottom edge.</summary>
        static ArtCanvas BigHero()
        {
            var c = new ArtCanvas(S, S);
            Color top = Hex("#4A2A8C"), bottom = Hex("#1C1038");
            c.Fill(new Rect(0, 0, S, S), (x, y) => -1f,
                   (x, y) => Color.Lerp(bottom, top, Mathf.SmoothStep(0f, 1f, y / S)));

            // Spotlight so the hero separates from the ground colour
            Glow(c, 512, 600, 470, Hex("#9B6BF0"), 0.42f);

            Rect block = new(512 - 340, -170, 680, 470);
            GoldBlock(c, block, 78, 20);

            c.Ellipse(512, 298, 190, 26, WithAlpha(Color.black, 0.22f));
            IconArt.StampBilinear(c, CharacterArt.IdleFrame(CharacterArt.Default), 505, 292, 4.6f);
            return c;
        }

        // ---------------------------------------------------------------- B: object forward

        /// <summary>The block treated as a logo mark: one shape, tight crop, dark ground for maximum gold contrast.</summary>
        static ArtCanvas BlockMark()
        {
            var c = new ArtCanvas(S, S);
            Color mid = Hex("#272B42"), edge = Hex("#101119");
            c.Fill(new Rect(0, 0, S, S), (x, y) => -1f, (x, y) =>
            {
                float d = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), new Vector2(512, 560)) / 720f);
                return Color.Lerp(mid, edge, Mathf.SmoothStep(0f, 1f, d));
            });

            Glow(c, 512, 440, 470, Hex("#FFC23A"), 0.20f);

            Rect block = new(512 - 292, 150, 584, 530);
            GoldBlock(c, block, 84, 20);

            c.Ellipse(512, 678, 140, 20, WithAlpha(Color.black, 0.25f));
            IconArt.StampBilinear(c, CharacterArt.IdleFrame(CharacterArt.Default), 505, 670, 2.4f);
            return c;
        }

        // ---------------------------------------------------------------- C: action forward

        /// <summary>The verb: a block snapping into place under the hero, inside a tap ring.</summary>
        static ArtCanvas TapMoment()
        {
            var c = new ArtCanvas(S, S);
            Color top = Hex("#F3A23C"), bottom = Hex("#B02F14");
            c.Fill(new Rect(0, 0, S, S), (x, y) => -1f,
                   (x, y) => Color.Lerp(bottom, top, Mathf.SmoothStep(0f, 1f, y / S)));

            c.Polygon(new[] { new Vector2(0, 0), new Vector2(S, 0), new Vector2(S, 430), new Vector2(0, 250) },
                      WithAlpha(Hex("#5E1A0C"), 0.45f));

            Ring(c, 725, 405, 250, 24, WithAlpha(Color.white, 0.9f));
            GoldBlock(c, new Rect(560, 250, 330, 310), 52, 18);

            IconArt.StampBilinear(c, CharacterArt.IdleFrame(CharacterArt.Default), 360, 330, 3.5f);
            return c;
        }

        // ---------------------------------------------------------------- shared pieces

        /// <summary>The in-game golden block drawn at an arbitrary size, proportions kept.</summary>
        static void GoldBlock(ArtCanvas c, Rect b, float radius, float outline)
        {
            Color gold = Hex("#FFC23A"), edge = Shade(gold, -0.24f), light = Shade(gold, 0.28f);
            c.RoundRect(new Rect(b.x - outline, b.y - outline, b.width + outline * 2, b.height + outline * 2),
                        radius + outline * 0.8f, Ink);
            c.RoundRect(b, radius, edge);
            c.RoundRect(new Rect(b.x, b.y + b.height * 0.12f, b.width, b.height * 0.88f), radius, Shade(gold, 0.08f));

            Rect inner = new(b.x + b.width * 0.076f, b.y + b.height * 0.112f, b.width * 0.848f, b.height * 0.808f);
            c.RoundRect(inner, radius * 0.63f,
                        (x, y) => Color.Lerp(gold, light, (y - inner.y) / inner.height * 0.5f));

            c.RoundRect(new Rect(b.x + b.width * 0.129f, b.yMax - b.height * 0.24f, b.width * 0.326f, b.height * 0.088f),
                        b.height * 0.044f, WithAlpha(Color.white, 0.6f));
            c.Circle(b.x + b.width * 0.545f, b.yMax - b.height * 0.196f, b.width * 0.042f, WithAlpha(Color.white, 0.5f));
        }

        /// <summary>Soft radial falloff, so the subject lifts off the ground colour without a visible disc edge.</summary>
        static void Glow(ArtCanvas c, float cx, float cy, float r, Color col, float strength) =>
            c.Fill(new Rect(cx - r, cy - r, r * 2, r * 2),
                   (x, y) => SdCircle(x, y, cx, cy, r),
                   (x, y) =>
                   {
                       float d = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / r);
                       return WithAlpha(col, strength * (1f - d) * (1f - d));
                   });

        static void Ring(ArtCanvas c, float cx, float cy, float r, float t, Color col) =>
            c.Fill(new Rect(cx - r - t, cy - r - t, (r + t) * 2, (r + t) * 2),
                   (x, y) => Mathf.Abs(SdCircle(x, y, cx, cy, r)) - t, col);

        // ---------------------------------------------------------------- contact sheet

        static ArtCanvas Sheet(ArtCanvas a, ArtCanvas b, ArtCanvas cc)
        {
            const int W = 1180, H = 640, Col = 360, M = 25, G = 25, Big = 320;
            var s = new ArtCanvas(W, H);
            s.RoundRect(new Rect(0, 0, W, H), 0, Color.white);

            var icons = new[] { a, b, cc };
            for (int i = 0; i < 3; i++)
                Blit(s, Downsample(icons[i], Big, Big), M + i * (Col + G) + (Col - Big) / 2, H - M - Big);

            // The sizes users actually see, on both store themes
            s.RoundRect(new Rect(M, 155, W - M * 2, 110), 16, Hex("#EDF0F4"));
            s.RoundRect(new Rect(M, 25, W - M * 2, 110), 16, Hex("#15161F"));

            int[] sizes = { 96, 64, 48 };
            for (int i = 0; i < 3; i++)
            {
                int x = M + i * (Col + G) + (Col - (96 + 64 + 48 + 36)) / 2;
                foreach (int sz in sizes)
                {
                    var small = Downsample(icons[i], sz, sz);
                    Blit(s, small, x, 210 - sz / 2);
                    Blit(s, small, x, 80 - sz / 2);
                    x += sz + 18;
                }
            }
            return s;
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Box-filtered downscale, so small sizes show what the store's own resampling will show.</summary>
        static ArtCanvas Downsample(ArtCanvas src, int w, int h)
        {
            var dst = new ArtCanvas(w, h);
            float fx = (float)src.Width / w, fy = (float)src.Height / h;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int x0 = (int)(x * fx), x1 = Mathf.Min(src.Width, Mathf.Max(x0 + 1, (int)((x + 1) * fx)));
                int y0 = (int)(y * fy), y1 = Mathf.Min(src.Height, Mathf.Max(y0 + 1, (int)((y + 1) * fy)));
                float r = 0, g = 0, bl = 0, a = 0;
                int n = 0;
                for (int yy = y0; yy < y1; yy++)
                for (int xx = x0; xx < x1; xx++)
                {
                    Color p = src.Get(xx, yy);
                    r += p.r * p.a; g += p.g * p.a; bl += p.b * p.a; a += p.a; n++;
                }
                if (n == 0 || a <= 0f) continue;
                dst.Blend(x, y, new Color(r / a, g / a, bl / a, 1f), a / n);
            }
            return dst;
        }

        static void Blit(ArtCanvas dst, ArtCanvas src, int ox, int oy) => dst.Stamp(src, ox, oy);

        static void Write(ArtCanvas c, string path) => File.WriteAllBytes(path, c.EncodePng());
    }
}
