using System.Collections.Generic;
using UnityEngine;

namespace OneBlock.Data
{
    public enum ShopCategory
    {
        Characters,
        Blocks,
        Trails,
        Effects,
        Worlds,
        Bundles
    }

    /// <summary>A purchasable cosmetic, flattened for the shop UI.</summary>
    public readonly struct ShopItem
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Price;
        public readonly Sprite Icon;
        public readonly ShopCategory Category;
        public readonly ScriptableObject Asset;
        /// <summary>Non-empty when the item is exclusive to an ad bundle (not sold for coins).</summary>
        public readonly string BundleId;

        public ShopItem(string id, string name, int price, Sprite icon, ShopCategory category, ScriptableObject asset, string bundleId = null)
        {
            Id = id;
            Name = name;
            Price = price;
            Icon = icon;
            Category = category;
            Asset = asset;
            BundleId = bundleId;
        }

        /// <summary>"None" entries (no trail, no effect) have an empty id and are always owned.</summary>
        public bool IsNone => string.IsNullOrEmpty(Id);
        public bool IsBundleExclusive => !string.IsNullOrEmpty(BundleId);
        public Bundle AsBundle => Asset as Bundle;
    }

    /// <summary>Every cosmetic in the game, grouped by shop tab.</summary>
    [CreateAssetMenu(fileName = "ShopCatalog", menuName = "One Block/Shop Catalog")]
    public class ShopCatalog : ScriptableObject
    {
        public CharacterSkin[] characters;
        public BlockSkin[] blocks;
        public TrailSkin[] trails;
        public DeathEffect[] effects;
        public WorldTheme[] worlds;
        public Bundle[] bundles;
        public Sprite noneIcon;

        public List<ShopItem> Items(ShopCategory category)
        {
            var list = new List<ShopItem>();
            switch (category)
            {
                case ShopCategory.Characters:
                    foreach (var c in characters) list.Add(new ShopItem(c.id, c.displayName, c.price, c.portrait, category, c, c.bundleId));
                    break;
                case ShopCategory.Blocks:
                    foreach (var b in blocks) list.Add(new ShopItem(b.id, b.displayName, b.price, b.sprite, category, b, b.bundleId));
                    break;
                case ShopCategory.Trails:
                    list.Add(new ShopItem("", "None", 0, noneIcon, category, null));
                    foreach (var t in trails) list.Add(new ShopItem(t.id, t.displayName, t.price, t.icon, category, t, t.bundleId));
                    break;
                case ShopCategory.Effects:
                    list.Add(new ShopItem("", "None", 0, noneIcon, category, null));
                    foreach (var e in effects) list.Add(new ShopItem(e.id, e.displayName, e.price, e.icon, category, e, e.bundleId));
                    break;
                case ShopCategory.Worlds:
                    foreach (var w in worlds) list.Add(new ShopItem(w.id, w.displayName, w.price, w.thumbnail, category, w, w.bundleId));
                    break;
                case ShopCategory.Bundles:
                    foreach (var b in bundles) list.Add(new ShopItem(b.id, b.displayName, 0, b.thumbnail, category, b));
                    break;
            }
            return list;
        }

        public CharacterSkin Character(string id) => Find(characters, id, c => c.id) ?? characters[0];
        public BlockSkin Block(string id) => Find(blocks, id, b => b.id) ?? blocks[0];
        public TrailSkin Trail(string id) => Find(trails, id, t => t.id);
        public DeathEffect Effect(string id) => Find(effects, id, e => e.id);
        public WorldTheme World(string id) => Find(worlds, id, w => w.id) ?? worlds[0];
        public Bundle BundleById(string id) => Find(bundles, id, b => b.id);

        static T Find<T>(T[] items, string id, System.Func<T, string> key) where T : class
        {
            if (items == null || string.IsNullOrEmpty(id)) return null;
            foreach (var item in items)
                if (item != null && key(item) == id) return item;
            return null;
        }
    }
}
