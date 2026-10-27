using NUnit.Framework;
using OneBlock.Save;
using OneBlock.Services;
using UnityEngine;

namespace OneBlock.Tests
{
    public class AdPacingTests
    {
        AdsConfig _cfg;
        const long Now = 1_800_000_000;

        [SetUp]
        public void SetUp() => _cfg = ScriptableObject.CreateInstance<AdsConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_cfg);

        SaveData Veteran() => new SaveData { sessionCount = 5, totalRuns = 40, runsSinceInterstitial = 5, lastInterstitialUnixSeconds = Now - 1000 };

        [Test]
        public void VeteranBetweenRunsIsAllowed() =>
            Assert.IsTrue(AdPacing.CanShowInterstitial(Veteran(), _cfg, Now, 0, 60f));

        [Test]
        public void NewPlayersNeverSeeInterstitials()
        {
            var d = Veteran();
            d.sessionCount = 1;
            Assert.IsFalse(AdPacing.CanShowInterstitial(d, _cfg, Now, 0, 60f));
            d = Veteran();
            d.totalRuns = 3;
            Assert.IsFalse(AdPacing.CanShowInterstitial(d, _cfg, Now, 0, 60f));
        }

        [Test]
        public void RespectsRunAndTimeGaps()
        {
            var d = Veteran();
            d.runsSinceInterstitial = 1;
            Assert.IsFalse(AdPacing.CanShowInterstitial(d, _cfg, Now, 0, 60f));
            d = Veteran();
            d.lastInterstitialUnixSeconds = Now - 30;
            Assert.IsFalse(AdPacing.CanShowInterstitial(d, _cfg, Now, 0, 60f));
        }

        [Test]
        public void RewardedAdsAndQuickRunsBuyGrace()
        {
            Assert.IsFalse(AdPacing.CanShowInterstitial(Veteran(), _cfg, Now, Now - 20, 60f), "just watched a rewarded ad");
            Assert.IsFalse(AdPacing.CanShowInterstitial(Veteran(), _cfg, Now, 0, 5f), "quick retry");
        }

        [Test]
        public void ShowingResetsCounters()
        {
            var d = Veteran();
            AdPacing.MarkInterstitialShown(d, Now);
            Assert.AreEqual(0, d.runsSinceInterstitial);
            Assert.AreEqual(Now, d.lastInterstitialUnixSeconds);
            Assert.IsFalse(AdPacing.CanShowInterstitial(d, _cfg, Now + 10, 0, 60f));
        }
    }
}
