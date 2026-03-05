namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Parameters controlling puzzle generation difficulty.
/// Passed to PuzzleGenerator to control the complexity of generated puzzles.
/// </summary>
public class DifficultyParams
{
    /// <summary>Number of bags in the puzzle.</summary>
    public int BagCount { get; set; }

    /// <summary>Minimum number of cells per bag.</summary>
    public int BagMinSize { get; set; }

    /// <summary>Maximum number of cells per bag.</summary>
    public int BagMaxSize { get; set; }

    /// <summary>Number of reagents to include.</summary>
    public int ReagentCount { get; set; }

    /// <summary>Maximum number of distinct aspects on a single reagent.</summary>
    public int MaxAspectCount { get; set; }

    /// <summary>
    /// Percentage of extra space in bags beyond what reagents need (0.0 = exact fit, 0.5 = 50% spare).
    /// </summary>
    public float SpareSpacePercent { get; set; }

    /// <summary>Whether bags with special properties (Null, Insulated, etc.) are allowed.</summary>
    public bool AllowSpecialBags { get; set; }

    /// <summary>
    /// Controls mechanical depth — which aspects, bag types, and reaction rules are available.
    /// All standalone/daily puzzles use Simple regardless of difficulty level.
    /// </summary>
    public ComplexityLevel ComplexityLevel { get; set; }

    public DifficultyParams() { }

    public DifficultyParams(
        int bagCount,
        int bagMinSize,
        int bagMaxSize,
        int reagentCount,
        int maxAspectCount,
        float spareSpacePercent,
        bool allowSpecialBags,
        ComplexityLevel complexityLevel = ComplexityLevel.Simple)
    {
        BagCount = bagCount;
        BagMinSize = bagMinSize;
        BagMaxSize = bagMaxSize;
        ReagentCount = reagentCount;
        MaxAspectCount = maxAspectCount;
        SpareSpacePercent = spareSpacePercent;
        AllowSpecialBags = allowSpecialBags;
        ComplexityLevel = complexityLevel;
    }

    /// <summary>
    /// Get default params for a given difficulty level.
    /// All levels use Simple complexity — difficulty scaling is purely through scale.
    /// </summary>
    public static DifficultyParams ForLevel(DifficultyLevel level) => level switch
    {
        DifficultyLevel.Tutorial1 => new DifficultyParams(1, 9, 9, 2, 0, 0.5f, false, ComplexityLevel.Simple),
        DifficultyLevel.Tutorial2 => new DifficultyParams(1, 9, 9, 3, 1, 0.3f, false, ComplexityLevel.Simple),
        DifficultyLevel.Tutorial3 => new DifficultyParams(2, 6, 9, 4, 2, 0.3f, false, ComplexityLevel.Simple),
        DifficultyLevel.Tutorial4 => new DifficultyParams(2, 6, 12, 5, 2, 0.3f, true, ComplexityLevel.Simple),
        DifficultyLevel.Tutorial5 => new DifficultyParams(2, 6, 12, 5, 2, 0.3f, false, ComplexityLevel.Simple),
        DifficultyLevel.Easy => new DifficultyParams(2, 6, 12, 6, 2, 0.3f, false, ComplexityLevel.Simple),
        DifficultyLevel.Medium => new DifficultyParams(3, 6, 12, 8, 3, 0.2f, true, ComplexityLevel.Simple),
        DifficultyLevel.Hard => new DifficultyParams(4, 6, 12, 12, 4, 0.1f, true, ComplexityLevel.Simple),
        _ => new DifficultyParams(1, 9, 9, 2, 0, 0.5f, false, ComplexityLevel.Simple),
    };
}
