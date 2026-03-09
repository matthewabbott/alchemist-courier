using System;
using System.IO;
using System.Text.Json;

namespace AlchemistCourier.Core.Save;

/// <summary>
/// Handles saving and loading SaveData to/from JSON files.
/// File-based persistence for desktop; swap to localStorage for web export.
/// </summary>
public class SaveManager
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _savePath;

    /// <summary>
    /// Create a SaveManager targeting a specific file path.
    /// </summary>
    public SaveManager(string savePath)
    {
        _savePath = savePath ?? throw new ArgumentNullException(nameof(savePath));
    }

    /// <summary>
    /// Save data to the configured path.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    public void Save(SaveData data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        string? directory = Path.GetDirectoryName(_savePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string json = JsonSerializer.Serialize(data, SerializerOptions);
        File.WriteAllText(_savePath, json);
    }

    /// <summary>
    /// Load data from the configured path.
    /// Returns a fresh SaveData if the file doesn't exist or is corrupt.
    /// </summary>
    public SaveData Load()
    {
        if (!File.Exists(_savePath))
            return new SaveData();

        try
        {
            string json = File.ReadAllText(_savePath);
            return JsonSerializer.Deserialize<SaveData>(json) ?? new SaveData();
        }
        catch (JsonException)
        {
            return new SaveData();
        }
    }

    /// <summary>
    /// Check if a save file exists at the configured path.
    /// </summary>
    public bool SaveExists()
    {
        return File.Exists(_savePath);
    }

    /// <summary>
    /// Delete the save file if it exists.
    /// </summary>
    public void DeleteSave()
    {
        if (File.Exists(_savePath))
            File.Delete(_savePath);
    }

    /// <summary>
    /// Serialize SaveData to a JSON string (for web localStorage or testing).
    /// </summary>
    public static string Serialize(SaveData data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        return JsonSerializer.Serialize(data, SerializerOptions);
    }

    /// <summary>
    /// Deserialize SaveData from a JSON string (for web localStorage or testing).
    /// Returns a fresh SaveData if the string is null, empty, or corrupt.
    /// </summary>
    public static SaveData Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new SaveData();

        try
        {
            return JsonSerializer.Deserialize<SaveData>(json) ?? new SaveData();
        }
        catch (JsonException)
        {
            return new SaveData();
        }
    }
}
