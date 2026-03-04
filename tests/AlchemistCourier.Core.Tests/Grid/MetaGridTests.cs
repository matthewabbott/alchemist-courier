using System.Collections.Generic;
using Xunit;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
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
    public void Heavy_RejectedIfNonHeavyBelow()
    {
        // 3x3 Gravity bag. Place non-Heavy at bottom (1,2), then try Heavy at (1,1).
        // Heavy above non-Heavy = bad (Heavy must be at bottom).
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var nonHeavy = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.PlaceReagent(nonHeavy, new Vector2I(1, 2))); // bottom row

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        // Heavy at (1,1) — non-Heavy at (1,2) is below: rejected
        Assert.False(grid.CanPlaceReagent(heavy, new Vector2I(1, 1)));
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
    public void Heavy_NonHeavyRejectedBelowHeavy()
    {
        // 4x1 Gravity bag. Place Heavy at row 2, then try non-Heavy at row 3 (below).
        // Non-Heavy below Heavy = bad.
        var shape = new bool[,] { { true }, { true }, { true }, { true } };
        var gravDef = new BagDefinition("tall_grav", "Tall Gravity", shape,
            new[] { BagPropertyType.Gravity });
        var grid = CreateGrid();
        var bag = new Bag(gravDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        Assert.True(grid.PlaceReagent(heavy, new Vector2I(0, 3))); // bottom

        // Try placing non-Heavy at (0,2) — above Heavy at (0,3) — should succeed
        var nonHeavy1 = new Reagent(ReagentDefinition.SingleCell);
        Assert.True(grid.CanPlaceReagent(nonHeavy1, new Vector2I(0, 2)));

        // Now place non-Heavy at (0,2), then place Heavy at (0,1).
        // Heavy at (0,1) with non-Heavy at (0,2) below — rejected.
        Assert.True(grid.PlaceReagent(nonHeavy1, new Vector2I(0, 2)));

        var heavy2 = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        Assert.False(grid.CanPlaceReagent(heavy2, new Vector2I(0, 1)));
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
}
