using System.Collections.Generic;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>
    /// Coins and block packs. Kept as plain data + pooled renderers; the player overlap test is a
    /// simple circle-vs-rect check each frame (only a handful are on screen at once).
    /// </summary>
    public class CollectibleSystem : MonoBehaviour
    {
        public const float CoinRadius = 0.32f;
        public const float PackRadius = 0.36f;
        const float CoinFps = 10f;
        const float CollectFxDuration = 0.28f;

        class Item
        {
            public SpawnKind Kind;
            public Vector2 Position;
            public int Column;
            public SpriteRenderer Renderer;
            public float Phase;
        }

        Sprite[] _coinFrames;
        Sprite _blockPack;
        SpritePool _pool;
        readonly List<Item> _items = new();

        /// <summary>Raised for every pickup with its world position (for effects).</summary>
        public event System.Action<SpawnKind, Vector2> Collected;
        readonly List<(SpriteRenderer sr, float t, Vector3 start)> _fx = new();
        float _time;

        public void Init(Sprite[] coinFrames, Sprite blockPack)
        {
            _coinFrames = coinFrames;
            _blockPack = blockPack;
            _pool = new SpritePool(transform, "Collectible", 32);
        }

        /// <summary>Block packs show the equipped block skin.</summary>
        public void SetPackSprite(Sprite sprite)
        {
            _blockPack = sprite;
            foreach (var it in _items)
                if (it.Kind == SpawnKind.BlockPack) it.Renderer.sprite = sprite;
        }

        public void Spawn(SpawnRequest req)
        {
            bool coin = req.Kind == SpawnKind.Coin;
            var sr = _pool.Get(coin ? _coinFrames[0] : _blockPack, req.Position, TerrainRenderer.OrderProps + 1);
            sr.transform.localScale = Vector3.one * (coin ? 0.78f : 0.5f);
            _items.Add(new Item
            {
                Kind = req.Kind,
                Position = req.Position,
                Column = req.Column,
                Renderer = sr,
                Phase = req.Position.x * 0.37f
            });
        }

        /// <summary>Animates items and collects any overlapping the player.</summary>
        public void Tick(float dt, Rect player, out int coins, out int packs)
        {
            coins = 0;
            packs = 0;
            _time += dt;

            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var it = _items[i];
                if (it.Kind == SpawnKind.Coin)
                {
                    int frame = (int)((_time + it.Phase) * CoinFps) % _coinFrames.Length;
                    it.Renderer.sprite = _coinFrames[frame];
                    it.Renderer.flipX = frame == 3;
                }
                else
                {
                    float bob = Mathf.Sin((_time + it.Phase) * 4f) * 0.08f;
                    it.Renderer.transform.position = it.Position + new Vector2(0f, bob);
                }

                float r = it.Kind == SpawnKind.Coin ? CoinRadius : PackRadius;
                if (!CircleOverlapsRect(it.Position, r, player)) continue;

                if (it.Kind == SpawnKind.Coin) coins++;
                else packs++;
                Collected?.Invoke(it.Kind, it.Position);
                _fx.Add((it.Renderer, 0f, it.Renderer.transform.position));
                _items.RemoveAt(i);
            }

            for (int i = _fx.Count - 1; i >= 0; i--)
            {
                var (sr, t, start) = _fx[i];
                t += dt;
                float n = t / CollectFxDuration;
                sr.transform.position = start + new Vector3(0f, n * 0.8f, 0f);
                sr.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.1f, n);
                sr.color = new Color(1f, 1f, 1f, 1f - n);
                if (n >= 1f)
                {
                    _pool.Release(sr);
                    _fx.RemoveAt(i);
                }
                else _fx[i] = (sr, t, start);
            }
        }

        public void RemoveColumnsBefore(int column)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (_items[i].Column >= column) continue;
                _pool.Release(_items[i].Renderer);
                _items.RemoveAt(i);
            }
        }

        public void ClearAll()
        {
            foreach (var it in _items) _pool.Release(it.Renderer);
            foreach (var fx in _fx) _pool.Release(fx.sr);
            _items.Clear();
            _fx.Clear();
        }

        public static bool CircleOverlapsRect(Vector2 c, float r, Rect rect)
        {
            float nx = Mathf.Clamp(c.x, rect.xMin, rect.xMax);
            float ny = Mathf.Clamp(c.y, rect.yMin, rect.yMax);
            float dx = c.x - nx, dy = c.y - ny;
            return dx * dx + dy * dy <= r * r;
        }
    }
}
