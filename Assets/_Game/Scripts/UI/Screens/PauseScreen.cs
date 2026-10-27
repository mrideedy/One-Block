using OneBlock.Core;
using OneBlock.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace OneBlock.UI
{
    public class PauseScreen : UIScreen
    {
        [SerializeField] RunController run;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button homeButton;
        [SerializeField] SettingToggle musicToggle;
        [SerializeField] SettingToggle soundToggle;
        [SerializeField] SettingToggle hapticsToggle;

        void Start()
        {
            resumeButton.onClick.AddListener(() => GameManager.Instance.Resume());
            restartButton.onClick.AddListener(run.Restart);
            homeButton.onClick.AddListener(run.GoHome);
        }

        protected override void OnShow()
        {
            musicToggle.Refresh();
            soundToggle.Refresh();
            hapticsToggle.Refresh();
        }
    }
}
