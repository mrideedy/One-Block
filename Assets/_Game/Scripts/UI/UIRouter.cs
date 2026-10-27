using OneBlock.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OneBlock.UI
{
    /// <summary>Shows the right screens for each game state and hosts transient toasts.</summary>
    public class UIRouter : MonoBehaviour
    {
        [SerializeField] MainMenuScreen mainMenu;
        [SerializeField] HudScreen hud;
        [SerializeField] PauseScreen pause;
        [SerializeField] ContinueScreen continueScreen;
        [SerializeField] ResultsScreen results;
        [SerializeField] SettingsScreen settings;
        [SerializeField] StatsScreen stats;
        [SerializeField] ShopScreen shop;
        [SerializeField] MissionsScreen missions;
        [SerializeField] CanvasGroup toast;
        [SerializeField] TMP_Text toastText;

        void Awake()
        {
            toast.alpha = 0f;
            toast.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.MissionCompleted += OnMissionCompleted;
        }

        void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.MissionCompleted -= OnMissionCompleted;
        }

        // During a run the HUD banner announces completions; afterwards (e.g. "play 3 runs") use a toast
        void OnMissionCompleted(Save.MissionState m)
        {
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing) return;
            Toast($"Mission complete: {Save.Missions.Describe(m)}!");
        }



        void Start()
        {
            if (GameManager.Instance != null) OnStateChanged(GameState.Boot, GameManager.Instance.State);
        }

        void OnStateChanged(GameState from, GameState to)
        {
            Set(mainMenu, to == GameState.Menu || to == GameState.Boot);
            Set(hud, to == GameState.Playing || to == GameState.Paused || to == GameState.Continue);
            Set(pause, to == GameState.Paused);
            Set(continueScreen, to == GameState.Continue);
            Set(results, to == GameState.GameOver);
            if (to != GameState.Menu)
            {
                settings.Hide();
                stats.Hide();
                shop.Hide();
                missions.Hide();
            }
        }

        // ------------------------------------------------------------------ Android back button

        float _exitArmedUntil;

        void Update()
        {
            // The Input System reports Android's back button / gesture as Escape
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (GameManager.Instance == null) return;
            OnBack(GameManager.Instance.State);
        }

        void OnBack(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    GameManager.Instance.Pause();
                    break;
                case GameState.Paused:
                    GameManager.Instance.Resume();
                    break;
                case GameState.GameOver:
                    results.BackPressed();
                    break;
                case GameState.Menu:
                    // Close the top-most panel first; on the bare menu, press twice to leave the game
                    if (missions.IsVisible) missions.Hide();
                    else if (shop.IsVisible) shop.Hide();
                    else if (stats.IsVisible) stats.Hide();
                    else if (settings.IsVisible) settings.Hide();
                    else if (Time.unscaledTime < _exitArmedUntil) Application.Quit();
                    else
                    {
                        _exitArmedUntil = Time.unscaledTime + 2f;
                        Toast("Press back again to exit");
                    }
                    break;
                // Continue: ignored so a stray back press never throws away a revive
            }
        }

        static void Set(UIScreen screen, bool visible)
        {
            if (visible) screen.Show();
            else screen.Hide();
        }

        public void ShowSettings() => settings.Show();
        public void ShowStats() => stats.Show();
        public void ShowShop() => shop.Show();
        public void ShowMissions() => missions.Show();
        public void ShowShop(Data.ShopCategory category) => shop.ShowTab(category);

        public void Toast(string message)
        {
            toastText.text = message;
            Audio.AudioManager.Sfx(Audio.SfxId.Toast, 0.7f);
            toast.gameObject.SetActive(true);
            UITween.Run(toast, 0, 2.2f, Ease.Linear, t =>
            {
                toast.alpha = t < 0.12f ? t / 0.12f : t > 0.8f ? (1f - t) / 0.2f : 1f;
            }, () => toast.gameObject.SetActive(false));
        }
    }
}
