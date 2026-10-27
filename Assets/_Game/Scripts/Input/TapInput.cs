using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace OneBlock.Controls
{
    /// <summary>
    /// The game's single input: a tap anywhere (touch, mouse click or Space). Taps that land on UI are
    /// ignored. A tap stays pending for a short buffer so a slightly early tap still counts.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class TapInput : MonoBehaviour
    {
        InputAction _tap;
        bool _queued;
        bool _queuedFromPointer;
        Vector2 _queuedPosition;

        bool _pending;
        float _pendingSince;
        readonly List<RaycastResult> _hits = new();

        public bool HasTap(float buffer) => _pending && Time.unscaledTime - _pendingSince <= buffer;

        void OnEnable()
        {
            _tap = new InputAction("Tap", InputActionType.Button);
            _tap.AddBinding("<Touchscreen>/primaryTouch/press");
            _tap.AddBinding("<Mouse>/leftButton");
            _tap.AddBinding("<Keyboard>/space");
            _tap.performed += OnTap;
            _tap.Enable();
        }

        void OnDisable()
        {
            _tap.performed -= OnTap;
            _tap.Disable();
            _tap.Dispose();
        }

        void OnTap(InputAction.CallbackContext ctx)
        {
            _queued = true;
            _queuedFromPointer = !(ctx.control.device is Keyboard);
            _queuedPosition = ctx.control.device switch
            {
                Touchscreen ts => ts.primaryTouch.position.ReadValue(),
                Mouse m => m.position.ReadValue(),
                _ => Vector2.zero
            };
        }

        void Update()
        {
            if (!_queued) return;
            _queued = false;
            if (_queuedFromPointer && IsOverUI(_queuedPosition)) return;
            _pending = true;
            _pendingSince = Time.unscaledTime;
        }

        public void Consume() => _pending = false;

        /// <summary>Injects a tap as if the player touched the screen (used by the dev autoplayer).</summary>
        public void SimulateTap()
        {
            _pending = true;
            _pendingSince = Time.unscaledTime;
        }

        bool IsOverUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPos };
            _hits.Clear();
            es.RaycastAll(data, _hits);
            return _hits.Count > 0;
        }
    }
}
