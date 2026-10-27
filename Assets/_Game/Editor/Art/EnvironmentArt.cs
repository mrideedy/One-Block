using UnityEngine;
using static OneBlock.EditorTools.Art.ArtCanvas;

namespace OneBlock.EditorTools.Art
{
    /// <summary>
    /// Ground tiles, parallax background layers, decor and shop thumbnails for every world.
    /// Each world is a palette plus a handful of style switches, so new worlds are mostly data.
    /// Tiles are 100x100 (one cell) and tile seamlessly horizontally.
    /// </summary>
    public static class EnvironmentArt
    {
        const int T = 100;

        public enum MountainStyle { Peaks, SnowPeaks, Mesas, Volcanoes, Planets, Spires, Reef, Skyline }
        public enum GroundStyle { Grass, Sand, Snow, Crust, Moon, Frosting, Seabed, Neon }
        public enum HillStyle { TreeRidge, Dunes, Drifts, Rocks, Craters }
        public enum DecorStyle { Pines, Cacti, SnowPines, DeadTrees, Crystals, Candy, Seaweed, NeonPoles }
        public enum CloudStyle { Puffy, Wispy, Smoke, Stars, Bubbles }

        public class WorldPalette
        {
            public string Id;
            public string Name;
            public int Price;
            public string BundleId;               // set for ad-bundle worlds (not sold for coins)
            public Color Accent2 = Color.white;   // second neon / sprinkle colour
            public HazardStyle Hazards = HazardStyle.Spikes;
            public EnemyStyle Enemies = EnemyStyle.Shadow;

            public Color SkyTop, SkyBottom;
            public Color MountainFar, MountainNear;
            public Color HillColor;
            public Color Grass, GrassLight, GrassDark;
            public Color Dirt, DirtDark, DirtLight;
            public Color Cloud;
            public Color TreeLeaf, TreeLeafDark, TreeTrunk;
            public Color Accent = Color.white;   // lava glow, crystal light, planet colour...
            public Color Ink = Hex("#1F3A2A");

            public MountainStyle Far = MountainStyle.Peaks;
            public MountainStyle Near = MountainStyle.Peaks;
            public GroundStyle Ground = GroundStyle.Grass;
            public HillStyle Hills = HillStyle.TreeRidge;
            public DecorStyle Decor = DecorStyle.Pines;
            public CloudStyle Clouds = CloudStyle.Puffy;
        }

        public static WorldPalette Grassland => new WorldPalette
        {
            Id = "grassland", Name = "Grassland",
            SkyTop = Hex("#4FA3EE"), SkyBottom = Hex("#BFE6FA"),
            MountainFar = Hex("#9CC6EE"), MountainNear = Hex("#5F97DA"),
            HillColor = Hex("#4E9E6A"),
            Grass = Hex("#5DBE3C"), GrassLight = Hex("#8BDB52"), GrassDark = Hex("#3C9530"),
            Dirt = Hex("#4C3B37"), DirtDark = Hex("#3B2D2A"), DirtLight = Hex("#5E4A44"),
            Cloud = Color.white,
            TreeLeaf = Hex("#3FA34D"), TreeLeafDark = Hex("#2C7D3A"), TreeTrunk = Hex("#6B4A32"),
        };

        public static WorldPalette Desert => new WorldPalette
        {
            Id = "desert", Name = "Desert", Price = 1000,
            SkyTop = Hex("#F08A4B"), SkyBottom = Hex("#FCE2AE"),
            MountainFar = Hex("#EDB883"), MountainNear = Hex("#CF8752"),
            HillColor = Hex("#E7AE68"),
            Grass = Hex("#F2C46D"), GrassLight = Hex("#FFDF9C"), GrassDark = Hex("#D59A4A"),
            Dirt = Hex("#B97640"), DirtDark = Hex("#9A5D32"), DirtLight = Hex("#CC8B55"),
            Cloud = Hex("#FFF3DE"),
            TreeLeaf = Hex("#63B04F"), TreeLeafDark = Hex("#428C3A"), TreeTrunk = Hex("#8A5A36"),
            Ink = Hex("#4A2A16"),
            Far = MountainStyle.Mesas, Near = MountainStyle.Mesas, Ground = GroundStyle.Sand,
            Hills = HillStyle.Dunes, Decor = DecorStyle.Cacti, Clouds = CloudStyle.Wispy,
            Hazards = HazardStyle.MudSnake, Enemies = EnemyStyle.Scorpion
        };

        public static WorldPalette Snow => new WorldPalette
        {
            Id = "snow", Name = "Snow", Price = 2000,
            SkyTop = Hex("#6FA8DC"), SkyBottom = Hex("#E6F2FB"),
            MountainFar = Hex("#B9D0EA"), MountainNear = Hex("#7EA1CC"),
            HillColor = Hex("#D6E6F5"),
            Grass = Hex("#F2F8FF"), GrassLight = Hex("#FFFFFF"), GrassDark = Hex("#B9D3EC"),
            Dirt = Hex("#56697F"), DirtDark = Hex("#44546A"), DirtLight = Hex("#687D95"),
            Cloud = Color.white,
            TreeLeaf = Hex("#2F7D5A"), TreeLeafDark = Hex("#215E45"), TreeTrunk = Hex("#5A4436"),
            Ink = Hex("#1D2B3A"), Accent = Hex("#9FD8FF"),
            Far = MountainStyle.SnowPeaks, Near = MountainStyle.SnowPeaks, Ground = GroundStyle.Snow,
            Hills = HillStyle.Drifts, Decor = DecorStyle.SnowPines, Clouds = CloudStyle.Puffy,
            Hazards = HazardStyle.IceSpikes, Enemies = EnemyStyle.Penguin
        };

        public static WorldPalette Volcano => new WorldPalette
        {
            Id = "volcano", Name = "Volcano", Price = 3500,
            SkyTop = Hex("#2E1726"), SkyBottom = Hex("#E0613D"),
            MountainFar = Hex("#6E3036"), MountainNear = Hex("#3E2A31"),
            HillColor = Hex("#2C2228"),
            Grass = Hex("#4B3B40"), GrassLight = Hex("#6A565C"), GrassDark = Hex("#2E2429"),
            Dirt = Hex("#2B2226"), DirtDark = Hex("#1E181B"), DirtLight = Hex("#3A2E33"),
            Cloud = Hex("#5A4A52"),
            TreeLeaf = Hex("#3E3134"), TreeLeafDark = Hex("#2A2124"), TreeTrunk = Hex("#3E3134"),
            Ink = Hex("#140F12"), Accent = Hex("#FF7A2A"),
            Far = MountainStyle.Volcanoes, Near = MountainStyle.Volcanoes, Ground = GroundStyle.Crust,
            Hills = HillStyle.Rocks, Decor = DecorStyle.DeadTrees, Clouds = CloudStyle.Smoke,
            Hazards = HazardStyle.Lava, Enemies = EnemyStyle.MagmaBlob
        };

        public static WorldPalette Space => new WorldPalette
        {
            Id = "space", Name = "Space", Price = 5000,
            SkyTop = Hex("#090A24"), SkyBottom = Hex("#3C2270"),
            MountainFar = Hex("#C77DFF"), MountainNear = Hex("#4A3A7C"),
            HillColor = Hex("#5E5486"),
            Grass = Hex("#A99CCB"), GrassLight = Hex("#D4CAEF"), GrassDark = Hex("#7B6EA3"),
            Dirt = Hex("#4B436F"), DirtDark = Hex("#3A3358"), DirtLight = Hex("#60578A"),
            Cloud = Color.white,
            TreeLeaf = Hex("#7CF2E6"), TreeLeafDark = Hex("#3FB8C9"), TreeTrunk = Hex("#4A3A7C"),
            Ink = Hex("#120F2A"), Accent = Hex("#5EC8F2"),
            Far = MountainStyle.Planets, Near = MountainStyle.Spires, Ground = GroundStyle.Moon,
            Hills = HillStyle.Craters, Decor = DecorStyle.Crystals, Clouds = CloudStyle.Stars,
            Hazards = HazardStyle.Crystals, Enemies = EnemyStyle.Alien
        };

