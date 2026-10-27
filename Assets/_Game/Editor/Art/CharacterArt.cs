using System.Collections.Generic;
using UnityEngine;
using static OneBlock.EditorTools.Art.ArtCanvas;

namespace OneBlock.EditorTools.Art
{
    /// <summary>
    /// The caped hero. 128x128 frames, facing right, feet on the bottom edge (pivot bottom-centre).
    /// A <see cref="CharacterStyle"/> drives colours and headwear so skins reuse the same poses.
    /// </summary>
    public static class CharacterArt
    {
        const int Size = 128;
        const float Outline = 2.6f;
        static readonly Color Ink = Hex("#1A1C2C");

        public enum Headwear
        {
            Hair,
            NinjaHood,
            SpaceHelmet,
            RobotHead,
            WizardGhost
        }

        public class CharacterStyle
        {
            public string Id;
            public Headwear Head = Headwear.Hair;
            public bool HasLegs = true;
            public Color Accent = Hex("#E43B3B");   // headband, antenna tip, hat star...
            public Color Visor = Hex("#5B3FA8");
            public bool Backpack;
            public Color Skin = Hex("#F5C69B");
            public Color Hair = Hex("#22222C");
            public Color Shirt = Hex("#2F6CD6");
            public Color Pants = Hex("#1F2A48");
            public Color Shoes = Hex("#15161F");
            public Color Cape = Hex("#E43B3B");
            public bool HasCape = true;
            public Color Eye = Hex("#15161F");
        }

        struct Pose
        {
            public Vector2 FrontFoot, BackFoot;
            public Vector2 FrontHand, BackHand;
            public float Bob;       // vertical body offset
            public float CapeLift;  // 0 = hanging, 1 = streaming behind
            public float CapePhase;
            public bool Hurt;
        }

        public static CharacterStyle Default => new CharacterStyle { Id = "default" };

        public static CharacterStyle Ninja => new CharacterStyle
        {
            Id = "ninja", Head = Headwear.NinjaHood,
            Hair = Hex("#23232E"), Shirt = Hex("#2B2B38"), Pants = Hex("#1B1B24"), Shoes = Hex("#101016"),
            Cape = Hex("#D8322F"), Accent = Hex("#D8322F")
        };

        public static CharacterStyle Astronaut => new CharacterStyle
        {
            Id = "astronaut", Head = Headwear.SpaceHelmet, HasCape = false, Backpack = true,
            Shirt = Hex("#E9ECF4"), Pants = Hex("#D5DAE6"), Shoes = Hex("#8C93A6"),
            Accent = Hex("#FF8A3D"), Visor = Hex("#5B3FA8")
        };

        public static CharacterStyle Robot => new CharacterStyle
        {
            Id = "robot", Head = Headwear.RobotHead, HasCape = false,
            Skin = Hex("#A7B0C2"), Shirt = Hex("#7F899E"), Pants = Hex("#50586B"), Shoes = Hex("#2E3340"),
            Accent = Hex("#FF4D4D"), Eye = Hex("#FFB23A")
        };

        public static CharacterStyle GhostWizard => new CharacterStyle
        {
            Id = "ghost", Head = Headwear.WizardGhost, HasLegs = false, HasCape = false,
            Skin = Hex("#F4F5FF"), Shirt = Hex("#F4F5FF"), Accent = Hex("#FFD23F"), Visor = Hex("#6A4FD8"),
            Eye = Hex("#1A1C2C")
        };

        // Ad-bundle heroes
        public static CharacterStyle CandyKid => new CharacterStyle
        {
            Id = "candy", Hair = Hex("#FF6FAE"), Shirt = Hex("#7FE3D0"), Pants = Hex("#FF9FCF"), Shoes = Hex("#E0619F"),
            Cape = Hex("#FFC4E1"), Accent = Hex("#FFD23F")
        };

        public static CharacterStyle Diver => new CharacterStyle
        {
            Id = "diver", Head = Headwear.SpaceHelmet, HasCape = false, Backpack = true,
            Shirt = Hex("#1E9AA8"), Pants = Hex("#146D78"), Shoes = Hex("#FFB23A"),
            Accent = Hex("#FFB23A"), Visor = Hex("#2F9BD6")
        };

