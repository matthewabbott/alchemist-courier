using System;
using System.Collections.Generic;
using AlchemistCourier.Core.Puzzle;
using Xunit;

namespace AlchemistCourier.Core.Tests.Puzzle;

public class DailyPuzzleManagerTests
{
    [Fact]
    public void DateToSeed_ReturnsYYYYMMDD()
    {
        var date = new DateTime(2026, 3, 4);

        int seed = DailyPuzzleManager.DateToSeed(date);

        Assert.Equal(20260304, seed);
    }

    [Fact]
    public void DateToSeed_JanuaryFirst()
    {
        var date = new DateTime(2026, 1, 1);

        int seed = DailyPuzzleManager.DateToSeed(date);

        Assert.Equal(20260101, seed);
    }

    [Fact]
    public void DateToSeed_December31()
    {
        var date = new DateTime(2026, 12, 31);

        int seed = DailyPuzzleManager.DateToSeed(date);

        Assert.Equal(20261231, seed);
    }

    [Fact]
    public void SameDateProducesSamePuzzle()
    {
        var date = new DateTime(2026, 3, 4);

        var puzzle1 = DailyPuzzleManager.GenerateDailyPuzzle(date);
        var puzzle2 = DailyPuzzleManager.GenerateDailyPuzzle(date);

        Assert.Equal(puzzle1.Seed, puzzle2.Seed);
        Assert.Equal(puzzle1.Difficulty, puzzle2.Difficulty);
        Assert.Equal(puzzle1.PuzzleType, puzzle2.PuzzleType);
        Assert.Equal(puzzle1.Bags.Count, puzzle2.Bags.Count);
        Assert.Equal(puzzle1.Reagents.Count, puzzle2.Reagents.Count);
        Assert.Equal(puzzle1.Solution.Count, puzzle2.Solution.Count);
    }

    [Fact]
    public void DifferentDatesProduceDifferentPuzzles()
    {
        var date1 = new DateTime(2026, 3, 4);
        var date2 = new DateTime(2026, 3, 5);

        var puzzle1 = DailyPuzzleManager.GenerateDailyPuzzle(date1);
        var puzzle2 = DailyPuzzleManager.GenerateDailyPuzzle(date2);

        Assert.NotEqual(puzzle1.Seed, puzzle2.Seed);
    }

    [Fact]
    public void AllDailyPuzzlesUseSimpleComplexity()
    {
        // Test across several dates covering all 3 difficulty levels
        for (int day = 1; day <= 10; day++)
        {
            var date = new DateTime(2026, 3, day);
            var difficulty = DailyPuzzleManager.GetDailyDifficulty(date);
            var difficultyParams = DifficultyParams.ForLevel(difficulty);

            Assert.Equal(ComplexityLevel.Simple, difficultyParams.ComplexityLevel);
        }
    }

    [Fact]
    public void DifficultyRotatesOn3DayCycle()
    {
        // Collect difficulties for 9 consecutive days to verify 3-day cycle
        var difficulties = new List<DifficultyLevel>();
        for (int day = 1; day <= 9; day++)
        {
            var date = new DateTime(2026, 3, day);
            difficulties.Add(DailyPuzzleManager.GetDailyDifficulty(date));
        }

        // Each set of 3 consecutive days (aligned to cycle) should contain Easy, Medium, Hard
        var allThree = new HashSet<DifficultyLevel> { DifficultyLevel.Easy, DifficultyLevel.Medium, DifficultyLevel.Hard };

        // Verify only Easy/Medium/Hard are used (no tutorials)
        foreach (var diff in difficulties)
        {
            Assert.Contains(diff, allThree);
        }

        // Verify all three appear across 9 days
        var unique = new HashSet<DifficultyLevel>(difficulties);
        Assert.Equal(3, unique.Count);
    }

    [Fact]
    public void PuzzleTypeVariesBySeed()
    {
        // Test that different seeds can produce different puzzle types
        var types = new HashSet<PuzzleType>();
        for (int day = 1; day <= 30; day++)
        {
            var date = new DateTime(2026, 3, day);
            int seed = DailyPuzzleManager.DateToSeed(date);
            types.Add(DailyPuzzleManager.GetDailyPuzzleType(seed));
        }

        // With 30 different seeds modulo 3, all 3 types should appear
        Assert.Equal(3, types.Count);
    }

    [Fact]
    public void GeneratedPuzzleHasCorrectSeed()
    {
        var date = new DateTime(2026, 6, 15);

        var puzzle = DailyPuzzleManager.GenerateDailyPuzzle(date);

        Assert.Equal(20260615, puzzle.Seed);
    }

    [Fact]
    public void GeneratedPuzzleHasCorrectDifficulty()
    {
        var date = new DateTime(2026, 6, 15);
        var expectedDifficulty = DailyPuzzleManager.GetDailyDifficulty(date);

        var puzzle = DailyPuzzleManager.GenerateDailyPuzzle(date);

        Assert.Equal(expectedDifficulty, puzzle.Difficulty);
    }

    [Fact]
    public void GeneratedPuzzleHasCorrectPuzzleType()
    {
        var date = new DateTime(2026, 6, 15);
        int seed = DailyPuzzleManager.DateToSeed(date);
        var expectedType = DailyPuzzleManager.GetDailyPuzzleType(seed);

        var puzzle = DailyPuzzleManager.GenerateDailyPuzzle(date);

        Assert.Equal(expectedType, puzzle.PuzzleType);
    }

    [Fact]
    public void GeneratedPuzzleIsSolvable()
    {
        // Test several dates to ensure generated puzzles are all solvable
        for (int day = 1; day <= 10; day++)
        {
            var date = new DateTime(2026, 4, day);
            var puzzle = DailyPuzzleManager.GenerateDailyPuzzle(date);

            Assert.True(PuzzleSolver.IsSolvable(puzzle), $"Puzzle for 2026-04-{day:D2} should be solvable");
        }
    }

    [Fact]
    public void GeneratedPuzzleHasNonEmptyBagsAndReagents()
    {
        var date = new DateTime(2026, 5, 20);

        var puzzle = DailyPuzzleManager.GenerateDailyPuzzle(date);

        Assert.NotEmpty(puzzle.Bags);
        Assert.NotEmpty(puzzle.Reagents);
        Assert.NotEmpty(puzzle.Solution);
    }

    [Fact]
    public void GetDailyPuzzleType_IsDeterministic()
    {
        int seed = 20260304;

        var type1 = DailyPuzzleManager.GetDailyPuzzleType(seed);
        var type2 = DailyPuzzleManager.GetDailyPuzzleType(seed);

        Assert.Equal(type1, type2);
    }
}
