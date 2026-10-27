using UnityEngine;
using static OneBlock.EditorTools.Art.ArtCanvas;

namespace OneBlock.EditorTools.Art
{
    /// <summary>
    /// UI: 9-sliced panels/buttons and white glyph icons (tinted in UI). Imported at PPU 100 with borders.
    /// </summary>
    public static class UIArt
    {
        const int I = 128; // icon canvas
        static readonly Color W = Color.white;

        public static void Generate()
        {
            // 9-slice frames
            var slice = new ArtGenerator.ImportOptions { Border = new Vector4(40, 40, 40, 40), IsUI = true };
            var buttonSlice = new ArtGenerator.ImportOptions { Border = new Vector4(40, 48, 40, 40), IsUI = true };
            ArtGenerator.Save(Panel(Hex("#1C2533"), 0.92f), "UI/panel_dark", slice);
            ArtGenerator.Save(Panel(Hex("#F3F6FA"), 1f), "UI/panel_light", slice);
            ArtGenerator.Save(Panel(Hex("#FFFFFF"), 1f), "UI/panel_white", slice);
            ArtGenerator.Save(Button(Hex("#46C04B")), "UI/button_green", buttonSlice);
            ArtGenerator.Save(Button(Hex("#E5484D")), "UI/button_red", buttonSlice);
            ArtGenerator.Save(Button(Hex("#FFC23A")), "UI/button_yellow", buttonSlice);
            ArtGenerator.Save(Button(Hex("#2F86F0")), "UI/button_blue", buttonSlice);
            ArtGenerator.Save(Button(Hex("#1C2533")), "UI/button_dark", buttonSlice);
            ArtGenerator.Save(PillWhite(), "UI/pill_white", new ArtGenerator.ImportOptions { Border = new Vector4(48, 48, 48, 48), IsUI = true });
            ArtGenerator.Save(Pill(), "UI/pill_dark", new ArtGenerator.ImportOptions { Border = new Vector4(48, 48, 48, 48), IsUI = true });
            ArtGenerator.Save(White(), "UI/white", new ArtGenerator.ImportOptions { PixelsPerUnit = 8, Border = new Vector4(2, 2, 2, 2) });

            var icon = new ArtGenerator.ImportOptions { IsUI = true };
            ArtGenerator.Save(Ring(), "UI/ring", icon);
            ArtGenerator.Save(Disc(), "UI/circle", icon);
            ArtGenerator.Save(Pause(), "UI/Icons/icon_pause", icon);
            ArtGenerator.Save(Play(), "UI/Icons/icon_play", icon);
            ArtGenerator.Save(Gear(), "UI/Icons/icon_settings", icon);
            ArtGenerator.Save(Stats(), "UI/Icons/icon_stats", icon);
            ArtGenerator.Save(Shirt(), "UI/Icons/icon_shop", icon);
            ArtGenerator.Save(Home(), "UI/Icons/icon_home", icon);
            ArtGenerator.Save(Close(), "UI/Icons/icon_close", icon);
            ArtGenerator.Save(Cart(), "UI/Icons/icon_cart", icon);
            ArtGenerator.Save(Music(), "UI/Icons/icon_music", icon);
            ArtGenerator.Save(Speaker(true), "UI/Icons/icon_sound_on", icon);
            ArtGenerator.Save(Speaker(false), "UI/Icons/icon_sound_off", icon);
            ArtGenerator.Save(Vibrate(), "UI/Icons/icon_vibrate", icon);
            ArtGenerator.Save(Trophy(), "UI/Icons/icon_trophy", icon);
            ArtGenerator.Save(Infinity(), "UI/Icons/icon_infinity", icon);
            ArtGenerator.Save(Target(), "UI/Icons/icon_target", icon);
            ArtGenerator.Save(Calendar(), "UI/Icons/icon_calendar", icon);
            ArtGenerator.Save(AdTv(), "UI/Icons/icon_ad", icon);
            ArtGenerator.Save(Retry(), "UI/Icons/icon_retry", icon);
            ArtGenerator.Save(Lock(), "UI/Icons/icon_lock", icon);
            ArtGenerator.Save(Check(), "UI/Icons/icon_check", icon);
            ArtGenerator.Save(Tap(), "UI/Icons/icon_tap", icon);
            ArtGenerator.Save(Crown(), "UI/Icons/icon_crown", icon);
            ArtGenerator.Save(WorldIcon(), "UI/Icons/icon_world", icon);
            ArtGenerator.Save(Flame(), "UI/Icons/icon_flame", icon);
            ArtGenerator.Save(MissionsIcon(), "UI/Icons/icon_missions", icon);

            // Full-colour HUD icons
            ArtGenerator.Save(PropArt.Coin(96, 1f, false), "UI/Icons/hud_coin", icon);
            ArtGenerator.Save(PropArt.Star(96), "UI/Icons/hud_star", icon);
            ArtGenerator.Save(PropArt.Heart(96), "UI/Icons/hud_heart", icon);
        }

