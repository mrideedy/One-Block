using System;
using OneBlock.Save;

namespace OneBlock.Services
{
    /// <summary>
    /// Decides when a between-runs interstitial is allowed. Pure logic over the save data so the rules
    /// can be unit-tested: never in early sessions, never too often, never right after a rewarded ad,
    /// and never after a very short run.
    /// </summary>
    public static class AdPacing
    {
        public static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static bool CanShowInterstitial(SaveData d, AdsConfig cfg, long nowUnix, long lastRewardedUnix, float runSeconds)
        {
            if (cfg == null) return false;
            if (d.sessionCount < cfg.minSessionsBeforeInterstitials) return false;
            if (d.totalRuns < cfg.minTotalRuns) return false;
            if (d.runsSinceInterstitial < cfg.runsBetweenInterstitials) return false;
            if (d.lastInterstitialUnixSeconds > 0 && nowUnix - d.lastInterstitialUnixSeconds < cfg.secondsBetweenInterstitials) return false;
            if (lastRewardedUnix > 0 && nowUnix - lastRewardedUnix < cfg.rewardedGrace) return false;
            if (runSeconds < cfg.minRunSeconds) return false;
            return true;
        }

        public static void MarkInterstitialShown(SaveData d, long nowUnix)
        {
            d.runsSinceInterstitial = 0;
            d.lastInterstitialUnixSeconds = nowUnix;
        }
    }
}
