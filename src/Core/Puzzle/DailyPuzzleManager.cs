using System;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Manages daily puzzle generation. Generates deterministic puzzles from date seeds.
/// Difficulty rotates on a 3-day cycle (Easy/Medium/Hard).
/// Puzzle type varies by seed. All daily puzzles use Simple mechanical depth.
/// </summary>
public static class DailyPuzzleManager
{
    /// <summary>
    /// Compute the integer seed for a given date: YYYYMMDD.
    /// </summary>
    public static int DateToSeed(DateTime date)
    {
        return date.Year * 10000 + date.Month * 100 + date.Day;
    }

    /// <summary>
    /// Determine the daily difficulty based on date.
    /// Rotates on a 3-day cycle using the day-of-year: Easy, Medium, Hard.
    /// </summary>
    public static DifficultyLevel GetDailyDifficulty(DateTime date)
    {
        int dayOfYear = date.DayOfYear;
        int cycle = dayOfYear % 3;
        return cycle switch
        {
            0 => DifficultyLevel.Easy,
            1 => DifficultyLevel.Medium,
            2 => DifficultyLevel.Hard,
            _ => DifficultyLevel.Easy,
        };
    }

    /// <summary>
    /// Determine the daily puzzle type based on seed.
    /// Uses the seed to pick from the three puzzle types deterministically.
    /// </summary>
    public static PuzzleType GetDailyPuzzleType(int seed)
    {
        // Use a simple deterministic mapping from seed
        int index = Math.Abs(seed) % 3;
        return index switch
        {
            0 => PuzzleType.FitAllSafely,
            1 => PuzzleType.MaximizeValue,
            2 => PuzzleType.MeetRequirement,
            _ => PuzzleType.FitAllSafely,
        };
    }

    /// <summary>
    /// Generate today's daily puzzle for the given date.
    /// </summary>
    public static PuzzleState GenerateDailyPuzzle(DateTime date)
    {
        int seed = DateToSeed(date);
        var difficulty = GetDailyDifficulty(date);
        var difficultyParams = DifficultyParams.ForLevel(difficulty);
        var puzzleType = GetDailyPuzzleType(seed);

        var generator = new PuzzleGenerator();
        var puzzle = generator.Generate(seed, difficultyParams);
        puzzle.PuzzleType = puzzleType;
        return puzzle;
    }

    /// <summary>
    /// Generate today's daily puzzle using the current date.
    /// </summary>
    public static PuzzleState GenerateToday()
    {
        return GenerateDailyPuzzle(DateTime.UtcNow.Date);
    }
}
