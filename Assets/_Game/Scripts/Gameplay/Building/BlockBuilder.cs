using System;
using OneBlock.Core;
using UnityEngine;

namespace OneBlock.Gameplay
{
    public enum BuildKind
    {
        None,
        Bridge,  // fills the support cell of a gap ahead
        Cover,   // replaces spikes on the walking row
        Step,    // a block on the walking row that the player hops onto
        Pillar,  // blocked by a tall wall: block goes under the player, who pops up
        Rescue,  // player just dropped into a gap: block catches them
        Crush    // drops onto an enemy's cell, squashing it
    }

    public enum BuildFailReason
    {
        NoBlocks,
        NoTarget
    }

    public readonly struct BuildTarget
    {
        public readonly BuildKind Kind;
        public readonly Vector2Int Cell;

        public BuildTarget(BuildKind kind, Vector2Int cell)
        {
            Kind = kind;
            Cell = cell;
        }

        public bool IsValid => Kind != BuildKind.None;
        public static BuildTarget None => new BuildTarget(BuildKind.None, default);
    }

    /// <summary>
    /// One-tap block placement. The rule, in priority order, scanning from the player's column up to
    /// <c>buildLookahead</c> columns ahead:
    ///  1. Danger first: fill the nearest pit, cover spikes or squash an enemy on the walking row.
    ///  2. Otherwise place a step on the walking row in the first column not overlapping the player.
    ///  3. If that column is a wall 2+ cells tall, build a pillar under the player instead.
    /// The same search drives the dashed preview, so what you see is what a tap does.
    /// </summary>
    public class BlockBuilder : MonoBehaviour
    {
        [SerializeField] SpriteRenderer preview;
        [SerializeField] Sprite previewSprite;
        [SerializeField] Sprite previewBlockedSprite;

        GameConfig _cfg;
        WorldGrid _grid;
        DynamicWorld _dynamic;
        PlayerController _player;

        public int Blocks { get; private set; }
        public int BlocksUsed { get; private set; }

        public event Action<int> BlocksChanged;
        public event Action<BuildTarget> Built;
        public event Action<BuildFailReason> BuildFailed;
        public event Action<Enemy> EnemySquashed;

        public void Init(GameConfig cfg, WorldGrid grid, DynamicWorld dynamic, PlayerController player)
        {
            _cfg = cfg;
            _grid = grid;
            _dynamic = dynamic;
            _player = player;
        }

        public void SetBlocks(int count)
        {
            Blocks = Mathf.Max(0, count);
            BlocksUsed = 0;
            BlocksChanged?.Invoke(Blocks);
        }

        public void AddBlocks(int count)
        {
            Blocks += count;
            BlocksChanged?.Invoke(Blocks);
        }

        public void SetPreviewVisible(bool visible)
        {
            if (preview != null) preview.enabled = visible;
        }

        /// <summary>Updates the placement preview to the cell a tap would use right now.</summary>
        public void TickPreview()
        {
            if (preview == null) return;
            var target = FindTarget();
            if (!target.IsValid || _player.IsDead)
            {
                preview.enabled = false;
                return;
            }
            preview.enabled = true;
            preview.sprite = Blocks > 0 ? previewSprite : previewBlockedSprite;
            preview.transform.position = new Vector3(target.Cell.x + 0.5f, target.Cell.y + 0.5f, 0f);
        }

        public bool TryBuild(out BuildFailReason reason)
        {
            reason = BuildFailReason.NoTarget;
            var target = FindTarget();
            if (!target.IsValid) return false;
            if (Blocks <= 0)
            {
                reason = BuildFailReason.NoBlocks;
                BuildFailed?.Invoke(reason);
                return false;
            }

            var c = target.Cell;
            if (target.Kind == BuildKind.Crush)
            {
                var enemy = _dynamic?.EnemyInCell(c.x, c.y);
                if (enemy != null)
                {
                    _dynamic.Squash(enemy);
                    EnemySquashed?.Invoke(enemy);
                }
            }
            _grid.Set(c.x, c.y, CellType.Block);
            switch (target.Kind)
            {
                case BuildKind.Pillar:
                    _player.PlaceOnSurface(c.y + 1, 5f);
                    break;
                case BuildKind.Rescue:
                    _player.PlaceOnSurface(c.y + 1, 0f);
                    break;
            }

            Blocks--;
            BlocksUsed++;
            BlocksChanged?.Invoke(Blocks);
            Built?.Invoke(target);
            return true;
        }

        public void NotifyFailed(BuildFailReason reason) => BuildFailed?.Invoke(reason);

