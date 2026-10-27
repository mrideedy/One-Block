using System;
using NUnit.Framework;
using OneBlock.Core;
using OneBlock.Save;
using UnityEngine;

namespace OneBlock.Tests
{
    /// <summary>Daily Challenge streak and bonus rules. Uses in-memory save data only (never the real save).</summary>
    public class DailyProgressTests
    {
        GameConfig _cfg;
        static readonly DateTime Day1 = new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp() => _cfg = ScriptableObject.CreateInstance<GameConfig>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_cfg);

        [Test]
        public void FirstRunOfDayGrantsBonusOnce()
        {
            var d = new SaveData();
            var first = DailyProgress.RegisterRun(d, _cfg, 120f, Day1);
            var second = DailyProgress.RegisterRun(d, _cfg, 300f, Day1.AddHours(3));

            Assert.IsTrue(first.FirstToday);
            Assert.AreEqual(_cfg.dailyBaseReward, first.Reward);
            Assert.AreEqual(1, first.StreakDay);
            Assert.IsFalse(second.FirstToday);
            Assert.AreEqual(0, second.Reward);
            Assert.AreEqual(_cfg.dailyBaseReward, d.coins);
            Assert.AreEqual(2, DailyProgress.RecordFor(d, DailyProgress.Key(Day1)).attempts);
        }

        [Test]
        public void BestOnlyImprovesWithinTheDay()
        {
            var d = new SaveData();
            DailyProgress.RegisterRun(d, _cfg, 300f, Day1);
            var worse = DailyProgress.RegisterRun(d, _cfg, 100f, Day1);
            var better = DailyProgress.RegisterRun(d, _cfg, 450f, Day1);

            Assert.IsFalse(worse.IsNewBest);
            Assert.AreEqual(300f, worse.PreviousBest);
            Assert.IsTrue(better.IsNewBest);
            Assert.AreEqual(450f, DailyProgress.RecordFor(d, DailyProgress.Key(Day1)).bestDistance);
        }

        [Test]
        public void ConsecutiveDaysGrowStreakAndBonusUpToCap()
        {
            var d = new SaveData();
            int expectedCoins = 0;
            for (int day = 0; day < 10; day++)
            {
                var o = DailyProgress.RegisterRun(d, _cfg, 50f, Day1.AddDays(day));
                int capped = Mathf.Min(day + 1, _cfg.dailyMaxStreakBonusDays);
                Assert.AreEqual(day + 1, o.StreakDay);
                Assert.AreEqual(_cfg.dailyBaseReward + _cfg.dailyRewardPerStreakDay * (capped - 1), o.Reward);
                expectedCoins += o.Reward;
            }
            Assert.AreEqual(expectedCoins, d.coins);
            Assert.AreEqual(10, d.bestDailyStreak);
        }

        [Test]
        public void MissingADayResetsStreakButKeepsBest()
        {
            var d = new SaveData();
            DailyProgress.RegisterRun(d, _cfg, 50f, Day1);
            DailyProgress.RegisterRun(d, _cfg, 50f, Day1.AddDays(1));
            DailyProgress.RegisterRun(d, _cfg, 50f, Day1.AddDays(2));
            Assert.AreEqual(3, DailyProgress.CurrentStreak(d, Day1.AddDays(3)));   // still alive the next day
            Assert.AreEqual(0, DailyProgress.CurrentStreak(d, Day1.AddDays(4)));   // a full day missed

            var o = DailyProgress.RegisterRun(d, _cfg, 50f, Day1.AddDays(4));
            Assert.AreEqual(1, o.StreakDay);
            Assert.AreEqual(_cfg.dailyBaseReward, o.Reward);
            Assert.AreEqual(3, d.bestDailyStreak);
        }

        [Test]
        public void DayBoundaryIsUtcMidnight()
        {
            var d = new SaveData();
            var lateNight = new DateTime(2026, 9, 14, 23, 59, 0, DateTimeKind.Utc);
            DailyProgress.RegisterRun(d, _cfg, 50f, lateNight);
            var nextDay = DailyProgress.RegisterRun(d, _cfg, 50f, lateNight.AddMinutes(2));
            Assert.IsTrue(nextDay.FirstToday);
            Assert.AreEqual(2, nextDay.StreakDay);
            Assert.AreEqual(TimeSpan.FromMinutes(1), DailyProgress.TimeUntilReset(lateNight));
        }

        [Test]
        public void OldRecordsArePruned()
        {
            var d = new SaveData();
            for (int day = 0; day < 45; day++) DailyProgress.RegisterRun(d, _cfg, 10f, Day1.AddDays(day));
            Assert.LessOrEqual(d.dailyRecords.Count, DailyProgress.KeepDays + 1);
        }
    }
}