        public static CharacterStyle CyberNinja => new CharacterStyle
        {
            Id = "cyber", Head = Headwear.NinjaHood,
            Hair = Hex("#1C1630"), Shirt = Hex("#241C3E"), Pants = Hex("#16112A"), Shoes = Hex("#3DF2FF"),
            Cape = Hex("#FF3DCB"), Accent = Hex("#3DF2FF")
        };

        public static CharacterStyle[] All => new[] { Default, Ninja, Astronaut, Robot, GhostWizard, CandyKid, Diver, CyberNinja };

        public static void Generate()
        {
            foreach (var style in All) GenerateSet(style);
        }

        public static void GenerateSet(CharacterStyle s)
        {
            string dir = $"Characters/{s.Id}";
            // PPU 84 renders the hero ~1.3 cells tall, matching the reference's proportions against blocks
            var o = new ArtGenerator.ImportOptions { Pivot = new Vector2(0.5f, 0f), PixelsPerUnit = 84 };

            ArtGenerator.Save(Draw(s, Idle(0f)), $"{dir}/{s.Id}_idle_0", o);
            ArtGenerator.Save(Draw(s, Idle(1f)), $"{dir}/{s.Id}_idle_1", o);

            // 4-frame run cycle: contact, passing, contact (mirrored legs), passing
            ArtGenerator.Save(Draw(s, new Pose
            {
                FrontFoot = new(86, 8), BackFoot = new(42, 16),
                FrontHand = new(50, 40), BackHand = new(86, 46),
                Bob = 0, CapeLift = 0.9f, CapePhase = 0f
            }), $"{dir}/{s.Id}_run_0", o);
            ArtGenerator.Save(Draw(s, new Pose
            {
                FrontFoot = new(66, 6), BackFoot = new(56, 26),
                FrontHand = new(70, 37), BackHand = new(62, 38),
                Bob = 3, CapeLift = 0.85f, CapePhase = 1.6f
            }), $"{dir}/{s.Id}_run_1", o);
            ArtGenerator.Save(Draw(s, new Pose
            {
                FrontFoot = new(42, 16), BackFoot = new(86, 8),
                FrontHand = new(86, 46), BackHand = new(50, 40),
                Bob = 0, CapeLift = 0.9f, CapePhase = 3.2f
            }), $"{dir}/{s.Id}_run_2", o);
            ArtGenerator.Save(Draw(s, new Pose
            {
                FrontFoot = new(56, 26), BackFoot = new(66, 6),
                FrontHand = new(62, 38), BackHand = new(70, 37),
                Bob = 3, CapeLift = 0.85f, CapePhase = 4.8f
            }), $"{dir}/{s.Id}_run_3", o);

            ArtGenerator.Save(Draw(s, new Pose
            {
                FrontFoot = new(80, 22), BackFoot = new(54, 14),
                FrontHand = new(90, 62), BackHand = new(44, 44),
                Bob = 6, CapeLift = 0.45f, CapePhase = 0.8f
            }), $"{dir}/{s.Id}_jump", o);
            ArtGenerator.Save(Draw(s, new Pose
            {
                FrontFoot = new(76, 10), BackFoot = new(52, 16),
                FrontHand = new(92, 66), BackHand = new(40, 66),
                Bob = 4, CapeLift = 1.25f, CapePhase = 2.2f
            }), $"{dir}/{s.Id}_fall", o);
            ArtGenerator.Save(Draw(s, new Pose
            {
                FrontFoot = new(78, 12), BackFoot = new(50, 12),
                FrontHand = new(92, 62), BackHand = new(38, 62),
                Bob = 2, CapeLift = 0.2f, CapePhase = 0f, Hurt = true
            }), $"{dir}/{s.Id}_hit", o);

            // Large hero art for menus (2x resolution idle)
            ArtGenerator.Save(Upscale(Draw(s, Idle(0f)), 2), $"{dir}/{s.Id}_portrait",
                new ArtGenerator.ImportOptions { Pivot = new Vector2(0.5f, 0f), IsUI = true });
        }

