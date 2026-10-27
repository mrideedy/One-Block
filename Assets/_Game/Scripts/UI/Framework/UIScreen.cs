using UnityEngine;

namespace OneBlock.UI
{
    /// <summary>
    /// Base for full-screen UI layers. Fades the whole layer and pops its main panel in with a small
    /// overshoot. Subclasses refresh their contents in <see cref="OnShow"/>.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreen : MonoBehaviour
    {
        const int ChannelFade = 0;
        const int ChannelPanel = 1;

        [Tooltip("Optional panel that scales in when the screen appears.")]
        [SerializeField] protected RectTransform panel;

        CanvasGroup _group;

        public bool IsVisible { get; private set; }

        protected CanvasGroup Group => _group != null ? _group : _group = GetComponent<CanvasGroup>();

        protected virtual void Awake()
        {
            if (!IsVisible) HideImmediate();
        }

        public void Show()
        {
            if (IsVisible) { OnShow(); return; }
            IsVisible = true;
            gameObject.SetActive(true);
            Group.blocksRaycasts = true;
            Group.interactable = true;
            float from = Group.alpha;
            UITween.Run(this, ChannelFade, 0.18f, Ease.OutQuad, t => Group.alpha = Mathf.Lerp(from, 1f, t));
            if (panel != null)
                UITween.Run(panel, ChannelPanel, 0.32f, Ease.OutBack, t => panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.86f, 1f, t));
            OnShow();
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            Group.blocksRaycasts = false;
            Group.interactable = false;
            float from = Group.alpha;
            UITween.Run(this, ChannelFade, 0.14f, Ease.OutQuad, t => Group.alpha = Mathf.Lerp(from, 0f, t),
                () => { if (!IsVisible) gameObject.SetActive(false); });
            OnHide();
        }

        public void HideImmediate()
        {
            IsVisible = false;
            UITween.Cancel(this);
            Group.alpha = 0f;
            Group.blocksRaycasts = false;
            Group.interactable = false;
            gameObject.SetActive(false);
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
    }
}
