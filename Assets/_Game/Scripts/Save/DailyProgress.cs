using System;
using OneBlock.Core;
using UnityEngine;

namespace OneBlock.Save
{
    /// <summary>
    /// Daily Challenge bookkeeping: per-day best, play streak and the once-a-day streak bonus.
    /// Days are UTC so everyone shares the same level and the same reset moment.
    /// The core methods take the save data and time explicitly so they can be unit-tested.
    /// </summary>
    public static class DailyProgress
    {
        /// <summary>How many past days of records to keep.</summary>
        public const int KeepDays = 30;

        public struct Outcome
        {
            public bool FirstToday;     // this run claimed today's streak bonus
            public int StreakDay;       // streak length after this run
            public int Reward;          // bonus coins granted by this run (0 unless FirstToday)
            public float PreviousBest;  // today's best before this run
            public bool IsNewBest;
        }

        public static Func<DateTime> Clock = () => DateTime.UtcNow;

        public static int Key(DateTime utc) => utc.Year * 10000 + utc.Month * 100 + utc.Day;
        public static int Today => Key(Clock());

        /// <summary>Time left until the next daily level (UTC midnight).</summary>
        public static TimeSpan TimeUntilReset(DateTime utcNow) => utcNow.Date.AddDays(1) - utcNow;
        public static TimeSpan TimeUntilReset() => TimeUntilReset(Clock());

        public static int RewardForStreakDay(GameConfig cfg, int day)
        {
            int capped = Mathf.Clamp(day, 1, Mathf.Max(1, cfg.dailyMaxStreakBonusDays));
            return cfg.dailyBaseReward + cfg.dailyRewardPerStreakDay * (capped - 1);
        }

        public static DailyRecord RecordFor(SaveData d, int dayKey)
        {
            foreach (var r in d.dailyRecords)
                if (r.date == dayKey) return r;
            return null;
        }

        public static bool ClaimedToday(SaveData d, DateTime utcNow) => d.lastDailyDate == Key(utcNow);

        /// <summary>The streak as it stands now: alive if the last daily run was today or yesterday, otherwise 0.</summary>
        public static int CurrentStreak(SaveData d, DateTime utcNow)
        {
            if (d.lastDailyDate == Key(utcNow) || d.lastDailyDate == Key(utcNow.Date.AddDays(-1))) return d.dailyStreak;
            return 0;
        }

        /// <summary>The streak day the next run today counts as (unchanged if today is already claimed).</summary>
        public static int NextStreakDay(SaveData d, DateTime utcNow) =>
            ClaimedToday(d, utcNow) ? d.dailyStreak : CurrentStreak(d, utcNow) + 1;

        /// <summary>Records a finished (or abandoned) Daily Challenge run and grants the streak bonus once per day.</summary>
        public static Outcome RegisterRun(SaveData d, GameConfig cfg, float distance, DateTime utcNow)
        {
            int today = Key(utcNow);
            var outcome = new Outcome();

            var rec = RecordFor(d, today);
            if (rec == null)
            {
                rec = new DailyRecord { date = today };
                d.dailyRecords.Add(rec);
            }
            rec.attempts++;
            outcome.PreviousBest = rec.bestDistance;
            if (distance > rec.bestDistance)
            {
                rec.bestDistance = distance;
                outcome.IsNewBest = true;
            }

            if (d.lastDailyDate != today)
            {
                d.dailyStreak = CurrentStreak(d, utcNow) + 1;
                d.bestDailyStreak = Mathf.Max(d.bestDailyStreak, d.dailyStreak);
                d.lastDailyDate = today;
                outcome.FirstToday = true;
                outcome.Reward = RewardForStreakDay(cfg, d.dailyStreak);
                d.coins += outcome.Reward;
                d.totalCoinsEarned += outcome.Reward;
            }
            outcome.StreakDay = d.dailyStreak;

            Prune(d, utcNow);
            return outcome;
        }

        static void Prune(SaveData d, DateTime utcNow)
        {
            int oldest = Key(utcNow.Date.AddDays(-KeepDays));
            d.dailyRecords.RemoveAll(r => r.date < oldest);
        }
    }
}