        // ---------------------------------------------------------------- frames

        static ArtCanvas Panel(Color color, float alpha)
        {
            var c = new ArtCanvas(128, 128);
            c.RoundRect(new Rect(0, 0, 128, 128), 28, WithAlpha(color, alpha));
            return c;
        }

        static ArtCanvas Button(Color color)
        {
            var c = new ArtCanvas(128, 128);
            c.RoundRect(new Rect(0, 0, 128, 124), 28, Shade(color, -0.3f));          // lip
            c.RoundRect(new Rect(0, 10, 128, 118), 28, color);
            c.RoundRect(new Rect(10, 88, 108, 30), 14, WithAlpha(Color.white, 0.14f)); // gloss
            return c;
        }

        static ArtCanvas Ring()
        {
            var c = new ArtCanvas(256, 256);
            c.Fill(new Rect(0, 0, 256, 256), (x, y) => Mathf.Abs(SdCircle(x, y, 128, 128, 108)) - 14, W);
            return c;
        }

        static ArtCanvas Disc()
        {
            var c = new ArtCanvas(128, 128);
            c.Circle(64, 64, 62, W);
            return c;
        }

        static ArtCanvas White()
        {
            var c = new ArtCanvas(8, 8);
            c.RoundRect(new Rect(0, 0, 8, 8), 0, Color.white);
            return c;
        }

        static ArtCanvas PillWhite()
        {
            var c = new ArtCanvas(128, 128);
            c.RoundRect(new Rect(0, 0, 128, 128), 64, Color.white);
            return c;
        }

        static ArtCanvas Pill()
        {
            var c = new ArtCanvas(128, 128);
            c.RoundRect(new Rect(0, 0, 128, 128), 64, WithAlpha(Hex("#1C2533"), 0.85f));
            return c;
        }

        // ---------------------------------------------------------------- icons (white glyphs)

        static ArtCanvas Pause()
        {
            var c = new ArtCanvas(I, I);
            c.RoundRect(new Rect(34, 24, 22, 80), 7, W);
            c.RoundRect(new Rect(72, 24, 22, 80), 7, W);
            return c;
        }

        static ArtCanvas Play()
        {
            var c = new ArtCanvas(I, I);
            var tri = new[] { new Vector2(40, 22), new Vector2(40, 106), new Vector2(106, 64) };
            c.Fill(new Rect(30, 12, 90, 104), (x, y) => SdPolygon(x, y, tri) - 6, W);
            return c;
        }

        static ArtCanvas Gear()
        {
            var c = new ArtCanvas(I, I);
            c.Fill(new Rect(0, 0, I, I), (x, y) =>
            {
                float dx = x - 64, dy = y - 64;
                float a = Mathf.Atan2(dy, dx);
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float teeth = Mathf.Clamp(Mathf.Cos(a * 8) * 3f, -1f, 1f) * 7f;
                float outer = r - (42 + teeth);
                float hole = 16 - r;
                return Mathf.Max(outer, hole);
            }, W);
            return c;
        }

        static ArtCanvas Stats()
        {
            var c = new ArtCanvas(I, I);
            c.RoundRect(new Rect(22, 20, 22, 44), 5, W);
            c.RoundRect(new Rect(53, 20, 22, 88), 5, W);
            c.RoundRect(new Rect(84, 20, 22, 64), 5, W);
            return c;
        }

        static ArtCanvas Shirt()
        {
            var c = new ArtCanvas(I, I);
            var body = new[]
            {
                new Vector2(38, 18), new Vector2(90, 18), new Vector2(90, 72), new Vector2(110, 62),
                new Vector2(120, 88), new Vector2(84, 108), new Vector2(64, 96), new Vector2(44, 108),
                new Vector2(8, 88), new Vector2(18, 62), new Vector2(38, 72)
            };
            c.Fill(new Rect(0, 0, I, I), (x, y) => SdPolygon(x, y, body) - 3, W);
            return c;
        }

