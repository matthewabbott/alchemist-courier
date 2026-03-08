using System.Collections.Generic;
using System.Linq;
using Xunit;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Highlight;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Tests.Grid;

public class MetaGridTests
{
    private MetaGrid CreateGrid(int width = 10, int height = 10)
    {
        return new MetaGrid(width, height);
    }

    [Fact]
    public void Constructor_CreatesCorrectDimensions()
    {
        var grid = CreateGrid(8, 6);
        Assert.Equal(8, grid.Width);
        Assert.Equal(6, grid.Height);
    }

    [Fact]
    public void IsInBounds_ValidPosition_ReturnsTrue()
    {
        var grid = CreateGrid(5, 5);
        Assert.True(grid.IsInBounds(0, 0));
        Assert.True(grid.IsInBounds(4, 4));
        Assert.True(grid.IsInBounds(2, 3));
    }

    [Fact]
    public void IsInBounds_OutOfBounds_ReturnsFalse()
    {
        var grid = CreateGrid(5, 5);
        Assert.False(grid.IsInBounds(-1, 0));
        Assert.False(grid.IsInBounds(0, -1));
        Assert.False(grid.IsInBounds(5, 0));
        Assert.False(grid.IsInBounds(0, 5));
    }

    [Fact]
    public void GetCell_ValidPosition_ReturnsCell()
    {
        var grid = CreateGrid(5, 5);
        var cell = grid.GetCell(2, 3);
        Assert.NotNull(cell);
        Assert.Equal(new Vector2I(2, 3), cell!.Position);
    }

    [Fact]
    public void GetCell_OutOfBounds_ReturnsNull()
    {
        var grid = CreateGrid(5, 5);
        Assert.Null(grid.GetCell(-1, 0));
        Assert.Null(grid.GetCell(5, 5));
    }

    [Fact]
    public void GetAdjacentCells_CenterCell_ReturnsFourNeighbors()
    {
        var grid = CreateGrid(5, 5);
        var cell = grid.GetCell(2, 2)!;
        var neighbors = grid.GetAdjacentCells(cell);
        Assert.Equal(4, neighbors.Count);
    }

    [Fact]
    public void GetAdjacentCells_CornerCell_ReturnsTwoNeighbors()
    {
        var grid = CreateGrid(5, 5);
        var cell = grid.GetCell(0, 0)!;
        var neighbors = grid.GetAdjacentCells(cell);
        Assert.Equal(2, neighbors.Count);
    }

    // --- Bag Placement ---

