using System.Collections.Generic;
using System.Linq;
using Xunit;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Tests.Reagents;

public class ReagentCatalogTests
{
    [Fact]
    public void GetAll_Returns24Definitions()
    {
        var all = ReagentCatalog.GetAll();
        Assert.Equal(24, all.Count);
    }

    [Fact]
    public void AllDefinitions_HaveUniqueIds()
    {
        var all = ReagentCatalog.GetAll();
        var ids = all.Select(d => d.Id).ToHashSet();
        Assert.Equal(24, ids.Count);
    }

    [Theory]
    [InlineData(AspectIds.Hot, 3)]
    [InlineData(AspectIds.Cold, 3)]
    [InlineData(AspectIds.Volatile, 3)]
    [InlineData(AspectIds.Toxic, 3)]
    [InlineData(AspectIds.Heavy, 3)]
    [InlineData(AspectIds.Stable, 3)]
    [InlineData(AspectIds.Sharp, 3)]
    [InlineData(AspectIds.Fragile, 3)]
    public void GetByAspect_Returns3PerAspect(string aspectId, int expected)
    {
        var defs = ReagentCatalog.GetByAspect(aspectId);
        Assert.Equal(expected, defs.Count);
    }

    [Theory]
    [InlineData("monomino", 8)]
    [InlineData("domino", 7)]
    [InlineData("i_tri", 2)]
    [InlineData("l_tri", 2)]
    [InlineData("i_tet", 1)]
    [InlineData("o_tet", 1)]
    [InlineData("t_tet", 1)]
    [InlineData("s_tet", 1)]
    [InlineData("l_tet", 1)]
    public void GetByShape_ReturnsExpectedCount(string shapeName, int expected)
    {
        var defs = ReagentCatalog.GetByShape(shapeName);
        Assert.Equal(expected, defs.Count);
    }

    [Theory]
    [InlineData("ember_seed", AspectIds.Hot)]
    [InlineData("firevine", AspectIds.Hot)]
    [InlineData("magma_slug", AspectIds.Hot)]
    [InlineData("frozen_teardrop", AspectIds.Cold)]
    [InlineData("snowflower", AspectIds.Cold)]
    [InlineData("frost_fern", AspectIds.Cold)]
    [InlineData("spark_petal", AspectIds.Volatile)]
    [InlineData("burstbloom", AspectIds.Volatile)]
    [InlineData("thunder_egg", AspectIds.Volatile)]
    [InlineData("venom_drop", AspectIds.Toxic)]
    [InlineData("purple_creeper", AspectIds.Toxic)]
    [InlineData("blight_moss", AspectIds.Toxic)]
    [InlineData("lead_nugget", AspectIds.Heavy)]
    [InlineData("lead_ingot", AspectIds.Heavy)]
    [InlineData("anvil_shard", AspectIds.Heavy)]
    [InlineData("calm_stone", AspectIds.Stable)]
    [InlineData("nullflower", AspectIds.Stable)]
    [InlineData("ironbark_root", AspectIds.Stable)]
    [InlineData("glass_shard", AspectIds.Sharp)]
    [InlineData("sharpstone", AspectIds.Sharp)]
    [InlineData("razor_fern", AspectIds.Sharp)]
    [InlineData("crystal_tear", AspectIds.Fragile)]
    [InlineData("glasswork", AspectIds.Fragile)]
    [InlineData("gossamer_thread", AspectIds.Fragile)]
    public void GetById_ReturnsCorrectAspect(string id, string expectedAspect)
    {
        var def = ReagentCatalog.GetById(id);
        Assert.NotNull(def);
        foreach (var aspectSet in def!.AspectsPerCell)
        {
            Assert.True(aspectSet.Has(expectedAspect));
        }
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        Assert.Null(ReagentCatalog.GetById("nonexistent"));
    }

    [Fact]
    public void GetByAspect_UnknownAspect_ReturnsEmpty()
    {
        var defs = ReagentCatalog.GetByAspect("nonexistent");
        Assert.Empty(defs);
    }

    [Fact]
    public void GetByShape_UnknownShape_ReturnsEmpty()
    {
        var defs = ReagentCatalog.GetByShape("nonexistent");
        Assert.Empty(defs);
    }

    [Theory]
    [InlineData("monomino", 1)]
    [InlineData("domino", 2)]
    [InlineData("i_tri", 3)]
    [InlineData("l_tri", 3)]
    [InlineData("i_tet", 4)]
    [InlineData("o_tet", 4)]
    [InlineData("t_tet", 4)]
    [InlineData("s_tet", 4)]
    [InlineData("l_tet", 4)]
    public void ShapeCellCounts_AreCorrect(string shapeName, int expectedCells)
    {
        var defs = ReagentCatalog.GetByShape(shapeName);
        foreach (var def in defs)
        {
            Assert.Equal(expectedCells, def.CellCount);
        }
    }

