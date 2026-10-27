using OneBlock.Core;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>
    /// Locks the player at a fixed horizontal screen position and eases vertically to keep the last
    /// ground the player stood on at a fixed height. Falling into a pit doesn't drag the camera down.
    /// On the main menu the player stands further left so the centred menu column doesn't cover them;
    /// starting a run glides to the gameplay framing.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        Camera _cam;
        GameConfig _cfg;
        PlayerController _player;
        float _velY;
        float _screenX;
        float _screenXVel;
        Vector3 _base;
        float _trauma;
        float _shakeSeed;

        const float MaxShake = 0.45f;

        /// <summary>Adds screen shake (0..1). Stacks and decays over ~half a second.</summary>
        public void AddTrauma(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

        public float HalfWidth => _cam.orthographicSize * _cam.aspect;
        public float HalfHeight => _cam.orthographicSize;
        public float Left => _base.x - HalfWidth;
        public float Right => _base.x + HalfWidth;

        public void Init(GameConfig cfg, PlayerController player)
        {
            _cam = GetComponent<Camera>();
            _cfg = cfg;
            _player = player;
            _screenX = DesiredScreenX();
        }

        public void Snap()
        {
            _screenX = DesiredScreenX();
            _screenXVel = 0f;
            _base = Target();
            transform.position = _base;
            _velY = 0f;
        }

        float DesiredScreenX()
        {
            var gm = GameManager.Instance;
            bool menu = gm == null || gm.State == GameState.Menu || gm.State == GameState.Boot;
            return menu ? _cfg.menuPlayerScreenX : _cfg.playerScreenX;
        }

        public void Tick(float dt)
        {
            _screenX = Mathf.SmoothDamp(_screenX, DesiredScreenX(), ref _screenXVel, _cfg.framingSmoothing, Mathf.Infinity, dt);
            Vector3 target = Target();
            Vector3 pos = _base;
            if (!_player.IsDead) pos.x = target.x;   // hold still while the death animation plays
            pos.y = Mathf.SmoothDamp(pos.y, target.y, ref _velY, _cfg.cameraVerticalSmoothing, Mathf.Infinity, dt);
            _base = pos;

            // Trauma-based shake (squared for a punchy falloff), on unscaled time so hit-stop doesn't freeze it
            _trauma = Mathf.Max(0f, _trauma - Time.unscaledDeltaTime * 1.8f);
            Vector3 shake = Vector3.zero;
            if (_trauma > 0f)
            {
                float amount = _trauma * _trauma;
                _shakeSeed += Time.unscaledDeltaTime * 28f;
                shake = new Vector3(
                    (Mathf.PerlinNoise(_shakeSeed, 0.3f) - 0.5f) * 2f * MaxShake * amount,
                    (Mathf.PerlinNoise(0.7f, _shakeSeed) - 0.5f) * 2f * MaxShake * amount, 0f);
            }
            transform.position = pos + shake;
        }

        Vector3 Target()
        {
            float halfW = HalfWidth, halfH = HalfHeight;
            float x = _player.Position.x + halfW - _screenX * 2f * halfW;
            float y = _player.GroundRow + halfH - _cfg.groundScreenY * 2f * halfH;
            return new Vector3(x, y, -10f);
        }
    }
}
