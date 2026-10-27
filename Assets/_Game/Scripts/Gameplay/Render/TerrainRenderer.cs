using System.Collections.Generic;
using OneBlock.Data;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>
    /// Draws the grid: ground tiles with edge variants, placed blocks (with a pop-in), themed hazards
    /// (animated), decor and milestone flags. Everything is pooled and owned per column so streaming can release it.
    /// </summary>
    public class TerrainRenderer : MonoBehaviour
    {
        public const int OrderDecor = -5;
        public const int OrderGround = 0;
        public const int OrderBlock = 5;
        public const int OrderProps = 6;

        /// <summary>How many ground rows below the surface get drawn.</summary>
        const int DepthRows = 12;
        const float PopDuration = 0.2f;
        const float HazardFps = 5f;

        WorldGrid _grid;
        WorldTheme _theme;
        BlockSkin _blockSkin;
        Sprite _spikes;
        Sprite[] _hazardFrames;
        Sprite _flag;

        SpritePool _tilePool, _blockPool, _propPool;
        readonly Dictionary<long, SpriteRenderer> _tiles = new();
        readonly Dictionary<long, SpriteRenderer> _cellProps = new();   // blocks + hazards keyed by cell
        readonly HashSet<SpriteRenderer> _hazards = new();
        readonly Dictionary<int, List<SpriteRenderer>> _columnDecor = new();
        readonly List<(SpriteRenderer sr, float t)> _pops = new();

        static long Key(int x, int y) => ((long)x << 32) | (uint)y;

        public void Init(WorldGrid grid, WorldTheme theme, BlockSkin blockSkin, Sprite spikes, Sprite flag)
        {
            _grid = grid;
            _theme = theme;
            _blockSkin = blockSkin;
            _spikes = spikes;
            _flag = flag;
            _hazardFrames = FramesOf(theme);

            _tilePool = new SpritePool(transform, "Tile", 256);
            _blockPool = new SpritePool(transform, "Block", 16);
            _propPool = new SpritePool(transform, "Prop", 32);
            _grid.CellChanged += OnCellChanged;
        }

        void OnDestroy()
        {
            if (_grid != null) _grid.CellChanged -= OnCellChanged;
        }

        /// <summary>Swaps ground/decor art. Existing tiles keep their sprites until the world is reset; hazards update now.</summary>
        public void SetTheme(WorldTheme theme)
        {
            _theme = theme;
            _hazardFrames = FramesOf(theme);
            foreach (var sr in _hazards) sr.sprite = HazardFrame(sr.transform.position.x);
        }

        Sprite[] FramesOf(WorldTheme theme) =>
            theme != null && theme.hazardFrames != null && theme.hazardFrames.Length > 0 && theme.hazardFrames[0] != null
                ? theme.hazardFrames
                : new[] { _spikes };

        /// <summary>Neighbouring hazards are offset in phase so a row of them doesn't pulse in lockstep.</summary>
        Sprite HazardFrame(float x)
        {
            int i = Mathf.FloorToInt(Time.time * HazardFps + x * 1.37f);
            return _hazardFrames[((i % _hazardFrames.Length) + _hazardFrames.Length) % _hazardFrames.Length];
        }

        public void SetBlockSkin(BlockSkin skin)
        {
            _blockSkin = skin;
            foreach (var kv in _cellProps)
            {
                int x = (int)(kv.Key >> 32), y = (int)(uint)kv.Key;
                if (_grid.Get(x, y) == CellType.Block) kv.Value.sprite = skin.sprite;
            }
        }

        // ------------------------------------------------------------------ columns

        /// <summary>Draws a generated column. Call once both neighbours exist so edges are correct.</summary>
        public void RenderColumn(int x)
        {
            int top = _grid.TopGroundRow(x);
            if (top != int.MinValue)
            {
                int bottom = Mathf.Max(WorldGrid.MinRow, top - DepthRows);
                for (int y = top; y >= bottom; y--)
                {
                    if (_grid.Get(x, y) != CellType.Ground) continue;
                    bool isTop = _grid.Get(x, y + 1) != CellType.Ground;
                    bool openL = _grid.Get(x - 1, y) != CellType.Ground;
                    bool openR = _grid.Get(x + 1, y) != CellType.Ground;
                    var sr = _tilePool.Get(_theme.GetTile(isTop, openL, openR), new Vector3(x, y, 0f), OrderGround);
                    _tiles[Key(x, y)] = sr;
                }
            }

            for (int y = WorldGrid.MinRow; y <= WorldGrid.MaxRow; y++)
            {
                var t = _grid.Get(x, y);
                if (t == CellType.Spikes || t == CellType.Block) ShowCellProp(x, y, t, pop: false);
            }
        }

        public void ClearColumn(int x)
        {
            for (int y = WorldGrid.MinRow; y <= WorldGrid.MaxRow; y++)
            {
                long k = Key(x, y);
                if (_tiles.Remove(k, out var tile)) _tilePool.Release(tile);
                if (_cellProps.Remove(k, out var prop)) ReleaseProp(prop);
            }
            if (_columnDecor.Remove(x, out var list))
            {
                foreach (var sr in list) _propPool.Release(sr);
            }
        }

        public void ClearAll()
        {
            foreach (var sr in _tiles.Values) _tilePool.Release(sr);
            foreach (var sr in _cellProps.Values) ReleaseProp(sr);
            foreach (var list in _columnDecor.Values)
                foreach (var sr in list) _propPool.Release(sr);
            _tiles.Clear();
            _cellProps.Clear();
            _hazards.Clear();
            _columnDecor.Clear();
            _pops.Clear();
        }

        public void SpawnDecor(SpawnRequest req)
        {
            Sprite sprite = null;
            int order = OrderDecor;
            float scale = 1f;
            switch (req.Kind)
            {
                case SpawnKind.Tree:
                    if (_theme.trees != null && _theme.trees.Length > 0) sprite = _theme.trees[req.Variant % _theme.trees.Length];
                    scale = 0.85f + (req.Variant % 2) * 0.15f;
                    break;
                case SpawnKind.Bush:
                    sprite = _theme.bush;
                    scale = 0.7f;
                    break;
                case SpawnKind.Tuft:
                    sprite = _theme.tuft;
                    order = OrderProps;
                    break;
                case SpawnKind.Flag:
                    sprite = _flag;
                    order = OrderProps;
                    break;
            }
            if (sprite == null) return;

            var sr = _propPool.Get(sprite, req.Position, order);
            sr.transform.localScale = Vector3.one * scale;
            if (!_columnDecor.TryGetValue(req.Column, out var list))
            {
                list = new List<SpriteRenderer>(2);
                _columnDecor[req.Column] = list;
            }
            list.Add(sr);
        }

        // ------------------------------------------------------------------ cell props

        void OnCellChanged(int x, int y, CellType oldType, CellType newType)
        {
            long k = Key(x, y);
            if (_cellProps.Remove(k, out var existing)) ReleaseProp(existing);
            if (newType == CellType.Block || newType == CellType.Spikes) ShowCellProp(x, y, newType, pop: true);
        }

        void ShowCellProp(int x, int y, CellType type, bool pop)
        {
            long k = Key(x, y);
            if (_cellProps.ContainsKey(k)) return;
            SpriteRenderer sr;
            if (type == CellType.Block)
                sr = _blockPool.Get(_blockSkin.sprite, new Vector3(x + 0.5f, y + 0.5f, 0f), OrderBlock);
            else
            {
                sr = _propPool.Get(HazardFrame(x + 0.5f), new Vector3(x + 0.5f, y, 0f), OrderProps);
                _hazards.Add(sr);
            }
            _cellProps[k] = sr;
            if (pop)
            {
                sr.transform.localScale = Vector3.one * 0.3f;
                _pops.Add((sr, 0f));
            }
        }

        void ReleaseProp(SpriteRenderer sr)
        {
            for (int i = _pops.Count - 1; i >= 0; i--)
                if (_pops[i].sr == sr) _pops.RemoveAt(i);
            if (_hazards.Remove(sr)) _propPool.Release(sr);
            else _blockPool.Release(sr);
        }

        void Update()
        {
            if (_hazardFrames != null && _hazardFrames.Length > 1)
                foreach (var sr in _hazards) sr.sprite = HazardFrame(sr.transform.position.x);

            for (int i = _pops.Count - 1; i >= 0; i--)
            {
                var (sr, t) = _pops[i];
                t += Time.deltaTime;
                float n = Mathf.Clamp01(t / PopDuration);
                sr.transform.localScale = Vector3.one * EaseOutBack(0.3f, 1f, n);
                if (n >= 1f) _pops.RemoveAt(i);
                else _pops[i] = (sr, t);
            }
        }

        static float EaseOutBack(float from, float to, float t)
        {
            const float c1 = 1.9f, c3 = c1 + 1f;
            float k = 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
            return Mathf.LerpUnclamped(from, to, k);
        }
    }
}