        public static ArtCanvas IdleFrame(CharacterStyle s) => Draw(s, Idle(0f));

        static Pose Idle(float breath) => new Pose
        {
            FrontFoot = new(72, 6), BackFoot = new(56, 6),
            FrontHand = new(76, 34 + breath), BackHand = new(54, 35 + breath),
            Bob = breath, CapeLift = 0.12f, CapePhase = breath * 1.5f
        };

        static ArtCanvas Draw(CharacterStyle s, Pose p)
        {
            var c = new ArtCanvas(Size, Size);
            float b = p.Bob;

            Vector2 hip = new(64, 38 + b);
            Vector2 shoulderFront = new(70, 56 + b);
            Vector2 shoulderBack = new(58, 56 + b);

            if (!s.HasLegs)
            {
                DrawGhost(c, s, p);
                return c;
            }

            if (s.HasCape) DrawCape(c, s, new Vector2(60, 64 + b), p);
            if (s.Backpack)
            {
                Rect pack = new(37, 36 + b, 16, 28);
                c.RoundRect(Grow(pack, Outline), 6 + Outline, Ink);
                c.RoundRect(pack, 6, Shade(s.Shirt, -0.2f));
                c.RoundRect(new Rect(40, 44 + b, 10, 4), 2, s.Accent);
            }

            // Back limbs (slightly darker so the silhouette reads as depth)
            DrawArm(c, s, shoulderBack, p.BackHand, back: true);
            DrawLeg(c, s, hip + new Vector2(-4, 0), p.BackFoot, back: true);

            DrawLeg(c, s, hip + new Vector2(4, 0), p.FrontFoot, back: false);

            // Torso
            Rect torso = new(49, 34 + b, 31, 32);
            c.RoundRect(Grow(torso, Outline), 9 + Outline, Ink);
            c.RoundRect(torso, 9, (x, y) => y < torso.y + 9 ? Shade(s.Shirt, -0.18f) : s.Shirt);
            // Belt line
            c.RoundRect(new Rect(49, 38 + b, 31, 4), 1, Shade(s.Pants, 0.1f));

            DrawHead(c, s, new Vector2(66, 64 + b), p.Hurt);

            DrawArm(c, s, shoulderFront, p.FrontHand, back: false);
            return c;
        }

        static void DrawLeg(ArtCanvas c, CharacterStyle s, Vector2 hip, Vector2 foot, bool back)
        {
            Color pants = back ? Shade(s.Pants, -0.25f) : s.Pants;
            Vector2 ankle = foot + new Vector2(0, 7);
            c.CapsuleOutlined(hip, ankle, 6.2f, pants, Ink, Outline);
            // Shoe: rounded toe pointing forward
            Rect shoe = new(foot.x - 7, foot.y, 17, 10);
            c.RoundRect(Grow(shoe, Outline), 5 + Outline, Ink);
            c.RoundRect(shoe, 5, back ? Shade(s.Shoes, 0.05f) : Shade(s.Shoes, 0.15f));
        }

        static void DrawArm(ArtCanvas c, CharacterStyle s, Vector2 shoulder, Vector2 hand, bool back)
        {
            Color sleeve = back ? Shade(s.Shirt, -0.28f) : Shade(s.Shirt, 0.05f);
            Vector2 dir = (hand - shoulder).normalized;
            Vector2 elbow = Vector2.Lerp(shoulder, hand, 0.55f);
            c.CapsuleOutlined(shoulder, elbow, 5.4f, sleeve, Ink, Outline);
            c.CapsuleOutlined(elbow - dir * 2, hand, 4.4f, back ? Shade(s.Skin, -0.12f) : s.Skin, Ink, Outline);
        }

