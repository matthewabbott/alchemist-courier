using System.Collections.Generic;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Outcome types for a hint attempt.
/// </summary>
public enum HintResultType
{
    /// <summary>Hint was successfully applied.</summary>
    Success,

    /// <summary>No hints remaining in the budget.</summary>
    NoHintsRemaining,

    /// <summary>The target reagent is locked and cannot be moved.</summary>
    ReagentLocked,

    /// <summary>No solution placement found for this reagent.</summary>
    NoSolutionFound,

    /// <summary>A locked reagent occupies the solution position and cannot be dislodged.</summary>
    BlockedByLockedReagent,

    /// <summary>The reagent is already at its solution position and rotation.</summary>
    AlreadyAtSolutionPosition,
}

/// <summary>
/// Result of a hint operation, including which reagent was hinted,
/// where it was placed, and which reagents had to be dislodged.
/// </summary>
public class HintResult
{
    /// <summary>The outcome type.</summary>
    public HintResultType Type { get; }

    /// <summary>Index of the reagent that was (or would be) hinted.</summary>
    public int ReagentIndex { get; }

    /// <summary>The solution placement for the hinted reagent, if known.</summary>
    public Placement? SolutionPlacement { get; }

    /// <summary>Indices of reagents that were dislodged to make room.</summary>
    public List<int> DislodgedReagentIndices { get; }

    /// <summary>Whether the hint was successfully applied.</summary>
    public bool IsSuccess => Type == HintResultType.Success;

    public HintResult(HintResultType type, int reagentIndex, Placement? solutionPlacement = null, List<int>? dislodgedReagentIndices = null)
    {
        Type = type;
        ReagentIndex = reagentIndex;
        SolutionPlacement = solutionPlacement;
        DislodgedReagentIndices = dislodgedReagentIndices ?? new List<int>();
    }
}
