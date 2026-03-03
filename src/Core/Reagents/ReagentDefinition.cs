using System.Collections.Generic;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Core.Reagents;

/// <summary>
/// The data definition for a reagent type. Immutable template from which runtime Reagent instances are created.
/// Shape is a bool[rows, cols] matrix where true = cell exists, false = void.
/// Each cell can have its own set of aspects (per-cell aspects).
/// </summary>
public class ReagentDefinition
{
    /// <summary>Unique identifier for this reagent type.</summary>
    public string Id { get; }

    /// <summary>Display name.</summary>
    public string Name { get; }

    /// <summary>
    /// The shape of the reagent as a bool matrix [rows, cols].
    /// true = cell exists, false = void. Origin is top-left.
    /// </summary>
    public bool[,] Shape { get; }

    /// <summary>Number of rows in the shape matrix.</summary>
    public int ShapeRows => Shape.GetLength(0);

    /// <summary>Number of columns in the shape matrix.</summary>
    public int ShapeCols => Shape.GetLength(1);

    /// <summary>Weight value (contributes to carrying capacity).</summary>
    public float Weight { get; }

    /// <summary>Value when brought home (for scoring/quests/currency).</summary>
    public int Value { get; }

    /// <summary>Rarity tier.</summary>
    public Rarity Rarity { get; }

    /// <summary>
    /// Aspects for each cell, indexed by flattened position (left-to-right, top-to-bottom)
    /// through the shape's true cells. The i-th entry corresponds to the i-th true cell
    /// in row-major order. Null or empty list means no per-cell aspects.
    /// </summary>
    public IReadOnlyList<AspectSet> AspectsPerCell { get; }

    /// <summary>Number of cells this reagent occupies.</summary>
    public int CellCount => ShapeUtil.CellCount(Shape);

    public ReagentDefinition(string id, string name, bool[,] shape,
        float weight = 1f, int value = 1, Rarity rarity = Rarity.Common,
        IList<AspectSet>? aspectsPerCell = null)
    {
        Id = id;
        Name = name;
        Shape = shape;
        Weight = weight;
        Value = value;
        Rarity = rarity;

        // If aspects provided, use them. Otherwise, create empty sets for each cell.
        int cellCount = ShapeUtil.CellCount(shape);
        if (aspectsPerCell != null && aspectsPerCell.Count == cellCount)
        {
            AspectsPerCell = new List<AspectSet>(aspectsPerCell).AsReadOnly();
        }
        else
        {
            var empty = new List<AspectSet>(cellCount);
            for (int i = 0; i < cellCount; i++)
                empty.Add(AspectSet.Empty);
            AspectsPerCell = empty.AsReadOnly();
        }
    }

    /// <summary>Get cell positions in the base (unrotated) shape.</summary>
    public List<Vector2I> GetLocalCellPositions() => ShapeUtil.GetCellPositions(Shape);

    // --- Example reagent shapes for testing ---

    /// <summary>A single cell reagent.</summary>
    public static ReagentDefinition SingleCell => new("single", "Pebble", new bool[,]
    {
        { true },
    });

    /// <summary>A horizontal 2-cell domino.</summary>
    public static ReagentDefinition Domino => new("domino", "Crystal Shard", new bool[,]
    {
        { true, true },
    });

    /// <summary>A vertical 3-cell bar.</summary>
    public static ReagentDefinition TriBar => new("tri_bar", "Wand Fragment", new bool[,]
    {
        { true },
        { true },
        { true },
    });

    /// <summary>An L-shaped tetromino.</summary>
    public static ReagentDefinition LShape => new("l_shape", "Dragon's Tooth", new bool[,]
    {
        { true, false },
        { true, false },
        { true, true  },
    }, weight: 3f, value: 5, rarity: Rarity.Uncommon);

    /// <summary>A T-shaped tetromino.</summary>
    public static ReagentDefinition TShape => new("t_shape", "Star Jelly", new bool[,]
    {
        { true, true, true },
        { false, true, false },
    }, weight: 2f, value: 4, rarity: Rarity.Uncommon);

    /// <summary>A 2x2 square.</summary>
    public static ReagentDefinition Square => new("square", "Cinnabar Block", new bool[,]
    {
        { true, true },
        { true, true },
    }, weight: 4f, value: 6, rarity: Rarity.Rare);
}
