using System;
using OneBlock.Core;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>
    /// Grid-aware kinematic runner. Position is the centre of the feet. No Rigidbody: every solid is
    /// an axis-aligned cell, so resolving against the grid is exact and deterministic.
    /// Ticked by <see cref="RunController"/>.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        const float Skin = 0.002f;
        const float MaxSubstep = 0.2f;

        GameConfig _cfg;
        WorldGrid _grid;
        DynamicWorld _dynamic;
        MovingPlatform _platform;
        Vector2 _pos;
        Vector2 _vel;

        public Vector2 Position => _pos;
        public Vector2 Velocity => _vel;
        public float Speed { get; set; }
        public bool Grounded { get; private set; }
        /// <summary>Row of the surface the player last stood on (feet y when grounded).</summary>
        public int GroundRow { get; private set; }
        public bool IsBlocked { get; private set; }
        /// <summary>Consecutive solid cells in front of the feet when blocked.</summary>
        public int WallHeight { get; private set; }
        public bool OnPlatform => _platform != null;
        /// <summary>In the air on an automatic hop; GroundRow already points at the row being hopped onto.</summary>
        public bool IsHopping { get; private set; }
        public bool IsDead { get; private set; }
        public DeathCause DeathCause { get; private set; }

        public float HalfWidth => _cfg.playerHalfWidth;
        public float Height => _cfg.playerHeight;
        public Rect Bounds => new Rect(_pos.x - HalfWidth, _pos.y, HalfWidth * 2f, Height);

        /// <summary>Raised when landing, with the downward speed at impact.</summary>
        public event Action<float> Landed;
        public event Action Hopped;
        public event Action<DeathCause> Died;

        public void Init(GameConfig cfg, WorldGrid grid, DynamicWorld dynamic)
        {
            _cfg = cfg;
            _grid = grid;
            _dynamic = dynamic;
        }

        public void ResetAt(Vector2 feet)
        {
            _pos = feet;
            _vel = Vector2.zero;
            Speed = 0f;
            Grounded = true;
            GroundRow = Mathf.RoundToInt(feet.y);
            IsBlocked = false;
            WallHeight = 0;
            IsDead = false;
            DeathCause = DeathCause.None;
            _platform = null;
            IsHopping = false;
            transform.position = _pos;
        }

        public void Tick(float dt)
        {
            if (IsDead)
            {
                if (DeathCause == DeathCause.OutOfBlocks) return;   // stays standing at the wall
                _vel.y = Mathf.Max(_vel.y - _cfg.gravity * dt, -_cfg.maxFallSpeed);
                _pos += _vel * dt;
                transform.position = _pos;
                return;
            }

            // Ride the elevator we are standing on (platforms have already moved this frame)
            if (_platform != null) _pos.y = _platform.Top;

            float maxMove = Mathf.Max(Speed, Mathf.Abs(_vel.y) + _cfg.gravity * dt) * dt;
            int steps = Mathf.Max(1, Mathf.CeilToInt(maxMove / MaxSubstep));
            float h = dt / steps;
            bool wasGrounded = Grounded;
            float impactSpeed = 0f;

            for (int i = 0; i < steps; i++)
            {
                MoveHorizontal(Speed * h);
                _vel.y = Mathf.Max(_vel.y - _cfg.gravity * h, -_cfg.maxFallSpeed);
                float before = _vel.y;
                MoveVertical(_vel.y * h);
                if (Grounded && before < 0f) impactSpeed = Mathf.Max(impactSpeed, -before);
            }

            // Automatic hop onto 1-high steps (the wall in front has free space above it)
            if (Grounded && IsBlocked && WallHeight == 1)
            {
                _vel.y = _cfg.hopVelocity;
                Grounded = false;
                IsHopping = true;
                GroundRow += 1;
                Hopped?.Invoke();
            }

            if (!wasGrounded && Grounded && impactSpeed > 2f) Landed?.Invoke(impactSpeed);

            if (TouchesHazard()) Kill(DeathCause.Spikes);
            else if (_dynamic != null && _dynamic.TouchesEnemy(Bounds)) Kill(DeathCause.Enemy);
            else if (_pos.y < GroundRow - _cfg.killDepth) Kill(DeathCause.Fell);

            transform.position = _pos;
        }

        void MoveHorizontal(float dx)
        {
            IsBlocked = false;
            WallHeight = 0;
            if (dx <= 0f)
            {
                CheckWallContact();
                return;
            }

            float newX = _pos.x + dx;
            int col = Mathf.FloorToInt(newX + HalfWidth);
            int rowMin = Mathf.FloorToInt(_pos.y + 0.01f);
            int rowMax = Mathf.FloorToInt(_pos.y + Height - 0.01f);

            for (int r = rowMin; r <= rowMax; r++)
            {
                if (!_grid.IsSolid(col, r)) continue;
                float limit = col - HalfWidth - Skin;
                // Never get pushed backwards more than a hair (protects against rounding when embedded)
                newX = Mathf.Max(Mathf.Min(newX, limit), _pos.x - 0.02f);
                IsBlocked = true;
                WallHeight = CountWall(col, rowMin);
                break;
            }

            if (!IsBlocked && _dynamic != null)
            {
                // Elevator slab edges act like walls when they stick up above our feet
                foreach (var p in _dynamic.Platforms)
                {
                    float rise = p.Top - _pos.y;
                    if (rise <= 0.05f || p.Top - MovingPlatform.Thickness >= _pos.y + Height) continue;
                    if (_pos.x + HalfWidth > p.XMin + 0.05f || newX + HalfWidth <= p.XMin) continue;
                    newX = Mathf.Max(p.XMin - HalfWidth - Skin, _pos.x - 0.02f);
                    IsBlocked = true;
                    WallHeight = rise <= 1.05f ? 1 : 2;
                    break;
                }
            }
            _pos.x = newX;
        }

        /// <summary>When stopped (speed 0) still report a wall we are pressed against.</summary>
        void CheckWallContact()
        {
            int col = Mathf.FloorToInt(_pos.x + HalfWidth + Skin * 2f);
            int row = Mathf.FloorToInt(_pos.y + 0.01f);
            if (_grid.IsSolid(col, row))
            {
                IsBlocked = true;
                WallHeight = CountWall(col, row);
            }
        }

        int CountWall(int col, int fromRow)
        {
            int n = 0;
            while (n < 8 && _grid.IsSolid(col, fromRow + n)) n++;
            return n;
        }

        void MoveVertical(float dy)
        {
            int c0 = Mathf.FloorToInt(_pos.x - HalfWidth + 0.01f);
            int c1 = Mathf.FloorToInt(_pos.x + HalfWidth - 0.01f);

            if (dy <= 0f)
            {
                float newY = _pos.y + dy;
                int row = Mathf.FloorToInt(newY);
                bool landed = false;
                // Only land on a cell whose top we were at or above last step
                if (_pos.y >= row + 1 - 0.01f)
                {
                    for (int c = c0; c <= c1 && !landed; c++)
                        landed = _grid.IsSolid(c, row);
                }
                if (landed)
                {
                    _pos.y = row + 1;
                    _vel.y = 0f;
                    Grounded = true;
                    GroundRow = row + 1;
                    _platform = null;
                    IsHopping = false;
                }
                else if (TryLandOnPlatform(newY, out var platform))
                {
                    _pos.y = platform.Top;
                    _vel.y = 0f;
                    Grounded = true;
                    GroundRow = Mathf.RoundToInt(platform.Top);
                    _platform = platform;
                    IsHopping = false;
                }
                else
                {
                    _pos.y = newY;
                    Grounded = false;
                    _platform = null;
                }
            }
            else
            {
                float newY = _pos.y + dy;
                int headRow = Mathf.FloorToInt(newY + Height);
                if (_pos.y + Height <= headRow + 0.01f)
                {
                    for (int c = c0; c <= c1; c++)
                    {
                        if (!_grid.IsSolid(c, headRow)) continue;
                        newY = headRow - Height;
                        _vel.y = 0f;
                        break;
                    }
                }
                _pos.y = newY;
                Grounded = false;
                _platform = null;
            }
        }

        bool TryLandOnPlatform(float newY, out MovingPlatform platform)
        {
            platform = null;
            if (_dynamic == null) return false;
            float left = _pos.x - HalfWidth, right = _pos.x + HalfWidth;
            foreach (var p in _dynamic.Platforms)
            {
                if (right <= p.XMin || left >= p.XMax) continue;
                // Were at/above the slab top (allowing for the slab rising into us) and would pass below it
                if (_pos.y >= p.Top - 0.12f && newY <= p.Top)
                {
                    platform = p;
                    return true;
                }
            }
            return false;
        }

        bool TouchesHazard()
        {
            Rect body = Bounds;
            body.xMin += 0.06f;
            body.xMax -= 0.06f;
            int c0 = Mathf.FloorToInt(body.xMin), c1 = Mathf.FloorToInt(body.xMax);
            int r0 = Mathf.FloorToInt(body.yMin), r1 = Mathf.FloorToInt(body.yMax);
            for (int c = c0; c <= c1; c++)
            for (int r = r0; r <= r1; r++)
            {
                if (!_grid.IsHazard(c, r)) continue;
                var spike = new Rect(c + 0.14f, r, 0.72f, 0.42f);
                if (spike.Overlaps(body)) return true;
            }
            return false;
        }

        /// <summary>Stand the player on a surface at the given row (used by pillar builds and rescues).</summary>
        public void PlaceOnSurface(int row, float popVelocity)
        {
            _pos.y = row;
            _vel.y = popVelocity;
            _platform = null;
            IsHopping = false;
            Grounded = popVelocity <= 0f;
            GroundRow = row;
            transform.position = _pos;
        }

        public void Kill(DeathCause cause)
        {
            if (IsDead) return;
            IsDead = true;
            DeathCause = cause;
            Speed = 0f;
            _vel = cause switch
            {
                DeathCause.Fell => new Vector2(0f, Mathf.Min(_vel.y, 0f)),
                DeathCause.OutOfBlocks => Vector2.zero,
                _ => new Vector2(-1.2f, 11f)
            };
            Died?.Invoke(cause);
        }

        /// <summary>Bring the player back after a revive, standing on the given surface.</summary>
        public void Revive(Vector2 feet)
        {
            ResetAt(feet);
        }
    }
}
