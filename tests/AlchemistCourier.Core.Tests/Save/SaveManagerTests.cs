using System;
using System.IO;
using System.Text.Json;
using AlchemistCourier.Core.Save;
using Xunit;

namespace AlchemistCourier.Core.Tests.Save;

public class SaveManagerTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testPath;

    public SaveManagerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "alchemist_courier_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _testPath = Path.Combine(_testDir, "test_save.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip()
    {
        var manager = new SaveManager(_testPath);
        var data = new SaveData();
        data.CompleteTutorialLesson(3);
        data.CompleteDailyPuzzle(new DateTime(2026, 3, 1));
        data.CompleteDailyPuzzle(new DateTime(2026, 3, 2));

        manager.Save(data);
        var loaded = manager.Load();

        Assert.Equal(3, loaded.TutorialLessonCompleted);
        Assert.Equal(2, loaded.CurrentStreak);
        Assert.Equal(20260302, loaded.LastCompletedDate);
        Assert.Equal(2, loaded.CompletedDailies.Count);
        Assert.Contains(20260301, loaded.CompletedDailies);
        Assert.Contains(20260302, loaded.CompletedDailies);
    }

    [Fact]
    public void Load_FileDoesNotExist_ReturnsFreshSaveData()
    {
        var manager = new SaveManager(Path.Combine(_testDir, "nonexistent.json"));

        var loaded = manager.Load();

        Assert.Equal(0, loaded.TutorialLessonCompleted);
        Assert.Empty(loaded.CompletedDailies);
        Assert.Equal(0, loaded.CurrentStreak);
    }

    [Fact]
    public void Load_CorruptJson_ReturnsFreshSaveData()
    {
        File.WriteAllText(_testPath, "not valid json {{{");
        var manager = new SaveManager(_testPath);

        var loaded = manager.Load();

        Assert.Equal(0, loaded.TutorialLessonCompleted);
        Assert.Empty(loaded.CompletedDailies);
    }

    [Fact]
    public void SaveExists_ReturnsTrueAfterSave()
    {
        var manager = new SaveManager(_testPath);
        Assert.False(manager.SaveExists());

        manager.Save(new SaveData());
        Assert.True(manager.SaveExists());
    }

    [Fact]
    public void DeleteSave_RemovesFile()
    {
        var manager = new SaveManager(_testPath);
        manager.Save(new SaveData());
        Assert.True(manager.SaveExists());

        manager.DeleteSave();
        Assert.False(manager.SaveExists());
    }

    [Fact]
    public void DeleteSave_NoFile_NoError()
    {
        var manager = new SaveManager(Path.Combine(_testDir, "nonexistent.json"));

        manager.DeleteSave(); // Should not throw
    }

    [Fact]
    public void Save_CreatesDirectory()
    {
        string nestedPath = Path.Combine(_testDir, "sub", "deep", "save.json");
        var manager = new SaveManager(nestedPath);

        manager.Save(new SaveData());

        Assert.True(File.Exists(nestedPath));
    }

    [Fact]
    public void Serialize_ProducesValidJson()
    {
        var data = new SaveData();
        data.CompleteTutorialLesson(2);
        data.CompleteDailyPuzzle(new DateTime(2026, 3, 4));

        string json = SaveManager.Serialize(data);

        Assert.Contains("\"TutorialLessonCompleted\": 2", json);
        Assert.Contains("20260304", json);
    }

    [Fact]
    public void Deserialize_RoundTrip()
    {
        var data = new SaveData();
        data.CompleteTutorialLesson(4);
        data.CompleteDailyPuzzle(new DateTime(2026, 1, 15));
        data.CompleteDailyPuzzle(new DateTime(2026, 1, 16));

        string json = SaveManager.Serialize(data);
        var loaded = SaveManager.Deserialize(json);

        Assert.Equal(4, loaded.TutorialLessonCompleted);
        Assert.Equal(2, loaded.CurrentStreak);
        Assert.Equal(2, loaded.CompletedDailies.Count);
    }

    [Fact]
    public void Deserialize_NullOrEmpty_ReturnsFreshSaveData()
    {
        var fromNull = SaveManager.Deserialize(null);
        var fromEmpty = SaveManager.Deserialize("");

        Assert.Equal(0, fromNull.TutorialLessonCompleted);
        Assert.Equal(0, fromEmpty.TutorialLessonCompleted);
    }

    [Fact]
    public void Deserialize_CorruptJson_ReturnsFreshSaveData()
    {
        var loaded = SaveManager.Deserialize("{invalid}}}");

        Assert.Equal(0, loaded.TutorialLessonCompleted);
        Assert.Empty(loaded.CompletedDailies);
    }

    [Fact]
    public void Save_OverwritesPreviousData()
    {
        var manager = new SaveManager(_testPath);

        var data1 = new SaveData();
        data1.CompleteTutorialLesson(1);
        manager.Save(data1);

        var data2 = new SaveData();
        data2.CompleteTutorialLesson(5);
        manager.Save(data2);

        var loaded = manager.Load();
        Assert.Equal(5, loaded.TutorialLessonCompleted);
    }
}
