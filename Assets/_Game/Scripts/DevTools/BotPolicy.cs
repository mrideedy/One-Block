using OneBlock.Gameplay;
using UnityEngine;

namespace OneBlock.DevTools
{
    /// <summary>
    /// Decision-making for test bots. The perfect profile only spends blocks on real obstacles at the
    /// right moment; the human profile adds occasional wasted taps and late reactions.
    /// </summary>
    public sealed class BotPolicy
    {
        public sealed class Profile
        {
            public string Name = "perfect";
            public int TapAtColumnsAhead = 1;
            [Tooltip("Stray taps per minute that waste a block on an unnecessary step.")]
            public float WasteTapsPerMinute;
            [Tooltip("Chance to react too late to a pit, spikes or an enemy.")]
            public float MissChance;
            [Tooltip("Extra miss chance per 1 unit of speed above the starting speed.")]
            public float MissChancePerSpeed;

            public static Profile Perfect => new Profile();

            public static Profile Human => new Profile
            {
                Name = "human",
                WasteTapsPerMinute = 3f,
                MissChance = 0.025f,
                MissChancePerSpeed = 0.012f
            };
        }

        readonly Profile _profile;
        readonly System.Random _rng;
        Vector2Int _decidedCell = new(int.MinValue, 0);
        bool _decidedMiss;

        public BotPolicy(Profile profile, int seed)
        {
            _profile = profile;
            _rng = new System.Random(seed);
        }

        /// <summary>Returns true when the bot wants to tap this frame.</summary>
        public bool ShouldTap(BlockBuilder builder, PlayerController player, float startSpeed, float dt)
        {
            if (player.IsDead) return false;

            // Stray taps
            if (_profile.WasteTapsPerMinute > 0f && _rng.NextDouble() < _profile.WasteTapsPerMinute / 60f * dt)
                return true;

            var t = builder.FindTarget();
            if (!t.IsValid) return false;
            int ahead = t.Cell.x - Mathf.FloorToInt(player.Position.x);

            bool wants = t.Kind switch
            {
                BuildKind.Bridge => ahead <= _profile.TapAtColumnsAhead,
                BuildKind.Cover => ahead <= _profile.TapAtColumnsAhead,
                BuildKind.Crush => ahead <= _profile.TapAtColumnsAhead + 1,   // enemies close in fast
                BuildKind.Rescue => true,
                BuildKind.Pillar => true,
                _ => false
            };
            if (!wants) return false;

            // Decide once per obstacle whether this attempt is a miss (a human reacting too late)
            if (t.Cell != _decidedCell && t.Kind != BuildKind.Rescue)
            {
                _decidedCell = t.Cell;
                float chance = _profile.MissChance + Mathf.Max(0f, player.Speed - startSpeed) * _profile.MissChancePerSpeed;
                _decidedMiss = t.Kind != BuildKind.Pillar && _rng.NextDouble() < chance;
            }
            if (t.Kind == BuildKind.Rescue) return !_decidedMiss;
            return !_decidedMiss;
        }
    }
}