        public static WorldPalette Candy => new WorldPalette
        {
            Id = "candy", Name = "Candy Land", BundleId = "bundle_candy",
            SkyTop = Hex("#FF9FCF"), SkyBottom = Hex("#FFEAF5"),
            MountainFar = Hex("#F6C3E4"), MountainNear = Hex("#E58BC4"),
            HillColor = Hex("#FFB0D2"),
            Grass = Hex("#FF8CC3"), GrassLight = Hex("#FFC4E1"), GrassDark = Hex("#E0619F"),
            Dirt = Hex("#C98A5B"), DirtDark = Hex("#A8693E"), DirtLight = Hex("#FFF1D6"),
            Cloud = Color.white,
            TreeLeaf = Hex("#FF5C99"), TreeLeafDark = Hex("#FF7FB6"), TreeTrunk = Color.white,
            Ink = Hex("#5A2440"), Accent = Hex("#7FE3D0"), Accent2 = Hex("#FFD23F"),
            Far = MountainStyle.SnowPeaks, Near = MountainStyle.SnowPeaks, Ground = GroundStyle.Frosting,
            Hills = HillStyle.TreeRidge, Decor = DecorStyle.Candy, Clouds = CloudStyle.Puffy,
            Hazards = HazardStyle.CandyCorn, Enemies = EnemyStyle.GummyBear
        };

        public static WorldPalette Ocean => new WorldPalette
        {
            Id = "ocean", Name = "Deep Ocean", BundleId = "bundle_ocean",
            SkyTop = Hex("#062E52"), SkyBottom = Hex("#2296BF"),
            MountainFar = Hex("#2D7597"), MountainNear = Hex("#1B587A"),
            HillColor = Hex("#D8BF88"),
            Grass = Hex("#E8D29A"), GrassLight = Hex("#F7E7BA"), GrassDark = Hex("#C6AC6E"),
            Dirt = Hex("#8C7651"), DirtDark = Hex("#6F5C3E"), DirtLight = Hex("#A58C62"),
            Cloud = Hex("#CFF4FF"),
            TreeLeaf = Hex("#3FBF8A"), TreeLeafDark = Hex("#2A8F66"), TreeTrunk = Hex("#2A8F66"),
            Ink = Hex("#0D2A3A"), Accent = Hex("#FF7A6B"), Accent2 = Hex("#FFD23F"),
            Far = MountainStyle.Reef, Near = MountainStyle.Reef, Ground = GroundStyle.Seabed,
            Hills = HillStyle.Dunes, Decor = DecorStyle.Seaweed, Clouds = CloudStyle.Bubbles,
            Hazards = HazardStyle.Urchin, Enemies = EnemyStyle.Crab
        };

        public static WorldPalette Neon => new WorldPalette
        {
            Id = "neon", Name = "Neon City", BundleId = "bundle_neon",
            SkyTop = Hex("#0B061F"), SkyBottom = Hex("#3B105E"),
            MountainFar = Hex("#2B1C52"), MountainNear = Hex("#1A1236"),
            HillColor = Hex("#140E2A"),
            Grass = Hex("#2E2548"), GrassLight = Hex("#FF3DCB"), GrassDark = Hex("#1C1630"),
            Dirt = Hex("#1A1430"), DirtDark = Hex("#120E22"), DirtLight = Hex("#261E44"),
            Cloud = Hex("#8A6CFF"),
            TreeLeaf = Hex("#3DF2FF"), TreeLeafDark = Hex("#FF3DCB"), TreeTrunk = Hex("#3A3160"),
            Ink = Hex("#07040F"), Accent = Hex("#3DF2FF"), Accent2 = Hex("#FF3DCB"),
            Far = MountainStyle.Skyline, Near = MountainStyle.Skyline, Ground = GroundStyle.Neon,
            Hills = HillStyle.Rocks, Decor = DecorStyle.NeonPoles, Clouds = CloudStyle.Wispy,
            Hazards = HazardStyle.Zapper, Enemies = EnemyStyle.Drone
        };

        public static WorldPalette[] All => new[] { Grassland, Desert, Snow, Volcano, Space, Candy, Ocean, Neon };

        public static void Generate()
        {
            foreach (var world in All) GenerateWorld(world);
        }

        /// <summary>The 320x200 shop thumbnail for a world (used by bundle cards too).</summary>
        public static ArtCanvas ThumbnailFor(WorldPalette p) =>
            Thumbnail(p, Mountains(p, p.Far, p.MountainFar, 1024, 360, 7, 11, 170, 330),
                Mountains(p, p.Near, p.MountainNear, 1024, 340, 5, 23, 150, 320), Hills(p, 1024, 200, 5), Cloud(p, 0), Tree(p, 0));

        public static void GenerateWorld(WorldPalette p)
        {
            string dir = $"Worlds/{p.Id}";
            var tile = ArtGenerator.ImportOptions.BottomLeft;

            ArtGenerator.Save(GroundTop(p, false, false), $"{dir}/tile_top", tile);
            ArtGenerator.Save(GroundTop(p, true, false), $"{dir}/tile_top_left", tile);
            ArtGenerator.Save(GroundTop(p, false, true), $"{dir}/tile_top_right", tile);
            ArtGenerator.Save(GroundTop(p, true, true), $"{dir}/tile_top_single", tile);
            ArtGenerator.Save(GroundFill(p, false, false, 1), $"{dir}/tile_fill", tile);
            ArtGenerator.Save(GroundFill(p, true, false, 2), $"{dir}/tile_fill_left", tile);
            ArtGenerator.Save(GroundFill(p, false, true, 3), $"{dir}/tile_fill_right", tile);
            ArtGenerator.Save(GroundFill(p, true, true, 4), $"{dir}/tile_fill_single", tile);

            var repeat = new ArtGenerator.ImportOptions { Pivot = Vector2.zero, Repeat = true };
            var far = Mountains(p, p.Far, p.MountainFar, 1024, 360, 7, 11, 170, 330);
            var near = Mountains(p, p.Near, p.MountainNear, 1024, 340, 5, 23, 150, 320);
            var hills = Hills(p, 1024, 200, 5);
            ArtGenerator.Save(Sky(p), $"{dir}/bg_sky", new ArtGenerator.ImportOptions { Pivot = new Vector2(0.5f, 0.5f), Repeat = true, PixelsPerUnit = 16 });
            ArtGenerator.Save(far, $"{dir}/bg_mountains_far", repeat);
            ArtGenerator.Save(near, $"{dir}/bg_mountains_near", repeat);
            ArtGenerator.Save(hills, $"{dir}/bg_hills", repeat);

            var bottom = ArtGenerator.ImportOptions.BottomCenter;
            var cloud0 = Cloud(p, 0);
            var tree0 = Tree(p, 0);
            ArtGenerator.Save(cloud0, $"{dir}/cloud_0", ArtGenerator.ImportOptions.Center);
            ArtGenerator.Save(Cloud(p, 1), $"{dir}/cloud_1", ArtGenerator.ImportOptions.Center);
            ArtGenerator.Save(tree0, $"{dir}/decor_tree_0", bottom);
            ArtGenerator.Save(Tree(p, 1), $"{dir}/decor_tree_1", bottom);
            ArtGenerator.Save(Bush(p), $"{dir}/decor_bush", bottom);
            ArtGenerator.Save(Tuft(p), $"{dir}/decor_tuft", bottom);

            ArtGenerator.Save(Thumbnail(p, far, near, hills, cloud0, tree0), $"UI/Worlds/world_{p.Id}", new ArtGenerator.ImportOptions { IsUI = true });
            HazardArt.Generate(p, dir);
        }

