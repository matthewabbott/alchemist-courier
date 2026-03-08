using System.Collections.Generic;
using System.Linq;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Highlight;
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
    /// In Gravity bags, Heavy reagents must be at the bottom (no non-Heavy below).
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

        // Check Heavy aspect constraint in Gravity bags
        if (!IsHeavyConstraintValid(reagent, positions))
            return false;

        // Check Sharp aspect constraint: Sharp reagents can't be on edge cells of non-Reinforced bags
        if (!IsSharpConstraintValid(reagent, positions))
            return false;

        // Check Fragile aspect constraint (currently no hard placement constraints)
        if (!IsFragileConstraintValid(reagent, positions))
            return false;

        return true;
    }

    /// <summary>
    /// Compute the "bearing cells" for a set of positions relative to a gravity direction.
    /// A bearing cell is one whose neighbor in the gravity direction is NOT part of the
    /// position set — i.e., the exposed underside of the shape. These are the cells
    /// that can provide or receive structural support.
    /// </summary>
    public static List<Vector2I> GetBearingCells(List<Vector2I> positions, Vector2I gravityDirection)
    {
        var positionSet = new HashSet<Vector2I>(positions);
        var bearing = new List<Vector2I>();

        foreach (var pos in positions)
        {
            var neighbor = pos + gravityDirection;
            if (!positionSet.Contains(neighbor))
                bearing.Add(pos);
        }

        return bearing;
    }

    /// <summary>
    /// Get detailed gravity constraint violations for the given positions.
    /// Uses bearing cells: a reagent is supported if ANY of its bearing cells
    /// (cells whose gravity-direction neighbor is not part of the same reagent)
    /// have support — either at the bag floor or resting on an occupied cell.
    /// The gravity direction comes from the bag containing the positions (A.3).
    /// </summary>
    public List<ConstraintViolation> GetGravityViolations(List<Vector2I> positions)
    {
        var violations = new List<ConstraintViolation>();

        // Determine gravity direction from the bag containing these positions.
        // If positions span multiple gravity bags (unusual), use the first one found.
        var gravityDirection = GetGravityDirectionForPositions(positions);
        if (gravityDirection == null)
            return violations; // No positions in gravity bags

        var gravDir = gravityDirection.Value;

        // Filter to positions that are in gravity bags
        var gravityPositions = new List<Vector2I>();
        foreach (var pos in positions)
        {
            var bag = GetBagAtCell(pos);
            if (bag != null && bag.HasProperty(BagPropertyType.Gravity))
                gravityPositions.Add(pos);
        }

        if (gravityPositions.Count == 0)
            return violations;

        // Compute bearing cells from all positions (including non-gravity ones for self-support)
        var bearingCells = GetBearingCells(positions, gravDir);

        // Only consider bearing cells that are in gravity bags
        var gravityBearingCells = new List<Vector2I>();
        foreach (var cell in bearingCells)
        {
            var bag = GetBagAtCell(cell);
            if (bag != null && bag.HasProperty(BagPropertyType.Gravity))
                gravityBearingCells.Add(cell);
        }

        if (gravityBearingCells.Count == 0)
            return violations;

        // Check if ANY bearing cell is supported
        foreach (var pos in gravityBearingCells)
        {
            var bag = GetBagAtCell(pos);
            var below = pos + gravDir;

            // If the cell in the gravity direction is not part of this bag, we're at the bag floor — supported
            var belowBag = GetBagAtCell(below);
            if (belowBag != bag)
                return violations; // At least one bearing cell supported → valid

            // The cell in the gravity direction is in the same bag — check for support from other reagents
            var belowCell = GetCell(below);
            if (belowCell != null && belowCell.IsOccupied)
                return violations; // At least one bearing cell supported → valid
        }

        // No bearing cell has support — report violation
        violations.Add(new ConstraintViolation(
            ConstraintType.GravityUnsupported,
            gravityBearingCells,
            "Reagent is unsupported in Gravity bag — no bearing cell has support"));

        return violations;
    }

    /// <summary>
    /// Determine the gravity direction vector for a set of positions.
    /// Returns the gravity direction from the first gravity bag found, or null
    /// if no positions are in a gravity bag.
    /// </summary>
    private Vector2I? GetGravityDirectionForPositions(List<Vector2I> positions)
    {
        foreach (var pos in positions)
        {
            var bag = GetBagAtCell(pos);
            if (bag != null && bag.HasProperty(BagPropertyType.Gravity))
                return bag.GravityDirection.ToVector();
        }
        return null;
    }

    private bool IsGravityValid(List<Vector2I> positions) =>
        GetGravityViolations(positions).Count == 0;

    /// <summary>
    /// Get detailed Heavy constraint violations for the given reagent and positions.
    /// In Gravity bags, Heavy reagent cells must be at or "below" (in gravity direction)
    /// all non-Heavy cells. The gravity direction comes from the bag (A.3).
    /// </summary>
    public List<ConstraintViolation> GetHeavyViolations(Reagent reagent, List<Vector2I> positions)
    {
        var violations = new List<ConstraintViolation>();
        bool isHeavy = reagent.HasAspect(AspectIds.Heavy);

        foreach (var pos in positions)
        {
            var bag = GetBagAtCell(pos);
            if (bag == null || !bag.HasProperty(BagPropertyType.Gravity))
                continue;

            // Gravity direction for this bag (A.3)
            var gravDir = bag.GravityDirection.ToVector();
            // "Against gravity" is the opposite direction (checking upward from heavy,
            // or downward from non-heavy, relative to gravity direction)
            var againstGravity = new Vector2I(-gravDir.Col, -gravDir.Row);

            if (isHeavy)
            {
                // Heavy cell: scan in gravity direction — should not find non-heavy below
                var checkPos = pos + gravDir;
                while (IsInBounds(checkPos))
                {
                    var checkBag = GetBagAtCell(checkPos);
                    if (checkBag != bag)
                        break;

                    var occupant = GetReagentAtCell(checkPos);
                    if (occupant != null && !occupant.HasAspect(AspectIds.Heavy))
                    {
                        violations.Add(new ConstraintViolation(
                            ConstraintType.HeavyAboveNonHeavy,
                            new List<Vector2I> { pos },
                            "Heavy reagent must be below non-Heavy reagents in Gravity bag",
                            AspectIds.Heavy));
                        break; // One violation per cell is enough
                    }

                    checkPos = checkPos + gravDir;
                }
            }
            else
            {
                // Non-heavy cell: scan against gravity — should not find heavy above
                var checkPos = pos + againstGravity;
                while (IsInBounds(checkPos))
                {
                    var checkBag = GetBagAtCell(checkPos);
                    if (checkBag != bag)
                        break;

                    var occupant = GetReagentAtCell(checkPos);
                    if (occupant != null && occupant.HasAspect(AspectIds.Heavy))
                    {
                        violations.Add(new ConstraintViolation(
                            ConstraintType.HeavyAboveNonHeavy,
                            new List<Vector2I> { pos },
                            "Non-Heavy reagent cannot be placed above Heavy reagent in Gravity bag",
                            AspectIds.Heavy));
                        break;
                    }

                    checkPos = checkPos + againstGravity;
                }
            }
        }

        return violations;
    }

    private bool IsHeavyConstraintValid(Reagent reagent, List<Vector2I> positions) =>
        GetHeavyViolations(reagent, positions).Count == 0;

    /// <summary>
    /// Get detailed Sharp constraint violations for the given reagent and positions.
    /// Sharp reagents can't be on edge cells of non-Reinforced bags.
    /// </summary>
    public List<ConstraintViolation> GetSharpViolations(Reagent reagent, List<Vector2I> positions)
    {
        var violations = new List<ConstraintViolation>();

        if (!reagent.HasAspect(AspectIds.Sharp))
            return violations;

        foreach (var pos in positions)
        {
            var bag = GetBagAtCell(pos);
            if (bag == null)
                continue;

            if (bag.HasProperty(BagPropertyType.Reinforced))
                continue;

            if (bag.IsEdgeCell(pos))
            {
                violations.Add(new ConstraintViolation(
                    ConstraintType.SharpOnEdge,
                    new List<Vector2I> { pos },
                    "Sharp reagent on edge cell of non-Reinforced bag",
                    AspectIds.Sharp));
            }
        }

        return violations;
    }

    private bool IsSharpConstraintValid(Reagent reagent, List<Vector2I> positions) =>
        GetSharpViolations(reagent, positions).Count == 0;

    /// <summary>
    /// Get detailed Fragile constraint violations for the given reagent and positions.
    /// Fragile reagents are allowed in all bag types (including Gravity bags).
    /// Future: will detect crushing (heavy resting on fragile), thermal fracture,
    /// and piercing (sharp adjacency) as pre-confirm warnings.
    /// </summary>
    public List<ConstraintViolation> GetFragileViolations(Reagent reagent, List<Vector2I> positions)
    {
        var violations = new List<ConstraintViolation>();

        // Fragile reagents have no hard placement constraints.
        // Breaking conditions (crushing, thermal fracture, piercing) are
        // evaluated as live warnings and at confirm time, not as placement blocks.

        return violations;
    }

    private bool IsFragileConstraintValid(Reagent reagent, List<Vector2I> positions) =>
        GetFragileViolations(reagent, positions).Count == 0;

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
        if (!reagent.IsPlaced || reagent.IsLocked || !_placedReagents.Remove(reagent))
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

    /// <summary>
    /// Calculates the final resting position for a reagent subject to gravity.
    /// Falling rules:
    ///   - Inside gravity bags: ALL reagents fall in the bag's gravity direction (A.3).
    ///   - Outside gravity bags: only Heavy reagents fall (downward by default).
    ///   - Non-heavy in non-gravity bag: no falling.
    /// The reagent falls until it cannot shift further (cell occupied, out of bag, etc.),
    /// which naturally ensures at least one bearing cell has support.
    /// Returns the input position if the reagent should not fall or cannot fall further.
    /// </summary>
    public Vector2I CalculateFallPosition(Reagent reagent, Vector2I dropPosition)
    {
        var dropPositions = reagent.GetMetaGridPositions(dropPosition);
        if (dropPositions.Count == 0)
            return dropPosition;

        // Verify all cells are in a single bag and determine bag type
        Bag? primaryBag = null;
        bool allInGravityBag = true;

        foreach (var pos in dropPositions)
        {
            var cell = GetCell(pos);
            if (cell == null || !cell.IsInBag)
                return dropPosition;

            var bag = GetBagAtCell(pos);
            if (bag == null)
                return dropPosition;

            if (primaryBag == null)
                primaryBag = bag;
            else if (bag != primaryBag)
                return dropPosition; // Spans multiple bags — don't fall

            if (!bag.HasProperty(BagPropertyType.Gravity))
                allInGravityBag = false;
        }

        // Determine if reagent should fall:
        // In gravity bag: ALL reagents fall
        // Outside gravity bag: only Heavy reagents fall
        bool shouldFall = allInGravityBag || reagent.HasAspect(AspectIds.Heavy);
        if (!shouldFall)
            return dropPosition;

        // Gravity direction: use bag's direction if in gravity bag, otherwise default Down (A.3)
        var gravityDirection = (allInGravityBag && primaryBag != null)
            ? primaryBag.GravityDirection.ToVector()
            : Vector2I.Down;

        // Shift in gravity direction one step at a time, checking all cells fit
        var bestPos = dropPosition;
        for (int shift = 1; ; shift++)
        {
            var candidate = new Vector2I(
                dropPosition.Col + gravityDirection.Col * shift,
                dropPosition.Row + gravityDirection.Row * shift);
            var candidatePositions = reagent.GetMetaGridPositions(candidate);

            bool valid = true;
            foreach (var pos in candidatePositions)
            {
                var cell = GetCell(pos);
                if (cell == null || !cell.IsInBag || GetBagAtCell(pos) != primaryBag || cell.IsOccupied)
                {
                    valid = false;
                    break;
                }
            }

            if (!valid) break;
            bestPos = candidate;
        }

        return bestPos;
    }
}
