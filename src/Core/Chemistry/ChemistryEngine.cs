using System;
using System.Collections.Generic;
using System.Linq;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Chemistry;

/// <summary>
/// Evaluates all reaction rules against the current state of a MetaGrid,
/// returning any triggered reactions.
/// </summary>
public static class ChemistryEngine
{
    /// <summary>
    /// Evaluate what would happen if a reagent were placed at the given position,
    /// without permanently mutating the grid. Temporarily places the reagent,
    /// evaluates all rules, then removes it.
    /// </summary>
    public static List<ReactionResult> EvaluateProspective(
        MetaGrid grid, Reagent reagent, Vector2I position, List<ReactionRule> rules)
    {
        if (!grid.CanPlaceReagent(reagent, position))
            return new List<ReactionResult>();

        grid.PlaceReagent(reagent, position);
        try
        {
            return Evaluate(grid, rules);
        }
        finally
        {
            grid.RemoveReagent(reagent);
        }
    }

    /// <summary>
    /// Evaluate all given reaction rules against the current grid state.
    /// Returns a list of all reactions that triggered.
    /// </summary>
    public static List<ReactionResult> Evaluate(MetaGrid grid, List<ReactionRule> rules)
    {
        var results = new List<ReactionResult>();

        foreach (var rule in rules)
        {
            switch (rule.Sensitivity)
            {
                case SensitivityTier.Adjacent:
                    EvaluateAdjacentRule(grid, rule, results);
                    break;
                case SensitivityTier.SameBag:
                    EvaluateSameBagRule(grid, rule, results);
                    break;
                case SensitivityTier.WholeInventory:
                    EvaluateWholeInventoryRule(grid, rule, results);
                    break;
            }
        }

        // Second pass: Fragile-specific breaking conditions (D.2)
        EvaluateFragileThermalFracture(grid, results);
        EvaluateFragileCrushing(grid, results);
        // Existing: Fragile reagents break from ANY adjacent reaction
        EvaluateFragileVulnerability(grid, results);

        return results;
    }

