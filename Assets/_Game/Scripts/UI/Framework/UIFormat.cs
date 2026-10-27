using OneBlock.Core;

namespace OneBlock.UI
{
    public static class UIFormat
    {
        public static string Metres(float m) => $"{m:0} m";

        public static string Coins(int c) => c >= 10000 ? $"{c / 1000f:0.#}k" : c.ToString();

        public static string ModeName(GameMode mode) => mode switch
        {
            GameMode.DailyChallenge => "DAILY CHALLENGE",
            GameMode.OneBlockChallenge => "ONE BLOCK CHALLENGE",
            _ => "ENDLESS RUN"
        };

        /// <summary>"BEST" label for a mode; Daily bests reset every day.</summary>
        public static string BestLabel(GameMode mode) => mode == GameMode.DailyChallenge ? "TODAY" : "BEST";

        public static string Countdown(System.TimeSpan t) =>
            t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes:00}m" : $"{t.Minutes}m {t.Seconds:00}s";

        public static string ModeDescription(GameMode mode) => mode switch
        {
            GameMode.DailyChallenge => "Same level for everyone today.\nHow far can you get?",
            GameMode.OneBlockChallenge => "Start with 3 blocks.\n+1 block every 100 m.",
            _ => "Classic mode.\nGo as far as you can!"
        };

        public static string DeathTitle(DeathCause cause) => cause switch
        {
            DeathCause.OutOfBlocks => "OUT OF BLOCKS",
            _ => "GAME OVER"
        };
    }
}
