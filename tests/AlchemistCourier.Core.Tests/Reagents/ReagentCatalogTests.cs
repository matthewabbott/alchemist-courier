using System.Collections.Generic;
using System.Linq;
using Xunit;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Tests.Reagents;

public class ReagentCatalogTests
{
    [Fact]
    public void GetAll_Returns72Definitions()
    {
        var all = ReagentCatalog.GetAll();
        Assert.Equal(72, all.Count);
    }

    [Fact]
    public void AllDefinitions_HaveUniqueIds()
    {
        var all = ReagentCatalog.GetAll();
        var ids = all.Select(d => d.Id).ToHashSet();
        Assert.Equal(72, ids.Count);
    }

    [Theory]
    [InlineData(AspectIds.Hot, 9)]
    [InlineData(AspectIds.Cold, 9)]
    [InlineData(AspectIds.Volatile, 9)]
    [InlineData(AspectIds.Toxic, 9)]
    [InlineData(AspectIds.Heavy, 9)]
    [InlineData(AspectIds.Stable, 9)]
    [InlineData(AspectIds.Sharp, 9)]
    [InlineData(AspectIds.Fragile, 9)]
    public void GetByAspect_Returns9PerSpecies(string aspectId, int expected)
    {
        var defs = ReagentCatalog.GetByAspect(aspectId);
        Assert.Equal(expected, defs.Count);
    }

    [Theory]
    [InlineData("monomino", 8)]
    [InlineData("domino", 8)]
    [InlineData("i_tri", 8)]
    [InlineData("l_tri", 8)]
    [InlineData("i_tet", 8)]
    [InlineData("o_tet", 8)]
    [InlineData("t_tet", 8)]
    [InlineData("s_tet", 8)]
    [InlineData("l_tet", 8)]
    public void GetByShape_Returns8PerShape(string shapeName, int expected)
    {
        var defs = ReagentCatalog.GetByShape(shapeName);
        Assert.Equal(expected, defs.Count);
    }

    [Theory]
    [InlineData("firevine", "domino", AspectIds.Hot)]
    [InlineData("snowflower", "monomino", AspectIds.Cold)]
    [InlineData("burstbloom", "t_tet", AspectIds.Volatile)]
    [InlineData("creeper", "l_tri", AspectIds.Toxic)]
    [InlineData("leadingot", "i_tet", AspectIds.Heavy)]
    [InlineData("nullflower", "o_tet", AspectIds.Stable)]
    [InlineData("sharpstone", "s_tet", AspectIds.Sharp)]
    [InlineData("glass", "l_tet", AspectIds.Fragile)]
    public void Get_ReturnsCorrectDefinition(string speciesId, string shapeName, string expectedAspect)
    {
        var def = ReagentCatalog.Get(speciesId, shapeName);
        Assert.NotNull(def);
        Assert.Equal($"{speciesId}_{shapeName}", def!.Id);

        // All cells should have the expected aspect
        foreach (var aspectSet in def.AspectsPerCell)
        {
            Assert.True(aspectSet.Has(expectedAspect));
        }
    }

    [Fact]
    public void Get_UnknownSpecies_ReturnsNull()
    {
        var def = ReagentCatalog.Get("unknown_species", "domino");
        Assert.Null(def);
    }

    [Fact]
    public void Get_UnknownShape_ReturnsNull()
    {
        var def = ReagentCatalog.Get("firevine", "pentomino");
        Assert.Null(def);
    }

    [Fact]
    public void GetById_ReturnsCorrectDefinition()
    {
        var def = ReagentCatalog.GetById("sharpstone_t_tet");
        Assert.NotNull(def);
        Assert.Equal("Sharpstone T-Piece", def!.Name);
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
                Assert.Equal(1, aspectSet.Aspects.Count);
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
        Assert.Equal("firevine_domino", match!.Id);
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
}
