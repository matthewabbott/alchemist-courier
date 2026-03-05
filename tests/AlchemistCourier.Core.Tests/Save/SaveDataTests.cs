using System;
using System.Collections.Generic;
using AlchemistCourier.Core.Save;
using Xunit;

namespace AlchemistCourier.Core.Tests.Save;

public class SaveDataTests
{
    [Fact]
    public void NewSaveData_HasDefaultValues()
    {
        var data = new SaveData();

        Assert.Equal(0, data.TutorialLessonCompleted);
        Assert.Empty(data.CompletedDailies);
        Assert.Equal(0, data.CurrentStreak);
        Assert.Equal(0, data.LastCompletedDate);
    }

    [Fact]
    public void CompleteTutorialLesson_UpdatesHighestCompleted()
    {
        var data = new SaveData();

        data.CompleteTutorialLesson(1);
        Assert.Equal(1, data.TutorialLessonCompleted);

        data.CompleteTutorialLesson(3);
        Assert.Equal(3, data.TutorialLessonCompleted);
    }

    [Fact]
    public void CompleteTutorialLesson_DoesNotRegress()
    {
        var data = new SaveData();

        data.CompleteTutorialLesson(3);
        data.CompleteTutorialLesson(1);

        Assert.Equal(3, data.TutorialLessonCompleted);
    }

    [Fact]
    public void IsTutorialCompleted_FalseWhenIncomplete()
    {
        var data = new SaveData();
        data.CompleteTutorialLesson(4);

        Assert.False(data.IsTutorialCompleted());
    }

    [Fact]
    public void IsTutorialCompleted_TrueWhenAllFiveDone()
    {
        var data = new SaveData();
        data.CompleteTutorialLesson(5);

        Assert.True(data.IsTutorialCompleted());
    }

    [Fact]
    public void IsTutorialCompleted_TrueWhenBeyondFive()
    {
        var data = new SaveData();
        data.CompleteTutorialLesson(7);

        Assert.True(data.IsTutorialCompleted());
    }

    [Fact]
    public void GetNextTutorialLesson_ReturnsNextLesson()
    {
        var data = new SaveData();
        Assert.Equal(1, data.GetNextTutorialLesson());

        data.CompleteTutorialLesson(2);
        Assert.Equal(3, data.GetNextTutorialLesson());
    }

    [Fact]
    public void GetNextTutorialLesson_ReturnsZeroWhenComplete()
    {
        var data = new SaveData();
        data.CompleteTutorialLesson(5);

        Assert.Equal(0, data.GetNextTutorialLesson());
    }

    [Fact]
    public void CompleteDailyPuzzle_FirstCompletion_StartsStreak()
    {
        var data = new SaveData();
        var date = new DateTime(2026, 3, 4);

        data.CompleteDailyPuzzle(date);

        Assert.Equal(1, data.CurrentStreak);
        Assert.Equal(20260304, data.LastCompletedDate);
        Assert.Single(data.CompletedDailies);
        Assert.Contains(20260304, data.CompletedDailies);
    }

    [Fact]
    public void CompleteDailyPuzzle_ConsecutiveDays_ExtendsStreak()
    {
        var data = new SaveData();

        data.CompleteDailyPuzzle(new DateTime(2026, 3, 1));
        data.CompleteDailyPuzzle(new DateTime(2026, 3, 2));
        data.CompleteDailyPuzzle(new DateTime(2026, 3, 3));

        Assert.Equal(3, data.CurrentStreak);
        Assert.Equal(20260303, data.LastCompletedDate);
        Assert.Equal(3, data.CompletedDailies.Count);
    }

    [Fact]
    public void CompleteDailyPuzzle_GapBreaksStreak()
    {
        var data = new SaveData();

        data.CompleteDailyPuzzle(new DateTime(2026, 3, 1));
        data.CompleteDailyPuzzle(new DateTime(2026, 3, 2));
        Assert.Equal(2, data.CurrentStreak);

        // Skip March 3
        data.CompleteDailyPuzzle(new DateTime(2026, 3, 4));
        Assert.Equal(1, data.CurrentStreak);
    }

    [Fact]
    public void CompleteDailyPuzzle_DuplicateDate_NoChange()
    {
        var data = new SaveData();
        var date = new DateTime(2026, 3, 4);

        data.CompleteDailyPuzzle(date);
        data.CompleteDailyPuzzle(date);

        Assert.Equal(1, data.CurrentStreak);
        Assert.Single(data.CompletedDailies);
    }

    [Fact]
    public void IsDailyCompleted_TrueForCompletedDate()
    {
        var data = new SaveData();
        var date = new DateTime(2026, 3, 4);

        data.CompleteDailyPuzzle(date);

        Assert.True(data.IsDailyCompleted(date));
    }

    [Fact]
    public void IsDailyCompleted_FalseForUncompletedDate()
    {
        var data = new SaveData();

        Assert.False(data.IsDailyCompleted(new DateTime(2026, 3, 4)));
    }

    [Fact]
    public void GetEffectiveStreak_ReturnsStreakWhenPlayedToday()
    {
        var data = new SaveData();

        data.CompleteDailyPuzzle(new DateTime(2026, 3, 3));
        data.CompleteDailyPuzzle(new DateTime(2026, 3, 4));

        Assert.Equal(2, data.GetEffectiveStreak(new DateTime(2026, 3, 4)));
    }

    [Fact]
    public void GetEffectiveStreak_ReturnsStreakWhenPlayedYesterday()
    {
        var data = new SaveData();

        data.CompleteDailyPuzzle(new DateTime(2026, 3, 3));
        data.CompleteDailyPuzzle(new DateTime(2026, 3, 4));

        // Checking on March 5 — yesterday was last played
        Assert.Equal(2, data.GetEffectiveStreak(new DateTime(2026, 3, 5)));
    }

    [Fact]
    public void GetEffectiveStreak_ReturnsZeroWhenStreakBroken()
    {
        var data = new SaveData();

        data.CompleteDailyPuzzle(new DateTime(2026, 3, 3));

        // Checking on March 5 — two days since last play
        Assert.Equal(0, data.GetEffectiveStreak(new DateTime(2026, 3, 5)));
    }

    [Fact]
    public void GetEffectiveStreak_ReturnsZeroWhenNeverPlayed()
    {
        var data = new SaveData();

        Assert.Equal(0, data.GetEffectiveStreak(new DateTime(2026, 3, 4)));
    }

    [Fact]
    public void CompleteDailyPuzzle_OlderDate_DoesNotUpdateLastCompleted()
    {
        var data = new SaveData();

        data.CompleteDailyPuzzle(new DateTime(2026, 3, 4));
        data.CompleteDailyPuzzle(new DateTime(2026, 3, 2)); // older

        Assert.Equal(20260304, data.LastCompletedDate);
        Assert.Equal(2, data.CompletedDailies.Count);
    }

    [Fact]
    public void CompleteDailyPuzzle_MonthBoundary_ConsecutiveStreak()
    {
        var data = new SaveData();

        data.CompleteDailyPuzzle(new DateTime(2026, 2, 28));
        data.CompleteDailyPuzzle(new DateTime(2026, 3, 1));

        Assert.Equal(2, data.CurrentStreak);
    }

    [Fact]
    public void CompleteDailyPuzzle_LongStreak()
    {
        var data = new SaveData();
        var start = new DateTime(2026, 3, 1);

        for (int i = 0; i < 10; i++)
        {
            data.CompleteDailyPuzzle(start.AddDays(i));
        }

        Assert.Equal(10, data.CurrentStreak);
        Assert.Equal(10, data.CompletedDailies.Count);
    }
}
