using System.Collections.Generic;
using System.Linq;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Highlight;

/// <summary>
/// Core highlight computation engine. Converts constraint violations and other
/// game state into CellHighlight entries for rendering.
/// </summary>
public static class HighlightEngine
{
    /// <summary>
    /// Aggregates all constraint violations for a hypothetical reagent placement.
    /// </summary>
    public static List<ConstraintViolation> GetConstraintViolations(
        MetaGrid grid, Reagent reagent, Vector2I position)
    {
        var positions = reagent.GetMetaGridPositions(position);
        var violations = new List<ConstraintViolation>();

        violations.AddRange(grid.GetSharpViolations(reagent, positions));
        violations.AddRange(grid.GetFragileViolations(reagent, positions));
        violations.AddRange(grid.GetHeavyViolations(reagent, positions));
        violations.AddRange(grid.GetGravityViolations(positions));

        return violations;
    }

    /// <summary>
    /// Converts constraint violations into CellHighlight entries on the Constraint layer.
    /// </summary>
    public static HighlightSet ComputeConstraintHighlights(
        MetaGrid grid, Reagent reagent, Vector2I position)
    {
        var violations = GetConstraintViolations(grid, reagent, position);
        var highlightSet = new HighlightSet();

        if (violations.Count == 0)
            return highlightSet;

        // Collect all affected cells for boundary computation
        var allCells = violations.SelectMany(v => v.AffectedCells).Distinct().ToList();
        var boundaries = ContiguousBoundary.Compute(allCells);

        foreach (var violation in violations)
        {
            var severity = GetSeverityForConstraint(violation.Type);

            foreach (var cell in violation.AffectedCells)
            {
                var edges = boundaries.TryGetValue(cell, out var e) ? e : CellEdge.All;
                highlightSet.Add(new CellHighlight(
                    cell,
                    edges,
                    HighlightLayer.Constraint,
                    severity,
                    violation.AspectId,
                    violation.Reason));
            }
        }

        return highlightSet;
    }

    /// <summary>
    /// Computes interaction highlights for a reagent at a given position.
    /// For each cell of the reagent, checks cardinal neighbors for aspects that
    /// participate in Adjacent reaction rules. Highlighted cells are grouped by
    /// aspect and rendered with ContiguousBoundary edges.
    /// Works for both placed reagents and ghost previews.
    /// </summary>
    public static HighlightSet ComputeInteractionHighlights(
        MetaGrid grid, Reagent reagent, Vector2I position,
        List<ReactionRule> rules, StatusThresholds? thresholds)
    {
        var highlightSet = new HighlightSet();
        var positions = reagent.GetMetaGridPositions(position);

        // Build interaction pairs from Adjacent rules
        var interactionPairs = BuildInteractionPairs(rules);
        if (interactionPairs.Count == 0)
            return highlightSet;

        // Collect highlighted neighbor cells grouped by aspect
        var cellsByAspect = new Dictionary<string, HashSet<Vector2I>>();

        foreach (var pos in positions)
        {
            if (ChemistryEngine.IsAspectSuppressed(grid, pos))
                continue;

            var cellAspects = reagent.GetAspectsAtLocalPosition(pos - position);

            foreach (var dir in Vector2I.CardinalDirections)
            {
                var neighborPos = pos + dir;
                var neighbor = grid.GetReagentAtCell(neighborPos);
                if (neighbor == null || neighbor == reagent)
                    continue;

                if (ChemistryEngine.IsAspectSuppressed(grid, neighborPos))
                    continue;

                var neighborAspects = neighbor.GetAspectsAtMetaGridPosition(neighborPos);

                foreach (var (ourAspect, theirAspect) in interactionPairs)
                {
                    if (cellAspects.Has(ourAspect) && neighborAspects.Has(theirAspect))
                    {
                        if (!cellsByAspect.TryGetValue(theirAspect, out var set))
                        {
                            set = new HashSet<Vector2I>();
                            cellsByAspect[theirAspect] = set;
                        }
                        set.Add(neighborPos);
                    }
                }
            }
        }

        // For each aspect group, compute boundary and create highlights
        foreach (var (aspectId, cellSet) in cellsByAspect)
        {
            var boundary = ContiguousBoundary.Compute(cellSet.ToList());
            var severity = GetSeverityForInteraction(aspectId, thresholds);

            foreach (var (cellPos, edges) in boundary)
            {
                highlightSet.Add(new CellHighlight(
                    cellPos, edges, HighlightLayer.Interaction, severity, aspectId));
            }
        }

        return highlightSet;
    }

