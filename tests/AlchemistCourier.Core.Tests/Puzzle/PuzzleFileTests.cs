using System;
using System.Collections.Generic;
using System.IO;
using AlchemistCourier.Core.Puzzle;
using Xunit;

namespace AlchemistCourier.Core.Tests.Puzzle;

public class PuzzleFileTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testPath;

    public PuzzleFileTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "alchemist_courier_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _testPath = Path.Combine(_testDir, "test_puzzle.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    private static PuzzleFile CreateSamplePuzzleFile()
    {
        var shape2x2 = new bool[][] { new[] { true, true }, new[] { true, false } };
        return new PuzzleFile
        {
            Name = "Test Puzzle",
            Description = "A test puzzle for unit tests",
            Author = "TestBot",
            CreatedAt = new DateTime(2026, 3, 5, 12, 0, 0, DateTimeKind.Utc),
            Puzzle = new PuzzleState
            {
                Seed = 42,
                Difficulty = DifficultyLevel.Easy,
                PuzzleType = PuzzleType.FitAllSafely,
                Bags = new List<PuzzleBag>
                {
                    new PuzzleBag
                    {
                        Shape = new bool[][] { new[] { true, true, true }, new[] { true, true, true } },
                        PositionCol = 0,
                        PositionRow = 0,
                        Properties = new List<string> { "Null" },
                    }
                },
                Reagents = new List<PuzzleReagent>
                {
                    new PuzzleReagent
                    {
                        Shape = shape2x2,
                        AspectsPerCell = new List<List<string>>
                        {
                            new List<string> { "hot" },
                            new List<string> { "cold" },
                            new List<string> { "volatile" },
                        },
                        Weight = 2.5f,
                        Value = 10,
                        IsLocked = false,
                    }
                },
                MaxHints = 3,
            }
        };
    }

    [Fact]
    public void SaveAndLoad_RoundTrip()
    {
        var original = CreateSamplePuzzleFile();

        PuzzleFile.Save(original, _testPath);
        var loaded = PuzzleFile.Load(_testPath);

        Assert.NotNull(loaded);
        Assert.Equal("Test Puzzle", loaded.Name);
        Assert.Equal("A test puzzle for unit tests", loaded.Description);
        Assert.Equal("TestBot", loaded.Author);
        Assert.Equal(original.CreatedAt, loaded.CreatedAt);
        Assert.Equal(42, loaded.Puzzle.Seed);
        Assert.Equal(DifficultyLevel.Easy, loaded.Puzzle.Difficulty);
        Assert.Equal(PuzzleType.FitAllSafely, loaded.Puzzle.PuzzleType);
        Assert.Single(loaded.Puzzle.Bags);
        Assert.Single(loaded.Puzzle.Reagents);
        Assert.Equal(3, loaded.Puzzle.MaxHints);
        Assert.Equal(2.5f, loaded.Puzzle.Reagents[0].Weight);
        Assert.Equal(10, loaded.Puzzle.Reagents[0].Value);
        Assert.Equal(3, loaded.Puzzle.Reagents[0].AspectsPerCell.Count);
        Assert.Contains("hot", loaded.Puzzle.Reagents[0].AspectsPerCell[0]);
    }

    [Fact]
    public void SaveToStringAndLoadFromString_RoundTrip()
    {
        var original = CreateSamplePuzzleFile();

        string json = PuzzleFile.SaveToString(original);
        var loaded = PuzzleFile.LoadFromString(json);

        Assert.NotNull(loaded);
        Assert.Equal("Test Puzzle", loaded.Name);
        Assert.Equal("TestBot", loaded.Author);
        Assert.Equal(42, loaded.Puzzle.Seed);
        Assert.Single(loaded.Puzzle.Bags);
        Assert.Contains("Null", loaded.Puzzle.Bags[0].Properties);
    }

    [Fact]
    public void NullOptionalFields_RoundTrip()
    {
        var file = new PuzzleFile
        {
            Name = "Minimal",
            Description = null,
            Author = null,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Puzzle = new PuzzleState { Seed = 1 },
        };

        string json = PuzzleFile.SaveToString(file);
        var loaded = PuzzleFile.LoadFromString(json);

        Assert.NotNull(loaded);
        Assert.Equal("Minimal", loaded.Name);
        Assert.Null(loaded.Description);
        Assert.Null(loaded.Author);
        Assert.Equal(1, loaded.Puzzle.Seed);
    }

    [Fact]
    public void Load_FileDoesNotExist_ReturnsNull()
    {
        var loaded = PuzzleFile.Load(Path.Combine(_testDir, "nonexistent.json"));

        Assert.Null(loaded);
    }

    [Fact]
    public void Load_InvalidJson_ReturnsNull()
    {
        File.WriteAllText(_testPath, "not valid json {{{");

        var loaded = PuzzleFile.Load(_testPath);

        Assert.Null(loaded);
    }

    [Fact]
    public void LoadFromString_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(PuzzleFile.LoadFromString(null));
        Assert.Null(PuzzleFile.LoadFromString(""));
    }

    [Fact]
    public void LoadFromString_InvalidJson_ReturnsNull()
    {
        Assert.Null(PuzzleFile.LoadFromString("{garbage}}}"));
    }

    [Fact]
    public void Save_CreatesDirectory()
    {
        string nestedPath = Path.Combine(_testDir, "sub", "deep", "puzzle.json");
        var file = new PuzzleFile
        {
            Name = "Nested",
            CreatedAt = DateTime.UtcNow,
            Puzzle = new PuzzleState(),
        };

        PuzzleFile.Save(file, nestedPath);

        Assert.True(File.Exists(nestedPath));
        var loaded = PuzzleFile.Load(nestedPath);
        Assert.NotNull(loaded);
        Assert.Equal("Nested", loaded.Name);
    }

    [Fact]
    public void Save_OverwritesPreviousFile()
    {
        var file1 = new PuzzleFile { Name = "First", CreatedAt = DateTime.UtcNow, Puzzle = new PuzzleState() };
        var file2 = new PuzzleFile { Name = "Second", CreatedAt = DateTime.UtcNow, Puzzle = new PuzzleState() };

        PuzzleFile.Save(file1, _testPath);
        PuzzleFile.Save(file2, _testPath);

        var loaded = PuzzleFile.Load(_testPath);
        Assert.NotNull(loaded);
        Assert.Equal("Second", loaded.Name);
    }

    [Fact]
    public void BagProperties_PreservedThroughSerialization()
    {
        var file = new PuzzleFile
        {
            Name = "Props Test",
            CreatedAt = DateTime.UtcNow,
            Puzzle = new PuzzleState
            {
                Bags = new List<PuzzleBag>
                {
                    new PuzzleBag
                    {
                        Shape = new bool[][] { new[] { true } },
                        PositionCol = 2,
                        PositionRow = 3,
                        Properties = new List<string> { "Insulated", "Reinforced" },
                    }
                }
            }
        };

        string json = PuzzleFile.SaveToString(file);
        var loaded = PuzzleFile.LoadFromString(json);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Puzzle.Bags[0].PositionCol);
        Assert.Equal(3, loaded.Puzzle.Bags[0].PositionRow);
        Assert.Equal(2, loaded.Puzzle.Bags[0].Properties.Count);
        Assert.Contains("Insulated", loaded.Puzzle.Bags[0].Properties);
        Assert.Contains("Reinforced", loaded.Puzzle.Bags[0].Properties);
    }
}
