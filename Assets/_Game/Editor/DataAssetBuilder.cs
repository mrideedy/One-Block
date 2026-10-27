using System.Linq;
using OneBlock.Data;
using UnityEditor;
using UnityEngine;

namespace OneBlock.EditorTools
{
    /// <summary>
    /// Creates/updates the ScriptableObject data (skins, worlds, shared asset refs) from generated art.
    /// Existing assets are updated in place so references and GUIDs stay stable.
    /// </summary>
    public static class DataAssetBuilder
    {
        const string ArtRoot = "Assets/_Game/Art/Generated/";
        const string Data = ProjectSetup.DataDir;

        [MenuItem("One Block/Setup/Build Data Assets", priority = 22)]
        public static GameAssets BuildAll()
        {
            ProjectSetup.EnsureConfig();
            var characters = new CharacterSkin[Art.CosmeticDefs.Characters.Length];
            for (int i = 0; i < characters.Length; i++)
            {
                var def = Art.CosmeticDefs.Characters[i];
                characters[i] = BuildCharacter(def.Id, def.Name, def.Price, def.BundleId);
            }
            var character = characters[0];
            var blocks = new BlockSkin[Art.CosmeticDefs.Blocks.Length];
            for (int i = 0; i < blocks.Length; i++)
            {
                var def = Art.CosmeticDefs.Blocks[i];
                blocks[i] = BuildBlock(def.Id, def.Name, def.Price, def.BundleId);
            }
            var worlds = new WorldTheme[Art.EnvironmentArt.All.Length];
            for (int i = 0; i < worlds.Length; i++) worlds[i] = BuildWorld(Art.EnvironmentArt.All[i]);
            var world = worlds[0];

            var trails = new TrailSkin[Art.CosmeticDefs.Trails.Length];
            for (int i = 0; i < trails.Length; i++) trails[i] = BuildTrail(Art.CosmeticDefs.Trails[i]);
            var effects = new DeathEffect[Art.CosmeticDefs.Effects.Length];
            for (int i = 0; i < effects.Length; i++) effects[i] = BuildEffect(Art.CosmeticDefs.Effects[i]);
            var bundles = new Bundle[Art.CosmeticDefs.Bundles.Length];
            for (int i = 0; i < bundles.Length; i++) bundles[i] = BuildBundle(Art.CosmeticDefs.Bundles[i], characters, blocks, worlds, trails, effects);

            var catalog = LoadOrCreate<ShopCatalog>($"{Data}/ShopCatalog.asset");
            catalog.characters = characters;
            catalog.blocks = blocks;
            catalog.trails = trails;
            catalog.effects = effects;
            catalog.worlds = worlds;
            catalog.bundles = bundles;
            catalog.noneIcon = LoadSprite("UI/Icons/shop_none");
            EditorUtility.SetDirty(catalog);

            var assets = LoadOrCreate<GameAssets>($"{Data}/GameAssets.asset");
            assets.defaultCharacter = character;
            assets.defaultBlock = blocks[0];
            assets.defaultWorld = world;
            assets.blockPreview = LoadSprite("Blocks/block_preview");
            assets.blockPreviewBlocked = LoadSprite("Blocks/block_preview_blocked");
            assets.spikes = LoadSprite("Props/spikes");
            assets.flag = LoadSprite("Props/flag");
            assets.coinFrames = Enumerable.Range(0, 4).Select(i => LoadSprite($"Props/coin_{i}")).ToArray();
            assets.blockPack = LoadSprite("Blocks/block_normal");
            assets.whitePixel = LoadSprite("UI/white");
            EditorUtility.SetDirty(assets);

            AssetDatabase.SaveAssets();
            return assets;
        }

        static Bundle BuildBundle(Art.CosmeticDefs.BundleDef def, CharacterSkin[] characters, BlockSkin[] blocks,
            WorldTheme[] worlds, TrailSkin[] trails, DeathEffect[] effects)
        {
            var b = LoadOrCreate<Bundle>($"{Data}/Bundles/{def.Id}.asset");
            b.id = def.Id;
            b.displayName = def.Name;
            b.description = def.Description;
            b.adsRequired = def.Ads;
            b.thumbnail = LoadSprite($"UI/Bundles/{def.Id}");
            b.world = System.Array.Find(worlds, x => x.id == $"world_{def.World}");
            b.character = System.Array.Find(characters, x => x.id == $"char_{def.Character}");
            b.block = System.Array.Find(blocks, x => x.id == $"block_{def.Block}");
            b.trail = System.Array.Find(trails, x => x.id == def.Trail);
            b.effect = System.Array.Find(effects, x => x.id == def.Effect);
            if (!b.world || !b.character || !b.block || !b.trail || !b.effect)
                Debug.LogError($"[One Block] Bundle {def.Id} has a missing item.");
            EditorUtility.SetDirty(b);
            return b;
        }

        static CharacterSkin BuildCharacter(string id, string name, int price, string bundleId)
        {
            string p = $"Characters/{id}/{id}";
            var skin = LoadOrCreate<CharacterSkin>($"{Data}/Characters/char_{id}.asset");
            skin.id = $"char_{id}";
            skin.displayName = name;
            skin.price = price;
            skin.bundleId = bundleId;
            skin.idle = new[] { LoadSprite($"{p}_idle_0"), LoadSprite($"{p}_idle_1") };
            skin.run = Enumerable.Range(0, 4).Select(i => LoadSprite($"{p}_run_{i}")).ToArray();
            skin.jump = LoadSprite($"{p}_jump");
            skin.fall = LoadSprite($"{p}_fall");
            skin.hit = LoadSprite($"{p}_hit");
            skin.portrait = LoadSprite($"{p}_portrait");
            EditorUtility.SetDirty(skin);
            return skin;
        }

