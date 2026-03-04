using System.Collections.Generic;
using System.Linq;
using Xunit;
using AlchemistCourier.Core.Puzzle;

namespace AlchemistCourier.Core.Tests.Puzzle;

public class PuzzleSolverTests
{
    private readonly PuzzleGenerator _generator = new();

    [Fact]
    public void ValidateSolution_ValidPlacements_ReturnsTrue()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(42, difficulty);

        // Copy solution into placements
        puzzle.Placements = new List<Placement>(puzzle.Solution);

        Assert.True(PuzzleSolver.ValidateSolution(puzzle));
    }

    [Fact]
    public void ValidateSolution_EmptyPlacements_ReturnsFalse()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(42, difficulty);

        // Placements is empty by default
        Assert.False(PuzzleSolver.ValidateSolution(puzzle));
    }

    [Fact]
    public void ValidateSolution_MissingReagent_ReturnsFalse()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(42, difficulty);

        // Place all but the last reagent
        puzzle.Placements = puzzle.Solution.Take(puzzle.Solution.Count - 1).ToList();

        Assert.False(PuzzleSolver.ValidateSolution(puzzle));
    }

    [Fact]
    public void ValidateSolution_OverlappingPlacements_ReturnsFalse()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(42, difficulty);

        // Copy the solution, then make two reagents overlap by giving them the same position
        puzzle.Placements = new List<Placement>(puzzle.Solution);
        if (puzzle.Placements.Count >= 2)
        {
            puzzle.Placements[1] = new Placement
            {
                ReagentIndex = puzzle.Solution[1].ReagentIndex,
                PositionCol = puzzle.Solution[0].PositionCol,
                PositionRow = puzzle.Solution[0].PositionRow,
                Rotation = puzzle.Solution[0].Rotation,
            };
        }

        Assert.False(PuzzleSolver.ValidateSolution(puzzle));
    }

    [Fact]
    public void ValidateSolution_PlacementOutsideBag_ReturnsFalse()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(42, difficulty);

        // Place all at solution positions except move one to position (0,0) which is outside all bags
        puzzle.Placements = new List<Placement>(puzzle.Solution);
        puzzle.Placements[0] = new Placement
        {
            ReagentIndex = puzzle.Solution[0].ReagentIndex,
            PositionCol = 0,
            PositionRow = 0,
            Rotation = 0,
        };

        Assert.False(PuzzleSolver.ValidateSolution(puzzle));
    }

    [Fact]
    public void ValidateSolution_WithReaction_ReturnsFalse()
    {
        // Build a minimal puzzle with two reagents that would cause a reaction if adjacent
        var puzzle = BuildReactivePuzzle();

        // Place the two reagents adjacent to each other
        puzzle.Placements = new List<Placement>
        {
            new Placement { ReagentIndex = 0, PositionCol = 1, PositionRow = 1, Rotation = 0 },
            new Placement { ReagentIndex = 1, PositionCol = 2, PositionRow = 1, Rotation = 0 },
        };

        Assert.False(PuzzleSolver.ValidateSolution(puzzle));
    }

    [Fact]
    public void IsSolvable_GeneratedPuzzle_ReturnsTrue()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(999, difficulty);

        Assert.True(PuzzleSolver.IsSolvable(puzzle));
    }

    [Fact]
    public void IsSolvable_EmptySolution_ReturnsFalse()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(42, difficulty);
        puzzle.Solution = new List<Placement>();

        Assert.False(PuzzleSolver.IsSolvable(puzzle));
    }

    [Fact]
    public void IsSolvable_MultipleDifficulties()
    {
        var levels = new[] { DifficultyLevel.Tutorial1, DifficultyLevel.Easy, DifficultyLevel.Medium };
        foreach (var level in levels)
        {
            var difficulty = DifficultyParams.ForLevel(level);
            var puzzle = _generator.Generate(42, difficulty);

            Assert.True(PuzzleSolver.IsSolvable(puzzle), $"IsSolvable failed for {level}");
        }
    }

    [Fact]
    public void ValidateSolution_DuplicateReagentIndex_ReturnsFalse()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(42, difficulty);

        // Create placements with a duplicate reagent index
        puzzle.Placements = new List<Placement>(puzzle.Solution);
        if (puzzle.Placements.Count >= 2)
        {
            puzzle.Placements[1] = new Placement
            {
                ReagentIndex = puzzle.Solution[0].ReagentIndex,
                PositionCol = puzzle.Solution[1].PositionCol,
                PositionRow = puzzle.Solution[1].PositionRow,
                Rotation = puzzle.Solution[1].Rotation,
            };
        }

        Assert.False(PuzzleSolver.ValidateSolution(puzzle));
    }

    [Fact]
    public void ValidateSolution_InvalidReagentIndex_ReturnsFalse()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(42, difficulty);

        puzzle.Placements = new List<Placement>(puzzle.Solution);
        puzzle.Placements[0] = new Placement
        {
            ReagentIndex = 999,
            PositionCol = puzzle.Solution[0].PositionCol,
            PositionRow = puzzle.Solution[0].PositionRow,
            Rotation = 0,
        };

        Assert.False(PuzzleSolver.ValidateSolution(puzzle));
    }

    /// <summary>
    /// Builds a minimal puzzle with hot + volatile reagents in a single bag.
    /// Adjacent placement triggers an Explosion reaction.
    /// </summary>
    private static PuzzleState BuildReactivePuzzle()
    {
        var bagShape = new bool[,] { { true, true, true, true, true } };

        return new PuzzleState
        {
            Seed = 1,
            Difficulty = DifficultyLevel.Easy,
            PuzzleType = PuzzleType.FitAllSafely,
            Bags = new List<PuzzleBag>
            {
                PuzzleBag.FromMatrix(bagShape, 1, 1),
            },
            Reagents = new List<PuzzleReagent>
            {
                // Hot reagent (1 cell)
                PuzzleReagent.FromMatrix(
                    new bool[,] { { true } },
                    aspectsPerCell: new List<List<string>> { new() { "hot" } }),
                // Volatile reagent (1 cell)
                PuzzleReagent.FromMatrix(
                    new bool[,] { { true } },
                    aspectsPerCell: new List<List<string>> { new() { "volatile" } }),
            },
            Placements = new List<Placement>(),
            Solution = new List<Placement>(),
        };
    }
}
