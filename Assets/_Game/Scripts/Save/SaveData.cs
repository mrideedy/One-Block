using System;
using System.Collections.Generic;

namespace OneBlock.Save
{
    /// <summary>
    /// Everything persisted between sessions. Bump <see cref="CurrentVersion"/> and migrate in
    /// <see cref="SaveSystem"/> when the shape changes.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        // Economy & records
        public int coins;
        public float bestDistance;            // Endless
        public float challengeBestDistance;   // One Block Challenge
        public int totalRuns;
        public long totalCoinsEarned;

        // Progression
        public int playerLevel = 1;
        public int playerXp;                  // XP into the current level
        public List<MissionState> missions = new List<MissionState>();
        public int missionsGenerated;
        public int missionsCompleted;

        // Customization (ids reference catalog entries)
        public List<string> ownedItems = new List<string> { "char_default", "block_normal", "world_grassland" };
        public string equippedCharacter = "char_default";
        public string equippedBlock = "block_normal";
        public string equippedTrail = "";
        public string equippedDeathEffect = "";
        public string selectedWorld = "world_grassland";
        public int selectedMode;   // GameMode

        // Rewarded-ad progress towards ad-exclusive bundles
        public List<BundleProgress> bundleProgress = new List<BundleProgress>();

        // Daily challenge: yyyyMMdd (UTC) -> best distance, plus the play streak
        public List<DailyRecord> dailyRecords = new List<DailyRecord>();
        public int dailyStreak;
        public int bestDailyStreak;
        public int lastDailyDate;   // yyyyMMdd of the last day a daily run was played (0 = never)

        // Ads pacing
        public int runsSinceInterstitial;
        public long lastInterstitialUnixSeconds;
        public int sessionCount;

        // Onboarding & settings
        public bool tutorialCompleted;
        public bool musicOn = true;
        public string musicTrack = "";        // "" = auto (each world plays its own track)
        public bool sfxOn = true;
        public bool hapticsOn = true;
    }

    [Serializable]
    public class BundleProgress
    {
        public string id;
        public int adsWatched;
    }

    [Serializable]
    public class DailyRecord
    {
        public int date; // yyyyMMdd
        public float bestDistance;
        public int attempts;
    }
}