    /// <summary>
    /// Finds all placed cells with the given aspect and computes Legend layer highlights
    /// with contiguous boundary edges.
    /// </summary>
    public static HighlightSet ComputeLegendHighlights(MetaGrid grid, string aspectId)
    {
        var highlightSet = new HighlightSet();
        var positions = new List<Vector2I>();

        foreach (var reagent in grid.PlacedReagents)
        {
            foreach (var pos in reagent.GetCurrentMetaGridPositions())
            {
                if (reagent.GetAspectsAtMetaGridPosition(pos).Has(aspectId))
                    positions.Add(pos);
            }
        }

        if (positions.Count == 0)
            return highlightSet;

        var boundary = ContiguousBoundary.Compute(positions);
        foreach (var (pos, edges) in boundary)
        {
            highlightSet.Add(new CellHighlight(
                pos, edges, HighlightLayer.Legend, HighlightSeverity.Info, aspectId));
        }

        return highlightSet;
    }

    /// <summary>
    /// Computes interaction highlights for ALL placed reagents and merges
    /// them into a single HighlightSet.
    /// </summary>
    public static HighlightSet ComputeEagleSenseHighlights(
        MetaGrid grid, List<ReactionRule> rules, StatusThresholds? thresholds)
    {
        var highlightSet = new HighlightSet();

        foreach (var reagent in grid.PlacedReagents)
        {
            if (!reagent.IsPlaced)
                continue;

            var interactions = ComputeInteractionHighlights(
                grid, reagent, reagent.PlacedPosition!.Value, rules, thresholds);
            highlightSet.AddRange(interactions.GetByLayer(HighlightLayer.Interaction));
        }

        return highlightSet;
    }

    /// <summary>
    /// Builds a list of (ourAspect, theirAspect) interaction pairs from Adjacent rules.
    /// For 2-trigger rules: both orderings of the trigger pair.
    /// For 1-trigger rules (Volatile net-hot): pairs with Hot and Cold.
    /// </summary>
    private static List<(string OurAspect, string TheirAspect)> BuildInteractionPairs(
        List<ReactionRule> rules)
    {
        var pairs = new List<(string, string)>();

        foreach (var rule in rules)
        {
            if (rule.Sensitivity != SensitivityTier.Adjacent)
                continue;

            if (rule.TriggerAspects.Count == 2)
            {
                var a = rule.TriggerAspects[0];
                var b = rule.TriggerAspects[1];
                pairs.Add((a, b));
                if (a != b)
                    pairs.Add((b, a));
            }
            else if (rule.TriggerAspects.Count == 1)
            {
                // Net-hot mechanic: trigger aspect interacts with Hot and Cold
                var trigger = rule.TriggerAspects[0];
                pairs.Add((trigger, AspectIds.Hot));
                pairs.Add((trigger, AspectIds.Cold));
                pairs.Add((AspectIds.Hot, trigger));
                pairs.Add((AspectIds.Cold, trigger));
            }
        }

        return pairs;
    }

    private static HighlightSeverity GetSeverityForInteraction(
        string aspectId, StatusThresholds? thresholds)
    {
        if (thresholds != null)
        {
            var severity = thresholds.GetSeverity(aspectId);
            if (severity != HighlightSeverity.Info)
                return severity;
        }
        return HighlightSeverity.Warning;
    }

    private static HighlightSeverity GetSeverityForConstraint(ConstraintType type)
    {
        return type switch
        {
            ConstraintType.SharpOnEdge => HighlightSeverity.Danger,
            ConstraintType.FragileInGravity => HighlightSeverity.Warning,
            ConstraintType.HeavyAboveNonHeavy => HighlightSeverity.Warning,
            ConstraintType.GravityUnsupported => HighlightSeverity.Warning,
            _ => HighlightSeverity.Info
        };
    }
}
