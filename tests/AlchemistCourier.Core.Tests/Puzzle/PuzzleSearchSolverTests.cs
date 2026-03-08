using System.Collections.Generic;
using System.Threading;
using Xunit;
using AlchemistCourier.Core.Puzzle;

namespace AlchemistCourier.Core.Tests.Puzzle;

public class PuzzleSearchSolverTests
{
    private readonly PuzzleGenerator _generator = new();

    [Fact]
    public void Solve_KnownSolvablePuzzle_ReturnsSolution()
    {
        var puzzle = BuildSimpleSolvablePuzzle();
        var result = PuzzleSearchSolver.Solve(puzzle);

        Assert.NotNull(result);
        Assert.Equal(puzzle.Reagents.Count, result.Count);
    }

    [Fact]
    public void Solve_ReturnedSolution_PassesValidation()
    {
        var puzzle = BuildSimpleSolvablePuzzle();
        var result = PuzzleSearchSolver.Solve(puzzle);

        Assert.NotNull(result);
        puzzle.Placements = result;
        Assert.True(PuzzleSolver.ValidateSolution(puzzle));
    }

    [Fact]
    public void Solve_GeneratedPuzzle_FindsValidSolution()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Tutorial1);
        var puzzle = _generator.Generate(42, difficulty);

        // Clear placements so solver finds its own
        puzzle.Placements = new List<Placement>();

        var result = PuzzleSearchSolver.Solve(puzzle);

        Assert.NotNull(result);
        puzzle.Placements = result;
        Assert.True(PuzzleSolver.ValidateSolution(puzzle));
    }

    [Fact]
    public void Solve_UnsolvablePuzzle_ReturnsNull()
    {
        var puzzle = BuildUnsolvablePuzzle();
        var result = PuzzleSearchSolver.Solve(puzzle);

        Assert.Null(result);
    }

    [Fact]
    public void Solve_CancelledToken_ReturnsNull()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(42, difficulty);
        puzzle.Placements = new List<Placement>();

        // Pre-cancelled token
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = PuzzleSearchSolver.Solve(puzzle, cts.Token);
        Assert.Null(result);
    }

    [Fact]
    public void Solve_Timeout_ReturnsNull()
    {
        // Create a hard puzzle then give it an instant timeout
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Hard);
        var puzzle = _generator.Generate(42, difficulty);
        puzzle.Placements = new List<Placement>();

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Immediate cancellation simulates timeout

        var result = PuzzleSearchSolver.Solve(puzzle, cts.Token);
        Assert.Null(result);
    }

    [Fact]
    public void Solve_ReactiveReagents_FindsSafeArrangement()
    {
        // Puzzle with hot and volatile reagents that must be separated
        var puzzle = BuildReactiveSolvablePuzzle();
        var result = PuzzleSearchSolver.Solve(puzzle);

        Assert.NotNull(result);
        puzzle.Placements = result;
        Assert.True(PuzzleSolver.ValidateSolution(puzzle));
    }

    [Fact]
    public void CountSolutions_SmallPuzzle_ReturnsExpectedCount()
    {
        // One 1-cell reagent in a 3-cell bag, no aspects = 3 positions × 1 rotation (symmetric)
        // But rotations may produce duplicates; the count should be > 0
        var puzzle = BuildSingleReagentPuzzle();

        int count = PuzzleSearchSolver.CountSolutions(puzzle, 100);
        Assert.True(count > 0, "Should find at least one solution");
    }

    [Fact]
    public void CountSolutions_UnsolvablePuzzle_ReturnsZero()
    {
        var puzzle = BuildUnsolvablePuzzle();
        int count = PuzzleSearchSolver.CountSolutions(puzzle, 100);

        Assert.Equal(0, count);
    }

    [Fact]
    public void CountSolutions_RespectsMaxCount()
    {
        var puzzle = BuildSingleReagentPuzzle();

        int count = PuzzleSearchSolver.CountSolutions(puzzle, 1);
        Assert.Equal(1, count);
    }

    [Fact]
    public void CountSolutions_CancelledToken_ReturnsPartialCount()
    {
        var puzzle = BuildSingleReagentPuzzle();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        int count = PuzzleSearchSolver.CountSolutions(puzzle, 100, cts.Token);
        Assert.Equal(0, count);
    }

    [Fact]
    public void Solve_MultipleGeneratedSeeds_AllValid()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Tutorial1);
        foreach (var seed in new[] { 1, 42, 100, 999 })
        {
            var puzzle = _generator.Generate(seed, difficulty);
            puzzle.Placements = new List<Placement>();

            var result = PuzzleSearchSolver.Solve(puzzle);
            Assert.NotNull(result);

            puzzle.Placements = result;
            Assert.True(PuzzleSolver.ValidateSolution(puzzle), $"Solver solution invalid for seed {seed}");
        }
    }

    /// <summary>
    /// Simple solvable puzzle: two 1-cell reagents with no aspects in a 1x4 bag.
    /// </summary>
    private static PuzzleState BuildSimpleSolvablePuzzle()
    {
        var bagShape = new bool[,] { { true, true, true, true } };

        return new PuzzleState
        {
            Seed = 1,
            Difficulty = DifficultyLevel.Tutorial1,
            PuzzleType = PuzzleType.FitAllSafely,
            Bags = new List<PuzzleBag>
            {
                PuzzleBag.FromMatrix(bagShape, 1, 1),
            },
            Reagents = new List<PuzzleReagent>
            {
                PuzzleReagent.FromMatrix(new bool[,] { { true } }),
                PuzzleReagent.FromMatrix(new bool[,] { { true } }),
            },
            Placements = new List<Placement>(),
            Solution = new List<Placement>
            {
                new Placement { ReagentIndex = 0, PositionCol = 1, PositionRow = 1, Rotation = 0 },
                new Placement { ReagentIndex = 1, PositionCol = 2, PositionRow = 1, Rotation = 0 },
            },
        };
    }

    /// <summary>
    /// Unsolvable puzzle: two 1-cell reagents (hot + volatile) in a 1x2 bag.
    /// They must be adjacent which triggers an explosion.
    /// </summary>
    private static PuzzleState BuildUnsolvablePuzzle()
    {
        var bagShape = new bool[,] { { true, true } };

        return new PuzzleState
        {
            Seed = 1,
            Difficulty = DifficultyLevel.Tutorial1,
            PuzzleType = PuzzleType.FitAllSafely,
            Bags = new List<PuzzleBag>
            {
                PuzzleBag.FromMatrix(bagShape, 1, 1),
            },
            Reagents = new List<PuzzleReagent>
            {
                PuzzleReagent.FromMatrix(
                    new bool[,] { { true } },
                    aspectsPerCell: new List<List<string>> { new() { "hot" } }),
                PuzzleReagent.FromMatrix(
                    new bool[,] { { true } },
                    aspectsPerCell: new List<List<string>> { new() { "volatile" } }),
            },
            Placements = new List<Placement>(),
            Solution = new List<Placement>(),
        };
    }

    /// <summary>
    /// Solvable puzzle with reactive reagents: hot and volatile in a long bag
    /// with a stable buffer reagent that can be placed between them.
    /// Actually uses separation — bag is wide enough to keep them apart.
    /// </summary>
    private static PuzzleState BuildReactiveSolvablePuzzle()
    {
        // 1x5 bag: hot at one end, volatile at the other, safe reagent in between
        var bagShape = new bool[,] { { true, true, true, true, true } };

        return new PuzzleState
        {
            Seed = 1,
            Difficulty = DifficultyLevel.Tutorial1,
            PuzzleType = PuzzleType.FitAllSafely,
            Bags = new List<PuzzleBag>
            {
                PuzzleBag.FromMatrix(bagShape, 1, 1),
            },
            Reagents = new List<PuzzleReagent>
            {
                PuzzleReagent.FromMatrix(
                    new bool[,] { { true } },
                    aspectsPerCell: new List<List<string>> { new() { "hot" } }),
                PuzzleReagent.FromMatrix(
                    new bool[,] { { true } }),
                PuzzleReagent.FromMatrix(
                    new bool[,] { { true } },
                    aspectsPerCell: new List<List<string>> { new() { "volatile" } }),
            },
            Placements = new List<Placement>(),
            Solution = new List<Placement>
            {
                new Placement { ReagentIndex = 0, PositionCol = 1, PositionRow = 1, Rotation = 0 },
                new Placement { ReagentIndex = 1, PositionCol = 3, PositionRow = 1, Rotation = 0 },
                new Placement { ReagentIndex = 2, PositionCol = 5, PositionRow = 1, Rotation = 0 },
            },
        };
    }

    /// <summary>
    /// Single 1-cell reagent in a 1x3 bag — has multiple valid positions.
    /// </summary>
    private static PuzzleState BuildSingleReagentPuzzle()
    {
        var bagShape = new bool[,] { { true, true, true } };

        return new PuzzleState
        {
            Seed = 1,
            Difficulty = DifficultyLevel.Tutorial1,
            PuzzleType = PuzzleType.FitAllSafely,
            Bags = new List<PuzzleBag>
            {
                PuzzleBag.FromMatrix(bagShape, 1, 1),
            },
            Reagents = new List<PuzzleReagent>
            {
                PuzzleReagent.FromMatrix(new bool[,] { { true } }),
            },
            Placements = new List<Placement>(),
            Solution = new List<Placement>
            {
                new Placement { ReagentIndex = 0, PositionCol = 1, PositionRow = 1, Rotation = 0 },
            },
        };
    }

    // --- I.1: CountSolutions tests ---

    [Fact]
    public void CountSolutions_SimpleOneCellPuzzle_CountsCorrectly()
    {
        // 1 reagent (1-cell) in a 2x2 bag = 4 possible positions
        var bagShape = new bool[,] { { true, true }, { true, true } };
        var puzzle = new PuzzleState
        {
            Seed = 1,
            Difficulty = DifficultyLevel.Tutorial1,
            PuzzleType = PuzzleType.FitAllSafely,
            Bags = new List<PuzzleBag>
            {
                PuzzleBag.FromMatrix(bagShape, 0, 0),
            },
            Reagents = new List<PuzzleReagent>
            {
                PuzzleReagent.FromMatrix(new bool[,] { { true } }),
            },
            Placements = new List<Placement>(),
            Solution = new List<Placement>
            {
                new Placement { ReagentIndex = 0, PositionCol = 0, PositionRow = 0, Rotation = 0 },
            },
        };

        int count = PuzzleSearchSolver.CountSolutions(puzzle, 100);

        Assert.Equal(4, count); // 4 cells in the bag, 1-cell reagent can go in any
    }

    [Fact]
    public void CountSolutions_StopsAtMaxCount_FourCellBag()
    {
        // Same as above but maxCount=2 — should stop at 2
        var bagShape = new bool[,] { { true, true }, { true, true } };
        var puzzle = new PuzzleState
        {
            Seed = 1,
            Difficulty = DifficultyLevel.Tutorial1,
            PuzzleType = PuzzleType.FitAllSafely,
            Bags = new List<PuzzleBag>
            {
                PuzzleBag.FromMatrix(bagShape, 0, 0),
            },
            Reagents = new List<PuzzleReagent>
            {
                PuzzleReagent.FromMatrix(new bool[,] { { true } }),
            },
            Placements = new List<Placement>(),
            Solution = new List<Placement>
            {
                new Placement { ReagentIndex = 0, PositionCol = 0, PositionRow = 0, Rotation = 0 },
            },
        };

        int count = PuzzleSearchSolver.CountSolutions(puzzle, 2);

        Assert.Equal(2, count); // Stopped at maxCount
    }

    [Fact]
    public void CountSolutions_TightFitPuzzle_FewSolutions()
    {
        // 2 reagents that perfectly fill a 1x2 bag — should have exactly 2 solutions
        // (swap left/right)
        var bagShape = new bool[,] { { true, true } };
        var puzzle = new PuzzleState
        {
            Seed = 1,
            Difficulty = DifficultyLevel.Tutorial1,
            PuzzleType = PuzzleType.FitAllSafely,
            Bags = new List<PuzzleBag>
            {
                PuzzleBag.FromMatrix(bagShape, 0, 0),
            },
            Reagents = new List<PuzzleReagent>
            {
                PuzzleReagent.FromMatrix(new bool[,] { { true } }),
                PuzzleReagent.FromMatrix(new bool[,] { { true } }),
            },
            Placements = new List<Placement>(),
            Solution = new List<Placement>
            {
                new Placement { ReagentIndex = 0, PositionCol = 0, PositionRow = 0, Rotation = 0 },
                new Placement { ReagentIndex = 1, PositionCol = 1, PositionRow = 0, Rotation = 0 },
            },
        };

        int count = PuzzleSearchSolver.CountSolutions(puzzle, 100);

        Assert.Equal(2, count); // A,B or B,A
    }
}
