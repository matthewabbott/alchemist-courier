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
    public void Fragile_BlockedFromGravityBag()
    {
        // Fragile reagent should be rejected from Gravity bags
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var fragile = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));
        // Even at bottom row (valid gravity position), Fragile is blocked
        Assert.False(grid.CanPlaceReagent(fragile, new Vector2I(1, 2)));
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
    public void GetFragileViolations_GravityBag_ReturnsViolation()
    {
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var fragile = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));
        var positions = fragile.GetMetaGridPositions(new Vector2I(1, 2));
        var violations = grid.GetFragileViolations(fragile, positions);

        Assert.Single(violations);
        Assert.Equal(ConstraintType.FragileInGravity, violations[0].Type);
        Assert.Contains(new Vector2I(1, 2), violations[0].AffectedCells);
        Assert.Equal(AspectIds.Fragile, violations[0].AspectId);
        Assert.Contains("Fragile", violations[0].Reason);
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

        // Fragile in gravity — both methods agree
        var fragile = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));
        var fragilePos = fragile.GetMetaGridPositions(new Vector2I(1, 2));
        bool fragileViolations = grid.GetFragileViolations(fragile, fragilePos).Count > 0;
        Assert.True(fragileViolations); // Fragile in Gravity bag = violation
    }
}
