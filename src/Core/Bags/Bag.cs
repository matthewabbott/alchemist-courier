using System.Collections.Generic;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

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
    /// Rotation in 90-degree clockwise steps (0 = no rotation, 1 = 90°, 2 = 180°, 3 = 270°).
    /// </summary>
    public int Rotation
    {
        get => _rotation;
        set => _rotation = ((value % 4) + 4) % 4;
    }
    private int _rotation;

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
    /// The gravity direction of this bag. Only meaningful when HasProperty(Gravity).
    /// </summary>
    public GravityDirection GravityDirection => Definition.GravityDirection;

    /// <summary>Get the shape after applying the current rotation.</summary>
    public bool[,] GetRotatedShape() => ShapeUtil.Rotate(Definition.Shape, _rotation);

    /// <summary>
    /// Get the local cell positions (relative to bag origin) after rotation.
    /// </summary>
    public List<Vector2I> GetRotatedLocalCellPositions() => ShapeUtil.GetCellPositions(GetRotatedShape());

    /// <summary>Rotate 90° clockwise.</summary>
    public void RotateClockwise() => Rotation++;

    /// <summary>
    /// Get all meta-grid positions this bag would occupy if placed at the given origin,
    /// accounting for rotation.
    /// </summary>
    public List<Vector2I> GetMetaGridPositions(Vector2I origin)
    {
        var positions = new List<Vector2I>();
        foreach (var localPos in GetRotatedLocalCellPositions())
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

    /// <summary>
    /// Check if a meta-grid position is an edge cell of this bag.
    /// An edge cell is a bag cell that has at least one cardinal neighbor
    /// that is NOT part of this same bag. Accounts for rotation.
    /// </summary>
    public bool IsEdgeCell(Vector2I metaGridPos)
    {
        if (!IsPlaced)
            return false;

        var rotatedShape = GetRotatedShape();
        int shapeRows = rotatedShape.GetLength(0);
        int shapeCols = rotatedShape.GetLength(1);

        // Convert to local position
        var origin = PositionOnMetaGrid!.Value;
        var localCol = metaGridPos.Col - origin.Col;
        var localRow = metaGridPos.Row - origin.Row;

        // Check if this position is even in the bag
        if (localRow < 0 || localRow >= shapeRows ||
            localCol < 0 || localCol >= shapeCols ||
            !rotatedShape[localRow, localCol])
            return false;

        // Check each cardinal neighbor — if any is not part of this bag, it's an edge
        foreach (var dir in Vector2I.CardinalDirections)
        {
            var neighborLocalRow = localRow + dir.Row;
            var neighborLocalCol = localCol + dir.Col;

            if (neighborLocalRow < 0 || neighborLocalRow >= shapeRows ||
                neighborLocalCol < 0 || neighborLocalCol >= shapeCols ||
                !rotatedShape[neighborLocalRow, neighborLocalCol])
            {
                return true; // Neighbor is outside the bag shape
            }
        }

        return false; // All cardinal neighbors are within the bag
    }

    public override string ToString()
    {
        var pos = IsPlaced ? PositionOnMetaGrid!.Value.ToString() : "not placed";
        return $"Bag[{Definition.Name}] at {pos}";
    }
}