    /// <summary>
    /// Evaluate an Adjacent sensitivity rule. Dispatches to net-hot evaluation for
    /// single-trigger rules (Volatile), or pair evaluation for two-trigger rules.
    /// </summary>
    private static void EvaluateAdjacentRule(MetaGrid grid, ReactionRule rule, List<ReactionResult> results)
    {
        if (rule.TriggerAspects.Count == 1)
        {
            EvaluateVolatileNetHotRule(grid, rule, results);
            return;
        }

        if (rule.TriggerAspects.Count != 2)
            return;

        var aspect1 = rule.TriggerAspects[0];
        var aspect2 = rule.TriggerAspects[1];

        // Track which reagent pairs we've already reported to avoid duplicates
        var reportedPairs = new HashSet<(Reagent, Reagent)>();

        foreach (var reagent in grid.PlacedReagents)
        {
            foreach (var pos in reagent.GetCurrentMetaGridPositions())
            {
                if (IsAspectSuppressed(grid, pos))
                    continue;

                var cellAspects = reagent.GetAspectsAtMetaGridPosition(pos);

                foreach (var adjCell in grid.GetAdjacentCells(pos))
                {
                    var neighbor = grid.GetReagentAtCell(adjCell.Position);
                    if (neighbor == null || neighbor == reagent)
                        continue;

                    if (IsAspectSuppressed(grid, adjCell.Position))
                        continue;

                    var neighborAspects = neighbor.GetAspectsAtMetaGridPosition(adjCell.Position);

                    // Check both orderings: reagent has aspect1 & neighbor has aspect2, or vice versa
                    bool triggered = false;
                    if (aspect1 == aspect2)
                    {
                        // Same-aspect adjacency (unlikely for Adjacent tier but handle it)
                        triggered = cellAspects.Has(aspect1) && neighborAspects.Has(aspect1);
                    }
                    else
                    {
                        triggered =
                            (cellAspects.Has(aspect1) && neighborAspects.Has(aspect2)) ||
                            (cellAspects.Has(aspect2) && neighborAspects.Has(aspect1));
                    }

                    if (triggered)
                    {
                        // Normalize pair order for dedup
                        var pair = OrderPair(reagent, neighbor);
                        if (reportedPairs.Add(pair))
                        {
                            var affectedCells = new List<Vector2I> { pos, adjCell.Position };

                            // Stable buffering and Catalytic reduction
                            int stableBuffer = GetAdjacentAspectMagnitude(grid, affectedCells, AspectIds.Stable);
                            int catalyticReduction = GetAdjacentAspectMagnitude(grid, affectedCells, AspectIds.Catalytic);
                            int effectiveThreshold = Math.Max(0, rule.Threshold + stableBuffer - catalyticReduction);
                            if (effectiveThreshold > 0)
                                continue;

                            results.Add(new ReactionResult(
                                rule.ResultType,
                                1f,
                                affectedCells,
                                rule));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Evaluate a single-trigger Adjacent rule using net-hot calculation.
    /// For each cell with the trigger aspect (Volatile), sum adjacent Hot magnitudes
    /// minus adjacent Cold magnitudes. If net_hot > effective_threshold, trigger.
    /// </summary>
    private static void EvaluateVolatileNetHotRule(MetaGrid grid, ReactionRule rule, List<ReactionResult> results)
    {
        var triggerAspect = rule.TriggerAspects[0];

        foreach (var reagent in grid.PlacedReagents)
        {
            foreach (var pos in reagent.GetCurrentMetaGridPositions())
            {
                if (IsAspectSuppressed(grid, pos))
                    continue;

                var cellAspects = reagent.GetAspectsAtMetaGridPosition(pos);
                if (!cellAspects.Has(triggerAspect))
                    continue;

                // Compute net_hot from cardinal neighbors
                int hotSum = 0;
                int coldSum = 0;
                var hotCells = new List<Vector2I>();

                foreach (var adjCell in grid.GetAdjacentCells(pos))
                {
                    if (IsAspectSuppressed(grid, adjCell.Position))
                        continue;

                    var neighbor = grid.GetReagentAtCell(adjCell.Position);
                    if (neighbor == null || neighbor == reagent)
                        continue;

                    var neighborAspects = neighbor.GetAspectsAtMetaGridPosition(adjCell.Position);
                    int hotMag = neighborAspects.GetMagnitude(AspectIds.Hot);
                    int coldMag = neighborAspects.GetMagnitude(AspectIds.Cold);

                    if (hotMag > 0)
                    {
                        hotSum += hotMag;
                        hotCells.Add(adjCell.Position);
                    }
                    coldSum += coldMag;
                }

                int netHot = hotSum - coldSum;
                if (netHot <= 0)
                    continue;

                var affectedCells = new List<Vector2I> { pos };
                affectedCells.AddRange(hotCells);

                // Stable buffering raises the effective threshold, Catalytic lowers it
                int stableBuffer = GetAdjacentAspectMagnitude(grid, affectedCells, AspectIds.Stable);
                int catalyticReduction = GetAdjacentAspectMagnitude(grid, affectedCells, AspectIds.Catalytic);
                int effectiveThreshold = Math.Max(0, rule.Threshold + stableBuffer - catalyticReduction);

                if (netHot > effectiveThreshold)
                {
                    results.Add(new ReactionResult(
                        rule.ResultType,
                        netHot,
                        affectedCells,
                        rule));
                }
            }
        }
    }

    /// <summary>
    /// Evaluate a SameBag sensitivity rule. For each bag, find reagent pairs
    /// that share the bag and have the required trigger aspects.
    /// </summary>
    private static void EvaluateSameBagRule(MetaGrid grid, ReactionRule rule, List<ReactionResult> results)
    {
        if (rule.TriggerAspects.Count != 2)
            return;

        var aspect1 = rule.TriggerAspects[0];
        var aspect2 = rule.TriggerAspects[1];
        bool sameAspect = aspect1 == aspect2;

        foreach (var bag in grid.PlacedBags)
        {
            // Null bags suppress all aspects — skip entirely
            if (bag.HasProperty(BagPropertyType.Null))
                continue;

            // Collect distinct reagents in this bag
            var reagentsInBag = new HashSet<Reagent>();
            foreach (var cell in bag.OwnedCells)
            {
                if (cell.OccupyingReagent is Reagent r)
                    reagentsInBag.Add(r);
            }

            var reagentList = reagentsInBag.ToList();

            if (sameAspect)
            {
                // Count reagents that have this aspect (for threshold check)
                var withAspect = reagentList.Where(r => ReagentHasUnsuppressedAspect(grid, r, aspect1)).ToList();
                var affectedCells = GetAllCellsForReagents(withAspect);
                int stableBuffer = GetAdjacentAspectMagnitude(grid, affectedCells, AspectIds.Stable);
                int catalyticReduction = GetAdjacentAspectMagnitude(grid, affectedCells, AspectIds.Catalytic);
                int effectiveThreshold = Math.Max(0, rule.Threshold + stableBuffer - catalyticReduction);
                if (withAspect.Count >= effectiveThreshold && withAspect.Count >= 2)
                {
                    results.Add(new ReactionResult(
                        rule.ResultType,
                        withAspect.Count,
                        affectedCells,
                        rule));
                }
            }
            else
            {
                // Check all pairs for the two distinct aspects
                var reportedPairs = new HashSet<(Reagent, Reagent)>();
                for (int i = 0; i < reagentList.Count; i++)
                {
                    for (int j = i + 1; j < reagentList.Count; j++)
                    {
                        var a = reagentList[i];
                        var b = reagentList[j];

                        bool triggered =
                            (ReagentHasUnsuppressedAspect(grid, a, aspect1) && ReagentHasUnsuppressedAspect(grid, b, aspect2)) ||
                            (ReagentHasUnsuppressedAspect(grid, a, aspect2) && ReagentHasUnsuppressedAspect(grid, b, aspect1));

                        if (triggered)
                        {
                            var pair = OrderPair(a, b);
                            if (reportedPairs.Add(pair))
                            {
                                var affectedCells = GetAllCellsForReagents(new List<Reagent> { a, b });

                                // Stable buffering and Catalytic reduction
                                int stableBuffer = GetAdjacentAspectMagnitude(grid, affectedCells, AspectIds.Stable);
                                int catalyticReduction = GetAdjacentAspectMagnitude(grid, affectedCells, AspectIds.Catalytic);
                                int effectiveThreshold = Math.Max(0, rule.Threshold + stableBuffer - catalyticReduction);
                                if (effectiveThreshold > 0)
                                    continue;

                                results.Add(new ReactionResult(
                                    rule.ResultType,
                                    1f,
                                    affectedCells,
                                    rule));
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Evaluate a WholeInventory cumulative rule. Sum the aspect magnitude
    /// across the entire inventory and check against threshold.
    /// </summary>
    private static void EvaluateWholeInventoryRule(MetaGrid grid, ReactionRule rule, List<ReactionResult> results)
    {
        if (rule.TriggerAspects.Count != 1)
            return;

        var aspectId = rule.TriggerAspects[0];
        int totalMagnitude = 0;
        var affectedCells = new List<Vector2I>();

        if (rule.Granularity == Granularity.PerCell)
        {
            foreach (var reagent in grid.PlacedReagents)
            {
                foreach (var pos in reagent.GetCurrentMetaGridPositions())
                {
                    if (IsAspectSuppressed(grid, pos))
                        continue;

                    var aspects = reagent.GetAspectsAtMetaGridPosition(pos);
                    int mag = aspects.GetMagnitude(aspectId);
                    if (mag > 0)
                    {
                        totalMagnitude += mag;
                        affectedCells.Add(pos);
                    }
                }
            }
        }
        else // PerItem
        {
            foreach (var reagent in grid.PlacedReagents)
            {
                if (ReagentHasUnsuppressedAspect(grid, reagent, aspectId))
                {
                    totalMagnitude++;
                    affectedCells.AddRange(reagent.GetCurrentMetaGridPositions());
                }
            }
        }

        // Stable buffering raises, Catalytic lowers the effective threshold
        int stableBuffer = GetAdjacentAspectMagnitude(grid, affectedCells, AspectIds.Stable);
        int catalyticReduction = GetAdjacentAspectMagnitude(grid, affectedCells, AspectIds.Catalytic);

        // Medicinal passively raises cumulative thresholds (whole inventory, not adjacency-based)
        int medicinalBuffer = GetTotalAspectMagnitude(grid, AspectIds.Medicinal);

        int effectiveThreshold = Math.Max(0, rule.Threshold + stableBuffer + medicinalBuffer - catalyticReduction);

        if (totalMagnitude >= effectiveThreshold)
        {
            results.Add(new ReactionResult(
                rule.ResultType,
                totalMagnitude,
                affectedCells,
                rule));
        }
    }

    /// <summary>
    /// Compute the total magnitude of a given aspect on cells adjacent to the given set of cells.
    /// Each unique cell is counted only once (by position).
    /// </summary>
    private static int GetAdjacentAspectMagnitude(MetaGrid grid, List<Vector2I> involvedCells, string aspectId)
    {
        var countedPositions = new HashSet<Vector2I>();
        int total = 0;

        foreach (var cell in involvedCells)
        {
            foreach (var adjCell in grid.GetAdjacentCells(cell))
            {
                if (countedPositions.Contains(adjCell.Position))
                    continue;

                // Skip cells that are part of the involved set
                if (involvedCells.Contains(adjCell.Position))
                    continue;

                if (IsAspectSuppressed(grid, adjCell.Position))
                    continue;

                var reagent = grid.GetReagentAtCell(adjCell.Position);
                if (reagent == null)
                    continue;

                int mag = reagent.GetAspectsAtMetaGridPosition(adjCell.Position).GetMagnitude(aspectId);
                if (mag > 0)
                {
                    total += mag;
                    countedPositions.Add(adjCell.Position);
                }
            }
        }

        return total;
    }

    /// <summary>
    /// Compute the total magnitude of a given aspect across the entire inventory,
    /// skipping cells that are suppressed (e.g., in Null bags).
    /// Used for passive effects like Medicinal that don't require adjacency.
    /// </summary>
    private static int GetTotalAspectMagnitude(MetaGrid grid, string aspectId)
    {
        int total = 0;
        foreach (var reagent in grid.PlacedReagents)
        {
            foreach (var pos in reagent.GetCurrentMetaGridPositions())
            {
                if (IsAspectSuppressed(grid, pos))
                    continue;

                int mag = reagent.GetAspectsAtMetaGridPosition(pos).GetMagnitude(aspectId);
                total += mag;
            }
        }
        return total;
    }

    /// <summary>
    /// Check if a cell's aspects are suppressed. A cell is suppressed if:
    /// (a) the cell itself is in a Null bag, OR
    /// (b) the reagent occupying the cell has ANY cell in a Null bag (full nullification).
    /// Full nullification means touching a Null bag with any part of a reagent
    /// nullifies the entire reagent, even cells in non-null bags.
    /// </summary>
    public static bool IsAspectSuppressed(MetaGrid grid, Vector2I pos)
    {
        var bag = grid.GetBagAtCell(pos);
        if (bag != null && bag.HasProperty(BagPropertyType.Null))
            return true;

        // Full nullification: if the reagent at this cell has ANY cell in a Null bag,
        // the entire reagent is nullified.
        var reagent = grid.GetReagentAtCell(pos);
        if (reagent != null)
            return IsReagentNullified(grid, reagent);

        return false;
    }

    /// <summary>
    /// Check if a reagent is fully nullified — i.e., any of its cells is in a Null bag.
    /// When nullified, ALL of the reagent's aspects are suppressed on every cell.
    /// </summary>
    public static bool IsReagentNullified(MetaGrid grid, Reagent reagent)
    {
        foreach (var cellPos in reagent.GetCurrentMetaGridPositions())
        {
            var cellBag = grid.GetBagAtCell(cellPos);
            if (cellBag != null && cellBag.HasProperty(BagPropertyType.Null))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Check hot/cold balance in each non-null bag. Returns a violation for each bag
    /// where total hot magnitude != total cold magnitude. Null bags and fully-nullified
    /// reagents are excluded from the calculation.
    /// </summary>
    public static List<BagBalanceViolation> GetBagBalanceViolations(MetaGrid grid)
    {
        var violations = new List<BagBalanceViolation>();

        foreach (var bag in grid.PlacedBags)
        {
            // Null bags suppress all aspects — skip entirely
            if (bag.HasProperty(BagPropertyType.Null))
                continue;

            int hotTotal = 0;
            int coldTotal = 0;

            foreach (var cell in bag.OwnedCells)
            {
                if (cell.OccupyingReagent is not Reagent reagent)
                    continue;

                // Full nullification: skip reagents with any cell in a null bag
                if (IsAspectSuppressed(grid, cell.Position))
                    continue;

                var aspects = reagent.GetAspectsAtMetaGridPosition(cell.Position);
                hotTotal += aspects.GetMagnitude(AspectIds.Hot);
                coldTotal += aspects.GetMagnitude(AspectIds.Cold);
            }

            if (hotTotal != coldTotal)
            {
                violations.Add(new BagBalanceViolation(bag, hotTotal, coldTotal));
            }
        }

        return violations;
    }

    /// <summary>
    /// Check if a reagent has a specific aspect on any of its non-suppressed cells.
    /// Cells in Null bags are skipped.
    /// </summary>
    private static bool ReagentHasUnsuppressedAspect(MetaGrid grid, Reagent reagent, string aspectId)
    {
        foreach (var pos in reagent.GetCurrentMetaGridPositions())
        {
            if (IsAspectSuppressed(grid, pos))
                continue;
            if (reagent.GetAspectsAtMetaGridPosition(pos).Has(aspectId))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Get all meta-grid positions for a list of reagents.
    /// </summary>
    private static List<Vector2I> GetAllCellsForReagents(List<Reagent> reagents)
    {
        var cells = new List<Vector2I>();
        foreach (var r in reagents)
            cells.AddRange(r.GetCurrentMetaGridPositions());
        return cells;
    }

    /// <summary>
    /// Order a pair of reagents deterministically for dedup (by hash code).
    /// </summary>
    private static (Reagent, Reagent) OrderPair(Reagent a, Reagent b)
    {
        return a.GetHashCode() <= b.GetHashCode() ? (a, b) : (b, a);
    }

    /// <summary>
    /// D.2: Thermal fracture — a Fragile reagent breaks if any of its cells are
    /// simultaneously adjacent to at least one Hot cell AND at least one Cold cell
    /// (from different reagents or same reagent). Adjacency is computed across the
    /// entire reagent's cell footprint (convex hull of adjacencies).
    /// </summary>
    private static void EvaluateFragileThermalFracture(MetaGrid grid, List<ReactionResult> results)
    {
        foreach (var reagent in grid.PlacedReagents)
        {
            if (!reagent.HasAspect(AspectIds.Fragile))
                continue;

            if (IsReagentNullified(grid, reagent))
                continue;

            var reagentPositions = reagent.GetCurrentMetaGridPositions();

            // Scan all adjacent cells across the entire reagent's footprint
            bool hasAdjacentHot = false;
            bool hasAdjacentCold = false;

            foreach (var pos in reagentPositions)
            {
                foreach (var adjCell in grid.GetAdjacentCells(pos))
                {
                    var adjReagent = grid.GetReagentAtCell(adjCell.Position);
                    if (adjReagent == null || adjReagent == reagent)
                        continue;

                    // Skip nullified reagents
                    if (IsReagentNullified(grid, adjReagent))
                        continue;

                    // Check aspects on the adjacent cell
                    var adjAspects = adjReagent.GetAspectsAtMetaGridPosition(adjCell.Position);
                    if (adjAspects != null)
                    {
                        if (adjAspects.Has(AspectIds.Hot))
                            hasAdjacentHot = true;
                        if (adjAspects.Has(AspectIds.Cold))
                            hasAdjacentCold = true;
                    }

                    if (hasAdjacentHot && hasAdjacentCold)
                        break;
                }

                if (hasAdjacentHot && hasAdjacentCold)
                    break;
            }

            if (hasAdjacentHot && hasAdjacentCold)
            {
                results.Add(new ReactionResult(
                    ReactionResultType.ThermalFracture,
                    1f,
                    reagentPositions,
                    new ReactionRule(
                        name: "Thermal Fracture",
                        triggerAspects: new List<string> { AspectIds.Fragile },
                        sensitivity: SensitivityTier.Adjacent,
                        granularity: Granularity.PerCell,
                        threshold: 0,
                        resultType: ReactionResultType.ThermalFracture)));
            }
        }
    }

    /// <summary>
    /// D.2: Crushing — a Fragile reagent breaks if a Heavy reagent's bearing cells
    /// rest directly on it. "Directly on it" means the Heavy reagent's bearing cells
    /// (in the gravity direction) sit on top of the Fragile reagent's cells.
    /// Non-heavy reagents on top of Fragile do NOT crush it.
    /// </summary>
    private static void EvaluateFragileCrushing(MetaGrid grid, List<ReactionResult> results)
    {
        // Gather all Fragile reagents and their positions
        var fragileReagents = new List<(Reagent reagent, HashSet<Vector2I> positions)>();
        foreach (var reagent in grid.PlacedReagents)
        {
            if (!reagent.HasAspect(AspectIds.Fragile) || IsReagentNullified(grid, reagent))
                continue;

            fragileReagents.Add((reagent, new HashSet<Vector2I>(reagent.GetCurrentMetaGridPositions())));
        }

        if (fragileReagents.Count == 0)
            return;

        // For each Heavy reagent, compute bearing cells and check if they sit on any Fragile
        var crushedFragile = new HashSet<Reagent>();

        foreach (var heavyReagent in grid.PlacedReagents)
        {
            if (!heavyReagent.HasAspect(AspectIds.Heavy) || IsReagentNullified(grid, heavyReagent))
                continue;

            var heavyPositions = heavyReagent.GetCurrentMetaGridPositions();

            // Determine gravity direction from the bag containing the heavy reagent
            var gravDir = Vector2I.Down; // default
            foreach (var pos in heavyPositions)
            {
                var bag = grid.GetBagAtCell(pos);
                if (bag != null && bag.HasProperty(BagPropertyType.Gravity))
                {
                    gravDir = bag.GravityDirection.ToVector();
                    break;
                }
            }

            // Compute bearing cells for the heavy reagent
            var bearingCells = MetaGrid.GetBearingCells(heavyPositions, gravDir);

            // For each bearing cell, check the cell in the gravity direction ("below" it)
            // If that cell belongs to a Fragile reagent, it's being crushed
            foreach (var bearingCell in bearingCells)
            {
                var cellBelow = bearingCell + gravDir;
                var belowReagent = grid.GetReagentAtCell(cellBelow);
                if (belowReagent == null || belowReagent == heavyReagent)
                    continue;

                foreach (var (fragile, fragilePositions) in fragileReagents)
                {
                    if (fragile == belowReagent && !crushedFragile.Contains(fragile))
                    {
                        crushedFragile.Add(fragile);
                        results.Add(new ReactionResult(
                            ReactionResultType.Crushing,
                            1f,
                            fragile.GetCurrentMetaGridPositions(),
                            new ReactionRule(
                                name: "Crushing",
                                triggerAspects: new List<string> { AspectIds.Fragile, AspectIds.Heavy },
                                sensitivity: SensitivityTier.Adjacent,
                                granularity: Granularity.PerCell,
                                threshold: 0,
                                resultType: ReactionResultType.Crushing)));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Second pass: for each reaction result, check if any Fragile reagent has cells
    /// adjacent to the reaction's affected cells. If so, add a Breakage result.
    /// Only considers reactions from the first pass (no cascading).
    /// </summary>
    private static void EvaluateFragileVulnerability(MetaGrid grid, List<ReactionResult> results)
    {
        if (results.Count == 0)
            return;

        // Collect all cells affected by any reaction
        var reactionCells = new HashSet<Vector2I>();
        foreach (var result in results)
        {
            foreach (var cell in result.AffectedCells)
                reactionCells.Add(cell);
        }

        // Find Fragile reagents with cells adjacent to any reaction cell
        var brokenReagents = new HashSet<Reagent>();
        var newResults = new List<ReactionResult>();

        foreach (var reagent in grid.PlacedReagents)
        {
            if (!reagent.HasAspect(AspectIds.Fragile))
                continue;

            // Full nullification: reagent with any cell in Null bag has no aspects
            if (IsReagentNullified(grid, reagent))
                continue;

            // Check if any cell of this Fragile reagent is adjacent to a reaction cell
            bool isAdjacent = false;
            foreach (var pos in reagent.GetCurrentMetaGridPositions())
            {
                foreach (var adjCell in grid.GetAdjacentCells(pos))
                {
                    if (reactionCells.Contains(adjCell.Position))
                    {
                        isAdjacent = true;
                        break;
                    }
                }
                if (isAdjacent)
                    break;
            }

            if (!isAdjacent)
                continue;

            // Skip if this reagent is already directly involved in a Breakage result
            // (e.g., from Sharp+Fragile pair rule)
            if (brokenReagents.Contains(reagent))
                continue;

            // Also skip if the reagent's cells are already in the reaction cells
            // from a Breakage result (already broken by Sharp+Fragile)
            var reagentPositions = reagent.GetCurrentMetaGridPositions();
            bool alreadyBroken = results.Any(r =>
                r.Type == ReactionResultType.Breakage &&
                reagentPositions.Any(p => r.AffectedCells.Contains(p)));
            if (alreadyBroken)
                continue;

            brokenReagents.Add(reagent);
            newResults.Add(new ReactionResult(
                ReactionResultType.Breakage,
                1f,
                reagentPositions,
                new ReactionRule(
                    name: "Fragile Vulnerability",
                    triggerAspects: new List<string> { AspectIds.Fragile },
                    sensitivity: SensitivityTier.Adjacent,
                    granularity: Granularity.PerCell,
                    threshold: 0,
                    resultType: ReactionResultType.Breakage)));
        }

        results.AddRange(newResults);
    }
}
