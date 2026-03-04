using Xunit;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Bags;
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
}
