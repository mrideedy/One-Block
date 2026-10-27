using System.Collections.Generic;
using OneBlock.Gameplay;
using OneBlock.Save;
using UnityEngine;

namespace OneBlock.Core
{
    /// <summary>
    /// Feeds in-run gameplay events into mission progress and announces completions as they happen.
    /// Whole-run missions (runs played, daily played) are reported by <see cref="GameManager"/> when a run is recorded.
    /// </summary>
    public class MissionTracker : MonoBehaviour
    {
        readonly List<MissionState> _completed = new();

        void OnEnable()
        {
            GameEvents.DistanceChanged += OnDistance;
            GameEvents.RunCoinsChanged += OnRunCoins;
            GameEvents.CoinCollected += OnCoin;
            GameEvents.EnemySquashed += OnSquash;
            GameEvents.BlockBuilt += OnBlockBuilt;
            GameEvents.PackCollected += OnPacks;
        }

        void OnDisable()
        {
            GameEvents.DistanceChanged -= OnDistance;
            GameEvents.RunCoinsChanged -= OnRunCoins;
            GameEvents.CoinCollected -= OnCoin;
            GameEvents.EnemySquashed -= OnSquash;
            GameEvents.BlockBuilt -= OnBlockBuilt;
            GameEvents.PackCollected -= OnPacks;
        }

        static bool Running => GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;

        void OnDistance(float metres)
        {
            if (!Running) return;
            int m = Mathf.FloorToInt(metres);
            Report(MissionType.RunDistance, m);
            if (GameManager.Instance.CurrentMode == GameMode.OneBlockChallenge) Report(MissionType.ChallengeDistance, m);
        }

        void OnRunCoins(int coins)
        {
            if (Running) Report(MissionType.RunCoins, coins);
        }

        void OnCoin() => Report(MissionType.TotalCoins, 1);
        void OnSquash() => Report(MissionType.Squash, 1);
        void OnPacks(int count) => Report(MissionType.Packs, count);

        void OnBlockBuilt(BuildKind kind)
        {
            Report(MissionType.BlocksPlaced, 1);
            if (kind == BuildKind.Bridge || kind == BuildKind.Rescue) Report(MissionType.Bridge, 1);
            else if (kind == BuildKind.Cover) Report(MissionType.Cover, 1);
        }

        void Report(MissionType type, int amount)
        {
            var data = SaveSystem.Data;
            bool any = false;
            foreach (var m in data.missions) if (m.type == type && !m.IsComplete) any = true;
            if (!any) return;

            _completed.Clear();
            Missions.Report(data, type, amount, _completed);
            SaveSystem.MarkDirty();
            foreach (var m in _completed) GameEvents.RaiseMissionCompleted(m);
            if (_completed.Count > 0) GameEvents.RaiseMissionsChanged();
        }
    }
}
