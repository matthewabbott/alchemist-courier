using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using AlchemistCourier.Core.Puzzle;

namespace AlchemistCourier.Core.Tests.Puzzle;

public class PuzzleStateTests
{
    private static PuzzleState CreateSamplePuzzle()
    {
        return new PuzzleState
        {
            Seed = 20260303,
            Difficulty = DifficultyLevel.Easy,
            PuzzleType = PuzzleType.FitAllSafely,
            Bags = new List<PuzzleBag>
            {
                PuzzleBag.FromMatrix(new bool[,]
                {
                    { true, true, true },
                    { true, true, true },
                    { true, true, true },
                }, col: 0, row: 0),
                PuzzleBag.FromMatrix(new bool[,]
                {
                    { true, true },
                    { true, true },
                }, col: 4, row: 0, properties: new List<string> { "Null" }),
            },
            Reagents = new List<PuzzleReagent>
            {
                PuzzleReagent.FromMatrix(
                    new bool[,] { { true } },
                    aspectsPerCell: new List<List<string>> { new() { "hot" } },
                    weight: 1f, value: 2),
                PuzzleReagent.FromMatrix(
                    new bool[,] { { true, true }, { false, true } },
                    aspectsPerCell: new List<List<string>>
                    {
                        new() { "volatile" },
                        new() { "cold" },
                        new() { "toxic" },
                    },
                    weight: 2f, value: 5),
            },
            Placements = new List<Placement>
            {
                new() { ReagentIndex = 0, PositionCol = 1, PositionRow = 1, Rotation = 0 },
            },
            Solution = new List<Placement>
            {
                new() { ReagentIndex = 0, PositionCol = 1, PositionRow = 1, Rotation = 0 },
                new() { ReagentIndex = 1, PositionCol = 4, PositionRow = 0, Rotation = 1 },
            },
        };
    }

    [Fact]
    public void JsonRoundTrip_PreservesAllFields()
    {
        var original = CreateSamplePuzzle();

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<PuzzleState>(json)!;

        Assert.Equal(original.Seed, restored.Seed);
        Assert.Equal(original.Difficulty, restored.Difficulty);
        Assert.Equal(original.PuzzleType, restored.PuzzleType);
        Assert.Equal(original.Bags.Count, restored.Bags.Count);
        Assert.Equal(original.Reagents.Count, restored.Reagents.Count);
        Assert.Equal(original.Placements.Count, restored.Placements.Count);
        Assert.Equal(original.Solution.Count, restored.Solution.Count);
    }

    [Fact]
    public void JsonRoundTrip_PreservesBagData()
    {
        var original = CreateSamplePuzzle();

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<PuzzleState>(json)!;

        var bag0 = restored.Bags[0];
        Assert.Equal(3, bag0.Shape.Length);        // 3 rows
        Assert.Equal(3, bag0.Shape[0].Length);     // 3 cols
        Assert.True(bag0.Shape[1][2]);             // all cells true in 3x3
        Assert.Equal(0, bag0.PositionCol);
        Assert.Equal(0, bag0.PositionRow);
        Assert.Empty(bag0.Properties);

        var bag1 = restored.Bags[1];
        Assert.Equal(2, bag1.Shape.Length);
        Assert.Equal(4, bag1.PositionCol);
        Assert.Single(bag1.Properties);
        Assert.Equal("Null", bag1.Properties[0]);
    }

    [Fact]
    public void JsonRoundTrip_PreservesReagentData()
    {
        var original = CreateSamplePuzzle();

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<PuzzleState>(json)!;

        var reagent0 = restored.Reagents[0];
        Assert.Single(reagent0.Shape);               // 1 row
        Assert.Single(reagent0.Shape[0]);             // 1 col
        Assert.True(reagent0.Shape[0][0]);
        Assert.Single(reagent0.AspectsPerCell);
        Assert.Equal("hot", reagent0.AspectsPerCell[0][0]);
        Assert.Equal(1f, reagent0.Weight);
        Assert.Equal(2, reagent0.Value);
        Assert.False(reagent0.IsLocked);

        var reagent1 = restored.Reagents[1];
        Assert.Equal(3, reagent1.AspectsPerCell.Count);
        Assert.Equal("volatile", reagent1.AspectsPerCell[0][0]);
        Assert.Equal("toxic", reagent1.AspectsPerCell[2][0]);
    }

    [Fact]
    public void JsonRoundTrip_PreservesPlacementData()
    {
        var original = CreateSamplePuzzle();

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<PuzzleState>(json)!;

        var placement = restored.Placements[0];
        Assert.Equal(0, placement.ReagentIndex);
        Assert.Equal(1, placement.PositionCol);
        Assert.Equal(1, placement.PositionRow);
        Assert.Equal(0, placement.Rotation);

        var sol1 = restored.Solution[1];
        Assert.Equal(1, sol1.ReagentIndex);
        Assert.Equal(4, sol1.PositionCol);
        Assert.Equal(0, sol1.PositionRow);
        Assert.Equal(1, sol1.Rotation);
    }

