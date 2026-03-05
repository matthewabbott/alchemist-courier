using System.Collections.Generic;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Tracks hint usage during a puzzle session: budget, usage count, and which reagents have been hinted.
/// </summary>
public class HintState
{
    /// <summary>Maximum number of hints allowed for this puzzle.</summary>
    public int MaxHints { get; }

    /// <summary>Number of hints the player has used so far.</summary>
    public int HintsUsed { get; set; }

    /// <summary>Indices of reagents that have been hinted (placed at solution positions).</summary>
    public HashSet<int> HintedReagentIndices { get; } = new();

    /// <summary>Whether the player has remaining hint budget.</summary>
    public bool HasHintsRemaining => HintsUsed < MaxHints;

    public HintState(int maxHints)
    {
        MaxHints = maxHints;
    }

    /// <summary>Check whether the given reagent index has been hinted.</summary>
    public bool WasHinted(int reagentIndex) => HintedReagentIndices.Contains(reagentIndex);
}
