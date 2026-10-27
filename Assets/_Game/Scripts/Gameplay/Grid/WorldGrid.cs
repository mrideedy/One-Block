using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneBlock.Gameplay
{
    public enum CellType : byte
    {
        Empty = 0,
        Ground = 1,
        Block = 2,
        Spikes = 3
    }

    /// <summary>
    /// The level as a grid of 1x1 cells, stored per column. Cell (x, y) covers world [x, x+1) × [y, y+1).
    /// Rows outside [MinRow, MaxRow] and columns not generated yet read as Empty (pits are bottomless).
    /// </summary>
    public sealed class WorldGrid
    {
        public const int MinRow = -12;
        public const int MaxRow = 30;
        const int Rows = MaxRow - MinRow + 1;

        readonly Dictionary<int, CellType[]> _columns = new();
        readonly Stack<CellType[]> _spare = new();

        /// <summary>(x, y, oldType, newType). Only raised for notifying writes.</summary>
        public event Action<int, int, CellType, CellType> CellChanged;

        public int FirstColumn { get; private set; } = int.MaxValue;
        public int LastColumn { get; private set; } = int.MinValue;

        public CellType Get(int x, int y)
        {
            if (y < MinRow || y > MaxRow) return CellType.Empty;
            return _columns.TryGetValue(x, out var col) ? col[y - MinRow] : CellType.Empty;
        }

        public bool IsSolid(int x, int y)
        {
            var t = Get(x, y);
            return t == CellType.Ground || t == CellType.Block;
        }

        public bool IsHazard(int x, int y) => Get(x, y) == CellType.Spikes;

        public bool HasColumn(int x) => _columns.ContainsKey(x);

        public void Set(int x, int y, CellType type, bool notify = true)
        {
            if (y < MinRow || y > MaxRow) return;
            if (!_columns.TryGetValue(x, out var col))
            {
                col = _spare.Count > 0 ? _spare.Pop() : new CellType[Rows];
                _columns[x] = col;
                FirstColumn = Mathf.Min(FirstColumn, x);
                LastColumn = Mathf.Max(LastColumn, x);
            }
            var old = col[y - MinRow];
            if (old == type) return;
            col[y - MinRow] = type;
            if (notify) CellChanged?.Invoke(x, y, old, type);
        }

        /// <summary>Registers an all-empty column (a gap) so streaming knows it was generated.</summary>
        public void EnsureColumn(int x)
        {
            if (_columns.ContainsKey(x)) return;
            var col = _spare.Count > 0 ? _spare.Pop() : new CellType[Rows];
            _columns[x] = col;
            FirstColumn = Mathf.Min(FirstColumn, x);
            LastColumn = Mathf.Max(LastColumn, x);
        }

        /// <summary>Highest Ground row in the column, or int.MinValue for a gap.</summary>
        public int TopGroundRow(int x)
        {
            if (!_columns.TryGetValue(x, out var col)) return int.MinValue;
            for (int i = Rows - 1; i >= 0; i--)
                if (col[i] == CellType.Ground) return i + MinRow;
            return int.MinValue;
        }

        public void RemoveColumn(int x)
        {
            if (!_columns.TryGetValue(x, out var col)) return;
            Array.Clear(col, 0, col.Length);
            _spare.Push(col);
            _columns.Remove(x);
            if (x == FirstColumn) FirstColumn = x + 1;
        }

        public void Clear()
        {
            foreach (var col in _columns.Values)
            {
                Array.Clear(col, 0, col.Length);
                _spare.Push(col);
            }
            _columns.Clear();
            FirstColumn = int.MaxValue;
            LastColumn = int.MinValue;
        }
    }
}
