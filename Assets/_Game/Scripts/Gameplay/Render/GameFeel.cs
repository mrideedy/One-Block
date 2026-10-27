using System.Collections.Generic;
using OneBlock.Core;
using TMPro;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>
    /// Game feel: pooled sprite particles, floating world text, speed lines, running/landing dust,
    /// camera shake triggers and hit-stop. Purely cosmetic; never affects gameplay state.
    /// </summary>
    public class GameFeel : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] CameraFollow cameraFollow;
        [SerializeField] CollectibleSystem collectibles;
        [SerializeField] BlockBuilder builder;

        [Header("Sprites")]
        [SerializeField] Sprite puff;
        [SerializeField] Sprite spark;
        [SerializeField] Sprite star;
        [SerializeField] Sprite ring;
        [SerializeField] Sprite square;
        [SerializeField] Sprite streak;

        [Header("Floating text")]
        [SerializeField] TMP_FontAsset font;
        [SerializeField] Material fontMaterial;

        const int OrderFx = TerrainRenderer.OrderProps + 4;
        static readonly Color Gold = new(1f, 0.82f, 0.25f);
        static readonly Color Dust = new(1f, 1f, 1f, 0.55f);
        static readonly Color[] Confetti = { new(1f, 0.35f, 0.35f), new(1f, 0.76f, 0.23f), new(0.36f, 0.75f, 0.24f), new(0.18f, 0.53f, 0.94f), new(0.61f, 0.36f, 0.94f) };

        struct Particle
        {
            public SpriteRenderer Renderer;
            public Vector2 Position, Velocity;
            public float Gravity, Drag, Rotation, Spin, StartScale, EndScale, Life, Age;
            public Color Color;
            public bool Stretch;   // scale X by speed (streaks)
        }

        struct FloatText
        {
            public TextMeshPro Text;
            public Vector2 Position;
            public float Age, Life;
        }

        SpritePool _pool;
        readonly List<Particle> _particles = new();
        readonly List<FloatText> _texts = new();
        readonly Stack<TextMeshPro> _textPool = new();
        float _dustTimer, _streakTimer;
        float _hitStopUntil = -1f;
        bool _wasBlocked;
        int _lastMilestone;

        // ------------------------------------------------------------------ lifecycle

        void Awake() => _pool = new SpritePool(transform, "Fx", 64);

        void Start()
        {
            if (collectibles != null) collectibles.Collected += OnCollected;
            if (builder != null)
            {
                builder.Built += OnBuilt;
                builder.EnemySquashed += OnSquashed;
            }
            if (run != null && run.Player != null) run.Player.Landed += OnLanded;
        }

        void OnEnable()
        {
            GameEvents.PlayerDied += OnDied;
            GameEvents.DistanceChanged += OnDistance;
            GameEvents.RunStarted += OnRunStarted;
            GameEvents.StateChanged += OnState;
        }

        void OnDisable()
        {
            GameEvents.PlayerDied -= OnDied;
            GameEvents.DistanceChanged -= OnDistance;
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.StateChanged -= OnState;
        }

        static bool Playing => GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;

        // ------------------------------------------------------------------ reactions

        void OnCollected(SpawnKind kind, Vector2 pos)
        {
            if (kind == SpawnKind.Coin)
            {
                Burst(spark, pos, 6, 2.5f, 4f, 0.45f, 0.1f, 0.3f, Gold, gravity: 2f);
                Emit(ring, pos, Vector2.zero, 0f, 0f, 0.06f, 0.22f, 0.22f, new Color(1f, 0.9f, 0.5f, 0.8f));
            }
            else
            {
                Emit(ring, pos, Vector2.zero, 0f, 0f, 0.1f, 0.5f, 0.35f, new Color(1f, 1f, 1f, 0.9f));
                Burst(star, pos, 7, 3f, 5f, 0.5f, 0.15f, 0.45f, Gold, gravity: 4f, spin: 400f);
                ShowText($"+{GameManager.Instance.Config.blocksPerPack}", pos + new Vector2(0f, 0.5f), new Color(0.55f, 0.9f, 1f), 6f);
            }
        }

        void OnBuilt(BuildTarget target)
        {
            var c = target.Cell;
            Vector2 bottom = new(c.x + 0.5f, c.y + 0.02f);
            for (int i = 0; i < 7; i++)
            {
                float dir = i % 2 == 0 ? -1f : 1f;
                var vel = new Vector2(dir * Random.Range(1.2f, 2.6f), Random.Range(0.2f, 1.2f));
                Emit(puff, bottom + new Vector2(dir * 0.35f, 0f), vel, -0.5f, 3.5f, Random.Range(0.18f, 0.28f), Random.Range(0.4f, 0.55f), Random.Range(0.3f, 0.45f), Dust);
            }
            if (target.Kind == BuildKind.Pillar) cameraFollow.AddTrauma(0.18f);
            else cameraFollow.AddTrauma(0.06f);
        }

        void OnSquashed(Enemy e)
        {
            Vector2 pos = new(e.X, e.Row + 0.35f);
            Burst(star, pos, 8, 3.5f, 6f, 0.55f, 0.12f, 0.5f, Color.white, gravity: 6f, spin: 540f);
            Emit(ring, pos, Vector2.zero, 0f, 0f, 0.12f, 0.6f, 0.3f, new Color(1f, 1f, 1f, 0.8f));
            ShowText($"+{GameManager.Instance.Config.coinsPerSquash}", pos + new Vector2(0f, 0.6f), Gold, 6f);
            cameraFollow.AddTrauma(0.28f);
            HitStop(0.06f);
        }

        void OnLanded(float impactSpeed)
        {
            if (impactSpeed < 6f) return;
            var feet = run.Player.Position;
            int n = impactSpeed > 12f ? 8 : 5;
            for (int i = 0; i < n; i++)
            {
                float dir = i % 2 == 0 ? -1f : 1f;
                Emit(puff, feet + new Vector2(dir * 0.2f, 0.05f), new Vector2(dir * Random.Range(1.5f, 3f), Random.Range(0.1f, 0.8f)), -0.3f, 4f,
                    0.2f, 0.5f, Random.Range(0.3f, 0.4f), Dust);
            }
            if (impactSpeed > 12f) cameraFollow.AddTrauma(0.12f);
        }

        void OnDied()
        {
            cameraFollow.AddTrauma(0.65f);
            HitStop(0.12f);
        }

        void OnDistance(float metres)
        {
            int hundreds = Mathf.FloorToInt(metres / 100f);
            if (hundreds <= _lastMilestone || !Playing) return;
            _lastMilestone = hundreds;
            var p = run.Player.Position + new Vector2(0.5f, 1.8f);
            ShowText($"{hundreds * 100} m!", p, Color.white, 9f, life: 1.1f);
            for (int i = 0; i < 22; i++)
            {
                var v = new Vector2(Random.Range(-3.5f, 5.5f), Random.Range(4f, 9f));
                var part = Emit(square, p, v, 14f, 1.2f, 0.7f, 0.6f, Random.Range(0.8f, 1.2f), Confetti[i % Confetti.Length]);
                part.Spin = Random.Range(-720f, 720f);
                Replace(part);
            }
        }

        void OnRunStarted(GameMode mode) => _lastMilestone = 0;

        void OnState(GameState from, GameState to)
        {
            if (to == GameState.Menu) Clear();
            if (to != GameState.Playing && to != GameState.Continue) EndHitStop();
        }

        // ------------------------------------------------------------------ hit-stop

        void HitStop(float seconds)
        {
            if (!Playing && GameManager.Instance.State != GameState.Continue) return;
            Time.timeScale = 0.05f;
            _hitStopUntil = Time.unscaledTime + seconds;
        }

        void EndHitStop()
        {
            if (_hitStopUntil < 0f) return;
            _hitStopUntil = -1f;
            var gm = GameManager.Instance;
            Time.timeScale = gm != null && gm.State == GameState.Paused ? 0f : 1f;
        }

        // ------------------------------------------------------------------ frame

        void Update()
        {
            if (_hitStopUntil > 0f && Time.unscaledTime >= _hitStopUntil) EndHitStop();

            float dt = Time.deltaTime;
            var player = run != null ? run.Player : null;
            if (player != null && Playing)
            {
                // Running dust
                if (player.Grounded && player.Speed > 0.5f && !player.IsDead)
                {
                    _dustTimer -= dt;
                    if (_dustTimer <= 0f)
                    {
                        _dustTimer = Mathf.Lerp(0.16f, 0.07f, Mathf.InverseLerp(4f, 9f, player.Speed));
                        var feet = player.Position + new Vector2(-0.15f, 0.04f);
                        Emit(puff, feet, new Vector2(-Random.Range(0.6f, 1.4f), Random.Range(0.3f, 0.8f)), -0.4f, 2.5f,
                            Random.Range(0.1f, 0.16f), Random.Range(0.26f, 0.36f), 0.35f, new Color(1f, 1f, 1f, 0.35f));
                    }
                }

                // Wall bump
                bool blocked = player.IsBlocked && player.WallHeight >= 2 && player.Grounded;
                if (blocked && !_wasBlocked) cameraFollow.AddTrauma(0.1f);
                _wasBlocked = blocked;

                // Speed lines once the run is fast
                var cfg = GameManager.Instance.Config;
                float fast = Mathf.InverseLerp(cfg.startSpeed + (cfg.maxSpeed - cfg.startSpeed) * 0.45f, cfg.maxSpeed, player.Speed);
                if (fast > 0f && !player.IsDead)
                {
                    _streakTimer -= dt;
                    if (_streakTimer <= 0f)
                    {
                        _streakTimer = Mathf.Lerp(0.12f, 0.035f, fast);
                        var cam = cameraFollow;
                        float y = cam.transform.position.y + Random.Range(-cam.HalfHeight * 0.9f, cam.HalfHeight * 0.9f);
                        var pos = new Vector2(cam.Right + 1.5f, y);
                        var part = Emit(streak, pos, new Vector2(-Random.Range(38f, 55f), 0f), 0f, 0f, 1f, 1f, 0.45f,
                            new Color(1f, 1f, 1f, Mathf.Lerp(0.12f, 0.3f, fast)));
                        part.Stretch = true;
                        Replace(part);
                    }
                }
            }

            UpdateParticles(dt);
            UpdateTexts(Time.unscaledDeltaTime);
        }

        // ------------------------------------------------------------------ particles

        void Burst(Sprite sprite, Vector2 origin, int count, float minSpeed, float maxSpeed, float startScale, float endScale, float life, Color color,
            float gravity = 0f, float spin = 0f)
        {
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var p = Emit(sprite, origin, dir * Random.Range(minSpeed, maxSpeed), gravity, 3f, startScale, endScale, life * Random.Range(0.8f, 1.1f), color);
                p.Spin = Random.Range(-spin, spin);
                p.Rotation = Random.Range(0f, 360f);
                Replace(p);
            }
        }

        Particle Emit(Sprite sprite, Vector2 pos, Vector2 vel, float gravity, float drag, float startScale, float endScale, float life, Color color)
        {
            if (sprite == null) return default;
            var sr = _pool.Get(sprite, pos, OrderFx);
            sr.color = color;
            var p = new Particle
            {
                Renderer = sr, Position = pos, Velocity = vel, Gravity = gravity, Drag = drag,
                StartScale = startScale, EndScale = endScale, Life = life, Color = color
            };
            _particles.Add(p);
            return p;
        }

        void Replace(Particle p)
        {
            if (p.Renderer != null) _particles[_particles.Count - 1] = p;
        }

        void UpdateParticles(float dt)
        {
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
                float s = Mathf.Lerp(p.StartScale, p.EndScale, t);
                tr.localScale = p.Stretch ? new Vector3(Mathf.Lerp(1.5f, 3f, Mathf.Abs(p.Velocity.x) / 55f), 0.05f, 1f) : Vector3.one * s;
                float alpha = p.Stretch ? Mathf.Sin(t * Mathf.PI) : 1f - t * t;
                p.Renderer.color = new Color(p.Color.r, p.Color.g, p.Color.b, p.Color.a * alpha);
                _particles[i] = p;
            }
        }

        // ------------------------------------------------------------------ floating text

        void ShowText(string text, Vector2 pos, Color color, float size, float life = 0.8f)
        {
            if (font == null) return;
            var tmp = _textPool.Count > 0 ? _textPool.Pop() : CreateText();
            tmp.gameObject.SetActive(true);
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.transform.position = pos;
            tmp.transform.localScale = Vector3.one * 0.3f;
            _texts.Add(new FloatText { Text = tmp, Position = pos, Life = life });
        }

        TextMeshPro CreateText()
        {
            var go = new GameObject("FloatText");
            go.transform.SetParent(transform, false);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = font;
            if (fontMaterial != null) tmp.fontSharedMaterial = fontMaterial;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(6f, 2f);
            tmp.sortingOrder = OrderFx + 2;
            return tmp;
        }

        void UpdateTexts(float dt)
        {
            for (int i = _texts.Count - 1; i >= 0; i--)
            {
                var f = _texts[i];
                f.Age += dt;
                float t = f.Age / f.Life;
                if (t >= 1f)
                {
                    f.Text.gameObject.SetActive(false);
                    _textPool.Push(f.Text);
                    _texts.RemoveAt(i);
                    continue;
                }
                float pop = t < 0.2f ? Mathf.LerpUnclamped(0.3f, 1.15f, t / 0.2f) : Mathf.Lerp(1.15f, 1f, (t - 0.2f) / 0.3f);
                f.Text.transform.position = f.Position + new Vector2(0f, t * 0.9f);
                f.Text.transform.localScale = Vector3.one * pop;
                f.Text.alpha = t > 0.7f ? (1f - t) / 0.3f : 1f;
                _texts[i] = f;
            }
        }

        public void Clear()
        {
            foreach (var p in _particles) _pool.Release(p.Renderer);
            _particles.Clear();
            foreach (var f in _texts)
            {
                f.Text.gameObject.SetActive(false);
                _textPool.Push(f.Text);
            }
            _texts.Clear();
        }
    }
}
