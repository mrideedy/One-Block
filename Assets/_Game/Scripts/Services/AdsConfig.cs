using UnityEngine;

namespace OneBlock.Services
{
    /// <summary>
    /// Ad network IDs and pacing rules. Ships with Google's official TEST ids: replace them with your own
    /// AdMob app and ad unit ids (and untick Use Test Ads) before releasing.
    /// </summary>
    [CreateAssetMenu(fileName = "AdsConfig", menuName = "One Block/Ads Config")]
    public class AdsConfig : ScriptableObject
    {
        public const string TestAppId = "ca-app-pub-3940256099942544~3347511713";
        public const string TestRewarded = "ca-app-pub-3940256099942544/5224354917";
        public const string TestInterstitial = "ca-app-pub-3940256099942544/1033173712";

        [Header("AdMob (Android)")]
        [Tooltip("Your AdMob app id, ca-app-pub-XXXX~YYYY. Also written into the Google Mobile Ads settings.")]
        public string androidAppId = TestAppId;
        public string rewardedUnitId = TestRewarded;
        public string interstitialUnitId = TestInterstitial;
        [Tooltip("Forces Google's test ad units. Keep on until your AdMob app is approved; never click your own live ads.")]
        public bool useTestAds = true;
        [Tooltip("Hashed device ids that always receive test ads (see logcat for your device's id).")]
        public string[] testDeviceIds = new string[0];

        [Header("Content")]
        [Tooltip("Audience is 13+, so ads are capped at this rating rather than child-directed.")]
        public MaxRating maxAdContentRating = MaxRating.PG;

        [Header("Interstitial pacing (between runs only)")]
        [Tooltip("No interstitials in a player's first sessions.")]
        public int minSessionsBeforeInterstitials = 2;
        [Tooltip("No interstitials until this many runs have been played in total.")]
        public int minTotalRuns = 6;
        [Tooltip("Runs between interstitials.")]
        public int runsBetweenInterstitials = 3;
        [Tooltip("Real seconds between interstitials.")]
        public float secondsBetweenInterstitials = 150f;
        [Tooltip("Skip an interstitial if the player watched a rewarded ad this recently (seconds).")]
        public float rewardedGrace = 90f;
        [Tooltip("Skip interstitials for runs shorter than this (seconds) so quick retries aren't punished.")]
        public float minRunSeconds = 20f;

        [Header("Loading")]
        public float retryBaseSeconds = 4f;
        public float retryMaxSeconds = 60f;

        public enum MaxRating { G, PG, T }
    }
}
