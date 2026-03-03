namespace AlchemistCourier.Core.Grid;

/// <summary>
/// The atomic unit of the spatial system. Every position on the meta-grid is a GridCell.
/// Cells can be owned by a bag, occupied by a reagent, damaged, or locked.
/// </summary>
public class GridCell
{
    /// <summary>Position of this cell on the meta-grid (col, row).</summary>
    public Vector2I Position { get; }

    /// <summary>The bag that owns this cell, or null if this cell is bare meta-grid (not in any bag).</summary>
    public object? BagOwner { get; set; }

    /// <summary>The reagent occupying this cell, or null if empty.</summary>
    public object? OccupyingReagent { get; set; }

    /// <summary>Damaged cells cannot hold reagents (can be mended).</summary>
    public bool IsDamaged { get; set; }

    /// <summary>Locked cells cannot have reagents moved or removed.</summary>
    public bool IsLocked { get; set; }

    /// <summary>Whether this cell belongs to a bag.</summary>
    public bool IsInBag => BagOwner != null;

    /// <summary>Whether this cell has a reagent in it.</summary>
    public bool IsOccupied => OccupyingReagent != null;

    /// <summary>Whether a reagent can be placed in this cell.</summary>
    public bool IsAvailable => IsInBag && !IsOccupied && !IsDamaged;

    public GridCell(Vector2I position)
    {
        Position = position;
    }

    public override string ToString()
    {
        var status = IsOccupied ? "occupied" : IsInBag ? "empty" : "no-bag";
        return $"Cell{Position} [{status}]";
    }
}
