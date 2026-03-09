using System.Collections.Generic;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Highlight;

/// <summary>
/// Resolves hover frame keys and perimeter edges for reagents.
/// Frame keys encode aspect + shape + rotation for texture lookup.
/// Perimeter edges flag only external boundaries (internal edges between
/// cells of the same reagent are excluded).
/// </summary>
public static class HoverFrameResolver
{
    /// <summary>
    /// Get a frame texture key for the given reagent, encoding its primary aspect,
    /// catalog shape name, and rotation. Format: "{aspect}_{shape}_r{rotation}".
    /// Falls back to "{aspect}_custom{cellCount}_r{rotation}" for non-catalog shapes.
    /// Returns null if the reagent has no aspects.
    /// </summary>
    public static string? GetFrameKey(Reagent reagent)
    {
        var aspectId = reagent.Definition.PrimaryAspectId;
        if (aspectId == null)
            return null;

        var shapeName = ReagentCatalog.GetShapeName(reagent.Definition);
        var shapeKey = shapeName ?? $"custom{reagent.Definition.CellCount}";

        return $"{aspectId}_{shapeKey}_r{reagent.Rotation}";
    }

    /// <summary>
    /// Get perimeter edges for the reagent's cells at their current rotation.
    /// Returns a dictionary mapping each local cell position (relative to placement origin)
    /// to its external boundary edges. Internal edges shared between cells of the
    /// same reagent are NOT flagged.
    /// </summary>
    public static Dictionary<Vector2I, CellEdge> GetPerimeterEdges(Reagent reagent)
    {
        var localPositions = reagent.GetLocalCellPositions();
        return ContiguousBoundary.Compute(localPositions);
    }

    /// <summary>
    /// Get perimeter edges for a shape matrix directly (no reagent instance needed).
    /// Useful for generating frame textures from catalog shapes.
    /// </summary>
    public static Dictionary<Vector2I, CellEdge> GetPerimeterEdges(bool[,] shape)
    {
        var positions = ShapeUtil.GetCellPositions(shape);
        return ContiguousBoundary.Compute(positions);
    }

}