        static void DrawCape(ArtCanvas c, CharacterStyle s, Vector2 anchor, Pose p)
        {
            // Direction blends from hanging (down-back) to streaming (back, slightly up)
            Vector2 hang = new Vector2(-0.28f, -1f).normalized;
            Vector2 stream = new Vector2(-1f, 0.12f).normalized;
            Vector2 dir = Vector2.Lerp(hang, stream, Mathf.Clamp01(p.CapeLift)).normalized;
            if (p.CapeLift > 1f) dir = Vector2.Lerp(stream, new Vector2(-0.6f, 0.8f).normalized, p.CapeLift - 1f).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);

            const int n = 10;
            float length = 50f;
            var top = new Vector2[n + 1];
            var bottom = new Vector2[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float wave = Mathf.Sin(t * Mathf.PI * 1.6f + p.CapePhase) * 4.5f * t;
                Vector2 center = anchor + dir * length * t + perp * wave;
                float w = 4f + 10f * t;
                top[i] = center + perp * w;
                bottom[i] = center - perp * w;
            }
            var poly = new Vector2[(n + 1) * 2 + 1];
            for (int i = 0; i <= n; i++) poly[i] = top[i];
            // Notched tip
            poly[n + 1] = anchor + dir * (length - 7f);
            for (int i = 0; i <= n; i++) poly[n + 2 + i] = bottom[n - i];

            Color dark = Shade(s.Cape, -0.25f);
            c.PolygonOutlined(poly, s.Cape, Ink, Outline);
            // Inner fold shading on the lower half
            var shade = new Vector2[n + 2];
            for (int i = 0; i <= n; i++) shade[i] = Vector2.Lerp(top[i], bottom[i], 0.55f);
            shade[n + 1] = anchor;
            var fold = new Vector2[(n + 1) * 2];
            for (int i = 0; i <= n; i++) fold[i] = shade[i];
            for (int i = 0; i <= n; i++) fold[n + 1 + i] = bottom[n - i];
            c.Polygon(fold, WithAlpha(dark, 0.9f));
        }

        static void DrawHead(ArtCanvas c, CharacterStyle s, Vector2 neck, bool hurt)
        {
            switch (s.Head)
            {
                case Headwear.NinjaHood: DrawNinjaHead(c, s, neck, hurt); return;
                case Headwear.SpaceHelmet: DrawHelmetHead(c, s, neck, hurt); return;
                case Headwear.RobotHead: DrawRobotHead(c, s, neck, hurt); return;
            }

            // Head block sits on the neck; big chibi proportions
            Rect head = new(neck.x - 22, neck.y - 2, 46, 46);
            Color skinShade = Shade(s.Skin, -0.14f);

            // Hair back mass (outlined), drawn first so the face overlaps it
            Rect hairBack = new(head.x - 3, head.y + 16, 16, 34);
            c.RoundRect(Grow(hairBack, Outline), 10 + Outline, Ink);
            c.RoundRect(Grow(head, Outline), 11 + Outline, Ink);
            c.RoundRect(hairBack, 10, s.Hair);

            c.RoundRect(head, 11, (x, y) => y < head.y + 8 ? skinShade : s.Skin);
            // Back of the head is hair from the ear backwards
            c.RoundRect(new Rect(head.x, head.y + 10, 13, 30), 9, s.Hair);

            // Hair cap with a spiky fringe over the forehead
            Rect cap = new(head.x - 3, head.y + 27, head.width + 6, 22);
            var fringe = new[]
            {
                new Vector2(head.xMax + 3, head.y + 33),
                new Vector2(head.xMax - 3, head.y + 22),
                new Vector2(head.xMax - 9, head.y + 29),
                new Vector2(head.xMax - 15, head.y + 21),
                new Vector2(head.xMax - 21, head.y + 30),
                new Vector2(head.x + 14, head.y + 24),
                new Vector2(head.x + 10, head.y + 34),
            };
            var fringePoly = new Vector2[fringe.Length + 2];
            fringePoly[0] = new Vector2(head.x - 2, head.y + 36);
            for (int i = 0; i < fringe.Length; i++) fringePoly[fringe.Length - i] = fringe[i];
            fringePoly[fringe.Length + 1] = new Vector2(head.xMax + 3, head.y + 40);
            c.Fill(Grow(cap, Outline), (x, y) => SdRoundRect(x, y, cap, 12) - Outline, Ink);
            c.PolygonOutlined(fringePoly, s.Hair, Ink, Outline);
            c.RoundRect(cap, 12, s.Hair);
            // Ear sits at the hairline towards the back of the head
            c.Ellipse(head.x + 14, head.y + 18, 4.2f, 5.2f, Ink);
            c.Ellipse(head.x + 14, head.y + 18, 2.6f, 3.6f, skinShade);

            // Eye
            float ex = head.xMax - 11, ey = head.y + 17;
            if (hurt)
            {
                c.Capsule(new Vector2(ex - 4, ey - 4), new Vector2(ex + 4, ey + 4), 1.6f, s.Eye);
                c.Capsule(new Vector2(ex - 4, ey + 4), new Vector2(ex + 4, ey - 4), 1.6f, s.Eye);
            }
            else
            {
                c.RoundRect(new Rect(ex - 3, ey - 5, 7, 12), 3.2f, s.Eye);
                c.Circle(ex + 1.5f, ey + 4f, 1.4f, Color.white);
            }
            // Cheek blush
            c.Ellipse(head.xMax - 6, head.y + 9, 4, 2.4f, WithAlpha(Hex("#F28B7A"), 0.45f));
        }

