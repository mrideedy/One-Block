using System.Collections.Generic;
using OneBlock.Save;
using UnityEngine;

namespace OneBlock.Core
{
    /// <summary>
    /// Owns the top-level game state machine. Menu and gameplay share one scene so retry is instant.
    /// Menu → Playing ⇄ Paused; Playing → Continue (revive offer) → Playing | GameOver; GameOver → Playing | Menu.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] GameConfig config;
        [Tooltip("Stay in Boot until the intro calls FinishBoot (with a safety timeout).")]
        [SerializeField] bool waitForIntro = true;
        const float IntroTimeout = 6f;

        public GameConfig Config => config;
        public GameState State { get; private set; } = GameState.Boot;
        public GameMode CurrentMode { get; private set; } = GameMode.Endless;
        public GameMode SelectedMode => (GameMode)Mathf.Clamp(SaveSystem.Data.selectedMode, 0, 2);

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Application.targetFrameRate = config != null ? config.targetFrameRate : 60;
            QualitySettings.vSyncCount = 0;
#if UNITY_EDITOR
            Application.runInBackground = true; // keep simulating while the editor window is unfocused
#endif

            SaveSystem.Load();
            Missions.EnsureMissions(SaveSystem.Data);
            if (GetComponent<MissionTracker>() == null) gameObject.AddComponent<MissionTracker>();
            SaveSystem.Data.sessionCount++;
            SaveSystem.MarkDirty();
            CurrentMode = SelectedMode;
        }

        void Start()
        {
            if (waitForIntro) Invoke(nameof(FinishBoot), IntroTimeout);
            else FinishBoot();
        }

        /// <summary>Leaves the boot intro for the main menu.</summary>
        public void FinishBoot()
        {
            CancelInvoke(nameof(FinishBoot));
            if (State == GameState.Boot) SetState(GameState.Menu);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SelectMode(GameMode mode)
        {
            SaveSystem.Data.selectedMode = (int)mode;
            SaveSystem.MarkDirty();
            CurrentMode = mode;
            GameEvents.RaiseModeSelected(mode);
        }

        public void StartRun(GameMode mode)
        {
            if (State != GameState.Menu && State != GameState.GameOver) return;
            CurrentMode = mode;
            SetState(GameState.Playing);
            GameEvents.RaiseRunStarted(mode);
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            SetState(GameState.Playing);
        }

        public void OfferContinue()
        {
            if (State != GameState.Playing) return;
            SetState(GameState.Continue);
        }

        public void Revive()
        {
            if (State != GameState.Continue) return;
            SetState(GameState.Playing);
            GameEvents.RaiseRevived();
        }

        /// <summary>Finishes the run and shows results.</summary>
        public void EndRun(RunResult result)
        {
            if (State != GameState.Playing && State != GameState.Continue) return;
            result = Record(result);
            SetState(GameState.GameOver);
            GameEvents.RaiseRunEnded(result);
        }

        /// <summary>Quitting to the menu mid-run: progress still counts, but no results screen.</summary>
        public void AbandonRun(RunResult result)
        {
            if (State == GameState.Menu || State == GameState.GameOver) return;
            Record(result);
            SetState(GameState.Menu);
        }

        public void ReturnToMenu() => SetState(GameState.Menu);

        /// <summary>Best distance for a mode; for Daily it's today's best.</summary>
        public static float BestFor(GameMode mode)
        {
            var d = SaveSystem.Data;
            return mode switch
            {
                GameMode.DailyChallenge => DailyProgress.RecordFor(d, DailyProgress.Today)?.bestDistance ?? 0f,
                GameMode.OneBlockChallenge => d.challengeBestDistance,
                _ => d.bestDistance
            };
        }

        RunResult Record(RunResult result)
        {
            var data = SaveSystem.Data;
            data.totalRuns++;
            data.runsSinceInterstitial++;
            switch (result.Mode)
            {
                case GameMode.DailyChallenge:
                {
                    var daily = DailyProgress.RegisterRun(data, config, result.Distance, DailyProgress.Clock());
                    result.PreviousBest = daily.PreviousBest;
                    result.IsNewBest = daily.IsNewBest;
                    result.DailyFirstToday = daily.FirstToday;
                    result.DailyStreak = daily.StreakDay;
                    result.DailyReward = daily.Reward;
                    break;
                }
                case GameMode.OneBlockChallenge:
                    result.PreviousBest = data.challengeBestDistance;
                    if (result.Distance > data.challengeBestDistance)
                    {
                        data.challengeBestDistance = result.Distance;
                        result.IsNewBest = true;
                    }
                    break;
                default:
                    result.PreviousBest = data.bestDistance;
                    if (result.Distance > data.bestDistance)
                    {
                        data.bestDistance = result.Distance;
                        result.IsNewBest = true;
                    }
                    break;
            }
            // Missions that count whole runs, then XP for the distance
            var completed = new List<MissionState>();
            Missions.Report(data, MissionType.Runs, 1, completed);
            if (result.Mode == GameMode.DailyChallenge) Missions.Report(data, MissionType.PlayDaily, 1, completed);

            result.LevelBefore = data.playerLevel;
            result.XpProgressBefore = Progression.LevelProgress(config, data);
            var level = Progression.AddXp(data, config, Progression.XpForRun(config, result.Distance));
            result.XpGained = level.XpGained;
            result.LevelAfter = level.LevelAfter;
            result.LevelUpCoins = level.Coins;

            SaveSystem.Save();
            foreach (var m in completed) GameEvents.RaiseMissionCompleted(m);
            if (completed.Count > 0) GameEvents.RaiseMissionsChanged();
            if (result.DailyReward > 0 || level.Coins > 0) GameEvents.RaiseWalletChanged(data.coins);
            return result;
        }

        void SetState(GameState next)
        {
            if (State == next) return;
            var prev = State;
            State = next;

            Time.timeScale = next == GameState.Paused ? 0f : 1f;
            Screen.sleepTimeout = next == GameState.Playing ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;

            GameEvents.RaiseStateChanged(prev, next);
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            Pause();
            SaveSystem.SaveIfDirty();
        }

        void OnApplicationQuit() => SaveSystem.SaveIfDirty();
    }
}
