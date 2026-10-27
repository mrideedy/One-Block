#if UNITY_EDITOR
using System;
using OneBlock.Core;
using OneBlock.Gameplay;
using UnityEngine;

namespace OneBlock.DevTools
{
    /// <summary>
    /// Editor-only: grabs a frame every few seconds of gameplay (for picking store screenshots).
    /// The actual capture is supplied by the editor tools through <see cref="Capture"/>.
    /// </summary>
    public class ShotRecorder : MonoBehaviour
    {
        public static Action<string, int> Capture;     // (prefix, index)
        public static Action<string, int> Finished;    // (prefix, count)

        public string Prefix = "shot";
        public int Count = 12;
        public float Interval = 2.5f;
        public float MinDistance;

        RunController _run;

        int _taken;
        float _timer;

        void LateUpdate()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (_run == null) _run = FindFirstObjectByType<RunController>();
            if (_run != null && _run.Distance < MinDistance) return;
            _timer += Time.unscaledDeltaTime;
            if (_timer < Interval) return;
            _timer = 0f;

            Capture?.Invoke(Prefix, _taken);
            _taken++;
            if (_taken < Count) return;
            Finished?.Invoke(Prefix, Count);
            Destroy(gameObject);
        }
    }
}
#endif
