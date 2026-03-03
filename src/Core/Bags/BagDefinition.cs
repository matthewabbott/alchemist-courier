using System.Collections.Generic;
using System.Linq;

namespace AlchemistCourier.Core.Bags;

/// <summary>
/// The data definition for a bag type. Immutable template from which runtime Bag instances are created.
/// Shape is a bool[rows, cols] matrix where true = cell exists, false = void.
/// </summary>
public class BagDefinition
{
    /// <summary>Unique identifier for this bag type.</summary>
    public string Id { get; }

    /// <summary>Display name.</summary>
    public string Name { get; }

    /// <summary>
    /// The shape of the bag as a bool matrix [rows, cols].
    /// true = this cell exists in the bag, false = void (no cell).
    /// Origin is top-left (row 0, col 0).
    /// </summary>
    public bool[,] Shape { get; }

    /// <summary>Number of rows in the shape matrix.</summary>
    public int ShapeRows => Shape.GetLength(0);

    /// <summary>Number of columns in the shape matrix.</summary>
    public int ShapeCols => Shape.GetLength(1);

    /// <summary>Special properties this bag has.</summary>
    public IReadOnlyList<BagPropertyType> Properties { get; }

    public BagDefinition(string id, string name, bool[,] shape, IEnumerable<BagPropertyType>? properties = null)
    {
        Id = id;
        Name = name;
        Shape = shape;
        Properties = (properties?.ToList() ?? new List<BagPropertyType>()).AsReadOnly();
    }

    /// <summary>Check if this bag has a specific property.</summary>
    public bool HasProperty(BagPropertyType type) => Properties.Contains(type);

    /// <summary>
    /// Get all local positions (col, row relative to bag origin) where a cell exists.
    /// </summary>
    public List<Grid.Vector2I> GetLocalCellPositions()
    {
        var positions = new List<Grid.Vector2I>();
        for (int row = 0; row < ShapeRows; row++)
        {
            for (int col = 0; col < ShapeCols; col++)
            {
                if (Shape[row, col])
                {
                    positions.Add(new Grid.Vector2I(col, row));
                }
            }
        }
        return positions;
    }

    /// <summary>Count of actual cells (true values) in the shape.</summary>
    public int CellCount => GetLocalCellPositions().Count;

    // --- Starter bag definitions for testing/prototyping ---

    /// <summary>A simple 3x3 square bag.</summary>
    public static BagDefinition SmallPouch => new("small_pouch", "Small Pouch", new bool[,]
    {
        { true, true, true },
        { true, true, true },
        { true, true, true },
    });

    /// <summary>A 4x3 rectangular bag.</summary>
    public static BagDefinition Satchel => new("satchel", "Satchel", new bool[,]
    {
        { true, true, true, true },
        { true, true, true, true },
        { true, true, true, true },
    });

    /// <summary>A 1x4 horizontal strip bag.</summary>
    public static BagDefinition BeltPouch => new("belt_pouch", "Belt Pouch", new bool[,]
    {
        { true, true, true, true },
    });

    /// <summary>An L-shaped bag.</summary>
    public static BagDefinition LBag => new("l_bag", "L-Bag", new bool[,]
    {
        { true, false },
        { true, false },
        { true, true  },
    });

    /// <summary>A 3x3 null bag (suppresses aspects).</summary>
    public static BagDefinition NullPouch => new("null_pouch", "Null Pouch", new bool[,]
    {
        { true, true, true },
        { true, true, true },
        { true, true, true },
    }, new[] { BagPropertyType.Null });
}
