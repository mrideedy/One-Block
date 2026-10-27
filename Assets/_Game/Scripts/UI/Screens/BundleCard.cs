using System;
using OneBlock.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneBlock.UI
{
    /// <summary>A large shop card for an ad bundle: preview, contents, ad progress and a watch-ad / equip-all button.</summary>
    public class BundleCard : MonoBehaviour
    {
        [SerializeField] Image thumbnail;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] Image[] contentIcons = new Image[4];
        [SerializeField] RectTransform progressFill;
        [SerializeField] TMP_Text progressText;
        [SerializeField] Button actionButton;
        [SerializeField] Image actionBackground;
        [SerializeField] GameObject actionIcon;
        [SerializeField] TMP_Text actionLabel;
        [SerializeField] Sprite watchSprite;
        [SerializeField] Sprite equipSprite;
        [SerializeField] Sprite equippedSprite;

        public Bundle Bundle { get; private set; }
        public event Action<BundleCard> Clicked;

        float _fillWidth;
        float _labelX;

        void Awake()
        {
            _labelX = actionLabel.rectTransform.anchoredPosition.x;
            actionButton.onClick.AddListener(() => Clicked?.Invoke(this));
            _fillWidth = ((RectTransform)progressFill.parent).rect.width;
        }

        public void Bind(Bundle bundle, int adsWatched, bool owned, bool equipped)
        {
            Bundle = bundle;
            gameObject.SetActive(true);
            thumbnail.sprite = bundle.thumbnail;
            nameText.text = bundle.displayName;
            descriptionText.text = bundle.description;
            contentIcons[0].sprite = bundle.character.portrait;
            contentIcons[1].sprite = bundle.block.sprite;
            contentIcons[2].sprite = bundle.trail.icon;
            contentIcons[3].sprite = bundle.effect.icon;

            int watched = owned ? bundle.adsRequired : Mathf.Min(adsWatched, bundle.adsRequired);
            float t = bundle.adsRequired > 0 ? watched / (float)bundle.adsRequired : 1f;
            if (_fillWidth <= 0) _fillWidth = ((RectTransform)progressFill.parent).rect.width;
            // Keep at least the pill's height so the rounded ends never collapse
            float h = progressFill.rect.height;
            progressFill.gameObject.SetActive(t > 0);
            progressFill.sizeDelta = new Vector2(Mathf.Max(h, _fillWidth * t), progressFill.sizeDelta.y);
            progressText.text = owned ? "UNLOCKED" : $"{watched} / {bundle.adsRequired} ADS";

            if (!owned)
            {
                actionBackground.sprite = watchSprite;
                actionIcon.SetActive(true);
                actionLabel.text = "WATCH AD";
                SetLabelX(_labelX);
            }
            else
            {
                actionBackground.sprite = equipped ? equippedSprite : equipSprite;
                actionIcon.SetActive(false);
                actionLabel.text = equipped ? "EQUIPPED" : "EQUIP ALL";
                SetLabelX(0f);
            }
        }

        void SetLabelX(float x)
        {
            var rt = actionLabel.rectTransform;
            rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
        }

        public void Hide() => gameObject.SetActive(false);

        public void Celebrate()
        {
            UITween.Run(this, 2, 0.5f, Ease.Linear, t => transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.08f),
                () => transform.localScale = Vector3.one);
        }
    }
}
