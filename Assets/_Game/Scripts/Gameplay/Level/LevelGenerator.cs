using System.Collections.Generic;
using OneBlock.Core;
using UnityEngine;

namespace OneBlock.Gameplay
{
    public enum SpawnKind
    {
        Coin,
        BlockPack,
        Tree,
        Bush,
        Tuft,
        Flag,
        Enemy,
        Elevator
    }

    public struct SpawnRequest
    {
        public SpawnKind Kind;
        public Vector2 Position;
        public int Column;
        public int Variant;
        public int Span;     // enemy patrol length / elevator width, in columns
        public float Value;  // enemy speed / elevator period
    }

    /// <summary>
    /// Streams the level column by column from a seeded RNG by chaining authored
    /// <see cref="LevelPatterns"/> separated by recovery flats.
    ///
    /// Fairness comes from a block ledger: the generator tracks how many blocks a perfect player
    /// would hold. Before each pattern, if paying its cost would drop the ledger below a reserve,
    /// a block pack is placed in the recovery flat. The reserve shrinks with distance and, late in a
    /// run, packs are occasionally withheld, so even flawless runs eventually end.
    /// </summary>
    public sealed class LevelGenerator
    {
        const int NoCoin = int.MinValue;
        public const int MinHeight = -2;
        public const int MaxHeight = 6;
        public const int ElevatorWidth = 3;

        struct Column
        {
            public bool Gap;
            public int Height;       // walking surface row; ground fills rows below it
            public bool Spikes;
            public int CoinRow;      // coin centre row (walking row so the runner collects it)
            public bool BlockPack;
            public bool AllowDecor;
            public int EnemySpan;    // >0 on the first column of an enemy patrol
            public int Elevator;     // >0 on the first column of an elevator lane
        }

        readonly GameConfig _cfg;
        readonly Queue<Column> _plan = new();
        readonly List<Column> _scratch = new();
        System.Random _rng;
        GameMode _mode;
        int _height;
        int _startX;
        int _lastFlagMetres;
        int _challengeBlocksCredited;
        LevelPatterns.Pattern _lastPattern;
        readonly List<(int column, string name)> _patternLog = new();

        /// <summary>Diagnostics: (first column of pattern, name, cost, ledger after paying, pack placed before it).</summary>
        public readonly List<(int column, string name, int cost, float ledger, bool pack)> PlanLog = new();

        /// <summary>Blocks a perfect player would hold at the most recently planned point.</summary>
        public float Ledger { get; private set; }
        public int PacksPlaced { get; private set; }
        public string LastPatternName => _lastPattern?.Name;

        /// <summary>Name of the pattern that produced the given column (recent columns only; for diagnostics).</summary>
        public string PatternAt(int column)
        {
            string name = "runway";
            foreach (var (col, n) in _patternLog)
            {
                if (col > column) break;
                name = n;
            }
            return name;
        }

        public LevelGenerator(GameConfig cfg)
        {
            _cfg = cfg;
        }

        /// <summary>First-run tutorial obstacles, in order, before the random level starts.</summary>
        public static readonly LevelPatterns.Pattern[] TutorialPatterns =
        {
            new LevelPatterns.Pattern { Name = "tutorial gap", Tokens = "F3 G1 F6" },
            new LevelPatterns.Pattern { Name = "tutorial spike", Tokens = "F3 S1 F8" },
        };

        readonly Queue<LevelPatterns.Pattern> _scripted = new();

        public void Reset(int seed, int startX, GameMode mode, bool tutorial = false)
        {
            _scripted.Clear();
            if (tutorial) foreach (var p in TutorialPatterns) _scripted.Enqueue(p);
            _rng = new System.Random(seed);
            _plan.Clear();
            _mode = mode;
            _height = 0;
            _startX = startX;
            _lastFlagMetres = 0;
            _challengeBlocksCredited = 0;
            _lastPattern = null;
            _patternLog.Clear();
            PlanLog.Clear();
            PacksPlaced = 0;
            Ledger = mode == GameMode.OneBlockChallenge ? _cfg.challengeStartingBlocks : _cfg.startingBlocks;

            // Safe runway that also covers the columns behind the start position
            for (int i = 0; i < 26; i++) _plan.Enqueue(FlatColumn(false, true));
        }

