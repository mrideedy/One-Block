using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace OneBlock.Services
{
    /// <summary>
    /// Google AdMob implementation of <see cref="IAdService"/> (rewarded + interstitial), with Google's UMP
    /// consent flow. Only compiled when the Google Mobile Ads package is installed, and only used on devices
    /// (the editor keeps the quick mock overlay).
    /// </summary>
    public sealed class AdMobAdService : MonoBehaviour, IAdService
    {
        AdsConfig _config;
        bool _initialised;
        RewardedAd _rewarded;
        InterstitialAd _interstitial;
        bool _loadingRewarded, _loadingInterstitial;
        float _rewardedRetryAt, _interstitialRetryAt;
        const float ConsentRetrySeconds = 30f;
        float _consentRetryAt;
        int _rewardedFailures, _interstitialFailures;
        bool _showing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            if (Application.isEditor) return;
            Ads.NetworkFactory = config =>
            {
                var go = new GameObject("[AdMob]");
                DontDestroyOnLoad(go);
                var service = go.AddComponent<AdMobAdService>();
                service.Begin(config);
                return service;
            };
        }

        string RewardedId => _config.useTestAds ? AdsConfig.TestRewarded : _config.rewardedUnitId;
        string InterstitialId => _config.useTestAds ? AdsConfig.TestInterstitial : _config.interstitialUnitId;

        void Begin(AdsConfig config)
        {
            _config = config;

            Ads.PrivacyOptionsRequired = () =>
                ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
            Ads.ShowPrivacyOptions = done => ConsentForm.ShowPrivacyOptionsForm(error => OnMain(() =>
            {
                if (error != null) Debug.LogWarning($"[Ads] Privacy options form: {error.Message}");
                if (ConsentInformation.CanRequestAds()) Initialise();
                done?.Invoke();
            }));

            RequestConsent();
        }

        /// <summary>
        /// Asks for consent (when the region needs it) and starts the SDK. A failed network call here used to
        /// leave ads dead for the whole session, so <see cref="Update"/> keeps retrying until the SDK is up.
        /// </summary>
        void RequestConsent()
        {
            _consentRetryAt = Time.unscaledTime + ConsentRetrySeconds;

            // Consent from a previous session lets ads start loading immediately
            if (ConsentInformation.CanRequestAds()) { Initialise(); return; }

            var request = new ConsentRequestParameters { TagForUnderAgeOfConsent = false };
            ConsentInformation.Update(request, updateError => OnMain(() =>
            {
                if (updateError != null) Debug.LogWarning($"[Ads] Consent update: {updateError.Message}");
                if (ConsentInformation.CanRequestAds()) Initialise();
                ConsentForm.LoadAndShowConsentFormIfRequired(formError => OnMain(() =>
                {
                    if (formError != null) Debug.LogWarning($"[Ads] Consent form: {formError.Message}");
                    if (ConsentInformation.CanRequestAds()) Initialise();
                }));
            }));
        }

        void Initialise()
        {
            if (_initialised) return;
            _initialised = true;

            var rating = _config.maxAdContentRating switch
            {
                AdsConfig.MaxRating.G => MaxAdContentRating.G,
                AdsConfig.MaxRating.T => MaxAdContentRating.T,
                _ => MaxAdContentRating.PG
            };
            // 13+ audience: not child-directed, content capped by rating
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                MaxAdContentRating = rating,
                TestDeviceIds = new List<string>(_config.testDeviceIds ?? Array.Empty<string>())
            });
            MobileAds.Initialize(_ => OnMain(() =>
            {
                LoadRewarded();
                LoadInterstitial();
            }));
        }

        // ------------------------------------------------------------------ loading

        void Update()
        {
            // Never give up on starting the SDK: one failed consent call must not kill ads for the session
            if (!_initialised)
            {
                if (Time.unscaledTime >= _consentRetryAt) RequestConsent();
                return;
            }
            if (_showing) return;
            if (_rewarded == null && !_loadingRewarded && Time.unscaledTime >= _rewardedRetryAt) LoadRewarded();
            if (_interstitial == null && !_loadingInterstitial && Time.unscaledTime >= _interstitialRetryAt) LoadInterstitial();
        }

        float Backoff(int failures) => Mathf.Min(_config.retryMaxSeconds, _config.retryBaseSeconds * Mathf.Pow(2f, Mathf.Max(0, failures - 1)));

        void LoadRewarded()
        {
            _loadingRewarded = true;
            RewardedAd.Load(RewardedId, new AdRequest(), (ad, error) => OnMain(() =>
            {
                _loadingRewarded = false;
                if (error != null || ad == null)
                {
                    _rewardedFailures++;
                    _rewardedRetryAt = Time.unscaledTime + Backoff(_rewardedFailures);
                    Debug.LogWarning($"[Ads] Rewarded failed to load: {error?.GetMessage()}");
                    return;
                }
                _rewardedFailures = 0;
                _rewarded = ad;
            }));
        }

        void LoadInterstitial()
        {
            _loadingInterstitial = true;
            InterstitialAd.Load(InterstitialId, new AdRequest(), (ad, error) => OnMain(() =>
            {
                _loadingInterstitial = false;
                if (error != null || ad == null)
                {
                    _interstitialFailures++;
                    _interstitialRetryAt = Time.unscaledTime + Backoff(_interstitialFailures);
                    Debug.LogWarning($"[Ads] Interstitial failed to load: {error?.GetMessage()}");
                    return;
                }
                _interstitialFailures = 0;
                _interstitial = ad;
            }));
        }

        // ------------------------------------------------------------------ IAdService

        public bool IsRewardedReady(string placement) => _rewarded != null && _rewarded.CanShowAd();
        public bool IsInterstitialReady(string placement) => _interstitial != null && _interstitial.CanShowAd();

        /// <summary>The player is waiting: drop any back-off and ask for an ad straight away.</summary>
        public void Prefetch(string placement)
        {
            if (!_initialised)
            {
                _consentRetryAt = 0f;   // Update() will retry the consent + init immediately
                return;
            }
            if (placement == AdPlacements.BetweenRuns)
            {
                _interstitialFailures = 0;
                _interstitialRetryAt = 0f;
                if (_interstitial == null && !_loadingInterstitial) LoadInterstitial();
                return;
            }
            _rewardedFailures = 0;
            _rewardedRetryAt = 0f;
            if (_rewarded == null && !_loadingRewarded) LoadRewarded();
        }

        public void ShowRewarded(string placement, Action<bool> onComplete)
        {
            if (_showing || !IsRewardedReady(placement))
            {
                onComplete?.Invoke(false);
                return;
            }
            var ad = _rewarded;
            _rewarded = null;
            bool earned = false;
            BeginShow();
            ad.OnAdFullScreenContentClosed += () => OnMain(() =>
            {
                EndShow();
                ad.Destroy();
                if (earned) Ads.LastRewardedUnix = AdPacing.NowUnix();
                onComplete?.Invoke(earned);
            });
            ad.OnAdFullScreenContentFailed += error => OnMain(() =>
            {
                Debug.LogWarning($"[Ads] Rewarded failed to show: {error?.GetMessage()}");
                EndShow();
                ad.Destroy();
                onComplete?.Invoke(false);
            });
            ad.Show(_ => earned = true);
        }

        public void ShowInterstitial(string placement, Action onClosed)
        {
            if (_showing || !IsInterstitialReady(placement))
            {
                onClosed?.Invoke();
                return;
            }
            var ad = _interstitial;
            _interstitial = null;
            BeginShow();
            ad.OnAdFullScreenContentClosed += () => OnMain(() =>
            {
                EndShow();
                ad.Destroy();
                onClosed?.Invoke();
            });
            ad.OnAdFullScreenContentFailed += error => OnMain(() =>
            {
                Debug.LogWarning($"[Ads] Interstitial failed to show: {error?.GetMessage()}");
                EndShow();
                ad.Destroy();
                onClosed?.Invoke();
            });
            ad.Show();
        }

        /// <summary>SDK callbacks can arrive on a background thread; game code must run on Unity's main thread.</summary>
        static void OnMain(Action action) => MobileAdsEventExecutor.ExecuteInUpdate(action);

        // Game audio pauses while an ad plays
        void BeginShow()
        {
            _showing = true;
            AudioListener.pause = true;
        }

        void EndShow()
        {
            _showing = false;
            AudioListener.pause = false;
        }
    }
}