        // ================================================================ ground

        static void DirtSpeckles(ArtCanvas c, WorldPalette p, int seed, float maxY)
        {
            // Chunky speckles on a 10px lattice, wrapped so tiles repeat seamlessly
            for (int gy = 0; gy < 10; gy++)
            for (int gx = 0; gx < 10; gx++)
            {
                float h = Hash(gx, gy, seed);
                if (h > 0.2f) continue;
                float x = gx * 10 + Hash(gx, gy, seed + 1) * 4;
                float y = gy * 10 + Hash(gx, gy, seed + 2) * 4;
                if (y + 8 > maxY) continue;
                float w = 6 + Hash(gx, gy, seed + 3) * 8, hgt = 5 + Hash(gx, gy, seed + 4) * 4;
                Color col = h < 0.12f ? p.DirtDark : p.DirtLight;
                if (p.Ground == GroundStyle.Crust && h < 0.005f) col = p.Accent;   // glowing embers in volcanic rock
                c.RoundRect(new Rect(x, y, w, hgt), 1.5f, col);
                if (x + w > T) c.RoundRect(new Rect(x - T, y, w, hgt), 1.5f, col);
            }
            if (p.Ground == GroundStyle.Frosting)
            {
                // Cream filling layer in the cake
                c.RoundRect(new Rect(0, 44, T, 8), 0, p.DirtLight);
                c.RoundRect(new Rect(0, 44, T, 2), 0, Shade(p.DirtLight, -0.12f));
            }
            if (p.Ground == GroundStyle.Neon)
            {
                // Faint circuit lines in the metal
                for (int i = 0; i < 4; i++) c.RoundRect(new Rect(12 + i * 25, 0, 2, maxY), 0, WithAlpha(p.Accent, 0.12f));
                c.RoundRect(new Rect(0, 30, T, 2), 0, WithAlpha(p.Accent2, 0.12f));
            }
            if (p.Ground == GroundStyle.Sand)
            {
                // Sandstone strata
                for (int i = 0; i < 3; i++)
                    c.RoundRect(new Rect(0, 12 + i * 22, T, 3), 0, WithAlpha(p.DirtDark, 0.35f));
            }
        }

        static void SideEdges(ArtCanvas c, WorldPalette p, bool left, bool right, float top)
        {
            Color edge = Shade(p.DirtDark, -0.25f);
            if (left) c.RoundRect(new Rect(0, 0, 6, top), 2, edge);
            if (right) c.RoundRect(new Rect(T - 6, 0, 6, top), 2, edge);
        }

        static ArtCanvas GroundFill(WorldPalette p, bool edgeLeft, bool edgeRight, int seed)
        {
            var c = new ArtCanvas(T, T);
            c.RoundRect(new Rect(0, 0, T, T), 0, p.Dirt);
            DirtSpeckles(c, p, 40 + seed, T);
            SideEdges(c, p, edgeLeft, edgeRight, T);
            return c;
        }

        static ArtCanvas GroundTop(WorldPalette p, bool edgeLeft, bool edgeRight)
        {
            var c = new ArtCanvas(T, T);
            const float top = T;
            float baseY = p.Ground switch
            {
                GroundStyle.Snow => 68,
                GroundStyle.Frosting => 66,
                GroundStyle.Sand or GroundStyle.Seabed => 72,
                _ => 74
            };

            c.RoundRect(new Rect(0, 0, T, baseY + 4), 0, p.Dirt);
            DirtSpeckles(c, p, 17, baseY - 6);
            SideEdges(c, p, edgeLeft, edgeRight, baseY);

            float xMin = edgeLeft ? 0 : -8, xMax = edgeRight ? T : T + 8;
            c.RoundRect(Rect.MinMaxRect(xMin, baseY, xMax, top), 6, p.GrassDark);
            c.RoundRect(Rect.MinMaxRect(xMin, baseY + 4, xMax, top), 5, p.Grass);
            c.RoundRect(Rect.MinMaxRect(xMin, top - 8, xMax, top), 4, p.GrassLight);

            switch (p.Ground)
            {
                case GroundStyle.Grass:
                    for (int i = 0; i < 4; i++)
                    {
                        float cx = 12.5f + i * 25f + (i % 2 == 0 ? 2 : -3);
                        if (edgeLeft && cx < 10 || edgeRight && cx > T - 10) continue;
                        float depth = i % 2 == 0 ? 10 : 6;
                        c.RoundRect(new Rect(cx - 6, baseY - depth, 12, depth + 6), 5, p.GrassDark);
                        c.RoundRect(new Rect(cx - 5, baseY - depth + 3, 10, depth + 4), 4, p.Grass);
                    }
                    for (int i = 0; i < 5; i++)
                        c.RoundRect(new Rect(8 + i * 20, top - 5, 6, 3), 1.5f, WithAlpha(Color.white, 0.25f));
                    break;

                case GroundStyle.Sand:
                    // Soft wavy lip and a few pebbles
                    for (int i = 0; i < 4; i++)
                    {
                        float cx = 12.5f + i * 25f;
                        if (edgeLeft && cx < 10 || edgeRight && cx > T - 10) continue;
                        c.Ellipse(cx, baseY + 1, 13, 5, p.Grass);
                    }
                    c.Circle(30, top - 14, 2.5f, p.GrassDark);
                    c.Circle(72, top - 18, 2f, p.GrassDark);
                    c.RoundRect(new Rect(48, top - 12, 10, 3), 1.5f, WithAlpha(Color.white, 0.3f));
                    break;

                case GroundStyle.Snow:
                    // Icicles hanging from the snow cap
                    for (int i = 0; i < 5; i++)
                    {
                        float cx = 10 + i * 20f;
                        if (edgeLeft && cx < 10 || edgeRight && cx > T - 10) continue;
                        float len = i % 2 == 0 ? 14 : 9;
                        var ice = new[] { new Vector2(cx - 5, baseY + 1), new Vector2(cx + 5, baseY + 1), new Vector2(cx, baseY - len) };
                        c.Polygon(ice, p.GrassDark);
                        c.Polygon(new[] { new Vector2(cx - 3, baseY + 1), new Vector2(cx + 1, baseY + 1), new Vector2(cx - 1, baseY - len + 4) }, WithAlpha(p.Accent, 0.9f));
                    }
                    for (int i = 0; i < 4; i++)
                        c.Circle(12 + i * 25, top - 6, 1.8f, WithAlpha(p.Accent, 0.8f));
                    break;

                case GroundStyle.Crust:
                    // Glowing lava seam under the crust plus cracks in the top
                    c.RoundRect(Rect.MinMaxRect(xMin, baseY - 3, xMax, baseY + 2), 2, p.Accent);
                    c.RoundRect(Rect.MinMaxRect(xMin, baseY - 1, xMax, baseY + 1), 1, Shade(p.Accent, 0.4f));
                    c.Capsule(new Vector2(22, top - 6), new Vector2(30, baseY + 8), 1.4f, p.Accent);
                    c.Capsule(new Vector2(30, baseY + 8), new Vector2(38, baseY + 4), 1.2f, p.Accent);
                    c.Capsule(new Vector2(70, top - 4), new Vector2(64, baseY + 10), 1.4f, p.Accent);
                    break;

                case GroundStyle.Moon:
                    // Shallow craters in the crust
                    foreach (var cr in new[] { new Vector3(26, top - 12, 8), new Vector3(70, top - 14, 6), new Vector3(52, baseY + 8, 4) })
                    {
                        c.Ellipse(cr.x, cr.y, cr.z + 1.5f, cr.z * 0.55f + 1.2f, p.GrassDark);
                        c.Ellipse(cr.x + 1, cr.y + 1, cr.z, cr.z * 0.5f, Shade(p.Grass, -0.08f));
                    }
                    break;

                case GroundStyle.Frosting:
                    // Rounded icing drips and sprinkles
                    for (int i = 0; i < 4; i++)
                    {
                        float cx = 12.5f + i * 25f;
                        if (edgeLeft && cx < 10 || edgeRight && cx > T - 10) continue;
                        float depth = i % 2 == 0 ? 16 : 9;
                        c.Capsule(new Vector2(cx, baseY + 2), new Vector2(cx, baseY - depth), 5.5f, p.GrassDark);
                        c.Capsule(new Vector2(cx - 0.5f, baseY + 3), new Vector2(cx - 0.5f, baseY - depth + 2), 4f, p.Grass);
                    }
                    var sprinkles = new[] { p.Accent, p.Accent2, Color.white, Hex("#8A6CFF") };
                    for (int i = 0; i < 7; i++)
                    {
                        float x = 8 + i * 13 + (i % 2) * 4, y = baseY + 12 + (i % 3) * 6;
                        float a = i * 1.3f;
                        var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 3f;
                        c.Capsule(new Vector2(x, y) - d, new Vector2(x, y) + d, 1.6f, sprinkles[i % sprinkles.Length]);
                    }
                    break;

                case GroundStyle.Seabed:
                    for (int i = 0; i < 4; i++)
                    {
                        float cx = 12.5f + i * 25f;
                        if (edgeLeft && cx < 10 || edgeRight && cx > T - 10) continue;
                        c.Ellipse(cx, baseY + 1, 13, 5, p.Grass);
                    }
                    // A shell and a pebble
                    c.Fill(new Rect(22, top - 20, 16, 12), (x, y) => Mathf.Max(SdCircle(x, y, 30, top - 20, 8), (top - 20) - y), p.Accent);
                    c.Capsule(new Vector2(30, top - 20), new Vector2(26, top - 13), 0.8f, Shade(p.Accent, -0.25f));
                    c.Capsule(new Vector2(30, top - 20), new Vector2(34, top - 13), 0.8f, Shade(p.Accent, -0.25f));
                    c.Circle(74, top - 16, 3f, p.GrassDark);
                    break;

                case GroundStyle.Neon:
                    // Glowing magenta edge on top and a cyan seam under the plate
                    c.RoundRect(Rect.MinMaxRect(xMin, top - 5, xMax, top - 2), 1.5f, p.GrassLight);
                    c.RoundRect(Rect.MinMaxRect(xMin, baseY - 2, xMax, baseY + 2), 2, p.Accent);
                    c.RoundRect(Rect.MinMaxRect(xMin, baseY - 1, xMax, baseY + 1), 1, Color.white);
                    for (int i = 0; i < 4; i++) c.RoundRect(new Rect(10 + i * 25, baseY + 8, 8, 4), 1.5f, WithAlpha(p.Accent, 0.4f));
                    break;
            }
            return c;
        }

