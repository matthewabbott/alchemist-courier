using Xunit;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Tests.Reagents;

public class ShapeUtilTests
{
    [Fact]
    public void CellCount_CountsTrueCells()
    {
        var shape = new bool[,]
        {
            { true, false },
            { true, true  },
        };
        Assert.Equal(3, ShapeUtil.CellCount(shape));
    }

    [Fact]
    public void CellCount_AllTrue()
    {
        var shape = new bool[,]
        {
            { true, true },
            { true, true },
        };
        Assert.Equal(4, ShapeUtil.CellCount(shape));
    }

    [Fact]
    public void GetCellPositions_ReturnsRowMajorOrder()
    {
        // L-shape:
        // X .
        // X .
        // X X
        var shape = new bool[,]
        {
            { true, false },
            { true, false },
            { true, true  },
        };

        var positions = ShapeUtil.GetCellPositions(shape);
        Assert.Equal(4, positions.Count);
        Assert.Equal(new Vector2I(0, 0), positions[0]); // top-left
        Assert.Equal(new Vector2I(0, 1), positions[1]); // middle-left
        Assert.Equal(new Vector2I(0, 2), positions[2]); // bottom-left
        Assert.Equal(new Vector2I(1, 2), positions[3]); // bottom-right
    }

    [Fact]
    public void Rotate90Clockwise_SquareShape_SameShape()
    {
        var shape = new bool[,]
        {
            { true, true },
            { true, true },
        };
        var rotated = ShapeUtil.Rotate90Clockwise(shape);
        Assert.Equal(4, ShapeUtil.CellCount(rotated));
        Assert.Equal(2, rotated.GetLength(0));
        Assert.Equal(2, rotated.GetLength(1));
    }

    [Fact]
    public void Rotate90Clockwise_HorizontalBar_BecomesVertical()
    {
        // Horizontal: X X X (1 row, 3 cols)
        var shape = new bool[,]
        {
            { true, true, true },
        };

        var rotated = ShapeUtil.Rotate90Clockwise(shape);

        // Should become vertical: 3 rows, 1 col
        Assert.Equal(3, rotated.GetLength(0));
        Assert.Equal(1, rotated.GetLength(1));
        Assert.Equal(3, ShapeUtil.CellCount(rotated));
    }

    [Fact]
    public void Rotate_ZeroSteps_ReturnsSameShape()
    {
        var shape = new bool[,]
        {
            { true, false },
            { true, true  },
        };
        var rotated = ShapeUtil.Rotate(shape, 0);
        Assert.Equal(shape.GetLength(0), rotated.GetLength(0));
        Assert.Equal(shape.GetLength(1), rotated.GetLength(1));
    }

    [Fact]
    public void Rotate_FourSteps_ReturnsSameDimensions()
    {
        var shape = new bool[,]
        {
            { true, false },
            { true, true  },
        };
        var rotated = ShapeUtil.Rotate(shape, 4);
        // 360° rotation should give same dimensions
        Assert.Equal(shape.GetLength(0), rotated.GetLength(0));
        Assert.Equal(shape.GetLength(1), rotated.GetLength(1));
    }

    [Fact]
    public void Rotate_NegativeSteps_NormalizesCorrectly()
    {
        var shape = new bool[,]
        {
            { true, true, true },
        };

        // -1 step should be same as 3 steps
        var rotatedNeg1 = ShapeUtil.Rotate(shape, -1);
        var rotated3 = ShapeUtil.Rotate(shape, 3);

        Assert.Equal(rotated3.GetLength(0), rotatedNeg1.GetLength(0));
        Assert.Equal(rotated3.GetLength(1), rotatedNeg1.GetLength(1));
        Assert.Equal(ShapeUtil.CellCount(rotated3), ShapeUtil.CellCount(rotatedNeg1));
    }
}
