using System.Collections.Generic;
using System.Linq;
using Xunit;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Tests.Puzzle;

public class PuzzleGeneratorTests
{
    private readonly PuzzleGenerator _generator = new();

    [Fact]
    public void Generate_ReturnsNonEmptyBagsAndReagents()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Tutorial1);
        var puzzle = _generator.Generate(12345, difficulty);

        Assert.NotEmpty(puzzle.Bags);
        Assert.NotEmpty(puzzle.Reagents);
        Assert.NotEmpty(puzzle.Solution);
    }

    [Fact]
    public void Generate_SameSeedProducesSamePuzzle()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);

        var puzzle1 = _generator.Generate(42, difficulty);
        var puzzle2 = _generator.Generate(42, difficulty);

        Assert.Equal(puzzle1.Bags.Count, puzzle2.Bags.Count);
        Assert.Equal(puzzle1.Reagents.Count, puzzle2.Reagents.Count);
        Assert.Equal(puzzle1.Solution.Count, puzzle2.Solution.Count);

        // Verify bag shapes are identical
        for (int i = 0; i < puzzle1.Bags.Count; i++)
        {
            Assert.Equal(puzzle1.Bags[i].PositionCol, puzzle2.Bags[i].PositionCol);
            Assert.Equal(puzzle1.Bags[i].PositionRow, puzzle2.Bags[i].PositionRow);
            AssertShapesEqual(puzzle1.Bags[i].Shape, puzzle2.Bags[i].Shape);
        }

        // Verify solution placements are identical
        for (int i = 0; i < puzzle1.Solution.Count; i++)
        {
            Assert.Equal(puzzle1.Solution[i].ReagentIndex, puzzle2.Solution[i].ReagentIndex);
            Assert.Equal(puzzle1.Solution[i].PositionCol, puzzle2.Solution[i].PositionCol);
            Assert.Equal(puzzle1.Solution[i].PositionRow, puzzle2.Solution[i].PositionRow);
            Assert.Equal(puzzle1.Solution[i].Rotation, puzzle2.Solution[i].Rotation);
        }
    }

    [Fact]
    public void Generate_DifferentSeedsProduceDifferentPuzzles()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);

        var puzzle1 = _generator.Generate(100, difficulty);
        var puzzle2 = _generator.Generate(200, difficulty);

        // At least one property should differ (bag positions, reagent shapes, or solution)
        bool anyDifference = puzzle1.Bags.Count != puzzle2.Bags.Count
            || puzzle1.Reagents.Count != puzzle2.Reagents.Count
            || puzzle1.Solution.Count != puzzle2.Solution.Count;

        if (!anyDifference && puzzle1.Solution.Count > 0)
        {
            anyDifference = puzzle1.Solution
                .Zip(puzzle2.Solution, (a, b) =>
                    a.PositionCol != b.PositionCol
                    || a.PositionRow != b.PositionRow
                    || a.Rotation != b.Rotation
                    || a.ReagentIndex != b.ReagentIndex)
                .Any(diff => diff);
        }

        Assert.True(anyDifference, "Different seeds should produce different puzzles");
    }

    [Fact]
    public void Generate_SolutionIsValid_NoNegativeReactions()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(999, difficulty);
        var rules = ReactionRuleDatabase.GetMvpRules();

        // Reconstruct the grid and place reagents according to the solution
        var grid = ReconstructGrid(puzzle);
        var reagents = ReconstructReagents(puzzle);

        foreach (var placement in puzzle.Solution)
        {
            var reagent = reagents[placement.ReagentIndex];
            reagent.Rotation = placement.Rotation;
            var pos = new Vector2I(placement.PositionCol, placement.PositionRow);
            bool placed = grid.PlaceReagent(reagent, pos);
            Assert.True(placed, $"Solution placement failed for reagent {placement.ReagentIndex} at ({placement.PositionCol}, {placement.PositionRow})");
        }

        // Evaluate — should have no reactions
        var reactions = ChemistryEngine.Evaluate(grid, rules);
        Assert.Empty(reactions);
    }

    [Fact]
    public void Generate_AllReagentsPlacedInSolution()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Tutorial1);
        var puzzle = _generator.Generate(777, difficulty);

        // Every reagent should appear in the solution exactly once
        var placedIndices = puzzle.Solution.Select(p => p.ReagentIndex).OrderBy(i => i).ToList();
        var allIndices = Enumerable.Range(0, puzzle.Reagents.Count).ToList();
        Assert.Equal(allIndices, placedIndices);
    }

    [Fact]
    public void Generate_SolutionPlacementsWithinBagBounds()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(555, difficulty);

        var grid = ReconstructGrid(puzzle);
        var reagents = ReconstructReagents(puzzle);

        foreach (var placement in puzzle.Solution)
        {
            var reagent = reagents[placement.ReagentIndex];
            reagent.Rotation = placement.Rotation;
            var pos = new Vector2I(placement.PositionCol, placement.PositionRow);

            // Every cell should be within bounds and in a bag
            foreach (var cellPos in reagent.GetMetaGridPositions(pos))
            {
                Assert.True(grid.IsInBounds(cellPos), $"Cell {cellPos} out of bounds");
                var cell = grid.GetCell(cellPos);
                Assert.NotNull(cell);
                Assert.True(cell!.IsInBag, $"Cell {cellPos} is not in a bag");
            }
        }
    }

    [Fact]
    public void Generate_NoOverlapsInSolution()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Medium);
        var puzzle = _generator.Generate(333, difficulty);

        var grid = ReconstructGrid(puzzle);
        var reagents = ReconstructReagents(puzzle);

        var occupiedCells = new HashSet<Vector2I>();
        foreach (var placement in puzzle.Solution)
        {
            var reagent = reagents[placement.ReagentIndex];
            reagent.Rotation = placement.Rotation;
            var pos = new Vector2I(placement.PositionCol, placement.PositionRow);

            foreach (var cellPos in reagent.GetMetaGridPositions(pos))
            {
                bool isNew = occupiedCells.Add(cellPos);
                Assert.True(isNew, $"Overlapping placement at {cellPos}");
            }
        }
    }

    [Fact]
    public void Generate_Tutorial1_HasNoAspects()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Tutorial1);
        var puzzle = _generator.Generate(111, difficulty);

        foreach (var reagent in puzzle.Reagents)
        {
            foreach (var cellAspects in reagent.AspectsPerCell)
            {
                Assert.Empty(cellAspects);
            }
        }
    }

    [Fact]
    public void Generate_SetsCorrectSeed()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(20260303, difficulty);

        Assert.Equal(20260303, puzzle.Seed);
    }

    [Fact]
    public void Generate_SetsPuzzleTypeFitAllSafely()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(20260303, difficulty);

        Assert.Equal(PuzzleType.FitAllSafely, puzzle.PuzzleType);
    }

    [Fact]
    public void Generate_EmptyPlacementsInitially()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy);
        var puzzle = _generator.Generate(20260303, difficulty);

        Assert.Empty(puzzle.Placements);
    }

    [Theory]
    [InlineData(DifficultyLevel.Tutorial1)]
    [InlineData(DifficultyLevel.Tutorial2)]
    [InlineData(DifficultyLevel.Easy)]
    [InlineData(DifficultyLevel.Medium)]
    public void Generate_SucceedsForMultipleDifficulties(DifficultyLevel level)
    {
        var difficulty = DifficultyParams.ForLevel(level);
        var puzzle = _generator.Generate(42, difficulty);

        Assert.NotNull(puzzle);
        Assert.NotEmpty(puzzle.Bags);
        Assert.NotEmpty(puzzle.Reagents);
        Assert.NotEmpty(puzzle.Solution);
    }

    [Fact]
    public void Generate_BagCountMatchesDifficulty()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Tutorial1);
        var puzzle = _generator.Generate(42, difficulty);

        Assert.Equal(difficulty.BagCount, puzzle.Bags.Count);
    }

    // --- Locked Reagent Tests ---

    [Fact]
    public void Generate_WithAllowSpecialBags_ProducesLockedReagents()
    {
        // Medium and Hard have AllowSpecialBags=true and enough reagents to lock
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Medium);

        // Try multiple seeds to find one that produces locked reagents
        bool foundLocked = false;
        for (int seed = 1; seed <= 20; seed++)
        {
            var puzzle = _generator.Generate(seed, difficulty);
            if (puzzle.Reagents.Any(r => r.IsLocked))
            {
                foundLocked = true;
                break;
            }
        }

        Assert.True(foundLocked, "Expected at least one seed to produce locked reagents with AllowSpecialBags=true");
    }

    [Fact]
    public void Generate_WithoutAllowSpecialBags_NoLockedReagents()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Easy); // AllowSpecialBags=false

        for (int seed = 1; seed <= 10; seed++)
        {
            var puzzle = _generator.Generate(seed, difficulty);
            Assert.DoesNotContain(puzzle.Reagents, r => r.IsLocked);
        }
    }

    [Fact]
    public void Generate_LockedReagentsSolutionIsStillValid()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Medium);

        for (int seed = 1; seed <= 10; seed++)
        {
            var puzzle = _generator.Generate(seed, difficulty);
            Assert.True(PuzzleSolver.IsSolvable(puzzle),
                $"Puzzle with seed={seed} should be solvable even with locked reagents");
        }
    }

    [Fact]
    public void Generate_LockedReagentCountNeverExceedsLimit()
    {
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Hard);

        for (int seed = 1; seed <= 20; seed++)
        {
            var puzzle = _generator.Generate(seed, difficulty);
            int lockedCount = puzzle.Reagents.Count(r => r.IsLocked);
            // Should lock at most 2 and never lock all reagents
            Assert.True(lockedCount <= 2, $"Seed {seed}: locked {lockedCount} reagents, expected <= 2");
            Assert.True(lockedCount < puzzle.Reagents.Count,
                $"Seed {seed}: all reagents locked, player has nothing to place");
        }
    }

    // --- Heavy in Gravity Bags ---

    [Fact]
    public void Generate_HeavyReagentsPlacedValidlyInGravityBags()
    {
        // Medium and Hard have AllowSpecialBags=true, which can produce Gravity bags.
        // The generator should place Heavy reagents before non-Heavy, so Heavy ends up
        // at the bottom. Verify the solution is valid by reconstructing and re-placing.
        var difficulty = DifficultyParams.ForLevel(DifficultyLevel.Hard);

        for (int seed = 1; seed <= 30; seed++)
        {
            var puzzle = _generator.Generate(seed, difficulty);
            var grid = ReconstructGrid(puzzle);
            var reagents = ReconstructReagents(puzzle);

            // Re-place reagents in solution order — should all succeed including Heavy constraint
            foreach (var placement in puzzle.Solution)
            {
                var reagent = reagents[placement.ReagentIndex];
                reagent.Rotation = placement.Rotation;
                var pos = new Vector2I(placement.PositionCol, placement.PositionRow);
                bool placed = grid.PlaceReagent(reagent, pos);
                Assert.True(placed, $"Seed {seed}: failed to place reagent {placement.ReagentIndex} at ({pos.Col},{pos.Row})");
            }
        }
    }

    // --- Helper methods ---

    private static MetaGrid ReconstructGrid(PuzzleState puzzle)
    {
        // Calculate grid size from bag positions and shapes
        int maxCol = 0, maxRow = 0;
        foreach (var bag in puzzle.Bags)
        {
            var shape = bag.ToShapeMatrix();
            maxCol = System.Math.Max(maxCol, bag.PositionCol + shape.GetLength(1));
            maxRow = System.Math.Max(maxRow, bag.PositionRow + shape.GetLength(0));
        }

        var grid = new MetaGrid(maxCol + 2, maxRow + 2);

        foreach (var puzzleBag in puzzle.Bags)
        {
            var shape = puzzleBag.ToShapeMatrix();
            var properties = new List<BagPropertyType>();
            foreach (var s in puzzleBag.Properties)
            {
                if (System.Enum.TryParse<BagPropertyType>(s, out var prop))
                    properties.Add(prop);
            }
            var def = new BagDefinition("bag", "Bag", shape, properties);
            var bag = new Bag(def);
            grid.PlaceBag(bag, new Vector2I(puzzleBag.PositionCol, puzzleBag.PositionRow));
        }

        return grid;
    }

    private static List<Reagent> ReconstructReagents(PuzzleState puzzle)
    {
        var reagents = new List<Reagent>();
        foreach (var pr in puzzle.Reagents)
        {
            var shape = pr.ToShapeMatrix();
            var aspects = new List<AspectSet>();
            foreach (var cellAspects in pr.AspectsPerCell)
            {
                var set = new AspectSet();
                foreach (var id in cellAspects)
                {
                    set.Add(Aspect.FromId(id));
                }
                aspects.Add(set);
            }

            var def = new ReagentDefinition("r", "R", shape, aspectsPerCell: aspects);
            reagents.Add(new Reagent(def));
        }
        return reagents;
    }

    private static void AssertShapesEqual(bool[][] a, bool[][] b)
    {
        Assert.Equal(a.Length, b.Length);
        for (int r = 0; r < a.Length; r++)
        {
            Assert.Equal(a[r].Length, b[r].Length);
            for (int c = 0; c < a[r].Length; c++)
            {
                Assert.Equal(a[r][c], b[r][c]);
            }
        }
    }
}
