using System.Collections.Generic;
using System.Linq;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Highlight;

/// <summary>
/// Aggregates all current placement violations into a unified list (J.3).
/// This provides live feedback to the player about issues that would cause
/// failure at confirm time. Violations update as reagents are placed/removed.
///
/// Detects:
/// - Per-bag hot/cold imbalance (I.2)
/// - Fragile crushing by Heavy (D.2)
/// - Fragile thermal fracture from hot+cold adjacency (D.2)
/// - Fragile piercing by Sharp (existing)
/// - Volatile explosion danger
/// - Cumulative overheating/hypothermia
/// </summary>
public static class ViolationTracker
{
    /// <summary>
    /// Compute all current violations for the given grid state.
    /// Returns an ordered list of violation entries suitable for display.
    /// </summary>
    public static List<ViolationEntry> ComputeAll(MetaGrid grid, List<ReactionRule> rules)
    {
        var violations = new List<ViolationEntry>();

        // 1. Per-bag hot/cold balance violations (I.2)
        AddBagBalanceViolations(grid, violations);

        // 2. Chemistry-based violations (reactions that would fire)
        AddChemistryViolations(grid, rules, violations);

        return violations;
    }

    /// <summary>
    /// Check per-bag hot/cold balance (I.2). Each non-null bag must have
    /// equal total hot and cold magnitudes.
    /// </summary>
    private static void AddBagBalanceViolations(MetaGrid grid, List<ViolationEntry> violations)
    {
        var balanceViolations = ChemistryEngine.GetBagBalanceViolations(grid);

        foreach (var bv in balanceViolations)
        {
            var bagCells = GetBagCells(grid, bv.Bag);
            var type = bv.Imbalance > 0 ? ViolationType.BagTooHot : ViolationType.BagTooCold;
            var aspectId = bv.Imbalance > 0 ? AspectIds.Hot : AspectIds.Cold;
            var label = bv.Imbalance > 0 ? "too hot" : "too cold";
            var diff = System.Math.Abs(bv.Imbalance);

            violations.Add(new ViolationEntry(
                type,
                $"{bv.Bag.Definition.Name}: {label} (+{diff})",
                HighlightSeverity.Warning,
                bagCells,
                aspectId));
        }
    }

    /// <summary>
    /// Detect chemistry-based violations by evaluating all rules against the
    /// current grid state. Each triggered reaction becomes a violation entry.
    /// </summary>
    private static void AddChemistryViolations(
        MetaGrid grid, List<ReactionRule> rules, List<ViolationEntry> violations)
    {
        var reactions = ChemistryEngine.Evaluate(grid, rules);

        foreach (var reaction in reactions)
        {
            var entry = ReactionToViolation(reaction);
            if (entry != null)
                violations.Add(entry);
        }
    }

    /// <summary>
    /// Convert a ReactionResult into a ViolationEntry, or null if it's not
    /// a type we track as a live violation.
    /// </summary>
    private static ViolationEntry? ReactionToViolation(ReactionResult reaction)
    {
        return reaction.Type switch
        {
            ReactionResultType.Explosion => new ViolationEntry(
                ViolationType.VolatileExplosion,
                $"Volatile explosion: {reaction.SourceRule.Name}",
                HighlightSeverity.Danger,
                reaction.AffectedCells,
                AspectIds.Volatile),

            ReactionResultType.Breakage => new ViolationEntry(
                ViolationType.FragilePierced,
                $"Fragile breakage: {reaction.SourceRule.Name}",
                HighlightSeverity.Danger,
                reaction.AffectedCells,
                AspectIds.Fragile),

            ReactionResultType.ThermalFracture => new ViolationEntry(
                ViolationType.FragileThermalFracture,
                $"Thermal fracture: hot + cold adjacent to fragile",
                HighlightSeverity.Danger,
                reaction.AffectedCells,
                AspectIds.Fragile),

            ReactionResultType.Crushing => new ViolationEntry(
                ViolationType.FragileCrushed,
                $"Fragile crushed by heavy",
                HighlightSeverity.Danger,
                reaction.AffectedCells,
                AspectIds.Fragile),

            ReactionResultType.Overheating => new ViolationEntry(
                ViolationType.Overheating,
                $"Overheating: cumulative hot exceeds threshold",
                HighlightSeverity.Warning,
                reaction.AffectedCells,
                AspectIds.Hot),

            ReactionResultType.Hypothermia => new ViolationEntry(
                ViolationType.Hypothermia,
                $"Hypothermia: cumulative cold exceeds threshold",
                HighlightSeverity.Warning,
                reaction.AffectedCells,
                AspectIds.Cold),

            _ => null, // Other reaction types aren't tracked as live violations
        };
    }

    /// <summary>
    /// Get all grid positions belonging to a specific bag.
    /// </summary>
    private static List<Vector2I> GetBagCells(MetaGrid grid, Bag bag)
    {
        var cells = new List<Vector2I>();
        for (int col = 0; col < grid.Width; col++)
        {
            for (int row = 0; row < grid.Height; row++)
            {
                var pos = new Vector2I(col, row);
                if (grid.GetBagAtCell(pos) == bag)
                    cells.Add(pos);
            }
        }
        return cells;
    }
}