        // ================================================================ background

        static ArtCanvas Sky(WorldPalette p)
        {
            var c = new ArtCanvas(8, 256);
            for (int y = 0; y < c.Height; y++)
            {
                float t = y / (float)(c.Height - 1);
                Color col = Color.Lerp(p.SkyBottom, p.SkyTop, Mathf.SmoothStep(0, 1, t));
                for (int x = 0; x < c.Width; x++) c.Blend(x, y, col, 1f);
            }
            return c;
        }

        static float WrapDx(float x, float center, int w)
        {
            float dx = x + 0.5f - center;
            return dx - Mathf.Round(dx / w) * w;
        }

        static ArtCanvas Mountains(WorldPalette p, MountainStyle style, Color color, int w, int h, int peaks, int seed, float minH, float maxH)
        {
            if (style == MountainStyle.Planets) return Planets(p, w, h, seed);
            if (style == MountainStyle.Skyline) return Skyline(p, color, w, h, seed, minH, maxH);

            var c = new ArtCanvas(w, h);
            var px = new float[peaks];
            var ph = new float[peaks];
            var slope = new float[peaks];
            var halfTop = new float[peaks];
            for (int i = 0; i < peaks; i++)
            {
                px[i] = (i + 0.2f + Hash(i, 0, seed) * 0.6f) * w / peaks;
                ph[i] = Mathf.Lerp(minH, maxH, Hash(i, 1, seed));
                slope[i] = 0.75f + Hash(i, 2, seed) * 0.5f;
                halfTop[i] = style switch
                {
                    MountainStyle.Mesas => 40 + Hash(i, 3, seed) * 50,
                    MountainStyle.Volcanoes => 14 + Hash(i, 3, seed) * 8,
                    MountainStyle.Spires => 0,
                    _ => 0
                };
                if (style == MountainStyle.Mesas) { slope[i] *= 2.4f; ph[i] *= 0.75f; }
                if (style == MountainStyle.Spires) slope[i] *= 3.2f;
            }

            Color shade = Shade(color, -0.12f);
            Color light = Shade(color, 0.08f);

            for (int x = 0; x < w; x++)
            {
                float best = 0; int bi = 0; float bdx = 0;
                for (int i = 0; i < peaks; i++)
                {
                    float dx = WrapDx(x, px[i], w);
                    float y = ph[i] - Mathf.Max(0f, Mathf.Abs(dx) - halfTop[i]) * slope[i];
                    if (style == MountainStyle.Volcanoes && Mathf.Abs(dx) < halfTop[i]) y -= (halfTop[i] - Mathf.Abs(dx)) * 0.35f;   // crater dip
                    if (style == MountainStyle.Spires) y -= Mathf.Pow(Mathf.Abs(dx) / 40f, 2f) * 20f;
                    if (style == MountainStyle.Reef) y = ph[i] - dx * dx / (90f + Hash(i, 5, seed) * 80f);   // rounded coral domes
                    if (y > best) { best = y; bi = i; bdx = dx; }
                }
                for (int y = 0; y < h; y++)
                {
                    float cov = Mathf.Clamp01(best - y + 0.5f);
                    if (cov <= 0) break;
                    Color col = bdx > halfTop[bi] * 0.3f ? shade : light;
                    switch (style)
                    {
                        case MountainStyle.SnowPeaks:
                            if (y > ph[bi] - 45 - Mathf.Sin(x * 0.08f) * 6) col = Color.Lerp(col, Color.white, 0.85f);
                            break;
                        case MountainStyle.Mesas:
                            if (((int)(y / 26f)) % 2 == 1) col = Shade(col, -0.06f);
                            break;
                        case MountainStyle.Volcanoes:
                            // Molten crater bowl plus one thin lava trickle down the lit face
                            if (Mathf.Abs(bdx) < halfTop[bi] * 0.7f && y > best - 5) col = Color.Lerp(col, p.Accent, 0.85f);
                            else if (Mathf.Abs(bdx + halfTop[bi] * 0.4f + Mathf.Sin(y * 0.06f) * 4f) < 2f && y > ph[bi] - 80 && y < best - 4)
                                col = Color.Lerp(col, p.Accent, 0.6f);
                            break;
                    }
                    c.Blend(x, y, col, cov);
                }
            }

            if (style == MountainStyle.Volcanoes)
            {
                // Glow above craters
                for (int i = 0; i < peaks; i++)
                    for (int k = -1; k <= 1; k++)
                        c.Ellipse(px[i] + k * w, ph[i] + 2, halfTop[i] * 0.8f, 5, WithAlpha(p.Accent, 0.22f));
            }
            return c;
        }

