using System.Collections.Generic;
using OneBlock.Core;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>
    /// Keeps the level generated a few columns beyond the right edge of the view and releases columns
    /// that scrolled off the left edge.
    /// </summary>
    public class LevelStreamer : MonoBehaviour
    {
        const int AheadMargin = 4;
        const int BehindMargin = 4;
        const int BackfillColumns = 12;

        WorldGrid _grid;
        TerrainRenderer _terrain;
        CollectibleSystem _collectibles;
        DynamicWorld _dynamic;
        LevelGenerator _generator;
        readonly List<SpawnRequest> _spawns = new();

        int _nextColumn;
        int _renderedUpTo;
        int _releasedUpTo;

        public LevelGenerator Generator => _generator;

        public void Init(GameConfig cfg, WorldGrid grid, DynamicWorld dynamic, TerrainRenderer terrain, CollectibleSystem collectibles)
        {
            _grid = grid;
            _dynamic = dynamic;
            _terrain = terrain;
            _collectibles = collectibles;
            _generator = new LevelGenerator(cfg);
        }

        public void ResetLevel(int seed, int startColumn, GameMode mode, bool tutorial = false)
        {
            _terrain.ClearAll();
            _collectibles.ClearAll();
            _dynamic.Clear();
            _grid.Clear();
            _generator.Reset(seed, startColumn, mode, tutorial);
            _nextColumn = startColumn - BackfillColumns;
            _renderedUpTo = _nextColumn - 1;
            _releasedUpTo = _nextColumn - 1;
        }

        public void Tick(float viewLeft, float viewRight)
        {
            int wantUpTo = Mathf.CeilToInt(viewRight) + AheadMargin;
            while (_nextColumn <= wantUpTo + 1)
            {
                _spawns.Clear();
                _generator.Generate(_nextColumn, _grid, _spawns);
                foreach (var s in _spawns)
                {
                    switch (s.Kind)
                    {
                        case SpawnKind.Coin:
                        case SpawnKind.BlockPack:
                            _collectibles.Spawn(s);
                            break;
                        case SpawnKind.Enemy:
                        case SpawnKind.Elevator:
                            _dynamic.Spawn(s);
                            break;
                        default:
                            _terrain.SpawnDecor(s);
                            break;
                    }
                }
                _nextColumn++;

                // A column's edge tiles depend on both neighbours, so render one column behind generation
                while (_renderedUpTo < _nextColumn - 2)
                {
                    _renderedUpTo++;
                    _terrain.RenderColumn(_renderedUpTo);
                }
            }

            int releaseBefore = Mathf.FloorToInt(viewLeft) - BehindMargin;
            if (releaseBefore > _releasedUpTo + 1)
            {
                for (int x = _releasedUpTo + 1; x < releaseBefore; x++)
                {
                    _terrain.ClearColumn(x);
                    _grid.RemoveColumn(x);
                }
                _collectibles.RemoveColumnsBefore(releaseBefore);
                _dynamic.RemoveColumnsBefore(releaseBefore);
                _releasedUpTo = releaseBefore - 1;
            }
        }
    }
}
