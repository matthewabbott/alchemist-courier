using System.Collections.Generic;
using Xunit;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Tests.Reagents;

public class ReagentTests
{
    private static Reagent MakeSingleCellReagent(string id, params Aspect[] aspects)
    {
        var def = new ReagentDefinition(id, id, new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { new AspectSet(aspects) });
        return new Reagent(def);
    }

    private static Reagent MakeMultiCellReagent(string id, List<AspectSet> aspectsPerCell)
    {
        // 1x N horizontal shape
        int cellCount = aspectsPerCell.Count;
        var shape = new bool[1, cellCount];
        for (int i = 0; i < cellCount; i++)
            shape[0, i] = true;
        var def = new ReagentDefinition(id, id, shape, aspectsPerCell: aspectsPerCell);
        return new Reagent(def);
    }

    [Fact]
    public void HasAspect_ReturnsTrue_WhenReagentHasAspect()
    {
        var reagent = MakeSingleCellReagent("fire", Aspect.FromId(AspectIds.Hot));
        Assert.True(reagent.HasAspect(AspectIds.Hot));
    }

    [Fact]
    public void HasAspect_ReturnsFalse_WhenReagentLacksAspect()
    {
        var reagent = MakeSingleCellReagent("fire", Aspect.FromId(AspectIds.Hot));
        Assert.False(reagent.HasAspect(AspectIds.Cold));
    }

    [Fact]
    public void HasAspect_ReturnsFalse_WhenNoAspects()
    {
        var def = new ReagentDefinition("inert", "Inert", new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { AspectSet.Empty });
        var reagent = new Reagent(def);
        Assert.False(reagent.HasAspect(AspectIds.Hot));
    }

    [Fact]
    public void HasAspect_ChecksAllCells()
    {
        // First cell has Hot, second cell has Cold
        var reagent = MakeMultiCellReagent("dual", new List<AspectSet>
        {
            new AspectSet(new[] { Aspect.FromId(AspectIds.Hot) }),
            new AspectSet(new[] { Aspect.FromId(AspectIds.Cold) })
        });

        Assert.True(reagent.HasAspect(AspectIds.Hot));
        Assert.True(reagent.HasAspect(AspectIds.Cold));
        Assert.False(reagent.HasAspect(AspectIds.Toxic));
    }

    [Fact]
    public void HasAspect_WorksWithHeavy()
    {
        var reagent = MakeSingleCellReagent("rock", Aspect.FromId(AspectIds.Heavy));
        Assert.True(reagent.HasAspect(AspectIds.Heavy));
        Assert.False(reagent.HasAspect(AspectIds.Hot));
    }

    [Fact]
    public void GetAggregatedAspects_EmptyAspects_ReturnsEmptyDictionary()
    {
        var def = new ReagentDefinition("empty", "Empty", new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { AspectSet.Empty });
        var reagent = new Reagent(def);

        var result = reagent.GetAggregatedAspects();

        Assert.Empty(result);
    }

    [Fact]
    public void GetAggregatedAspects_SingleCell_ReturnsSingleEntry()
    {
        var hot = new Aspect(AspectIds.Hot, "Hot", SchoolType.Physical, magnitude: 2);
        var reagent = MakeSingleCellReagent("fire", hot);

        var result = reagent.GetAggregatedAspects();

        Assert.Single(result);
        Assert.Equal(2, result[AspectIds.Hot].totalMagnitude);
        Assert.Equal("Hot", result[AspectIds.Hot].aspect.Name);
    }

    [Fact]
    public void GetAggregatedAspects_MultiCell_SumsMagnitudes()
    {
        var hot1 = new Aspect(AspectIds.Hot, "Hot", SchoolType.Physical);
        var hot2 = new Aspect(AspectIds.Hot, "Hot", SchoolType.Physical, magnitude: 3);
        var reagent = MakeMultiCellReagent("lava", new List<AspectSet>
        {
            new AspectSet(hot1),
            new AspectSet(hot2)
        });

        var result = reagent.GetAggregatedAspects();

        Assert.Single(result);
        Assert.Equal(4, result[AspectIds.Hot].totalMagnitude);
    }

    [Fact]
    public void GetAggregatedAspects_MultipleAspects_GroupsById()
    {
        var hot = new Aspect(AspectIds.Hot, "Hot", SchoolType.Physical, magnitude: 2);
        var cold = new Aspect(AspectIds.Cold, "Cold", SchoolType.Physical);
        var reagent = MakeMultiCellReagent("mixed", new List<AspectSet>
        {
            new AspectSet(hot),
            new AspectSet(cold)
        });

        var result = reagent.GetAggregatedAspects();

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[AspectIds.Hot].totalMagnitude);
        Assert.Equal(1, result[AspectIds.Cold].totalMagnitude);
    }
}