        static ArtCanvas Planets(WorldPalette p, int w, int h, int seed)
        {
            var c = new ArtCanvas(w, h);
            // Scattered stars
            for (int i = 0; i < 70; i++)
            {
                float x = Hash(i, 1, seed) * w, y = 60 + Hash(i, 2, seed) * (h - 70);
                float r = 0.8f + Hash(i, 3, seed) * 1.8f;
                c.Circle(x, y, r, WithAlpha(Color.white, 0.4f + Hash(i, 4, seed) * 0.6f));
            }
            // Ringed planet
            DrawPlanet(c, 300, 230, 62, p.MountainFar, true);
            // Small blue moon and a far red dot
            DrawPlanet(c, 760, 290, 30, p.Accent, false);
            DrawPlanet(c, 560, 150, 14, Hex("#FF9A7A"), false);
            return c;
        }

        static void DrawPlanet(ArtCanvas c, float x, float y, float r, Color color, bool ring)
        {
            if (ring) c.Fill(new Rect(x - r * 2, y - r, r * 4, r * 2), (px, py) => Mathf.Abs(SdEllipse(px, py, x, y, r * 1.9f, r * 0.45f)) - 4, WithAlpha(Shade(color, 0.35f), 0.8f));
            c.Circle(x, y, r, Shade(color, -0.15f));
            c.Fill(new Rect(x - r, y - r, r * 2, r * 2), (px, py) => Mathf.Max(SdCircle(px, py, x, y, r), SdCircle(px, py, x + r * 0.35f, y - r * 0.25f, r) * -1f), color);
            c.Circle(x - r * 0.4f, y + r * 0.35f, r * 0.18f, WithAlpha(Color.white, 0.35f));
            if (ring) c.Fill(new Rect(x - r * 2, y - r, r * 4, r), (px, py) => Mathf.Max(Mathf.Abs(SdEllipse(px, py, x, y, r * 1.9f, r * 0.45f)) - 4, py - y), WithAlpha(Shade(color, 0.35f), 0.9f));
        }

        static float Ridge(float x, int w, int seed) =>
            110 + Mathf.Sin(x / w * Mathf.PI * 4 + seed) * 28 + Mathf.Sin(x / w * Mathf.PI * 10 + seed * 2) * 12 + Mathf.Sin(x / w * Mathf.PI * 18) * 5;

        static ArtCanvas Hills(WorldPalette p, int w, int h, int seed)
        {
            var c = new ArtCanvas(w, h);
            Color hill = p.HillColor, dark = Shade(p.HillColor, -0.12f);
            for (int x = 0; x < w; x++)
            {
                float y = p.Hills switch
                {
                    HillStyle.Dunes => 100 + Mathf.Sin(x / (float)w * Mathf.PI * 6 + seed) * 22 + Mathf.Sin(x / (float)w * Mathf.PI * 2) * 12,
                    HillStyle.Rocks => 95 + Mathf.Abs(Mathf.Sin(x / (float)w * Mathf.PI * 8 + seed)) * 40 + Hash(x / 24, 0, seed) * 14,
                    _ => Ridge(x, w, seed)
                };
                for (int yy = 0; yy < h; yy++)
                {
                    float cov = Mathf.Clamp01(y - yy + 0.5f);
                    if (cov <= 0) break;
                    Color col = yy < y - 30 ? dark : hill;
                    if (p.Hills == HillStyle.Dunes && yy > y - 6) col = Shade(hill, 0.12f);
                    if (p.Hills == HillStyle.Drifts && yy > y - 10) col = Color.white;
                    c.Blend(x, yy, col, cov);
                }
            }

            switch (p.Hills)
            {
                case HillStyle.TreeRidge:
                case HillStyle.Drifts:
                    for (int i = 0; i < 18; i++)
                    {
                        float x = (i + Hash(i, 3, seed) * 0.6f) * w / 18f;
                        float ridge = Ridge(x, w, seed);
                        float r = 10 + Hash(i, 4, seed) * 8;
                        Color col = p.Hills == HillStyle.Drifts ? Shade(p.TreeLeafDark, 0.15f) : Shade(p.TreeLeafDark, 0.05f);
                        for (int k = -1; k <= 1; k++)
                        {
                            if (p.Hills == HillStyle.Drifts)
                                c.Polygon(new[] { new Vector2(x + k * w - r * 0.7f, ridge - 4), new Vector2(x + k * w + r * 0.7f, ridge - 4), new Vector2(x + k * w, ridge + r * 1.8f) }, col);
                            else
                                c.Circle(x + k * w, ridge + r * 0.4f, r, col);
                        }
                    }
                    break;
                case HillStyle.Rocks:
                    for (int i = 0; i < 10; i++)
                    {
                        float x = (i + 0.5f) * w / 10f;
                        c.Capsule(new Vector2(x, 20 + Hash(i, 1, seed) * 30), new Vector2(x + 12, 50 + Hash(i, 2, seed) * 30), 2f, WithAlpha(p.Accent, 0.8f));
                    }
                    break;
                case HillStyle.Craters:
                    for (int i = 0; i < 9; i++)
                    {
                        float x = (i + 0.5f) * w / 9f, y = 40 + Hash(i, 1, seed) * 50;
                        float r = 12 + Hash(i, 2, seed) * 14;
                        c.Ellipse(x, y, r, r * 0.45f, Shade(hill, -0.2f));
                        c.Ellipse(x + 2, y + 2, r - 3, r * 0.35f, Shade(hill, -0.05f));
                    }
                    break;
            }
            return c;
        }

        static ArtCanvas Cloud(WorldPalette p, int variant)
        {
            var c = new ArtCanvas(256, 128);
            switch (p.Clouds)
            {
                case CloudStyle.Stars:
                {
                    // A little constellation with a comet streak
                    for (int i = 0; i < 9; i++)
                    {
                        float x = 30 + Hash(i, 1, variant + 3) * 196, y = 20 + Hash(i, 2, variant + 3) * 88;
                        float r = i == 0 ? 7f : 2f + Hash(i, 3, variant) * 3f;
                        c.Polygon(PropArt.StarPoints(x, y, r * 1.6f, r * 0.55f), WithAlpha(Color.white, 0.9f));
                    }
                    if (variant == 1)
                    {
                        c.Fill(new Rect(20, 40, 200, 40), (x, y) => SdCapsule(x, y, new Vector2(40, 70), new Vector2(200, 50), Mathf.Lerp(1f, 7f, Mathf.InverseLerp(40, 200, x))),
                            (x, y) => WithAlpha(Color.Lerp(p.Accent, Color.white, (x - 40) / 160f), Mathf.InverseLerp(40, 200, x)));
                    }
                    return c;
                }
                case CloudStyle.Bubbles:
                {
                    var bubbles = variant == 0
                        ? new[] { new Vector3(60, 40, 18), new Vector3(96, 70, 11), new Vector3(120, 44, 7), new Vector3(150, 86, 14), new Vector3(196, 58, 9) }
                        : new[] { new Vector3(70, 60, 12), new Vector3(110, 36, 8), new Vector3(150, 70, 16), new Vector3(190, 40, 6) };
                    foreach (var bb in bubbles)
                    {
                        c.Fill(new Rect(bb.x - bb.z - 3, bb.y - bb.z - 3, bb.z * 2 + 6, bb.z * 2 + 6),
                            (x, y) => Mathf.Abs(SdCircle(x, y, bb.x, bb.y, bb.z)) - 1.8f, WithAlpha(p.Cloud, 0.85f));
                        c.Circle(bb.x, bb.y, bb.z, WithAlpha(p.Cloud, 0.15f));
                        c.Circle(bb.x - bb.z * 0.4f, bb.y + bb.z * 0.4f, bb.z * 0.22f, WithAlpha(Color.white, 0.9f));
                    }
                    return c;
                }
                case CloudStyle.Wispy:
                {
                    Color body = p.Cloud, shade = Color.Lerp(p.Cloud, p.SkyTop, 0.25f);
                    c.RoundRect(Rect.MinMaxRect(20, 44, 236, 66), 11, shade);
                    c.RoundRect(Rect.MinMaxRect(60, 58, 200, 80), 11, body);
                    c.RoundRect(Rect.MinMaxRect(30, 48, 180, 64), 8, body);
                    if (variant == 0) c.RoundRect(Rect.MinMaxRect(120, 76, 230, 92), 8, WithAlpha(body, 0.85f));
                    return c;
                }
                default:
                {
                    Color body = p.Cloud;
                    Color shade = p.Clouds == CloudStyle.Smoke ? Shade(p.Cloud, -0.2f) : Color.Lerp(p.Cloud, p.SkyTop, 0.18f);
                    var puffs = variant == 0
                        ? new[] { new Vector3(70, 50, 34), new Vector3(118, 68, 46), new Vector3(170, 52, 36), new Vector3(205, 42, 24), new Vector3(40, 40, 22) }
                        : new[] { new Vector3(80, 48, 30), new Vector3(128, 62, 38), new Vector3(176, 46, 28) };
                    c.RoundRect(Rect.MinMaxRect(30, 22, 226, 52), 15, shade);
                    foreach (var pf in puffs) c.Circle(pf.x, pf.y, pf.z, shade);
                    foreach (var pf in puffs) c.Circle(pf.x, pf.y + 5, pf.z - 3, body);
                    c.RoundRect(Rect.MinMaxRect(34, 30, 222, 50), 12, body);
                    if (p.Clouds == CloudStyle.Smoke)
                        foreach (var pf in puffs) c.Circle(pf.x - 6, pf.y + 12, pf.z * 0.3f, WithAlpha(Color.white, 0.08f));
                    return c;
                }
            }
        }

