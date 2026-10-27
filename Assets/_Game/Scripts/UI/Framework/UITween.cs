using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneBlock.UI
{
    public enum Ease
    {
        Linear,
        OutQuad,
        OutCubic,
        OutBack,
        InQuad
    }

    /// <summary>
    /// Minimal tween runner for UI. Uses unscaled time so menus animate while the game is paused.
    /// Tweens are keyed by owner object + channel so starting a new one cancels the previous.
    /// </summary>
    public sealed class UITween : MonoBehaviour
    {
        struct Entry
        {
            public object Owner;
            public int Channel;
            public float Duration;
            public float Elapsed;
            public float Delay;
            public Ease Ease;
            public Action<float> Apply;
            public Action Done;
        }

        static UITween _instance;
        readonly List<Entry> _entries = new();

        static UITween Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("[UITween]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<UITween>();
                return _instance;
            }
        }

        /// <summary>Runs apply(t) with eased t from 0 to 1.</summary>
        public static void Run(object owner, int channel, float duration, Ease ease, Action<float> apply,
            Action done = null, float delay = 0f)
        {
            var list = Instance._entries;
            for (int i = list.Count - 1; i >= 0; i--)
                if (ReferenceEquals(list[i].Owner, owner) && list[i].Channel == channel) list.RemoveAt(i);

            list.Add(new Entry { Owner = owner, Channel = channel, Duration = Mathf.Max(0.0001f, duration), Ease = ease, Apply = apply, Done = done, Delay = delay });
            if (delay <= 0f) apply(0f);
        }

        public static void Cancel(object owner)
        {
            if (_instance == null) return;
            _instance._entries.RemoveAll(e => ReferenceEquals(e.Owner, owner));
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.Owner is UnityEngine.Object uo && uo == null)
                {
                    _entries.RemoveAt(i--);
                    continue;
                }
                if (e.Delay > 0f)
                {
                    e.Delay -= dt;
                    _entries[i] = e;
                    continue;
                }
                e.Elapsed += dt;
                float t = Mathf.Clamp01(e.Elapsed / e.Duration);
                e.Apply(Evaluate(e.Ease, t));
                if (t >= 1f)
                {
                    _entries.RemoveAt(i--);
                    e.Done?.Invoke();
                }
                else _entries[i] = e;
            }
        }

        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.InQuad: return t * t;
                case Ease.OutBack:
                {
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
                default: return t;
            }
        }
    }
}
