using OneBlock.Core;
using OneBlock.DevTools;
using OneBlock.EditorTools;
using OneBlock.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace OneBlock.Tests
{
    /// <summary>
    /// Guards the level generator's promises using the headless simulator:
    /// perfect play is never killed while holding blocks, and flawless runs always reach 1000 m.
    /// </summary>
    public class LevelFairnessTests
    {
        const int Seeds = 25;
        GameConfig _cfg;

        [OneTimeSetUp]
        public void Setup() => _cfg = ProjectSetup.EnsureConfig();

        [Test]
        public void PerfectPlay_NeverDiesWhileHoldingBlocks()
        {
            for (int seed = 1; seed <= Seeds; seed++)
            {
                var r = LevelSimulator.Run(_cfg, GameMode.Endless, BotPolicy.Profile.Perfect, seed, maxMetres: 1500f);
                if (r.Cause == DeathCause.None) continue;
                Assert.AreEqual(0, r.BlocksLeft,
                    $"Seed {seed}: died of {r.Cause} at {r.Metres:0} m with {r.BlocksLeft} blocks (last pattern '{r.LastPattern}')");
            }
        }

        [Test]
        public void PerfectPlay_Reaches1000Metres()
        {
            for (int seed = 1; seed <= Seeds; seed++)
            {
                var r = LevelSimulator.Run(_cfg, GameMode.Endless, BotPolicy.Profile.Perfect, seed, maxMetres: 1000f);
                Assert.GreaterOrEqual(r.Metres, 999f,
                    $"Seed {seed}: perfect run ended at {r.Metres:0} m ({r.Cause}, last pattern '{r.LastPattern}')");
            }
        }

        [Test]
        public void Generator_IsDeterministicForDailySeed()
        {
            var a = LevelSimulator.Run(_cfg, GameMode.DailyChallenge, BotPolicy.Profile.Perfect, 20260913, maxMetres: 400f);
            var b = LevelSimulator.Run(_cfg, GameMode.DailyChallenge, BotPolicy.Profile.Perfect, 20260913, maxMetres: 400f);
            Assert.AreEqual(a.Metres, b.Metres, 0.001f);
            Assert.AreEqual(a.BlocksUsed, b.BlocksUsed);
            Assert.AreEqual(a.Coins, b.Coins);
        }

        [Test]
        public void Patterns_AllParse()
        {
            foreach (var p in LevelPatterns.All)
            {
                var tokens = LevelPatterns.Parse(p.Tokens);
                Assert.IsNotEmpty(tokens, p.Name);
                foreach (var t in tokens) StringAssert.Contains(t.Op.ToString(), "FCGSUDWTEL", $"{p.Name}: unknown token {t.Op}");
            }
        }
    }
}
