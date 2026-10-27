using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>Stretches the sky gradient over the whole view and drifts clouds with slow parallax.</summary>
    public class SkyBackground : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] SpriteRenderer sky;
        [SerializeField] Sprite[] cloudSprites;
        [SerializeField] int cloudCount = 5;
        [SerializeField] float cloudScrollFactor = 0.06f;
        [SerializeField] float cloudDrift = 0.25f;

        struct Cloud
        {
            public SpriteRenderer Renderer;
            public float X;
            public float Y;
        }

        Cloud[] _clouds;
        float _time;

        public void Configure(Camera cam, Sprite skySprite, Sprite[] clouds)
        {
            if (cam != null) targetCamera = cam;
            sky.sprite = skySprite;
            cloudSprites = clouds;
            BuildClouds();
        }

        void Awake() => BuildClouds();

        void BuildClouds()
        {
            if (_clouds != null)
                foreach (var c in _clouds) if (c.Renderer != null) Destroy(c.Renderer.gameObject);
            _clouds = null;
            if (cloudSprites == null || cloudSprites.Length == 0 || !Application.isPlaying) return;

            var rng = new System.Random(7);
            _clouds = new Cloud[cloudCount];
            for (int i = 0; i < cloudCount; i++)
            {
                var go = new GameObject("Cloud");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = cloudSprites[i % cloudSprites.Length];
                sr.sortingOrder = -95;
                float s = 0.6f + (float)rng.NextDouble() * 0.7f;
                go.transform.localScale = Vector3.one * s;
                _clouds[i] = new Cloud
                {
                    Renderer = sr,
                    X = i * 7.3f + (float)rng.NextDouble() * 3f,
                    Y = 0.8f + (float)rng.NextDouble() * 2.6f
                };
            }
        }

        void LateUpdate()
        {
            if (targetCamera == null) return;
            _time += Time.deltaTime;
            Vector3 cam = targetCamera.transform.position;
            float halfW = targetCamera.orthographicSize * targetCamera.aspect;
            float halfH = targetCamera.orthographicSize;

            if (sky != null && sky.sprite != null)
            {
                Vector2 size = sky.sprite.bounds.size;
                sky.transform.position = new Vector3(cam.x, cam.y, 0f);
                sky.transform.localScale = new Vector3(halfW * 2.2f / size.x, halfH * 2.2f / size.y, 1f);
            }

            if (_clouds == null) return;
            float wrap = halfW * 2f + 8f;
            for (int i = 0; i < _clouds.Length; i++)
            {
                float sx = Mathf.Repeat(_clouds[i].X - cam.x * cloudScrollFactor - _time * cloudDrift, wrap);
                _clouds[i].Renderer.transform.position = new Vector3(cam.x - halfW - 4f + sx, cam.y + _clouds[i].Y, 0f);
            }
        }
    }
}
