using System.Collections.Generic;
using System.Linq;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Grid;

/// <summary>
/// The top-level rectangular grid that represents the player's carrying space.
/// Bags are placed onto this grid; reagents are placed into bag cells.
/// The meta-grid itself does not hold reagents directly — only bags do.
/// </summary>
public class MetaGrid
{
    public int Width { get; }
    public int Height { get; }

    private readonly GridCell[,] _cells;
    private readonly List<Bag> _placedBags = new();

    /// <summary>All bags currently placed on this meta-grid.</summary>
    public IReadOnlyList<Bag> PlacedBags => _placedBags.AsReadOnly();

    public MetaGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = new GridCell[width, height];

        for (int col = 0; col < width; col++)
        {
            for (int row = 0; row < height; row++)
            {
                _cells[col, row] = new GridCell(new Vector2I(col, row));
            }
        }
    }

    /// <summary>Check if a position is within the grid bounds.</summary>
    public bool IsInBounds(int col, int row) =>
        col >= 0 && col < Width && row >= 0 && row < Height;

    /// <summary>Check if a position is within the grid bounds.</summary>
    public bool IsInBounds(Vector2I pos) => IsInBounds(pos.Col, pos.Row);

    /// <summary>Get the cell at (col, row), or null if out of bounds.</summary>
    public GridCell? GetCell(int col, int row) =>
        IsInBounds(col, row) ? _cells[col, row] : null;

    /// <summary>Get the cell at a position, or null if out of bounds.</summary>
    public GridCell? GetCell(Vector2I pos) => GetCell(pos.Col, pos.Row);

    /// <summary>
    /// Get all cardinal (up/down/left/right) neighbors of a cell that are in bounds.
    /// </summary>
    public List<GridCell> GetAdjacentCells(GridCell cell)
    {
        var neighbors = new List<GridCell>(4);
        foreach (var dir in Vector2I.CardinalDirections)
        {
            var neighborPos = cell.Position + dir;
            var neighbor = GetCell(neighborPos);
            if (neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }
        return neighbors;
    }

    /// <summary>
    /// Get all cardinal neighbors of a position that are in bounds.
    /// </summary>
    public List<GridCell> GetAdjacentCells(Vector2I pos)
    {
        var cell = GetCell(pos);
        return cell != null ? GetAdjacentCells(cell) : new List<GridCell>();
    }

    /// <summary>Get all cells that currently have a reagent in them.</summary>
    public List<GridCell> GetAllOccupiedCells()
    {
        var occupied = new List<GridCell>();
        for (int col = 0; col < Width; col++)
        {
            for (int row = 0; row < Height; row++)
            {
                if (_cells[col, row].IsOccupied)
                {
                    occupied.Add(_cells[col, row]);
                }
            }
        }
        return occupied;
    }

    /// <summary>Get all cells that belong to a bag (have a BagOwner).</summary>
    public List<GridCell> GetAllBagCells()
    {
        var bagCells = new List<GridCell>();
        for (int col = 0; col < Width; col++)
        {
            for (int row = 0; row < Height; row++)
            {
                if (_cells[col, row].IsInBag)
                {
                    bagCells.Add(_cells[col, row]);
                }
            }
        }
        return bagCells;
    }

    /// <summary>Get all cells on the grid (for iteration).</summary>
    public IEnumerable<GridCell> GetAllCells()
    {
        for (int col = 0; col < Width; col++)
        {
            for (int row = 0; row < Height; row++)
            {
                yield return _cells[col, row];
            }
        }
    }

    // --- Bag Placement ---

    /// <summary>
    /// Check if a bag can be placed at the given position.
    /// Returns false if any target cell is out of bounds or already owned by another bag.
    /// </summary>
    public bool CanPlaceBag(Bag bag, Vector2I position)
    {
        var positions = bag.GetMetaGridPositions(position);
        foreach (var pos in positions)
        {
            if (!IsInBounds(pos))
                return false;

            var cell = GetCell(pos)!;
            if (cell.IsInBag)
                return false; // Cell already belongs to another bag
        }
        return true;
    }

    /// <summary>
    /// Place a bag on the meta-grid at the given position.
    /// Assigns bag ownership to all covered cells.
    /// </summary>
    /// <returns>True if successfully placed, false if placement is invalid.</returns>
    public bool PlaceBag(Bag bag, Vector2I position)
    {
        if (!CanPlaceBag(bag, position))
            return false;

        bag.PositionOnMetaGrid = position;
        bag.OwnedCells.Clear();

        var positions = bag.GetMetaGridPositions(position);
        foreach (var pos in positions)
        {
            var cell = GetCell(pos)!;
            cell.BagOwner = bag;
            bag.OwnedCells.Add(cell);
        }

        _placedBags.Add(bag);
        return true;
    }

    /// <summary>
    /// Remove a bag from the meta-grid.
    /// Clears bag ownership from all cells. Also clears any reagents in those cells.
    /// </summary>
    /// <returns>True if the bag was on this grid and was removed.</returns>
    public bool RemoveBag(Bag bag)
    {
        if (!_placedBags.Remove(bag))
            return false;

        foreach (var cell in bag.OwnedCells)
        {
            cell.BagOwner = null;
            cell.OccupyingReagent = null;
        }

        bag.OwnedCells.Clear();
        bag.PositionOnMetaGrid = null;
        return true;
    }

    /// <summary>Get all cells belonging to a specific bag.</summary>
    public List<GridCell> GetBagCells(Bag bag) => bag.OwnedCells;

    /// <summary>Get the bag that owns a specific cell, or null.</summary>
    public Bag? GetBagAtCell(Vector2I pos)
    {
        var cell = GetCell(pos);
        return cell?.BagOwner as Bag;
    }

    // --- Reagent Placement ---

    private readonly List<Reagent> _placedReagents = new();

    /// <summary>All reagents currently placed on this meta-grid.</summary>
    public IReadOnlyList<Reagent> PlacedReagents => _placedReagents.AsReadOnly();

    /// <summary>
    /// Check if a reagent can be placed at the given position with its current rotation.
    /// All target cells must be in-bounds, in a bag, not occupied, and not damaged.
    /// In Gravity bags, each cell must be supported from below.
    /// Spanning across multiple bags IS allowed.
    /// </summary>
    public bool CanPlaceReagent(Reagent reagent, Vector2I position)
    {
        var positions = reagent.GetMetaGridPositions(position);
        foreach (var pos in positions)
        {
            var cell = GetCell(pos);
            if (cell == null)
                return false; // Out of bounds

            if (!cell.IsAvailable)
                return false; // Not in bag, occupied, or damaged
        }
        if (positions.Count == 0)
            return false;

        // Check gravity constraints for cells in Gravity bags
        if (!IsGravityValid(positions))
            return false;

        return true;
    }

    /// <summary>
    /// Check if all positions in Gravity bags are supported from below.
    /// A cell is supported if: (a) it is at the bottom of the bag for its column, or
    /// (b) another reagent cell exists at (col, row+1) — either already placed or part of
    /// the positions being checked (self-support within a multi-cell reagent).
    /// </summary>
    private bool IsGravityValid(List<Vector2I> positions)
    {
        HashSet<Vector2I>? positionSet = null;

        foreach (var pos in positions)
        {
            var bag = GetBagAtCell(pos);
            if (bag == null || !bag.HasProperty(BagPropertyType.Gravity))
                continue;

            var below = new Vector2I(pos.Col, pos.Row + 1);

            // If the cell below is not part of this bag, we're at the bottom — supported
            var belowBag = GetBagAtCell(below);
            if (belowBag != bag)
                continue;

            // The cell below is in the same bag — check for support
            var belowCell = GetCell(below);
            if (belowCell != null && belowCell.IsOccupied)
                continue; // Supported by an already-placed reagent

            // Check if another cell of the reagent being placed provides support
            positionSet ??= new HashSet<Vector2I>(positions);
            if (positionSet.Contains(below))
                continue; // Self-supported by another cell of this reagent

            return false; // Unsupported — floating in a Gravity bag
        }

        return true;
    }

    /// <summary>
    /// Place a reagent on the meta-grid at the given position.
    /// Marks all covered cells as occupied by this reagent.
    /// </summary>
    /// <returns>True if successfully placed, false if placement is invalid.</returns>
    public bool PlaceReagent(Reagent reagent, Vector2I position)
    {
        if (!CanPlaceReagent(reagent, position))
            return false;

        reagent.PlacedPosition = position;

        var positions = reagent.GetMetaGridPositions(position);
        foreach (var pos in positions)
        {
            var cell = GetCell(pos)!;
            cell.OccupyingReagent = reagent;
        }

        _placedReagents.Add(reagent);
        return true;
    }

    /// <summary>
    /// Remove a reagent from the meta-grid.
    /// Clears occupancy from all cells it was in.
    /// </summary>
    /// <returns>True if the reagent was placed and was removed.</returns>
    public bool RemoveReagent(Reagent reagent)
    {
        if (!reagent.IsPlaced || !_placedReagents.Remove(reagent))
            return false;

        var positions = reagent.GetCurrentMetaGridPositions();
        foreach (var pos in positions)
        {
            var cell = GetCell(pos);
            if (cell != null && cell.OccupyingReagent == reagent)
            {
                cell.OccupyingReagent = null;
            }
        }

        reagent.PlacedPosition = null;
        return true;
    }

    /// <summary>Get the reagent at a specific cell, or null.</summary>
    public Reagent? GetReagentAtCell(Vector2I pos)
    {
        var cell = GetCell(pos);
        return cell?.OccupyingReagent as Reagent;
    }

    /// <summary>
    /// Get all distinct reagents that are cardinally adjacent to any cell of the given reagent.
    /// A reagent is adjacent if any of its cells shares a cardinal edge with any cell of the target reagent.
    /// The given reagent itself is excluded from results.
    /// </summary>
    public List<Reagent> GetAdjacentReagents(Reagent reagent)
    {
        var neighbors = new HashSet<Reagent>();
        foreach (var pos in reagent.GetCurrentMetaGridPositions())
        {
            foreach (var adjCell in GetAdjacentCells(pos))
            {
                if (adjCell.OccupyingReagent is Reagent neighbor && neighbor != reagent)
                {
                    neighbors.Add(neighbor);
                }
            }
        }
        return neighbors.ToList();
    }

    /// <summary>
    /// Get all distinct bags that a placed reagent spans across.
    /// A reagent can span multiple bags if it occupies cells owned by different bags.
    /// </summary>
    public List<Bag> GetBagsForReagent(Reagent reagent)
    {
        var bags = new HashSet<Bag>();
        foreach (var pos in reagent.GetCurrentMetaGridPositions())
        {
            var bag = GetBagAtCell(pos);
            if (bag != null)
                bags.Add(bag);
        }
        return bags.ToList();
    }
}
