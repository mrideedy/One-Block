using OneBlock.Core;
using OneBlock.Gameplay;
using OneBlock.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneBlock.UI
{
    /// <summary>
    /// Revive offer shown once per run after dying: watch a rewarded ad to continue, or give up.
    /// Auto-declines when the countdown runs out.
    /// </summary>
    public class ContinueScreen : UIScreen
    {
        const float Countdown = 6f;
        int _lastShown = -1;

        [SerializeField] RunController run;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text distanceText;
        [SerializeField] TMP_Text rewardText;
        [SerializeField] TMP_Text countdownText;
        [SerializeField] Image countdownRing;
        [SerializeField] Button reviveButton;
        [SerializeField] TMP_Text reviveLabel;
        [SerializeField] Button giveUpButton;

        float _remaining;
        bool _waitingForAd;

        void Start()
        {
            reviveButton.onClick.AddListener(OnRevive);
            giveUpButton.onClick.AddListener(OnGiveUp);
        }

        protected override void OnShow()
        {
            bool stuck = run.LastDeathCause == DeathCause.OutOfBlocks;
            title.text = UIFormat.DeathTitle(run.LastDeathCause);
            distanceText.text = UIFormat.Metres(run.Distance);
            rewardText.text = stuck ? "Get +5 blocks and keep going!" : "Continue from where you fell with +3 blocks";
            reviveLabel.text = stuck ? "+5 BLOCKS" : "REVIVE";
            reviveButton.interactable = true;   // when no ad is available the revive is given for free
            _remaining = Countdown;
            _lastShown = -1;
            _waitingForAd = false;
        }

        void OnRevive()
        {
            if (_waitingForAd) return;
            _waitingForAd = true;
            reviveButton.interactable = false;
            giveUpButton.interactable = false;
            string placement = run.LastDeathCause == DeathCause.OutOfBlocks ? AdPlacements.ExtraBlocks : AdPlacements.Revive;
            StartCoroutine(AdFlow.ShowRewarded(placement, outcome =>
            {
                _waitingForAd = false;
                giveUpButton.interactable = true;
                switch (outcome)
                {
                    case AdOutcome.Earned:
                        run.Revive();
                        break;
                    case AdOutcome.Unavailable:   // couldn't fetch one in time: give the revive anyway
                        FindAnyObjectByType<UIRouter>()?.Toast("No ad available - this one's on us!");
                        run.Revive();
                        break;
                    default:
                        run.DeclineRevive();
                        break;
                }
            }, () => FindAnyObjectByType<UIRouter>()?.Toast("Loading ad...")));
        }

        void OnGiveUp()
        {
            if (_waitingForAd) return;
            run.DeclineRevive();
        }

        void Update()
        {
            if (!IsVisible || _waitingForAd) return;
            _remaining -= Time.unscaledDeltaTime;
            int shown = Mathf.CeilToInt(Mathf.Max(0f, _remaining));
            if (shown != _lastShown)
            {
                if (_lastShown >= 0 && shown > 0) Audio.AudioManager.Sfx(Audio.SfxId.Tick, 0.8f);
                _lastShown = shown;
            }
            countdownText.text = shown.ToString();
            if (countdownRing != null) countdownRing.fillAmount = Mathf.Clamp01(_remaining / Countdown);
            if (_remaining <= 0f) OnGiveUp();
        }
    }
}
