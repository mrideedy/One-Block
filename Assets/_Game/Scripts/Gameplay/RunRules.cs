using OneBlock.Core;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>
    /// Run rules shared by <see cref="RunController"/> and the headless simulator, so tuning data from
    /// simulations matches what players actually experience.
    /// </summary>
    public static class RunRules
    {
        public const float StuckGrace = 0.8f;

        public static float Speed(GameConfig cfg, float metres) =>
            Mathf.Min(cfg.startSpeed + metres / 100f * cfg.speedPerHundredMetres, cfg.maxSpeed);

        public static int StartingBlocks(GameConfig cfg, GameMode mode) =>
            mode == GameMode.OneBlockChallenge ? cfg.challengeStartingBlocks : cfg.startingBlocks;

        /// <summary>Pressed against a tall wall on solid ground with nothing to build: the run can't continue.</summary>
        public static bool IsStuck(PlayerController player, BlockBuilder builder) =>
            !player.IsDead && builder.Blocks == 0 && player.Grounded && !player.OnPlatform &&
            player.IsBlocked && player.WallHeight >= 2;

        public static float Metres(GameConfig cfg, PlayerController player, float startX) =>
            Mathf.Max(0f, (player.Position.x - startX) * cfg.metresPerCell);
    }
}