        // ================================================================ decor

        static ArtCanvas Tree(WorldPalette p, int variant)
        {
            switch (p.Decor)
            {
                case DecorStyle.Cacti: return Cactus(p, variant);
                case DecorStyle.DeadTrees: return DeadTree(p, variant);
                case DecorStyle.Crystals: return CrystalSpire(p, variant);
                case DecorStyle.Candy: return variant == 0 ? Lollipop(p) : CandyCane(p);
                case DecorStyle.Seaweed: return variant == 0 ? SeaweedStalk(p) : CoralBranch(p);
                case DecorStyle.NeonPoles: return variant == 0 ? NeonLamp(p) : NeonSign(p);
            }

            var c = new ArtCanvas(128, 200);
            Color ink = p.Ink;
            c.RoundRect(new Rect(56, 0, 16, 60), 4, ink);
            c.RoundRect(new Rect(58, 0, 12, 58), 3, p.TreeTrunk);
            int tiers = variant == 0 ? 3 : 4;
            for (int i = 0; i < tiers; i++)
            {
                float y = 40 + i * (variant == 0 ? 42 : 34);
                float halfW = (variant == 0 ? 54 : 46) - i * 11;
                float hgt = variant == 0 ? 58 : 48;
                var tri = new[] { new Vector2(64 - halfW, y), new Vector2(64 + halfW, y), new Vector2(64, y + hgt) };
                c.PolygonOutlined(tri, p.TreeLeaf, ink, 2.5f);
                c.Polygon(new[] { new Vector2(64, y), new Vector2(64 + halfW, y), new Vector2(64, y + hgt) }, p.TreeLeafDark);
                c.RoundRect(new Rect(64 - halfW + 4, y + 1, halfW * 2 - 8, 5), 2, WithAlpha(Color.black, 0.12f));
                if (p.Decor == DecorStyle.SnowPines)
                {
                    // Snow resting on each tier
                    var snow = new[] { new Vector2(64 - halfW * 0.55f, y + hgt * 0.45f), new Vector2(64 + halfW * 0.55f, y + hgt * 0.45f), new Vector2(64, y + hgt) };
                    c.Polygon(snow, Color.white);
                    c.RoundRect(new Rect(64 - halfW + 2, y, halfW * 2 - 4, 6), 3, Color.white);
                }
            }
            return c;
        }

        static ArtCanvas Cactus(WorldPalette p, int variant)
        {
            var c = new ArtCanvas(128, 200);
            float trunkTop = variant == 0 ? 170 : 130;
            void Limb(Vector2 a, Vector2 b, float r)
            {
                c.CapsuleOutlined(a, b, r, p.TreeLeaf, p.Ink, 2.5f);
            }
            Limb(new Vector2(64, 8), new Vector2(64, trunkTop), 15);
            Limb(new Vector2(64, 70), new Vector2(34, 70), 9);
            Limb(new Vector2(34, 70), new Vector2(34, 118), 9);
            if (variant == 0)
            {
                Limb(new Vector2(64, 96), new Vector2(94, 96), 9);
                Limb(new Vector2(94, 96), new Vector2(94, 140), 9);
            }
            // Re-fill the trunk so joints read as one plant, then add ribs and a flower
            c.Capsule(new Vector2(64, 8), new Vector2(64, trunkTop), 15, p.TreeLeaf);
            c.Capsule(new Vector2(70, 12), new Vector2(70, trunkTop - 4), 3, p.TreeLeafDark);
            c.Capsule(new Vector2(56, 12), new Vector2(56, trunkTop - 4), 2, WithAlpha(Color.white, 0.2f));
            c.Circle(64, trunkTop + 14, 6, Hex("#FF6FA0"));
            c.RoundRect(new Rect(40, 0, 48, 8), 3, p.Ink);
            return c;
        }

        static ArtCanvas DeadTree(WorldPalette p, int variant)
        {
            var c = new ArtCanvas(128, 200);
            void Branch(Vector2 a, Vector2 b, float r) => c.CapsuleOutlined(a, b, r, p.TreeTrunk, p.Ink, 2f);
            Branch(new Vector2(64, 0), new Vector2(60, variant == 0 ? 150 : 120), 9);
            Branch(new Vector2(61, 80), new Vector2(26, 124), 5);
            Branch(new Vector2(61, 100), new Vector2(100, 150), 5);
            Branch(new Vector2(40, 108), new Vector2(34, 140), 3);
            if (variant == 0) Branch(new Vector2(60, 140), new Vector2(84, 180), 4);
            // Embers glinting on the charred bark
            c.Circle(58, 44, 3, p.Accent);
            c.Circle(66, 96, 2.2f, p.Accent);
            return c;
        }

        static ArtCanvas CrystalSpire(WorldPalette p, int variant)
        {
            var c = new ArtCanvas(128, 200);
            void Shard(float x, float w, float h, Color col)
            {
                var pts = new[] { new Vector2(x - w, 0), new Vector2(x + w, 0), new Vector2(x + w * 0.7f, h * 0.8f), new Vector2(x, h), new Vector2(x - w * 0.7f, h * 0.8f) };
                c.PolygonOutlined(pts, col, p.Ink, 2.5f);
                c.Polygon(new[] { new Vector2(x, 0), new Vector2(x + w, 0), new Vector2(x + w * 0.7f, h * 0.8f), new Vector2(x, h) }, Shade(col, -0.2f));
                c.Capsule(new Vector2(x - w * 0.35f, h * 0.2f), new Vector2(x - w * 0.2f, h * 0.65f), 2f, WithAlpha(Color.white, 0.45f));
            }
            Color main = variant == 0 ? p.TreeLeaf : Hex("#C77DFF");
            Shard(40, 16, variant == 0 ? 110 : 90, Shade(main, -0.1f));
            Shard(88, 14, variant == 0 ? 90 : 120, Shade(main, 0.05f));
            Shard(64, 22, variant == 0 ? 170 : 150, main);
            return c;
        }