        static ArtCanvas Home()
        {
            var c = new ArtCanvas(I, I);
            var roof = new[] { new Vector2(14, 64), new Vector2(64, 110), new Vector2(114, 64) };
            c.Fill(new Rect(0, 0, I, I), (x, y) => SdPolygon(x, y, roof) - 4, W);
            c.RoundRect(new Rect(28, 18, 72, 54), 6, W);
            c.EraseShape(new Rect(52, 18, 24, 32), (x, y) => SdRoundRect(x, y, new Rect(53, 14, 22, 34), 4));
            return c;
        }

        static ArtCanvas Close()
        {
            var c = new ArtCanvas(I, I);
            c.Capsule(new Vector2(32, 32), new Vector2(96, 96), 11, W);
            c.Capsule(new Vector2(32, 96), new Vector2(96, 32), 11, W);
            return c;
        }

        static ArtCanvas Cart()
        {
            var c = new ArtCanvas(I, I);
            c.Capsule(new Vector2(12, 100), new Vector2(28, 100), 6, W);
            c.Capsule(new Vector2(28, 100), new Vector2(40, 46), 6, W);
            var basket = new[] { new Vector2(34, 84), new Vector2(114, 84), new Vector2(102, 44), new Vector2(42, 44) };
            c.Fill(new Rect(0, 0, I, I), (x, y) => SdPolygon(x, y, basket) - 3, W);
            c.Circle(50, 24, 10, W);
            c.Circle(96, 24, 10, W);
            return c;
        }

        static ArtCanvas Music()
        {
            var c = new ArtCanvas(I, I);
            c.Ellipse(40, 30, 17, 13, W);
            c.Ellipse(92, 42, 17, 13, W);
            c.Capsule(new Vector2(54, 32), new Vector2(54, 104), 5, W);
            c.Capsule(new Vector2(106, 44), new Vector2(106, 116), 5, W);
            var beam = new[] { new Vector2(49, 94), new Vector2(111, 106), new Vector2(111, 122), new Vector2(49, 110) };
            c.Polygon(beam, W);
            return c;
        }

        static ArtCanvas Speaker(bool on)
        {
            var c = new ArtCanvas(I, I);
            var body = new[] { new Vector2(12, 46), new Vector2(36, 46), new Vector2(66, 18), new Vector2(66, 110), new Vector2(36, 82), new Vector2(12, 82) };
            c.Fill(new Rect(0, 0, I, I), (x, y) => SdPolygon(x, y, body) - 3, W);
            if (on)
            {
                for (int i = 0; i < 2; i++)
                {
                    float r = 22 + i * 20;
                    c.Fill(new Rect(60, 0, 68, I), (x, y) =>
                    {
                        float d = Mathf.Abs(SdCircle(x, y, 70, 64, r)) - 5;
                        float ang = Mathf.Abs(Mathf.Atan2(y - 64, x - 70));
                        return ang > 0.85f ? Mathf.Max(d, (ang - 0.85f) * r) : d;
                    }, W);
                }
            }
            else
            {
                c.Capsule(new Vector2(82, 46), new Vector2(114, 82), 7, W);
                c.Capsule(new Vector2(82, 82), new Vector2(114, 46), 7, W);
            }
            return c;
        }

        static ArtCanvas Vibrate()
        {
            var c = new ArtCanvas(I, I);
            c.RoundRect(new Rect(40, 14, 48, 100), 10, W);
            c.EraseShape(new Rect(48, 26, 32, 76), (x, y) => SdRoundRect(x, y, new Rect(49, 28, 30, 72), 4));
            foreach (float s in new[] { -1f, 1f })
            {
                float bx = 64 + s * 38;
                c.Capsule(new Vector2(bx, 40), new Vector2(bx + s * 8, 52), 4, W);
                c.Capsule(new Vector2(bx + s * 8, 52), new Vector2(bx, 64), 4, W);
                c.Capsule(new Vector2(bx, 64), new Vector2(bx + s * 8, 76), 4, W);
                c.Capsule(new Vector2(bx + s * 8, 76), new Vector2(bx, 88), 4, W);
            }
            return c;
        }

