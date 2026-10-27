using OneBlock.Core;
using UnityEngine;

namespace OneBlock.Save
{
    /// <summary>Player level and XP. Pure functions over <see cref="SaveData"/> so they can be unit-tested.</summary>
    public static class Progression
    {
        public struct LevelUp
        {
            public int XpGained;
            public int LevelBefore;
            public int LevelAfter;
            public int Coins;          // total level-up coins granted
            public bool Leveled => LevelAfter > LevelBefore;
        }

        /// <summary>XP needed to go from <paramref name="level"/> to the next level.</summary>
        public static int XpToNext(GameConfig cfg, int level) => cfg.xpBase + cfg.xpPerLevel * (Mathf.Max(1, level) - 1);

        public static int LevelUpCoins(GameConfig cfg, int newLevel) => cfg.levelUpCoinsBase + cfg.levelUpCoinsPerLevel * newLevel;

        public static int XpForRun(GameConfig cfg, float metres) => Mathf.FloorToInt(metres / Mathf.Max(1f, cfg.metresPerXp));

        /// <summary>Progress towards the next level, 0..1.</summary>
        public static float LevelProgress(GameConfig cfg, SaveData d) =>
            Mathf.Clamp01(d.playerXp / (float)XpToNext(cfg, d.playerLevel));

        /// <summary>Adds XP, rolling over as many levels as it covers and granting their coins.</summary>
        public static LevelUp AddXp(SaveData d, GameConfig cfg, int xp)
        {
            if (d.playerLevel < 1) d.playerLevel = 1;
            var r = new LevelUp { XpGained = Mathf.Max(0, xp), LevelBefore = d.playerLevel };
            d.playerXp += r.XpGained;
            while (d.playerXp >= XpToNext(cfg, d.playerLevel))
            {
                d.playerXp -= XpToNext(cfg, d.playerLevel);
                d.playerLevel++;
                int coins = LevelUpCoins(cfg, d.playerLevel);
                r.Coins += coins;
                d.coins += coins;
                d.totalCoinsEarned += coins;
            }
            r.LevelAfter = d.playerLevel;
            return r;
        }
    }
}
