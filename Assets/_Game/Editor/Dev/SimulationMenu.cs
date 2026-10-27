using System.Collections.Generic;
using System.Linq;
using System.Text;
using OneBlock.Core;
using OneBlock.DevTools;
using UnityEditor;
using UnityEngine;

namespace OneBlock.EditorTools.Dev
{
    /// <summary>Runs batches of headless simulations and prints distance/economy statistics.</summary>
    public static class SimulationMenu
    {
        public const int DefaultSeeds = 60;

        [MenuItem("One Block/Dev/Simulate Economy (Endless)", priority = 100)]
        static void Endless() => Debug.Log(Report(GameMode.Endless, DefaultSeeds));

        [MenuItem("One Block/Dev/Simulate Economy (One Block Challenge)", priority = 101)]
        static void Challenge() => Debug.Log(Report(GameMode.OneBlockChallenge, DefaultSeeds));

        public static string Report(GameMode mode, int seeds)
        {
            var cfg = ProjectSetup.EnsureConfig();
            var sb = new StringBuilder();
            sb.AppendLine($"=== One Block simulation: {mode}, {seeds} seeds ===");
            foreach (var profile in new[] { BotPolicy.Profile.Perfect, BotPolicy.Profile.Human })
            {
                var results = new List<LevelSimulator.Result>();
                for (int s = 1; s <= seeds; s++) results.Add(LevelSimulator.Run(cfg, mode, profile, s));
                sb.AppendLine(Summarise(profile.Name, results));
            }
            return sb.ToString();
        }

        public static string Summarise(string name, List<LevelSimulator.Result> results)
        {
            var d = results.Select(r => r.Metres).OrderBy(m => m).ToList();
            float P(float q) => d[Mathf.Clamp(Mathf.RoundToInt(q * (d.Count - 1)), 0, d.Count - 1)];
            var causes = results.GroupBy(r => r.Cause).OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key} {g.Count() * 100 / results.Count}%");
            int withBlocks = results.Count(r => r.Cause != DeathCause.None && r.BlocksLeft > 0);
            return $"[{name}] median {P(0.5f):0} m | p25 {P(0.25f):0} | p75 {P(0.75f):0} | p90 {P(0.9f):0} | max {d[^1]:0}" +
                   $" | ≥1000 m {results.Count(r => r.Metres >= 1000) * 100 / results.Count}%" +
                   $" | avg coins {results.Average(r => r.Coins):0} | avg packs {results.Average(r => r.PacksPlaced):0.0}" +
                   $" | deaths with blocks left {withBlocks} | causes: {string.Join(", ", causes)}" +
                   "\n    death spots: " + string.Join(", ", results.Where(r => r.Cause != DeathCause.None)
                       .GroupBy(r => $"{r.LastPattern}/{r.Cause}{(r.BlocksLeft > 0 ? "*" : "")}")
                       .OrderByDescending(g => g.Count()).Take(8).Select(g => $"{g.Key} x{g.Count()}"));
        }
    }
}
