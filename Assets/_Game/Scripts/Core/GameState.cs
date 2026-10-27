namespace OneBlock.Core
{
    public enum GameState
    {
        Boot,
        Menu,
        Playing,
        Paused,
        Continue,   // run frozen after death, offering a revive
        GameOver
    }

    public enum GameMode
    {
        Endless,
        DailyChallenge,
        OneBlockChallenge
    }
}
