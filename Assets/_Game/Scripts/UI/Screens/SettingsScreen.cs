using OneBlock.Save;
using OneBlock.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneBlock.UI
{
    /// <summary>Music / sound / vibration toggles.</summary>
    public class SettingsScreen : UIScreen
    {
        [SerializeField] Button closeButton;
        [SerializeField] SettingToggle[] toggles;
        [SerializeField] TMP_Text versionText;
        [Tooltip("Shown only where the consent SDK requires it (e.g. EU/UK).")]
        [SerializeField] Button privacyButton;

        void Start()
        {
            closeButton.onClick.AddListener(Hide);
            if (versionText != null) versionText.text = $"One Block v{Application.version}";
            if (privacyButton != null) privacyButton.onClick.AddListener(() => Ads.ShowPrivacyOptions(null));
        }

        protected override void OnShow()
        {
            foreach (var t in toggles) t.Refresh();
            if (privacyButton != null) privacyButton.gameObject.SetActive(Ads.PrivacyOptionsRequired());
        }
    }
}