        public void Generate(int x, WorldGrid grid, List<SpawnRequest> spawns)
        {
            if (_plan.Count == 0) PlanNext(x);
            var col = _plan.Dequeue();

            grid.EnsureColumn(x);
            if (!col.Gap)
            {
                for (int y = WorldGrid.MinRow; y < col.Height; y++)
                    grid.Set(x, y, CellType.Ground, notify: false);
                if (col.Spikes) grid.Set(x, col.Height, CellType.Spikes, notify: false);
            }

            float metres = (x - _startX) * _cfg.metresPerCell;
            float d = Difficulty(metres);

            if (col.CoinRow != NoCoin)
                spawns.Add(new SpawnRequest { Kind = SpawnKind.Coin, Column = x, Position = new Vector2(x + 0.5f, col.CoinRow + 0.5f) });

            if (col.BlockPack)
                spawns.Add(new SpawnRequest { Kind = SpawnKind.BlockPack, Column = x, Position = new Vector2(x + 0.5f, col.Height + 0.65f) });

            if (col.EnemySpan > 0)
            {
                spawns.Add(new SpawnRequest
                {
                    Kind = SpawnKind.Enemy,
                    Column = x,
                    Span = col.EnemySpan,
                    Position = new Vector2(x + col.EnemySpan - 0.5f, col.Height),
                    Value = Mathf.Lerp(1.0f, 1.8f, d)
                });
            }

            if (col.Elevator > 0)
            {
                spawns.Add(new SpawnRequest
                {
                    Kind = SpawnKind.Elevator,
                    Column = x,
                    Span = col.Elevator,
                    Position = new Vector2(x, col.Height),
                    Value = Mathf.Lerp(3.2f, 2.4f, d)
                });
            }

            if (!col.Gap && !col.Spikes && col.AllowDecor && !col.BlockPack && col.CoinRow == NoCoin)
            {
                double roll = _rng.NextDouble();
                if (roll < 0.07) spawns.Add(Decor(SpawnKind.Tree, x, col.Height, Range(0, 2)));
                else if (roll < 0.12) spawns.Add(Decor(SpawnKind.Bush, x, col.Height, 0));
                else if (roll < 0.22) spawns.Add(Decor(SpawnKind.Tuft, x, col.Height, 0));
            }

            // Milestone flag every 100 m
            int m = Mathf.FloorToInt(metres);
            if (m > 0 && m % 100 == 0 && m != _lastFlagMetres && !col.Gap)
            {
                _lastFlagMetres = m;
                spawns.Add(new SpawnRequest { Kind = SpawnKind.Flag, Column = x, Position = new Vector2(x + 0.3f, col.Height), Variant = m });
            }
        }

        // ------------------------------------------------------------------ planning

        float Difficulty(float metres) => Mathf.Clamp01(metres / 1200f);

