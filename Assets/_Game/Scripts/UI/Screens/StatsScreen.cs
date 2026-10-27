using OneBlock.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneBlock.UI
{
    /// <summary>Lifetime statistics.</summary>
    public class StatsScreen : UIScreen
    {
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text bestText;
        [SerializeField] TMP_Text challengeBestText;
        [SerializeField] TMP_Text streakText;
        [SerializeField] TMP_Text runsText;
        [SerializeField] TMP_Text earnedText;

        void Start() => closeButton.onClick.AddListener(Hide);

        protected override void OnShow()
        {
            var d = SaveSystem.Data;
            bestText.text = d.bestDistance > 0 ? UIFormat.Metres(d.bestDistance) : "—";
            challengeBestText.text = d.challengeBestDistance > 0 ? UIFormat.Metres(d.challengeBestDistance) : "—";
            int streak = DailyProgress.CurrentStreak(d, DailyProgress.Clock());
            streakText.text = $"{streak}  <size=70%><alpha=#99>(BEST {d.bestDailyStreak})</size>";
            runsText.text = d.totalRuns.ToString();
            earnedText.text = d.totalCoinsEarned.ToString();
        }
    }
}
