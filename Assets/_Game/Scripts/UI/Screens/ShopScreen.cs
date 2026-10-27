using OneBlock.Core;
using OneBlock.Data;
using OneBlock.Gameplay;
using OneBlock.Save;
using OneBlock.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneBlock.UI
{
    /// <summary>
    /// Tabbed cosmetics shop. Tap a locked item to buy it with coins (then it auto-equips); tap an owned item to equip.
    /// The Bundles tab unlocks themed sets by watching rewarded ads.
    /// </summary>
    public class ShopScreen : UIScreen
    {
        [SerializeField] RunController run;
        [SerializeField] UIRouter router;
        [SerializeField] Button closeButton;
        [SerializeField] Button[] tabButtons = new Button[6];
        [SerializeField] Image[] tabBackgrounds = new Image[6];
        [SerializeField] TMP_Text[] tabLabels = new TMP_Text[6];
        [SerializeField] ScrollRect scroll;
        [SerializeField] ShopCard[] cards;
        [SerializeField] BundleCard[] bundleCards;
        [SerializeField] TMP_Text coinsText;
        [SerializeField] Image previewIcon;
        [SerializeField] TMP_Text previewName;
        [SerializeField] float cardRowHeight = 300f;
        [SerializeField] float bundleRowHeight = 700f;
        [SerializeField] int columns = 5;

        static readonly Color TabSelected = new Color(1f, 0.76f, 0.23f, 1f);
        static readonly Color TabNormal = new Color(0.19f, 0.24f, 0.32f, 1f);

        ShopCategory _category;
        bool _adInProgress;

        void Start()
        {
            closeButton.onClick.AddListener(Hide);
            for (int i = 0; i < tabButtons.Length; i++)
            {
                var cat = (ShopCategory)i;
                tabButtons[i].onClick.AddListener(() => SelectTab(cat));
            }
            foreach (var card in cards) card.Clicked += OnCardClicked;
            foreach (var card in bundleCards) card.Clicked += OnBundleClicked;
        }

        void OnEnable() => GameEvents.WalletChanged += OnWallet;
        void OnDisable() => GameEvents.WalletChanged -= OnWallet;

        protected override void OnShow() => SelectTab(_category);

        /// <summary>Opens the shop directly on a tab, e.g. from the world button on the menu.</summary>
        public void ShowTab(ShopCategory category)
        {
            _category = category;
            if (IsVisible) SelectTab(category);
            else Show();
        }

        void OnWallet(int coins)
        {
            coinsText.text = coins.ToString();
            if (IsVisible) Refresh();
        }

        void SelectTab(ShopCategory category)
        {
            _category = category;
            for (int i = 0; i < tabButtons.Length; i++)
            {
                bool selected = i == (int)category;
                tabBackgrounds[i].color = selected ? TabSelected : TabNormal;
                tabLabels[i].color = selected ? new Color(0.16f, 0.12f, 0.05f) : Color.white;
            }
            Refresh();
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }

        void Refresh()
        {
            var catalog = run.Cosmetics.Catalog;
            int coins = SaveSystem.Data.coins;
            coinsText.text = coins.ToString();
            bool bundles = _category == ShopCategory.Bundles;

            int shown;
            if (bundles)
            {
                foreach (var card in cards) card.Hide();
                var list = catalog.bundles;
                for (int i = 0; i < bundleCards.Length; i++)
                {
                    if (list == null || i >= list.Length) { bundleCards[i].Hide(); continue; }
                    var b = list[i];
                    bool owned = Inventory.IsOwned(b.id);
                    bundleCards[i].Bind(b, Inventory.BundleAdsWatched(b.id), owned, owned && Inventory.IsEquipped(ToItem(b)));
                }
                shown = list == null ? 0 : Mathf.Min(list.Length, bundleCards.Length);
                SetContentHeight(Mathf.CeilToInt(shown / 3f) * bundleRowHeight);

                var hero = catalog.Character(SaveSystem.Data.equippedCharacter);
                previewIcon.sprite = hero.portrait;
                previewName.text = hero.displayName;
                return;
            }

            foreach (var card in bundleCards) card.Hide();
            var items = catalog.Items(_category);
            for (int i = 0; i < cards.Length; i++)
            {
                if (i >= items.Count)
                {
                    cards[i].Hide();
                    continue;
                }
                var item = items[i];
                bool owned = Inventory.IsOwned(item.Id);
                cards[i].Bind(item, owned, Inventory.IsEquipped(item), coins >= item.Price);
                if (Inventory.IsEquipped(item))
                {
                    previewIcon.sprite = item.Icon;
                    previewName.text = item.Name;
                }
            }
            shown = Mathf.Min(items.Count, cards.Length);
            SetContentHeight(Mathf.CeilToInt(shown / (float)columns) * cardRowHeight);
        }

        void SetContentHeight(float height)
        {
            var content = scroll.content;
            content.sizeDelta = new Vector2(content.sizeDelta.x, height + 20f);
        }

        static ShopItem ToItem(Bundle b) => new ShopItem(b.id, b.displayName, 0, b.thumbnail, ShopCategory.Bundles, b);

        void OnCardClicked(ShopCard card)
        {
            var item = card.Item;
            if (!Inventory.IsOwned(item.Id))
            {
                switch (Inventory.Buy(item))
                {
                    case PurchaseResult.NotEnoughCoins:
                        card.Shake();
                        Audio.AudioManager.Sfx(Audio.SfxId.Denied);
                        router.Toast($"You need {item.Price - SaveSystem.Data.coins} more coins");
                        return;
                    case PurchaseResult.BundleOnly:
                        Audio.AudioManager.Sfx(Audio.SfxId.UiBack);
                        var bundle = run.Cosmetics.Catalog.BundleById(item.BundleId);
                        router.Toast(bundle != null ? $"Part of the {bundle.displayName} bundle - watch ads to unlock" : "Unlocked by an ad bundle");
                        SelectTab(ShopCategory.Bundles);
                        return;
                    case PurchaseResult.Purchased:
                        Audio.AudioManager.Sfx(Audio.SfxId.Purchase);
                        router.Toast($"Unlocked {item.Name}!");
                        break;
                }
            }
            if (!Inventory.IsEquipped(item))
            {
                Inventory.Equip(item);
                card.Celebrate();
                Audio.AudioManager.Sfx(Audio.SfxId.Equip);
            }
            Refresh();
        }

        void OnBundleClicked(BundleCard card)
        {
            var bundle = card.Bundle;
            if (bundle == null || _adInProgress) return;

            if (Inventory.IsOwned(bundle.id))
            {
                if (!Inventory.IsEquipped(ToItem(bundle)))
                {
                    Inventory.EquipBundle(bundle);
                    card.Celebrate();
                    Audio.AudioManager.Sfx(Audio.SfxId.Equip);
                    router.Toast($"{bundle.displayName} set equipped!");
                }
                Refresh();
                return;
            }

            _adInProgress = true;
            StartCoroutine(AdFlow.ShowRewarded(AdPlacements.Bundle, outcome =>
            {
                _adInProgress = false;
                if (outcome == AdOutcome.Unavailable)
                {
                    // Bundle progress stays ad-only, so it can't be farmed with no network
                    router.Toast("No ad available right now - try again soon");
                    return;
                }
                bool rewarded = outcome == AdOutcome.Earned;
                if (!rewarded)
                {
                    router.Toast("Watch the whole ad to earn progress");
                    return;
                }
                bool completed = Inventory.AddBundleAd(bundle);
                Audio.AudioManager.Sfx(completed ? Audio.SfxId.Purchase : Audio.SfxId.AdReward);
                if (completed)
                {
                    router.Toast($"{bundle.displayName} unlocked! Tap EQUIP ALL");
                    card.Celebrate();
                }
                else
                {
                    int left = bundle.adsRequired - Inventory.BundleAdsWatched(bundle.id);
                    router.Toast(left == 1 ? "1 more ad to go!" : $"{left} more ads to go!");
                }
                if (IsVisible) Refresh();
            }, () => router.Toast("Loading ad...")));
        }
    }
}