        static void DrawEye(ArtCanvas c, CharacterStyle s, float ex, float ey, bool hurt)
        {
            if (hurt)
            {
                c.Capsule(new Vector2(ex - 4, ey - 4), new Vector2(ex + 4, ey + 4), 1.6f, s.Eye);
                c.Capsule(new Vector2(ex - 4, ey + 4), new Vector2(ex + 4, ey - 4), 1.6f, s.Eye);
            }
            else
            {
                c.RoundRect(new Rect(ex - 3, ey - 5, 7, 12), 3.2f, s.Eye);
                c.Circle(ex + 1.5f, ey + 4f, 1.4f, Color.white);
            }
        }

        static void DrawNinjaHead(ArtCanvas c, CharacterStyle s, Vector2 neck, bool hurt)
        {
            Rect head = new(neck.x - 22, neck.y - 2, 46, 46);
            // Headband tails trail behind the hood
            c.CapsuleOutlined(new Vector2(head.x + 4, head.y + 32), new Vector2(head.x - 14, head.y + 26), 3.2f, s.Accent, Ink, Outline);
            c.CapsuleOutlined(new Vector2(head.x + 4, head.y + 30), new Vector2(head.x - 11, head.y + 15), 3f, Shade(s.Accent, -0.15f), Ink, Outline);

            c.RoundRect(Grow(head, Outline), 11 + Outline, Ink);
            c.RoundRect(head, 11, (x, y) => y < head.y + 8 ? Shade(s.Hair, -0.2f) : s.Hair);
            // Eye slit showing skin
            Rect slit = new(head.x + 18, head.y + 12, head.width - 18, 13);
            c.RoundRect(slit, 5, s.Skin);
            c.RoundRect(new Rect(slit.x, slit.y, slit.width, 3), 1.5f, Shade(s.Skin, -0.15f));
            // Headband across the forehead
            c.RoundRect(new Rect(head.x, head.y + 29, head.width, 7), 2, s.Accent);
            c.RoundRect(new Rect(head.x, head.y + 34, head.width, 2), 1, Shade(s.Accent, 0.25f));

            DrawEye(c, s, head.xMax - 11, head.y + 18, hurt);
        }

