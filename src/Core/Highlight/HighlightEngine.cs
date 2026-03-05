using System.Collections.Generic;
using System.Linq;
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
