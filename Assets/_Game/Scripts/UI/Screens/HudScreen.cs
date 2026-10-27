using OneBlock.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneBlock.UI
{
    /// <summary>In-run HUD: pause, block count, distance, coins and the "tap to go" prompt after a revive.</summary>
    public class HudScreen : UIScreen
    {
        [SerializeField] Button pauseButton;
        [SerializeField] RectTransform blocksPill;
        [SerializeField] Image blocksPillBackground;
        [SerializeField] TMP_Text blocksText;
        [SerializeField] TMP_Text coinsText;
        [SerializeField] TMP_Text distanceText;
        [SerializeField] RectTransform coinIcon;
        [SerializeField] GameObject tapPrompt;
        [SerializeField] TMP_Text floatingText;
        [SerializeField] Image blockIcon;

        static readonly Color PillColor = new Color(0.11f, 0.15f, 0.2f, 0.94f);
        static readonly Color DeniedColor = new Color(0.9f, 0.25f, 0.27f, 0.95f);
        static readonly Color LowColor = new Color(0.85f, 0.45f, 0.15f, 0.95f);

        float _shake;
        Vector2 _pillBase;
        int _blocks;
        float _time;

        protected override void Awake()
        {
            base.Awake();
            _pillBase = blocksPill.anchoredPosition;
        }

        void Start() => pauseButton.onClick.AddListener(() => GameManager.Instance.Pause());

        void OnEnable()
        {
            GameEvents.BlocksChanged += OnBlocks;
            GameEvents.RunCoinsChanged += OnCoins;
            GameEvents.DistanceChanged += OnDistance;
            GameEvents.BuildDenied += OnDenied;
            GameEvents.BlocksGained += OnGained;
            GameEvents.AwaitingTapChanged += OnAwaitingTap;
            GameEvents.StateChanged += OnState;
            GameEvents.CosmeticsChanged += OnCosmetics;
        }

        void OnDisable()
        {
            GameEvents.BlocksChanged -= OnBlocks;
            GameEvents.RunCoinsChanged -= OnCoins;
            GameEvents.DistanceChanged -= OnDistance;
            GameEvents.BuildDenied -= OnDenied;
            GameEvents.BlocksGained -= OnGained;
            GameEvents.AwaitingTapChanged -= OnAwaitingTap;
            GameEvents.StateChanged -= OnState;
            GameEvents.CosmeticsChanged -= OnCosmetics;
        }

        // The HUD deactivates while hidden, so it misses events (including the one that starts a run)
        // and has to re-sync everything here, pause button included.
        protected override void OnShow()
        {
            if (GameManager.Instance != null)
                pauseButton.interactable = GameManager.Instance.State == GameState.Playing;
            var run = FindAnyObjectByType<Gameplay.RunController>();
            if (run == null) return;
            OnBlocks(run.Blocks);
            OnCoins(run.Coins);
            OnDistance(run.Distance);
            OnCosmetics();
        }

        void OnCosmetics()
        {
            var run = FindAnyObjectByType<Gameplay.RunController>();
            if (run != null && run.Cosmetics != null && run.Cosmetics.CurrentBlock != null && blockIcon != null)
                blockIcon.sprite = run.Cosmetics.CurrentBlock.sprite;
        }

        void OnState(GameState from, GameState to) => pauseButton.interactable = to == GameState.Playing;

        void OnBlocks(int count)
        {
            if (count > _blocks && _blocks >= 0 && IsVisible) Punch(blocksPill, 0.22f);
            _blocks = count;
            blocksText.text = count.ToString();
        }

        void OnCoins(int count)
        {
            coinsText.text = count.ToString();
            if (count > 0 && IsVisible) Punch(coinIcon, 0.25f);
        }

        void OnDistance(float metres) => distanceText.text = UIFormat.Metres(metres);

        void OnDenied() => _shake = 0.4f;

        void OnGained(int amount)
        {
            if (floatingText == null || !IsVisible) return;
            floatingText.text = $"+{amount}";
            var rt = floatingText.rectTransform;
            UITween.Run(floatingText, 0, 0.9f, Ease.OutCubic, t =>
            {
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -150f - t * 60f);
                floatingText.alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            });
        }

        void OnAwaitingTap(bool waiting) => tapPrompt.SetActive(waiting);

        static void Punch(Transform target, float amount)
        {
            UITween.Run(target, 7, 0.3f, Ease.Linear, t => target.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * amount),
                () => target.localScale = Vector3.one);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _time += dt;
            _shake = Mathf.Max(0f, _shake - dt);
            float offset = _shake > 0f ? Mathf.Sin(_shake * 70f) * 16f * (_shake / 0.4f) : 0f;
            blocksPill.anchoredPosition = _pillBase + new Vector2(offset, 0f);

            Color bg = _shake > 0f ? DeniedColor : _blocks <= 1 ? Color.Lerp(PillColor, LowColor, 0.5f + 0.5f * Mathf.Sin(_time * 8f)) : PillColor;
            blocksPillBackground.color = bg;

            if (tapPrompt.activeSelf)
                tapPrompt.transform.localScale = Vector3.one * (1f + Mathf.Sin(_time * 5f) * 0.05f);
        }
    }
}
