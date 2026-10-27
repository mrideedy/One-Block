using System.Collections.Generic;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>Draws enemies and elevators from <see cref="DynamicWorld"/> state each frame.</summary>
    public class DynamicWorldRenderer : MonoBehaviour
    {
        const float SquashDuration = 0.45f;
        const float EnemyScale = 0.8f;

        DynamicWorld _world;
        Sprite[] _enemyFrames;
        Sprite _platform;
        Sprite _chain;
        SpritePool _pool;
        readonly Dictionary<object, SpriteRenderer> _views = new();
        readonly Dictionary<MovingPlatform, SpriteRenderer> _chains = new();
        readonly List<object> _stale = new();
        float _time;

        public void Init(DynamicWorld world, Sprite[] enemyFrames, Sprite platform, Sprite chainPixel)
        {
            _world = world;
            _enemyFrames = enemyFrames;
            _platform = platform;
            _chain = chainPixel;
            _pool = new SpritePool(transform, "Dynamic", 8);
        }

        /// <summary>Swaps the enemy look (per world). Ignored if the theme has no frames.</summary>
        public void SetEnemyFrames(Sprite[] frames)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null) return;
            _enemyFrames = frames;
            foreach (var kv in _views)
                if (kv.Key is Enemy) kv.Value.sprite = frames[0];
        }

        void LateUpdate()
        {
            if (_world == null) return;
            _time += Time.deltaTime;

            foreach (var e in _world.Enemies)
            {
                if (!e.Alive && e.DeadTime >= SquashDuration) continue;   // finished; view gets released below
                var sr = View(e, _enemyFrames[0], TerrainRenderer.OrderProps + 1);
                if (e.Alive)
                {
                    sr.sprite = _enemyFrames[(int)(_time * 6f + e.SpawnColumn) % _enemyFrames.Length];
                    sr.flipX = e.Direction > 0;
                    float bob = Mathf.Abs(Mathf.Sin(_time * 9f + e.SpawnColumn)) * 0.04f;
                    sr.transform.position = new Vector3(e.X, e.Row + bob, 0f);
                    sr.transform.localScale = Vector3.one * EnemyScale;
                    sr.color = Color.white;
                }
                else
                {
                    float t = Mathf.Clamp01(e.DeadTime / SquashDuration);
                    sr.transform.position = new Vector3(e.X, e.Row, 0f);
                    sr.transform.localScale = new Vector3(EnemyScale * (1f + t * 0.4f), EnemyScale * Mathf.Lerp(0.35f, 0.1f, t), 1f);
                    sr.color = new Color(1f, 1f, 1f, 1f - t);
                }
            }

            foreach (var p in _world.Platforms)
            {
                var sr = View(p, _platform, TerrainRenderer.OrderBlock);
                float width = _platform.bounds.size.x;
                sr.transform.position = new Vector3(p.XMin + p.Width * 0.5f, p.Top - MovingPlatform.Thickness * 0.5f, 0f);
                sr.transform.localScale = new Vector3(p.Width / width, 1f, 1f);

                if (!_chains.TryGetValue(p, out var chain))
                {
                    chain = _pool.Get(_chain, Vector3.zero, TerrainRenderer.OrderDecor);
                    chain.color = new Color(0.25f, 0.27f, 0.33f, 0.9f);
                    _chains[p] = chain;
                }
                // Guide rail under the slab so the elevator reads as a machine, not a floating plank
                float railTop = p.Top - MovingPlatform.Thickness;
                float railBottom = p.MinTop - 6f;
                chain.transform.position = new Vector3(p.XMin + p.Width * 0.5f, (railTop + railBottom) * 0.5f, 0f);
                chain.transform.localScale = new Vector3(0.12f, railTop - railBottom, 1f);
            }

            // Release views whose data was removed
            _stale.Clear();
            foreach (var kv in _views)
            {
                bool alive = kv.Key is Enemy en ? Contains(_world.Enemies, en) && en.DeadTime < SquashDuration
                                                : Contains(_world.Platforms, (MovingPlatform)kv.Key);
                if (!alive) _stale.Add(kv.Key);
            }
            foreach (var key in _stale)
            {
                _pool.Release(_views[key]);
                _views.Remove(key);
                if (key is MovingPlatform mp && _chains.Remove(mp, out var c)) _pool.Release(c);
            }
        }

        SpriteRenderer View(object key, Sprite sprite, int order)
        {
            if (_views.TryGetValue(key, out var sr)) return sr;
            sr = _pool.Get(sprite, Vector3.zero, order);
            _views[key] = sr;
            return sr;
        }

        public void ClearAll()
        {
            foreach (var sr in _views.Values) _pool.Release(sr);
            foreach (var sr in _chains.Values) _pool.Release(sr);
            _views.Clear();
            _chains.Clear();
        }

        static bool Contains<T>(IReadOnlyList<T> list, T item) where T : class
        {
            for (int i = 0; i < list.Count; i++)
                if (ReferenceEquals(list[i], item)) return true;
            return false;
        }
    }
}
