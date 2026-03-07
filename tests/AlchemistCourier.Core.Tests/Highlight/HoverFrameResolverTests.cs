using System.Collections.Generic;
using Xunit;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Highlight;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Tests.Highlight;

public class HoverFrameResolverTests
{
    [Fact]
    public void GetFrameKey_CatalogItem_ReturnsAspectShapeRotation()
    {
        var reagent = new Reagent(ReagentCatalog.GetById("ember_seed")!);
        var key = HoverFrameResolver.GetFrameKey(reagent);
        Assert.Equal("hot_monomino_r0", key);
    }

    [Fact]
    public void GetFrameKey_Rotated_IncludesRotation()
    {
        var reagent = new Reagent(ReagentCatalog.GetById("firevine")!);
        reagent.Rotation = 1;
        var key = HoverFrameResolver.GetFrameKey(reagent);
        Assert.Equal("hot_domino_r1", key);
    }

    [Fact]
    public void GetFrameKey_AllRotations()
    {
        var reagent = new Reagent(ReagentCatalog.GetById("frost_fern")!);
        for (int r = 0; r < 4; r++)
        {
            reagent.Rotation = r;
            var key = HoverFrameResolver.GetFrameKey(reagent);
            Assert.Equal($"cold_i_tri_r{r}", key);
        }
    }

    [Fact]
    public void GetFrameKey_NoAspects_ReturnsNull()
    {
        var def = new ReagentDefinition("empty", "Empty", new bool[,] { { true } });
        var reagent = new Reagent(def);
        Assert.Null(HoverFrameResolver.GetFrameKey(reagent));
    }

    [Fact]
    public void GetFrameKey_NonCatalogShape_FallsBackToCustom()
    {
        // 5-cell plus shape not in catalog
        var def = new ReagentDefinition("plus", "Plus", new bool[,]
        {
            { false, true, false },
            { true, true, true },
            { false, true, false },
        }, aspectsPerCell: new[]
        {
            new AspectSet(Aspect.Hot()),
            new AspectSet(Aspect.Hot()),
            new AspectSet(Aspect.Hot()),
            new AspectSet(Aspect.Hot()),
            new AspectSet(Aspect.Hot()),
        });
        var reagent = new Reagent(def);
        var key = HoverFrameResolver.GetFrameKey(reagent);
        Assert.Equal("hot_custom5_r0", key);
    }

    [Fact]
    public void GetFrameKey_EachAspect_UsesCorrectAspectId()
    {
        var testCases = new[]
        {
            ("ember_seed", "hot"),
            ("frozen_teardrop", "cold"),
            ("spark_petal", "volatile"),
            ("venom_drop", "toxic"),
            ("lead_nugget", "heavy"),
            ("calm_stone", "stable"),
            ("glass_shard", "sharp"),
            ("crystal_tear", "fragile"),
        };

        foreach (var (id, expectedAspect) in testCases)
        {
            var reagent = new Reagent(ReagentCatalog.GetById(id)!);
            var key = HoverFrameResolver.GetFrameKey(reagent);
            Assert.StartsWith($"{expectedAspect}_", key);
        }
    }

    // --- Perimeter edge tests ---

    [Fact]
    public void GetPerimeterEdges_Monomino_AllFourEdges()
    {
        var reagent = new Reagent(ReagentCatalog.GetById("ember_seed")!);
        var edges = HoverFrameResolver.GetPerimeterEdges(reagent);

        Assert.Single(edges);
        var cell = new Vector2I(0, 0);
        Assert.True(edges.ContainsKey(cell));
        Assert.Equal(CellEdge.All, edges[cell]);
    }

    [Fact]
    public void GetPerimeterEdges_Domino_SharedEdgeExcluded()
    {
        var reagent = new Reagent(ReagentCatalog.GetById("firevine")!);
        var edges = HoverFrameResolver.GetPerimeterEdges(reagent);

        Assert.Equal(2, edges.Count);

        // Left cell (0,0): has Top, Bottom, Left but NOT Right (shared with right cell)
        var leftCell = new Vector2I(0, 0);
        Assert.True(edges[leftCell].HasFlag(CellEdge.Top));
        Assert.True(edges[leftCell].HasFlag(CellEdge.Bottom));
        Assert.True(edges[leftCell].HasFlag(CellEdge.Left));
        Assert.False(edges[leftCell].HasFlag(CellEdge.Right));

        // Right cell (1,0): has Top, Bottom, Right but NOT Left (shared with left cell)
        var rightCell = new Vector2I(1, 0);
        Assert.True(edges[rightCell].HasFlag(CellEdge.Top));
        Assert.True(edges[rightCell].HasFlag(CellEdge.Bottom));
        Assert.True(edges[rightCell].HasFlag(CellEdge.Right));
        Assert.False(edges[rightCell].HasFlag(CellEdge.Left));
    }

