using Xunit;
using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Core.Tests.Grid;

public class Vector2ITests
{
    [Fact]
    public void Addition_CombinesComponents()
    {
        var a = new Vector2I(1, 2);
        var b = new Vector2I(3, 4);
        Assert.Equal(new Vector2I(4, 6), a + b);
    }

    [Fact]
    public void Subtraction_SubtractsComponents()
    {
        var a = new Vector2I(5, 10);
        var b = new Vector2I(2, 3);
        Assert.Equal(new Vector2I(3, 7), a - b);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        Assert.Equal(new Vector2I(5, 10), new Vector2I(5, 10));
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        Assert.NotEqual(new Vector2I(1, 2), new Vector2I(2, 1));
    }

    [Fact]
    public void Zero_IsOrigin()
    {
        var zero = Vector2I.Zero;
        Assert.Equal(0, zero.Col);
        Assert.Equal(0, zero.Row);
    }

    [Fact]
    public void CardinalDirections_HasFourEntries()
    {
        Assert.Equal(4, Vector2I.CardinalDirections.Length);
    }

    [Fact]
    public void CardinalDirections_SumToZero()
    {
        var sum = Vector2I.Zero;
        foreach (var dir in Vector2I.CardinalDirections)
            sum = sum + dir;
        Assert.Equal(Vector2I.Zero, sum);
    }

    [Fact]
    public void GetHashCode_EqualValues_SameHash()
    {
        var a = new Vector2I(3, 7);
        var b = new Vector2I(3, 7);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        Assert.Equal("(3, 7)", new Vector2I(3, 7).ToString());
    }
}
