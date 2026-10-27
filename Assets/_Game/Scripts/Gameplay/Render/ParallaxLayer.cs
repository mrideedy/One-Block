using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>
    /// Endless horizontally-tiled background layer. <see cref="scrollFactor"/> is how fast it scrolls
    /// relative to the world (0 = fixed to the screen, 1 = moves with the ground).
    /// Optionally paints a solid fill below the layer so pits never reveal empty space.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [Range(0f, 1f)] [SerializeField] float scrollFactor = 0.2f;
        [Range(0f, 1f)] [SerializeField] float verticalFactor = 0.15f;
        [Tooltip("Bottom of the layer in world units when the camera is at y = 0.")]
        [SerializeField] float baseY = -1f;
        [SerializeField] float scale = 1.3f;
        [SerializeField] SpriteRenderer fillBelow;

        SpriteRenderer _sr;
        float _tileWidth;

        public void Configure(Camera cam, Sprite sprite, float factor, float vertical, float bottomY, float layerScale, int order)
        {
            targetCamera = cam;
            scrollFactor = factor;
            verticalFactor = vertical;
            baseY = bottomY;
            scale = layerScale;
            _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = sprite;
            _sr.sortingOrder = order;
            Setup();
        }

        public void SetSprite(Sprite sprite, Color fill)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = sprite;
            if (fillBelow != null) fillBelow.color = fill;
            Setup();
        }

        void Awake() => Setup();

        void Setup()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr.sprite == null) return;
            _sr.drawMode = SpriteDrawMode.Tiled;
            transform.localScale = Vector3.one * scale;
            _tileWidth = _sr.sprite.bounds.size.x * scale;
        }

        void LateUpdate()
        {
            if (targetCamera == null || _sr.sprite == null) return;

            Vector3 cam = targetCamera.transform.position;
            float halfW = targetCamera.orthographicSize * targetCamera.aspect;
            float halfH = targetCamera.orthographicSize;

            float offset = Mathf.Repeat(cam.x * scrollFactor, _tileWidth);
            float left = cam.x - halfW - offset;
            float width = halfW * 2f + _tileWidth + 0.5f;
            float y = baseY + cam.y * (1f - verticalFactor);

            transform.position = new Vector3(left, y, 0f);
            _sr.size = new Vector2(width / scale, _sr.sprite.bounds.size.y);

            if (fillBelow != null)
            {
                float fillHeight = Mathf.Max(0.5f, y - (cam.y - halfH) + 0.5f);
                fillBelow.drawMode = SpriteDrawMode.Simple;
                Transform ft = fillBelow.transform;
                ft.position = new Vector3(left + width * 0.5f, y + 0.02f - fillHeight * 0.5f, 0f);
                ft.localScale = new Vector3(width / scale, fillHeight / scale, 1f);
            }
        }
    }
}
