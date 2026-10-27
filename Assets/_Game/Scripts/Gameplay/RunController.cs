using OneBlock.Controls;
using OneBlock.Core;
using OneBlock.Data;
using OneBlock.Save;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>
    /// Orchestrates a run and ticks gameplay systems in a fixed order each frame:
    /// input → build → dynamic world → player physics → pickups → distance → camera → streaming.
    /// Also exposes the run actions the UI calls (play, restart, quit, revive, double coins).
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class RunController : MonoBehaviour
    {
        [SerializeField] GameAssets assets;
        [SerializeField] PlayerController player;
        [SerializeField] PlayerAnimator playerAnimator;
        [SerializeField] BlockBuilder builder;
        [SerializeField] LevelStreamer streamer;
        [SerializeField] TerrainRenderer terrain;
        [SerializeField] CollectibleSystem collectibles;
        [SerializeField] DynamicWorldRenderer dynamicRenderer;
        [SerializeField] Sprite[] enemyFrames;
        [SerializeField] Sprite elevatorSprite;
        [SerializeField] CameraFollow cameraFollow;
        [SerializeField] TapInput tapInput;
        [SerializeField] PlayerCosmetics cosmetics;
        [SerializeField] WorldThemeApplier worlds;

        const int StartColumn = 0;
        const int ReviveBlocks = 3;
        const int ReviveBlocksWhenStuck = 5;
        const int ReviveClearColumns = 5;

        // First-run tutorial: a fixed, safe opening that freezes at each lesson obstacle until the player taps
        const int TutorialSeed = 1409;
        static readonly BuildKind[] TutorialLessons = { BuildKind.Bridge, BuildKind.Cover };

        GameConfig _cfg;
        WorldGrid _grid;
        DynamicWorld _dynamic;
        GameManager _gm;

        float _distance;
        int _coins;
        int _nextChallengeBlockAt;
        float _deathTimer;
        float _stuckTimer;
        bool _worldDirty;
        bool _revived;
        bool _awaitingTap;
        bool _coinsDoubled;
        Vector2 _lastSafeFeet;
        bool _tutorial;
        bool _tutorialHold;
        int _lessonsDone;
        float _wallTimer;
        bool _wallHintShown;
        const float WallHintDelay = 1.0f;

        public float Distance => _distance;
        public int Coins => _coins;
        public int Blocks => builder.Blocks;
        public PlayerCosmetics Cosmetics => cosmetics;
        public WorldGrid Grid => _grid;
        public DynamicWorld Dynamic => _dynamic;
        public DynamicWorldRenderer DynamicRenderer => dynamicRenderer;
        public PlayerController Player => player;
        public LevelGenerator Generator => streamer.Generator;
        public DeathCause LastDeathCause => player.DeathCause;
        /// <summary>A revive can be offered: the player just died and hasn't revived this run.</summary>
        public bool CanRevive => player.IsDead && !_revived;
        public bool CanDoubleCoins => !_coinsDoubled && _coins > 0 && _gm.State == GameState.GameOver;
        /// <summary>The first-run tutorial is running (until its last lesson is done).</summary>
        public bool TutorialActive => _tutorial;
        /// <summary>Frozen in front of a tutorial obstacle, waiting for a tap.</summary>
        public bool TutorialHolding => _tutorialHold;

        void Start()
        {
            _gm = GameManager.Instance;
            _cfg = _gm.Config;
            _grid = new WorldGrid();
            _dynamic = new DynamicWorld(_grid);

            player.Init(_cfg, _grid, _dynamic);
            playerAnimator.Init(player, assets.defaultCharacter);
            builder.Init(_cfg, _grid, _dynamic, player);
            dynamicRenderer.Init(_dynamic, enemyFrames, elevatorSprite, assets.whitePixel);
            terrain.Init(_grid, assets.defaultWorld, assets.defaultBlock, assets.spikes, assets.flag);
            collectibles.Init(assets.coinFrames, assets.blockPack);
            streamer.Init(_cfg, _grid, _dynamic, terrain, collectibles);
            cameraFollow.Init(_cfg, player);

            builder.BlocksChanged += GameEvents.RaiseBlocksChanged;
            builder.Built += t =>
            {
                GameEvents.RaiseBlockPlaced();
                GameEvents.RaiseBlockBuilt(t.Kind);
            };
            builder.BuildFailed += r => { if (r == BuildFailReason.NoBlocks) GameEvents.RaiseBuildDenied(); };
            builder.EnemySquashed += _ =>
            {
                _coins += _cfg.coinsPerSquash;
                GameEvents.RaiseRunCoinsChanged(_coins);
                GameEvents.RaiseEnemySquashed();
            };
            player.Died += _ => GameEvents.RaisePlayerDied();

            cosmetics.Init();
            worlds.Init();
            ResetWorld(_gm.SelectedMode);
        }

        void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.ModeSelected += OnModeSelected;
        }

        void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.ModeSelected -= OnModeSelected;
        }

        void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Menu && _worldDirty) ResetWorld(_gm.SelectedMode);
            if (to == GameState.Playing) tapInput.Consume();
            builder.SetPreviewVisible(to == GameState.Playing && !_awaitingTap);
        }

        // Re-roll the menu backdrop so the Daily Challenge preview shows today's level
        void OnModeSelected(GameMode mode)
        {
            if (_gm.State == GameState.Menu) ResetWorld(mode);
        }

        // ------------------------------------------------------------------ UI actions

        /// <summary>Starts a run in the mode selected on the menu.</summary>
        public void Play() => BeginRun(_gm.SelectedMode);

        /// <summary>Starts a fresh run in the same mode (from pause or results).</summary>
        public void Restart()
        {
            var mode = _gm.CurrentMode;
            if (_gm.State == GameState.Paused || _gm.State == GameState.Continue)
            {
                BankCoins();
                _gm.AbandonRun(BuildResult());
            }
            if (_gm.State != GameState.Menu && _gm.State != GameState.GameOver) return;
            ResetWorld(mode);
            BeginRun(mode);
        }

        /// <summary>Back to the main menu. Coins collected so far are kept.</summary>
        public void GoHome()
        {
            if (_gm.State == GameState.Paused || _gm.State == GameState.Playing || _gm.State == GameState.Continue)
            {
                BankCoins();
                _gm.AbandonRun(BuildResult());
            }
            else _gm.ReturnToMenu();
        }

        /// <summary>Called after a rewarded ad succeeds on the Continue screen.</summary>
        public void Revive()
        {
            if (!CanRevive || _gm.State != GameState.Continue) return;
            bool stuck = player.DeathCause == DeathCause.OutOfBlocks;
            Vector2 feet = stuck ? player.Position : _lastSafeFeet;

            ClearDangersFrom(Mathf.FloorToInt(feet.x));
            player.Revive(feet);
            playerAnimator.ResetVisuals();
            cosmetics.ResetVisuals();
            builder.AddBlocks(stuck ? ReviveBlocksWhenStuck : ReviveBlocks);
            GameEvents.RaiseBlocksGained(stuck ? ReviveBlocksWhenStuck : ReviveBlocks);

            _revived = true;
            _deathTimer = 0f;
            _stuckTimer = 0f;
            cameraFollow.Snap();
            SetAwaitingTap(true);
            _gm.Revive();
        }

        /// <summary>Player declined (or timed out) the revive offer.</summary>
        public void DeclineRevive()
        {
            if (_gm.State == GameState.Continue) FinishRun();
        }

        /// <summary>Called after a rewarded ad succeeds on the results screen.</summary>
        public void DoubleCoins()
        {
            if (!CanDoubleCoins) return;
            _coinsDoubled = true;
            var data = SaveSystem.Data;
            data.coins += _coins;
            data.totalCoinsEarned += _coins;
            SaveSystem.Save();
            GameEvents.RaiseWalletChanged(data.coins);
            GameEvents.RaiseCoinsDoubled(_coins);
        }

        // ------------------------------------------------------------------ run lifecycle

        public void ResetWorld(GameMode mode, bool tutorial = false)
        {
            int seed = tutorial ? TutorialSeed
                : mode == GameMode.DailyChallenge ? DailySeed() : Random.Range(int.MinValue, int.MaxValue);
            dynamicRenderer.ClearAll();
            streamer.ResetLevel(seed, StartColumn, mode, tutorial);
            if (_tutorial && !tutorial) GameEvents.RaiseTutorial(new TutorialEvent { Phase = TutorialPhase.Hidden });
            _tutorial = tutorial;
            _tutorialHold = false;
            _lessonsDone = 0;
            HideWallHint();

            var feet = new Vector2(StartColumn + 0.5f, 0f);
            player.ResetAt(feet);
            playerAnimator.ResetVisuals();
            cosmetics.ResetVisuals();
            _lastSafeFeet = feet;

            builder.SetBlocks(RunRules.StartingBlocks(_cfg, mode));
            _nextChallengeBlockAt = _cfg.challengeBlockEveryMetres;
            _distance = 0f;
            _coins = 0;
            _deathTimer = 0f;
            _stuckTimer = 0f;
            _worldDirty = false;
            _revived = false;
            _coinsDoubled = false;
            _coinsBanked = false;
            SetAwaitingTap(false);

            cameraFollow.Snap();
            streamer.Tick(cameraFollow.Left, cameraFollow.Right);
            GameEvents.RaiseDistanceChanged(0f);
            GameEvents.RaiseRunCoinsChanged(0);
        }

        void BeginRun(GameMode mode)
        {
            // New players always start with the tutorial, in Endless
            bool tutorial = !SaveSystem.Data.tutorialCompleted;
            if (tutorial) mode = GameMode.Endless;
            if (_worldDirty || mode != _gm.CurrentMode || tutorial) ResetWorld(mode, tutorial);
            _worldDirty = true;
            tapInput.Consume();
            _gm.StartRun(mode);
            _runStartRealtime = Time.realtimeSinceStartup;
            if (tutorial) GameEvents.RaiseTutorial(new TutorialEvent { Phase = TutorialPhase.Intro });
        }

        string HazardName => worlds != null && worlds.Current != null && !string.IsNullOrEmpty(worlds.Current.hazardName)
            ? worlds.Current.hazardName : "spikes";

        /// <summary>Shows a tap hint when the player has been pressed against a tall wall for a while with blocks in hand.</summary>
        void TickWallHint(float dt)
        {
            bool atWall = !player.IsDead && player.Grounded && !player.OnPlatform && player.IsBlocked &&
                          player.WallHeight >= 2 && builder.Blocks > 0;
            _wallTimer = atWall ? _wallTimer + dt : 0f;
            if (atWall && !_wallHintShown && _wallTimer >= WallHintDelay)
            {
                _wallHintShown = true;
                var t = builder.FindTarget();
                GameEvents.RaiseTutorial(new TutorialEvent { Phase = TutorialPhase.WallHint, Cell = t.IsValid ? t.Cell : Vector2Int.FloorToInt(player.Position) });
            }
            else if (!atWall) HideWallHint();
        }

        void HideWallHint()
        {
            _wallTimer = 0f;
            if (!_wallHintShown) return;
            _wallHintShown = false;
            GameEvents.RaiseTutorial(new TutorialEvent { Phase = TutorialPhase.WallHintEnd });
        }

        /// <summary>
        /// Runs the tutorial for this frame. Returns true when the run must not advance (frozen at a lesson).
        /// Until every lesson is done, taps only count while frozen, so a stray tap can't skip a lesson.
        /// </summary>
        bool TickTutorial(float dt)
        {
            if (!_tutorial || player.IsDead || _lessonsDone >= TutorialLessons.Length) return false;

            if (_tutorialHold)
            {
                player.Speed = 0f;
                playerAnimator.Tick(dt, runActive: false);
                builder.TickPreview();
                if (!tapInput.HasTap(_cfg.tapBufferTime)) return true;
                tapInput.Consume();

                var target = builder.FindTarget();
                if (!builder.TryBuild(out _)) return true;

                _tutorialHold = false;
                _lessonsDone++;
                bool done = _lessonsDone >= TutorialLessons.Length;
                GameEvents.RaiseTutorial(new TutorialEvent
                {
                    Phase = done ? TutorialPhase.Complete : TutorialPhase.Success,
                    Lesson = _lessonsDone - 1,
                    Cell = target.Cell
                });
                if (done)
                {
                    _tutorial = false;
                    SaveSystem.Data.tutorialCompleted = true;
                    SaveSystem.Save();
                }
                return true;
            }

            var t = builder.FindTarget();
            if (t.Kind == TutorialLessons[_lessonsDone])
            {
                _tutorialHold = true;
                player.Speed = 0f;
                builder.TickPreview();
                GameEvents.RaiseTutorial(new TutorialEvent { Phase = TutorialPhase.Hold, Lesson = _lessonsDone, Cell = t.Cell, Hazard = HazardName });
                return true;
            }

            tapInput.Consume();
            return false;
        }

        public static int DailySeed() => DailyProgress.Today;

        void SetAwaitingTap(bool waiting)
        {
            if (_awaitingTap == waiting) return;
            _awaitingTap = waiting;
            builder.SetPreviewVisible(!waiting && _gm != null && _gm.State == GameState.Playing);
            GameEvents.RaiseAwaitingTapChanged(waiting);
        }

        /// <summary>Removes spikes and enemies just ahead of a revive point so the player isn't killed instantly.</summary>
        void ClearDangersFrom(int column)
        {
            for (int c = column - 1; c <= column + ReviveClearColumns; c++)
            for (int y = WorldGrid.MinRow; y <= WorldGrid.MaxRow; y++)
                if (_grid.IsHazard(c, y)) _grid.Set(c, y, CellType.Empty);

            foreach (var e in _dynamic.Enemies)
                if (e.Alive && e.X >= column - 1 && e.X <= column + ReviveClearColumns + 1) _dynamic.Squash(e);
        }

        // ------------------------------------------------------------------ frame

        void Update()
        {
            if (_gm == null) return;
            float dt = Mathf.Min(Time.deltaTime, _cfg.maxDeltaTime);

            switch (_gm.State)
            {
                case GameState.Menu:
                    playerAnimator.Tick(dt, runActive: false);
                    tapInput.Consume();
                    break;

                case GameState.Playing:
                    TickRun(dt);
                    break;

                default:
                    tapInput.Consume();
                    break;
            }

            if (_gm.State != GameState.Playing && _gm.State != GameState.Paused) _dynamic.Tick(dt);
            cameraFollow.Tick(dt);
            streamer.Tick(cameraFollow.Left, cameraFollow.Right);
        }

        void TickRun(float dt)
        {
            if (_awaitingTap)
            {
                // Frozen after a revive until the player taps, so they're ready for what's ahead
                player.Speed = 0f;
                playerAnimator.Tick(dt, runActive: false);
                if (tapInput.HasTap(_cfg.tapBufferTime))
                {
                    tapInput.Consume();
                    SetAwaitingTap(false);
                }
                return;
            }

            if (TickTutorial(dt)) return;

            if (!player.IsDead)
            {
                player.Speed = RunRules.Speed(_cfg, _distance);

                if (tapInput.HasTap(_cfg.tapBufferTime))
                {
                    if (builder.TryBuild(out var reason) || reason == BuildFailReason.NoBlocks)
                        tapInput.Consume();
                    // NoTarget: leave the tap buffered and retry next frame
                }
            }
            else tapInput.Consume();

            _dynamic.Tick(dt);
            player.Tick(dt);
            playerAnimator.Tick(dt, runActive: true);

            TickWallHint(dt);

            // Pressed against a tall wall with no blocks left: nothing can ever change, so end the run
            _stuckTimer = RunRules.IsStuck(player, builder) ? _stuckTimer + dt : 0f;
            if (_stuckTimer > RunRules.StuckGrace) player.Kill(DeathCause.OutOfBlocks);

            if (player.Grounded && !player.OnPlatform && !player.IsDead)
                _lastSafeFeet = new Vector2(Mathf.Floor(player.Position.x) + 0.5f, player.GroundRow);

            if (!player.IsDead)
            {
                collectibles.Tick(dt, player.Bounds, out int coins, out int packs);
                if (coins > 0)
                {
                    _coins += coins;
                    GameEvents.RaiseRunCoinsChanged(_coins);
                    for (int i = 0; i < coins; i++) GameEvents.RaiseCoinCollected();
                }
                if (packs > 0)
                {
                    GameEvents.RaisePackCollected(packs);
                    builder.AddBlocks(packs * _cfg.blocksPerPack);
                    GameEvents.RaiseBlocksGained(packs * _cfg.blocksPerPack);
                }

                float metres = RunRules.Metres(_cfg, player, StartColumn + 0.5f);
                if (metres > _distance + 0.01f)
                {
                    _distance = metres;
                    GameEvents.RaiseDistanceChanged(_distance);
                }

                if (_gm.CurrentMode == GameMode.OneBlockChallenge && _distance >= _nextChallengeBlockAt)
                {
                    _nextChallengeBlockAt += _cfg.challengeBlockEveryMetres;
                    builder.AddBlocks(1);
                    GameEvents.RaiseBlocksGained(1);
                }

                builder.TickPreview();
            }
            else
            {
                collectibles.Tick(dt, Rect.zero, out _, out _);
                builder.SetPreviewVisible(false);
                _deathTimer += dt;
                if (_deathTimer >= _cfg.deathDelay)
                {
                    if (CanRevive) _gm.OfferContinue();
                    else FinishRun();
                }
            }
        }

        // ------------------------------------------------------------------ results

        bool _coinsBanked;

        void BankCoins()
        {
            if (_coinsBanked) return;
            _coinsBanked = true;
            var data = SaveSystem.Data;
            data.coins += _coins;
            data.totalCoinsEarned += _coins;
            GameEvents.RaiseWalletChanged(data.coins);
        }

        RunResult BuildResult() => new RunResult
        {
            Mode = _gm.CurrentMode,
            Distance = _distance,
            CoinsCollected = _coins,
            BlocksUsed = builder.BlocksUsed,
            Revived = _revived,
            Cause = player.DeathCause
        };

        float _runStartRealtime;

        /// <summary>Real seconds the last finished run lasted (ad pacing skips interstitials after quick runs).</summary>
        public float LastRunSeconds { get; private set; }

        void FinishRun()
        {
            LastRunSeconds = Time.realtimeSinceStartup - _runStartRealtime;
            BankCoins();
            _gm.EndRun(BuildResult());
        }
    }
}
