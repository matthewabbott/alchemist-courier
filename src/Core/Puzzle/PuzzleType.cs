namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// The win condition type for a puzzle.
/// </summary>
public enum PuzzleType
{
    /// <summary>Place ALL reagents without triggering negative reactions.</summary>
    FitAllSafely,

    /// <summary>Place the highest total value of reagents without negative reactions.</summary>
    MaximizeValue,

    /// <summary>Meet a specific requirement (e.g., include N hot reagents) without negative reactions.</summary>
    MeetRequirement,
}
