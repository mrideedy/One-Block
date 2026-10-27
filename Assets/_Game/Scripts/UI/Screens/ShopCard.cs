using System;
using OneBlock.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneBlock.UI
{
    /// <summary>One item tile in the shop grid: icon, name and buy / equip / equipped state.</summary>
    public class ShopCard : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameText;
        [SerializeField] GameObject priceRow;
        [SerializeField] TMP_Text priceText;
        [SerializeField] TMP_Text stateText;
        [SerializeField] GameObject lockBadge;
        [SerializeField] GameObject adTag;

        static readonly Color Normal = new Color(0.19f, 0.24f, 0.32f, 1f);
        static readonly Color Equipped = new Color(0.22f, 0.5f, 0.3f, 1f);
        static readonly Color CanAfford = Color.white;
        static readonly Color TooExpensive = new Color(1f, 0.45f, 0.4f);

        public ShopItem Item { get; private set; }
        public event Action<ShopCard> Clicked;

        void Awake() => button.onClick.AddListener(() => Clicked?.Invoke(this));

        public void Bind(ShopItem item, bool owned, bool equipped, bool affordable)
        {
            Item = item;
            gameObject.SetActive(true);
            icon.sprite = item.Icon;
            icon.color = owned ? Color.white : new Color(1f, 1f, 1f, 0.85f);
            nameText.text = item.Name;
            background.color = equipped ? Equipped : Normal;

            bool adOnly = !owned && item.IsBundleExclusive;
            priceRow.SetActive(!owned && !adOnly);
            adTag.SetActive(adOnly);
            priceText.text = item.Price.ToString();
            priceText.color = affordable ? CanAfford : TooExpensive;
            lockBadge.SetActive(!owned);

            stateText.gameObject.SetActive(owned);
            stateText.text = equipped ? "EQUIPPED" : "EQUIP";
            stateText.color = equipped ? new Color(0.75f, 1f, 0.7f) : Color.white;
        }

        public void Hide() => gameObject.SetActive(false);

        public void Shake()
        {
            var rt = (RectTransform)transform;
            Vector2 basePos = rt.anchoredPosition;
            UITween.Run(this, 1, 0.35f, Ease.Linear, t => rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(t * 50f) * 12f * (1f - t), 0f),
                () => rt.anchoredPosition = basePos);
        }

        public void Celebrate()
        {
            UITween.Run(this, 2, 0.45f, Ease.Linear, t => transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.15f),
                () => transform.localScale = Vector3.one);
        }
    }
}
