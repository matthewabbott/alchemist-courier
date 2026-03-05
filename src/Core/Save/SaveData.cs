using System;
using System.Collections.Generic;
using System.Linq;

namespace AlchemistCourier.Core.Save;

/// <summary>
/// Persistent save data: tutorial progress, daily completion history, and streak tracking.
/// </summary>
public class SaveData
{
    /// <summary>
    /// The highest completed tutorial lesson number (0 = none completed).
    /// </summary>
    public int TutorialLessonCompleted { get; set; }

    /// <summary>
    /// Set of dates (YYYYMMDD format) where the daily puzzle was completed.
    /// </summary>
    public List<int> CompletedDailies { get; set; } = new();

    /// <summary>
    /// Current streak count (consecutive days of daily puzzle completion).
    /// </summary>
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Last date a daily puzzle was completed (YYYYMMDD format), or 0 if never.
    /// </summary>
    public int LastCompletedDate { get; set; }

    /// <summary>
    /// Record completion of a tutorial lesson.
    /// </summary>
    public void CompleteTutorialLesson(int lessonNumber)
    {
        if (lessonNumber > TutorialLessonCompleted)
            TutorialLessonCompleted = lessonNumber;
    }

    /// <summary>
    /// Record completion of a daily puzzle for the given date.
    /// Updates streak tracking.
    /// </summary>
    public void CompleteDailyPuzzle(DateTime date)
    {
        int dateSeed = DateToSeed(date);

        if (CompletedDailies.Contains(dateSeed))
            return; // Already completed

        CompletedDailies.Add(dateSeed);

        // Update streak
        if (LastCompletedDate == 0)
        {
            // First ever completion
            CurrentStreak = 1;
        }
        else
        {
            DateTime lastDate = SeedToDate(LastCompletedDate);
            int daysDiff = (date.Date - lastDate.Date).Days;

            if (daysDiff == 1)
            {
                // Consecutive day — extend streak
                CurrentStreak++;
            }
            else if (daysDiff > 1)
            {
                // Streak broken — reset to 1
                CurrentStreak = 1;
            }
            // daysDiff == 0 handled by early return above
            // daysDiff < 0 (completing an older date) — don't update streak
        }

        if (dateSeed > LastCompletedDate)
            LastCompletedDate = dateSeed;
    }

    /// <summary>
    /// Check if a specific daily puzzle has been completed.
    /// </summary>
    public bool IsDailyCompleted(DateTime date)
    {
        return CompletedDailies.Contains(DateToSeed(date));
    }

    /// <summary>
    /// Check if the tutorial is fully completed (all 5 lessons).
    /// </summary>
    public bool IsTutorialCompleted()
    {
        return TutorialLessonCompleted >= 5;
    }

    /// <summary>
    /// Get the next tutorial lesson number to play (1-indexed), or 0 if tutorial is complete.
    /// </summary>
    public int GetNextTutorialLesson()
    {
        if (IsTutorialCompleted())
            return 0;
        return TutorialLessonCompleted + 1;
    }

    /// <summary>
    /// Calculate the current streak based on the provided "today" date.
    /// If the player hasn't played today or yesterday, streak may be stale.
    /// Returns the effective streak (0 if broken).
    /// </summary>
    public int GetEffectiveStreak(DateTime today)
    {
        if (LastCompletedDate == 0)
            return 0;

        DateTime lastDate = SeedToDate(LastCompletedDate);
        int daysDiff = (today.Date - lastDate.Date).Days;

        if (daysDiff <= 1)
            return CurrentStreak; // Today or yesterday — streak is live

        return 0; // More than 1 day gap — streak is broken
    }

    private static int DateToSeed(DateTime date)
    {
        return date.Year * 10000 + date.Month * 100 + date.Day;
    }

    private static DateTime SeedToDate(int seed)
    {
        int year = seed / 10000;
        int month = (seed / 100) % 100;
        int day = seed % 100;
        return new DateTime(year, month, day);
    }
}
