using System;
using TMPro;
using UnityEngine;

namespace OneBlock.Services
{
    /// <summary>
    /// Stand-in ad network for the editor: shows a full-screen "test ad" for a couple of seconds and
    /// always grants the reward. Steps aside when a real network (AdMob on devices) is active.
    /// </summary>
    public class MockAdService : MonoBehaviour, IAdService
    {
        [SerializeField] GameObject overlay;
        [SerializeField] TMP_Text label;
        [SerializeField] float duration = 1.6f;

        Action _pending;
        float _remaining;
        string _placement;

        void Awake()
        {
            if (overlay != null) overlay.SetActive(false);
            if (Ads.IsNetwork) return;
            Ads.Service = this;
        }

        public bool IsRewardedReady(string placement) => true;
        public bool IsInterstitialReady(string placement) => true;

        public void ShowRewarded(string placement, Action<bool> onComplete) => Show(placement, () =>
        {
            Ads.LastRewardedUnix = AdPacing.NowUnix();
            onComplete?.Invoke(true);
        });

        public void ShowInterstitial(string placement, Action onClosed) => Show(placement, onClosed);

        public void Prefetch(string placement) { }

        void Show(string placement, Action done)
        {
            if (_pending != null) return;
            _placement = placement;
            _pending = done;
            _remaining = duration;
            if (overlay != null) overlay.SetActive(true);
        }

        void Update()
        {
            if (_pending == null) return;
            _remaining -= Time.unscaledDeltaTime;
            if (label != null) label.text = $"TEST AD\n<size=50%>{_placement} • {Mathf.CeilToInt(Mathf.Max(0f, _remaining))}</size>";
            if (_remaining > 0f) return;

            if (overlay != null) overlay.SetActive(false);
            var done = _pending;
            _pending = null;
            done();
        }
    }
}