        public static ShopCatalog LoadCatalog() => AssetDatabase.LoadAssetAtPath<ShopCatalog>($"{Data}/ShopCatalog.asset");

        static TrailSkin BuildTrail(Art.CosmeticDefs.TrailDef def)
        {
            var skin = LoadOrCreate<TrailSkin>($"{Data}/Trails/{def.Id}.asset");
            skin.id = def.Id;
            skin.displayName = def.Name;
            skin.price = def.Price;
            skin.bundleId = def.BundleId;
            skin.icon = LoadSprite($"UI/Icons/shop_{def.Id}");
            var keys = new GradientColorKey[def.Colors.Length];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = new GradientColorKey(def.Colors[i], keys.Length == 1 ? 0f : i / (float)(keys.Length - 1));
            var g = new Gradient();
            g.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.75f, 0.5f), new GradientAlphaKey(0f, 1f) });
            skin.colors = g;
            skin.width = def.Id == "trail_rainbow" ? 0.7f : 0.6f;
            skin.lifetime = 0.34f;
            EditorUtility.SetDirty(skin);
            return skin;
        }

        static DeathEffect BuildEffect(Art.CosmeticDefs.EffectDef def)
        {
            var fx = LoadOrCreate<DeathEffect>($"{Data}/Effects/{def.Id}.asset");
            fx.id = def.Id;
            fx.displayName = def.Name;
            fx.price = def.Price;
            fx.bundleId = def.BundleId;
            fx.icon = LoadSprite($"UI/Icons/shop_{def.Id}");
            fx.style = def.Style;
            fx.palette = def.Palette;
            fx.particles = def.Style switch
            {
                DeathEffectStyle.Explosion => new[] { LoadSprite("FX/fx_spark"), LoadSprite("FX/fx_flash") },
                DeathEffectStyle.Poof => new[] { LoadSprite("FX/fx_puff") },
                DeathEffectStyle.Skull => new[] { LoadSprite("FX/fx_skull"), LoadSprite("FX/fx_puff") },
                _ => new[] { LoadSprite("FX/fx_square") }
            };
            EditorUtility.SetDirty(fx);
            return fx;
        }

        static BlockSkin BuildBlock(string id, string name, int price, string bundleId)
        {
            var skin = LoadOrCreate<BlockSkin>($"{Data}/Blocks/block_{id}.asset");
            skin.id = $"block_{id}";
            skin.displayName = name;
            skin.price = price;
            skin.bundleId = bundleId;
            skin.sprite = LoadSprite($"Blocks/block_{id}");
            EditorUtility.SetDirty(skin);
            return skin;
        }

        static WorldTheme BuildWorld(Art.EnvironmentArt.WorldPalette palette)
        {
            string id = palette.Id;
            string w = $"Worlds/{id}/";
            var theme = LoadOrCreate<WorldTheme>($"{Data}/Worlds/world_{id}.asset");
            theme.id = $"world_{id}";
            theme.displayName = palette.Name;
            theme.price = palette.Price;
            theme.bundleId = palette.BundleId;
            theme.thumbnail = LoadSprite($"UI/Worlds/world_{id}");
            theme.tileTop = LoadSprite(w + "tile_top");
            theme.tileTopLeft = LoadSprite(w + "tile_top_left");
            theme.tileTopRight = LoadSprite(w + "tile_top_right");
            theme.tileTopSingle = LoadSprite(w + "tile_top_single");
            theme.tileFill = LoadSprite(w + "tile_fill");
            theme.tileFillLeft = LoadSprite(w + "tile_fill_left");
            theme.tileFillRight = LoadSprite(w + "tile_fill_right");
            theme.tileFillSingle = LoadSprite(w + "tile_fill_single");
            theme.sky = LoadSprite(w + "bg_sky");
            theme.mountainsFar = LoadSprite(w + "bg_mountains_far");
            theme.mountainsNear = LoadSprite(w + "bg_mountains_near");
            theme.hills = LoadSprite(w + "bg_hills");
            theme.clouds = new[] { LoadSprite(w + "cloud_0"), LoadSprite(w + "cloud_1") };
            theme.trees = new[] { LoadSprite(w + "decor_tree_0"), LoadSprite(w + "decor_tree_1") };
            theme.bush = LoadSprite(w + "decor_bush");
            theme.tuft = LoadSprite(w + "decor_tuft");
            theme.hazardName = Art.HazardArt.DisplayName(palette.Hazards);
            theme.enemyStyle = palette.Enemies.ToString();
            theme.hazardFrames = Enumerable.Range(0, Art.HazardArt.HazardFrames).Select(i => LoadSprite(w + $"hazard_{i}")).ToArray();
            theme.enemyFrames = Enumerable.Range(0, Art.HazardArt.EnemyFrames).Select(i => LoadSprite(w + $"enemy_{i}")).ToArray();
            theme.hillsFill = ArtCanvasShade(palette.HillColor);
            theme.cameraBackground = palette.SkyBottom;
            EditorUtility.SetDirty(theme);
            return theme;
        }

        // The hills sprite shades its lower body 12% darker; the fill below must match it
        static Color ArtCanvasShade(Color c) => OneBlock.EditorTools.Art.ArtCanvas.Shade(c, -0.12f);

        static Sprite LoadSprite(string relative)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}{relative}.png");
            if (s == null) Debug.LogWarning($"[One Block] Missing sprite: {relative}");
            return s;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            ProjectSetup.EnsureFolder(System.IO.Path.GetDirectoryName(path)!.Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
