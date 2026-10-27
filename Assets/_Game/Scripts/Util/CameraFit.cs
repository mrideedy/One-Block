using OneBlock.Core;
using UnityEngine;

namespace OneBlock.Util
{
    /// <summary>
    /// Landscape camera sizing: fixed height on phones (wider phones simply see further ahead),
    /// grows vertically on narrow screens like 4:3 tablets so a minimum width is always visible.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class CameraFit : MonoBehaviour
    {
        [SerializeField] GameConfig config;

        Camera _cam;
        int _lastW, _lastH;

        void OnEnable()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
            Apply();
        }

        void Update()
        {
            if (Screen.width != _lastW || Screen.height != _lastH) Apply();
        }

        void Apply()
        {
            if (config == null || _cam == null) return;
            _lastW = Screen.width;
            _lastH = Screen.height;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float sizeForMinWidth = config.minVisibleWidth / (2f * aspect);
            _cam.orthographicSize = Mathf.Max(config.orthographicSize, sizeForMinWidth);
        }
    }
}