    [Fact]
    public void PuzzleBag_ToShapeMatrix_ConvertsCorrectly()
    {
        var bag = PuzzleBag.FromMatrix(new bool[,]
        {
            { true, false },
            { true, true },
        }, col: 2, row: 3);

        var matrix = bag.ToShapeMatrix();
        Assert.Equal(2, matrix.GetLength(0));
        Assert.Equal(2, matrix.GetLength(1));
        Assert.True(matrix[0, 0]);
        Assert.False(matrix[0, 1]);
        Assert.True(matrix[1, 0]);
        Assert.True(matrix[1, 1]);
    }

    [Fact]
    public void PuzzleReagent_ToShapeMatrix_ConvertsCorrectly()
    {
        var reagent = PuzzleReagent.FromMatrix(new bool[,]
        {
            { true, true, true },
            { false, true, false },
        });

        var matrix = reagent.ToShapeMatrix();
        Assert.Equal(2, matrix.GetLength(0));
        Assert.Equal(3, matrix.GetLength(1));
        Assert.True(matrix[0, 0]);
        Assert.True(matrix[0, 2]);
        Assert.False(matrix[1, 0]);
        Assert.True(matrix[1, 1]);
    }

    [Fact]
    public void EmptyPuzzleState_SerializesAndDeserializes()
    {
        var original = new PuzzleState
        {
            Seed = 0,
            Difficulty = DifficultyLevel.Tutorial1,
            PuzzleType = PuzzleType.FitAllSafely,
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<PuzzleState>(json)!;

        Assert.Equal(0, restored.Seed);
        Assert.Equal(DifficultyLevel.Tutorial1, restored.Difficulty);
        Assert.Empty(restored.Bags);
        Assert.Empty(restored.Reagents);
        Assert.Empty(restored.Placements);
        Assert.Empty(restored.Solution);
    }

    [Fact]
    public void DifficultyParams_ForLevel_ReturnsCorrectValues()
    {
        var tutorial1 = DifficultyParams.ForLevel(DifficultyLevel.Tutorial1);
        Assert.Equal(1, tutorial1.BagCount);
        Assert.Equal(2, tutorial1.ReagentCount);
        Assert.Equal(0, tutorial1.MaxAspectCount);
        Assert.False(tutorial1.AllowSpecialBags);

        var hard = DifficultyParams.ForLevel(DifficultyLevel.Hard);
        Assert.Equal(4, hard.BagCount);
        Assert.Equal(12, hard.ReagentCount);
        Assert.Equal(4, hard.MaxAspectCount);
        Assert.True(hard.AllowSpecialBags);
    }

    [Fact]
    public void DifficultyParams_Constructor_SetsAllFields()
    {
        var p = new DifficultyParams(3, 4, 12, 8, 3, 0.25f, true);

        Assert.Equal(3, p.BagCount);
        Assert.Equal(4, p.BagMinSize);
        Assert.Equal(12, p.BagMaxSize);
        Assert.Equal(8, p.ReagentCount);
        Assert.Equal(3, p.MaxAspectCount);
        Assert.Equal(0.25f, p.SpareSpacePercent);
        Assert.True(p.AllowSpecialBags);
    }

    [Fact]
    public void DifficultyParams_JsonRoundTrip()
    {
        var original = DifficultyParams.ForLevel(DifficultyLevel.Medium);

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<DifficultyParams>(json)!;

        Assert.Equal(original.BagCount, restored.BagCount);
        Assert.Equal(original.BagMinSize, restored.BagMinSize);
        Assert.Equal(original.BagMaxSize, restored.BagMaxSize);
        Assert.Equal(original.ReagentCount, restored.ReagentCount);
        Assert.Equal(original.MaxAspectCount, restored.MaxAspectCount);
        Assert.Equal(original.SpareSpacePercent, restored.SpareSpacePercent);
        Assert.Equal(original.AllowSpecialBags, restored.AllowSpecialBags);
    }

    [Fact]
    public void PuzzleReagent_IsLocked_Serializes()
    {
        var reagent = PuzzleReagent.FromMatrix(
            new bool[,] { { true } },
            isLocked: true);

        var json = JsonSerializer.Serialize(reagent);
        var restored = JsonSerializer.Deserialize<PuzzleReagent>(json)!;

        Assert.True(restored.IsLocked);
    }

    [Fact]
    public void PuzzleBag_EmptyShape_HandledGracefully()
    {
        var bag = new PuzzleBag();
        var matrix = bag.ToShapeMatrix();
        Assert.Equal(0, matrix.GetLength(0));
        Assert.Equal(0, matrix.GetLength(1));
    }
}
