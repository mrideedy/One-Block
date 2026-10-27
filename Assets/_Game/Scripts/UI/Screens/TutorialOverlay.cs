using OneBlock.Core;
using TMPro;
using UnityEngine;

namespace OneBlock.UI
{
    /// <summary>
    /// First-run tutorial presentation: a dimmed freeze with a pulsing ring on the target cell, a tapping hand
    /// and an instruction card; then quick praise and a closing tip. Nothing here blocks raycasts, so a tap
    /// anywhere reaches the game.
    /// </summary>
    public class TutorialOverlay : MonoBehaviour
    {
        [SerializeField] CanvasGroup dim;
        [SerializeField] RectTransform ring;
        [SerializeField] RectTransform hand;
        [SerializeField] CanvasGroup card;
        [SerializeField] TMP_Text cardTitle;
        [SerializeField] TMP_Text cardBody;
        [SerializeField] CanvasGroup banner;
        [SerializeField] TMP_Text bannerTitle;
        [SerializeField] TMP_Text bannerBody;
        [SerializeField] TMP_Text praise;

        static readonly (string title, string body)[] Lessons =
        {
            ("TAP TO PLACE A BLOCK", "Bridge the gap so your hero can cross!"),
            ("TAP AGAIN!", "Cover the {0} with a block to cross safely!"),
        };

        RectTransform _rect;
        Camera _cam;
        Vector2Int _cell;
        bool _holding;
        bool _wallHint;
        float _holdTime;
        float _bannerHideAt = -1f;

        void Awake()
        {
            _rect = (RectTransform)transform;
            HideAll();
        }

        void OnEnable() => GameEvents.Tutorial += OnTutorial;
        void OnDisable() => GameEvents.Tutorial -= OnTutorial;

        void HideAll()
        {
            _holding = false;
            _wallHint = false;
            dim.alpha = 0f;
            card.alpha = 0f;
            banner.alpha = 0f;
            ring.gameObject.SetActive(false);
            hand.gameObject.SetActive(false);
            praise.gameObject.SetActive(false);
            _bannerHideAt = -1f;
        }

        void OnTutorial(TutorialEvent e)
        {
            switch (e.Phase)
            {
                case TutorialPhase.Hidden:
                    HideAll();
                    break;

                case TutorialPhase.Intro:
                    HideAll();
                    ShowBanner("LET'S GO!", "Your hero runs on their own. Watch the path ahead!", 2.6f);
                    break;

                case TutorialPhase.WallHint:
                    if (_holding) break;
                    _wallHint = true;
                    _holdTime = 0f;
                    _cell = e.Cell;
                    hand.gameObject.SetActive(true);
                    ShowBanner("TAP TO BUILD A STEP!", "Walls stop you. Blocks help you climb over them.", 999f);
                    break;

                case TutorialPhase.WallHintEnd:
                    if (!_wallHint) break;
                    _wallHint = false;
                    hand.gameObject.SetActive(false);
                    HideBannerNow();
                    break;

                case TutorialPhase.Hold:
                    _wallHint = false;
                    _holding = true;
                    _holdTime = 0f;
                    _cell = e.Cell;
                    var (title, body) = Lessons[Mathf.Clamp(e.Lesson, 0, Lessons.Length - 1)];
                    cardTitle.text = title;
                    cardBody.text = string.Format(body, string.IsNullOrEmpty(e.Hazard) ? "spikes" : e.Hazard);
                    ring.gameObject.SetActive(true);
                    hand.gameObject.SetActive(true);
                    HideBannerNow();
                    Fade(dim, 0.55f, 0.25f, 1);
                    Fade(card, 1f, 0.25f, 2);
                    UITween.Run(card.transform, 3, 0.35f, Ease.OutBack, t => card.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, t));
                    break;

                case TutorialPhase.Success:
                case TutorialPhase.Complete:
                    _holding = false;
                    ring.gameObject.SetActive(false);
                    hand.gameObject.SetActive(false);
                    Fade(dim, 0f, 0.2f, 1);
                    Fade(card, 0f, 0.15f, 2);
                    ShowPraise(e.Cell, e.Phase == TutorialPhase.Complete ? "GREAT!" : "NICE!");
                    if (e.Phase == TutorialPhase.Complete)
                        ShowBanner("YOU'VE GOT IT!", "Blocks are limited. Grab block packs to get more!", 3.4f, delay: 0.5f);
                    break;
            }
        }

        void ShowBanner(string title, string body, float seconds, float delay = 0f)
        {
            bannerTitle.text = title;
            bannerBody.text = body;
            banner.alpha = 0f;
            UITween.Run(banner, 4, 0.3f, Ease.OutCubic, t =>
            {
                banner.alpha = t;
                banner.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, t);
            }, delay: delay);
            _bannerHideAt = Time.unscaledTime + delay + seconds;
        }

        void HideBannerNow()
        {
            _bannerHideAt = -1f;
            Fade(banner, 0f, 0.15f, 4);
        }

        void ShowPraise(Vector2Int cell, string text)
        {
            praise.text = text;
            praise.gameObject.SetActive(true);
            var rt = praise.rectTransform;
            Vector2 basePos = CellToLocal(cell) + new Vector2(0f, 130f);
            UITween.Run(praise, 5, 0.9f, Ease.Linear, t =>
            {
                rt.anchoredPosition = basePos + new Vector2(0f, t * 60f);
                float pop = t < 0.25f ? Mathf.LerpUnclamped(0.4f, 1.15f, t / 0.25f) : Mathf.Lerp(1.15f, 1f, (t - 0.25f) / 0.75f);
                rt.localScale = Vector3.one * pop;
                praise.alpha = t > 0.7f ? (1f - t) / 0.3f : 1f;
            }, () => praise.gameObject.SetActive(false));
        }

        void Fade(CanvasGroup group, float to, float duration, int channel)
        {
            float from = group.alpha;
            UITween.Run(group, channel, duration, Ease.OutCubic, t => group.alpha = Mathf.Lerp(from, to, t));
        }

        Vector2 CellToLocal(Vector2Int cell)
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return Vector2.zero;
            Vector3 screen = _cam.WorldToScreenPoint(new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, null, out var local);
            return local;
        }

        void Update()
        {
            if (_bannerHideAt > 0f && Time.unscaledTime >= _bannerHideAt) HideBannerNow();
            if (_wallHint)
            {
                _holdTime += Time.unscaledDeltaTime;
                float bob = Mathf.Abs(Mathf.Sin(_holdTime * 4f));
                hand.anchoredPosition = CellToLocal(_cell) + new Vector2(55f, -95f - bob * 22f);
                hand.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, bob);
                return;
            }
            if (!_holding) return;

            _holdTime += Time.unscaledDeltaTime;
            Vector2 target = CellToLocal(_cell);

            ring.anchoredPosition = target;
            float pulse = Mathf.Repeat(_holdTime, 1f);
            ring.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.25f, pulse);
            var ringImg = ring.GetComponent<UnityEngine.UI.Image>();
            if (ringImg != null) ringImg.color = new Color(1f, 0.86f, 0.3f, Mathf.Lerp(1f, 0.35f, pulse));

            // Hand taps just below the target
            float tap = Mathf.Abs(Mathf.Sin(_holdTime * 4f));
            hand.anchoredPosition = target + new Vector2(55f, -95f - tap * 22f);
            hand.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, tap);
        }
    }
}