    [Fact]
    public void AllDefinitions_HaveFlavorText()
    {
        foreach (var def in ReagentCatalog.GetAll())
        {
            Assert.NotNull(def.FlavorText);
            Assert.NotEmpty(def.FlavorText!);
        }
    }

    [Fact]
    public void AllDefinitions_AreSingleAspect()
    {
        foreach (var def in ReagentCatalog.GetAll())
        {
            foreach (var aspectSet in def.AspectsPerCell)
            {
                Assert.Single(aspectSet.Aspects);
            }
        }
    }

    [Fact]
    public void AllDefinitions_HaveConsistentAspectAcrossCells()
    {
        foreach (var def in ReagentCatalog.GetAll())
        {
            if (def.AspectsPerCell.Count <= 1)
                continue;

            var firstAspectId = def.AspectsPerCell[0].Aspects[0].Id;
            foreach (var aspectSet in def.AspectsPerCell)
            {
                Assert.Equal(firstAspectId, aspectSet.Aspects[0].Id);
            }
        }
    }

    [Fact]
    public void FindMatch_MatchesCorrectEntry()
    {
        var dominoShape = new bool[,] { { true, true } };
        var match = ReagentCatalog.FindMatch(AspectIds.Hot, dominoShape);
        Assert.NotNull(match);
        Assert.Equal("firevine", match!.Id);
    }

    [Fact]
    public void FindMatch_NoMatch_ReturnsNull()
    {
        // A 5-cell shape that doesn't exist in the catalog
        var bigShape = new bool[,] { { true, true, true, true, true } };
        var match = ReagentCatalog.FindMatch(AspectIds.Hot, bigShape);
        Assert.Null(match);
    }

    [Fact]
    public void FindMatch_UnknownAspect_ReturnsNull()
    {
        var shape = new bool[,] { { true } };
        var match = ReagentCatalog.FindMatch("nonexistent", shape);
        Assert.Null(match);
    }

    [Fact]
    public void GetShapeMatrix_ReturnsCorrectShape()
    {
        var shape = ReagentCatalog.GetShapeMatrix("t_tet");
        Assert.NotNull(shape);
        Assert.Equal(2, shape!.GetLength(0)); // rows
        Assert.Equal(3, shape.GetLength(1));  // cols
        // T-shape: top row all true, bottom middle true
        Assert.True(shape[0, 0]);
        Assert.True(shape[0, 1]);
        Assert.True(shape[0, 2]);
        Assert.False(shape[1, 0]);
        Assert.True(shape[1, 1]);
        Assert.False(shape[1, 2]);
    }

    [Fact]
    public void GetShapeMatrix_UnknownShape_ReturnsNull()
    {
        Assert.Null(ReagentCatalog.GetShapeMatrix("nonexistent"));
    }

    [Fact]
    public void GetShapeMatrix_ReturnsClone()
    {
        var shape1 = ReagentCatalog.GetShapeMatrix("monomino");
        var shape2 = ReagentCatalog.GetShapeMatrix("monomino");
        Assert.NotSame(shape1, shape2);
    }

    [Fact]
    public void AllNineShapes_AreCoveredInCatalog()
    {
        var coveredShapes = new HashSet<string>();
        foreach (var def in ReagentCatalog.GetAll())
        {
            var shapeName = ReagentCatalog.GetShapeName(def);
            if (shapeName != null)
                coveredShapes.Add(shapeName);
        }

        foreach (var shapeName in ReagentCatalog.ShapeNames)
        {
            Assert.Contains(shapeName, coveredShapes);
        }
    }

    [Fact]
    public void Rarity_MatchesCellCount()
    {
        foreach (var def in ReagentCatalog.GetAll())
        {
            if (def.CellCount <= 2)
                Assert.Equal(Rarity.Common, def.Rarity);
            else if (def.CellCount == 3)
                Assert.Equal(Rarity.Uncommon, def.Rarity);
            else
                Assert.Equal(Rarity.Rare, def.Rarity);
        }
    }

    [Fact]
    public void GetShapeName_ReturnsCorrectShape()
    {
        var firevine = ReagentCatalog.GetById("firevine");
        Assert.NotNull(firevine);
        Assert.Equal("domino", ReagentCatalog.GetShapeName(firevine!));

        var thunderEgg = ReagentCatalog.GetById("thunder_egg");
        Assert.NotNull(thunderEgg);
        Assert.Equal("o_tet", ReagentCatalog.GetShapeName(thunderEgg!));
    }
}
