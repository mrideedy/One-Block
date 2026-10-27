using OneBlock.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneBlock.UI
{
    /// <summary>Icon button bound to a boolean setting in the save data.</summary>
    public class SettingToggle : MonoBehaviour
    {
        public enum Setting
        {
            Music,
            Sound,
            Haptics
        }

        [SerializeField] Setting setting;
        [SerializeField] Button button;
        [SerializeField] Image icon;
        [SerializeField] Sprite onSprite;
        [SerializeField] Sprite offSprite;
        [SerializeField] TMP_Text stateLabel;
        [SerializeField] GameObject offSlash;

        public static event System.Action<Setting, bool> Changed;

        void Awake() => button.onClick.AddListener(Toggle);
        void OnEnable() => Refresh();

        bool Value
        {
            get
            {
                var d = SaveSystem.Data;
                return setting switch { Setting.Music => d.musicOn, Setting.Sound => d.sfxOn, _ => d.hapticsOn };
            }
            set
            {
                var d = SaveSystem.Data;
                switch (setting)
                {
                    case Setting.Music: d.musicOn = value; break;
                    case Setting.Sound: d.sfxOn = value; break;
                    default: d.hapticsOn = value; break;
                }
                SaveSystem.MarkDirty();
                Changed?.Invoke(setting, value);
            }
        }

        void Toggle()
        {
            Value = !Value;
            SaveSystem.SaveIfDirty();
            Refresh();
        }

        public void Refresh()
        {
            bool on = Value;
            if (icon != null)
            {
                if (onSprite != null && offSprite != null) icon.sprite = on ? onSprite : offSprite;
                icon.color = on ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            }
            if (offSlash != null) offSlash.SetActive(!on);
            if (stateLabel != null)
            {
                stateLabel.text = on ? "ON" : "OFF";
                stateLabel.color = on ? new Color(0.45f, 0.85f, 0.35f) : new Color(1f, 1f, 1f, 0.5f);
            }
        }
    }
}
