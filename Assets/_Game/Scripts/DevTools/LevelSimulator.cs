using System;
using System.Collections.Generic;
using OneBlock.Core;
using OneBlock.Gameplay;
using UnityEngine;

namespace OneBlock.DevTools
{
    /// <summary>
    /// Plays a whole run without rendering, at a fixed timestep, using the real generator, physics,
    /// builder and run rules. Thousands of metres simulate in milliseconds, so economy and difficulty
    /// can be tuned from data and guarded by tests.
    /// </summary>
    public static class LevelSimulator
    {
        public struct Result
        {
            public int Seed;
            public float Metres;
            public DeathCause Cause;
            public int BlocksUsed;
            public int BlocksLeft;
            public int PacksPlaced;
            public int PacksCollected;
            public int PacksMissed;   // packs the player passed without collecting
            public int Coins;
            public float Seconds;
            public string LastPattern;
        }

        const float StartX = 0.5f;
        const float TapCooldown = 0.08f;

        public static Result Run(GameConfig cfg, GameMode mode, BotPolicy.Profile profile, int seed,
            float maxMetres = 3000f, float dt = 1f / 60f, Action<BuildTarget, string> onBuild = null,
            Action<LevelGenerator, int, int> onEnterColumn = null)
        {
            var grid = new WorldGrid();
            var dynamic = new DynamicWorld(grid);
            var generator = new LevelGenerator(cfg);
            generator.Reset(seed, 0, mode);

            var go = new GameObject("SimulatedRun") { hideFlags = HideFlags.HideAndDontSave };
            var player = go.AddComponent<PlayerController>();
            var builder = go.AddComponent<BlockBuilder>();
            player.Init(cfg, grid, dynamic);
            builder.Init(cfg, grid, dynamic, player);
            builder.SetBlocks(RunRules.StartingBlocks(cfg, mode));
            player.ResetAt(new Vector2(StartX, 0f));

            var bot = new BotPolicy(profile, seed * 7919 + 13);
            var spawns = new List<SpawnRequest>();
            var pickups = new List<SpawnRequest>();
            int coins = 0, packsCollected = 0, packsMissed = 0;
            builder.EnemySquashed += _ => coins += cfg.coinsPerSquash;
            if (onBuild != null) builder.Built += t => onBuild(t, generator.PatternAt(t.Cell.x));

            int nextColumn = -12;
            int releasedBefore = nextColumn;
            float time = 0f, metres = 0f, stuck = 0f, cooldown = 0f;
            int lastColumn = int.MinValue;
            int nextChallengeBlock = cfg.challengeBlockEveryMetres;

            try
            {
                while (time < 3600f)
                {
                    // Stream the level ahead and release it behind, like LevelStreamer
                    while (nextColumn <= player.Position.x + 24f)
                    {
                        spawns.Clear();
                        generator.Generate(nextColumn, grid, spawns);
                        foreach (var s in spawns)
                        {
                            if (s.Kind == SpawnKind.Coin || s.Kind == SpawnKind.BlockPack) pickups.Add(s);
                            else if (s.Kind == SpawnKind.Enemy || s.Kind == SpawnKind.Elevator) dynamic.Spawn(s);
                        }
                        nextColumn++;
                    }
                    int behind = Mathf.FloorToInt(player.Position.x) - 14;
                    while (releasedBefore < behind) grid.RemoveColumn(releasedBefore++);
                    dynamic.RemoveColumnsBefore(behind);

                    cooldown -= dt;
                    if (!player.IsDead)
                    {
                        player.Speed = RunRules.Speed(cfg, metres);
                        if (cooldown <= 0f && bot.ShouldTap(builder, player, cfg.startSpeed, dt))
                        {
                            builder.TryBuild(out _);
                            cooldown = TapCooldown;
                        }
                    }

                    dynamic.Tick(dt);
                    player.Tick(dt);

                    stuck = RunRules.IsStuck(player, builder) ? stuck + dt : 0f;
                    if (stuck > RunRules.StuckGrace) player.Kill(DeathCause.OutOfBlocks);
                    if (player.IsDead) break;

                    Rect body = player.Bounds;
                    for (int i = pickups.Count - 1; i >= 0; i--)
                    {
                        var p = pickups[i];
                        if (p.Column < behind)
                        {
                            if (p.Kind == SpawnKind.BlockPack) packsMissed++;
                            pickups.RemoveAt(i);
                            continue;
                        }
                        float r = p.Kind == SpawnKind.Coin ? CollectibleSystem.CoinRadius : CollectibleSystem.PackRadius;
                        if (!CollectibleSystem.CircleOverlapsRect(p.Position, r, body)) continue;
                        if (p.Kind == SpawnKind.Coin) coins++;
                        else
                        {
                            builder.AddBlocks(cfg.blocksPerPack);
                            packsCollected++;
                        }
                        pickups.RemoveAt(i);
                    }

                    int col = Mathf.FloorToInt(player.Position.x);
                    if (col > lastColumn)
                    {
                        lastColumn = col;
                        onEnterColumn?.Invoke(generator, col, builder.Blocks);
                    }
                    metres = Mathf.Max(metres, RunRules.Metres(cfg, player, StartX));
                    if (mode == GameMode.OneBlockChallenge && metres >= nextChallengeBlock)
                    {
                        nextChallengeBlock += cfg.challengeBlockEveryMetres;
                        builder.AddBlocks(1);
                    }
                    if (metres >= maxMetres) break;
                    time += dt;
                }

                return new Result
                {
                    Seed = seed,
                    Metres = metres,
                    Cause = player.IsDead ? player.DeathCause : DeathCause.None,
                    BlocksUsed = builder.BlocksUsed,
                    BlocksLeft = builder.Blocks,
                    PacksPlaced = generator.PacksPlaced,
                    PacksCollected = packsCollected,
                    PacksMissed = packsMissed,
                    Coins = coins,
                    Seconds = time,
                    LastPattern = generator.PatternAt(Mathf.FloorToInt(player.Position.x))
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
