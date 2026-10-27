using OneBlock.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OneBlock.UI
{
    /// <summary>
    /// Boot intro: the yellow block drops in, "ONE" pops, "BL" and "CK" slide in around it to spell the logo,
    /// then the backdrop fades away to the main menu, whose logo sits in exactly the same place.
    /// A tap skips ahead to the fade.
    /// </summary>
    public class IntroScreen : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] CanvasGroup background;
        [SerializeField] CanvasGroup logoGroup;
        [SerializeField] RectTransform block;
        [SerializeField] RectTransform one;
        [SerializeField] RectTransform bl;
        [SerializeField] RectTransform ck;
        [SerializeField] RectTransform shadow;
        [SerializeField] UnityEngine.UI.Image[] dust;
        [SerializeField] RectTransform[] clouds;

        Vector2[] _cloudStart;

        const float BlockY = -84f;           // block rest position inside the logo (matches the menu logo)
        const float OneY = 92f, LetterY = -92f, BlX = -265f, CkX = 265f;
        const float SkipAllowedAfter = 0.35f;
        const float HoldUntil = 2.1f;
        const float FadeDuration = 0.5f;

        float _t;
        float _fadeT;
        bool _fading;

        void Start()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Boot)
            {
                gameObject.SetActive(false);
                return;
            }
            group.alpha = 1f;
            background.alpha = 1f;
            logoGroup.alpha = 1f;
            Audio.AudioManager.Sfx(Audio.SfxId.IntroSting);
            _cloudStart = new Vector2[clouds.Length];
            for (int i = 0; i < clouds.Length; i++) _cloudStart[i] = clouds[i].anchoredPosition;
            Apply(0f);
        }

        void Update()
        {
            // Clouds keep drifting through the fade
            if (_cloudStart != null)
                for (int i = 0; i < clouds.Length; i++)
                    clouds[i].anchoredPosition = _cloudStart[i] + new Vector2((_t + (_fading ? _fadeT : 0f)) * (18f + i * 10f), 0f);
            if (_fading)
            {
                _fadeT += Time.unscaledDeltaTime;
                return;
            }
            _t += Time.unscaledDeltaTime;
            Apply(_t);

            bool tapped = (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) ||
                          (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame);
            if (_t >= HoldUntil || (tapped && _t >= SkipAllowedAfter)) FinishIntro();
        }

        void FinishIntro()
        {
            _fading = true;
            Apply(10f);   // snap the logo into its final pose
            GameManager.Instance.FinishBoot();
            UITween.Run(this, 0, FadeDuration, Ease.OutQuad, t =>
            {
                background.alpha = 1f - t;
                // The menu's own logo is underneath in the same spot; fade ours out a touch later
                logoGroup.alpha = 1f - Mathf.InverseLerp(0.35f, 1f, t);
            }, () => gameObject.SetActive(false));
        }

        /// <summary>Poses every element for time t (seconds since the intro started).</summary>
        void Apply(float t)
        {
            // Block: falls from above, lands at 0.55s with a squash, settles
            const float dropStart = 0.1f, land = 0.55f;
            float y;
            Vector3 scale = Vector3.one;
            if (t < dropStart) y = 900f;
            else if (t < land)
            {
                float k = (t - dropStart) / (land - dropStart);
                y = Mathf.Lerp(900f, BlockY, k * k);
            }
            else
            {
                float k = Mathf.Clamp01((t - land) / 0.35f);
                float bounce = Mathf.Sin(k * Mathf.PI) * (1f - k) * 40f;
                y = BlockY + bounce;
                float squash = Mathf.Sin(Mathf.Clamp01((t - land) / 0.18f) * Mathf.PI) * 0.22f;
                scale = new Vector3(1f + squash, 1f - squash, 1f);
            }
            block.anchoredPosition = new Vector2(0f, y);
            block.localScale = scale;
            block.localRotation = Quaternion.Euler(0f, 0f, t < land ? Mathf.Lerp(-25f, 0f, Mathf.Clamp01((t - dropStart) / (land - dropStart))) : 0f);

            // Shadow grows as the block approaches
            float s = t < land ? Mathf.Clamp01((t - dropStart) / (land - dropStart)) : 1f;
            shadow.localScale = new Vector3(Mathf.Lerp(0.2f, 1f, s), Mathf.Lerp(0.2f, 1f, s), 1f);
            shadow.gameObject.SetActive(t >= dropStart && t < 1.1f);

            // Dust puffs at impact
            for (int i = 0; i < dust.Length; i++)
            {
                float k = Mathf.Clamp01((t - land) / 0.45f);
                bool on = t >= land && k < 1f;
                dust[i].gameObject.SetActive(on);
                if (!on) continue;
                float dir = i % 2 == 0 ? -1f : 1f;
                float spread = 60f + i * 25f;
                var rt = dust[i].rectTransform;
                rt.anchoredPosition = new Vector2(dir * (70f + spread * k), BlockY - 70f + k * 30f);
                rt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.2f, k) * (1f - k * 0.5f);
                var c = dust[i].color;
                dust[i].color = new Color(c.r, c.g, c.b, 1f - k);
            }

            // "ONE" pops in
            float oneK = Mathf.Clamp01((t - 0.75f) / 0.3f);
            one.anchoredPosition = new Vector2(0f, OneY);
            one.localScale = Vector3.one * EaseOutBack(oneK);

            // "BL" and "CK" slide in from the sides
            float slideK = EaseOutBack(Mathf.Clamp01((t - 0.95f) / 0.4f));
            bl.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-1400f, BlX, slideK), LetterY);
            ck.anchoredPosition = new Vector2(Mathf.LerpUnclamped(1400f, CkX, slideK), LetterY);
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
