using OneBlock.Core;
using OneBlock.Data;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>
    /// Flipbook animation from a <see cref="CharacterSkin"/> plus squash-and-stretch.
    /// Lives on the player root; drives a child SpriteRenderer so scaling pivots at the feet.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] SpriteRenderer body;

        CharacterSkin _skin;
        PlayerController _player;
        float _runTime;
        float _idleTime;
        float _squash;       // 1 → 0 after landing
        float _stretch;      // 1 → 0 after a hop

        public void Init(PlayerController player, CharacterSkin skin)
        {
            _player = player;
            SetSkin(skin);
            player.Landed += speed => _squash = Mathf.Clamp01(speed / 14f);
            player.Hopped += () => _stretch = 1f;
        }

        public void SetSkin(CharacterSkin skin)
        {
            _skin = skin;
            if (skin != null && skin.idle.Length > 0) body.sprite = skin.idle[0];
        }

        public void SetVisible(bool visible) => body.enabled = visible;

        public void ResetVisuals()
        {
            body.enabled = true;
            _runTime = _idleTime = _squash = _stretch = 0f;
            body.transform.localScale = Vector3.one;
            body.transform.localRotation = Quaternion.identity;
            if (_skin != null) body.sprite = _skin.idle[0];
        }

        public void Tick(float dt, bool runActive)
        {
            if (_skin == null) return;

            Sprite sprite;
            if (_player.IsDead)
            {
                sprite = _skin.hit;
                if (_player.DeathCause != DeathCause.OutOfBlocks) body.transform.Rotate(0f, 0f, 220f * dt);
            }
            else if (!_player.Grounded)
            {
                sprite = _player.Velocity.y > 0.5f ? _skin.jump : _skin.fall;
            }
            else if (!runActive || _player.Speed < 0.05f || _player.IsBlocked && _player.WallHeight >= 2)
            {
                _idleTime += dt;
                sprite = _skin.idle[(int)(_idleTime * 2.5f) % _skin.idle.Length];
            }
            else
            {
                // Cadence scales with speed so feet don't slide
                _runTime += dt * (_player.Speed / 4.5f);
                sprite = _skin.run[(int)(_runTime * 12f) % _skin.run.Length];
            }
            body.sprite = sprite;

            _squash = Mathf.MoveTowards(_squash, 0f, dt * 6f);
            _stretch = Mathf.MoveTowards(_stretch, 0f, dt * 5f);
            float sx = 1f + _squash * 0.22f - _stretch * 0.1f;
            float sy = 1f - _squash * 0.2f + _stretch * 0.12f;
            if (!_player.IsDead) body.transform.localScale = new Vector3(sx, sy, 1f);
        }
    }
}