    [Fact]
    public void GetPerimeterEdges_LTri_CornerEdgesCorrect()
    {
        // l_tri shape: {{true, false}, {true, true}}
        // Cells: (0,0), (0,1), (1,1)
        var reagent = new Reagent(ReagentCatalog.GetById("magma_slug")!);
        var edges = HoverFrameResolver.GetPerimeterEdges(reagent);

        Assert.Equal(3, edges.Count);

        // Top-left (0,0): Top, Left, Right exposed. Bottom shared with (0,1).
        var topLeft = new Vector2I(0, 0);
        Assert.True(edges[topLeft].HasFlag(CellEdge.Top));
        Assert.True(edges[topLeft].HasFlag(CellEdge.Left));
        Assert.True(edges[topLeft].HasFlag(CellEdge.Right));
        Assert.False(edges[topLeft].HasFlag(CellEdge.Bottom));

        // Bottom-left (0,1): Bottom, Left exposed. Top shared with (0,0). Right shared with (1,1).
        var bottomLeft = new Vector2I(0, 1);
        Assert.True(edges[bottomLeft].HasFlag(CellEdge.Bottom));
        Assert.True(edges[bottomLeft].HasFlag(CellEdge.Left));
        Assert.False(edges[bottomLeft].HasFlag(CellEdge.Top));
        Assert.False(edges[bottomLeft].HasFlag(CellEdge.Right));

        // Bottom-right (1,1): Top, Bottom, Right exposed. Left shared with (0,1).
        var bottomRight = new Vector2I(1, 1);
        Assert.True(edges[bottomRight].HasFlag(CellEdge.Top));
        Assert.True(edges[bottomRight].HasFlag(CellEdge.Bottom));
        Assert.True(edges[bottomRight].HasFlag(CellEdge.Right));
        Assert.False(edges[bottomRight].HasFlag(CellEdge.Left));
    }

    [Fact]
    public void GetPerimeterEdges_Rotated_UsesRotatedShape()
    {
        // Firevine (domino): unrotated is horizontal {{true, true}}
        // After 90° CW rotation, becomes vertical
        var reagent = new Reagent(ReagentCatalog.GetById("firevine")!);
        reagent.Rotation = 1;
        var edges = HoverFrameResolver.GetPerimeterEdges(reagent);

        Assert.Equal(2, edges.Count);

        // After 90° CW, domino should be vertical: (0,0) and (0,1)
        var topCell = new Vector2I(0, 0);
        var bottomCell = new Vector2I(0, 1);
        Assert.True(edges.ContainsKey(topCell));
        Assert.True(edges.ContainsKey(bottomCell));

        // Top cell: Top, Left, Right exposed. Bottom shared.
        Assert.True(edges[topCell].HasFlag(CellEdge.Top));
        Assert.True(edges[topCell].HasFlag(CellEdge.Left));
        Assert.True(edges[topCell].HasFlag(CellEdge.Right));
        Assert.False(edges[topCell].HasFlag(CellEdge.Bottom));

        // Bottom cell: Bottom, Left, Right exposed. Top shared.
        Assert.True(edges[bottomCell].HasFlag(CellEdge.Bottom));
        Assert.True(edges[bottomCell].HasFlag(CellEdge.Left));
        Assert.True(edges[bottomCell].HasFlag(CellEdge.Right));
        Assert.False(edges[bottomCell].HasFlag(CellEdge.Top));
    }

    [Fact]
    public void GetPerimeterEdges_ShapeOverload_MatchesReagentOverload()
    {
        var def = ReagentCatalog.GetById("blight_moss")!;
        var reagent = new Reagent(def);
        var fromReagent = HoverFrameResolver.GetPerimeterEdges(reagent);
        var fromShape = HoverFrameResolver.GetPerimeterEdges(def.Shape);

        Assert.Equal(fromReagent.Count, fromShape.Count);
        foreach (var kvp in fromReagent)
        {
            Assert.True(fromShape.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, fromShape[kvp.Key]);
        }
    }

    [Fact]
    public void GetPerimeterEdges_OTet_InternalEdgesFullyExcluded()
    {
        // o_tet: 2x2 square. All 4 cells share internal edges.
        var reagent = new Reagent(ReagentCatalog.GetById("thunder_egg")!);
        var edges = HoverFrameResolver.GetPerimeterEdges(reagent);

        Assert.Equal(4, edges.Count);

        // Top-left (0,0): Top and Left only
        Assert.Equal(CellEdge.Top | CellEdge.Left, edges[new Vector2I(0, 0)]);
        // Top-right (1,0): Top and Right only
        Assert.Equal(CellEdge.Top | CellEdge.Right, edges[new Vector2I(1, 0)]);
        // Bottom-left (0,1): Bottom and Left only
        Assert.Equal(CellEdge.Bottom | CellEdge.Left, edges[new Vector2I(0, 1)]);
        // Bottom-right (1,1): Bottom and Right only
        Assert.Equal(CellEdge.Bottom | CellEdge.Right, edges[new Vector2I(1, 1)]);
    }
}
