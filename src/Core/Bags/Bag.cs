using System.Collections.Generic;
using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Core.Bags;

/// <summary>
/// A runtime instance of a bag, placed at a specific position on the meta-grid.
/// Created from a BagDefinition.
/// </summary>
public class Bag
{
    /// <summary>The bag type definition this instance was created from.</summary>
    public BagDefinition Definition { get; }

    /// <summary>
    /// Top-left corner position of this bag on the meta-grid.
    /// Null if the bag is not currently placed.
    /// </summary>
    public Vector2I? PositionOnMetaGrid { get; set; }

    /// <summary>
    /// The grid cells owned by this bag (populated when placed on the meta-grid).
    /// </summary>
    public List<GridCell> OwnedCells { get; } = new();

    /// <summary>Whether this bag is currently placed on a meta-grid.</summary>
    public bool IsPlaced => PositionOnMetaGrid.HasValue;

    public Bag(BagDefinition definition)
    {
        Definition = definition;
    }

    /// <summary>Check if this bag has a specific property.</summary>
    public bool HasProperty(BagPropertyType type) => Definition.HasProperty(type);

    /// <summary>
    /// Get all meta-grid positions this bag would occupy if placed at the given origin.
    /// </summary>
    public List<Vector2I> GetMetaGridPositions(Vector2I origin)
    {
        var positions = new List<Vector2I>();
        foreach (var localPos in Definition.GetLocalCellPositions())
        {
            positions.Add(origin + localPos);
        }
        return positions;
    }

    /// <summary>
    /// Get all meta-grid positions this bag currently occupies.
    /// Returns empty list if not placed.
    /// </summary>
    public List<Vector2I> GetCurrentMetaGridPositions()
    {
        return IsPlaced ? GetMetaGridPositions(PositionOnMetaGrid!.Value) : new List<Vector2I>();
    }

    public override string ToString()
    {
        var pos = IsPlaced ? PositionOnMetaGrid!.Value.ToString() : "not placed";
        return $"Bag[{Definition.Name}] at {pos}";
    }
}