        void PlanNext(int x)
        {
            float metres = (x - _startX) * _cfg.metresPerCell;
            float d = Difficulty(metres);
            bool challenge = _mode == GameMode.OneBlockChallenge;

            if (challenge)
            {
                int earned = Mathf.FloorToInt(metres / _cfg.challengeBlockEveryMetres);
                Ledger += earned - _challengeBlocksCredited;
                _challengeBlocksCredited = earned;
            }

            var pattern = PickPattern(metres, challenge);
            _lastPattern = pattern;

            // Build the pattern into scratch first so its real cost (after height clamping) is known
            int heightBefore = _height;
            _scratch.Clear();
            int cost = Emit(pattern, d);
            int heightAfter = _height;

            // Recovery flat at the height before the pattern, carrying as many block packs as needed to
            // keep the perfect-play ledger at or above the reserve after paying for the pattern
            _height = heightBefore;
            int minRecovery = Mathf.RoundToInt(Mathf.Lerp(4, 2, d));
            int recovery = Range(minRecovery, minRecovery + 2);
            int packs = 0;
            if (!challenge)
            {
                float shortfall = Reserve(metres) - (Ledger - cost);
                int needed = shortfall > 0f ? Mathf.CeilToInt(shortfall / _cfg.blocksPerPack) : 0;
                for (int i = 0; i < needed; i++)
                    if (_rng.NextDouble() >= PackSkipChance(metres)) packs++;
            }
            bool pack = packs > 0;
            if (pack)
            {
                recovery = Mathf.Max(recovery, 2 + packs);
                Ledger += packs * _cfg.blocksPerPack;
                PacksPlaced += packs;
            }
            for (int i = 0; i < recovery; i++)
            {
                var c = FlatColumn(false, true);
                c.BlockPack = i >= 1 && i <= packs;
                _plan.Enqueue(c);
            }

            _patternLog.Add((x + _plan.Count, pattern.Name));
            if (_patternLog.Count > 64) _patternLog.RemoveAt(0);
            foreach (var c in _scratch) _plan.Enqueue(c);
            _height = heightAfter;
            Ledger -= cost;
            if (PlanLog.Count < 4096) PlanLog.Add((x + recovery, pattern.Name, cost, Ledger, pack));
        }

        LevelPatterns.Pattern PickPattern(float metres, bool challenge)
        {
            if (_scripted.Count > 0) return _scripted.Dequeue();
            var all = LevelPatterns.All;
            int highestTier = 0;
            foreach (var p in all)
                if (p.MinMetres <= metres) highestTier = Mathf.Max(highestTier, p.MinMetres);

            int pressure = challenge && _rng.NextDouble() < ChallengePressure(metres) ? 1 : 0;
            float total = 0f;
            for (int pass = 0; pass < 2; pass++)
            {
                double roll = _rng.NextDouble() * total;
                foreach (var p in all)
                {
                    if (!Eligible(p, metres, challenge, pressure)) continue;
                    float w = p.Weight * (p.MinMetres == highestTier && highestTier > 0 ? 1.6f : 1f);
                    if (pass == 0) total += w;
                    else
                    {
                        roll -= w;
                        if (roll <= 0) return p;
                    }
                }
            }
            return all[0];
        }

        bool Eligible(LevelPatterns.Pattern p, float metres, bool challenge, int pressure)
        {
            if (p.MinMetres > metres || p == _lastPattern) return false;
            if (!challenge) return true;
            if (!p.Challenge) return false;
            // In the challenge, only offer what a perfect player can pay for, except for occasional
            // over-budget patterns late on (see ChallengePressure)
            int allowance = Mathf.FloorToInt(Ledger) + pressure;
            return EstimateCost(p) <= allowance;
        }

        /// <summary>Chance a challenge pattern may cost one block more than a perfect player holds.</summary>
        static float ChallengePressure(float metres) => Mathf.Lerp(0f, 0.25f, Mathf.InverseLerp(800f, 2000f, metres));

        static int EstimateCost(LevelPatterns.Pattern p)
        {
            int cost = 0;
            foreach (var t in LevelPatterns.Parse(p.Tokens))
            {
                switch (t.Op)
                {
                    case 'G': case 'S': cost += t.A; break;
                    case 'W': case 'T': cost += Mathf.Max(0, t.A - 1); break;
                    case 'E': cost += 1; break;
                }
            }
            return cost;
        }

