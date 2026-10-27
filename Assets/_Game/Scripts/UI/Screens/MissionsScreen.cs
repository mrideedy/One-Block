using OneBlock.Core;
using OneBlock.Gameplay;
using OneBlock.Save;
using OneBlock.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneBlock.UI
{
    /// <summary>Player level with XP bar and the three active missions: claim rewards or swap one via a rewarded ad.</summary>
    public class MissionsScreen : UIScreen
    {
        public const string RerollPlacement = "mission_reroll";

        [SerializeField] RunController run;
        [SerializeField] UIRouter router;
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text levelText;
        [SerializeField] RectTransform xpFill;
        [SerializeField] TMP_Text xpText;

        [Header("Rows (one per mission slot)")]
        [SerializeField] Image[] rowBackgrounds;
        [SerializeField] Image[] rowIcons;
        [SerializeField] TMP_Text[] rowTexts;
        [SerializeField] RectTransform[] rowFills;
        [SerializeField] TMP_Text[] rowProgress;
        [SerializeField] TMP_Text[] rowCoins;
        [SerializeField] TMP_Text[] rowXp;
        [SerializeField] Button[] claimButtons;
        [SerializeField] Button[] rerollButtons;

        [Tooltip("Icon per MissionType, in enum order.")]
        [SerializeField] Sprite[] typeIcons;

        static readonly Color RowNormal = new Color(1f, 1f, 1f, 0.07f);
        static readonly Color RowDone = new Color(0.35f, 0.85f, 0.4f, 0.22f);

        bool _adBusy;

        void Start()
        {
            closeButton.onClick.AddListener(Hide);
            for (int i = 0; i < claimButtons.Length; i++)
            {
                int slot = i;
                claimButtons[i].onClick.AddListener(() => Claim(slot));
                rerollButtons[i].onClick.AddListener(() => Reroll(slot));
            }
        }

        void OnEnable() => GameEvents.MissionsChanged += Refresh;
        void OnDisable() => GameEvents.MissionsChanged -= Refresh;

        protected override void OnShow() => Refresh();

        string HazardName
        {
            get
            {
                var w = run != null && run.Cosmetics != null ? run.Cosmetics.Catalog.World(SaveSystem.Data.selectedWorld) : null;
                return w != null && !string.IsNullOrEmpty(w.hazardName) ? w.hazardName : "traps";
            }
        }

        void Refresh()
        {
            if (!IsVisible) return;
            var d = SaveSystem.Data;
            var cfg = GameManager.Instance.Config;
            Missions.EnsureMissions(d);

            levelText.text = $"LEVEL {d.playerLevel}";
            SetFill(xpFill, Progression.LevelProgress(cfg, d));
            xpText.text = $"{d.playerXp} / {Progression.XpToNext(cfg, d.playerLevel)} XP";

            string hazard = HazardName;
            for (int i = 0; i < rowTexts.Length; i++)
            {
                bool has = i < d.missions.Count;
                rowBackgrounds[i].gameObject.SetActive(has);
                if (!has) continue;
                var m = d.missions[i];
                rowBackgrounds[i].color = m.IsComplete ? RowDone : RowNormal;
                int iconIndex = (int)m.type;
                rowIcons[i].sprite = typeIcons != null && iconIndex < typeIcons.Length ? typeIcons[iconIndex] : null;
                rowTexts[i].text = Missions.Describe(m, hazard);
                SetFill(rowFills[i], m.Progress01);
                rowProgress[i].text = m.IsComplete ? "DONE!" : $"{m.progress} / {m.target}";
                rowCoins[i].text = $"+{m.rewardCoins}";
                rowXp[i].text = $"+{m.rewardXp} XP";
                claimButtons[i].gameObject.SetActive(m.IsComplete);
                rerollButtons[i].gameObject.SetActive(!m.IsComplete);
                rerollButtons[i].interactable = !_adBusy;   // free swap when no ad is available
            }
        }

        static void SetFill(RectTransform fill, float t)
        {
            float width = ((RectTransform)fill.parent).rect.width;
            float h = fill.rect.height;
            fill.gameObject.SetActive(t > 0.001f);
            fill.sizeDelta = new Vector2(Mathf.Max(h, width * Mathf.Clamp01(t)), fill.sizeDelta.y);
        }

        void Claim(int slot)
        {
            var d = SaveSystem.Data;
            var cfg = GameManager.Instance.Config;
            if (slot >= d.missions.Count) return;
            int coins = d.missions[slot].rewardCoins;
            if (!Missions.Claim(d, cfg, slot, out var level)) return;
            SaveSystem.Save();
            Audio.AudioManager.Sfx(level.Leveled ? Audio.SfxId.LevelUp : Audio.SfxId.Purchase);

            GameEvents.RaiseWalletChanged(d.coins);
            router.Toast(level.Leveled ? $"LEVEL UP! Level {level.LevelAfter}  +{coins + level.Coins} coins" : $"+{coins} coins!");
            var row = rowBackgrounds[slot].transform;
            UITween.Run(row, 7, 0.4f, Ease.Linear, t => row.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.06f),
                () => row.localScale = Vector3.one);
            GameEvents.RaiseMissionsChanged();
        }

        void Reroll(int slot)
        {
            if (_adBusy) return;
            _adBusy = true;
            StartCoroutine(AdFlow.ShowRewarded(RerollPlacement, outcome =>
            {
                _adBusy = false;
                // No ad in time: swap for free rather than blocking the player
                bool grant = outcome == AdOutcome.Earned || outcome == AdOutcome.Unavailable;
                if (grant && Missions.Reroll(SaveSystem.Data, slot))
                {
                    SaveSystem.Save();
                    Audio.AudioManager.Sfx(Audio.SfxId.AdReward);
                    router.Toast(outcome == AdOutcome.Earned ? "New mission!" : "No ad available - free swap!");
                }
                GameEvents.RaiseMissionsChanged();
                Refresh();
            }, () => router.Toast("Loading ad...")));
        }
    }
}
