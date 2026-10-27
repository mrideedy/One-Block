using UnityEngine;
using static OneBlock.EditorTools.Art.ArtCanvas;

namespace OneBlock.EditorTools.Art
{
    /// <summary>Single source of truth for cosmetic names, prices and colours (used by art + data builders).</summary>
    public static class CosmeticDefs
    {
        public struct CharacterDef
        {
            public string Id, Name;
            public int Price;
            public string BundleId;
        }

        public struct TrailDef
        {
            public string Id, Name;
            public int Price;
            public string BundleId;
            public Color[] Colors;
        }

        public struct EffectDef
        {
            public string Id, Name;
            public int Price;
            public string BundleId;
            public OneBlock.Data.DeathEffectStyle Style;
            public Color[] Palette;
        }

        public static readonly CharacterDef[] Characters =
        {
            new() { Id = "default", Name = "Default", Price = 0 },
            new() { Id = "ninja", Name = "Ninja", Price = 750 },
            new() { Id = "astronaut", Name = "Astronaut", Price = 1200 },
            new() { Id = "robot", Name = "Robot", Price = 1000 },
            new() { Id = "ghost", Name = "Ghost Wizard", Price = 1500 },
            new() { Id = "candy", Name = "Candy Kid", BundleId = "bundle_candy" },
            new() { Id = "diver", Name = "Diver", BundleId = "bundle_ocean" },
            new() { Id = "cyber", Name = "Cyber Ninja", BundleId = "bundle_neon" },
        };

        public struct BlockDef
        {
            public string Id, Name;
            public int Price;
            public string BundleId;
        }

        public static readonly BlockDef[] Blocks =
        {
            new() { Id = "normal", Name = "Normal", Price = 0 },
            new() { Id = "bouncy", Name = "Bouncy", Price = 250 },
            new() { Id = "ice", Name = "Ice", Price = 250 },
            new() { Id = "explosive", Name = "Explosive", Price = 400 },
            new() { Id = "ghost", Name = "Ghost", Price = 400 },
            new() { Id = "stone", Name = "Stone", Price = 150 },
            new() { Id = "wood", Name = "Wood", Price = 150 },
            new() { Id = "grass", Name = "Grass", Price = 200 },
            new() { Id = "crystal", Name = "Crystal", Price = 500 },
            new() { Id = "galaxy", Name = "Galaxy", Price = 750 },
            new() { Id = "candy", Name = "Candy", BundleId = "bundle_candy" },
            new() { Id = "coral", Name = "Coral", BundleId = "bundle_ocean" },
            new() { Id = "neon", Name = "Neon", BundleId = "bundle_neon" },
        };

        public struct BundleDef
        {
            public string Id, Name, Description, World, Character, Block, Trail, Effect;
            public int Ads;
        }

        public static readonly BundleDef[] Bundles =
        {
            new() { Id = "bundle_candy", Name = "Candy Land", Ads = 15, Description = "A sugar-rush world with a hero, block, trail and effect to match.",
                World = "candy", Character = "candy", Block = "candy", Trail = "trail_sprinkles", Effect = "fx_sprinkles" },
            new() { Id = "bundle_ocean", Name = "Deep Ocean", Ads = 18, Description = "Dive under the waves with the full ocean set.",
                World = "ocean", Character = "diver", Block = "coral", Trail = "trail_bubbles", Effect = "fx_bubbles" },
            new() { Id = "bundle_neon", Name = "Neon City", Ads = 20, Description = "Glow through the city at night with the complete neon set.",
                World = "neon", Character = "cyber", Block = "neon", Trail = "trail_neon", Effect = "fx_glitch" },
        };

        public static readonly TrailDef[] Trails =
        {
            new() { Id = "trail_blue", Name = "Blue Streak", Price = 300, Colors = new[] { Hex("#7FD8FF"), Hex("#2F6CD6") } },
            new() { Id = "trail_leaf", Name = "Leaf", Price = 400, Colors = new[] { Hex("#B5F27A"), Hex("#2E9E3A") } },
            new() { Id = "trail_fire", Name = "Fire", Price = 600, Colors = new[] { Hex("#FFE36B"), Hex("#FF8A2A"), Hex("#E53935") } },
            new() { Id = "trail_galaxy", Name = "Galaxy", Price = 800, Colors = new[] { Hex("#F0C8FF"), Hex("#8B5CF6"), Hex("#3B1F8F") } },
            new() { Id = "trail_rainbow", Name = "Rainbow", Price = 1200, Colors = new[] { Hex("#FF5A5A"), Hex("#FFC23A"), Hex("#5DBE3C"), Hex("#2F86F0"), Hex("#9B5CF0") } },
            new() { Id = "trail_sprinkles", Name = "Sprinkles", BundleId = "bundle_candy", Colors = new[] { Hex("#FFC4E1"), Hex("#7FE3D0"), Hex("#FFD23F"), Hex("#FF6FAE") } },
            new() { Id = "trail_bubbles", Name = "Bubbles", BundleId = "bundle_ocean", Colors = new[] { Hex("#E6FBFF"), Hex("#8FE3FF"), Hex("#2F9BD6") } },
            new() { Id = "trail_neon", Name = "Neon", BundleId = "bundle_neon", Colors = new[] { Hex("#3DF2FF"), Hex("#8A6CFF"), Hex("#FF3DCB") } },
        };

        public static readonly EffectDef[] Effects =
        {
            new() { Id = "fx_explosion", Name = "Explosion", Price = 300, Style = OneBlock.Data.DeathEffectStyle.Explosion,
                Palette = new[] { Hex("#FFF2A8"), Hex("#FFC23A"), Hex("#FF7A2A"), Hex("#E53935") } },
            new() { Id = "fx_poof", Name = "Cloud Poof", Price = 300, Style = OneBlock.Data.DeathEffectStyle.Poof,
                Palette = new[] { Color.white, Hex("#E3E8F2"), Hex("#C9D2E3") } },
            new() { Id = "fx_skull", Name = "Skull", Price = 500, Style = OneBlock.Data.DeathEffectStyle.Skull,
                Palette = new[] { Color.white, Hex("#BFC6D6") } },
            new() { Id = "fx_confetti", Name = "Confetti", Price = 700, Style = OneBlock.Data.DeathEffectStyle.Confetti,
                Palette = new[] { Hex("#FF5A5A"), Hex("#FFC23A"), Hex("#5DBE3C"), Hex("#2F86F0"), Hex("#9B5CF0") } },
            new() { Id = "fx_sprinkles", Name = "Sprinkle Burst", BundleId = "bundle_candy", Style = OneBlock.Data.DeathEffectStyle.Confetti,
                Palette = new[] { Hex("#FF6FAE"), Hex("#7FE3D0"), Hex("#FFD23F"), Color.white, Hex("#8A6CFF") } },
            new() { Id = "fx_bubbles", Name = "Bubble Pop", BundleId = "bundle_ocean", Style = OneBlock.Data.DeathEffectStyle.Poof,
                Palette = new[] { Hex("#E6FBFF"), Hex("#8FE3FF"), Hex("#5CC8F0") } },
            new() { Id = "fx_glitch", Name = "Glitch", BundleId = "bundle_neon", Style = OneBlock.Data.DeathEffectStyle.Explosion,
                Palette = new[] { Color.white, Hex("#3DF2FF"), Hex("#8A6CFF"), Hex("#FF3DCB") } },
        };
    }
}
