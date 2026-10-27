using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OneBlock.UI
{
    /// <summary>Squashes a button while pressed and springs it back on release.</summary>
    [RequireComponent(typeof(Selectable))]
    public class ButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] float pressedScale = 0.92f;

        Selectable _selectable;
        bool _pressed;

        void Awake() => _selectable = GetComponent<Selectable>();

        void OnDisable()
        {
            _pressed = false;
            UITween.Cancel(this);
            transform.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_selectable.IsInteractable()) return;
            _pressed = true;
            Audio.AudioManager.Sfx(Audio.SfxId.UiClick);
            float from = transform.localScale.x;
            UITween.Run(this, 0, 0.08f, Ease.OutQuad, t => transform.localScale = Vector3.one * Mathf.Lerp(from, pressedScale, t));
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        void Release()
        {
            if (!_pressed) return;
            _pressed = false;
            float from = transform.localScale.x;
            UITween.Run(this, 0, 0.25f, Ease.OutBack, t => transform.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1f, t));
        }
    }
}
