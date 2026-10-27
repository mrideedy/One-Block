using NUnit.Framework;
using OneBlock.Core;
using OneBlock.Save;
using UnityEngine;

namespace OneBlock.Tests
{
    /// <summary>Levels, XP and missions. In-memory save data only.</summary>
    public class ProgressionTests
    {
        GameConfig _cfg;

        [SetUp]
        public void SetUp() => _cfg = ScriptableObject.CreateInstance<GameConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_cfg);

        [Test]
        public void XpRollsOverMultipleLevelsAndPaysCoins()
        {
            var d = new SaveData();
            int needed = Progression.XpToNext(_cfg, 1) + Progression.XpToNext(_cfg, 2) + 10;
            var r = Progression.AddXp(d, _cfg, needed);

            Assert.AreEqual(1, r.LevelBefore);
            Assert.AreEqual(3, r.LevelAfter);
            Assert.AreEqual(10, d.playerXp);
            int expectedCoins = Progression.LevelUpCoins(_cfg, 2) + Progression.LevelUpCoins(_cfg, 3);
            Assert.AreEqual(expectedCoins, r.Coins);
            Assert.AreEqual(expectedCoins, d.coins);
        }

        [Test]
        public void EnsureMissionsFillsSlotsWithDistinctTypes()
        {
            var d = new SaveData { playerLevel = 5 };
            Missions.EnsureMissions(d);
            Assert.AreEqual(Missions.Slots, d.missions.Count);
            for (int i = 0; i < d.missions.Count; i++)
            for (int j = i + 1; j < d.missions.Count; j++)
                Assert.AreNotEqual(d.missions[i].type, d.missions[j].type);
        }

        [Test]
        public void LevelGatedMissionsDontAppearEarly()
        {
            for (int n = 0; n < 200; n++)
            {
                var d = new SaveData { playerLevel = 1, missionsGenerated = n };
                var m = Missions.Generate(d);
                Assert.AreNotEqual(MissionType.ChallengeDistance, m.type);
                Assert.AreNotEqual(MissionType.Squash, m.type);
                Assert.AreNotEqual(MissionType.PlayDaily, m.type);
            }
        }

        [Test]
        public void CumulativeAndSingleRunProgress()
        {
            var d = new SaveData();
            d.missions.Add(new MissionState { type = MissionType.Squash, target = 3 });
            d.missions.Add(new MissionState { type = MissionType.RunDistance, target = 500 });

            Missions.Report(d, MissionType.Squash, 1);
            Missions.Report(d, MissionType.Squash, 1);
            Assert.AreEqual(2, d.missions[0].progress);

            Missions.Report(d, MissionType.RunDistance, 320);   // run 1
            Missions.Report(d, MissionType.RunDistance, 150);   // run 2 is worse: best is kept, not summed
            Assert.AreEqual(320, d.missions[1].progress);

            var done = Missions.Report(d, MissionType.Squash, 5);
            Assert.AreEqual(1, done.Count);
            Assert.AreEqual(3, d.missions[0].progress, "progress clamps at the target");
            Assert.AreEqual(1, Missions.ClaimableCount(d));
        }

        [Test]
        public void ClaimGrantsRewardsAndReplacesMission()
        {
            var d = new SaveData();
            d.missions.Add(new MissionState { type = MissionType.Runs, target = 1, progress = 1, rewardCoins = 70, rewardXp = 30 });
            d.missions.Add(new MissionState { type = MissionType.Bridge, target = 5 });
            d.missions.Add(new MissionState { type = MissionType.Cover, target = 5 });

            Assert.IsTrue(Missions.Claim(d, _cfg, 0, out var level));
            Assert.AreEqual(70, d.coins);
            Assert.AreEqual(30, d.playerXp);
            Assert.IsFalse(level.Leveled);
            Assert.AreEqual(1, d.missionsCompleted);
            Assert.AreEqual(Missions.Slots, d.missions.Count);
            Assert.AreNotEqual(MissionType.Runs, d.missions[0].type);
            Assert.IsFalse(d.missions[0].IsComplete);

            Assert.IsFalse(Missions.Claim(d, _cfg, 1, out _), "incomplete missions can't be claimed");
        }

        [Test]
        public void RerollSwapsTypeAndOnlyForIncomplete()
        {
            var d = new SaveData { playerLevel = 4 };
            Missions.EnsureMissions(d);
            var before = d.missions[1].type;
            Assert.IsTrue(Missions.Reroll(d, 1));
            Assert.AreNotEqual(before, d.missions[1].type);

            d.missions[2].progress = d.missions[2].target;
            Assert.IsFalse(Missions.Reroll(d, 2));
        }
    }
}
