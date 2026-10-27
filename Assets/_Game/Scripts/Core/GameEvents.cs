using System;
using UnityEngine;

namespace OneBlock.Core
{
    /// <summary>
    /// Global event hub. Systems publish here so gameplay, UI, audio and ads stay decoupled.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<GameState, GameState> StateChanged; // (from, to)
        public static event Action<GameMode> RunStarted;
        public static event Action<RunResult> RunEnded;
        public static event Action Revived;

        public static event Action<int> WalletChanged;      // total saved coins
        public static event Action<int> RunCoinsChanged;    // coins collected this run
        public static event Action<int> BlocksChanged;      // blocks left this run
        public static event Action<float> DistanceChanged;  // metres this run
        public static event Action BlockPlaced;
        public static event Action BuildDenied;             // tapped with no blocks left
        public static event Action<int> BlocksGained;
        public static event Action CoinCollected;
        public static event Action PlayerDied;
        public static event Action<GameMode> ModeSelected;
        public static event Action<bool> AwaitingTapChanged;   // true while the run waits for a tap to (re)start moving
        public static event Action<int> CoinsDoubled;
        public static event Action CosmeticsChanged;
        public static void RaiseCosmeticsChanged() => CosmeticsChanged?.Invoke();
        public static event Action<Gameplay.BuildKind> BlockBuilt;
        public static event Action EnemySquashed;
        public static event Action<int> PackCollected;              // number of packs picked up this frame
        public static event Action<Save.MissionState> MissionCompleted;
        public static event Action MissionsChanged;                 // claimed, rerolled or progressed
        public static void RaiseBlockBuilt(Gameplay.BuildKind kind) => BlockBuilt?.Invoke(kind);
        public static void RaiseEnemySquashed() => EnemySquashed?.Invoke();
        public static void RaisePackCollected(int count) => PackCollected?.Invoke(count);
        public static void RaiseMissionCompleted(Save.MissionState m) => MissionCompleted?.Invoke(m);
        public static void RaiseMissionsChanged() => MissionsChanged?.Invoke();
        public static event Action<TutorialEvent> Tutorial;
        public static void RaiseTutorial(TutorialEvent e) => Tutorial?.Invoke(e);

        public static void RaiseStateChanged(GameState from, GameState to) => StateChanged?.Invoke(from, to);
        public static void RaiseRunStarted(GameMode mode) => RunStarted?.Invoke(mode);
        public static void RaiseRunEnded(RunResult result) => RunEnded?.Invoke(result);
        public static void RaiseRevived() => Revived?.Invoke();
        public static void RaiseWalletChanged(int total) => WalletChanged?.Invoke(total);
        public static void RaiseRunCoinsChanged(int count) => RunCoinsChanged?.Invoke(count);
        public static void RaiseBlocksChanged(int count) => BlocksChanged?.Invoke(count);
        public static void RaiseDistanceChanged(float metres) => DistanceChanged?.Invoke(metres);
        public static void RaiseBlockPlaced() => BlockPlaced?.Invoke();
        public static void RaiseBuildDenied() => BuildDenied?.Invoke();
        public static void RaiseBlocksGained(int amount) => BlocksGained?.Invoke(amount);
        public static void RaiseCoinCollected() => CoinCollected?.Invoke();
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseModeSelected(GameMode mode) => ModeSelected?.Invoke(mode);
        public static void RaiseAwaitingTapChanged(bool waiting) => AwaitingTapChanged?.Invoke(waiting);
        public static void RaiseCoinsDoubled(int bonus) => CoinsDoubled?.Invoke(bonus);
    }

    public struct RunResult
    {
        public GameMode Mode;
        public float Distance;
        public int CoinsCollected;
        public int BlocksUsed;
        public bool Revived;
        public bool IsNewBest;
        public float PreviousBest;      // best for this mode (today's best for Daily) before the run
        public bool DailyFirstToday;    // Daily: this run claimed today's streak bonus
        public int DailyStreak;
        public int DailyReward;
        public int XpGained;
        public int LevelBefore;
        public int LevelAfter;
        public float XpProgressBefore;  // 0..1 within LevelBefore
        public int LevelUpCoins;
        public DeathCause Cause;
    }

    public enum TutorialPhase
    {
        Hidden,    // no tutorial (or it was cancelled)
        Intro,     // run just started: "your hero runs on their own"
        Hold,      // frozen in front of a lesson obstacle, waiting for the tap
        Success,   // the lesson's block was placed
        Complete,  // last lesson done; the run continues normally
        WallHint,  // (any run) pressed against a tall wall for a while with blocks in hand
        WallHintEnd
    }

    public struct TutorialEvent
    {
        public TutorialPhase Phase;
        public int Lesson;          // 0-based lesson index
        public Vector2Int Cell;     // grid cell the lesson's block goes into
        public string Hazard;       // the current world's name for its deadly cells ("spikes", "lava"...)
    }

    public enum DeathCause
    {
        None,
        Fell,
        Spikes,
        Enemy,
        OutOfBlocks,   // stuck at a wall with nothing left to build
        Quit
    }
}