        static ArtCanvas Trophy()
        {
            var c = new ArtCanvas(I, I);
            var cup = new[] { new Vector2(30, 112), new Vector2(98, 112), new Vector2(90, 70), new Vector2(64, 52), new Vector2(38, 70) };
            c.Fill(new Rect(0, 0, I, I), (x, y) => SdPolygon(x, y, cup) - 4, W);
            c.Fill(new Rect(0, 60, I, 60), (x, y) => Mathf.Abs(SdCircle(x, y, 28, 90, 14)) - 5, W);
            c.Fill(new Rect(0, 60, I, 60), (x, y) => Mathf.Abs(SdCircle(x, y, 100, 90, 14)) - 5, W);
            c.RoundRect(new Rect(56, 28, 16, 28), 3, W);
            c.RoundRect(new Rect(36, 12, 56, 18), 5, W);
            return c;
        }

        static ArtCanvas Infinity()
        {
            var c = new ArtCanvas(I, I);
            c.Fill(new Rect(0, 0, I, I), (x, y) =>
            {
                float d1 = Mathf.Abs(SdCircle(x, y, 40, 64, 22)) - 8;
                float d2 = Mathf.Abs(SdCircle(x, y, 88, 64, 22)) - 8;
                return Mathf.Min(d1, d2);
            }, W);
            return c;
        }

        static ArtCanvas Target()
        {
            var c = new ArtCanvas(I, I);
            c.Fill(new Rect(0, 0, I, I), (x, y) =>
            {
                float r = Mathf.Sqrt((x - 60) * (x - 60) + (y - 60) * (y - 60));
                float ring1 = Mathf.Abs(r - 44) - 7;
                float ring2 = Mathf.Abs(r - 22) - 7;
                float dot = r - 7;
                return Mathf.Min(ring1, Mathf.Min(ring2, dot));
            }, W);
            c.Capsule(new Vector2(62, 62), new Vector2(108, 108), 5, W);
            c.Polygon(new[] { new Vector2(96, 114), new Vector2(124, 124), new Vector2(114, 96) }, W);
            return c;
        }

        /// <summary>Clipboard with a tick.</summary>
        static ArtCanvas MissionsIcon()
        {
            var c = new ArtCanvas(I, I);
            c.RoundRect(new Rect(22, 8, 84, 104), 14, W);
            c.EraseShape(new Rect(30, 16, 68, 80), (x, y) => SdRoundRect(x, y, new Rect(32, 18, 64, 76), 6));
            c.RoundRect(new Rect(44, 100, 40, 22), 8, W);
            // Tick
            c.Capsule(new Vector2(44, 58), new Vector2(58, 42), 6, W);
            c.Capsule(new Vector2(58, 42), new Vector2(84, 74), 6, W);
            return c;
        }

        /// <summary>Full-colour streak flame.</summary>
        static ArtCanvas Flame()
        {
            var c = new ArtCanvas(I, I);
            ArtCanvas.Sdf Drop(float cx, float cy, float r, float tipY, float lean) => (x, y) =>
            {
                float body = SdCircle(x, y, cx, cy, r);
                var tri = new[] { new Vector2(cx - r * 0.92f, cy + r * 0.35f), new Vector2(cx + r * 0.92f, cy + r * 0.35f), new Vector2(cx + lean, tipY) };
                return Mathf.Min(body, SdPolygon(x, y, tri));
            };
            c.Fill(new Rect(0, 0, I, I), (x, y) => Drop(64, 44, 36, 124, 10)(x, y) - 4f, Hex("#8A2A12"));
            c.Fill(new Rect(0, 0, I, I), Drop(64, 44, 36, 124, 10), Hex("#FF6A1A"));
            c.Fill(new Rect(0, 0, I, I), Drop(66, 38, 22, 92, 4), Hex("#FFC23A"));
            c.Fill(new Rect(0, 0, I, I), Drop(67, 32, 11, 62, 2), Hex("#FFF2A8"));
            return c;
        }

        static ArtCanvas Calendar()
        {
            var c = new ArtCanvas(I, I);
            c.RoundRect(new Rect(16, 14, 96, 92), 14, W);
            c.EraseShape(new Rect(26, 24, 76, 56), (x, y) => SdRoundRect(x, y, new Rect(27, 24, 74, 54), 5));
            c.RoundRect(new Rect(34, 96, 14, 24), 6, W);
            c.RoundRect(new Rect(80, 96, 14, 24), 6, W);
            for (int r = 0; r < 2; r++)
            for (int k = 0; k < 3; k++)
                c.RoundRect(new Rect(36 + k * 22, 34 + r * 22, 13, 13), 3, W);
            return c;
        }

