using UnityEngine;

namespace OneBlock.Core
{
    /// <summary>
    /// Central tuning values. One asset lives at Assets/_Game/Data/GameConfig.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "One Block/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("World")]
        [Tooltip("Metres shown in the HUD per grid cell travelled.")]
        public float metresPerCell = 1f;

        [Header("Camera (landscape)")]
        [Tooltip("Vertical half-height of the view in world units.")]
        public float orthographicSize = 5.5f;
        [Tooltip("Minimum visible width in cells, enforced on narrow (4:3) screens.")]
        public float minVisibleWidth = 16f;
        [Tooltip("Where the player sits horizontally, as a fraction of view width from the left.")]
        [Range(0.1f, 0.5f)] public float playerScreenX = 0.26f;
        [Tooltip("Player's horizontal screen position on the main menu, left of the centred menu column.")]
        [Range(0.05f, 0.5f)] public float menuPlayerScreenX = 0.13f;
        [Tooltip("Seconds to glide between the menu and gameplay framing.")]
        public float framingSmoothing = 0.3f;
        [Tooltip("Where the ground sits vertically, as a fraction of view height from the bottom.")]
        [Range(0.1f, 0.5f)] public float groundScreenY = 0.3f;
        public float cameraVerticalSmoothing = 0.22f;

        [Header("Run speed")]
        public float startSpeed = 4.5f;
        public float maxSpeed = 8f;
        [Tooltip("Speed gained per 100 m.")]
        public float speedPerHundredMetres = 0.3f;

        [Header("Player physics")]
        public float gravity = 55f;
        public float maxFallSpeed = 22f;
        [Tooltip("Upward velocity of the automatic hop onto 1-high steps.")]
        public float hopVelocity = 12.5f;
        public float playerHalfWidth = 0.28f;
        public float playerHeight = 0.95f;
        [Tooltip("Falling this many cells below the last ground row ends the run.")]
        public float killDepth = 5f;
        public float deathDelay = 1.1f;

        [Header("Building")]
        public int startingBlocks = 10;
        [Tooltip("How many columns ahead of the player a tap can place a block.")]
        public int buildLookahead = 2;
        [Tooltip("A tap still catches the player this far (in cells) after they drop into a gap.")]
        public float rescueDepth = 0.75f;
        [Tooltip("A tap that finds no valid cell is retried for this long (seconds).")]
        public float tapBufferTime = 0.15f;

        [Header("Pickups & block economy")]
        public int blocksPerPack = 3;
        [Tooltip("Blocks a perfect player should still hold after each pattern, at the start of a run.")]
        public float reserveStart = 4f;
        [Tooltip("The same reserve once reserveRampMetres is reached.")]
        public float reserveLate = 1f;
        public float reserveRampMetres = 900f;
        [Tooltip("After this distance needed packs are sometimes withheld, so flawless runs eventually end.")]
        public float scarcityStartMetres = 1200f;
        public float scarcityFullMetres = 3000f;
        [Range(0f, 1f)] public float maxPackSkipChance = 0.4f;
        public int coinsPerSquash = 3;

        [Header("Progression")]
        [Tooltip("Metres run per 1 XP.")]
        public float metresPerXp = 10f;
        [Tooltip("XP from level 1 to 2; each further level needs xpPerLevel more.")]
        public int xpBase = 100;
        public int xpPerLevel = 50;
        [Tooltip("Coins for reaching a level: base + perLevel x new level.")]
        public int levelUpCoinsBase = 50;
        public int levelUpCoinsPerLevel = 25;

        [Header("Daily Challenge")]
        [Tooltip("Bonus coins for the first daily run of the day on streak day 1.")]
        public int dailyBaseReward = 50;
        [Tooltip("Extra bonus per consecutive day.")]
        public int dailyRewardPerStreakDay = 25;
        [Tooltip("The bonus stops growing after this many days.")]
        public int dailyMaxStreakBonusDays = 7;

        [Header("One Block Challenge")]
        public int challengeStartingBlocks = 3;
        public int challengeBlockEveryMetres = 100;

        [Header("Performance")]
        public int targetFrameRate = 60;
        public float maxDeltaTime = 1f / 30f;
    }
}