        public BuildTarget FindTarget()
        {
            if (_player == null || _player.IsDead) return BuildTarget.None;

            Vector2 p = _player.Position;
            int row = _player.GroundRow;           // walking row (feet level)
            bool rescue = false;

            if (!_player.Grounded && !_player.IsHopping)
            {
                if (p.y < row - _cfg.rescueDepth) return BuildTarget.None;  // fell too far
                rescue = p.y < row - 0.01f && _player.Velocity.y <= 0f;
            }

            int cx = Mathf.FloorToInt(p.x);
            int last = cx + _cfg.buildLookahead;

            // Hopping onto a step or popping up off a pillar: GroundRow is the row being landed on, so
            // dangers ahead can already be handled. Any other airborne state (dropping off a ledge) has a
            // stale GroundRow, so the only valid build is catching the player over a pit.
            bool rising = _player.IsHopping || p.y >= row - 0.01f;
            if (!_player.Grounded && !rising)
            {
                if (!rescue) return BuildTarget.None;
                for (int c = Mathf.FloorToInt(p.x - _player.HalfWidth); c <= Mathf.FloorToInt(p.x + _player.HalfWidth); c++)
                {
                    var cell = new Vector2Int(c, row - 1);
                    if (!_grid.IsSolid(c, row - 1) && IsPit(c, row) && OverlapsPlayer(cell) && CanRescueInto(cell))
                        return new BuildTarget(BuildKind.Rescue, cell);
                }
                return BuildTarget.None;
            }

            // 1. Dangers: pits, spikes and enemies, nearest first. The scan follows the path the player
            // will actually walk: it steps down through 1-high drops and stops at walls and deeper drops.
            int scanRow = row;
            for (int c = cx; c <= last; c++)
            {
                if (!_grid.IsSolid(c, scanRow - 1))
                {
                    // Spikes one row down (e.g. walking on a block that covered the previous spike)
                    if (_grid.IsHazard(c, scanRow - 1))
                    {
                        var below = new Vector2Int(c, scanRow - 1);
                        if (!OverlapsPlayer(below)) return new BuildTarget(BuildKind.Cover, below);
                        break;
                    }
                    if (IsPit(c, scanRow))
                    {
                        var cell = new Vector2Int(c, scanRow - 1);
                        if (OverlapsPlayer(cell)) return BuildTarget.None;
                        return new BuildTarget(BuildKind.Bridge, cell);
                    }
                    // A free drop. Keep looking one row lower if it's 1 deep; deeper drops end the scan.
                    if (!_grid.IsSolid(c, scanRow - 2)) break;
                    scanRow--;
                    if (_dynamic?.EnemyInCell(c, scanRow) != null && !OverlapsPlayer(new Vector2Int(c, scanRow)))
                        return new BuildTarget(BuildKind.Crush, new Vector2Int(c, scanRow));
                    continue;
                }
                if (_grid.IsSolid(c, scanRow)) break;
                var walkCell = new Vector2Int(c, scanRow);
                if (OverlapsPlayer(walkCell)) continue;
                if (_grid.IsHazard(c, scanRow)) return new BuildTarget(BuildKind.Cover, walkCell);
                if (_dynamic?.EnemyInCell(c, scanRow) != null) return new BuildTarget(BuildKind.Crush, walkCell);
            }

            if (_player.OnPlatform || !_player.Grounded) return BuildTarget.None;

            // 2/3. Step on the walking row, or a pillar when right up against a tall wall
            for (int c = cx; c <= last; c++)
            {
                var cell = new Vector2Int(c, row);
                if (OverlapsPlayer(cell)) continue;
                if (!_grid.IsSolid(c, row - 1)) return BuildTarget.None;   // no floating steps over drops
                if (_grid.IsSolid(c, row))
                {
                    int height = 0;
                    while (height < 8 && _grid.IsSolid(c, row + height)) height++;
                    // Only pillar from the column directly before the wall; from further back the player
                    // would pop up, step off the block and need a second one.
                    bool adjacent = c == cx + 1;
                    return height >= 2 && adjacent && CanPillar(cx, row)
                        ? new BuildTarget(BuildKind.Pillar, new Vector2Int(cx, row))
                        : BuildTarget.None;
                }
                return new BuildTarget(BuildKind.Step, cell);
            }
            return BuildTarget.None;
        }

        /// <summary>True when nothing solid lies below the walking row within falling-death range.</summary>
        bool IsPit(int c, int row)
        {
            if (_dynamic != null && _dynamic.IsElevatorColumn(c)) return false;   // ride it instead
            int depth = Mathf.CeilToInt(_cfg.killDepth);
            for (int y = row - 1; y >= row - depth; y--)
                if (_grid.IsSolid(c, y)) return false;
            return true;
        }

        bool OverlapsPlayer(Vector2Int cell)
        {
            Rect body = _player.Bounds;
            const float e = 0.02f;
            return body.xMax > cell.x + e && body.xMin < cell.x + 1 - e &&
                   body.yMax > cell.y + e && body.yMin < cell.y + 1 - e;
        }

        /// <summary>The player will be lifted to stand on the rescue block, so the row above it must be free.</summary>
        bool CanRescueInto(Vector2Int cell)
        {
            Rect body = _player.Bounds;
            int c0 = Mathf.FloorToInt(body.xMin + 0.01f), c1 = Mathf.FloorToInt(body.xMax - 0.01f);
            for (int c = c0; c <= c1; c++)
                if (_grid.IsSolid(c, cell.y + 1)) return false;
            return true;
        }

        bool CanPillar(int cx, int row)
        {
            if (!_grid.IsSolid(cx, row - 1) || _grid.Get(cx, row) != CellType.Empty) return false;
            // Headroom for the player one row up across every column they overlap
            Rect body = _player.Bounds;
            int c0 = Mathf.FloorToInt(body.xMin + 0.01f), c1 = Mathf.FloorToInt(body.xMax - 0.01f);
            for (int c = c0; c <= c1; c++)
                if (_grid.IsSolid(c, row + 1)) return false;
            return true;
        }
    }
}
