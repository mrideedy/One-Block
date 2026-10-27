using OneBlock.Core;
using OneBlock.Gameplay;
using OneBlock.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneBlock.UI
{
    /// <summary>Title, mode selection, play, and entry points to settings/stats/shop.</summary>
    public class MainMenuScreen : UIScreen
    {
        [SerializeField] RunController run;
        [SerializeField] UIRouter router;

        [SerializeField] RectTransform logo;
        [SerializeField] RectTransform logoBlock;
        [SerializeField] Button playButton;
        [SerializeField] Button[] modeButtons = new Button[3];
        [SerializeField] Image[] modeBackgrounds = new Image[3];
        [SerializeField] TMP_Text[] modeLabels = new TMP_Text[3];
        [SerializeField] TMP_Text modeDescription;
        [SerializeField] TMP_Text coinsText;
        [SerializeField] TMP_Text bestText;
        [SerializeField] Button settingsButton;
        [SerializeField] Button statsButton;
        [SerializeField] Button shopButton;
        [SerializeField] Button worldButton;
        [SerializeField] TMP_Text worldText;

        [Header("Progression")]
        [SerializeField] Button missionsButton;
        [SerializeField] GameObject missionsBadge;
        [SerializeField] TMP_Text missionsBadgeText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] RectTransform levelFill;
        [SerializeField] Button levelButton;

        [Header("Daily badge")]
        [SerializeField] GameObject dailyBadge;
        [SerializeField] Image dailyBadgeBackground;
        [SerializeField] Image dailyBadgeIcon;
        [SerializeField] TMP_Text dailyBadgeText;
        [SerializeField] Sprite coinSprite;
        [SerializeField] Sprite flameSprite;

        static readonly Color SelectedBg = new Color(1f, 0.76f, 0.23f, 1f);
        static readonly Color NormalBg = new Color(0.11f, 0.15f, 0.2f, 0.92f);
        static readonly Color SelectedText = new Color(0.16f, 0.12f, 0.05f);
        static readonly Color BadgeUnclaimed = new Color(0.9f, 0.28f, 0.3f, 1f);
        static readonly Color BadgeClaimed = new Color(0.11f, 0.15f, 0.2f, 0.95f);

        float _time;
        float _nextDailyRefresh;

        void Start()
        {
            playButton.onClick.AddListener(run.Play);
            for (int i = 0; i < modeButtons.Length; i++)
            {
                var mode = (GameMode)i;
                modeButtons[i].onClick.AddListener(() => GameManager.Instance.SelectMode(mode));
            }
            settingsButton.onClick.AddListener(router.ShowSettings);
            statsButton.onClick.AddListener(router.ShowStats);
            shopButton.onClick.AddListener(router.ShowShop);
            missionsButton.onClick.AddListener(router.ShowMissions);
            levelButton.onClick.AddListener(router.ShowMissions);
            worldButton.onClick.AddListener(() => router.ShowShop(Data.ShopCategory.Worlds));
        }

        void OnEnable()
        {
            GameEvents.ModeSelected += OnModeSelected;
            GameEvents.WalletChanged += OnWallet;
            GameEvents.CosmeticsChanged += RefreshWorld;
            GameEvents.MissionsChanged += RefreshProgression;
            GameEvents.WalletChanged += OnWalletProgression;
        }

        void OnDisable()
        {
            GameEvents.ModeSelected -= OnModeSelected;
            GameEvents.WalletChanged -= OnWallet;
            GameEvents.CosmeticsChanged -= RefreshWorld;
            GameEvents.MissionsChanged -= RefreshProgression;
            GameEvents.WalletChanged -= OnWalletProgression;
        }

        void OnWalletProgression(int _) => RefreshProgression();

        void RefreshProgression()
        {
            var d = SaveSystem.Data;
            var cfg = GameManager.Instance.Config;
            int claimable = Missions.ClaimableCount(d);
            missionsBadge.SetActive(claimable > 0);
            missionsBadgeText.text = claimable.ToString();
            levelText.text = $"LV {d.playerLevel}";
            float width = ((RectTransform)levelFill.parent).rect.width;
            float t = Progression.LevelProgress(cfg, d);
            levelFill.gameObject.SetActive(t > 0.001f);
            levelFill.sizeDelta = new Vector2(Mathf.Max(levelFill.rect.height, width * t), levelFill.sizeDelta.y);
        }

        protected override void OnShow()
        {
            var data = SaveSystem.Data;
            OnWallet(data.coins);
            OnModeSelected(GameManager.Instance.SelectedMode);
            RefreshWorld();
            RefreshProgression();
        }

        void RefreshWorld()
        {
            var catalog = run.Cosmetics != null ? run.Cosmetics.Catalog : null;
            if (catalog == null || worldText == null) return;
            worldText.text = catalog.World(SaveSystem.Data.selectedWorld).displayName.ToUpperInvariant();
        }

        void OnWallet(int coins) => coinsText.text = UIFormat.Coins(coins);

        void OnModeSelected(GameMode mode)
        {
            for (int i = 0; i < modeButtons.Length; i++)
            {
                bool selected = i == (int)mode;
                modeBackgrounds[i].color = selected ? SelectedBg : NormalBg;
                modeLabels[i].color = selected ? SelectedText : Color.white;
            }
            float best = GameManager.BestFor(mode);
            bestText.text = best > 0 ? $"{UIFormat.BestLabel(mode)}  {UIFormat.Metres(best)}" : $"{UIFormat.BestLabel(mode)}  —";
            RefreshDaily();
        }

        /// <summary>Daily badge (unclaimed bonus or current streak) and the live daily description.</summary>
        void RefreshDaily()
        {
            var data = SaveSystem.Data;
            var now = DailyProgress.Clock();
            var cfg = GameManager.Instance.Config;
            bool claimed = DailyProgress.ClaimedToday(data, now);
            int streak = DailyProgress.CurrentStreak(data, now);
            int nextDay = DailyProgress.NextStreakDay(data, now);
            int reward = DailyProgress.RewardForStreakDay(cfg, nextDay);

            if (dailyBadge != null)
            {
                bool show = !claimed || streak > 0;
                dailyBadge.SetActive(show);
                dailyBadgeBackground.color = claimed ? BadgeClaimed : BadgeUnclaimed;
                dailyBadgeIcon.sprite = claimed ? flameSprite : coinSprite;
                dailyBadgeText.text = claimed ? streak.ToString() : $"+{reward}";
            }

            var mode = GameManager.Instance.SelectedMode;
            if (mode != GameMode.DailyChallenge)
            {
                modeDescription.text = UIFormat.ModeDescription(mode);
                return;
            }
            string reset = UIFormat.Countdown(DailyProgress.TimeUntilReset(now));
            string second = claimed
                ? $"Streak: {streak} {(streak == 1 ? "day" : "days")}  ·  more coins tomorrow"
                : nextDay > 1 ? $"First run today: +{reward} coins (day {nextDay} streak!)" : $"First run today: +{reward} coins";
            modeDescription.text = $"Same level for everyone. New one in {reset}\n{second}";
        }

        void Update()
        {
            if (!IsVisible) return;
            _time += Time.unscaledDeltaTime;
            if (logo != null) logo.anchoredPosition = new Vector2(logo.anchoredPosition.x, 300f + Mathf.Sin(_time * 1.6f) * 8f);
            if (logoBlock != null) logoBlock.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_time * 2.2f) * 6f);
            float pulse = 1f + Mathf.Sin(_time * 3f) * 0.025f;
            playButton.transform.parent.localScale = Vector3.one * pulse;

            if (_time >= _nextDailyRefresh)
            {
                _nextDailyRefresh = _time + 1f;
                RefreshDaily();
            }
            if (missionsBadge.activeSelf)
                missionsBadge.transform.localScale = Vector3.one * (1f + Mathf.Max(0f, Mathf.Sin(_time * 4f + 1.5f)) * 0.12f);
            if (dailyBadge != null && dailyBadge.activeSelf && !DailyProgress.ClaimedToday(SaveSystem.Data, DailyProgress.Clock()))
                dailyBadge.transform.localScale = Vector3.one * (1f + Mathf.Max(0f, Mathf.Sin(_time * 4f)) * 0.08f);
            else if (dailyBadge != null)
                dailyBadge.transform.localScale = Vector3.one;
        }
    }
}
