using System.IO;
using UnityEditor;
using UnityEngine;
using static OneBlock.EditorTools.Art.ArtCanvas;

namespace OneBlock.EditorTools.Art
{
    /// <summary>
    /// Studio branding: a minimal "moon + bit" mark — a crescent moon with one square bit breaking away,
    /// drawn flat in the same rounded, high-contrast language as the game.
    /// Exports straight to Brand/ next to the project (social use, not shipped in the game).
    /// </summary>
    public static class BrandArt
    {
        const int S = 1024;
        const string OutDir = "Brand";

        public static readonly Color Ink = Hex("#141A2E");
        public static readonly Color Cream = Hex("#FFD66B");

        [MenuItem("One Block/Build/Generate Studio Logo", priority = 82)]
        public static void Generate()
        {
            Directory.CreateDirectory(OutDir);
            Write(MarkOnCircle(), "moonbit_profile_circle.png");   // Instagram / WhatsApp avatar
            Write(MarkOnTile(), "moonbit_tile.png");               // square tile, rounded corners
            Write(Mark(Cream, true), "moonbit_mark_cream.png");    // transparent, light on dark
            Write(Mark(Ink, true), "moonbit_mark_ink.png");        // transparent, dark on light
            Debug.Log($"[One Block] Studio logo written to {Path.GetFullPath(OutDir)}");
        }

        static void Write(ArtCanvas canvas, string file)
        {
            var tex = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, false);
            for (int y = 0; y < canvas.Height; y++)
            for (int x = 0; x < canvas.Width; x++)
                tex.SetPixel(x, y, canvas.Get(x, y));
            tex.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, file), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>Round avatar: Instagram crops to a circle, so the mark sits well inside.</summary>
        public static ArtCanvas MarkOnCircle()
        {
            var c = new ArtCanvas(S, S);
            c.Circle(S * 0.5f, S * 0.5f, S * 0.5f - 1, Ink);
            Draw(c, Cream, 0.72f);
            return c;
        }

        /// <summary>Square tile with rounded corners, for places that don't crop.</summary>
        public static ArtCanvas MarkOnTile()
        {
            var c = new ArtCanvas(S, S);
            c.RoundRect(new Rect(0, 0, S, S), 220, Ink);
            Draw(c, Cream, 0.78f);
            return c;
        }

        /// <summary>The bare mark on transparency in one flat colour.</summary>
        public static ArtCanvas Mark(Color color, bool inset = false)
        {
            var c = new ArtCanvas(S, S);
            Draw(c, color, inset ? 0.92f : 1f);
            return c;
        }

        /// <summary>
        /// Crescent = a disc with an offset disc punched out (negative space, no outline needed),
        /// plus a detached square "bit" lifting off the upper tip.
        /// </summary>
        static void Draw(ArtCanvas c, Color color, float scale)
        {
            float outer = S * 0.40f * scale;
            float inner = outer * 0.84f;
            float offX = outer * 0.42f, offY = outer * 0.14f;
            // The crescent's mass sits left of its circle, so nudge the whole mark right to look centred
            float cx = S * 0.5f + outer * 0.13f, cy = S * 0.5f;

            var bounds = new Rect(cx - outer - 4, cy - outer - 4, outer * 2 + 8, outer * 2 + 8);
            c.Fill(bounds, (x, y) =>
            {
                float d1 = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - outer;
                float d2 = inner - Mathf.Sqrt((x - (cx + offX)) * (x - (cx + offX)) + (y - (cy + offY)) * (y - (cy + offY)));
                return Mathf.Max(d1, d2);
            }, color);

            // A single square "bit" lifting away from the tip, with a clear gap so it reads as detached
            float bit = outer * 0.26f;
            Square(c, cx + offX * 1.02f, cy + outer * 1.12f, bit, 14f * Mathf.Deg2Rad, color, bit * 0.26f);
        }

        /// <summary>Rounded square rotated about its centre.</summary>
        static void Square(ArtCanvas c, float cx, float cy, float size, float angle, Color color, float radius)
        {
            float half = size * 0.5f;
            float cos = Mathf.Cos(-angle), sin = Mathf.Sin(-angle);
            var bounds = new Rect(cx - size, cy - size, size * 2, size * 2);
            c.Fill(bounds, (x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                float rx = dx * cos - dy * sin, ry = dx * sin + dy * cos;
                float qx = Mathf.Abs(rx) - (half - radius), qy = Mathf.Abs(ry) - (half - radius);
                float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            }, color);
        }
    }
}
