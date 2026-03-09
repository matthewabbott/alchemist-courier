using System.Collections.Generic;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Static helper methods for the hint system: solution lookup, dislodge computation,
/// auto-pick target selection, and hint execution.
/// </summary>
public static class HintEngine
{
    /// <summary>
    /// Look up the solution placement for a given reagent index.
    /// Returns null if no solution placement exists for this reagent.
    /// </summary>
    public static Placement? GetSolutionPlacement(PuzzleState state, int reagentIndex)
    {
        foreach (var placement in state.Solution)
        {
            if (placement.ReagentIndex == reagentIndex)
                return placement;
        }
        return null;
    }

    /// <summary>
    /// Compute which reagent indices must be dislodged to place the target reagent
    /// at its solution position. Returns a list of reagent indices (excluding the target).
    /// If any occupying reagent is locked, returns null to indicate the placement is blocked.
    /// </summary>
    public static List<int>? ComputeDislodgeList(
        MetaGrid grid,
        List<Reagent> reagents,
        Reagent targetReagent,
        Placement solutionPlacement)
    {
        // Set the target's rotation to solution rotation so GetMetaGridPositions is accurate
        int originalRotation = targetReagent.Rotation;
        targetReagent.Rotation = solutionPlacement.Rotation;

        var targetPositions = targetReagent.GetMetaGridPositions(
            new Vector2I(solutionPlacement.PositionCol, solutionPlacement.PositionRow));

        // Restore original rotation
        targetReagent.Rotation = originalRotation;

        var dislodgeIndices = new HashSet<int>();
        int targetIndex = reagents.IndexOf(targetReagent);

        foreach (var pos in targetPositions)
        {
            var occupant = grid.GetReagentAtCell(pos);
            if (occupant == null || occupant == targetReagent)
                continue;

            if (occupant.IsLocked)
                return null; // Blocked by locked reagent

            int occupantIndex = reagents.IndexOf(occupant);
            if (occupantIndex >= 0)
                dislodgeIndices.Add(occupantIndex);
        }

        return new List<int>(dislodgeIndices);
    }

    /// <summary>
    /// Auto-pick the next reagent to hint. Priority:
    /// 1. Unplaced reagents whose index is in HintedReagentIndices (snap-back, free).
    /// 2. First unplaced reagent whose index is NOT in HintedReagentIndices (new hint, costs budget).
    /// Returns null if all reagents are placed.
    /// </summary>
    public static int? PickNextHintTarget(
        PuzzleState state,
        HintState hintState,
        List<Reagent> reagents)
    {
        // First pass: snap-back candidates (previously hinted but now unplaced)
        for (int i = 0; i < reagents.Count; i++)
        {
            if (!reagents[i].IsPlaced && hintState.HintedReagentIndices.Contains(i))
                return i;
        }

        // Second pass: new hint candidates (never hinted, unplaced)
        for (int i = 0; i < reagents.Count; i++)
        {
            if (!reagents[i].IsPlaced && !hintState.HintedReagentIndices.Contains(i))
                return i;
        }

        return null;
    }

    /// <summary>
    /// Execute a hint: remove the target (if placed), dislodge occupants, set rotation,
    /// place at solution position, and mark as hinted.
    /// Does NOT mutate HintState (caller handles budget).
    /// </summary>
    public static HintResult ExecuteHint(
        MetaGrid grid,
        List<Reagent> reagents,
        int reagentIndex,
        Placement solutionPlacement)
    {
        var target = reagents[reagentIndex];

        // Compute dislodge list before modifying state
        var dislodgeList = ComputeDislodgeList(grid, reagents, target, solutionPlacement);

        if (dislodgeList == null)
        {
            return new HintResult(
                HintResultType.BlockedByLockedReagent,
                reagentIndex,
                solutionPlacement);
        }

        // Remove target if currently placed
        if (target.IsPlaced)
            grid.RemoveReagent(target);

        // Dislodge occupants
        foreach (var dislodgeIndex in dislodgeList)
        {
            var dislodged = reagents[dislodgeIndex];
            if (dislodged.IsPlaced)
            {
                grid.RemoveReagent(dislodged);
                dislodged.Rotation = 0;
                dislodged.IsHinted = false;
            }
        }

        // Set target rotation and place at solution position
        target.Rotation = solutionPlacement.Rotation;
        var solutionPos = new Vector2I(solutionPlacement.PositionCol, solutionPlacement.PositionRow);
        grid.PlaceReagent(target, solutionPos);
        target.IsHinted = true;

        return new HintResult(
            HintResultType.Success,
            reagentIndex,
            solutionPlacement,
            dislodgeList);
    }
}
