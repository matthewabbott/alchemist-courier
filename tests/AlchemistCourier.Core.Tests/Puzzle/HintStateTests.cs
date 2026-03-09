using Xunit;
using AlchemistCourier.Core.Puzzle;

namespace AlchemistCourier.Core.Tests.Puzzle;

public class HintStateTests
{
    [Fact]
    public void HasHintsRemaining_WhenFresh_ReturnsTrue()
    {
        var state = new HintState(maxHints: 3);

        Assert.True(state.HasHintsRemaining);
        Assert.Equal(0, state.HintsUsed);
    }

    [Fact]
    public void HasHintsRemaining_WhenExhausted_ReturnsFalse()
    {
        var state = new HintState(maxHints: 2);
        state.HintsUsed = 2;

        Assert.False(state.HasHintsRemaining);
    }

    [Fact]
    public void WasHinted_ReturnsTrueAfterAdding()
    {
        var state = new HintState(maxHints: 3);
        state.HintedReagentIndices.Add(5);

        Assert.True(state.WasHinted(5));
    }

    [Fact]
    public void WasHinted_ReturnsFalseIfNotAdded()
    {
        var state = new HintState(maxHints: 3);
        state.HintedReagentIndices.Add(2);

        Assert.False(state.WasHinted(7));
    }
}