        static void DrawHelmetHead(ArtCanvas c, CharacterStyle s, Vector2 neck, bool hurt)
        {
            float cx = neck.x + 1, cy = neck.y + 21;
            const float r = 26f;
            c.CircleOutlined(cx, cy, r, s.Shirt, Ink, Outline);
            c.Fill(new Rect(cx - r, cy - r, r * 2, r), (x, y) => Mathf.Max(SdCircle(x, y, cx, cy, r), y - (cy - 10)), Shade(s.Shirt, -0.08f));
            // Visor
            Rect visor = new(cx - 12, cy - 12, 34, 26);
            c.RoundRect(Grow(visor, 2), 12, Ink);
            c.RoundRect(visor, 11, (x, y) => Color.Lerp(Shade(s.Visor, -0.3f), Shade(s.Visor, 0.15f), (y - visor.y) / visor.height));
            c.RoundRect(new Rect(visor.x + 5, visor.yMax - 8, 12, 4), 2, WithAlpha(Color.white, 0.6f));
            c.Circle(visor.x + 21, visor.yMax - 6, 1.8f, WithAlpha(Color.white, 0.5f));
            if (hurt)
            {
                float ex = visor.center.x + 4, ey = visor.center.y;
                c.Capsule(new Vector2(ex - 4, ey - 4), new Vector2(ex + 4, ey + 4), 1.6f, Color.white);
                c.Capsule(new Vector2(ex - 4, ey + 4), new Vector2(ex + 4, ey - 4), 1.6f, Color.white);
            }
            // Antenna light and ear pod
            c.Capsule(new Vector2(cx - 10, cy + r - 2), new Vector2(cx - 14, cy + r + 8), 1.5f, Ink);
            c.CircleOutlined(cx - 14, cy + r + 9, 3.2f, s.Accent, Ink, 1.5f);
            c.CircleOutlined(cx - r + 3, cy, 5f, Shade(s.Shirt, -0.15f), Ink, 2f);
        }

        static void DrawRobotHead(ArtCanvas c, CharacterStyle s, Vector2 neck, bool hurt)
        {
            Rect head = new(neck.x - 22, neck.y - 1, 46, 42);
            // Antenna
            c.Capsule(new Vector2(head.center.x - 4, head.yMax), new Vector2(head.center.x - 4, head.yMax + 10), 1.8f, Ink);
            c.CircleOutlined(head.center.x - 4, head.yMax + 12, 3.6f, s.Accent, Ink, 1.8f);

            c.RoundRect(Grow(head, Outline), 6 + Outline, Ink);
            c.RoundRect(head, 6, (x, y) => y < head.y + 8 ? Shade(s.Skin, -0.2f) : y > head.yMax - 6 ? Shade(s.Skin, 0.15f) : s.Skin);
            // Ear bolt
            c.CircleOutlined(head.x + 6, head.y + 20, 4f, Shade(s.Skin, -0.25f), Ink, 1.6f);
            // Screen with glowing eyes
            Rect screen = new(head.x + 14, head.y + 12, head.width - 18, 20);
            c.RoundRect(Grow(screen, 2), 6, Ink);
            c.RoundRect(screen, 5, Hex("#1E222C"));
            if (hurt)
            {
                c.Capsule(new Vector2(screen.x + 6, screen.center.y), new Vector2(screen.xMax - 6, screen.center.y), 1.8f, s.Eye);
            }
            else
            {
                c.Circle(screen.x + 10, screen.center.y, 6f, WithAlpha(s.Eye, 0.3f));
                c.Circle(screen.xMax - 8, screen.center.y, 6f, WithAlpha(s.Eye, 0.3f));
                c.RoundRect(new Rect(screen.x + 6, screen.center.y - 3.5f, 8, 7), 3, s.Eye);
                c.RoundRect(new Rect(screen.xMax - 12, screen.center.y - 3.5f, 8, 7), 3, s.Eye);
            }
            // Mouth grille
            for (int i = 0; i < 3; i++)
                c.RoundRect(new Rect(screen.x + 6 + i * 7, head.y + 4, 4, 3), 1, Shade(s.Skin, -0.35f));
        }

