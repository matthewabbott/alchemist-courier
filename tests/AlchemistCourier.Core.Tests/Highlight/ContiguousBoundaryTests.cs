using System.Collections.Generic;
using Xunit;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Highlight;

namespace AlchemistCourier.Core.Tests.Highlight;

public class ContiguousBoundaryTests
{
    [Fact]
    public void EmptySet_ReturnsEmpty()
    {
        var result = ContiguousBoundary.Compute(new List<Vector2I>());
        Assert.Empty(result);
    }

    [Fact]
    public void SingleCell_AllEdges()
    {
        var result = ContiguousBoundary.Compute(new[] { new Vector2I(3, 5) });
        Assert.Single(result);
        Assert.Equal(CellEdge.All, result[new Vector2I(3, 5)]);
    }

    [Fact]
    public void TwoByTwoBlock_NoInternalEdges()
    {
        // (0,0) (1,0)
        // (0,1) (1,1)
        var cells = new[]
        {
            new Vector2I(0, 0), new Vector2I(1, 0),
            new Vector2I(0, 1), new Vector2I(1, 1)
        };
        var result = ContiguousBoundary.Compute(cells);

        Assert.Equal(4, result.Count);

        // Top-left: top + left
        Assert.Equal(CellEdge.Top | CellEdge.Left, result[new Vector2I(0, 0)]);
        // Top-right: top + right
        Assert.Equal(CellEdge.Top | CellEdge.Right, result[new Vector2I(1, 0)]);
        // Bottom-left: bottom + left
        Assert.Equal(CellEdge.Bottom | CellEdge.Left, result[new Vector2I(0, 1)]);
        // Bottom-right: bottom + right
        Assert.Equal(CellEdge.Bottom | CellEdge.Right, result[new Vector2I(1, 1)]);
    }

    [Fact]
    public void LShape_BoundaryCorrect()
    {
        // L-shape:
        // (0,0)
        // (0,1)
        // (0,2) (1,2)
        var cells = new[]
        {
            new Vector2I(0, 0),
            new Vector2I(0, 1),
            new Vector2I(0, 2), new Vector2I(1, 2)
        };
        var result = ContiguousBoundary.Compute(cells);

        Assert.Equal(4, result.Count);

        // (0,0): top, left, right (no neighbor above, left, or right)
        Assert.Equal(CellEdge.Top | CellEdge.Left | CellEdge.Right, result[new Vector2I(0, 0)]);
        // (0,1): left, right (has neighbors above and below)
        Assert.Equal(CellEdge.Left | CellEdge.Right, result[new Vector2I(0, 1)]);
        // (0,2): bottom, left (neighbors above and right)
        Assert.Equal(CellEdge.Bottom | CellEdge.Left, result[new Vector2I(0, 2)]);
        // (1,2): top, bottom, right (neighbor to left only)
        Assert.Equal(CellEdge.Top | CellEdge.Bottom | CellEdge.Right, result[new Vector2I(1, 2)]);
    }

    [Fact]
    public void DisjointCells_EachGetAllEdges()
    {
        var cells = new[]
        {
            new Vector2I(0, 0),
            new Vector2I(5, 5),
            new Vector2I(10, 10)
        };
        var result = ContiguousBoundary.Compute(cells);

        Assert.Equal(3, result.Count);
        Assert.All(result.Values, edges => Assert.Equal(CellEdge.All, edges));
    }

    [Fact]
    public void HorizontalStrip_MiddleCellsHaveTopAndBottom()
    {
        // (0,0) (1,0) (2,0) (3,0)
        var cells = new[]
        {
            new Vector2I(0, 0), new Vector2I(1, 0),
            new Vector2I(2, 0), new Vector2I(3, 0)
        };
        var result = ContiguousBoundary.Compute(cells);

        Assert.Equal(4, result.Count);

        // Left end: top, bottom, left
        Assert.Equal(CellEdge.Top | CellEdge.Bottom | CellEdge.Left, result[new Vector2I(0, 0)]);
        // Middle cells: top, bottom only
        Assert.Equal(CellEdge.Top | CellEdge.Bottom, result[new Vector2I(1, 0)]);
        Assert.Equal(CellEdge.Top | CellEdge.Bottom, result[new Vector2I(2, 0)]);
        // Right end: top, bottom, right
        Assert.Equal(CellEdge.Top | CellEdge.Bottom | CellEdge.Right, result[new Vector2I(3, 0)]);
    }

    [Fact]
    public void VerticalStrip_MiddleCellsHaveLeftAndRight()
    {
        var cells = new[]
        {
            new Vector2I(0, 0), new Vector2I(0, 1), new Vector2I(0, 2)
        };
        var result = ContiguousBoundary.Compute(cells);

        Assert.Equal(3, result.Count);
        Assert.Equal(CellEdge.Top | CellEdge.Left | CellEdge.Right, result[new Vector2I(0, 0)]);
        Assert.Equal(CellEdge.Left | CellEdge.Right, result[new Vector2I(0, 1)]);
        Assert.Equal(CellEdge.Bottom | CellEdge.Left | CellEdge.Right, result[new Vector2I(0, 2)]);
    }

    [Fact]
    public void DuplicatePositions_HandledCorrectly()
    {
        var cells = new List<Vector2I>
        {
            new Vector2I(0, 0), new Vector2I(1, 0),
            new Vector2I(0, 0) // duplicate
        };
        var result = ContiguousBoundary.Compute(cells);

        Assert.Equal(2, result.Count);
        Assert.Equal(CellEdge.Top | CellEdge.Bottom | CellEdge.Left, result[new Vector2I(0, 0)]);
        Assert.Equal(CellEdge.Top | CellEdge.Bottom | CellEdge.Right, result[new Vector2I(1, 0)]);
    }
}
