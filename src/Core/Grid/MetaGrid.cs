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

    /// <summary>
    /// Move a bag to a new origin position on the meta-grid.
    /// Validates that the new position is in-bounds and does not overlap other bags.
    /// Reagents fully contained within the bag are carried along (translated by the same delta).
    /// Reagents spanning multiple bags are NOT moved — they stay in place.
    /// </summary>
    /// <returns>True if successfully moved, false if the move is invalid.</returns>
    public bool MoveBag(Bag bag, Vector2I newOrigin)
    {
        if (!_placedBags.Contains(bag))
            return false;

        var origin = bag.PositionOnMetaGrid!.Value;
        var delta = newOrigin - origin;

        // Identify reagents fully contained within this bag
        var contained = GetFullyContainedReagents(bag);

        // Temporarily clear occupancy for contained reagents so their cells
        // don't interfere with the bag move validation
        foreach (var reagent in contained)
            ClearReagentCellOccupancy(reagent);

        // Check new positions are valid
        var newPositions = bag.GetMetaGridPositions(newOrigin);
        foreach (var pos in newPositions)
        {
            if (!IsInBounds(pos))
            {
                RestoreReagentCellOccupancy(contained);
                return false;
            }

            var cell = GetCell(pos)!;
            // Cell must be free or already owned by this bag (for overlapping moves)
            if (cell.IsInBag && cell.BagOwner != bag)
            {
                RestoreReagentCellOccupancy(contained);
                return false;
            }
        }

        // Pre-check: reject if any locked non-contained reagent would be orphaned
        if (HasLockedOrphanedReagent(bag, contained, newPositions))
        {
            RestoreReagentCellOccupancy(contained);
            return false;
        }

        // Clear old cell ownership
        foreach (var cell in bag.OwnedCells)
        {
            cell.BagOwner = null;
        }
        bag.OwnedCells.Clear();

        // Assign new cell ownership
        bag.PositionOnMetaGrid = newOrigin;
        foreach (var pos in newPositions)
        {
            var cell = GetCell(pos)!;
            cell.BagOwner = bag;
            bag.OwnedCells.Add(cell);
        }

        // Translate contained reagent positions by the same delta
        foreach (var reagent in contained)
            reagent.PlacedPosition = reagent.PlacedPosition!.Value + delta;

        // Restore cell occupancy at the new positions
        RestoreReagentCellOccupancy(contained);

        // Remove spanning reagents that now have cells outside any bag
        RemoveOrphanedReagents(contained);

        return true;
    }

    /// <summary>
    /// Check whether a bag can be moved to a new origin (validation only, no mutation).
    /// </summary>
    public bool CanMoveBag(Bag bag, Vector2I newOrigin)
    {
        if (!_placedBags.Contains(bag))
            return false;

        var newPositions = bag.GetMetaGridPositions(newOrigin);
        foreach (var pos in newPositions)
        {
            if (!IsInBounds(pos))
                return false;

            var cell = GetCell(pos)!;
            if (cell.IsInBag && cell.BagOwner != bag)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Rotate a bag 90 degrees clockwise in place. The bag stays at the same origin
    /// but its shape rotates, changing which cells it covers.
    /// Validates that the rotated position is in-bounds and does not overlap other bags.
    /// Reagents fully contained within the bag are rotated along with it.
    /// Reagents spanning multiple bags are NOT rotated — they stay in place.
    /// </summary>
    /// <returns>True if successfully rotated, false if the rotation is invalid.</returns>
    public bool RotateBag(Bag bag)
    {
        if (!_placedBags.Contains(bag))
            return false;

        var origin = bag.PositionOnMetaGrid!.Value;

        // Identify contained reagents and record their current cell positions
        var contained = GetFullyContainedReagents(bag);

        // Record bag shape dimensions before rotation (needed for position transform)
        var oldShape = bag.GetRotatedShape();
        int oldShapeRows = oldShape.GetLength(0);

        // Temporarily clear occupancy for contained reagents
        foreach (var reagent in contained)
            ClearReagentCellOccupancy(reagent);

        // Compute new positions after rotation
        var oldRotation = bag.Rotation;
        bag.Rotation = oldRotation + 1;
        var newPositions = bag.GetMetaGridPositions(origin);

        // Validate new positions
        foreach (var pos in newPositions)
        {
            if (!IsInBounds(pos))
            {
                bag.Rotation = oldRotation; // Revert
                RestoreReagentCellOccupancy(contained);
                return false;
            }

            var cell = GetCell(pos)!;
            if (cell.IsInBag && cell.BagOwner != bag)
            {
                bag.Rotation = oldRotation; // Revert
                RestoreReagentCellOccupancy(contained);
                return false;
            }
        }

        // Pre-check: reject if any locked non-contained reagent would be orphaned
        if (HasLockedOrphanedReagent(bag, contained, newPositions))
        {
            bag.Rotation = oldRotation; // Revert
            RestoreReagentCellOccupancy(contained);
            return false;
        }

        // Clear old cell ownership
        foreach (var cell in bag.OwnedCells)
        {
            cell.BagOwner = null;
        }
        bag.OwnedCells.Clear();

        // Assign new cell ownership with rotated shape
        foreach (var pos in newPositions)
        {
            var cell = GetCell(pos)!;
            cell.BagOwner = bag;
            bag.OwnedCells.Add(cell);
        }

        // Rotate contained reagent positions around the bag origin
        foreach (var reagent in contained)
        {
            // Transform each of the reagent's old cell positions through the bag rotation
            var oldCells = reagent.GetCurrentMetaGridPositions();
            var newCells = new List<Vector2I>(oldCells.Count);
            foreach (var cellPos in oldCells)
            {
                var local = cellPos - origin;
                // 90° CW: (col, row) → (oldShapeRows - 1 - row, col)
                var rotatedLocal = new Vector2I(oldShapeRows - 1 - local.Row, local.Col);
                newCells.Add(origin + rotatedLocal);
            }

            // Rotate the reagent itself so its shape matches the new cell arrangement
            reagent.Rotation = reagent.Rotation + 1;

            // Set PlacedPosition to the min corner of the new cells
            int minCol = newCells[0].Col, minRow = newCells[0].Row;
            for (int i = 1; i < newCells.Count; i++)
            {
                if (newCells[i].Col < minCol) minCol = newCells[i].Col;
                if (newCells[i].Row < minRow) minRow = newCells[i].Row;
            }
            reagent.PlacedPosition = new Vector2I(minCol, minRow);
        }

        // Restore cell occupancy at the new positions
        RestoreReagentCellOccupancy(contained);

        // Remove spanning reagents that now have cells outside any bag
        RemoveOrphanedReagents(contained);

        return true;
    }

    /// <summary>
    /// Check if any locked non-contained reagent would have cells orphaned
    /// (outside any bag) after the bag moves to new positions.
    /// </summary>
    private bool HasLockedOrphanedReagent(Bag bag, List<Reagent> contained, List<Vector2I> newBagPositions)
    {
        var newPositionSet = new HashSet<Vector2I>(newBagPositions);
        var containedSet = new HashSet<Reagent>(contained);

        // Find cells that the bag currently covers but won't after the move
        var abandonedCells = new HashSet<Vector2I>();
        foreach (var cell in bag.OwnedCells)
        {
            if (!newPositionSet.Contains(cell.Position))
                abandonedCells.Add(cell.Position);
        }

        if (abandonedCells.Count == 0)
            return false;

        // Check if any locked, non-contained reagent has cells in abandoned positions
        foreach (var reagent in _placedReagents)
        {
            if (containedSet.Contains(reagent))
                continue;
            if (!reagent.IsLocked)
                continue;

            foreach (var pos in reagent.GetCurrentMetaGridPositions())
            {
                if (abandonedCells.Contains(pos))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get all reagents whose cells are ALL within the given bag.
    /// Reagents spanning multiple bags are excluded.
    /// </summary>
    private List<Reagent> GetFullyContainedReagents(Bag bag)
    {
        var bagCellSet = new HashSet<Vector2I>();
        foreach (var cell in bag.OwnedCells)
            bagCellSet.Add(cell.Position);

        // Collect unique reagents in this bag
        var reagentsInBag = new HashSet<Reagent>();
        foreach (var cell in bag.OwnedCells)
        {
            if (cell.OccupyingReagent is Reagent r)
                reagentsInBag.Add(r);
        }

        // Filter to only those fully contained
        var contained = new List<Reagent>();
        foreach (var reagent in reagentsInBag)
        {
            var positions = reagent.GetCurrentMetaGridPositions();
            bool allInBag = true;
            foreach (var pos in positions)
            {
                if (!bagCellSet.Contains(pos))
                {
                    allInBag = false;
                    break;
                }
            }
            if (allInBag)
                contained.Add(reagent);
        }

        return contained;
    }

    /// <summary>
    /// Clear a reagent's cell occupancy from the grid without changing its PlacedPosition.
    /// Used temporarily during bag movement to avoid interference.
    /// </summary>
    private void ClearReagentCellOccupancy(Reagent reagent)
    {
        foreach (var pos in reagent.GetCurrentMetaGridPositions())
        {
            var cell = GetCell(pos);
            if (cell != null && cell.OccupyingReagent == reagent)
                cell.OccupyingReagent = null;
        }
    }

    /// <summary>
    /// Restore cell occupancy for a list of reagents at their current PlacedPosition.
    /// </summary>
    private void RestoreReagentCellOccupancy(List<Reagent> reagents)
    {
        foreach (var reagent in reagents)
        {
            foreach (var pos in reagent.GetCurrentMetaGridPositions())
            {
                var cell = GetCell(pos);
                if (cell != null)
                    cell.OccupyingReagent = reagent;
            }
        }
    }

    /// <summary>
    /// After a bag move/rotate, remove any placed reagents that now have cells
    /// outside any bag. These are spanning reagents that were left in place when
    /// one of their bags moved away. Bypasses IsLocked since the bag is gone.
    /// </summary>
    private void RemoveOrphanedReagents(List<Reagent> excluded)
    {
        var excludedSet = new HashSet<Reagent>(excluded);
        // Snapshot to avoid modifying list during iteration
        var snapshot = new List<Reagent>(_placedReagents);
        foreach (var reagent in snapshot)
        {
            if (excludedSet.Contains(reagent))
                continue;

            var positions = reagent.GetCurrentMetaGridPositions();
            bool hasOrphanedCell = false;
            foreach (var pos in positions)
            {
                var cell = GetCell(pos);
                if (cell == null || cell.BagOwner == null)
                {
                    hasOrphanedCell = true;
                    break;
                }
            }

            if (hasOrphanedCell)
                ForceRemoveReagent(reagent);
        }
    }

    /// <summary>
    /// Force-remove a reagent from the grid, bypassing IsLocked.
    /// Used for cleanup when a bag moves away from under a spanning reagent.
    /// </summary>
    private void ForceRemoveReagent(Reagent reagent)
    {
        _placedReagents.Remove(reagent);
        foreach (var pos in reagent.GetCurrentMetaGridPositions())
        {
            var cell = GetCell(pos);
            if (cell != null && cell.OccupyingReagent == reagent)
                cell.OccupyingReagent = null;
        }
        reagent.PlacedPosition = null;
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
    /// Gravity is advisory (not a hard gate) — use GetGravityViolations() for warnings.
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

        // Gravity is advisory only (warnings via GetGravityViolations),
        // not a hard placement gate. Use PlaceReagentWithFall() for auto-fall behavior.

        // Heavy constraint is advisory only (warnings via GetHeavyViolations),
        // not a hard placement gate.

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
    /// Place a reagent at the fallen position determined by gravity.
    /// Calls CalculateFallPosition() to find the resting position, then PlaceReagent() there.
    /// In gravity bags, all reagents fall; outside gravity bags, only Heavy reagents fall.
    /// </summary>
    /// <returns>True if successfully placed, false if placement is invalid.</returns>
    public bool PlaceReagentWithFall(Reagent reagent, Vector2I position)
    {
        var fallenPos = CalculateFallPosition(reagent, position);
        return PlaceReagent(reagent, fallenPos);
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
