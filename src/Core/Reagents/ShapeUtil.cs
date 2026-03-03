using System.Collections.Generic;
using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Core.Reagents;

/// <summary>
/// Utility methods for working with shapes represented as bool[rows, cols] matrices.
/// Shapes use row-major indexing: shape[row, col]. true = cell exists, false = void.
/// </summary>
public static class ShapeUtil
{
    /// <summary>
    /// Rotate a shape 90 degrees clockwise.
    /// Original[row, col] → Rotated[col, maxRow - row]
    /// </summary>
    public static bool[,] Rotate90Clockwise(bool[,] shape)
    {
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);

        // After 90° CW rotation: new dimensions are [cols, rows]
        var rotated = new bool[cols, rows];
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                rotated[col, rows - 1 - row] = shape[row, col];
            }
        }
        return rotated;
    }

    /// <summary>
    /// Rotate a shape by the given number of 90-degree clockwise steps (0-3).
    /// </summary>
    public static bool[,] Rotate(bool[,] shape, int steps)
    {
        // Normalize to 0-3
        steps = ((steps % 4) + 4) % 4;

        var result = shape;
        for (int i = 0; i < steps; i++)
        {
            result = Rotate90Clockwise(result);
        }
        return result;
    }

    /// <summary>
    /// Get all local positions (col, row) where a cell exists in the shape.
    /// Returns positions relative to the shape's top-left origin.
    /// </summary>
    public static List<Vector2I> GetCellPositions(bool[,] shape)
    {
        var positions = new List<Vector2I>();
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (shape[row, col])
                {
                    positions.Add(new Vector2I(col, row));
                }
            }
        }
        return positions;
    }

    /// <summary>Count the number of true cells in a shape.</summary>
    public static int CellCount(bool[,] shape)
    {
        int count = 0;
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (shape[row, col]) count++;
            }
        }
        return count;
    }
}
