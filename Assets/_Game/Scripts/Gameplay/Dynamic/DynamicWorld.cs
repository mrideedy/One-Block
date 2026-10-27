using System.Collections.Generic;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>A patrolling ground enemy. Touching it kills; a block dropped on its cell squashes it.</summary>
    public sealed class Enemy
    {
        public const float HalfWidth = 0.34f;
        public const float Height = 0.62f;

        public float X;
        public int Row;
        public int MinColumn;
        public int MaxColumn;
        public float Speed;
        public int Direction = -1;
        public bool Alive = true;
        public float DeadTime;
        public int SpawnColumn;

        public int Column => Mathf.FloorToInt(X);
        public Rect Bounds => new Rect(X - HalfWidth, Row, HalfWidth * 2f, Height);
    }

    /// <summary>
    /// An elevator filling a pit: a slab that bobs between <see cref="MinTop"/> and <see cref="MaxTop"/>.
    /// Arrive when it is level and you cross for free; otherwise you drop onto it and ride it up.
    /// </summary>
    public sealed class MovingPlatform
    {
        public const float Thickness = 0.44f;

        public int Column;      // leftmost lane column
        public int Width;       // lane width in columns
        public float MinTop;
        public float MaxTop;
        public float Period;
        public float Phase;
        public float Top;
        public float LastTop;

        public float XMin => Column;
        public float XMax => Column + Width;
        public Rect Bounds => new Rect(XMin, Top - Thickness, Width, Thickness);

        public void Evaluate(float time)
        {
            LastTop = Top;
            float t = 0.5f - 0.5f * Mathf.Cos((time / Period + Phase) * Mathf.PI * 2f);
            Top = Mathf.Lerp(MinTop, MaxTop, t);
        }
    }

    /// <summary>
    /// Moving level content (enemies, elevators) as plain data so gameplay, rendering and the headless
    /// simulator all share one implementation.
    /// </summary>
    public sealed class DynamicWorld
    {
        readonly WorldGrid _grid;
        readonly List<Enemy> _enemies = new();
        readonly List<MovingPlatform> _platforms = new();
        float _time;

        public IReadOnlyList<Enemy> Enemies => _enemies;
        public IReadOnlyList<MovingPlatform> Platforms => _platforms;

        public DynamicWorld(WorldGrid grid)
        {
            _grid = grid;
        }

        public void Spawn(SpawnRequest req)
        {
            switch (req.Kind)
            {
                case SpawnKind.Enemy:
                    _enemies.Add(new Enemy
                    {
                        X = req.Position.x,
                        Row = Mathf.RoundToInt(req.Position.y),
                        MinColumn = req.Column,
                        MaxColumn = req.Column + req.Span - 1,
                        Speed = req.Value,
                        SpawnColumn = req.Column
                    });
                    break;
                case SpawnKind.Elevator:
                {
                    int row = Mathf.RoundToInt(req.Position.y);
                    var p = new MovingPlatform
                    {
                        Column = req.Column,
                        Width = req.Span,
                        MaxTop = row,
                        MinTop = row - 2f,
                        Period = req.Value,
                        Phase = req.Column * 0.137f % 1f
                    };
                    p.Evaluate(_time);
                    p.LastTop = p.Top;
                    _platforms.Add(p);
                    break;
                }
            }
        }

        public void Tick(float dt)
        {
            _time += dt;
            foreach (var p in _platforms) p.Evaluate(_time);

            foreach (var e in _enemies)
            {
                if (!e.Alive)
                {
                    e.DeadTime += dt;
                    continue;
                }
                float next = e.X + e.Direction * e.Speed * dt;
                int aheadCol = Mathf.FloorToInt(next + e.Direction * Enemy.HalfWidth);
                bool leavesPatrol = aheadCol < e.MinColumn || aheadCol > e.MaxColumn;
                bool blocked = _grid.IsSolid(aheadCol, e.Row) || _grid.IsHazard(aheadCol, e.Row) || !_grid.IsSolid(aheadCol, e.Row - 1);
                if (leavesPatrol || blocked) e.Direction = -e.Direction;
                else e.X = next;
            }
        }

        /// <summary>Alive enemy whose centre is in the given cell, or null.</summary>
        public Enemy EnemyInCell(int column, int row)
        {
            foreach (var e in _enemies)
                if (e.Alive && e.Row == row && e.Column == column) return e;
            return null;
        }

        public bool TouchesEnemy(Rect body)
        {
            foreach (var e in _enemies)
            {
                if (!e.Alive) continue;
                Rect r = e.Bounds;
                r.xMin += 0.08f;
                r.xMax -= 0.08f;
                r.yMax -= 0.08f;
                if (r.Overlaps(body)) return true;
            }
            return false;
        }

        public void Squash(Enemy e)
        {
            e.Alive = false;
            e.DeadTime = 0f;
        }

        public bool IsElevatorColumn(int column)
        {
            foreach (var p in _platforms)
                if (column >= p.Column && column < p.Column + p.Width) return true;
            return false;
        }

        public void RemoveColumnsBefore(int column)
        {
            _enemies.RemoveAll(e => e.MaxColumn < column);
            _platforms.RemoveAll(p => p.Column + p.Width <= column);
        }

        public void Clear()
        {
            _enemies.Clear();
            _platforms.Clear();
            _time = 0f;
        }
    }
}
