using System;
using System.Collections.Generic;
using OneBlock.Core;
using UnityEngine;

namespace OneBlock.Save
{
    public enum MissionType
    {
        RunDistance,        // reach X m in a single run (any mode)
        RunCoins,           // collect X coins in a single run
        TotalCoins,         // collect X coins in total
        Squash,             // squash X enemies
        Bridge,             // bridge X gaps
        Cover,              // cover X hazards
        Packs,              // grab X block packs
        BlocksPlaced,       // place X blocks
        Runs,               // play X runs
        ChallengeDistance,  // reach X m in One Block Challenge
        PlayDaily           // play the Daily Challenge X times
    }

    [Serializable]
    public class MissionState
    {
        public MissionType type;
        public int target;
        public int progress;
        public int rewardCoins;
        public int rewardXp;

        public bool IsComplete => progress >= target;
        public float Progress01 => target > 0 ? Mathf.Clamp01(progress / (float)target) : 1f;
        /// <summary>Single-run missions track the best run so far rather than a running total.</summary>
        public bool IsSingleRun => Missions.IsSingleRun(type);
    }

    /// <summary>
    /// Mission rules: generation scaled by player level, progress reports and claiming.
    /// Pure functions over <see cref="SaveData"/> so they can be unit-tested.
    /// </summary>
    public static class Missions
    {
        public const int Slots = 3;

        struct Template
        {
            public MissionType Type;
            public int Base, PerTier, MinLevel;
            public Template(MissionType type, int @base, int perTier, int minLevel = 1)
            {
                Type = type; Base = @base; PerTier = perTier; MinLevel = minLevel;
            }
        }

        static readonly Template[] Templates =
        {
            new(MissionType.RunDistance, 200, 100),
            new(MissionType.RunCoins, 15, 8),
            new(MissionType.TotalCoins, 60, 40),
            new(MissionType.Squash, 3, 2, minLevel: 2),
            new(MissionType.Bridge, 5, 3),
            new(MissionType.Cover, 3, 2),
            new(MissionType.Packs, 3, 2),
            new(MissionType.BlocksPlaced, 12, 8),
            new(MissionType.Runs, 3, 1),
            new(MissionType.ChallengeDistance, 100, 60, minLevel: 3),
            new(MissionType.PlayDaily, 1, 0, minLevel: 2),
        };

        public static bool IsSingleRun(MissionType t) =>
            t == MissionType.RunDistance || t == MissionType.RunCoins || t == MissionType.ChallengeDistance;

        public static int Tier(int level) => Mathf.Clamp((level - 1) / 2, 0, 8);

        public static string Describe(MissionState m, string hazardName = "traps") => m.type switch
        {
            MissionType.RunDistance => $"Run {m.target} m in one run",
            MissionType.RunCoins => $"Collect {m.target} coins in one run",
            MissionType.TotalCoins => $"Collect {m.target} coins",
            MissionType.Squash => $"Squash {m.target} enemies",
            MissionType.Bridge => $"Bridge {m.target} gaps",
            MissionType.Cover => $"Cover {m.target} {hazardName}",
            MissionType.Packs => $"Grab {m.target} block packs",
            MissionType.BlocksPlaced => $"Place {m.target} blocks",
            MissionType.Runs => $"Play {m.target} runs",
            MissionType.ChallengeDistance => $"Reach {m.target} m in One Block Challenge",
            MissionType.PlayDaily => m.target == 1 ? "Play the Daily Challenge" : $"Play the Daily Challenge {m.target} times",
            _ => m.type.ToString()
        };

        /// <summary>Fills empty slots with new missions (no duplicate types among the active ones).</summary>
        public static void EnsureMissions(SaveData d)
        {
            while (d.missions.Count < Slots) d.missions.Add(Generate(d));
        }

        public static MissionState Generate(SaveData d, MissionType? exclude = null)
        {
            int level = Mathf.Max(1, d.playerLevel);
            int tier = Tier(level);
            var rng = new System.Random(unchecked(d.missionsGenerated * 7919 + level * 131 + 17));
            d.missionsGenerated++;

            var pool = new List<Template>();
            foreach (var t in Templates)
            {
                if (t.MinLevel > level) continue;
                if (exclude.HasValue && t.Type == exclude.Value) continue;
                bool active = false;
                foreach (var m in d.missions) if (m.type == t.Type) active = true;
                if (!active) pool.Add(t);
            }
            if (pool.Count == 0) pool.Add(Templates[0]);

            var pick = pool[rng.Next(pool.Count)];
            int target = pick.Base + pick.PerTier * tier;
            // Round bigger numbers to tidy values
            if (target >= 50) target = Mathf.RoundToInt(target / 10f) * 10;
            return new MissionState
            {
                type = pick.Type,
                target = Mathf.Max(1, target),
                rewardCoins = 40 + 15 * tier,
                rewardXp = 20 + 5 * tier
            };
        }

        /// <summary>
        /// Adds progress to every active mission of a type. For single-run types pass the run's current value;
        /// progress keeps the best. Returns the missions that became complete with this report.
        /// </summary>
        public static List<MissionState> Report(SaveData d, MissionType type, int amount, List<MissionState> completed = null)
        {
            completed ??= new List<MissionState>();
            foreach (var m in d.missions)
            {
                if (m.type != type || m.IsComplete) continue;
                if (IsSingleRun(type)) m.progress = Mathf.Max(m.progress, amount);
                else m.progress += amount;
                if (m.IsComplete)
                {
                    m.progress = m.target;
                    completed.Add(m);
                }
            }
            return completed;
        }

        public static int ClaimableCount(SaveData d)
        {
            int n = 0;
            foreach (var m in d.missions) if (m.IsComplete) n++;
            return n;
        }

        /// <summary>Claims a completed mission: grants coins and XP and replaces it with a new mission.</summary>
        public static bool Claim(SaveData d, GameConfig cfg, int index, out Progression.LevelUp levelUp)
        {
            levelUp = default;
            if (index < 0 || index >= d.missions.Count || !d.missions[index].IsComplete) return false;
            var m = d.missions[index];
            d.coins += m.rewardCoins;
            d.totalCoinsEarned += m.rewardCoins;
            d.missionsCompleted++;
            levelUp = Progression.AddXp(d, cfg, m.rewardXp);
            d.missions[index] = Generate(d, exclude: m.type);
            return true;
        }

        /// <summary>Swaps an incomplete mission for a different one (after a rewarded ad).</summary>
        public static bool Reroll(SaveData d, int index)
        {
            if (index < 0 || index >= d.missions.Count || d.missions[index].IsComplete) return false;
            var old = d.missions[index];
            d.missions.RemoveAt(index);
            var fresh = Generate(d, exclude: old.type);
            d.missions.Insert(index, fresh);
            return true;
        }
    }
}
