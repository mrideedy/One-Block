using OneBlock.Core;
using OneBlock.Data;

namespace OneBlock.Save
{
    public enum PurchaseResult
    {
        Purchased,
        AlreadyOwned,
        NotEnoughCoins,
        BundleOnly
    }

    /// <summary>Ownership, purchases, ad-bundle progress and equipped cosmetics, stored in <see cref="SaveData"/>.</summary>
    public static class Inventory
    {
        public static bool IsOwned(string id) => string.IsNullOrEmpty(id) || SaveSystem.Data.ownedItems.Contains(id);

        public static string Equipped(ShopCategory category)
        {
            var d = SaveSystem.Data;
            return category switch
            {
                ShopCategory.Characters => d.equippedCharacter,
                ShopCategory.Blocks => d.equippedBlock,
                ShopCategory.Trails => d.equippedTrail,
                ShopCategory.Worlds => d.selectedWorld,
                _ => d.equippedDeathEffect
            };
        }

        public static bool IsEquipped(ShopItem item)
        {
            if (item.Category == ShopCategory.Bundles)
            {
                var b = item.AsBundle;
                var d = SaveSystem.Data;
                return b != null && d.selectedWorld == b.world.id && d.equippedCharacter == b.character.id &&
                       d.equippedBlock == b.block.id && d.equippedTrail == b.trail.id && d.equippedDeathEffect == b.effect.id;
            }
            return (Equipped(item.Category) ?? "") == item.Id;
        }

        public static PurchaseResult Buy(ShopItem item)
        {
            if (IsOwned(item.Id)) return PurchaseResult.AlreadyOwned;
            if (item.IsBundleExclusive || item.Category == ShopCategory.Bundles) return PurchaseResult.BundleOnly;
            var d = SaveSystem.Data;
            if (d.coins < item.Price) return PurchaseResult.NotEnoughCoins;

            d.coins -= item.Price;
            d.ownedItems.Add(item.Id);
            SaveSystem.Save();
            GameEvents.RaiseWalletChanged(d.coins);
            return PurchaseResult.Purchased;
        }

        // ------------------------------------------------------------------ ad bundles

        public static int BundleAdsWatched(string bundleId)
        {
            foreach (var p in SaveSystem.Data.bundleProgress)
                if (p.id == bundleId) return p.adsWatched;
            return 0;
        }

        /// <summary>Records one rewarded ad towards a bundle. Returns true when this ad completed it.</summary>
        public static bool AddBundleAd(Bundle bundle)
        {
            if (bundle == null || IsOwned(bundle.id)) return false;
            var d = SaveSystem.Data;
            BundleProgress progress = null;
            foreach (var p in d.bundleProgress)
                if (p.id == bundle.id) progress = p;
            if (progress == null)
            {
                progress = new BundleProgress { id = bundle.id };
                d.bundleProgress.Add(progress);
            }
            progress.adsWatched++;

            bool completed = progress.adsWatched >= bundle.adsRequired;
            if (completed)
                foreach (var id in bundle.ItemIds)
                    if (!d.ownedItems.Contains(id)) d.ownedItems.Add(id);
            SaveSystem.Save();
            return completed;
        }

        /// <summary>Equips every piece of an owned bundle at once.</summary>
        public static void EquipBundle(Bundle bundle)
        {
            if (bundle == null || !IsOwned(bundle.id)) return;
            var d = SaveSystem.Data;
            d.selectedWorld = bundle.world.id;
            d.equippedCharacter = bundle.character.id;
            d.equippedBlock = bundle.block.id;
            d.equippedTrail = bundle.trail.id;
            d.equippedDeathEffect = bundle.effect.id;
            SaveSystem.Save();
            GameEvents.RaiseCosmeticsChanged();
        }

        public static bool Equip(ShopItem item)
        {
            if (!IsOwned(item.Id)) return false;
            if (item.Category == ShopCategory.Bundles)
            {
                EquipBundle(item.AsBundle);
                return true;
            }
            var d = SaveSystem.Data;
            switch (item.Category)
            {
                case ShopCategory.Characters: d.equippedCharacter = item.Id; break;
                case ShopCategory.Blocks: d.equippedBlock = item.Id; break;
                case ShopCategory.Trails: d.equippedTrail = item.Id; break;
                case ShopCategory.Worlds: d.selectedWorld = item.Id; break;
                default: d.equippedDeathEffect = item.Id; break;
            }
            SaveSystem.Save();
            GameEvents.RaiseCosmeticsChanged();
            return true;
        }
    }
}