        static ArtCanvas Bush(WorldPalette p)
        {
            var c = new ArtCanvas(128, 72);
            switch (p.Decor)
            {
                case DecorStyle.Cacti:
                    // Desert rock
                    c.PolygonOutlined(new[] { new Vector2(14, 0), new Vector2(114, 0), new Vector2(100, 40), new Vector2(70, 58), new Vector2(34, 46) }, Hex("#C9925C"), p.Ink, 2.5f);
                    c.Polygon(new[] { new Vector2(70, 0), new Vector2(114, 0), new Vector2(100, 40), new Vector2(70, 58) }, Hex("#A8743F"));
                    return c;
                case DecorStyle.SnowPines:
                    // Snowman!
                    c.CircleOutlined(64, 22, 20, Color.white, p.Ink, 2.5f);
                    c.CircleOutlined(64, 52, 13, Color.white, p.Ink, 2.5f);
                    c.Circle(60, 55, 1.8f, p.Ink);
                    c.Circle(69, 55, 1.8f, p.Ink);
                    c.Polygon(new[] { new Vector2(64, 50), new Vector2(64, 46), new Vector2(76, 48) }, Hex("#FF8A3D"));
                    return c;
                case DecorStyle.DeadTrees:
                    c.PolygonOutlined(new[] { new Vector2(20, 0), new Vector2(108, 0), new Vector2(96, 34), new Vector2(62, 50), new Vector2(30, 36) }, Hex("#3A2E33"), p.Ink, 2.5f);
                    c.Capsule(new Vector2(40, 10), new Vector2(60, 34), 2f, p.Accent);
                    c.Capsule(new Vector2(60, 34), new Vector2(84, 22), 2f, p.Accent);
                    return c;
                case DecorStyle.Candy:
                {
                    // Gumdrops
                    var drops = new[] { (36f, p.Accent), (64f, p.TreeLeaf), (92f, p.Accent2) };
                    foreach (var (x, col) in drops)
                    {
                        c.Fill(new Rect(x - 20, 0, 40, 40), (px, py) => Mathf.Max(SdEllipse(px, py, x, 0, 17, 32), -py) - 2.5f, p.Ink);
                        c.Fill(new Rect(x - 18, 0, 36, 36), (px, py) => Mathf.Max(SdEllipse(px, py, x, 0, 15, 30), -py), col);
                        c.Circle(x - 5, 20, 3, WithAlpha(Color.white, 0.6f));
                    }
                    return c;
                }
                case DecorStyle.Seaweed:
                {
                    var star = PropArt.StarPoints(64, 26, 26, 11);
                    c.PolygonOutlined(star, p.Accent, p.Ink, 2.5f);
                    c.Circle(64, 26, 4, Shade(p.Accent, 0.3f));
                    return c;
                }
                case DecorStyle.NeonPoles:
                    c.RoundRect(new Rect(24, 0, 80, 46), 6, p.Ink);
                    c.RoundRect(new Rect(27, 3, 74, 40), 5, p.TreeTrunk);
                    c.RoundRect(new Rect(27, 20, 74, 5), 2, p.Accent2);
                    c.RoundRect(new Rect(27, 21, 74, 3), 1.5f, Color.white);
                    return c;
                case DecorStyle.Crystals:
                {
                    var small = new[] { new Vector3(44, 30, 10), new Vector3(66, 44, 13), new Vector3(88, 26, 9) };
                    foreach (var s in small)
                        c.PolygonOutlined(new[] { new Vector2(s.x - s.z, 0), new Vector2(s.x + s.z, 0), new Vector2(s.x, s.y) }, p.Accent, p.Ink, 2f);
                    return c;
                }
            }

            var blobs = new[] { new Vector3(36, 26, 24), new Vector3(66, 36, 32), new Vector3(96, 26, 22) };
            foreach (var b in blobs) c.Circle(b.x, b.y, b.z + 2.5f, p.Ink);
            c.RoundRect(Rect.MinMaxRect(10, 0, 118, 26), 8, p.Ink);
            foreach (var b in blobs) c.Circle(b.x, b.y, b.z, p.TreeLeaf);
            c.RoundRect(Rect.MinMaxRect(13, 2, 115, 24), 6, p.TreeLeafDark);
            c.Circle(58, 48, 7, WithAlpha(Color.white, 0.18f));
            return c;
        }

        static ArtCanvas Lollipop(WorldPalette p)
        {
            var c = new ArtCanvas(128, 200);
            c.CapsuleOutlined(new Vector2(64, 0), new Vector2(64, 130), 4.5f, p.TreeTrunk, p.Ink, 2.5f);
            c.CircleOutlined(64, 150, 38, p.TreeLeaf, p.Ink, 3f);
            // Swirl
            for (int i = 0; i < 40; i++)
            {
                float t = i / 40f;
                float a0 = t * Mathf.PI * 5f, a1 = (i + 1) / 40f * Mathf.PI * 5f;
                float r0 = t * 34f, r1 = (i + 1) / 40f * 34f;
                c.Capsule(new Vector2(64 + Mathf.Cos(a0) * r0, 150 + Mathf.Sin(a0) * r0), new Vector2(64 + Mathf.Cos(a1) * r1, 150 + Mathf.Sin(a1) * r1), 3.2f, Color.white);
            }
            c.Circle(50, 166, 6, WithAlpha(Color.white, 0.5f));
            return c;
        }

        static ArtCanvas CandyCane(WorldPalette p)
        {
            var c = new ArtCanvas(128, 200);
            // Shaft then the hook, drawn as a capsule chain so stripes can follow it
            var pts = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i <= 12; i++) pts.Add(new Vector2(58, i * 12f));
            for (int i = 1; i <= 10; i++)
            {
                float a = Mathf.PI - i / 10f * Mathf.PI;
                pts.Add(new Vector2(78 + Mathf.Cos(a) * 20, 144 + Mathf.Sin(a) * 20));
            }
            for (int i = 0; i < pts.Count - 1; i++) c.Capsule(pts[i], pts[i + 1], 11.5f, p.Ink);
            for (int i = 0; i < pts.Count - 1; i++) c.Capsule(pts[i], pts[i + 1], 9f, i % 2 == 0 ? Hex("#E53950") : Color.white);
            return c;
        }

        static ArtCanvas SeaweedStalk(WorldPalette p)
        {
            var c = new ArtCanvas(128, 200);
            foreach (var (x, h, phase) in new[] { (46f, 170f, 0f), (70f, 190f, 1.4f), (90f, 140f, 2.6f) })
            {
                Vector2 prev = new Vector2(x, 0);
                for (int i = 1; i <= 10; i++)
                {
                    float y = i / 10f * h;
                    var next = new Vector2(x + Mathf.Sin(i * 0.8f + phase) * 9f, y);
                    c.Capsule(prev, next, Mathf.Lerp(7f, 3f, i / 10f) + 2.5f, p.Ink);
                    prev = next;
                }
                prev = new Vector2(x, 0);
                for (int i = 1; i <= 10; i++)
                {
                    float y = i / 10f * h;
                    var next = new Vector2(x + Mathf.Sin(i * 0.8f + phase) * 9f, y);
                    c.Capsule(prev, next, Mathf.Lerp(7f, 3f, i / 10f), i % 2 == 0 ? p.TreeLeaf : p.TreeLeafDark);
                    prev = next;
                }
            }
            return c;
        }

