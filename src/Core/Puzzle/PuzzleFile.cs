using System;
using System.IO;
using System.Text.Json;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Wrapper around PuzzleState that adds editor/authoring metadata.
/// PuzzleState stays focused on gameplay data; PuzzleFile adds authoring context.
/// </summary>
public class PuzzleFile
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>Puzzle name (required for display/identification).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description of the puzzle.</summary>
    public string? Description { get; set; }

    /// <summary>Optional author name.</summary>
    public string? Author { get; set; }

    /// <summary>When this puzzle file was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>The gameplay puzzle data.</summary>
    public PuzzleState Puzzle { get; set; } = new();

    /// <summary>
    /// Save a PuzzleFile to a file path as JSON.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    public static void Save(PuzzleFile puzzleFile, string path)
    {
        if (puzzleFile == null) throw new ArgumentNullException(nameof(puzzleFile));
        if (path == null) throw new ArgumentNullException(nameof(path));

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string json = JsonSerializer.Serialize(puzzleFile, SerializerOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Load a PuzzleFile from a file path.
    /// Returns null if the file doesn't exist or contains invalid JSON.
    /// </summary>
    public static PuzzleFile? Load(string path)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));

        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<PuzzleFile>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Serialize a PuzzleFile to a JSON string (for in-memory use or testing).
    /// </summary>
    public static string SaveToString(PuzzleFile puzzleFile)
    {
        if (puzzleFile == null) throw new ArgumentNullException(nameof(puzzleFile));
        return JsonSerializer.Serialize(puzzleFile, SerializerOptions);
    }

    /// <summary>
    /// Deserialize a PuzzleFile from a JSON string.
    /// Returns null if the string is null, empty, or contains invalid JSON.
    /// </summary>
    public static PuzzleFile? LoadFromString(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<PuzzleFile>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