        static ArtCanvas AdTv()
        {
            var c = new ArtCanvas(I, I);
            c.RoundRect(new Rect(10, 22, 108, 80), 16, W);
            var tri = new[] { new Vector2(52, 42), new Vector2(52, 82), new Vector2(84, 62) };
            c.EraseShape(new Rect(40, 30, 56, 64), (x, y) => SdPolygon(x, y, tri) - 3);
            c.RoundRect(new Rect(40, 8, 48, 10), 5, W);
            return c;
        }

        static ArtCanvas Retry()
        {
            var c = new ArtCanvas(I, I);
            c.Fill(new Rect(0, 0, I, I), (x, y) =>
            {
                float d = Mathf.Abs(SdCircle(x, y, 64, 60, 38)) - 9;
                float ang = Mathf.Atan2(y - 60, x - 64); // gap near the top-right
                return ang > 0.2f && ang < 1.3f ? Mathf.Max(d, 12) : d;
            }, W);
            var head = new[] { new Vector2(84, 118), new Vector2(116, 90), new Vector2(76, 78) };
            c.Fill(new Rect(60, 60, 68, 68), (x, y) => SdPolygon(x, y, head) - 2, W);
            return c;
        }

        static ArtCanvas Lock()
        {
            var c = new ArtCanvas(I, I);
            c.RoundRect(new Rect(24, 14, 80, 64), 12, W);
            c.Fill(new Rect(20, 60, 88, 68), (x, y) =>
            {
                float d = Mathf.Abs(SdCircle(x, y, 64, 82, 26)) - 8;
                return y < 76 ? Mathf.Max(d, 1) : d;
            }, W);
            c.Capsule(new Vector2(38, 70), new Vector2(38, 82), 8, W);
            c.Capsule(new Vector2(90, 70), new Vector2(90, 82), 8, W);
            c.EraseShape(new Rect(54, 28, 20, 36), (x, y) => Mathf.Min(SdCircle(x, y, 64, 52, 8), SdRoundRect(x, y, new Rect(60, 30, 8, 20), 3)));
            return c;
        }

        static ArtCanvas Check()
        {
            var c = new ArtCanvas(I, I);
            c.Capsule(new Vector2(22, 66), new Vector2(50, 36), 11, W);
            c.Capsule(new Vector2(50, 36), new Vector2(106, 96), 11, W);
            return c;
        }

        static ArtCanvas Tap()
        {
            var c = new ArtCanvas(I, I);
            // Pointing finger
            c.Capsule(new Vector2(56, 44), new Vector2(56, 100), 10, W);
            c.RoundRect(new Rect(40, 8, 58, 56), 18, W);
            c.Capsule(new Vector2(74, 44), new Vector2(74, 64), 9, W);
            c.Capsule(new Vector2(90, 40), new Vector2(90, 58), 8, W);
            // Tap ripple
            c.Fill(new Rect(20, 80, 72, 48), (x, y) =>
            {
                float d = Mathf.Abs(SdCircle(x, y, 56, 106, 18)) - 3.5f;
                return y < 104 ? Mathf.Max(d, 104 - y) : d;
            }, W);
            return c;
        }

        static ArtCanvas WorldIcon()
        {
            var c = new ArtCanvas(I, I);
            c.Circle(92, 92, 16, W);
            c.Fill(new Rect(0, 0, I, I), (x, y) =>
                Mathf.Min(SdPolygon(x, y, new[] { new Vector2(6, 20), new Vector2(50, 84), new Vector2(92, 20) }),
                          SdPolygon(x, y, new[] { new Vector2(52, 20), new Vector2(84, 62), new Vector2(122, 20) })) - 3, W);
            return c;
        }

        static ArtCanvas Crown()
        {
            var c = new ArtCanvas(I, I);
            var crown = new[]
            {
                new Vector2(18, 24), new Vector2(110, 24), new Vector2(118, 92), new Vector2(88, 64),
                new Vector2(64, 106), new Vector2(40, 64), new Vector2(10, 92)
            };
            c.Fill(new Rect(0, 0, I, I), (x, y) => SdPolygon(x, y, crown) - 3, W);
            return c;
        }
    }
}