        static ArtCanvas CoralBranch(WorldPalette p)
        {
            var c = new ArtCanvas(128, 200);
            void Arm(Vector2 a, Vector2 b, float r)
            {
                c.CapsuleOutlined(a, b, r, p.Accent, p.Ink, 2.5f);
            }
            Arm(new Vector2(64, 0), new Vector2(64, 110), 10);
            Arm(new Vector2(64, 50), new Vector2(30, 100), 7);
            Arm(new Vector2(30, 100), new Vector2(26, 140), 6);
            Arm(new Vector2(64, 70), new Vector2(98, 120), 7);
            Arm(new Vector2(98, 120), new Vector2(104, 160), 5);
            c.Capsule(new Vector2(64, 0), new Vector2(64, 110), 10, p.Accent);
            for (int i = 0; i < 6; i++) c.Circle(58 + (i % 2) * 10, 20 + i * 16, 2.2f, Shade(p.Accent, 0.35f));
            return c;
        }

        static ArtCanvas NeonLamp(WorldPalette p)
        {
            var c = new ArtCanvas(128, 200);
            c.Circle(88, 160, 30, WithAlpha(p.Accent, 0.18f));
            c.CapsuleOutlined(new Vector2(40, 0), new Vector2(40, 164), 4.5f, p.TreeTrunk, p.Ink, 2.5f);
            c.CapsuleOutlined(new Vector2(40, 164), new Vector2(84, 164), 4f, p.TreeTrunk, p.Ink, 2.5f);
            c.RoundRect(new Rect(72, 148, 28, 14), 6, p.Ink);
            c.RoundRect(new Rect(75, 150, 22, 8), 4, p.Accent);
            c.RoundRect(new Rect(78, 151, 16, 4), 2, Color.white);
            c.RoundRect(new Rect(28, 0, 24, 10), 3, p.Ink);
            return c;
        }

        static ArtCanvas NeonSign(WorldPalette p)
        {
            var c = new ArtCanvas(128, 200);
            c.CapsuleOutlined(new Vector2(64, 0), new Vector2(64, 110), 4.5f, p.TreeTrunk, p.Ink, 2.5f);
            Rect board = new Rect(14, 104, 100, 70);
            c.RoundRect(new Rect(board.x - 6, board.y - 6, board.width + 12, board.height + 12), 14, WithAlpha(p.Accent2, 0.2f));
            c.RoundRect(board, 10, p.Ink);
            c.Fill(new Rect(board.x, board.y, board.width, board.height), (x, y) => Mathf.Abs(SdRoundRect(x, y, new Rect(board.x + 6, board.y + 6, board.width - 12, board.height - 12), 6)) - 2.5f, p.Accent2);
            // A little block glyph and an arrow
            c.RoundRect(new Rect(32, 124, 26, 26), 5, p.Accent);
            c.Polygon(new[] { new Vector2(70, 124), new Vector2(70, 150), new Vector2(96, 137) }, p.Accent);
            return c;
        }

        static ArtCanvas Skyline(WorldPalette p, Color color, int w, int h, int seed, float minH, float maxH)
        {
            var c = new ArtCanvas(w, h);
            Color windowOn = WithAlpha(p.Accent, 0.9f), windowAlt = WithAlpha(p.Accent2, 0.85f);
            float x = 0;
            int i = 0;
            while (x < w)
            {
                float bw = 50 + Hash(i, 1, seed) * 70;
                if (x + bw > w - 20) bw = w - x;   // last building closes the wrap seamlessly
                float bh = Mathf.Lerp(minH * 0.6f, maxH * 0.9f, Hash(i, 2, seed));
                Color body = Hash(i, 3, seed) > 0.5f ? color : Shade(color, 0.06f);
                c.RoundRect(new Rect(x + 2, 0, bw - 4, bh), 2, body);
                // Rooftop detail: antenna or step
                if (Hash(i, 4, seed) > 0.6f) c.RoundRect(new Rect(x + bw * 0.5f - 1.5f, bh, 3, 22), 1, body);
                else c.RoundRect(new Rect(x + bw * 0.25f, bh, bw * 0.5f, 10), 2, body);
                // Windows
                for (float wy = 14; wy < bh - 10; wy += 16)
                for (float wx = x + 10; wx < x + bw - 12; wx += 14)
                {
                    float r = Hash((int)wx, (int)wy, seed + i);
                    if (r < 0.35f) c.RoundRect(new Rect(wx, wy, 6, 8), 1, r < 0.08f ? windowAlt : windowOn);
                }
                x += bw;
                i++;
            }
            return c;
        }

        static ArtCanvas Tuft(WorldPalette p)
        {
            var c = new ArtCanvas(48, 32);
            Color a = p.GrassDark, b = p.Grass;
            switch (p.Decor)
            {
                case DecorStyle.Cacti: a = Hex("#B98A4E"); b = Hex("#D8AA66"); break;
                case DecorStyle.SnowPines: a = p.Accent; b = Color.white; break;
                case DecorStyle.DeadTrees: a = Shade(p.Accent, -0.2f); b = p.Accent; break;
                case DecorStyle.Crystals: a = p.TreeLeafDark; b = p.TreeLeaf; break;
                case DecorStyle.Candy: a = p.Accent; b = p.Accent2; break;
                case DecorStyle.Seaweed: a = p.TreeLeafDark; b = p.TreeLeaf; break;
                case DecorStyle.NeonPoles: a = p.Accent2; b = p.Accent; break;
            }
            c.Capsule(new Vector2(14, 2), new Vector2(8, 22), 3, a);
            c.Capsule(new Vector2(24, 2), new Vector2(24, 28), 3, b);
            c.Capsule(new Vector2(33, 2), new Vector2(40, 20), 3, a);
            return c;
        }

        // ================================================================ shop thumbnail

        static ArtCanvas Thumbnail(WorldPalette p, ArtCanvas far, ArtCanvas near, ArtCanvas hills, ArtCanvas cloud, ArtCanvas tree)
        {
            const int w = 320, h = 200;
            var c = new ArtCanvas(w, h);
            c.Fill(new Rect(0, 0, w, h), (x, y) => SdRoundRect(x, y, new Rect(0, 0, w, h), 24),
                (x, y) => Color.Lerp(p.SkyBottom, p.SkyTop, Mathf.SmoothStep(0, 1, y / h)));

            var scene = new ArtCanvas(w, h);
            scene.Stamp(Downscale(far, 2), -60, 30);
            scene.Stamp(Downscale(cloud, 2), 180, 140);
            scene.Stamp(Downscale(near, 2), -200, 20);
            scene.Stamp(Downscale(hills, 2), -100, 30);
            var top = Downscale(GroundTop(p, false, false), 2);
            var fill = Downscale(GroundFill(p, false, false, 1), 2);
            for (int i = 0; i < 7; i++)
            {
                scene.Stamp(fill, i * 50, 0);
                scene.Stamp(top, i * 50, 50);
            }
            scene.Stamp(Downscale(tree, 2), 200, 100);

            // Clip the composed scene to the rounded card
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color s = scene.Get(x, y);
                float mask = Mathf.Clamp01(0.5f - SdRoundRect(x + 0.5f, y + 0.5f, new Rect(0, 0, w, h), 24));
                if (s.a > 0) c.Blend(x, y, new Color(s.r, s.g, s.b, 1f), s.a * mask);
            }
            return c;
        }

        static ArtCanvas Downscale(ArtCanvas src, int factor)
        {
            var dst = new ArtCanvas(src.Width / factor, src.Height / factor);
            for (int y = 0; y < dst.Height; y++)
            for (int x = 0; x < dst.Width; x++)
            {
                Color sum = Color.clear;
                for (int oy = 0; oy < factor; oy++)
                for (int ox = 0; ox < factor; ox++)
                {
                    Color s = src.Get(x * factor + ox, y * factor + oy);
                    sum += new Color(s.r * s.a, s.g * s.a, s.b * s.a, s.a);
                }
                sum /= factor * factor;
                if (sum.a > 0) dst.Blend(x, y, new Color(sum.r / sum.a, sum.g / sum.a, sum.b / sum.a, 1f), sum.a);
            }
            return dst;
        }
    }
}
