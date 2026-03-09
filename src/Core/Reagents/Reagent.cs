using System.Collections.Generic;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Core.Reagents;

/// <summary>
/// A runtime instance of a reagent, with rotation state and placement info.
/// Created from a ReagentDefinition.
/// </summary>
public class Reagent
{
    /// <summary>The reagent type definition this instance was created from.</summary>
    public ReagentDefinition Definition { get; }

    /// <summary>
    /// Rotation in 90-degree clockwise steps (0 = no rotation, 1 = 90°, 2 = 180°, 3 = 270°).
    /// </summary>
    public int Rotation
    {
        get => _rotation;
        set => _rotation = ((value % 4) + 4) % 4; // Normalize to 0-3
    }
    private int _rotation;

    /// <summary>
    /// Top-left corner position of this reagent on the meta-grid.
    /// Null if not placed.
    /// </summary>
    public Vector2I? PlacedPosition { get; set; }

    /// <summary>Whether this reagent is currently placed on a grid.</summary>
    public bool IsPlaced => PlacedPosition.HasValue;

    /// <summary>Whether this reagent is locked in place and cannot be removed by the player.</summary>
    public bool IsLocked { get; set; }

    /// <summary>Whether this reagent has been placed at its solution position by the hint system.</summary>
    public bool IsHinted { get; set; }

    public Reagent(ReagentDefinition definition)
    {
        Definition = definition;
        _rotation = 0;
    }

    /// <summary>
    /// Check if this reagent has the given aspect on any of its cells.
    /// </summary>
    public bool HasAspect(string aspectId)
    {
        foreach (var aspectSet in Definition.AspectsPerCell)
        {
            if (aspectSet.Has(aspectId))
                return true;
        }
        return false;
    }

    /// <summary>Get the shape after applying the current rotation.</summary>
    public bool[,] GetRotatedShape() => ShapeUtil.Rotate(Definition.Shape, _rotation);

    /// <summary>
    /// Get the local cell positions (relative to placement origin) after rotation.
    /// </summary>
    public List<Vector2I> GetLocalCellPositions() => ShapeUtil.GetCellPositions(GetRotatedShape());

    /// <summary>
    /// Get the meta-grid positions this reagent would occupy if placed at the given origin
    /// with the current rotation.
    /// </summary>
    public List<Vector2I> GetMetaGridPositions(Vector2I origin)
    {
        var positions = new List<Vector2I>();
        foreach (var localPos in GetLocalCellPositions())
        {
            positions.Add(origin + localPos);
        }
        return positions;
    }

    /// <summary>
    /// Get the meta-grid positions this reagent currently occupies.
    /// Returns empty if not placed.
    /// </summary>
    public List<Vector2I> GetCurrentMetaGridPositions()
    {
        return IsPlaced ? GetMetaGridPositions(PlacedPosition!.Value) : new List<Vector2I>();
    }

    /// <summary>
    /// Get the aspects at a specific local cell position (relative to placement origin),
    /// accounting for rotation. The rotation maps rotated local positions back to
    /// the original shape's cell indices to look up the correct AspectSet.
    /// </summary>
    public AspectSet GetAspectsAtLocalPosition(Vector2I localPos)
    {
        // Get the rotated shape's cell positions in order
        var rotatedPositions = GetLocalCellPositions();
        int cellIndex = rotatedPositions.IndexOf(localPos);
        if (cellIndex < 0)
            return AspectSet.Empty;

        // Map the rotated cell index back to the original cell index.
        // The rotation permutes cell positions. We need to find which original cell
        // ended up at this rotated position.
        //
        // Strategy: the original cell positions and rotated cell positions both enumerate
        // in row-major order. We need to track which original cell index maps to which
        // rotated cell index through the rotation.
        return GetAspectsForRotatedCellIndex(cellIndex);
    }

    /// <summary>
    /// Get aspects for the i-th cell in the rotated shape (row-major order).
    /// Maps back through the rotation to find the corresponding original cell index.
    /// </summary>
    private AspectSet GetAspectsForRotatedCellIndex(int rotatedIndex)
    {
        if (Definition.AspectsPerCell.Count == 0)
            return AspectSet.Empty;

        // Build the mapping: for each cell in the rotated shape, find which original cell it came from.
        var originalPositions = Definition.GetLocalCellPositions(); // Unrotated, row-major order
        var rotatedShape = GetRotatedShape();
        var rotatedPositions = ShapeUtil.GetCellPositions(rotatedShape); // Rotated, row-major order

        if (rotatedIndex < 0 || rotatedIndex >= rotatedPositions.Count)
            return AspectSet.Empty;

        // The rotated position needs to be reverse-rotated to find the original position
        var rotatedPos = rotatedPositions[rotatedIndex];
        var originalPos = ReverseRotatePosition(rotatedPos);

        // Find which index this original position has in the original cell list
        int originalIndex = originalPositions.IndexOf(originalPos);
        if (originalIndex < 0 || originalIndex >= Definition.AspectsPerCell.Count)
            return AspectSet.Empty;

        return Definition.AspectsPerCell[originalIndex];
    }

    /// <summary>
    /// Reverse-rotate a local position from rotated space back to original space.
    /// If we rotated N steps clockwise, we reverse by rotating (4-N) steps clockwise.
    /// </summary>
    private Vector2I ReverseRotatePosition(Vector2I rotatedPos)
    {
        int reverseSteps = (4 - _rotation) % 4;
        if (reverseSteps == 0)
            return rotatedPos;

        // Create a 1-cell shape at the rotated position and reverse-rotate it
        // We need the bounding box of the rotated shape to compute the reverse mapping.
        var rotatedShape = GetRotatedShape();
        int rotatedRows = rotatedShape.GetLength(0);
        int rotatedCols = rotatedShape.GetLength(1);

        // Apply reverse rotation steps
        var pos = rotatedPos;
        int rows = rotatedRows;
        int cols = rotatedCols;

        for (int step = 0; step < reverseSteps; step++)
        {
            // 90° CW rotation: (col, row) in [rows, cols] → (rows - 1 - row, col) in [cols, rows]
            // Wait — we need to be careful with the coordinate system.
            // In our shape system: shape[row, col], position is (col, row).
            // 90° CW: new_col = rows - 1 - row, new_row = col
            var newCol = rows - 1 - pos.Row;
            var newRow = pos.Col;
            pos = new Vector2I(newCol, newRow);

            // Dimensions swap
            (rows, cols) = (cols, rows);
        }

        return pos;
    }

    /// <summary>
    /// Get the aspects at a specific meta-grid position.
    /// Returns empty if the reagent doesn't occupy that position.
    /// </summary>
    public AspectSet GetAspectsAtMetaGridPosition(Vector2I metaGridPos)
    {
        if (!IsPlaced)
            return AspectSet.Empty;

        var localPos = metaGridPos - PlacedPosition!.Value;
        return GetAspectsAtLocalPosition(localPos);
    }

    /// <summary>Rotate 90° clockwise.</summary>
    public void RotateClockwise() => Rotation++;

    /// <summary>Rotate 90° counter-clockwise.</summary>
    public void RotateCounterClockwise() => Rotation--;

    public override string ToString()
    {
        var pos = IsPlaced ? PlacedPosition!.Value.ToString() : "not placed";
        return $"Reagent[{Definition.Name}] rot={Rotation * 90}° at {pos}";
    }
}
