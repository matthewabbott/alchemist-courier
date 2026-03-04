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
}
