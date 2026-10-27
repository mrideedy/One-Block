using OneBlock.Core;
using OneBlock.Gameplay;
using OneBlock.Save;
using OneBlock.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneBlock.UI
{
    /// <summary>End-of-run summary with retry, home and the optional "double coins" rewarded ad.</summary>
    public class ResultsScreen : UIScreen
    {
        [SerializeField] RunController run;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text modeText;
        [SerializeField] TMP_Text distanceText;
        [SerializeField] TMP_Text bestText;
        [SerializeField] TMP_Text dailyText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] RectTransform xpFill;
        [SerializeField] TMP_Text xpText;
        [SerializeField] GameObject newBestBadge;
        [SerializeField] TMP_Text coinsText;
        [SerializeField] Button doubleCoinsButton;
        [SerializeField] Button retryButton;
        [SerializeField] Button homeButton;

        RunResult _result;
        int _shownDistance;

        void Start()
        {
            retryButton.onClick.AddListener(() => BetweenRuns(run.Restart));
            homeButton.onClick.AddListener(() => BetweenRuns(run.GoHome));
            doubleCoinsButton.onClick.AddListener(OnDoubleCoins);
        }

        void OnEnable()
        {
            GameEvents.RunEnded += OnRunEnded;
            GameEvents.CoinsDoubled += OnCoinsDoubled;
        }

        void OnDisable()
        {
            GameEvents.RunEnded -= OnRunEnded;
            GameEvents.CoinsDoubled -= OnCoinsDoubled;
        }

        // Results are raised right after the state change, so the screen may already be showing
        void OnRunEnded(RunResult result)
        {
            _result = result;
            if (IsVisible) Populate();
        }

        protected override void OnShow() => Populate();

        void Populate()
        {
            bool daily = _result.Mode == GameMode.DailyChallenge;
            // A day's first attempt is always "today's best", so only celebrate beating an earlier attempt
            bool celebrate = _result.IsNewBest && (!daily || _result.PreviousBest > 0f);
            title.text = celebrate ? (daily ? "TODAY'S BEST!" : "NEW BEST!") : UIFormat.DeathTitle(_result.Cause);
            title.color = celebrate ? new Color(1f, 0.78f, 0.24f) : new Color(1f, 0.42f, 0.36f);
            modeText.text = UIFormat.ModeName(_result.Mode);
            bestText.text = $"{(daily ? "TODAY'S BEST" : "BEST")}  {UIFormat.Metres(GameManager.BestFor(_result.Mode))}";
            newBestBadge.SetActive(celebrate);

            dailyText.gameObject.SetActive(daily);
            if (daily)
            {
                dailyText.text = _result.DailyFirstToday
                    ? $"STREAK DAY {_result.DailyStreak}  ·  +{_result.DailyReward} BONUS COINS"
                    : $"STREAK: {_result.DailyStreak} {(_result.DailyStreak == 1 ? "DAY" : "DAYS")}";
                if (_result.DailyFirstToday)
                    UITween.Run(dailyText.transform, 1, 0.5f, Ease.Linear, t => dailyText.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.2f), delay: 0.6f);
            }
            coinsText.text = $"+{_result.CoinsCollected}";
            doubleCoinsButton.gameObject.SetActive(run.CanDoubleCoins);
            doubleCoinsButton.interactable = true;

            AnimateXp();

            // Count the distance up for a bit of drama
            int target = Mathf.FloorToInt(_result.Distance);
            UITween.Run(distanceText, 0, 0.7f, Ease.OutCubic, t =>
            {
                _shownDistance = Mathf.RoundToInt(target * t);
                distanceText.text = UIFormat.Metres(_shownDistance);
            }, delay: 0.1f);
        }

        /// <summary>Fills the XP bar from where it was, wrapping through any level-ups.</summary>
        void AnimateXp()
        {
            var cfg = GameManager.Instance.Config;
            var d = SaveSystem.Data;
            int levelsGained = Mathf.Max(0, _result.LevelAfter - _result.LevelBefore);
            float from = _result.XpProgressBefore;
            float to = levelsGained + Progression.LevelProgress(cfg, d);   // in "levels" units from LevelBefore
            float width = ((RectTransform)xpFill.parent).rect.width;
            int shownLevel = _result.LevelBefore;
            levelText.text = $"LV {shownLevel}";
            xpText.text = $"+{_result.XpGained} XP";
            xpText.color = Color.white;

            UITween.Run(xpFill, 0, 0.9f + 0.4f * levelsGained, Ease.OutCubic, t =>
            {
                float v = Mathf.Lerp(from, to, t);
                int lvl = _result.LevelBefore + Mathf.FloorToInt(v);
                float frac = v - Mathf.Floor(v);
                if (lvl > _result.LevelAfter) { lvl = _result.LevelAfter; frac = 1f; }
                if (lvl != shownLevel)
                {
                    shownLevel = lvl;
                    levelText.text = $"LV {lvl}";
                    UITween.Run(levelText.transform, 1, 0.35f, Ease.Linear, k => levelText.transform.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.4f));
                }
                xpFill.gameObject.SetActive(frac > 0.001f);
                xpFill.sizeDelta = new Vector2(Mathf.Max(xpFill.rect.height, width * frac), xpFill.sizeDelta.y);
            }, () =>
            {
                if (levelsGained <= 0) return;
                xpText.text = $"LEVEL UP!  +{_result.LevelUpCoins}";
                xpText.color = new Color(1f, 0.78f, 0.24f);
            }, delay: 0.5f);
        }

        bool _leaving;

        /// <summary>Leaving the results screen is the one place an interstitial may appear, within the pacing rules.</summary>
        void BetweenRuns(System.Action proceed)
        {
            if (_leaving) return;
            var data = SaveSystem.Data;
            long now = AdPacing.NowUnix();
            bool tutorialJustDone = data.totalRuns <= 1;
            if (!tutorialJustDone && AdPacing.CanShowInterstitial(data, Ads.Config, now, Ads.LastRewardedUnix, run.LastRunSeconds)
                && Ads.Service.IsInterstitialReady(AdPlacements.BetweenRuns))
            {
                _leaving = true;
                AdPacing.MarkInterstitialShown(data, now);
                SaveSystem.Save();
                Ads.Service.ShowInterstitial(AdPlacements.BetweenRuns, () =>
                {
                    _leaving = false;
                    proceed();
                });
                return;
            }
            proceed();
        }

        /// <summary>Android back button: same as the Home button (including interstitial pacing).</summary>
        public void BackPressed() => BetweenRuns(run.GoHome);

        void Update()
        {
            // Always tappable: with no ad available the bonus is given for free instead
            if (IsVisible && doubleCoinsButton.gameObject.activeSelf && !_doubling)
                doubleCoinsButton.interactable = true;
        }

        bool _doubling;

        void OnDoubleCoins()
        {
            doubleCoinsButton.interactable = false;
            _doubling = true;
            StartCoroutine(AdFlow.ShowRewarded(AdPlacements.DoubleCoins, outcome =>
            {
                _doubling = false;
                switch (outcome)
                {
                    case AdOutcome.Earned:
                        run.DoubleCoins();
                        break;
                    case AdOutcome.Unavailable:   // couldn't fetch one in time: give the bonus anyway
                        FindAnyObjectByType<UIRouter>()?.Toast("No ad available - this one's on us!");
                        run.DoubleCoins();
                        break;
                    default:
                        doubleCoinsButton.interactable = true;
                        break;
                }
            }, () => FindAnyObjectByType<UIRouter>()?.Toast("Loading ad...")));
        }

        void OnCoinsDoubled(int bonus)
        {
            coinsText.text = $"+{_result.CoinsCollected + bonus}";
            doubleCoinsButton.gameObject.SetActive(false);
            UITween.Run(coinsText.transform, 0, 0.4f, Ease.Linear, t => coinsText.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.35f));
        }
    }
}
