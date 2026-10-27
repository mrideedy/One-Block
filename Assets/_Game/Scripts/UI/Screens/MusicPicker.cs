using System.Collections.Generic;
using OneBlock.Audio;
using OneBlock.Gameplay;
using OneBlock.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneBlock.UI
{
    /// <summary>Settings row that cycles the soundtrack: Auto (each world plays its own music) or any unlocked track.</summary>
    public class MusicPicker : MonoBehaviour
    {
        [SerializeField] Button previous;
        [SerializeField] Button next;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subtitle;

        void Awake()
        {
            previous.onClick.AddListener(() => Step(-1));
            next.onClick.AddListener(() => Step(1));
        }

        void OnEnable() => Refresh();

        List<MusicTrack> Options => AudioManager.Instance != null ? AudioManager.Instance.UnlockedTracks() : new List<MusicTrack>();

        void Step(int dir)
        {
            var options = Options;
            int count = options.Count + 1;   // index 0 is Auto
            int index = IndexOf(options, SaveSystem.Data.musicTrack);
            index = ((index + dir) % count + count) % count;
            SaveSystem.Data.musicTrack = index == 0 ? "" : options[index - 1].id;
            SaveSystem.Save();
            Refresh();
        }

        static int IndexOf(List<MusicTrack> options, string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            for (int i = 0; i < options.Count; i++) if (options[i].id == id) return i + 1;
            return 0;
        }

        void Refresh()
        {
            var options = Options;
            int index = IndexOf(options, SaveSystem.Data.musicTrack);
            if (index == 0)
            {
                title.text = "AUTO";
                subtitle.text = "Every world plays its own music";
                return;
            }
            var track = options[index - 1];
            title.text = track.title.ToUpperInvariant();
            if (string.IsNullOrEmpty(track.worldId)) subtitle.text = "Main theme";
            else
            {
                var run = FindAnyObjectByType<RunController>();
                var world = run != null && run.Cosmetics != null ? run.Cosmetics.Catalog.World(track.worldId) : null;
                subtitle.text = world != null ? $"{world.displayName} theme" : "World theme";
            }
        }
    }
}
