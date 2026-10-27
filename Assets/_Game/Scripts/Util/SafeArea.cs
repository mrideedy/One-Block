using UnityEngine;

namespace OneBlock.Util
{
    /// <summary>
    /// Fits a RectTransform to Screen.safeArea so HUD elements avoid notches and punch-hole cameras,
    /// which sit on the left/right edges in landscape. Horizontal insets are mirrored (the larger of the two
    /// is used on both sides) so centred UI stays centred on phones with a camera cut-out on one side only.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        RectTransform _rect;
        Rect _lastSafe;
        Vector2Int _lastScreen;

        void Awake()
        {
            _rect = GetComponent<RectTransform>();
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != _lastSafe || _lastScreen.x != Screen.width || _lastScreen.y != Screen.height)
                Apply();
        }

        void Apply()
        {
            Rect safe = Screen.safeArea;
            _lastSafe = safe;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            float inset = Mathf.Max(min.x, Screen.width - max.x);
            min.x = inset;
            max.x = Screen.width - inset;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
