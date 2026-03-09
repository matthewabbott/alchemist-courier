using System.Collections.Generic;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Visual;

/// <summary>
/// Determines the <see cref="VisualState"/> for each cell of a placed reagent
/// based on adjacent aspects from neighboring reagents on the grid.
/// Pure C# — no Godot dependencies.
/// </summary>
public static class VisualStateResolver
{
    /// <summary>
    /// Resolve the visual state for every cell of a placed reagent.
    /// Returns a dictionary mapping each meta-grid position to its visual state.
    /// Returns empty if the reagent is not placed.
    /// </summary>
    public static Dictionary<Vector2I, VisualState> Resolve(Reagent reagent, MetaGrid grid)
    {
        var result = new Dictionary<Vector2I, VisualState>();

        if (!reagent.IsPlaced)
            return result;

        var cellPositions = reagent.GetCurrentMetaGridPositions();
        bool isFragile = reagent.HasAspect(AspectIds.Fragile);

        foreach (var pos in cellPositions)
        {
            result[pos] = ResolveCell(pos, reagent, grid, isFragile);
        }

        return result;
    }

    private static VisualState ResolveCell(Vector2I pos, Reagent reagent, MetaGrid grid, bool isFragile)
    {
        // Check for Crushed: Heavy cell directly above (only for Fragile reagents)
        if (isFragile && HasHeavyAbove(pos, reagent, grid))
            return VisualState.Crushed;

        // Check cardinal neighbors for Hot/Cold aspects from OTHER reagents
        bool hasAdjacentHot = false;
        bool hasAdjacentCold = false;

        var neighbors = grid.GetAdjacentCells(pos);
        foreach (var neighbor in neighbors)
        {
            if (!neighbor.IsOccupied)
                continue;

            var occupant = neighbor.OccupyingReagent as Reagent;
            if (occupant == null || occupant == reagent)
                continue;

            var aspects = occupant.GetAspectsAtMetaGridPosition(neighbor.Position);
            if (aspects.Has(AspectIds.Hot))
                hasAdjacentHot = true;
            if (aspects.Has(AspectIds.Cold))
                hasAdjacentCold = true;
        }

        // ThermalStress: both Hot and Cold adjacent (Fragile-specific)
        if (isFragile && hasAdjacentHot && hasAdjacentCold)
            return VisualState.ThermalStress;

        // Heated/Cooled apply to all reagents
        if (hasAdjacentHot && !hasAdjacentCold)
            return VisualState.Heated;

        if (hasAdjacentCold && !hasAdjacentHot)
            return VisualState.Cooled;

        return VisualState.Normal;
    }

    /// <summary>
    /// Check if there is a Heavy-aspected cell directly above (row - 1) the given position,
    /// belonging to a different reagent.
    /// </summary>
    private static bool HasHeavyAbove(Vector2I pos, Reagent reagent, MetaGrid grid)
    {
        var above = new Vector2I(pos.Col, pos.Row - 1);
        var cell = grid.GetCell(above);
        if (cell == null || !cell.IsOccupied)
            return false;

        var occupant = cell.OccupyingReagent as Reagent;
        if (occupant == null || occupant == reagent)
            return false;

        var aspects = occupant.GetAspectsAtMetaGridPosition(above);
        return aspects.Has(AspectIds.Heavy);
    }
}