        static void DrawGhost(ArtCanvas c, CharacterStyle s, Pose p)
        {
            float b = p.Bob;
            float cx = 64, top = 86 + b;
            const float r = 27f;
            const float bottom = 12f;
            Color shade = Hex("#D4D6F2");

            // Body: dome head flowing into a scalloped tail that ripples with the run phase.
            // Points go counter-clockwise: dome from right to left, then the tail from left to right.
            var pts = new List<Vector2>();
            for (int i = 0; i <= 12; i++)
            {
                float a = Mathf.PI * i / 12f;
                pts.Add(new Vector2(cx + Mathf.Cos(a) * r, top - r + Mathf.Sin(a) * r));
            }
            const int scallops = 4;
            for (int i = 0; i <= scallops * 4; i++)
            {
                float t = i / (float)(scallops * 4);
                float x = cx - r + t * r * 2f;
                float wave = Mathf.Sin(t * scallops * Mathf.PI * 2f + p.CapePhase) * 4f;
                pts.Add(new Vector2(x, bottom + b * 0.5f + wave));
            }
            var body = pts.ToArray();
            c.PolygonOutlined(body, s.Skin, Ink, Outline);
            // Soft shading on the back and the tail
            c.Fill(new Rect(cx - r, 0, r * 2, top), (x, y) => Mathf.Max(SdPolygon(x, y, body), -(cx - r * 0.45f - x) ), WithAlpha(shade, 0.85f));

            // Little arm nub poking out of the front edge, swinging with the run pose
            Vector2 shoulder = new(cx + r - 4, 46 + b);
            Vector2 swing = (p.FrontHand - new Vector2(70, 40)) * 0.25f;
            c.CapsuleOutlined(shoulder, shoulder + new Vector2(10, -5) + swing, 4.5f, s.Skin, Ink, Outline);

            // Face
            float eyeY = 68 + b;
            if (p.Hurt)
            {
                foreach (float ex in new[] { cx + 6f, cx + 20f })
                {
                    c.Capsule(new Vector2(ex - 4, eyeY - 4), new Vector2(ex + 4, eyeY + 4), 1.6f, s.Eye);
                    c.Capsule(new Vector2(ex - 4, eyeY + 4), new Vector2(ex + 4, eyeY - 4), 1.6f, s.Eye);
                }
            }
            else
            {
                c.Ellipse(cx + 6, eyeY, 4f, 6f, s.Eye);
                c.Ellipse(cx + 20, eyeY, 4f, 6f, s.Eye);
                c.Circle(cx + 7.5f, eyeY + 3, 1.3f, Color.white);
                c.Circle(cx + 21.5f, eyeY + 3, 1.3f, Color.white);
            }
            c.Ellipse(cx + 14, eyeY - 12, 3.2f, 3.8f, s.Eye);

            // Wizard hat, tipped back
            var hat = new[] { new Vector2(cx - 24, top - 6), new Vector2(cx + 22, top - 4), new Vector2(cx - 22, top + 32) };
            c.PolygonOutlined(hat, s.Visor, Ink, Outline);
            c.Polygon(new[] { new Vector2(cx - 24, top - 6), new Vector2(cx - 4, top - 5), new Vector2(cx - 22, top + 32) }, Shade(s.Visor, -0.2f));
            c.Ellipse(cx - 1, top - 5, 30, 5.5f, Ink);
            c.Ellipse(cx - 1, top - 5, 28, 3.8f, Shade(s.Visor, 0.1f));
            c.Polygon(PropArt.StarPoints(cx - 7, top + 9, 6f, 2.6f), s.Accent);
        }

        static Rect Grow(Rect r, float by) => new Rect(r.x - by, r.y - by, r.width + by * 2, r.height + by * 2);

        static ArtCanvas Upscale(ArtCanvas src, int factor)
        {
            var dst = new ArtCanvas(src.Width * factor, src.Height * factor);
            var tex = src.ToTexture();
            for (int y = 0; y < dst.Height; y++)
            for (int x = 0; x < dst.Width; x++)
            {
                Color c = tex.GetPixelBilinear((x + 0.5f) / dst.Width, (y + 0.5f) / dst.Height);
                if (c.a > 0) dst.Blend(x, y, new Color(c.r, c.g, c.b, 1f), c.a);
            }
            Object.DestroyImmediate(tex);
            return dst;
        }
    }
}
