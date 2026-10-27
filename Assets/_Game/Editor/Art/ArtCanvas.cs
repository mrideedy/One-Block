using UnityEngine;

namespace OneBlock.EditorTools.Art
{
    /// <summary>
    /// Tiny anti-aliased vector rasteriser used to draw the game's flat art into textures.
    /// Coordinates are in pixels with origin at bottom-left (same as Unity textures).
    /// Every shape is a signed distance field, so edges come out smooth at any size.
    /// </summary>
    public class ArtCanvas
    {
        public readonly int Width;
        public readonly int Height;
        readonly Color[] _px;

        public ArtCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            _px = new Color[width * height];
        }

        // ---------------------------------------------------------------- output

        public Texture2D ToTexture()
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            tex.SetPixels(_px);
            tex.Apply();
            return tex;
        }

        public byte[] EncodePng()
        {
            var tex = ToTexture();
            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return bytes;
        }

        // ---------------------------------------------------------------- core blending

        public Color Get(int x, int y) => _px[y * Width + x];

        public void Blend(int x, int y, Color c, float coverage)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            float a = c.a * Mathf.Clamp01(coverage);
            if (a <= 0f) return;
            int i = y * Width + x;
            Color d = _px[i];
            float outA = a + d.a * (1f - a);
            if (outA <= 0f) return;
            Color o = (c * a + d * d.a * (1f - a)) / outA;
            o.a = outA;
            _px[i] = o;
        }

        /// <summary>Overwrite alpha to cut shapes out (e.g. dashed outlines, holes).</summary>
        public void Erase(int x, int y, float coverage)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            int i = y * Width + x;
            _px[i].a *= 1f - Mathf.Clamp01(coverage);
        }

        public delegate float Sdf(float x, float y);
        public delegate Color Shader(float x, float y);

        /// <summary>Fill the region where sdf &lt; 0, with 1px anti-aliasing.</summary>
        public void Fill(Rect bounds, Sdf sdf, Shader shader)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(bounds.xMin) - 2);
            int y0 = Mathf.Max(0, Mathf.FloorToInt(bounds.yMin) - 2);
            int x1 = Mathf.Min(Width - 1, Mathf.CeilToInt(bounds.xMax) + 2);
            int y1 = Mathf.Min(Height - 1, Mathf.CeilToInt(bounds.yMax) + 2);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float d = sdf(px, py);
                float cov = Mathf.Clamp01(0.5f - d);
                if (cov > 0f) Blend(x, y, shader(px, py), cov);
            }
        }

        public void Fill(Rect bounds, Sdf sdf, Color color) => Fill(bounds, sdf, (x, y) => color);

        public void EraseShape(Rect bounds, Sdf sdf)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(bounds.xMin) - 2);
            int y0 = Mathf.Max(0, Mathf.FloorToInt(bounds.yMin) - 2);
            int x1 = Mathf.Min(Width - 1, Mathf.CeilToInt(bounds.xMax) + 2);
            int y1 = Mathf.Min(Height - 1, Mathf.CeilToInt(bounds.yMax) + 2);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                Erase(x, y, Mathf.Clamp01(0.5f - sdf(x + 0.5f, y + 0.5f)));
        }

        // ---------------------------------------------------------------- shapes

        public static float SdRoundRect(float px, float py, Rect r, float radius)
        {
            radius = Mathf.Min(radius, Mathf.Min(r.width, r.height) * 0.5f);
            float cx = r.center.x, cy = r.center.y;
            float qx = Mathf.Abs(px - cx) - (r.width * 0.5f - radius);
            float qy = Mathf.Abs(py - cy) - (r.height * 0.5f - radius);
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        public static float SdCircle(float px, float py, float cx, float cy, float r)
        {
            float dx = px - cx, dy = py - cy;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        /// <summary>Approximate ellipse SDF (good enough for AA edges).</summary>
        public static float SdEllipse(float px, float py, float cx, float cy, float rx, float ry)
        {
            float nx = (px - cx) / rx, ny = (py - cy) / ry;
            float k = Mathf.Sqrt(nx * nx + ny * ny);
            return (k - 1f) * Mathf.Min(rx, ry);
        }

        public static float SdCapsule(float px, float py, Vector2 a, Vector2 b, float r)
        {
            Vector2 p = new Vector2(px, py);
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.0001f, Vector2.Dot(ba, ba)));
            return (pa - ba * h).magnitude - r;
        }

        /// <summary>Signed distance to a convex or concave polygon (even-odd).</summary>
        public static float SdPolygon(float px, float py, Vector2[] v)
        {
            Vector2 p = new Vector2(px, py);
            float d = Vector2.SqrMagnitude(p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        // ---------------------------------------------------------------- convenience

        public void RoundRect(Rect r, float radius, Color c) =>
            Fill(r, (x, y) => SdRoundRect(x, y, r, radius), c);

        public void RoundRect(Rect r, float radius, Shader s) =>
            Fill(r, (x, y) => SdRoundRect(x, y, r, radius), s);

        /// <summary>Filled rounded rect with an outline of the given thickness drawn inside its bounds.</summary>
        public void RoundRectOutlined(Rect r, float radius, Color fill, Color outline, float thickness)
        {
            RoundRect(r, radius, outline);
            Rect inner = new Rect(r.x + thickness, r.y + thickness, r.width - thickness * 2, r.height - thickness * 2);
            RoundRect(inner, Mathf.Max(0, radius - thickness), fill);
        }

        public void Circle(float cx, float cy, float r, Color c) =>
            Fill(new Rect(cx - r, cy - r, r * 2, r * 2), (x, y) => SdCircle(x, y, cx, cy, r), c);

        public void Ellipse(float cx, float cy, float rx, float ry, Color c) =>
            Fill(new Rect(cx - rx, cy - ry, rx * 2, ry * 2), (x, y) => SdEllipse(x, y, cx, cy, rx, ry), c);

        public void Capsule(Vector2 a, Vector2 b, float r, Color c)
        {
            Rect bounds = Rect.MinMaxRect(Mathf.Min(a.x, b.x) - r, Mathf.Min(a.y, b.y) - r,
                                          Mathf.Max(a.x, b.x) + r, Mathf.Max(a.y, b.y) + r);
            Fill(bounds, (x, y) => SdCapsule(x, y, a, b, r), c);
        }

        public void Polygon(Vector2[] v, Color c) => Polygon(v, (x, y) => c);

        public void Polygon(Vector2[] v, Shader s)
        {
            Fill(PolyBounds(v), (x, y) => SdPolygon(x, y, v), s);
        }

        /// <summary>Polygon with an outline: draws the shape grown by thickness in the outline colour first.</summary>
        public void PolygonOutlined(Vector2[] v, Color fill, Color outline, float thickness)
        {
            Rect b = PolyBounds(v);
            b.xMin -= thickness; b.yMin -= thickness; b.xMax += thickness; b.yMax += thickness;
            Fill(b, (x, y) => SdPolygon(x, y, v) - thickness, outline);
            Polygon(v, fill);
        }

        public void CapsuleOutlined(Vector2 a, Vector2 b, float r, Color fill, Color outline, float thickness)
        {
            Capsule(a, b, r + thickness, outline);
            Capsule(a, b, r, fill);
        }

        public void CircleOutlined(float cx, float cy, float r, Color fill, Color outline, float thickness)
        {
            Circle(cx, cy, r + thickness, outline);
            Circle(cx, cy, r, fill);
        }

        static Rect PolyBounds(Vector2[] v)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in v)
            {
                minX = Mathf.Min(minX, p.x); minY = Mathf.Min(minY, p.y);
                maxX = Mathf.Max(maxX, p.x); maxY = Mathf.Max(maxY, p.y);
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        /// <summary>Copies another canvas onto this one (alpha blended) at an offset, optionally mirrored.</summary>
        public void Stamp(ArtCanvas src, int ox, int oy, bool flipX = false)
        {
            for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
            {
                Color c = src.Get(flipX ? src.Width - 1 - x : x, y);
                if (c.a > 0f) Blend(ox + x, oy + y, new Color(c.r, c.g, c.b, 1f), c.a);
            }
        }

        // ---------------------------------------------------------------- colour helpers

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out Color c);
            return c;
        }

        public static Color Shade(Color c, float amount) =>
            amount >= 0 ? Color.Lerp(c, Color.white, amount) : Color.Lerp(c, Color.black, -amount);

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        /// <summary>Deterministic hash noise in [0,1) so regenerated art is identical every time.</summary>
        public static float Hash(int x, int y, int seed = 0)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 144665);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }
    }
}
