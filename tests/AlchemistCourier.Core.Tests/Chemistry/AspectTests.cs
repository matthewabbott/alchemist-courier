using Xunit;
using AlchemistCourier.Core.Chemistry;

namespace AlchemistCourier.Core.Tests.Chemistry;

public class AspectTests
{
    [Theory]
    [InlineData("hot", "Hot")]
    [InlineData("cold", "Cold")]
    [InlineData("volatile", "Volatile")]
    [InlineData("fragile", "Fragile")]
    [InlineData("toxic", "Toxic")]
    [InlineData("sharp", "Sharp")]
    [InlineData("heavy", "Heavy")]
    [InlineData("medicinal", "Medicinal")]
    [InlineData("stable", "Stable")]
    [InlineData("catalytic", "Catalytic")]
    public void FromId_KnownIds_ReturnsCorrectAspect(string id, string expectedName)
    {
        var aspect = Aspect.FromId(id);
        Assert.Equal(id, aspect.Id);
        Assert.Equal(expectedName, aspect.Name);
        Assert.Equal(1, aspect.Magnitude);
    }

    [Fact]
    public void FromId_UnknownId_ReturnsFallbackAspect()
    {
        var aspect = Aspect.FromId("mystical");
        Assert.Equal("mystical", aspect.Id);
        Assert.Equal("mystical", aspect.Name);
        Assert.Equal(SchoolType.Physical, aspect.School);
        Assert.Equal(1, aspect.Magnitude);
    }

    [Fact]
    public void FromId_MatchesFactoryMethods()
    {
        Assert.Equal(Aspect.Hot(), Aspect.FromId("hot"));
        Assert.Equal(Aspect.Cold(), Aspect.FromId("cold"));
        Assert.Equal(Aspect.Volatile(), Aspect.FromId("volatile"));
        Assert.Equal(Aspect.Stable(), Aspect.FromId("stable"));
    }
}
