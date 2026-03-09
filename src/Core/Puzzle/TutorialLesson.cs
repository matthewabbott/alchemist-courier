namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Data model for a single tutorial lesson.
/// Each lesson introduces one concept through a carefully tuned puzzle.
/// </summary>
public class TutorialLesson
{
    /// <summary>Lesson number (1-based, sequential).</summary>
    public int LessonNumber { get; set; }

    /// <summary>Short name for the concept being taught (e.g., "Basic Placement").</summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>Instructional text shown to the player during the lesson.</summary>
    public string HintText { get; set; } = string.Empty;

    /// <summary>Fixed seed for deterministic puzzle generation.</summary>
    public int FixedSeed { get; set; }

    /// <summary>Difficulty parameters controlling puzzle generation for this lesson.</summary>
    public DifficultyParams DifficultyParams { get; set; } = new();

    /// <summary>Win condition type required for this lesson's puzzle.</summary>
    public PuzzleType RequiredPuzzleType { get; set; }
}
