using System.Collections.Generic;
using OneBlock.Data;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>
    /// Lightweight pooled sprite particles for death effects (no ParticleSystem, so styles stay
    /// fully code-driven and cheap on low-end phones).
    /// </summary>
    public class EffectBurst : MonoBehaviour
    {
        struct Particle
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Gravity;
            public float Drag;
            public float Rotation;
            public float Spin;
            public float StartScale;
            public float EndScale;
            public float Life;
            public float Age;
            public Color Color;
            public bool FadeIn;
        }

        SpritePool _pool;
        readonly List<Particle> _particles = new();

        void Awake() => _pool = new SpritePool(transform, "Particle", 24);

        public void Play(DeathEffect effect, Vector2 origin)
        {
            if (effect == null || effect.particles == null || effect.particles.Length == 0) return;
            var sprites = effect.particles;
            var palette = effect.palette;

            switch (effect.style)
            {
                case DeathEffectStyle.Explosion:
                    Emit(sprites[Mathf.Min(1, sprites.Length - 1)], origin, Vector2.zero, 0f, 0f, 0.4f, 2.6f, 0.28f, new Color(1f, 0.95f, 0.7f, 0.9f));
                    for (int i = 0; i < 18; i++)
                    {
                        float a = i / 18f * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        var p = Emit(sprites[0], origin, dir * Random.Range(5f, 9f), 6f, 3f, Random.Range(1.2f, 1.8f), 0.1f,
                            Random.Range(0.35f, 0.6f), palette[i % palette.Length]);
                        p.Rotation = a * Mathf.Rad2Deg - 90f;
                        Replace(p);
                    }
                    break;

                case DeathEffectStyle.Poof:
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i / 10f * Mathf.PI * 2f;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.7f);
                        Emit(sprites[0], origin + dir * 0.2f, dir * Random.Range(1.5f, 3f) + Vector2.up * 0.8f, -0.5f, 2.5f,
                            Random.Range(0.35f, 0.55f), Random.Range(0.9f, 1.3f), Random.Range(0.5f, 0.75f), palette[i % palette.Length]);
                    }
                    break;

                case DeathEffectStyle.Skull:
                {
                    var skull = Emit(sprites[0], origin, Vector2.up * 2.2f, -0.5f, 1.2f, 0.2f, 1.1f, 1.1f, Color.white);
                    skull.FadeIn = true;
                    Replace(skull);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i / 6f * Mathf.PI * 2f;
                        Emit(sprites[Mathf.Min(1, sprites.Length - 1)], origin, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 2f, 0f, 3f, 0.3f, 0.7f, 0.45f,
                            palette[palette.Length - 1]);
                    }
                    break;
                }

                case DeathEffectStyle.Confetti:
                    for (int i = 0; i < 30; i++)
                    {
                        var v = new Vector2(Random.Range(-4.5f, 4.5f), Random.Range(5f, 11f));
                        var p = Emit(sprites[0], origin, v, 18f, 1.2f, 1.3f, 1.1f, Random.Range(0.9f, 1.4f), palette[i % palette.Length]);
                        p.Spin = Random.Range(-720f, 720f);
                        Replace(p);
                    }
                    break;
            }
        }

        public void Clear()
        {
            foreach (var p in _particles) _pool.Release(p.Renderer);
            _particles.Clear();
        }

        Particle Emit(Sprite sprite, Vector2 pos, Vector2 vel, float gravity, float drag, float startScale, float endScale, float life, Color color)
        {
            var sr = _pool.Get(sprite, pos, TerrainRenderer.OrderProps + 5);
            sr.color = color;
            var p = new Particle
            {
                Renderer = sr, Position = pos, Velocity = vel, Gravity = gravity, Drag = drag,
                StartScale = startScale, EndScale = endScale, Life = life, Color = color
            };
            _particles.Add(p);
            return p;
        }

        // Particles are structs; re-store the last emitted one after tweaking it
        void Replace(Particle p) => _particles[_particles.Count - 1] = p;

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Age += dt;
                float t = p.Age / p.Life;
                if (t >= 1f)
                {
                    _pool.Release(p.Renderer);
                    _particles.RemoveAt(i);
                    continue;
                }
                p.Velocity.y -= p.Gravity * dt;
                p.Velocity *= Mathf.Max(0f, 1f - p.Drag * dt);
                p.Position += p.Velocity * dt;
                p.Rotation += p.Spin * dt;

                var tr = p.Renderer.transform;
                tr.position = p.Position;
                tr.localRotation = Quaternion.Euler(0f, 0f, p.Rotation);
                float scale = Mathf.Lerp(p.StartScale, p.EndScale, p.FadeIn ? 1f - (1f - t) * (1f - t) : t);
                tr.localScale = Vector3.one * scale;
                float alpha = p.FadeIn ? Mathf.Min(1f, t * 6f) * (t > 0.75f ? (1f - t) / 0.25f : 1f) : 1f - t * t;
                p.Renderer.color = new Color(p.Color.r, p.Color.g, p.Color.b, p.Color.a * alpha);
                _particles[i] = p;
            }
        }
    }
}
