using System.Collections.Generic;
using System.Linq;
using Xunit;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Tests.Chemistry;

public class AspectMetadataTests
{
    [Theory]
    [InlineData(AspectIds.Hot, "Hot")]
    [InlineData(AspectIds.Cold, "Cold")]
    [InlineData(AspectIds.Volatile, "Volatile")]
    [InlineData(AspectIds.Stable, "Stable")]
    [InlineData(AspectIds.Heavy, "Heavy")]
    [InlineData(AspectIds.Toxic, "Toxic")]
    [InlineData(AspectIds.Sharp, "Sharp")]
    [InlineData(AspectIds.Fragile, "Fragile")]
    [InlineData(AspectIds.Catalytic, "Catalytic")]
    [InlineData(AspectIds.Medicinal, "Medicinal")]
    public void Get_AllTenAspects_ReturnsMetadata(string id, string expectedName)
    {
        var metadata = AspectMetadataDatabase.Get(id);
        Assert.NotNull(metadata);
        Assert.Equal(id, metadata!.Id);
        Assert.Equal(expectedName, metadata.Name);
        Assert.False(string.IsNullOrWhiteSpace(metadata.Description));
        Assert.Equal(SchoolType.Physical, metadata.School);
    }

    [Fact]
    public void GetAll_Returns10Entries()
    {
        var all = AspectMetadataDatabase.GetAll();
        Assert.Equal(10, all.Count);
    }

    [Fact]
    public void GetAll_AllEntriesHaveUniqueIds()
    {
        var all = AspectMetadataDatabase.GetAll();
        var ids = all.Select(m => m.Id).ToHashSet();
        Assert.Equal(all.Count, ids.Count);
    }

    [Fact]
    public void Get_UnknownId_ReturnsNull()
    {
        Assert.Null(AspectMetadataDatabase.Get("nonexistent"));
    }

    [Theory]
    [InlineData(AspectIds.Sharp)]
    [InlineData(AspectIds.Heavy)]
    public void ConstrainedAspects_HaveConstraintText(string id)
    {
        var metadata = AspectMetadataDatabase.Get(id);
        Assert.NotNull(metadata);
        Assert.NotNull(metadata!.ConstraintText);
        Assert.False(string.IsNullOrWhiteSpace(metadata.ConstraintText));
    }

    [Theory]
    [InlineData(AspectIds.Hot)]
    [InlineData(AspectIds.Cold)]
    [InlineData(AspectIds.Volatile)]
    [InlineData(AspectIds.Stable)]
    [InlineData(AspectIds.Toxic)]
    [InlineData(AspectIds.Fragile)]
    [InlineData(AspectIds.Catalytic)]
    [InlineData(AspectIds.Medicinal)]
    public void UnconstrainedAspects_HaveNullConstraintText(string id)
    {
        var metadata = AspectMetadataDatabase.Get(id);
        Assert.NotNull(metadata);
        Assert.Null(metadata!.ConstraintText);
    }

    [Fact]
    public void GetForPuzzle_FiltersToRelevantAspects()
    {
        // Rules that reference Hot and Volatile
        var rules = new List<ReactionRule>
        {
            new("Explosion", new List<string> { AspectIds.Volatile }, SensitivityTier.Adjacent,
                Granularity.PerCell, 0, ReactionResultType.Explosion),
        };

        // Reagent with Hot aspect
        var reagent = new Reagent(new ReagentDefinition(
            "test", "Test",
            new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { new AspectSet(Aspect.Hot()) }
        ));

        var result = AspectMetadataDatabase.GetForPuzzle(rules, new List<Reagent> { reagent });
        var ids = result.Select(m => m.Id).ToHashSet();

        // Should include Volatile (from rule) and Hot (from reagent)
        Assert.Contains(AspectIds.Volatile, ids);
        Assert.Contains(AspectIds.Hot, ids);
        // Should not include unrelated aspects
        Assert.DoesNotContain(AspectIds.Sharp, ids);
        Assert.DoesNotContain(AspectIds.Fragile, ids);
    }

    [Fact]
    public void GetForPuzzle_EmptyInputs_ReturnsEmpty()
    {
        var result = AspectMetadataDatabase.GetForPuzzle(
            new List<ReactionRule>(), new List<Reagent>());
        Assert.Empty(result);
    }

    [Fact]
    public void GetForPuzzle_DeduplicatesAspects()
    {
        // Rule and reagent both reference Hot
        var rules = new List<ReactionRule>
        {
            new("Neutralization", new List<string> { AspectIds.Hot, AspectIds.Cold }, SensitivityTier.SameBag,
                Granularity.PerItem, 0, ReactionResultType.Neutralization),
        };

        var reagent = new Reagent(new ReagentDefinition(
            "test", "Test",
            new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { new AspectSet(Aspect.Hot()) }
        ));

        var result = AspectMetadataDatabase.GetForPuzzle(rules, new List<Reagent> { reagent });
        // Hot should appear only once
        Assert.Equal(1, result.Count(m => m.Id == AspectIds.Hot));
    }

    [Fact]
    public void FlavorText_DefaultsToNull()
    {
        var def = new ReagentDefinition("test", "Test", new bool[,] { { true } });
        Assert.Null(def.FlavorText);
    }

    [Fact]
    public void FlavorText_CanBeSet()
    {
        var def = new ReagentDefinition("test", "Test", new bool[,] { { true } },
            flavorText: "A mysterious reagent.");
        Assert.Equal("A mysterious reagent.", def.FlavorText);
    }

    [Fact]
    public void FlavorText_RoundTrips_OnReagentDefinition()
    {
        var flavor = "Shimmering with arcane energy.";
        var def = new ReagentDefinition("test", "Test", new bool[,] { { true } },
            flavorText: flavor);
        var reagent = new Reagent(def);
        Assert.Equal(flavor, reagent.Definition.FlavorText);
    }
}
