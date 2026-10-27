using System;
using UnityEngine;

namespace OneBlock.Services
{
    public static class AdPlacements
    {
        public const string Revive = "revive";
        public const string ExtraBlocks = "extra_blocks";
        public const string DoubleCoins = "double_coins";
        public const string Bundle = "bundle";
        public const string BetweenRuns = "between_runs";
    }

    /// <summary>Ad provider abstraction: AdMob on devices, a mock overlay in the editor.</summary>
    public interface IAdService
    {
        bool IsRewardedReady(string placement);
        /// <summary>Shows a rewarded ad; the callback receives true only if the reward was earned.</summary>
        void ShowRewarded(string placement, Action<bool> onComplete);
        bool IsInterstitialReady(string placement);
        void ShowInterstitial(string placement, Action onClosed);
        /// <summary>Asks for a fresh ad right now (used when the player taps and nothing is cached yet).</summary>
        void Prefetch(string placement);
    }

    public static class Ads
    {
        static IAdService _service;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _service = null;
            IsNetwork = false;
            LastRewardedUnix = 0;
            PrivacyOptionsRequired = () => false;
            ShowPrivacyOptions = done => done?.Invoke();
        }

        /// <summary>The active ad service. Defaults to one that grants rewards instantly (no network).</summary>
        public static IAdService Service
        {
            get => _service ??= new InstantAdService();
            set => _service = value;
        }

        /// <summary>Pacing and ids, set by <see cref="AdsBootstrap"/>.</summary>
        public static AdsConfig Config { get; set; }

        /// <summary>Set by a network integration (AdMob) so the bootstrap can create it on devices.</summary>
        public static Func<AdsConfig, IAdService> NetworkFactory;

        /// <summary>True when a real ad network is serving ads (the editor mock then steps aside).</summary>
        public static bool IsNetwork { get; set; }

        /// <summary>Unix seconds of the last earned rewarded ad (interstitials give it some room).</summary>
        public static long LastRewardedUnix;

        /// <summary>Whether the consent SDK requires a "Privacy options" entry point (GDPR regions).</summary>
        public static Func<bool> PrivacyOptionsRequired = () => false;

        /// <summary>Re-opens the consent form so the player can change their choices.</summary>
        public static Action<Action> ShowPrivacyOptions = done => done?.Invoke();

        sealed class InstantAdService : IAdService
        {
            public bool IsRewardedReady(string placement) => true;
            public void ShowRewarded(string placement, Action<bool> onComplete) => onComplete?.Invoke(true);
            public bool IsInterstitialReady(string placement) => false;
            public void ShowInterstitial(string placement, Action onClosed) => onClosed?.Invoke();
            public void Prefetch(string placement) { }
        }
    }
}
