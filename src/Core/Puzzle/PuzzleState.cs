using System.Collections.Generic;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Fully serializable snapshot of a puzzle: bags, reagents, current placements, and solution.
/// Designed for JSON round-tripping (save/load, sharing, daily puzzle seeding).
/// </summary>
public class PuzzleState
{
    /// <summary>Seed used to generate this puzzle (e.g., YYYYMMDD for daily).</summary>
    public int Seed { get; set; }

    /// <summary>Difficulty level this puzzle was generated for.</summary>
    public DifficultyLevel Difficulty { get; set; }

    /// <summary>Win condition type.</summary>
    public PuzzleType PuzzleType { get; set; }

    /// <summary>Bag definitions with their positions on the meta-grid.</summary>
    public List<PuzzleBag> Bags { get; set; } = new();

    /// <summary>Reagent definitions available to the player.</summary>
    public List<PuzzleReagent> Reagents { get; set; } = new();

    /// <summary>Current player placements (mutable during gameplay).</summary>
    public List<Placement> Placements { get; set; } = new();

    /// <summary>The known-valid solution (recorded at generation time).</summary>
    public List<Placement> Solution { get; set; } = new();

    /// <summary>Maximum number of hints allowed for this puzzle (0 = no hints).</summary>
    public int MaxHints { get; set; }
}

/// <summary>
/// Serializable bag definition within a puzzle.
/// Shape is stored as a jagged array (bool[][]) for JSON compatibility.
/// </summary>
public class PuzzleBag
{
    /// <summary>
    /// Bag shape as a jagged array [rows][cols]. true = cell exists.
    /// Stored as jagged (not multidimensional) for JSON serialization.
    /// </summary>
    public bool[][] Shape { get; set; } = System.Array.Empty<bool[]>();

    /// <summary>Column position of the bag's top-left corner on the meta-grid.</summary>
    public int PositionCol { get; set; }

    /// <summary>Row position of the bag's top-left corner on the meta-grid.</summary>
    public int PositionRow { get; set; }

    /// <summary>Special properties on this bag (e.g., Null, Insulated).</summary>
    public List<string> Properties { get; set; } = new();

    /// <summary>Convert the jagged shape array to a 2D bool[rows, cols] matrix.</summary>
    public bool[,] ToShapeMatrix()
    {
        if (Shape.Length == 0)
            return new bool[0, 0];

        int rows = Shape.Length;
        int cols = Shape[0].Length;
        var matrix = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                matrix[r, c] = Shape[r][c];
        return matrix;
    }

    /// <summary>Create a PuzzleBag from a 2D shape matrix and position.</summary>
    public static PuzzleBag FromMatrix(bool[,] shape, int col, int row, List<string>? properties = null)
    {
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);
        var jagged = new bool[rows][];
        for (int r = 0; r < rows; r++)
        {
            jagged[r] = new bool[cols];
            for (int c = 0; c < cols; c++)
                jagged[r][c] = shape[r, c];
        }

        return new PuzzleBag
        {
            Shape = jagged,
            PositionCol = col,
            PositionRow = row,
            Properties = properties ?? new List<string>(),
        };
    }
}

/// <summary>
/// Serializable reagent definition within a puzzle.
/// </summary>
public class PuzzleReagent
{
    /// <summary>
    /// Reagent shape as a jagged array [rows][cols]. true = cell exists.
    /// </summary>
    public bool[][] Shape { get; set; } = System.Array.Empty<bool[]>();

    /// <summary>
    /// Aspect IDs for each cell, indexed by flattened row-major order through true cells.
    /// Outer list = per cell, inner list = aspect IDs on that cell.
    /// </summary>
    public List<List<string>> AspectsPerCell { get; set; } = new();

    /// <summary>Weight value.</summary>
    public float Weight { get; set; }

    /// <summary>Scoring value.</summary>
    public int Value { get; set; }

    /// <summary>Whether this reagent is locked in place (hint/constraint).</summary>
    public bool IsLocked { get; set; }

    /// <summary>Convert the jagged shape array to a 2D bool[rows, cols] matrix.</summary>
    public bool[,] ToShapeMatrix()
    {
        if (Shape.Length == 0)
            return new bool[0, 0];

        int rows = Shape.Length;
        int cols = Shape[0].Length;
        var matrix = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                matrix[r, c] = Shape[r][c];
        return matrix;
    }

    /// <summary>Create a PuzzleReagent from a 2D shape matrix.</summary>
    public static PuzzleReagent FromMatrix(
        bool[,] shape,
        List<List<string>>? aspectsPerCell = null,
        float weight = 1f,
        int value = 1,
        bool isLocked = false)
    {
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);
        var jagged = new bool[rows][];
        for (int r = 0; r < rows; r++)
        {
            jagged[r] = new bool[cols];
            for (int c = 0; c < cols; c++)
                jagged[r][c] = shape[r, c];
        }

        return new PuzzleReagent
        {
            Shape = jagged,
            AspectsPerCell = aspectsPerCell ?? new List<List<string>>(),
            Weight = weight,
            Value = value,
            IsLocked = isLocked,
        };
    }
}

/// <summary>
/// A reagent placement: which reagent, where, and at what rotation.
/// Used for both current player state and stored solutions.
/// </summary>
public class Placement
{
    /// <summary>Index into PuzzleState.Reagents.</summary>
    public int ReagentIndex { get; set; }

    /// <summary>Column of the placement position on the meta-grid.</summary>
    public int PositionCol { get; set; }

    /// <summary>Row of the placement position on the meta-grid.</summary>
    public int PositionRow { get; set; }

    /// <summary>Rotation in 90-degree clockwise steps (0-3).</summary>
    public int Rotation { get; set; }
}