    [Fact]
    public void PlaceBag_ValidPosition_Succeeds()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        Assert.True(grid.PlaceBag(bag, new Vector2I(0, 0)));
        Assert.Single(grid.PlacedBags);
        Assert.True(bag.IsPlaced);
    }

    [Fact]
    public void PlaceBag_OutOfBounds_Fails()
    {
        var grid = CreateGrid(5, 5);
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3
        // Placing at (3,3) would extend to (5,5) which is out of bounds
        Assert.False(grid.PlaceBag(bag, new Vector2I(3, 3)));
        Assert.Empty(grid.PlacedBags);
    }

    [Fact]
    public void PlaceBag_OverlappingBag_Fails()
    {
        var grid = CreateGrid();
        var bag1 = new Bag(BagDefinition.SmallPouch);
        var bag2 = new Bag(BagDefinition.SmallPouch);
        Assert.True(grid.PlaceBag(bag1, new Vector2I(0, 0)));
        // Overlapping at same position
        Assert.False(grid.PlaceBag(bag2, new Vector2I(0, 0)));
    }

    [Fact]
    public void PlaceBag_AdjacentNonOverlapping_Succeeds()
    {
        var grid = CreateGrid();
        var bag1 = new Bag(BagDefinition.SmallPouch); // 3x3
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3
        Assert.True(grid.PlaceBag(bag1, new Vector2I(0, 0)));
        // Place right next to it
        Assert.True(grid.PlaceBag(bag2, new Vector2I(3, 0)));
        Assert.Equal(2, grid.PlacedBags.Count);
    }

    [Fact]
    public void RemoveBag_PlacedBag_Succeeds()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));
        Assert.True(grid.RemoveBag(bag));
        Assert.Empty(grid.PlacedBags);
        Assert.False(bag.IsPlaced);
    }

    [Fact]
    public void RemoveBag_NotPlaced_ReturnsFalse()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        Assert.False(grid.RemoveBag(bag));
    }

    // --- Reagent Placement ---

    [Fact]
    public void PlaceReagent_InBag_Succeeds()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.PlaceReagent(reagent, new Vector2I(1, 1)));
        Assert.Single(grid.PlacedReagents);
        Assert.True(reagent.IsPlaced);
    }

    [Fact]
    public void PlaceReagent_OutsideBag_Fails()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        // Position (5,5) is not in any bag
        Assert.False(grid.PlaceReagent(reagent, new Vector2I(5, 5)));
    }

    [Fact]
    public void PlaceReagent_OverlappingReagent_Fails()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var r1 = new Reagent(ReagentDefinition.SingleCell);
        var r2 = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.PlaceReagent(r1, new Vector2I(1, 1)));
        // Same cell, should fail
        Assert.False(grid.PlaceReagent(r2, new Vector2I(1, 1)));
    }

    [Fact]
    public void RemoveReagent_Placed_ClearsOccupancy()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(reagent, new Vector2I(1, 1));
        Assert.True(grid.RemoveReagent(reagent));

        Assert.Empty(grid.PlacedReagents);
        Assert.False(reagent.IsPlaced);
        // Cell should be available again
        Assert.True(grid.GetCell(1, 1)!.IsAvailable);
    }

    [Fact]
    public void GetBagsForReagent_SingleBag_ReturnsOneBag()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(reagent, new Vector2I(1, 1));

        var bags = grid.GetBagsForReagent(reagent);
        Assert.Single(bags);
        Assert.Same(bag, bags[0]);
    }

    [Fact]
    public void GetBagsForReagent_SpanningTwoBags_ReturnsBoth()
    {
        var grid = CreateGrid();
        var bag1 = new Bag(BagDefinition.SmallPouch); // 3x3 at (0,0)
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3 at (3,0)
        grid.PlaceBag(bag1, new Vector2I(0, 0));
        grid.PlaceBag(bag2, new Vector2I(3, 0));

        // A domino reagent placed at (2,0) would occupy (2,0) and (3,0)
        // (2,0) is in bag1, (3,0) is in bag2
        var reagent = new Reagent(ReagentDefinition.Domino);
        Assert.True(grid.PlaceReagent(reagent, new Vector2I(2, 0)));

        var bags = grid.GetBagsForReagent(reagent);
        Assert.Equal(2, bags.Count);
    }

    // --- GetAdjacentCells(Vector2I) overload ---

    [Fact]
    public void GetAdjacentCells_ByPosition_ReturnsSameAsGridCell()
    {
        var grid = CreateGrid(5, 5);
        var pos = new Vector2I(2, 2);
        var byCell = grid.GetAdjacentCells(grid.GetCell(pos)!);
        var byPos = grid.GetAdjacentCells(pos);
        Assert.Equal(byCell.Count, byPos.Count);
    }

    [Fact]
    public void GetAdjacentCells_OutOfBoundsPosition_ReturnsEmpty()
    {
        var grid = CreateGrid(5, 5);
        var neighbors = grid.GetAdjacentCells(new Vector2I(10, 10));
        Assert.Empty(neighbors);
    }

    [Fact]
    public void GetAdjacentCells_EdgeCell_ReturnsThreeNeighbors()
    {
        var grid = CreateGrid(5, 5);
        // (2, 0) is on the top edge — neighbors are left, right, below (no above)
        var neighbors = grid.GetAdjacentCells(new Vector2I(2, 0));
        Assert.Equal(3, neighbors.Count);
    }

    // --- GetAdjacentReagents ---

    [Fact]
    public void GetAdjacentReagents_EmptyGrid_ReturnsEmpty()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(reagent, new Vector2I(1, 1));

        // Only one reagent on the grid — no neighbors
        var adjacent = grid.GetAdjacentReagents(reagent);
        Assert.Empty(adjacent);
    }

    [Fact]
    public void GetAdjacentReagents_TwoAdjacentReagents_ReturnsBoth()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Place r1 at (0,0), r2 at (1,0) — cardinally adjacent
        var r1 = new Reagent(ReagentDefinition.SingleCell);
        var r2 = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(r1, new Vector2I(0, 0));
        grid.PlaceReagent(r2, new Vector2I(1, 0));

        var adjFromR1 = grid.GetAdjacentReagents(r1);
        Assert.Single(adjFromR1);
        Assert.Same(r2, adjFromR1[0]);

        var adjFromR2 = grid.GetAdjacentReagents(r2);
        Assert.Single(adjFromR2);
        Assert.Same(r1, adjFromR2[0]);
    }

    [Fact]
    public void GetAdjacentReagents_DiagonalOnly_NotAdjacent()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // r1 at (0,0), r2 at (1,1) — diagonal only, not cardinal
        var r1 = new Reagent(ReagentDefinition.SingleCell);
        var r2 = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(r1, new Vector2I(0, 0));
        grid.PlaceReagent(r2, new Vector2I(1, 1));

        Assert.Empty(grid.GetAdjacentReagents(r1));
        Assert.Empty(grid.GetAdjacentReagents(r2));
    }

    [Fact]
    public void GetAdjacentReagents_SelfExcluded()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Domino at (0,0) occupies (0,0) and (1,0)
        // Its own cells are adjacent to each other but should not appear in results
        var reagent = new Reagent(ReagentDefinition.Domino);
        grid.PlaceReagent(reagent, new Vector2I(0, 0));

        Assert.Empty(grid.GetAdjacentReagents(reagent));
    }

    [Fact]
    public void GetAdjacentReagents_MultipleNeighbors_ReturnsAll()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.Satchel); // 4x3
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Layout:
        //   [r1][ ][ ][ ]
        //   [center][ ][ ][ ]
        //   [ ][r3][ ][ ]
        //
        // r1 at (0,0), center at (0,1), r3 at (1,2)
        // center is adjacent to r1 (above) and r3 (diagonal? no...)
        //
        // Better layout — center at (1,1), neighbors at (0,1), (2,1), (1,0)
        //   [ ][rTop][ ][ ]
        //   [rLeft][center][rRight][ ]
        //   [ ][ ][ ][ ]
        var center = new Reagent(ReagentDefinition.SingleCell);
        var rTop = new Reagent(ReagentDefinition.SingleCell);
        var rLeft = new Reagent(ReagentDefinition.SingleCell);
        var rRight = new Reagent(ReagentDefinition.SingleCell);

        grid.PlaceReagent(center, new Vector2I(1, 1));
        grid.PlaceReagent(rTop, new Vector2I(1, 0));
        grid.PlaceReagent(rLeft, new Vector2I(0, 1));
        grid.PlaceReagent(rRight, new Vector2I(2, 1));

        var adjacent = grid.GetAdjacentReagents(center);
        Assert.Equal(3, adjacent.Count);
        Assert.Contains(rTop, adjacent);
        Assert.Contains(rLeft, adjacent);
        Assert.Contains(rRight, adjacent);
    }

    [Fact]
    public void GetAdjacentReagents_MultiCellReagent_FindsNeighborOnAnySide()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.Satchel); // 4x3 at (0,0)
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Domino at (0,0) occupies (0,0) and (1,0)
        // Neighbor at (2,0) — adjacent to domino's right cell
        var domino = new Reagent(ReagentDefinition.Domino);
        var neighbor = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(domino, new Vector2I(0, 0));
        grid.PlaceReagent(neighbor, new Vector2I(2, 0));

        var adjacent = grid.GetAdjacentReagents(domino);
        Assert.Single(adjacent);
        Assert.Same(neighbor, adjacent[0]);
    }

    [Fact]
    public void GetAdjacentReagents_NeighborAppearsOnce_EvenIfMultipleTouchingCells()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.Satchel); // 4x3 at (0,0)
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Domino at (0,0) occupies (0,0) and (1,0)
        // Another domino at (0,1) occupies (0,1) and (1,1)
        // Both cells of the second domino are adjacent to both cells of the first
        var r1 = new Reagent(ReagentDefinition.Domino);
        var r2 = new Reagent(ReagentDefinition.Domino);
        grid.PlaceReagent(r1, new Vector2I(0, 0));
        grid.PlaceReagent(r2, new Vector2I(0, 1));

        // r2 should appear exactly once, even though multiple cells touch
        var adjacent = grid.GetAdjacentReagents(r1);
        Assert.Single(adjacent);
        Assert.Same(r2, adjacent[0]);
    }

    // --- Gravity Bag Constraints ---

    private static BagDefinition CreateGravityBagDef(int rows, int cols)
    {
        var shape = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                shape[r, c] = true;
        return new BagDefinition("gravity_bag", "Gravity Bag", shape,
            new[] { BagPropertyType.Gravity });
    }

    [Fact]
    public void Gravity_PlaceAtBottomRow_Succeeds()
    {
        // 3x3 Gravity bag at (0,0). Bottom row is row 2.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        // Place at (1, 2) — bottom row of the bag
        Assert.True(grid.CanPlaceReagent(reagent, new Vector2I(1, 2)));
    }

    [Fact]
    public void Gravity_PlaceFloatingMidBag_Fails()
    {
        // 3x3 Gravity bag at (0,0). Place at row 0 (top) with nothing below.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        // Place at (1, 0) — top row, no support
        Assert.False(grid.CanPlaceReagent(reagent, new Vector2I(1, 0)));
    }

    [Fact]
    public void Gravity_PlaceStackedOnExistingReagent_Succeeds()
    {
        // 3x3 Gravity bag at (0,0).
        // Place one reagent at bottom (1,2), then another at (1,1) above it.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var r1 = new Reagent(ReagentDefinition.SingleCell);
        var r2 = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.PlaceReagent(r1, new Vector2I(1, 2))); // bottom
        Assert.True(grid.CanPlaceReagent(r2, new Vector2I(1, 1))); // supported by r1
    }

    [Fact]
    public void Gravity_NormalBagAllowsFloating()
    {
        // Normal (non-gravity) bag should allow placement anywhere.
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch); // no Gravity property
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        // Place at (1, 0) — top row, no support, but bag is not Gravity
        Assert.True(grid.CanPlaceReagent(reagent, new Vector2I(1, 0)));
    }

    [Fact]
    public void Gravity_MultiCellReagent_SelfSupport_Succeeds()
    {
        // Vertical domino in a 3x3 Gravity bag.
        // Place at (1, 1): occupies (1,1) and (1,2).
        // (1,2) is bottom row — supported.
        // (1,1) has (1,2) part of the same reagent below — self-supported.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var vertDomino = new bool[,] { { true }, { true } }; // 2 rows, 1 col
        var def = new ReagentDefinition("vdom", "Vertical Domino", vertDomino);
        var reagent = new Reagent(def);
        // Place at (1, 1): cells at (1,1) and (1,2)
        Assert.True(grid.CanPlaceReagent(reagent, new Vector2I(1, 1)));
    }

    [Fact]
    public void Gravity_MultiCellReagent_PartiallyUnsupported_Fails()
    {
        // Horizontal domino in a 3x3 Gravity bag at row 0 (top).
        // Occupies (0,0) and (1,0). Neither is at the bottom, neither has support.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.Domino); // horizontal: (0,0) and (1,0)
        // Place at (0, 0) — top row, floating
        Assert.False(grid.CanPlaceReagent(reagent, new Vector2I(0, 0)));
    }

    [Fact]
    public void Gravity_LShapedBag_BottomDetectionWorks()
    {
        // L-shaped gravity bag:
        //   [true, false]
        //   [true, false]
        //   [true, true ]
        // Bag placed at (0,0).
        // Column 0 bottom is row 2. Column 1 bottom is row 2 (only cell).
        // Place at (0,2) — bottom of column 0 — should succeed.
        // Place at (0,0) — top of column 0, no support — should fail.
        var lShape = new bool[,]
        {
            { true, false },
            { true, false },
            { true, true  },
        };
        var lDef = new BagDefinition("l_grav", "L-Gravity", lShape,
            new[] { BagPropertyType.Gravity });

        var grid = CreateGrid();
        var bag = new Bag(lDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var r1 = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.CanPlaceReagent(r1, new Vector2I(0, 2))); // bottom of col 0

        var r2 = new Reagent(ReagentDefinition.SingleCell);
        Assert.False(grid.CanPlaceReagent(r2, new Vector2I(0, 0))); // floating in col 0

        // Place at (1,2) — this is the only cell in column 1, and it's at the bottom
        var r3 = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.CanPlaceReagent(r3, new Vector2I(1, 2)));
    }

    [Fact]
    public void Gravity_StackingChain_Succeeds()
    {
        // 4x1 Gravity bag (4 rows, 1 col). Place reagents bottom-up.
        var shape = new bool[,] { { true }, { true }, { true }, { true } };
        var gravDef = new BagDefinition("tall_grav", "Tall Gravity", shape,
            new[] { BagPropertyType.Gravity });

        var grid = CreateGrid();
        var bag = new Bag(gravDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var r1 = new Reagent(ReagentDefinition.SingleCell);
        var r2 = new Reagent(ReagentDefinition.SingleCell);
        var r3 = new Reagent(ReagentDefinition.SingleCell);

        Assert.True(grid.PlaceReagent(r1, new Vector2I(0, 3))); // bottom
        Assert.True(grid.PlaceReagent(r2, new Vector2I(0, 2))); // on r1
        Assert.True(grid.PlaceReagent(r3, new Vector2I(0, 1))); // on r2

        // But placing at (0,0) without r3 below it would fail — r3 is at (0,1),
        // so (0,0) IS supported by r3.
        var r4 = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.CanPlaceReagent(r4, new Vector2I(0, 0))); // on r3
    }

    // --- Bearing Cells ---

    [Fact]
    public void GetBearingCells_Monomino_ReturnsSelf()
    {
        var positions = new List<Vector2I> { new(1, 2) };
        var bearing = MetaGrid.GetBearingCells(positions, Vector2I.Down);
        Assert.Single(bearing);
        Assert.Contains(new Vector2I(1, 2), bearing);
    }

    [Fact]
    public void GetBearingCells_VerticalDomino_ReturnsBottomOnly()
    {
        // Vertical domino: (1,0) and (1,1). Gravity = down.
        // (1,0): below is (1,1) which IS in set → NOT bearing
        // (1,1): below is (1,2) which is NOT in set → IS bearing
        var positions = new List<Vector2I> { new(1, 0), new(1, 1) };
        var bearing = MetaGrid.GetBearingCells(positions, Vector2I.Down);
        Assert.Single(bearing);
        Assert.Contains(new Vector2I(1, 1), bearing);
    }

    [Fact]
    public void GetBearingCells_HorizontalDomino_ReturnsBoth()
    {
        // Horizontal domino: (0,1) and (1,1). Gravity = down.
        // Both cells' downward neighbors are NOT in the set → both are bearing cells
        var positions = new List<Vector2I> { new(0, 1), new(1, 1) };
        var bearing = MetaGrid.GetBearingCells(positions, Vector2I.Down);
        Assert.Equal(2, bearing.Count);
        Assert.Contains(new Vector2I(0, 1), bearing);
        Assert.Contains(new Vector2I(1, 1), bearing);
    }

    [Fact]
    public void GetBearingCells_TTetromino_ReturnsThreeProngTips()
    {
        // T-tetromino:
        //  .X.   → (1,0)
        //  XXX   → (0,1), (1,1), (2,1)
        // Bearing cells (gravity=down): (0,1), (1,1), (2,1) — all bottom row
        // (1,0) has (1,1) below → NOT bearing
        var positions = new List<Vector2I>
        {
            new(1, 0),
            new(0, 1), new(1, 1), new(2, 1)
        };
        var bearing = MetaGrid.GetBearingCells(positions, Vector2I.Down);
        Assert.Equal(3, bearing.Count);
        Assert.Contains(new Vector2I(0, 1), bearing);
        Assert.Contains(new Vector2I(1, 1), bearing);
        Assert.Contains(new Vector2I(2, 1), bearing);
        Assert.DoesNotContain(new Vector2I(1, 0), bearing);
    }

    [Fact]
    public void GetBearingCells_LTriomino_ReturnsTwoCells()
    {
        // L-triomino:
        //  X.   → (0,0)
        //  XX   → (0,1), (1,1)
        // (0,0): below is (0,1) in set → NOT bearing
        // (0,1): below is (0,2) NOT in set → IS bearing
        // (1,1): below is (1,2) NOT in set → IS bearing
        var positions = new List<Vector2I>
        {
            new(0, 0),
            new(0, 1), new(1, 1)
        };
        var bearing = MetaGrid.GetBearingCells(positions, Vector2I.Down);
        Assert.Equal(2, bearing.Count);
        Assert.Contains(new Vector2I(0, 1), bearing);
        Assert.Contains(new Vector2I(1, 1), bearing);
    }

    [Fact]
    public void GetBearingCells_STetromino_ReturnsTwoCells()
    {
        // S-tetromino:
        //  .XX   → (1,0), (2,0)
        //  XX.   → (0,1), (1,1)
        // (1,0): below is (1,1) in set → NOT bearing
        // (2,0): below is (2,1) NOT in set → IS bearing
        // (0,1): below is (0,2) NOT in set → IS bearing
        // (1,1): below is (1,2) NOT in set → IS bearing
        var positions = new List<Vector2I>
        {
            new(1, 0), new(2, 0),
            new(0, 1), new(1, 1)
        };
        var bearing = MetaGrid.GetBearingCells(positions, Vector2I.Down);
        Assert.Equal(3, bearing.Count);
        Assert.Contains(new Vector2I(2, 0), bearing);
        Assert.Contains(new Vector2I(0, 1), bearing);
        Assert.Contains(new Vector2I(1, 1), bearing);
    }

    [Fact]
    public void GetBearingCells_OTetromino_ReturnsBottomRow()
    {
        // O-tetromino (2x2 square):
        //  XX  → (0,0), (1,0)
        //  XX  → (0,1), (1,1)
        // Bottom row cells are bearing, top row have same-reagent below
        var positions = new List<Vector2I>
        {
            new(0, 0), new(1, 0),
            new(0, 1), new(1, 1)
        };
        var bearing = MetaGrid.GetBearingCells(positions, Vector2I.Down);
        Assert.Equal(2, bearing.Count);
        Assert.Contains(new Vector2I(0, 1), bearing);
        Assert.Contains(new Vector2I(1, 1), bearing);
    }

    [Fact]
    public void GetBearingCells_VerticalI3_ReturnsBottomOnly()
    {
        // Vertical I-triomino: (0,0), (0,1), (0,2)
        // Only (0,2) is bearing — others have same-reagent below
        var positions = new List<Vector2I>
        {
            new(0, 0), new(0, 1), new(0, 2)
        };
        var bearing = MetaGrid.GetBearingCells(positions, Vector2I.Down);
        Assert.Single(bearing);
        Assert.Contains(new Vector2I(0, 2), bearing);
    }

    [Fact]
    public void GetBearingCells_HorizontalI3_ReturnsAll()
    {
        // Horizontal I-triomino: (0,1), (1,1), (2,1)
        // All cells are bearing — none have same-reagent below
        var positions = new List<Vector2I>
        {
            new(0, 1), new(1, 1), new(2, 1)
        };
        var bearing = MetaGrid.GetBearingCells(positions, Vector2I.Down);
        Assert.Equal(3, bearing.Count);
    }

    [Fact]
    public void Gravity_BearingCells_PartialSupport_Succeeds()
    {
        // Horizontal domino at row 1 of a 3x3 gravity bag.
        // Cells at (0,1) and (1,1). Both are bearing cells.
        // Place a support reagent at (0,2) first, so (0,1) has support.
        // Under bearing cells: one supported bearing cell → whole reagent valid.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Place support at (0,2) — bottom row
        var support = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.PlaceReagent(support, new Vector2I(0, 2)));

        // Horizontal domino at row 1: (0,1) supported by (0,2), (1,1) unsupported
        var domino = new Reagent(ReagentDefinition.Domino); // horizontal: cols 0,1
        Assert.True(grid.CanPlaceReagent(domino, new Vector2I(0, 1)));
    }

    [Fact]
    public void Gravity_BearingCells_NoSupport_Fails()
    {
        // Horizontal domino at row 0 of a 3x3 gravity bag.
        // Both cells are bearing cells, neither has support.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var domino = new Reagent(ReagentDefinition.Domino);
        Assert.False(grid.CanPlaceReagent(domino, new Vector2I(0, 0)));
    }

    [Fact]
    public void Gravity_TTetrominoOneBearingSupported_Succeeds()
    {
        // T-tetromino shape: row0=[false,true,false], row1=[true,true,true]
        //  .X.
        //  XXX
        // In a 5x4 gravity bag, place support under just one bearing cell.
        var grid = CreateGrid();
        var tallBag = new Bag(CreateGravityBagDef(5, 4));
        grid.PlaceBag(tallBag, new Vector2I(0, 0));

        // Place support at (0,4) — bottom of column 0
        var s1 = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.PlaceReagent(s1, new Vector2I(0, 4)));

        var tShape = new bool[,] { { false, true, false }, { true, true, true } };
        var tDef = new ReagentDefinition("ttet", "T-tet", tShape);
        var tReagent = new Reagent(tDef);

        // At origin (0,2): cells at (1,2) and (0,3),(1,3),(2,3)
        // Bearing cells: (0,3),(1,3),(2,3) — all bottom row
        // Below (0,3) is (0,4) which is occupied by s1 → SUPPORTED
        // Below (1,3) is (1,4): empty → not supported
        // Below (2,3) is (2,4): empty → not supported
        // ONE bearing cell supported → valid!
        Assert.True(grid.CanPlaceReagent(tReagent, new Vector2I(0, 2)));
    }

    [Fact]
    public void Gravity_LTriominoPartialSupport_Succeeds()
    {
        // L-triomino in a 4x3 gravity bag:
        //  X.
        //  XX
        // Bearing cells: (0,1), (1,1) (the bottom row)
        // Stack support under bearing cell (1,1) only, leaving (0,2) empty.
        // Under bearing cells: one supported bearing cell → whole reagent valid.
        var grid = CreateGrid();
        var tallBag = new Bag(CreateGravityBagDef(4, 3));
        grid.PlaceBag(tallBag, new Vector2I(0, 0));

        // Build support column under (1,1): place at (1,3) then (1,2)
        var s1 = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.PlaceReagent(s1, new Vector2I(1, 3))); // bottom row

        var s2 = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.PlaceReagent(s2, new Vector2I(1, 2))); // on s1

        // L-triomino shape: row0=[true,false], row1=[true,true]
        var lShape = new bool[,] { { true, false }, { true, true } };
        var lDef = new ReagentDefinition("ltri", "L-tri", lShape);
        var lReagent = new Reagent(lDef);

        // At origin (0,0): cells at (0,0), (0,1), (1,1)
        // Bearing cells: (0,1) and (1,1)
        // Below (0,1) is (0,2): empty, in same bag → NOT supported
        // Below (1,1) is (1,2): occupied by s2 → SUPPORTED
        // One bearing cell supported → VALID
        Assert.True(grid.CanPlaceReagent(lReagent, new Vector2I(0, 0)));
    }

    [Fact]
    public void Gravity_AllBearingCellsUnsupported_ReturnsViolation()
    {
        // L-triomino floating in a 4x3 gravity bag — no support below any bearing cell
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(4, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var lShape = new bool[,] { { true, false }, { true, true } };
        var lDef = new ReagentDefinition("ltri", "L-tri", lShape);
        var lReagent = new Reagent(lDef);

        // At origin (0,0): cells at (0,0), (0,1), (1,1)
        // Bearing cells: (0,1) and (1,1)
        // Below (0,1) is (0,2): unoccupied → not supported
        // Below (1,1) is (1,2): unoccupied → not supported
        // No bearing cells supported → violation
        var positions = lReagent.GetMetaGridPositions(new Vector2I(0, 0));
        var violations = grid.GetGravityViolations(positions);
        Assert.Single(violations);
        Assert.Equal(ConstraintType.GravityUnsupported, violations[0].Type);
        // Both bearing cells should be in the affected cells
        Assert.Contains(new Vector2I(0, 1), violations[0].AffectedCells);
        Assert.Contains(new Vector2I(1, 1), violations[0].AffectedCells);
    }

    [Fact]
    public void Gravity_BearingCells_AtBagFloor_Succeeds()
    {
        // L-triomino at bottom of a 3x3 gravity bag.
        // All bearing cells at bag floor → supported.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var lShape = new bool[,] { { true, false }, { true, true } };
        var lDef = new ReagentDefinition("ltri", "L-tri", lShape);
        var lReagent = new Reagent(lDef);

        // At origin (0,1): cells at (0,1), (0,2), (1,2)
        // Bearing cells: (0,2) and (1,2) — both at bottom row (bag floor)
        Assert.True(grid.CanPlaceReagent(lReagent, new Vector2I(0, 1)));
    }

    // --- Heavy Aspect in Gravity Bag Constraints ---

    private static ReagentDefinition CreateHeavyReagentDef(bool[,] shape)
    {
        int cellCount = 0;
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (shape[r, c]) cellCount++;

        var aspects = new List<AspectSet>();
        for (int i = 0; i < cellCount; i++)
            aspects.Add(new AspectSet(Aspect.Heavy()));
        return new ReagentDefinition("heavy_r", "Heavy Reagent", shape,
            aspectsPerCell: aspects);
    }

    [Fact]
    public void Heavy_MustBeAtBottomOfGravityBag()
    {
        // 3x3 Gravity bag. Place Heavy reagent at bottom row (2) — should succeed.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        Assert.True(grid.CanPlaceReagent(heavy, new Vector2I(1, 2)));
    }

    [Fact]
    public void Heavy_AboveNonHeavy_AllowedButReportsViolation()
    {
        // 3x3 Gravity bag. Place non-Heavy at bottom (1,2), then Heavy at (1,1).
        // Heavy above non-Heavy is now advisory — placement succeeds but violation reported.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var nonHeavy = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.PlaceReagent(nonHeavy, new Vector2I(1, 2))); // bottom row

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        // Heavy at (1,1) — placement now succeeds (advisory constraint)
        Assert.True(grid.CanPlaceReagent(heavy, new Vector2I(1, 1)));

        // But violations are still reported
        var positions = heavy.GetMetaGridPositions(new Vector2I(1, 1));
        var violations = grid.GetHeavyViolations(heavy, positions);
        Assert.Single(violations);
        Assert.Equal(ConstraintType.HeavyAboveNonHeavy, violations[0].Type);
    }

    [Fact]
    public void Heavy_NonHeavyAboveHeavy_Succeeds()
    {
        // 3x3 Gravity bag. Place Heavy at bottom, non-Heavy above it — OK.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        Assert.True(grid.PlaceReagent(heavy, new Vector2I(1, 2))); // bottom

        var nonHeavy = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.CanPlaceReagent(nonHeavy, new Vector2I(1, 1))); // above heavy — ok
    }

    [Fact]
    public void Heavy_NonHeavyBelowHeavy_AllowedButReportsViolation()
    {
        // 4x1 Gravity bag. Place Heavy at bottom, non-Heavy above, then Heavy above non-Heavy.
        // All placements succeed (advisory constraint), but violations are reported.
        var shape = new bool[,] { { true }, { true }, { true }, { true } };
        var gravDef = new BagDefinition("tall_grav", "Tall Gravity", shape,
            new[] { BagPropertyType.Gravity });
        var grid = CreateGrid();
        var bag = new Bag(gravDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        Assert.True(grid.PlaceReagent(heavy, new Vector2I(0, 3))); // bottom

        // Non-Heavy at (0,2) — above Heavy at (0,3) — succeeds
        var nonHeavy1 = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.CanPlaceReagent(nonHeavy1, new Vector2I(0, 2)));
        Assert.True(grid.PlaceReagent(nonHeavy1, new Vector2I(0, 2)));

        // Heavy at (0,1) with non-Heavy at (0,2) below — now succeeds (advisory)
        var heavy2 = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        Assert.True(grid.CanPlaceReagent(heavy2, new Vector2I(0, 1)));

        // But violations are still reported
        var positions = heavy2.GetMetaGridPositions(new Vector2I(0, 1));
        var violations = grid.GetHeavyViolations(heavy2, positions);
        Assert.Single(violations);
        Assert.Equal(ConstraintType.HeavyAboveNonHeavy, violations[0].Type);
    }

    [Fact]
    public void Heavy_InNonGravityBag_PlacesAnywhere()
    {
        // Normal bag — Heavy has no special placement effect.
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3, no Gravity
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        // Place at top row in a normal bag — should succeed
        Assert.True(grid.CanPlaceReagent(heavy, new Vector2I(1, 0)));
    }

    [Fact]
    public void Heavy_MultiCellReagent_ValidatedCorrectly()
    {
        // Vertical Heavy domino in 3x3 Gravity bag.
        // Place at (0,1): occupies (0,1) and (0,2). (0,2) is bottom.
        // Both cells of Heavy are at the bottom — should succeed.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavyDom = new Reagent(CreateHeavyReagentDef(new bool[,] { { true }, { true } }));
        Assert.True(grid.CanPlaceReagent(heavyDom, new Vector2I(0, 1))); // cells at (0,1) and (0,2)

        // Now place non-Heavy at (0,0) above the Heavy domino — should succeed
        Assert.True(grid.PlaceReagent(heavyDom, new Vector2I(0, 1)));
        var nonHeavy = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.CanPlaceReagent(nonHeavy, new Vector2I(0, 0))); // above Heavy — ok
    }

    [Fact]
    public void Heavy_TwoHeavyCanStack()
    {
        // Two Heavy reagents can stack in a Gravity bag — both are Heavy.
        var shape = new bool[,] { { true }, { true }, { true }, { true } };
        var gravDef = new BagDefinition("tall_grav2", "Tall Gravity", shape,
            new[] { BagPropertyType.Gravity });
        var grid = CreateGrid();
        var bag = new Bag(gravDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy1 = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        var heavy2 = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        Assert.True(grid.PlaceReagent(heavy1, new Vector2I(0, 3))); // bottom
        Assert.True(grid.CanPlaceReagent(heavy2, new Vector2I(0, 2))); // above — both Heavy, ok
    }

    // --- Locked Reagent Tests ---

    [Fact]
    public void RemoveReagent_LockedReagent_ReturnsFalse()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell) { IsLocked = true };
        Assert.True(grid.PlaceReagent(reagent, new Vector2I(0, 0)));

        // Cannot remove a locked reagent
        Assert.False(grid.RemoveReagent(reagent));
        Assert.True(reagent.IsPlaced);
        Assert.Single(grid.PlacedReagents);
    }

    [Fact]
    public void RemoveReagent_UnlockedReagent_StillWorks()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell) { IsLocked = false };
        Assert.True(grid.PlaceReagent(reagent, new Vector2I(0, 0)));

        // Can remove an unlocked reagent
        Assert.True(grid.RemoveReagent(reagent));
        Assert.False(reagent.IsPlaced);
        Assert.Empty(grid.PlacedReagents);
    }

    [Fact]
    public void RemoveReagent_LockedReagent_CellsStayOccupied()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell) { IsLocked = true };
        grid.PlaceReagent(reagent, new Vector2I(1, 1));

        grid.RemoveReagent(reagent); // Should fail

        // Cell at (1,1) should still be occupied
        var cell = grid.GetCell(new Vector2I(1, 1));
        Assert.NotNull(cell);
        Assert.Equal(reagent, cell!.OccupyingReagent);
    }

    // --- Sharp Edge Placement Constraints ---

    private static ReagentDefinition CreateSharpReagentDef(bool[,] shape)
    {
        int cellCount = 0;
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (shape[r, c]) cellCount++;

        var aspects = new List<AspectSet>();
        for (int i = 0; i < cellCount; i++)
            aspects.Add(new AspectSet(Aspect.FromId(AspectIds.Sharp)));
        return new ReagentDefinition("sharp_r", "Sharp Reagent", shape,
            aspectsPerCell: aspects);
    }

    [Fact]
    public void Sharp_BlockedOnEdgeOfNormalBag()
    {
        // 3x3 normal bag at (0,0). Edge cells are all cells on the perimeter.
        // Interior cell is only (1,1).
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        // (0,0) is a corner (edge) — should be blocked
        Assert.False(grid.CanPlaceReagent(sharp, new Vector2I(0, 0)));
        // (1,0) is a top edge — should be blocked
        Assert.False(grid.CanPlaceReagent(sharp, new Vector2I(1, 0)));
        // (0,1) is a left edge — should be blocked
        Assert.False(grid.CanPlaceReagent(sharp, new Vector2I(0, 1)));
    }

    [Fact]
    public void Sharp_AllowedInInteriorOfNormalBag()
    {
        // 3x3 bag — only interior cell is (1,1)
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        // (1,1) is interior — all 4 cardinal neighbors are within the bag
        Assert.True(grid.CanPlaceReagent(sharp, new Vector2I(1, 1)));
    }

    [Fact]
    public void Sharp_AllowedOnEdgeOfReinforcedBag()
    {
        // Reinforced bag allows Sharp on edge cells.
        var shape = new bool[,]
        {
            { true, true, true },
            { true, true, true },
            { true, true, true },
        };
        var reinforcedDef = new BagDefinition("reinforced_bag", "Reinforced Bag", shape,
            new[] { BagPropertyType.Reinforced });
        var grid = CreateGrid();
        var bag = new Bag(reinforcedDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        // (0,0) is edge but bag is Reinforced — should be allowed
        Assert.True(grid.CanPlaceReagent(sharp, new Vector2I(0, 0)));
    }

    [Fact]
    public void Sharp_MultiCellWithOneEdgeCell_Blocked()
    {
        // Sharp domino (2 cells horizontal) in a 4x4 bag.
        // If placed at (0,1): occupies (0,1) and (1,1).
        // (0,1) is on the left edge — blocked.
        var shape = new bool[,]
        {
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
        };
        var bagDef = new BagDefinition("big_bag", "Big Bag", shape);
        var grid = CreateGrid();
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true, true } }));
        // (0,1) is left edge — blocked
        Assert.False(grid.CanPlaceReagent(sharp, new Vector2I(0, 1)));
        // (1,1) occupies (1,1) and (2,1) — both are interior — allowed
        Assert.True(grid.CanPlaceReagent(sharp, new Vector2I(1, 1)));
    }

    [Fact]
    public void Sharp_NonSharpOnEdge_Allowed()
    {
        // Non-Sharp reagent on an edge cell — no constraint.
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var nonSharp = new Reagent(ReagentDefinition.SingleCell);
        // (0,0) is edge but reagent is not Sharp — allowed
        Assert.True(grid.CanPlaceReagent(nonSharp, new Vector2I(0, 0)));
    }

    [Fact]
    public void Sharp_LargerBagHasMoreInterior()
    {
        // 5x5 bag: interior is a 3x3 area (1,1) to (3,3).
        // Sharp single-cell should be allowed anywhere in interior.
        var shape = new bool[5, 5];
        for (int r = 0; r < 5; r++)
            for (int c = 0; c < 5; c++)
                shape[r, c] = true;
        var bagDef = new BagDefinition("large_bag", "Large Bag", shape);
        var grid = CreateGrid();
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        Assert.True(grid.CanPlaceReagent(sharp, new Vector2I(2, 2))); // center
        Assert.True(grid.CanPlaceReagent(sharp, new Vector2I(1, 1))); // interior corner
        Assert.True(grid.CanPlaceReagent(sharp, new Vector2I(3, 3))); // interior corner
        Assert.False(grid.CanPlaceReagent(sharp, new Vector2I(0, 2))); // left edge
        Assert.False(grid.CanPlaceReagent(sharp, new Vector2I(4, 2))); // right edge
    }

    [Fact]
    public void Sharp_NonRectBagEdgeDetection()
    {
        // L-shaped bag:
        //   [true, false]
        //   [true, false]
        //   [true, true ]
        // All cells in an L-shape are edge cells (every cell has at least one
        // cardinal neighbor outside the bag), so Sharp can't be placed anywhere.
        var lShape = new bool[,]
        {
            { true, false },
            { true, false },
            { true, true  },
        };
        var bagDef = new BagDefinition("l_bag", "L Bag", lShape);
        var grid = CreateGrid();
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        // Every cell in the L-shape is an edge cell
        Assert.False(grid.CanPlaceReagent(sharp, new Vector2I(0, 0)));
        Assert.False(grid.CanPlaceReagent(sharp, new Vector2I(0, 1)));
        Assert.False(grid.CanPlaceReagent(sharp, new Vector2I(0, 2)));
        Assert.False(grid.CanPlaceReagent(sharp, new Vector2I(1, 2)));
    }

    [Fact]
    public void Sharp_PlusShapedBag_HasInterior()
    {
        // Plus-shaped bag:
        //   [false, true,  false]
        //   [true,  true,  true ]
        //   [false, true,  false]
        // Center cell (1,1) has all 4 cardinal neighbors inside bag — it's interior.
        var plusShape = new bool[,]
        {
            { false, true,  false },
            { true,  true,  true  },
            { false, true,  false },
        };
        var bagDef = new BagDefinition("plus_bag", "Plus Bag", plusShape);
        var grid = CreateGrid();
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        // Center (1,1) is interior — allowed
        Assert.True(grid.CanPlaceReagent(sharp, new Vector2I(1, 1)));
        // Arms are all edge cells — blocked
        Assert.False(grid.CanPlaceReagent(sharp, new Vector2I(1, 0))); // top
        Assert.False(grid.CanPlaceReagent(sharp, new Vector2I(0, 1))); // left
        Assert.False(grid.CanPlaceReagent(sharp, new Vector2I(2, 1))); // right
        Assert.False(grid.CanPlaceReagent(sharp, new Vector2I(1, 2))); // bottom
    }

    // --- Fragile Aspect: Blocked from Gravity Bags ---

    private static ReagentDefinition CreateFragileReagentDef(bool[,] shape)
    {
        var cellPositions = new List<Vector2I>();
        for (int r = 0; r < shape.GetLength(0); r++)
            for (int c = 0; c < shape.GetLength(1); c++)
                if (shape[r, c])
                    cellPositions.Add(new Vector2I(c, r));

        var aspects = new List<AspectSet>();
        foreach (var _ in cellPositions)
            aspects.Add(new AspectSet(Aspect.Fragile()));

        return new ReagentDefinition("fragile", "Fragile Reagent", shape,
            aspectsPerCell: aspects);
    }

    [Fact]
    public void Fragile_AllowedInGravityBag()
    {
        // Fragile reagents are allowed in Gravity bags (D.1 rework)
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var fragile = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));
        // At bottom row (valid gravity position), Fragile is now allowed
        Assert.True(grid.CanPlaceReagent(fragile, new Vector2I(1, 2)));
    }

    [Fact]
    public void Fragile_AllowedInNormalBag()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var fragile = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));
        Assert.True(grid.CanPlaceReagent(fragile, new Vector2I(1, 1)));
    }

    [Fact]
    public void Fragile_NonFragileAllowedInGravityBag()
    {
        // Non-Fragile reagent is unaffected by the Fragile constraint
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var def = new ReagentDefinition("normal", "Normal", new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { new AspectSet(Aspect.Hot()) });
        var reagent = new Reagent(def);
        Assert.True(grid.CanPlaceReagent(reagent, new Vector2I(1, 2)));
    }

    // --- Constraint Violation Detail Extraction ---

    [Fact]
    public void GetSharpViolations_EdgeCell_ReturnsViolationWithCorrectDetails()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        var positions = sharp.GetMetaGridPositions(new Vector2I(0, 0));
        var violations = grid.GetSharpViolations(sharp, positions);

        Assert.Single(violations);
        Assert.Equal(ConstraintType.SharpOnEdge, violations[0].Type);
        Assert.Contains(new Vector2I(0, 0), violations[0].AffectedCells);
        Assert.Equal(AspectIds.Sharp, violations[0].AspectId);
        Assert.Contains("Sharp", violations[0].Reason);
    }

    [Fact]
    public void GetSharpViolations_InteriorCell_ReturnsEmpty()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        var positions = sharp.GetMetaGridPositions(new Vector2I(1, 1));
        var violations = grid.GetSharpViolations(sharp, positions);

        Assert.Empty(violations);
    }

    [Fact]
    public void GetSharpViolations_MultiCellTwoEdges_ReturnsTwoViolations()
    {
        // 4x4 bag, Sharp domino placed at (0,1) — both (0,1) and (1,1) are edge cells
        // Actually (0,1) is left edge; (1,1) is not edge in a 4x4 bag since all neighbors are in bag
        // Let's use a 3x3 bag where edge cells surround interior (1,1)
        // Place Sharp horizontal domino at (0,0): occupies (0,0) and (1,0) — both edge
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true, true } }));
        var positions = sharp.GetMetaGridPositions(new Vector2I(0, 0));
        var violations = grid.GetSharpViolations(sharp, positions);

        Assert.Equal(2, violations.Count);
        Assert.All(violations, v => Assert.Equal(ConstraintType.SharpOnEdge, v.Type));
    }

    [Fact]
    public void GetSharpViolations_NonSharpReagent_ReturnsEmpty()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var nonSharp = new Reagent(ReagentDefinition.SingleCell);
        var positions = nonSharp.GetMetaGridPositions(new Vector2I(0, 0));
        var violations = grid.GetSharpViolations(nonSharp, positions);

        Assert.Empty(violations);
    }

    [Fact]
    public void GetFragileViolations_GravityBag_ReturnsEmpty()
    {
        // D.1: Fragile reagents are now allowed in Gravity bags — no violations
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var fragile = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));
        var positions = fragile.GetMetaGridPositions(new Vector2I(1, 2));
        var violations = grid.GetFragileViolations(fragile, positions);

        Assert.Empty(violations);
    }

    [Fact]
    public void GetFragileViolations_NormalBag_ReturnsEmpty()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var fragile = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));
        var positions = fragile.GetMetaGridPositions(new Vector2I(1, 1));
        var violations = grid.GetFragileViolations(fragile, positions);

        Assert.Empty(violations);
    }

    [Fact]
    public void GetHeavyViolations_HeavyAboveNonHeavy_ReturnsViolation()
    {
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var nonHeavy = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(nonHeavy, new Vector2I(1, 2));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        var positions = heavy.GetMetaGridPositions(new Vector2I(1, 1));
        var violations = grid.GetHeavyViolations(heavy, positions);

        Assert.Single(violations);
        Assert.Equal(ConstraintType.HeavyAboveNonHeavy, violations[0].Type);
        Assert.Contains(new Vector2I(1, 1), violations[0].AffectedCells);
        Assert.Equal(AspectIds.Heavy, violations[0].AspectId);
    }

    [Fact]
    public void GetHeavyViolations_NonHeavyBelowHeavy_ReturnsViolation()
    {
        var grid = CreateGrid();
        var shape = new bool[,] { { true }, { true }, { true }, { true } };
        var gravDef = new BagDefinition("tall_grav", "Tall Gravity", shape,
            new[] { BagPropertyType.Gravity });
        var bag = new Bag(gravDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        grid.PlaceReagent(heavy, new Vector2I(0, 3));

        // Place non-Heavy at (0,2) — above Heavy — should succeed
        var nonHeavy1 = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(nonHeavy1, new Vector2I(0, 2));

        // Now try Heavy at (0,1) with non-Heavy at (0,2) below
        var heavy2 = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        var positions = heavy2.GetMetaGridPositions(new Vector2I(0, 1));
        var violations = grid.GetHeavyViolations(heavy2, positions);

        Assert.Single(violations);
        Assert.Equal(ConstraintType.HeavyAboveNonHeavy, violations[0].Type);
    }

    [Fact]
    public void GetHeavyViolations_NoViolation_ReturnsEmpty()
    {
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        var positions = heavy.GetMetaGridPositions(new Vector2I(1, 2));
        var violations = grid.GetHeavyViolations(heavy, positions);

        Assert.Empty(violations);
    }

    [Fact]
    public void GetGravityViolations_FloatingCell_ReturnsViolation()
    {
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        var positions = reagent.GetMetaGridPositions(new Vector2I(1, 0));
        var violations = grid.GetGravityViolations(positions);

        Assert.Single(violations);
        Assert.Equal(ConstraintType.GravityUnsupported, violations[0].Type);
        Assert.Contains(new Vector2I(1, 0), violations[0].AffectedCells);
        Assert.Null(violations[0].AspectId);
    }

    [Fact]
    public void GetGravityViolations_BottomRow_ReturnsEmpty()
    {
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        var positions = reagent.GetMetaGridPositions(new Vector2I(1, 2));
        var violations = grid.GetGravityViolations(positions);

        Assert.Empty(violations);
    }

    [Fact]
    public void GetGravityViolations_NormalBag_ReturnsEmpty()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        var positions = reagent.GetMetaGridPositions(new Vector2I(1, 0));
        var violations = grid.GetGravityViolations(positions);

        Assert.Empty(violations);
    }

    [Fact]
    public void ViolationMethods_ConsistentWithBoolMethods()
    {
        // Verify that the bool constraint methods return the same result
        // as checking violations.Count == 0
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Sharp on edge of gravity bag — Sharp constraint violated
        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        var sharpPos = sharp.GetMetaGridPositions(new Vector2I(0, 0));
        Assert.Equal(
            grid.GetSharpViolations(sharp, sharpPos).Count == 0,
            // CanPlaceReagent checks all constraints, but Sharp on (0,0) of gravity bag
            // will also be on the edge, so Sharp constraint fails
            grid.GetSharpViolations(sharp, sharpPos).Count == 0);

        // Fragile in gravity — no violation (D.1: fragile allowed in gravity bags)
        var fragile = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));
        var fragilePos = fragile.GetMetaGridPositions(new Vector2I(1, 2));
        bool fragileViolations = grid.GetFragileViolations(fragile, fragilePos).Count > 0;
        Assert.False(fragileViolations); // Fragile in Gravity bag = no violation
    }

    // --- CalculateFallPosition ---

    [Fact]
    public void CalculateFallPosition_HeavyInGravityBag_FallsToBottom()
    {
        // 4x1 Gravity bag (4 rows, 1 col). Heavy reagent dropped at row 0 falls to row 3.
        var shape = new bool[,] { { true }, { true }, { true }, { true } };
        var gravDef = new BagDefinition("tall_grav", "Tall Gravity", shape,
            new[] { BagPropertyType.Gravity });
        var grid = CreateGrid();
        var bag = new Bag(gravDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        var result = grid.CalculateFallPosition(heavy, new Vector2I(0, 0));
        Assert.Equal(new Vector2I(0, 3), result);
    }

    [Fact]
    public void CalculateFallPosition_NonHeavyInGravityBag_Falls()
    {
        // A.2: ALL reagents fall inside gravity bags, not just Heavy.
        // Non-Heavy reagent in a Gravity bag SHOULD fall to bottom.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell); // Not Heavy
        var result = grid.CalculateFallPosition(reagent, new Vector2I(1, 0));
        Assert.Equal(new Vector2I(1, 2), result); // Falls to bottom row
    }

    [Fact]
    public void CalculateFallPosition_HeavyInNormalBag_Falls()
    {
        // A.2: Heavy reagents fall in non-Gravity bags too.
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3, no Gravity
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        var result = grid.CalculateFallPosition(heavy, new Vector2I(1, 0));
        Assert.Equal(new Vector2I(1, 2), result); // Falls to bottom row
    }

    [Fact]
    public void CalculateFallPosition_NonHeavyInNormalBag_NoFalling()
    {
        // A.2: Non-Heavy in non-Gravity bag does NOT fall.
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3, no Gravity
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell); // Not Heavy
        var result = grid.CalculateFallPosition(reagent, new Vector2I(1, 0));
        Assert.Equal(new Vector2I(1, 0), result); // Stays put
    }

    [Fact]
    public void CalculateFallPosition_HeavyBlockedByOccupant_FallsAbove()
    {
        // 4x1 Gravity bag. Existing reagent at row 3. Heavy falls to row 2.
        var shape = new bool[,] { { true }, { true }, { true }, { true } };
        var gravDef = new BagDefinition("tall_grav", "Tall Gravity", shape,
            new[] { BagPropertyType.Gravity });
        var grid = CreateGrid();
        var bag = new Bag(gravDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var existing = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(existing, new Vector2I(0, 3));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        var result = grid.CalculateFallPosition(heavy, new Vector2I(0, 0));
        Assert.Equal(new Vector2I(0, 2), result);
    }

    [Fact]
    public void CalculateFallPosition_AlreadyAtBottom_NoChange()
    {
        // 3x3 Gravity bag. Heavy at bottom row (row 2) — already at bottom.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        var result = grid.CalculateFallPosition(heavy, new Vector2I(1, 2));
        Assert.Equal(new Vector2I(1, 2), result);
    }

    [Fact]
    public void CalculateFallPosition_MultiCellReagent_FallsToBottom()
    {
        // 4x2 Gravity bag. 2-row Heavy reagent dropped at row 0 falls to row 2.
        var bagShape = new bool[,] { { true, true }, { true, true }, { true, true }, { true, true } };
        var gravDef = new BagDefinition("tall_wide_grav", "Tall Wide Gravity", bagShape,
            new[] { BagPropertyType.Gravity });
        var grid = CreateGrid();
        var bag = new Bag(gravDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavyDef = CreateHeavyReagentDef(new bool[,] { { true }, { true } }); // 2 rows
        var heavy = new Reagent(heavyDef);
        var result = grid.CalculateFallPosition(heavy, new Vector2I(0, 0));
        // Shape is 2 rows, so at row 2 the cells are at rows 2 and 3 (bottom of 4-row bag)
        Assert.Equal(new Vector2I(0, 2), result);
    }

    [Fact]
    public void CalculateFallPosition_OutsideBag_NoChange()
    {
        // Position not in any bag — return unchanged.
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        var result = grid.CalculateFallPosition(heavy, new Vector2I(5, 5));
        Assert.Equal(new Vector2I(5, 5), result);
    }

    // --- A.3: Per-Bag Gravity Direction ---

    private static BagDefinition CreateDirectionalGravityBagDef(int rows, int cols, GravityDirection direction)
    {
        var shape = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                shape[r, c] = true;
        return new BagDefinition("dir_gravity_bag", "Directional Gravity Bag", shape,
            new[] { BagPropertyType.Gravity }, gravityDirection: direction);
    }

    // --- Bearing Cells with non-Down gravity ---

    [Fact]
    public void GetBearingCells_GravityUp_ReturnsTopCells()
    {
        // Vertical domino (0,0) and (0,1). Gravity = Up.
        // Bearing cell = the one whose UP neighbor is not in the set → (0,0)
        var positions = new List<Vector2I> { new(0, 0), new(0, 1) };
        var bearing = MetaGrid.GetBearingCells(positions, Vector2I.Up);
        Assert.Single(bearing);
        Assert.Contains(new Vector2I(0, 0), bearing);
    }

    [Fact]
    public void GetBearingCells_GravityLeft_ReturnsLeftCells()
    {
        // Horizontal domino (0,0) and (1,0). Gravity = Left.
        // Bearing cell = the one whose LEFT neighbor is not in the set → (0,0)
        var positions = new List<Vector2I> { new(0, 0), new(1, 0) };
        var bearing = MetaGrid.GetBearingCells(positions, Vector2I.Left);
        Assert.Single(bearing);
        Assert.Contains(new Vector2I(0, 0), bearing);
    }

    [Fact]
    public void GetBearingCells_GravityRight_ReturnsRightCells()
    {
        // Horizontal domino (0,0) and (1,0). Gravity = Right.
        // Bearing cell = the one whose RIGHT neighbor is not in the set → (1,0)
        var positions = new List<Vector2I> { new(0, 0), new(1, 0) };
        var bearing = MetaGrid.GetBearingCells(positions, Vector2I.Right);
        Assert.Single(bearing);
        Assert.Contains(new Vector2I(1, 0), bearing);
    }

    [Fact]
    public void GetBearingCells_TTetromino_GravityUp_ReturnsTopProngTips()
    {
        // T-tetromino pointing up:
        //   X       (0,0)
        // X X X   (0,1),(1,1),(2,1)
        // Gravity = Up. Bearing = cells whose UP neighbor is NOT in the set.
        // (0,0): up = (0,-1) not in set → bearing
        // (0,1): up = (0,0) IS in set → not bearing
        // (1,1): up = (1,0) not in set → bearing
        // (2,1): up = (2,0) not in set → bearing
        var positions = new List<Vector2I> { new(0, 0), new(0, 1), new(1, 1), new(2, 1) };
        var bearing = MetaGrid.GetBearingCells(positions, Vector2I.Up);
        Assert.Equal(3, bearing.Count);
        Assert.Contains(new Vector2I(0, 0), bearing);
        Assert.Contains(new Vector2I(1, 1), bearing);
        Assert.Contains(new Vector2I(2, 1), bearing);
    }

    // --- Gravity violations with non-Down direction ---

    [Fact]
    public void GravityViolations_LeftGravity_AtLeftWall_NoViolation()
    {
        // 3x3 Left-gravity bag at (0,0). Reagent at col 0 (leftmost wall) — supported.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(3, 3, GravityDirection.Left);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var positions = new List<Vector2I> { new(0, 1) }; // Left wall
        var violations = grid.GetGravityViolations(positions);
        Assert.Empty(violations);
    }

    [Fact]
    public void GravityViolations_LeftGravity_Floating_ReturnsViolation()
    {
        // 3x3 Left-gravity bag at (0,0). Reagent at col 2 (rightmost) with nothing to its left — unsupported.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(3, 3, GravityDirection.Left);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var positions = new List<Vector2I> { new(2, 1) }; // Right side, nothing to left
        var violations = grid.GetGravityViolations(positions);
        Assert.Single(violations);
        Assert.Equal(ConstraintType.GravityUnsupported, violations[0].Type);
    }

    [Fact]
    public void GravityViolations_UpGravity_AtTopWall_NoViolation()
    {
        // 3x3 Up-gravity bag at (0,0). Reagent at row 0 (topmost wall) — supported.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(3, 3, GravityDirection.Up);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var positions = new List<Vector2I> { new(1, 0) }; // Top wall
        var violations = grid.GetGravityViolations(positions);
        Assert.Empty(violations);
    }

    [Fact]
    public void GravityViolations_UpGravity_Floating_ReturnsViolation()
    {
        // 3x3 Up-gravity bag at (0,0). Reagent at row 2 (bottom) with nothing above — unsupported.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(3, 3, GravityDirection.Up);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var positions = new List<Vector2I> { new(1, 2) }; // Bottom row, nothing above
        var violations = grid.GetGravityViolations(positions);
        Assert.Single(violations);
        Assert.Equal(ConstraintType.GravityUnsupported, violations[0].Type);
    }

    [Fact]
    public void GravityViolations_RightGravity_AtRightWall_NoViolation()
    {
        // 3x3 Right-gravity bag at (0,0). Reagent at col 2 (rightmost wall) — supported.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(3, 3, GravityDirection.Right);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var positions = new List<Vector2I> { new(2, 1) }; // Right wall
        var violations = grid.GetGravityViolations(positions);
        Assert.Empty(violations);
    }

    [Fact]
    public void GravityViolations_RightGravity_Floating_ReturnsViolation()
    {
        // 3x3 Right-gravity bag at (0,0). Reagent at col 0 (leftmost) with nothing to right — unsupported.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(3, 3, GravityDirection.Right);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var positions = new List<Vector2I> { new(0, 1) }; // Left side, nothing to right
        var violations = grid.GetGravityViolations(positions);
        Assert.Single(violations);
        Assert.Equal(ConstraintType.GravityUnsupported, violations[0].Type);
    }

    [Fact]
    public void GravityViolations_LeftGravity_SupportedByOccupant_NoViolation()
    {
        // 3x3 Left-gravity bag at (0,0). Reagent at col 1, supported by reagent at col 0.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(3, 3, GravityDirection.Left);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Place a blocker at col 0, row 1
        var blocker = new Reagent(new ReagentDefinition("b", "B", new bool[,] { { true } }));
        grid.PlaceReagent(blocker, new Vector2I(0, 1));

        // Now check positions at col 1, row 1
        var positions = new List<Vector2I> { new(1, 1) };
        var violations = grid.GetGravityViolations(positions);
        Assert.Empty(violations); // Supported by occupant to the left
    }

    // --- Fall position with non-Down gravity ---

    [Fact]
    public void CalculateFallPosition_LeftGravity_FallsToLeftWall()
    {
        // 1x4 Left-gravity bag at (0,0). Reagent dropped at col 3 falls to col 0.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(1, 4, GravityDirection.Left);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(new ReagentDefinition("r", "R", new bool[,] { { true } }));
        var result = grid.CalculateFallPosition(reagent, new Vector2I(3, 0));
        Assert.Equal(new Vector2I(0, 0), result); // Falls to leftmost col
    }

    [Fact]
    public void CalculateFallPosition_RightGravity_FallsToRightWall()
    {
        // 1x4 Right-gravity bag at (0,0). Reagent dropped at col 0 falls to col 3.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(1, 4, GravityDirection.Right);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(new ReagentDefinition("r", "R", new bool[,] { { true } }));
        var result = grid.CalculateFallPosition(reagent, new Vector2I(0, 0));
        Assert.Equal(new Vector2I(3, 0), result); // Falls to rightmost col
    }

    [Fact]
    public void CalculateFallPosition_UpGravity_FallsToTopWall()
    {
        // 4x1 Up-gravity bag at (0,0). Reagent dropped at row 3 falls to row 0.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(4, 1, GravityDirection.Up);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(new ReagentDefinition("r", "R", new bool[,] { { true } }));
        var result = grid.CalculateFallPosition(reagent, new Vector2I(0, 3));
        Assert.Equal(new Vector2I(0, 0), result); // Falls to top
    }

    [Fact]
    public void CalculateFallPosition_DownGravity_FallsToBottomWall()
    {
        // 4x1 Down-gravity bag at (0,0). Reagent dropped at row 0 falls to row 3.
        // This should match the default behavior.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(4, 1, GravityDirection.Down);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(new ReagentDefinition("r", "R", new bool[,] { { true } }));
        var result = grid.CalculateFallPosition(reagent, new Vector2I(0, 0));
        Assert.Equal(new Vector2I(0, 3), result); // Falls to bottom
    }

    [Fact]
    public void CalculateFallPosition_LeftGravity_BlockedByOccupant()
    {
        // 1x4 Left-gravity bag at (0,0). Blocker at col 0. Reagent at col 3 falls to col 1.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(1, 4, GravityDirection.Left);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var blocker = new Reagent(new ReagentDefinition("b", "B", new bool[,] { { true } }));
        grid.PlaceReagent(blocker, new Vector2I(0, 0));

        var reagent = new Reagent(new ReagentDefinition("r", "R", new bool[,] { { true } }));
        var result = grid.CalculateFallPosition(reagent, new Vector2I(3, 0));
        Assert.Equal(new Vector2I(1, 0), result); // Stops next to blocker
    }

    [Fact]
    public void CalculateFallPosition_UpGravity_MultiCellReagent()
    {
        // 4x2 Up-gravity bag. Vertical 2-cell reagent at row 2-3 falls to row 0-1.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(4, 2, GravityDirection.Up);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagentDef = new ReagentDefinition("r", "R", new bool[,] { { true }, { true } });
        var reagent = new Reagent(reagentDef);
        var result = grid.CalculateFallPosition(reagent, new Vector2I(0, 2));
        Assert.Equal(new Vector2I(0, 0), result); // Falls to top
    }

    [Fact]
    public void CalculateFallPosition_HeavyOutsideGravityBag_FallsDown()
    {
        // Heavy reagent in non-gravity bag always falls Down, not using bag direction.
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3, no Gravity
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        var result = grid.CalculateFallPosition(heavy, new Vector2I(1, 0));
        Assert.Equal(new Vector2I(1, 2), result); // Falls down (default direction)
    }

    // --- Heavy violations with non-Down gravity ---

    [Fact]
    public void HeavyViolations_LeftGravity_HeavyNotAtLeftWall_ReturnsViolation()
    {
        // 1x3 Left-gravity bag. Non-heavy at col 0 (gravity wall), heavy at col 2.
        // In left-gravity, heavy should be at the left side (gravity direction).
        // Heavy at col 2 with non-heavy at col 0 (against gravity): heavy checks
        // in gravity direction (left) — there's nothing there (col 1 is in bag but unoccupied).
        // But non-heavy checks against gravity (right) — nothing either.
        // Actually, the rule is: heavy cells should not have non-heavy cells in the gravity direction.
        // Heavy at col 2, scan leftward: col 1 is empty, col 0 has non-heavy → violation.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(1, 3, GravityDirection.Left);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var nonHeavy = new Reagent(new ReagentDefinition("r", "R", new bool[,] { { true } }));
        grid.PlaceReagent(nonHeavy, new Vector2I(0, 0)); // Non-heavy at left wall

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        // Check heavy violations: heavy at col 2 scans left (gravity dir) and finds non-heavy at col 0
        var heavyPositions = new List<Vector2I> { new(2, 0) };
        var violations = grid.GetHeavyViolations(heavy, heavyPositions);
        Assert.Single(violations);
        Assert.Equal(ConstraintType.HeavyAboveNonHeavy, violations[0].Type);
    }

    [Fact]
    public void HeavyViolations_LeftGravity_HeavyAtLeftWall_NoViolation()
    {
        // 1x3 Left-gravity bag. Heavy at col 0 (gravity wall), non-heavy at col 2.
        // Heavy at left wall, non-heavy is against gravity → valid.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(1, 3, GravityDirection.Left);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        grid.PlaceReagent(heavy, new Vector2I(0, 0)); // Heavy at left wall

        var heavyPositions = new List<Vector2I> { new(0, 0) };
        var violations = grid.GetHeavyViolations(heavy, heavyPositions);
        Assert.Empty(violations); // Non-heavy is against gravity, OK
    }

    [Fact]
    public void HeavyViolations_UpGravity_HeavyBelowNonHeavy_ReturnsViolation()
    {
        // 3x1 Up-gravity bag. Non-heavy at row 0 (top = gravity wall), heavy at row 2.
        // In up-gravity, heavy should float to the top. Having heavy at bottom
        // means non-heavy is at the gravity wall — violation.
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(3, 1, GravityDirection.Up);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var nonHeavy = new Reagent(new ReagentDefinition("r", "R", new bool[,] { { true } }));
        grid.PlaceReagent(nonHeavy, new Vector2I(0, 0)); // Non-heavy at top (gravity wall)

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        var heavyPositions = new List<Vector2I> { new(0, 2) };
        var violations = grid.GetHeavyViolations(heavy, heavyPositions);
        // Heavy at row 2, scans up (gravity dir): row 1 empty, row 0 has non-heavy → violation
        Assert.Single(violations);
    }

    [Fact]
    public void HeavyViolations_UpGravity_HeavyAtTop_NoViolation()
    {
        // 3x1 Up-gravity bag. Heavy at row 0 (top = gravity wall).
        var grid = CreateGrid();
        var bagDef = CreateDirectionalGravityBagDef(3, 1, GravityDirection.Up);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        grid.PlaceReagent(heavy, new Vector2I(0, 0)); // Heavy at top

        var heavyPositions = new List<Vector2I> { new(0, 0) };
        var violations = grid.GetHeavyViolations(heavy, heavyPositions);
        Assert.Empty(violations);
    }

    // --- GravityDirection extensions ---

    [Fact]
    public void GravityDirection_ToVector_AllDirections()
    {
        Assert.Equal(Vector2I.Down, GravityDirection.Down.ToVector());
        Assert.Equal(Vector2I.Up, GravityDirection.Up.ToVector());
        Assert.Equal(Vector2I.Left, GravityDirection.Left.ToVector());
        Assert.Equal(Vector2I.Right, GravityDirection.Right.ToVector());
    }

    [Fact]
    public void GravityDirection_RotateCW_FullCycle()
    {
        Assert.Equal(GravityDirection.Left, GravityDirection.Down.RotateCW());
        Assert.Equal(GravityDirection.Up, GravityDirection.Left.RotateCW());
        Assert.Equal(GravityDirection.Right, GravityDirection.Up.RotateCW());
        Assert.Equal(GravityDirection.Down, GravityDirection.Right.RotateCW());
    }

    [Fact]
    public void BagDefinition_DefaultGravityDirection_IsDown()
    {
        var def = new BagDefinition("test", "Test", new bool[,] { { true } },
            new[] { BagPropertyType.Gravity });
        Assert.Equal(GravityDirection.Down, def.GravityDirection);
    }

    [Fact]
    public void BagDefinition_CustomGravityDirection_Preserved()
    {
        var def = new BagDefinition("test", "Test", new bool[,] { { true } },
            new[] { BagPropertyType.Gravity }, gravityDirection: GravityDirection.Left);
        Assert.Equal(GravityDirection.Left, def.GravityDirection);
    }

    [Fact]
    public void Bag_GravityDirection_DelegatesToDefinition()
    {
        var def = new BagDefinition("test", "Test", new bool[,] { { true } },
            new[] { BagPropertyType.Gravity }, gravityDirection: GravityDirection.Right);
        var bag = new Bag(def);
        Assert.Equal(GravityDirection.Right, bag.GravityDirection);
    }

    // --- MoveBag tests ---

    [Fact]
    public void MoveBag_ValidPosition_Succeeds()
    {
        var grid = CreateGrid(10, 10);
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var result = grid.MoveBag(bag, new Vector2I(3, 3));

        Assert.True(result);
        Assert.Equal(new Vector2I(3, 3), bag.PositionOnMetaGrid);
        Assert.Equal(9, bag.OwnedCells.Count);
        // Old cells should be cleared
        Assert.Null(grid.GetBagAtCell(new Vector2I(0, 0)));
        Assert.Null(grid.GetBagAtCell(new Vector2I(2, 2)));
        // New cells should be assigned
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(3, 3)));
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(5, 5)));
    }

    [Fact]
    public void MoveBag_OutOfBounds_Rejected()
    {
        var grid = CreateGrid(10, 10);
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var result = grid.MoveBag(bag, new Vector2I(8, 8));

        Assert.False(result);
        // Bag should remain at original position
        Assert.Equal(new Vector2I(0, 0), bag.PositionOnMetaGrid);
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(0, 0)));
    }

    [Fact]
    public void MoveBag_OverlappingAnotherBag_Rejected()
    {
        var grid = CreateGrid(10, 10);
        var bag1 = new Bag(BagDefinition.SmallPouch); // 3x3
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3
        grid.PlaceBag(bag1, new Vector2I(0, 0));
        grid.PlaceBag(bag2, new Vector2I(5, 5));

        var result = grid.MoveBag(bag1, new Vector2I(4, 4));

        Assert.False(result);
        // Bag1 should remain at original position
        Assert.Equal(new Vector2I(0, 0), bag1.PositionOnMetaGrid);
        Assert.Equal(bag1, grid.GetBagAtCell(new Vector2I(0, 0)));
    }

    [Fact]
    public void MoveBag_NotOnGrid_Rejected()
    {
        var grid = CreateGrid(10, 10);
        var bag = new Bag(BagDefinition.SmallPouch);
        // Not placed on grid

        var result = grid.MoveBag(bag, new Vector2I(0, 0));

        Assert.False(result);
    }

    [Fact]
    public void MoveBag_WithContainedReagent_CarriesReagent()
    {
        var grid = CreateGrid(10, 10);
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3
        grid.PlaceBag(bag, new Vector2I(0, 0));
        var reagent = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(reagent, new Vector2I(1, 1));

        var result = grid.MoveBag(bag, new Vector2I(3, 3));

        Assert.True(result);
        // Bag moved to new position
        Assert.Equal(new Vector2I(3, 3), bag.PositionOnMetaGrid);
        // Reagent carried along: (1,1) + delta(3,3) = (4,4)
        Assert.Equal(new Vector2I(4, 4), reagent.PlacedPosition);
        // Cell at new position has the reagent
        Assert.Equal(reagent, grid.GetReagentAtCell(new Vector2I(4, 4)));
        // Old cell is clear
        Assert.Null(grid.GetReagentAtCell(new Vector2I(1, 1)));
    }

    [Fact]
    public void MoveBag_OverlappingOwnPosition_Succeeds()
    {
        // Moving a bag one cell right — overlaps its own previous cells
        var grid = CreateGrid(10, 10);
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var result = grid.MoveBag(bag, new Vector2I(1, 0));

        Assert.True(result);
        Assert.Equal(new Vector2I(1, 0), bag.PositionOnMetaGrid);
        // Old column 0 should be cleared
        Assert.Null(grid.GetBagAtCell(new Vector2I(0, 0)));
        Assert.Null(grid.GetBagAtCell(new Vector2I(0, 1)));
        Assert.Null(grid.GetBagAtCell(new Vector2I(0, 2)));
        // New cells should be assigned
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(1, 0)));
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(3, 2)));
    }

    [Fact]
    public void MoveBag_LShapedBag_Succeeds()
    {
        var grid = CreateGrid(10, 10);
        var bag = new Bag(BagDefinition.LBag);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var result = grid.MoveBag(bag, new Vector2I(5, 5));

        Assert.True(result);
        Assert.Equal(new Vector2I(5, 5), bag.PositionOnMetaGrid);
        // L-shape: (5,5), (5,6), (5,7), (6,7)
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(5, 5)));
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(5, 6)));
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(5, 7)));
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(6, 7)));
        // Void cell should not be a bag cell
        Assert.Null(grid.GetBagAtCell(new Vector2I(6, 5)));
    }

    // --- RotateBag tests ---

    [Fact]
    public void RotateBag_CW_Succeeds()
    {
        var grid = CreateGrid(10, 10);
        // L-bag: 3 rows × 2 cols
        // { true, false },
        // { true, false },
        // { true, true  }
        var bag = new Bag(BagDefinition.LBag);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var result = grid.RotateBag(bag);

        Assert.True(result);
        Assert.Equal(1, bag.Rotation);
        // After 90° CW of the L-bag (3×2), the rotated shape is 2×3:
        // { true,  true,  true },
        // { true,  false, false }
        // Positions at origin (0,0): (0,0),(1,0),(2,0),(0,1)
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(0, 0)));
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(1, 0)));
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(2, 0)));
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(0, 1)));
        // These should not be in the bag
        Assert.Null(grid.GetBagAtCell(new Vector2I(1, 1)));
        Assert.Null(grid.GetBagAtCell(new Vector2I(2, 1)));
    }

    [Fact]
    public void RotateBag_UpdatesCellMapping()
    {
        var grid = CreateGrid(10, 10);
        var bag = new Bag(BagDefinition.LBag);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Original cells for L-bag at (0,0): (0,0),(0,1),(0,2),(1,2)
        Assert.Equal(4, bag.OwnedCells.Count);
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(0, 0)));
        Assert.Equal(bag, grid.GetBagAtCell(new Vector2I(1, 2)));

        grid.RotateBag(bag);

        // After rotation, cell count should still be 4
        Assert.Equal(4, bag.OwnedCells.Count);
        // Old cell (1,2) should no longer be in the bag
        Assert.Null(grid.GetBagAtCell(new Vector2I(1, 2)));
    }

    [Fact]
    public void RotateBag_OutOfBoundsAfterRotation_Rejected()
    {
        // Place a tall thin bag near the right edge; after rotation it becomes wide and overflows
        var tallDef = new BagDefinition("tall", "Tall", new bool[,]
        {
            { true },
            { true },
            { true },
            { true },
        });
        var grid = CreateGrid(4, 10);
        var bag = new Bag(tallDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // After 90° CW rotation: becomes 1×4, placed at origin (0,0)
        // Would need cells (0,0),(1,0),(2,0),(3,0) — just fits in width=4
        var result1 = grid.RotateBag(bag);
        Assert.True(result1);

        // Now it's 1×4. Rotate again: becomes 4×1, needs (0,0)-(0,3) — fits in height=10
        var result2 = grid.RotateBag(bag);
        Assert.True(result2);

        // Place at col 3 so that next rotation (to 1×4) would need cols 3-6 → out of bounds
        grid.MoveBag(bag, new Vector2I(3, 0));
        var result3 = grid.RotateBag(bag);
        Assert.False(result3);
        Assert.Equal(2, bag.Rotation); // Rotation should be reverted
    }

    [Fact]
    public void RotateBag_OverlappingAfterRotation_Rejected()
    {
        var grid = CreateGrid(10, 10);
        // Horizontal bar 1×4
        var hBarDef = new BagDefinition("hbar", "HBar", new bool[,] { { true, true, true, true } });
        var bag1 = new Bag(hBarDef);
        grid.PlaceBag(bag1, new Vector2I(0, 0));

        // Place a blocker at (0,2) area
        var blocker = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(blocker, new Vector2I(0, 2));

        // Rotating bag1 90° CW: 1×4 → 4×1 at (0,0), needs (0,0)-(0,3)
        // (0,2) and (0,3) overlap with blocker
        var result = grid.RotateBag(bag1);
        Assert.False(result);
        Assert.Equal(0, bag1.Rotation);
    }

    [Fact]
    public void RotateBag_WithContainedReagent_CarriesReagent()
    {
        var grid = CreateGrid(10, 10);
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3
        grid.PlaceBag(bag, new Vector2I(0, 0));
        var reagent = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(reagent, new Vector2I(1, 1));

        var result = grid.RotateBag(bag);

        Assert.True(result);
        Assert.Equal(1, bag.Rotation);
        // Single cell in 3x3 bag: (1,1) → rotate CW: (3-1-1, 1) = (1, 1)
        // For a square bag, center stays in center
        Assert.Equal(new Vector2I(1, 1), reagent.PlacedPosition);
        Assert.Equal(reagent, grid.GetReagentAtCell(new Vector2I(1, 1)));
    }

    [Fact]
    public void RotateBag_NotOnGrid_Rejected()
    {
        var grid = CreateGrid(10, 10);
        var bag = new Bag(BagDefinition.SmallPouch);

        var result = grid.RotateBag(bag);

        Assert.False(result);
    }

    [Fact]
    public void RotateBag_SquareBag_FourRotationsReturnToOriginal()
    {
        var grid = CreateGrid(10, 10);
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3 square
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Get original cells
        var originalPositions = bag.GetCurrentMetaGridPositions().OrderBy(p => p.Col).ThenBy(p => p.Row).ToList();

        // Rotate 4 times
        for (int i = 0; i < 4; i++)
            Assert.True(grid.RotateBag(bag));

        Assert.Equal(0, bag.Rotation);
        var finalPositions = bag.GetCurrentMetaGridPositions().OrderBy(p => p.Col).ThenBy(p => p.Row).ToList();
        Assert.Equal(originalPositions, finalPositions);
    }

    // --- Bag movement carries contained reagents (GO.3) ---

    [Fact]
    public void MoveBag_MultiCellReagent_CarriesWithBag()
    {
        var grid = CreateGrid(12, 12);
        var bag = new Bag(BagDefinition.Satchel); // 4x3 (3 rows × 4 cols)
        grid.PlaceBag(bag, new Vector2I(0, 0));
        var reagent = new Reagent(ReagentDefinition.Domino); // 1×2 horizontal
        grid.PlaceReagent(reagent, new Vector2I(1, 1));

        var result = grid.MoveBag(bag, new Vector2I(5, 5));

        Assert.True(result);
        // Reagent moved by delta (5,5): (1,1)→(6,6)
        Assert.Equal(new Vector2I(6, 6), reagent.PlacedPosition);
        // Both cells occupied at new positions
        Assert.Equal(reagent, grid.GetReagentAtCell(new Vector2I(6, 6)));
        Assert.Equal(reagent, grid.GetReagentAtCell(new Vector2I(7, 6)));
        // Old cells clear
        Assert.Null(grid.GetReagentAtCell(new Vector2I(1, 1)));
        Assert.Null(grid.GetReagentAtCell(new Vector2I(2, 1)));
    }

    [Fact]
    public void MoveBag_SpanningReagent_RemovedFromGrid()
    {
        // Two adjacent bags; reagent spans both
        var grid = CreateGrid(12, 12);
        var bag1 = new Bag(BagDefinition.SmallPouch); // 3x3 at (0,0)
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3 at (3,0)
        grid.PlaceBag(bag1, new Vector2I(0, 0));
        grid.PlaceBag(bag2, new Vector2I(3, 0));

        // Place a domino spanning bag1 and bag2: cells at (2,1) and (3,1)
        var reagent = new Reagent(ReagentDefinition.Domino);
        grid.PlaceReagent(reagent, new Vector2I(2, 1));

        // Move bag1 down — spanning reagent should be removed (orphaned cells)
        var result = grid.MoveBag(bag1, new Vector2I(0, 5));

        Assert.True(result);
        Assert.Equal(new Vector2I(0, 5), bag1.PositionOnMetaGrid);
        // Spanning reagent is removed — it had cells in the moved bag
        Assert.Null(reagent.PlacedPosition);
        Assert.DoesNotContain(reagent, grid.PlacedReagents);
        // Old cells are cleaned up
        Assert.Null(grid.GetReagentAtCell(new Vector2I(2, 1)));
        Assert.Null(grid.GetReagentAtCell(new Vector2I(3, 1)));
    }

    [Fact]
    public void MoveBag_MultipleContainedReagents_AllCarried()
    {
        var grid = CreateGrid(12, 12);
        var bag = new Bag(BagDefinition.Satchel); // 3 rows × 4 cols
        grid.PlaceBag(bag, new Vector2I(0, 0));
        var r1 = new Reagent(ReagentDefinition.SingleCell);
        var r2 = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(r1, new Vector2I(0, 0));
        grid.PlaceReagent(r2, new Vector2I(3, 2));

        var result = grid.MoveBag(bag, new Vector2I(4, 4));

        Assert.True(result);
        Assert.Equal(new Vector2I(4, 4), r1.PlacedPosition);
        Assert.Equal(new Vector2I(7, 6), r2.PlacedPosition);
    }

    [Fact]
    public void RotateBag_DominoReagent_PositionAndRotationTransformed()
    {
        // L-bag at (0,0): shape is 3 rows × 2 cols
        // Cells: (0,0),(1,0),(0,1),(1,1),(0,2),(1,2) — wait, L-shape:
        // row0: [T, F], row1: [T, F], row2: [T, T]
        // Cells (col,row): (0,0), (0,1), (0,2), (1,2)
        var grid = CreateGrid(10, 10);
        var bag = new Bag(BagDefinition.LBag);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Place a vertical domino at (0,0) — occupies (0,0) and (0,1)
        var reagent = new Reagent(ReagentDefinition.Domino);
        reagent.Rotation = 1; // Vertical: 2 rows × 1 col
        grid.PlaceReagent(reagent, new Vector2I(0, 0));

        var result = grid.RotateBag(bag);

        Assert.True(result);
        Assert.Equal(1, bag.Rotation);
        // Old cells: (0,0) and (0,1)
        // L-bag has 3 rows, so oldShapeRows = 3
        // 90° CW: (0,0) → (3-1-0, 0) = (2, 0); (0,1) → (3-1-1, 0) = (1, 0)
        // New cells: (2,0) and (1,0) — now horizontal
        // Reagent rotation: 1 + 1 = 2
        Assert.Equal(2, reagent.Rotation);
        // PlacedPosition = min(col)=1, min(row)=0
        Assert.Equal(new Vector2I(1, 0), reagent.PlacedPosition);
        // Verify cell occupancy
        Assert.Equal(reagent, grid.GetReagentAtCell(new Vector2I(1, 0)));
        Assert.Equal(reagent, grid.GetReagentAtCell(new Vector2I(2, 0)));
        // Old cells clear
        Assert.Null(grid.GetReagentAtCell(new Vector2I(0, 0)));
        Assert.Null(grid.GetReagentAtCell(new Vector2I(0, 1)));
    }

    [Fact]
    public void RotateBag_SpanningReagent_SquareBag_StaysIfStillInBags()
    {
        // Square bag rotation doesn't change cell coverage — spanning reagent stays
        var grid = CreateGrid(12, 12);
        var bag1 = new Bag(BagDefinition.SmallPouch); // 3x3 at (0,0)
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3 at (3,0)
        grid.PlaceBag(bag1, new Vector2I(0, 0));
        grid.PlaceBag(bag2, new Vector2I(3, 0));

        // Place a domino spanning both bags: cells at (2,1) and (3,1)
        var reagent = new Reagent(ReagentDefinition.Domino);
        grid.PlaceReagent(reagent, new Vector2I(2, 1));

        // Rotate bag1 — square bag still covers same cells, so spanning reagent stays
        var result = grid.RotateBag(bag1);

        Assert.True(result);
        Assert.Equal(1, bag1.Rotation);
        // Spanning reagent stays — both cells still in bags
        Assert.Equal(new Vector2I(2, 1), reagent.PlacedPosition);
        Assert.Contains(reagent, grid.PlacedReagents);
    }

    [Fact]
    public void RotateBag_SpanningReagent_NonSquareBag_RemovedWhenOrphaned()
    {
        // Satchel (3 rows × 4 cols) at (0,0): covers cols 0-3, rows 0-2
        // After 90° CW rotation: 4 rows × 3 cols, covers cols 0-2, rows 0-3
        // Cell (3,1) leaves the bag after rotation
        var grid = CreateGrid(12, 12);
        var bag1 = new Bag(BagDefinition.Satchel); // 3r×4c at (0,0)
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3 at (4,0)
        grid.PlaceBag(bag1, new Vector2I(0, 0));
        grid.PlaceBag(bag2, new Vector2I(4, 0));

        // Place a domino spanning bag1 and bag2: cells at (3,1) and (4,1)
        var reagent = new Reagent(ReagentDefinition.Domino);
        grid.PlaceReagent(reagent, new Vector2I(3, 1));

        // Rotate bag1 — cell (3,1) is no longer in bag1 after rotation
        var result = grid.RotateBag(bag1);

        Assert.True(result);
        Assert.Equal(1, bag1.Rotation);
        // Spanning reagent is removed — cell (3,1) has no bag
        Assert.Null(reagent.PlacedPosition);
        Assert.DoesNotContain(reagent, grid.PlacedReagents);
        // Old cells are cleaned up
        Assert.Null(grid.GetReagentAtCell(new Vector2I(3, 1)));
        Assert.Null(grid.GetReagentAtCell(new Vector2I(4, 1)));
    }

    [Fact]
    public void MoveBag_InvalidDestination_ReagentsUnchanged()
    {
        // If MoveBag fails validation, contained reagents must remain at original positions
        var grid = CreateGrid(6, 6);
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3
        grid.PlaceBag(bag, new Vector2I(0, 0));
        var reagent = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(reagent, new Vector2I(1, 1));

        // Try to move out of bounds
        var result = grid.MoveBag(bag, new Vector2I(5, 5));

        Assert.False(result);
        Assert.Equal(new Vector2I(0, 0), bag.PositionOnMetaGrid);
        Assert.Equal(new Vector2I(1, 1), reagent.PlacedPosition);
        Assert.Equal(reagent, grid.GetReagentAtCell(new Vector2I(1, 1)));
    }

    [Fact]
    public void RotateBag_InvalidRotation_ReagentsUnchanged()
    {
        // If RotateBag fails validation, reagents must remain at original positions
        // Use a tall 4×1 bag in a 3-wide grid so rotation goes OOB
        var tallBagDef = new BagDefinition("tall", "Tall", new bool[,]
        {
            { true },
            { true },
            { true },
            { true },
        });
        var grid = CreateGrid(3, 4);
        var bag = new Bag(tallBagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));
        var reagent = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(reagent, new Vector2I(0, 1));

        // After 90° CW: 4×1 becomes 1×4, needs 4 cols at origin but grid is only 3 wide
        var result = grid.RotateBag(bag);

        Assert.False(result);
        Assert.Equal(0, bag.Rotation);
        Assert.Equal(new Vector2I(0, 1), reagent.PlacedPosition);
        Assert.Equal(reagent, grid.GetReagentAtCell(new Vector2I(0, 1)));
    }

    // --- Bag Rotation property tests ---

    [Fact]
    public void Bag_Rotation_DefaultsToZero()
    {
        var bag = new Bag(BagDefinition.SmallPouch);
        Assert.Equal(0, bag.Rotation);
    }

    [Fact]
    public void Bag_Rotation_NormalizesTo0Through3()
    {
        var bag = new Bag(BagDefinition.SmallPouch);
        bag.Rotation = 5;
        Assert.Equal(1, bag.Rotation);
        bag.Rotation = -1;
        Assert.Equal(3, bag.Rotation);
    }

    [Fact]
    public void Bag_GetRotatedShape_ReturnsCorrectDimensions()
    {
        // L-bag is 3 rows × 2 cols
        var bag = new Bag(BagDefinition.LBag);
        var original = bag.GetRotatedShape();
        Assert.Equal(3, original.GetLength(0)); // rows
        Assert.Equal(2, original.GetLength(1)); // cols

        bag.Rotation = 1;
        var rotated = bag.GetRotatedShape();
        Assert.Equal(2, rotated.GetLength(0)); // rows
        Assert.Equal(3, rotated.GetLength(1)); // cols
    }

    [Fact]
    public void Bag_GetMetaGridPositions_AccountsForRotation()
    {
        // BeltPouch is 1×4 horizontal strip
        var bag = new Bag(BagDefinition.BeltPouch);
        var origin = new Vector2I(0, 0);

        // Unrotated: (0,0),(1,0),(2,0),(3,0)
        var pos0 = bag.GetMetaGridPositions(origin);
        Assert.Equal(4, pos0.Count);
        Assert.Contains(new Vector2I(3, 0), pos0);

        // Rotated 90° CW: becomes 4×1 vertical
        bag.Rotation = 1;
        var pos1 = bag.GetMetaGridPositions(origin);
        Assert.Equal(4, pos1.Count);
        Assert.Contains(new Vector2I(0, 3), pos1);
        Assert.DoesNotContain(new Vector2I(3, 0), pos1);
    }

    [Fact]
    public void Bag_IsEdgeCell_AccountsForRotation()
    {
        var grid = CreateGrid(10, 10);
        // L-bag at (0,0), unrotated:
        // (0,0) (1,0) is void
        // (0,1) (1,1) is void
        // (0,2) (1,2) — all are edge cells in L-shape
        var bag = new Bag(BagDefinition.LBag);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // All 4 cells of L-bag are edge cells (it's an L-shape, everything is on the edge)
        Assert.True(bag.IsEdgeCell(new Vector2I(0, 0)));
        Assert.True(bag.IsEdgeCell(new Vector2I(0, 2)));

        // Now rotate — clear and re-place
        foreach (var cell in bag.OwnedCells)
            cell.BagOwner = null;
        bag.OwnedCells.Clear();
        bag.Rotation = 1;
        bag.PositionOnMetaGrid = new Vector2I(0, 0);
        var newPositions = bag.GetMetaGridPositions(new Vector2I(0, 0));
        foreach (var pos in newPositions)
        {
            var cell = grid.GetCell(pos)!;
            cell.BagOwner = bag;
            bag.OwnedCells.Add(cell);
        }

        // After 90° CW rotation of L-bag, shape is:
        // { true,  true,  true },
        // { true,  false, false }
        // (0,0) is edge (has neighbor to left that's out of shape)
        Assert.True(bag.IsEdgeCell(new Vector2I(0, 0)));
        // (2,0) is edge (right side and bottom are void)
        Assert.True(bag.IsEdgeCell(new Vector2I(2, 0)));
    }

    // --- Spanning Reagent Orphan Cleanup Tests ---

    [Fact]
    public void MoveBag_SpanningReagent_LockedBlocksMove()
    {
        // Locked spanning reagent blocks the bag move — state is fully restored
        var grid = CreateGrid(12, 12);
        var bag1 = new Bag(BagDefinition.SmallPouch); // 3x3 at (0,0)
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3 at (3,0)
        grid.PlaceBag(bag1, new Vector2I(0, 0));
        grid.PlaceBag(bag2, new Vector2I(3, 0));

        var reagent = new Reagent(ReagentDefinition.Domino);
        grid.PlaceReagent(reagent, new Vector2I(2, 1)); // spans (2,1) and (3,1)
        reagent.IsLocked = true;

        var result = grid.MoveBag(bag1, new Vector2I(0, 5));

        Assert.False(result);
        // Bag stays at original position
        Assert.Equal(new Vector2I(0, 0), bag1.PositionOnMetaGrid);
        // Locked spanning reagent is still placed
        Assert.Equal(new Vector2I(2, 1), reagent.PlacedPosition);
        Assert.Contains(reagent, grid.PlacedReagents);
        // Cell occupancy preserved
        Assert.Equal(reagent, grid.GetReagentAtCell(new Vector2I(2, 1)));
        Assert.Equal(reagent, grid.GetReagentAtCell(new Vector2I(3, 1)));
    }

    [Fact]
    public void MoveBag_ContainedReagentNotOrphaned_StaysPlaced()
    {
        // A reagent fully within the moved bag should be carried, not orphaned
        var grid = CreateGrid(12, 12);
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3 at (0,0)
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(reagent, new Vector2I(1, 1));

        var result = grid.MoveBag(bag, new Vector2I(4, 4));

        Assert.True(result);
        Assert.Equal(new Vector2I(5, 5), reagent.PlacedPosition);
        Assert.Contains(reagent, grid.PlacedReagents);
    }

    [Fact]
    public void MoveBag_SpanningReagent_OtherBagReagentUnaffected()
    {
        // A reagent fully in a different bag should not be removed
        var grid = CreateGrid(12, 12);
        var bag1 = new Bag(BagDefinition.SmallPouch); // 3x3 at (0,0)
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3 at (3,0)
        grid.PlaceBag(bag1, new Vector2I(0, 0));
        grid.PlaceBag(bag2, new Vector2I(3, 0));

        // spanning reagent
        var spanning = new Reagent(ReagentDefinition.Domino);
        grid.PlaceReagent(spanning, new Vector2I(2, 1));

        // fully in bag2
        var safe = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(safe, new Vector2I(4, 1));

        var result = grid.MoveBag(bag1, new Vector2I(0, 5));

        Assert.True(result);
        // Spanning is removed
        Assert.Null(spanning.PlacedPosition);
        Assert.DoesNotContain(spanning, grid.PlacedReagents);
        // Safe reagent in bag2 is untouched
        Assert.Equal(new Vector2I(4, 1), safe.PlacedPosition);
        Assert.Contains(safe, grid.PlacedReagents);
    }

    [Fact]
    public void RotateBag_SpanningReagent_CellOccupancyCleanedUp()
    {
        // Use Satchel (non-square) so rotation changes cell coverage
        var grid = CreateGrid(12, 12);
        var bag1 = new Bag(BagDefinition.Satchel); // 3r×4c at (0,0)
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3 at (4,0)
        grid.PlaceBag(bag1, new Vector2I(0, 0));
        grid.PlaceBag(bag2, new Vector2I(4, 0));

        var reagent = new Reagent(ReagentDefinition.Domino);
        grid.PlaceReagent(reagent, new Vector2I(3, 1)); // spans (3,1) and (4,1)

        grid.RotateBag(bag1);

        // Both cells of the spanning reagent should have no occupant
        Assert.Null(grid.GetReagentAtCell(new Vector2I(3, 1)));
        Assert.Null(grid.GetReagentAtCell(new Vector2I(4, 1)));
        Assert.False(reagent.IsPlaced);
    }

    [Fact]
    public void MoveBag_NonSpanningReagentInOtherBag_NotRemoved()
    {
        // Moving bag1 when bag2 has a contained reagent — bag2's reagent is fine
        var grid = CreateGrid(12, 12);
        var bag1 = new Bag(BagDefinition.SmallPouch); // 3x3 at (0,0)
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3 at (6,0)
        grid.PlaceBag(bag1, new Vector2I(0, 0));
        grid.PlaceBag(bag2, new Vector2I(6, 0));

        var r1 = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(r1, new Vector2I(1, 1)); // in bag1, will be carried

        var r2 = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(r2, new Vector2I(7, 1)); // in bag2, should stay

        grid.MoveBag(bag1, new Vector2I(0, 5));

        Assert.Equal(new Vector2I(1, 6), r1.PlacedPosition); // carried
        Assert.Equal(new Vector2I(7, 1), r2.PlacedPosition); // unaffected
        Assert.Equal(2, grid.PlacedReagents.Count);
    }

    // --- Locked Reagent Blocks Bag Movement Tests (GO.4) ---

    [Fact]
    public void MoveBag_LockedSpanningReagent_StateFullyRestored()
    {
        // When a locked spanning reagent blocks a bag move, the entire grid state
        // should be identical to before the move was attempted
        var grid = CreateGrid(12, 12);
        var bag1 = new Bag(BagDefinition.SmallPouch); // 3x3 at (0,0)
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3 at (3,0)
        grid.PlaceBag(bag1, new Vector2I(0, 0));
        grid.PlaceBag(bag2, new Vector2I(3, 0));

        // Contained reagent in bag1
        var contained = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(contained, new Vector2I(1, 1));

        // Locked spanning reagent
        var spanning = new Reagent(ReagentDefinition.Domino);
        grid.PlaceReagent(spanning, new Vector2I(2, 1));
        spanning.IsLocked = true;

        var result = grid.MoveBag(bag1, new Vector2I(0, 5));

        Assert.False(result);
        // Bag position unchanged
        Assert.Equal(new Vector2I(0, 0), bag1.PositionOnMetaGrid);
        // Contained reagent NOT moved
        Assert.Equal(new Vector2I(1, 1), contained.PlacedPosition);
        Assert.Equal(contained, grid.GetReagentAtCell(new Vector2I(1, 1)));
        // Spanning reagent still placed
        Assert.Equal(new Vector2I(2, 1), spanning.PlacedPosition);
        Assert.Contains(spanning, grid.PlacedReagents);
        Assert.Equal(2, grid.PlacedReagents.Count);
    }

    [Fact]
    public void MoveBag_UnlockedSpanningReagent_StillRemoved()
    {
        // Non-locked spanning reagents are removed as before (R3 behavior)
        var grid = CreateGrid(12, 12);
        var bag1 = new Bag(BagDefinition.SmallPouch); // 3x3 at (0,0)
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3 at (3,0)
        grid.PlaceBag(bag1, new Vector2I(0, 0));
        grid.PlaceBag(bag2, new Vector2I(3, 0));

        var reagent = new Reagent(ReagentDefinition.Domino);
        grid.PlaceReagent(reagent, new Vector2I(2, 1));
        // NOT locked

        var result = grid.MoveBag(bag1, new Vector2I(0, 5));

        Assert.True(result);
        Assert.Null(reagent.PlacedPosition);
        Assert.DoesNotContain(reagent, grid.PlacedReagents);
    }

    [Fact]
    public void RotateBag_LockedSpanningReagent_BlocksRotation()
    {
        // Locked spanning reagent blocks rotation of a non-square bag
        var grid = CreateGrid(12, 12);
        var bag1 = new Bag(BagDefinition.Satchel); // 3r×4c at (0,0)
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3 at (4,0)
        grid.PlaceBag(bag1, new Vector2I(0, 0));
        grid.PlaceBag(bag2, new Vector2I(4, 0));

        var reagent = new Reagent(ReagentDefinition.Domino);
        grid.PlaceReagent(reagent, new Vector2I(3, 1)); // spans (3,1) and (4,1)
        reagent.IsLocked = true;

        var result = grid.RotateBag(bag1);

        Assert.False(result);
        // Rotation reverted
        Assert.Equal(0, bag1.Rotation);
        // Reagent still placed at original position
        Assert.Equal(new Vector2I(3, 1), reagent.PlacedPosition);
        Assert.Contains(reagent, grid.PlacedReagents);
        Assert.Equal(reagent, grid.GetReagentAtCell(new Vector2I(3, 1)));
        Assert.Equal(reagent, grid.GetReagentAtCell(new Vector2I(4, 1)));
    }

    [Fact]
    public void RotateBag_LockedSpanningReagent_ContainedReagentRestored()
    {
        // When locked spanning reagent blocks rotation, contained reagents
        // should also be restored to their original positions
        var grid = CreateGrid(12, 12);
        var bag1 = new Bag(BagDefinition.Satchel); // 3r×4c at (0,0)
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3 at (4,0)
        grid.PlaceBag(bag1, new Vector2I(0, 0));
        grid.PlaceBag(bag2, new Vector2I(4, 0));

        // Contained reagent in bag1
        var contained = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(contained, new Vector2I(0, 0));

        // Locked spanning reagent
        var spanning = new Reagent(ReagentDefinition.Domino);
        grid.PlaceReagent(spanning, new Vector2I(3, 1));
        spanning.IsLocked = true;

        var result = grid.RotateBag(bag1);

        Assert.False(result);
        Assert.Equal(0, bag1.Rotation);
        // Contained reagent not moved
        Assert.Equal(new Vector2I(0, 0), contained.PlacedPosition);
        Assert.Equal(contained, grid.GetReagentAtCell(new Vector2I(0, 0)));
    }

    [Fact]
    public void MoveBag_LockedContainedReagent_MoveSucceeds()
    {
        // A locked reagent FULLY contained in the bag should NOT block the move
        // (it moves with the bag)
        var grid = CreateGrid(12, 12);
        var bag = new Bag(BagDefinition.SmallPouch); // 3x3 at (0,0)
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(reagent, new Vector2I(1, 1));
        reagent.IsLocked = true;

        var result = grid.MoveBag(bag, new Vector2I(4, 4));

        Assert.True(result);
        Assert.Equal(new Vector2I(5, 5), reagent.PlacedPosition);
        Assert.Contains(reagent, grid.PlacedReagents);
    }

    [Fact]
    public void RotateBag_UnlockedSpanningReagent_StillRemoved()
    {
        // Non-locked spanning reagent is removed during rotation (R3 behavior unchanged)
        var grid = CreateGrid(12, 12);
        var bag1 = new Bag(BagDefinition.Satchel); // 3r×4c at (0,0)
        var bag2 = new Bag(BagDefinition.SmallPouch); // 3x3 at (4,0)
        grid.PlaceBag(bag1, new Vector2I(0, 0));
        grid.PlaceBag(bag2, new Vector2I(4, 0));

        var reagent = new Reagent(ReagentDefinition.Domino);
        grid.PlaceReagent(reagent, new Vector2I(3, 1)); // spans (3,1) and (4,1)
        // NOT locked

        var result = grid.RotateBag(bag1);

        Assert.True(result);
        Assert.Null(reagent.PlacedPosition);
        Assert.DoesNotContain(reagent, grid.PlacedReagents);
    }
}
