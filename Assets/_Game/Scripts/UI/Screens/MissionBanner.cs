using System.Collections.Generic;
using OneBlock.Core;
using OneBlock.Save;
using TMPro;
using UnityEngine;

namespace OneBlock.UI
{
    /// <summary>"MISSION COMPLETE" banner that slides down from the top of the HUD during a run. Queues completions.</summary>
    public class MissionBanner : MonoBehaviour
    {
        [SerializeField] RectTransform banner;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text description;

        const float Show = 2.2f;
        const float Slide = 0.3f;

        readonly Queue<string> _queue = new();
        Vector2 _home;
        bool _busy;

        void Awake()
        {
            _home = banner.anchoredPosition;
            group.alpha = 0f;
        }

        void OnEnable() => GameEvents.MissionCompleted += OnCompleted;
        void OnDisable() => GameEvents.MissionCompleted -= OnCompleted;

        void OnCompleted(MissionState m)
        {
            // Completions after the run (runs played...) are announced by the router instead
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            _queue.Enqueue(Missions.Describe(m));
            if (!_busy) Next();
        }

        void Next()
        {
            if (_queue.Count == 0)
            {
                _busy = false;
                return;
            }
            _busy = true;
            description.text = _queue.Dequeue();
            UITween.Run(this, 0, Slide, Ease.OutBack, t =>
            {
                banner.anchoredPosition = _home + new Vector2(0f, Mathf.LerpUnclamped(160f, 0f, t));
                group.alpha = Mathf.Clamp01(t * 2f);
            });
            UITween.Run(this, 1, Slide, Ease.OutQuad, t =>
            {
                banner.anchoredPosition = _home + new Vector2(0f, Mathf.Lerp(0f, 160f, t));
                group.alpha = 1f - t;
            }, Next, delay: Show);
        }
    }
}