        /// <summary>Appends the pattern's columns to scratch, updating the running height. Returns block cost.</summary>
        int Emit(LevelPatterns.Pattern pattern, float d)
        {
            int cost = 0;
            foreach (var t in LevelPatterns.Parse(pattern.Tokens))
            {
                switch (t.Op)
                {
                    case 'F':
                        for (int i = 0; i < t.A; i++) _scratch.Add(FlatColumn(false, true));
                        break;
                    case 'C':
                        for (int i = 0; i < t.A; i++) _scratch.Add(FlatColumn(i > 0 && i < t.A - 1, false));
                        break;
                    case 'G':
                        for (int i = 0; i < t.A; i++) _scratch.Add(new Column { Gap = true, Height = _height, CoinRow = _height });
                        cost += t.A;
                        break;
                    case 'S':
                        for (int i = 0; i < t.A; i++) _scratch.Add(new Column { Height = _height, Spikes = true, CoinRow = NoCoin });
                        cost += t.A;
                        break;
                    case 'U':
                        _height = _height >= MaxHeight ? _height - 1 : _height + 1;
                        _scratch.Add(FlatColumn(false, false));
                        break;
                    case 'D':
                        _height = Mathf.Max(MinHeight, _height - t.A);
                        _scratch.Add(FlatColumn(false, false));
                        break;
                    case 'T':
                    {
                        int rise = Mathf.Min(t.A, MaxHeight - _height);
                        if (rise <= 0)
                        {
                            _height = Mathf.Max(MinHeight, _height - t.A);   // too high already: drop instead
                            _scratch.Add(FlatColumn(false, false));
                            break;
                        }
                        _height += rise;
                        cost += Mathf.Max(0, rise - 1);
                        _scratch.Add(FlatColumn(false, false));
                        break;
                    }
                    case 'W':
                    {
                        int top = Mathf.Min(_height + t.A, MaxHeight + 2);
                        int length = Mathf.Max(1, t.B);
                        cost += Mathf.Max(0, top - _height - 1);
                        for (int i = 0; i < length; i++)
                            _scratch.Add(new Column { Height = top, CoinRow = i >= 1 && i < length - 1 ? top : NoCoin });
                        break;
                    }
                    case 'E':
                    {
                        int span = Mathf.Max(3, t.A);
                        for (int i = 0; i < span; i++)
                        {
                            var c = FlatColumn(false, false);
                            if (i == 0) c.EnemySpan = span;
                            _scratch.Add(c);
                        }
                        cost += 1;
                        break;
                    }
                    case 'L':
                        for (int i = 0; i < ElevatorWidth; i++)
                            _scratch.Add(new Column { Gap = true, Height = _height, CoinRow = NoCoin, Elevator = i == 0 ? ElevatorWidth : 0 });
                        break;
                }
            }
            return cost;
        }

        /// <summary>Minimum blocks a perfect player should keep in hand after the next pattern.</summary>
        float Reserve(float metres)
        {
            float t = Mathf.Clamp01(metres / _cfg.reserveRampMetres);
            return Mathf.Lerp(_cfg.reserveStart, _cfg.reserveLate, t);
        }

        /// <summary>Chance a needed pack is withheld: zero early, ramping up late so runs eventually end.</summary>
        float PackSkipChance(float metres)
        {
            if (metres <= _cfg.scarcityStartMetres) return 0f;
            float t = Mathf.InverseLerp(_cfg.scarcityStartMetres, _cfg.scarcityFullMetres, metres);
            return Mathf.Lerp(0f, _cfg.maxPackSkipChance, t);
        }

        Column FlatColumn(bool coin, bool decor) =>
            new Column { Height = _height, CoinRow = coin ? _height : NoCoin, AllowDecor = decor };

        SpawnRequest Decor(SpawnKind kind, int x, int height, int variant) => new SpawnRequest
        {
            Kind = kind,
            Column = x,
            Variant = variant,
            Position = new Vector2(x + 0.5f + (float)(_rng.NextDouble() - 0.5) * 0.4f, height)
        };

        int Range(int minInclusive, int maxExclusive) =>
            maxExclusive <= minInclusive ? minInclusive : _rng.Next(minInclusive, maxExclusive);
    }
}
