using System;
using System.Collections;
using UnityEngine;

namespace OneBlock.Services
{
    public enum AdOutcome
    {
        /// <summary>The ad played and the reward was earned.</summary>
        Earned,
        /// <summary>An ad played but the player closed it early, so no reward.</summary>
        Skipped,
        /// <summary>No ad could be fetched in time; the caller decides what to give instead.</summary>
        Unavailable
    }

    /// <summary>
    /// Rewarded-ad flow used by every "watch an ad" button. If nothing is cached it asks the network for one
    /// and waits briefly (the player sees a "Loading ad..." hint) instead of giving up straight away.
    /// </summary>
    public static class AdFlow
    {
        public const float WaitSeconds = 8f;

        public static IEnumerator ShowRewarded(string placement, Action<AdOutcome> onDone, Action onWaiting = null)
        {
            if (!Ads.Service.IsRewardedReady(placement))
            {
                onWaiting?.Invoke();
                Ads.Service.Prefetch(placement);
                float until = Time.unscaledTime + WaitSeconds;
                while (Time.unscaledTime < until && !Ads.Service.IsRewardedReady(placement))
                    yield return null;
            }

            if (!Ads.Service.IsRewardedReady(placement))
            {
                onDone?.Invoke(AdOutcome.Unavailable);
                yield break;
            }

            bool finished = false;
            bool earned = false;
            Ads.Service.ShowRewarded(placement, ok => { earned = ok; finished = true; });
            while (!finished) yield return null;
            onDone?.Invoke(earned ? AdOutcome.Earned : AdOutcome.Skipped);
        }
    }
}
