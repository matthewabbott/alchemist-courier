using System.Collections.Generic;
using System.Linq;
using Xunit;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Highlight;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Tests.Highlight;

public class HighlightEngineTests
{
    // --- Helper methods ---

    private static MetaGrid CreateGrid(int width = 10, int height = 10)
        => new MetaGrid(width, height);

    private static BagDefinition CreateGravityBagDef(int rows, int cols)
    {
        var shape = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                shape[r, c] = true;
        return new BagDefinition("gravity_bag", "Gravity Bag", shape,
            new[] { BagPropertyType.Gravity });
    }

    private static ReagentDefinition CreateSharpReagentDef(bool[,] shape)
    {
        int cellCount = 0;
        for (int r = 0; r < shape.GetLength(0); r++)
            for (int c = 0; c < shape.GetLength(1); c++)
                if (shape[r, c]) cellCount++;

        var aspects = new List<AspectSet>();
        for (int i = 0; i < cellCount; i++)
            aspects.Add(new AspectSet(Aspect.FromId(AspectIds.Sharp)));
        return new ReagentDefinition("sharp_r", "Sharp Reagent", shape,
            aspectsPerCell: aspects);
    }

    private static ReagentDefinition CreateFragileReagentDef(bool[,] shape)
    {
        int cellCount = 0;
        for (int r = 0; r < shape.GetLength(0); r++)
            for (int c = 0; c < shape.GetLength(1); c++)
                if (shape[r, c]) cellCount++;

        var aspects = new List<AspectSet>();
        for (int i = 0; i < cellCount; i++)
            aspects.Add(new AspectSet(Aspect.Fragile()));
        return new ReagentDefinition("fragile", "Fragile Reagent", shape,
            aspectsPerCell: aspects);
    }

    private static ReagentDefinition CreateHeavyReagentDef(bool[,] shape)
    {
        int cellCount = 0;
        for (int r = 0; r < shape.GetLength(0); r++)
            for (int c = 0; c < shape.GetLength(1); c++)
                if (shape[r, c]) cellCount++;

        var aspects = new List<AspectSet>();
        for (int i = 0; i < cellCount; i++)
            aspects.Add(new AspectSet(Aspect.Heavy()));
        return new ReagentDefinition("heavy_r", "Heavy Reagent", shape,
            aspectsPerCell: aspects);
    }

    // --- ComputeConstraintHighlights tests ---

    [Fact]
    public void SharpOnEdge_ProducesDangerHighlightsWithCorrectEdges()
    {
        // 3x3 bag — all border cells are edge cells, only (1,1) is interior
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        var highlights = HighlightEngine.ComputeConstraintHighlights(grid, sharp, new Vector2I(0, 0));

        Assert.False(highlights.IsEmpty);
        var constraintHighlights = highlights.GetByLayer(HighlightLayer.Constraint);
        Assert.Single(constraintHighlights);

        var h = constraintHighlights[0];
        Assert.Equal(new Vector2I(0, 0), h.Position);
        Assert.Equal(HighlightSeverity.Danger, h.Severity);
        Assert.Equal(AspectIds.Sharp, h.AspectId);
        // Single cell should have All edges (contiguous boundary of one cell)
        Assert.Equal(CellEdge.All, h.Edges);
    }

    [Fact]
    public void SharpOnEdge_MultiCell_ProducesMultipleDangerHighlights()
    {
        // 3x3 bag, Sharp domino at (0,0) — occupies (0,0) and (1,0), both edge
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true, true } }));
        var highlights = HighlightEngine.ComputeConstraintHighlights(grid, sharp, new Vector2I(0, 0));

        var constraintHighlights = highlights.GetByLayer(HighlightLayer.Constraint);
        Assert.Equal(2, constraintHighlights.Count);
        Assert.All(constraintHighlights, h => Assert.Equal(HighlightSeverity.Danger, h.Severity));
        Assert.All(constraintHighlights, h => Assert.Equal(AspectIds.Sharp, h.AspectId));

        // Two adjacent cells: left cell has Left edge but not Right, right cell has Right but not Left
        var leftCell = constraintHighlights.First(h => h.Position == new Vector2I(0, 0));
        var rightCell = constraintHighlights.First(h => h.Position == new Vector2I(1, 0));
        Assert.True(leftCell.Edges.HasFlag(CellEdge.Left));
        Assert.False(leftCell.Edges.HasFlag(CellEdge.Right));
        Assert.True(rightCell.Edges.HasFlag(CellEdge.Right));
        Assert.False(rightCell.Edges.HasFlag(CellEdge.Left));
    }

    [Fact]
    public void SharpInInterior_NoHighlights()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        var highlights = HighlightEngine.ComputeConstraintHighlights(grid, sharp, new Vector2I(1, 1));

        Assert.True(highlights.IsEmpty);
    }

    [Fact]
    public void FragileInGravityBag_NoConstraintHighlight()
    {
        // D.1: Fragile reagents are now allowed in Gravity bags — no constraint highlight
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var fragile = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));
        var highlights = HighlightEngine.ComputeConstraintHighlights(grid, fragile, new Vector2I(1, 2));

        var constraintHighlights = highlights.GetByLayer(HighlightLayer.Constraint);
        // No Fragile constraint violations — gravity bag is allowed
        Assert.DoesNotContain(constraintHighlights, h => h.AspectId == AspectIds.Fragile);
    }

    [Fact]
    public void FragileInNormalBag_NoHighlights()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var fragile = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));
        var highlights = HighlightEngine.ComputeConstraintHighlights(grid, fragile, new Vector2I(1, 1));

        Assert.True(highlights.IsEmpty);
    }

    [Fact]
    public void HeavyAboveNonHeavy_ProducesWarningHighlight()
    {
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Place a non-Heavy reagent at bottom
        var nonHeavy = new Reagent(ReagentDefinition.SingleCell);
        grid.PlaceReagent(nonHeavy, new Vector2I(1, 2));

        // Heavy reagent above it should produce violation
        var heavy = new Reagent(CreateHeavyReagentDef(new bool[,] { { true } }));
        var highlights = HighlightEngine.ComputeConstraintHighlights(grid, heavy, new Vector2I(1, 1));

        Assert.False(highlights.IsEmpty);
        var constraintHighlights = highlights.GetByLayer(HighlightLayer.Constraint);
        Assert.Single(constraintHighlights);
        Assert.Equal(HighlightSeverity.Warning, constraintHighlights[0].Severity);
        Assert.Equal(AspectIds.Heavy, constraintHighlights[0].AspectId);
    }

    [Fact]
    public void GravityUnsupported_ProducesWarningHighlight()
    {
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Place at top row without support — floating
        var reagent = new Reagent(ReagentDefinition.SingleCell);
        var highlights = HighlightEngine.ComputeConstraintHighlights(grid, reagent, new Vector2I(1, 0));

        Assert.False(highlights.IsEmpty);
        var constraintHighlights = highlights.GetByLayer(HighlightLayer.Constraint);
        Assert.Single(constraintHighlights);
        Assert.Equal(HighlightSeverity.Warning, constraintHighlights[0].Severity);
    }

    [Fact]
    public void NoViolations_ReturnsEmptyHighlightSet()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        var highlights = HighlightEngine.ComputeConstraintHighlights(grid, reagent, new Vector2I(1, 1));

        Assert.True(highlights.IsEmpty);
    }

    // --- Interaction highlight helpers ---

    private static ReagentDefinition CreateAspectReagentDef(string id, string name, bool[,] shape, params Aspect[] aspects)
    {
        int cellCount = 0;
        for (int r = 0; r < shape.GetLength(0); r++)
            for (int c = 0; c < shape.GetLength(1); c++)
                if (shape[r, c]) cellCount++;

        var aspectSets = new List<AspectSet>();
        for (int i = 0; i < cellCount; i++)
            aspectSets.Add(new AspectSet(aspects));
        return new ReagentDefinition(id, name, shape, aspectsPerCell: aspectSets);
    }

    private static List<ReactionRule> MvpRules() => ReactionRuleDatabase.GetMvpRules();

    // --- ComputeInteractionHighlights tests ---

    [Fact]
    public void Interaction_VolatileAdjacentToHot_HighlightsHotCellWithHotAspect()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Place a Hot reagent at (1,1)
        var hotReagent = new Reagent(CreateAspectReagentDef("hot_r", "Fire", new bool[,] { { true } }, Aspect.Hot()));
        grid.PlaceReagent(hotReagent, new Vector2I(1, 1));

        // Volatile reagent at (0,1) — adjacent to Hot on the right
        var volatileReagent = new Reagent(CreateAspectReagentDef("vol_r", "Volatile", new bool[,] { { true } }, Aspect.Volatile()));
        var highlights = HighlightEngine.ComputeInteractionHighlights(
            grid, volatileReagent, new Vector2I(0, 1), MvpRules(), null);

        Assert.False(highlights.IsEmpty);
        var interactions = highlights.GetByLayer(HighlightLayer.Interaction);
        Assert.Single(interactions);

        var h = interactions[0];
        Assert.Equal(new Vector2I(1, 1), h.Position);
        Assert.Equal(AspectIds.Hot, h.AspectId);
    }

    [Fact]
    public void Interaction_VolatileAdjacentToCold_HighlightsColdCell()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var coldReagent = new Reagent(CreateAspectReagentDef("cold_r", "Ice", new bool[,] { { true } }, Aspect.Cold()));
        grid.PlaceReagent(coldReagent, new Vector2I(1, 1));

        var volatileReagent = new Reagent(CreateAspectReagentDef("vol_r", "Volatile", new bool[,] { { true } }, Aspect.Volatile()));
        var highlights = HighlightEngine.ComputeInteractionHighlights(
            grid, volatileReagent, new Vector2I(0, 1), MvpRules(), null);

        Assert.False(highlights.IsEmpty);
        var interactions = highlights.GetByLayer(HighlightLayer.Interaction);
        Assert.Single(interactions);
        Assert.Equal(AspectIds.Cold, interactions[0].AspectId);
    }

    [Fact]
    public void Interaction_SharpAdjacentToFragile_HighlightsFragileCell()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var fragileReagent = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));
        grid.PlaceReagent(fragileReagent, new Vector2I(1, 1));

        var sharpReagent = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        // Place Sharp at (2,1) — adjacent to Fragile on the right
        var highlights = HighlightEngine.ComputeInteractionHighlights(
            grid, sharpReagent, new Vector2I(2, 1), MvpRules(), null);

        Assert.False(highlights.IsEmpty);
        var interactions = highlights.GetByLayer(HighlightLayer.Interaction);
        Assert.Single(interactions);
        Assert.Equal(new Vector2I(1, 1), interactions[0].Position);
        Assert.Equal(AspectIds.Fragile, interactions[0].AspectId);
    }

    [Fact]
    public void Interaction_NoReactiveNeighbor_ReturnsEmpty()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Place a Stable reagent — not reactive with Volatile
        var stableReagent = new Reagent(CreateAspectReagentDef("stable_r", "Stable", new bool[,] { { true } }, Aspect.Stable()));
        grid.PlaceReagent(stableReagent, new Vector2I(1, 1));

        var volatileReagent = new Reagent(CreateAspectReagentDef("vol_r", "Volatile", new bool[,] { { true } }, Aspect.Volatile()));
        var highlights = HighlightEngine.ComputeInteractionHighlights(
            grid, volatileReagent, new Vector2I(0, 1), MvpRules(), null);

        Assert.True(highlights.IsEmpty);
    }

    [Fact]
    public void Interaction_ContiguousHotCells_BoundaryEliminatesInternalEdges()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Two adjacent Hot reagents: (1,0) and (1,1)
        var hot1 = new Reagent(CreateAspectReagentDef("hot1", "Fire1", new bool[,] { { true } }, Aspect.Hot()));
        var hot2 = new Reagent(CreateAspectReagentDef("hot2", "Fire2", new bool[,] { { true } }, Aspect.Hot()));
        grid.PlaceReagent(hot1, new Vector2I(1, 0));
        grid.PlaceReagent(hot2, new Vector2I(1, 1));

        // Volatile reagent at (0,0) — adjacent to hot1 at (1,0)
        var volatileReagent = new Reagent(CreateAspectReagentDef("vol_r", "Volatile", new bool[,] { { true, true } }, Aspect.Volatile()));
        var highlights = HighlightEngine.ComputeInteractionHighlights(
            grid, volatileReagent, new Vector2I(0, 0), MvpRules(), null);

        var interactions = highlights.GetByLayer(HighlightLayer.Interaction);
        var hotHighlights = interactions.Where(h => h.AspectId == AspectIds.Hot).ToList();
        Assert.Equal(2, hotHighlights.Count);

        // The two cells are vertically adjacent — internal Top/Bottom edges eliminated
        var topCell = hotHighlights.First(h => h.Position == new Vector2I(1, 0));
        var bottomCell = hotHighlights.First(h => h.Position == new Vector2I(1, 1));
        Assert.False(topCell.Edges.HasFlag(CellEdge.Bottom));
        Assert.False(bottomCell.Edges.HasFlag(CellEdge.Top));
    }

    [Fact]
    public void Interaction_SeverityFromStatusThresholds()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var hotReagent = new Reagent(CreateAspectReagentDef("hot_r", "Fire", new bool[,] { { true } }, Aspect.Hot()));
        grid.PlaceReagent(hotReagent, new Vector2I(1, 1));

        var volatileReagent = new Reagent(CreateAspectReagentDef("vol_r", "Volatile", new bool[,] { { true } }, Aspect.Volatile()));

        var thresholds = new StatusThresholds();
        thresholds.Set(AspectIds.Hot, 4, 4); // Danger level

        var highlights = HighlightEngine.ComputeInteractionHighlights(
            grid, volatileReagent, new Vector2I(0, 1), MvpRules(), thresholds);

        var interactions = highlights.GetByLayer(HighlightLayer.Interaction);
        Assert.Single(interactions);
        Assert.Equal(HighlightSeverity.Danger, interactions[0].Severity);
    }

    [Fact]
    public void Interaction_NoThresholdEntry_DefaultsToWarning()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var fragileReagent = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));
        grid.PlaceReagent(fragileReagent, new Vector2I(1, 1));

        var sharpReagent = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        var highlights = HighlightEngine.ComputeInteractionHighlights(
            grid, sharpReagent, new Vector2I(2, 1), MvpRules(), new StatusThresholds());

        var interactions = highlights.GetByLayer(HighlightLayer.Interaction);
        Assert.Single(interactions);
        // Fragile has no StatusThresholds entry → default Warning
        Assert.Equal(HighlightSeverity.Warning, interactions[0].Severity);
    }

    [Fact]
    public void Interaction_HotAdjacentToVolatile_HighlightsVolatileCell()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Volatile is placed on the grid
        var volatilePlaced = new Reagent(CreateAspectReagentDef("vol_p", "Volatile", new bool[,] { { true } }, Aspect.Volatile()));
        grid.PlaceReagent(volatilePlaced, new Vector2I(1, 1));

        // Preview Hot reagent at (0,1)
        var hotReagent = new Reagent(CreateAspectReagentDef("hot_r", "Fire", new bool[,] { { true } }, Aspect.Hot()));
        var highlights = HighlightEngine.ComputeInteractionHighlights(
            grid, hotReagent, new Vector2I(0, 1), MvpRules(), null);

        var interactions = highlights.GetByLayer(HighlightLayer.Interaction);
        Assert.Single(interactions);
        Assert.Equal(new Vector2I(1, 1), interactions[0].Position);
        Assert.Equal(AspectIds.Volatile, interactions[0].AspectId);
    }

    // --- ComputeLegendHighlights tests ---

    [Fact]
    public void Legend_FindsAllHotCellsWithBoundaryEdges()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Place two Hot reagents: single cells at (0,0) and (0,1) — vertically adjacent
        var hot1 = new Reagent(CreateAspectReagentDef("hot1", "Fire1", new bool[,] { { true } }, Aspect.Hot()));
        var hot2 = new Reagent(CreateAspectReagentDef("hot2", "Fire2", new bool[,] { { true } }, Aspect.Hot()));
        grid.PlaceReagent(hot1, new Vector2I(0, 0));
        grid.PlaceReagent(hot2, new Vector2I(0, 1));

        var highlights = HighlightEngine.ComputeLegendHighlights(grid, AspectIds.Hot);

        Assert.False(highlights.IsEmpty);
        var legendHighlights = highlights.GetByLayer(HighlightLayer.Legend);
        Assert.Equal(2, legendHighlights.Count);
        Assert.All(legendHighlights, h => Assert.Equal(AspectIds.Hot, h.AspectId));
        Assert.All(legendHighlights, h => Assert.Equal(HighlightSeverity.Info, h.Severity));

        // Adjacent cells: internal edge eliminated
        var topCell = legendHighlights.First(h => h.Position == new Vector2I(0, 0));
        var bottomCell = legendHighlights.First(h => h.Position == new Vector2I(0, 1));
        Assert.False(topCell.Edges.HasFlag(CellEdge.Bottom));
        Assert.False(bottomCell.Edges.HasFlag(CellEdge.Top));
    }

    [Fact]
    public void Legend_NoMatchingAspect_ReturnsEmpty()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var hot = new Reagent(CreateAspectReagentDef("hot_r", "Fire", new bool[,] { { true } }, Aspect.Hot()));
        grid.PlaceReagent(hot, new Vector2I(0, 0));

        var highlights = HighlightEngine.ComputeLegendHighlights(grid, AspectIds.Cold);
        Assert.True(highlights.IsEmpty);
    }

    [Fact]
    public void Legend_EmptyGrid_ReturnsEmpty()
    {
        var grid = CreateGrid();
        var highlights = HighlightEngine.ComputeLegendHighlights(grid, AspectIds.Hot);
        Assert.True(highlights.IsEmpty);
    }

    [Fact]
    public void Legend_MultiCellReagent_IncludesAllAspectCells()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Domino with Hot on both cells
        var hotDomino = new Reagent(CreateAspectReagentDef("hot_d", "Fire Shard",
            new bool[,] { { true, true } }, Aspect.Hot()));
        grid.PlaceReagent(hotDomino, new Vector2I(0, 0));

        var highlights = HighlightEngine.ComputeLegendHighlights(grid, AspectIds.Hot);
        var legendHighlights = highlights.GetByLayer(HighlightLayer.Legend);
        Assert.Equal(2, legendHighlights.Count);
        Assert.Contains(legendHighlights, h => h.Position == new Vector2I(0, 0));
        Assert.Contains(legendHighlights, h => h.Position == new Vector2I(1, 0));
    }

    // --- ComputeEagleSenseHighlights tests ---

    [Fact]
    public void EagleSense_ReturnsUnionOfAllInteractions()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Volatile at (0,1), Hot at (1,1) — adjacent, should interact
        var volatileReagent = new Reagent(CreateAspectReagentDef("vol_r", "Volatile", new bool[,] { { true } }, Aspect.Volatile()));
        var hotReagent = new Reagent(CreateAspectReagentDef("hot_r", "Fire", new bool[,] { { true } }, Aspect.Hot()));
        grid.PlaceReagent(volatileReagent, new Vector2I(0, 1));
        grid.PlaceReagent(hotReagent, new Vector2I(1, 1));

        var highlights = HighlightEngine.ComputeEagleSenseHighlights(grid, MvpRules(), null);

        Assert.False(highlights.IsEmpty);
        var interactions = highlights.GetByLayer(HighlightLayer.Interaction);
        // Volatile's perspective: Hot neighbor highlighted with "hot"
        // Hot's perspective: Volatile neighbor highlighted with "volatile"
        Assert.Contains(interactions, h => h.Position == new Vector2I(1, 1) && h.AspectId == AspectIds.Hot);
        Assert.Contains(interactions, h => h.Position == new Vector2I(0, 1) && h.AspectId == AspectIds.Volatile);
    }

    [Fact]
    public void EagleSense_NoInteractions_ReturnsEmpty()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Two non-interacting reagents far apart
        var hotReagent = new Reagent(CreateAspectReagentDef("hot_r", "Fire", new bool[,] { { true } }, Aspect.Hot()));
        var coldReagent = new Reagent(CreateAspectReagentDef("cold_r", "Ice", new bool[,] { { true } }, Aspect.Cold()));
        grid.PlaceReagent(hotReagent, new Vector2I(0, 0));
        grid.PlaceReagent(coldReagent, new Vector2I(2, 2)); // Not adjacent

        var highlights = HighlightEngine.ComputeEagleSenseHighlights(grid, MvpRules(), null);
        Assert.True(highlights.IsEmpty);
    }

    [Fact]
    public void EagleSense_MultipleInteractions_IncludesAll()
    {
        // Use a 5x5 bag so Sharp can be placed in interior cells
        var grid = CreateGrid();
        var largeBagDef = new BagDefinition("large", "Large Bag", new bool[,]
        {
            { true, true, true, true, true },
            { true, true, true, true, true },
            { true, true, true, true, true },
            { true, true, true, true, true },
            { true, true, true, true, true },
        });
        var bag = new Bag(largeBagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Volatile at (2,2), Hot at (3,2) — Volatile↔Hot interaction
        // Sharp at (1,2), Fragile at (1,1) — Sharp↔Fragile interaction
        var volatileReagent = new Reagent(CreateAspectReagentDef("vol_r", "Volatile", new bool[,] { { true } }, Aspect.Volatile()));
        var hotReagent = new Reagent(CreateAspectReagentDef("hot_r", "Fire", new bool[,] { { true } }, Aspect.Hot()));
        var sharpReagent = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        var fragileReagent = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));

        grid.PlaceReagent(volatileReagent, new Vector2I(2, 2));
        grid.PlaceReagent(hotReagent, new Vector2I(3, 2));
        grid.PlaceReagent(sharpReagent, new Vector2I(1, 2));
        grid.PlaceReagent(fragileReagent, new Vector2I(1, 1));

        var highlights = HighlightEngine.ComputeEagleSenseHighlights(grid, MvpRules(), null);
        var interactions = highlights.GetByLayer(HighlightLayer.Interaction);

        // Should include Volatile↔Hot and Sharp↔Fragile interactions
        Assert.True(interactions.Count >= 3);
        Assert.Contains(interactions, h => h.AspectId == AspectIds.Hot);
        Assert.Contains(interactions, h => h.AspectId == AspectIds.Volatile);
        Assert.Contains(interactions, h => h.AspectId == AspectIds.Fragile || h.AspectId == AspectIds.Sharp);
    }

    [Fact]
    public void EagleSense_EmptyGrid_ReturnsEmpty()
    {
        var grid = CreateGrid();
        var highlights = HighlightEngine.ComputeEagleSenseHighlights(grid, MvpRules(), null);
        Assert.True(highlights.IsEmpty);
    }

    // --- GetConstraintViolations tests ---

    [Fact]
    public void GetConstraintViolations_AggregatesAllTypes()
    {
        // Gravity bag + Sharp reagent at edge = both GravityUnsupported + SharpOnEdge
        var grid = CreateGrid();
        var bag = new Bag(new BagDefinition("grav_bag", "Gravity Bag",
            new bool[,]
            {
                { true, true, true },
                { true, true, true },
                { true, true, true },
            },
            new[] { BagPropertyType.Gravity }));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        var violations = HighlightEngine.GetConstraintViolations(grid, sharp, new Vector2I(0, 0));

        // (0,0) is edge (Sharp violation) AND top row with no support (Gravity violation)
        Assert.True(violations.Count >= 2);
        Assert.Contains(violations, v => v.Type == ConstraintType.SharpOnEdge);
        Assert.Contains(violations, v => v.Type == ConstraintType.GravityUnsupported);
    }

    [Fact]
    public void GetConstraintViolations_NoViolations_ReturnsEmpty()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var reagent = new Reagent(ReagentDefinition.SingleCell);
        var violations = HighlightEngine.GetConstraintViolations(grid, reagent, new Vector2I(1, 1));

        Assert.Empty(violations);
    }

    // --- ComputeGhostConstraintHighlights tests ---

    [Fact]
    public void GhostConstraint_SharpOnEdge_ProducesGhostOverlayHighlights()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        var highlights = HighlightEngine.ComputeGhostConstraintHighlights(grid, sharp, new Vector2I(0, 0));

        Assert.False(highlights.IsEmpty);
        var ghostHighlights = highlights.GetByLayer(HighlightLayer.GhostOverlay);
        Assert.Single(ghostHighlights);

        var h = ghostHighlights[0];
        Assert.Equal(new Vector2I(0, 0), h.Position);
        Assert.Equal(HighlightLayer.GhostOverlay, h.Layer);
        Assert.Equal(HighlightSeverity.Danger, h.Severity);
        Assert.Equal(AspectIds.Sharp, h.AspectId);
    }

    [Fact]
    public void GhostConstraint_NoViolations_ReturnsEmpty()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        // Interior position — no Sharp edge violation
        var highlights = HighlightEngine.ComputeGhostConstraintHighlights(grid, sharp, new Vector2I(1, 1));

        Assert.True(highlights.IsEmpty);
    }

    [Fact]
    public void GhostConstraint_MultiCellSharp_OnlyViolatingCellsHighlighted()
    {
        // 5x5 bag so we can have a 2-cell Sharp with one cell on edge, one interior
        var grid = CreateGrid();
        var largeBagDef = new BagDefinition("large", "Large Bag", new bool[,]
        {
            { true, true, true, true, true },
            { true, true, true, true, true },
            { true, true, true, true, true },
            { true, true, true, true, true },
            { true, true, true, true, true },
        });
        var bag = new Bag(largeBagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        // Horizontal domino at (0,1) — cell (0,1) is left edge, cell (1,1) is interior
        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true, true } }));
        var highlights = HighlightEngine.ComputeGhostConstraintHighlights(grid, sharp, new Vector2I(0, 1));

        var ghostHighlights = highlights.GetByLayer(HighlightLayer.GhostOverlay);
        // Only the edge cell (0,1) should be highlighted, not the interior cell (1,1)
        Assert.Single(ghostHighlights);
        Assert.Equal(new Vector2I(0, 1), ghostHighlights[0].Position);
    }

    [Fact]
    public void GhostConstraint_FragileInGravity_NoGhostOverlayWarning()
    {
        // D.1: Fragile reagents allowed in Gravity bags — no ghost overlay warning
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var fragile = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));
        var highlights = HighlightEngine.ComputeGhostConstraintHighlights(grid, fragile, new Vector2I(1, 2));

        var ghostHighlights = highlights.GetByLayer(HighlightLayer.GhostOverlay);
        // No FragileInGravity warnings — may still have gravity unsupported warnings
        Assert.DoesNotContain(ghostHighlights, h => h.AspectId == AspectIds.Fragile);
    }

    [Fact]
    public void GhostConstraint_HasNoConstraintLayerHighlights()
    {
        var grid = CreateGrid();
        var bag = new Bag(BagDefinition.SmallPouch);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var sharp = new Reagent(CreateSharpReagentDef(new bool[,] { { true } }));
        var highlights = HighlightEngine.ComputeGhostConstraintHighlights(grid, sharp, new Vector2I(0, 0));

        // Should only have GhostOverlay layer, not Constraint layer
        Assert.Empty(highlights.GetByLayer(HighlightLayer.Constraint));
        Assert.NotEmpty(highlights.GetByLayer(HighlightLayer.GhostOverlay));
    }
}

public class StatusThresholdsTests
{
    [Fact]
    public void UnknownAspect_ReturnsInfo()
    {
        var thresholds = new StatusThresholds();
        Assert.Equal(HighlightSeverity.Info, thresholds.GetSeverity("nonexistent"));
    }

    [Fact]
    public void CurrentBelowThresholdMinus1_ReturnsSafe()
    {
        var thresholds = new StatusThresholds();
        thresholds.Set("hot", 1, 4);
        Assert.Equal(HighlightSeverity.Safe, thresholds.GetSeverity("hot"));
    }

    [Fact]
    public void CurrentAtThresholdMinus1_ReturnsWarning()
    {
        var thresholds = new StatusThresholds();
        thresholds.Set("hot", 3, 4);
        Assert.Equal(HighlightSeverity.Warning, thresholds.GetSeverity("hot"));
    }

    [Fact]
    public void CurrentAtThreshold_ReturnsDanger()
    {
        var thresholds = new StatusThresholds();
        thresholds.Set("hot", 4, 4);
        Assert.Equal(HighlightSeverity.Danger, thresholds.GetSeverity("hot"));
    }

    [Fact]
    public void CurrentAboveThreshold_ReturnsDanger()
    {
        var thresholds = new StatusThresholds();
        thresholds.Set("toxic", 5, 3);
        Assert.Equal(HighlightSeverity.Danger, thresholds.GetSeverity("toxic"));
    }

    [Fact]
    public void SetOverwritesPreviousValue()
    {
        var thresholds = new StatusThresholds();
        thresholds.Set("hot", 1, 4); // Safe
        Assert.Equal(HighlightSeverity.Safe, thresholds.GetSeverity("hot"));

        thresholds.Set("hot", 4, 4); // Now Danger
        Assert.Equal(HighlightSeverity.Danger, thresholds.GetSeverity("hot"));
    }

    [Fact]
    public void MultipleAspects_TrackIndependently()
    {
        var thresholds = new StatusThresholds();
        thresholds.Set("hot", 4, 4);    // Danger
        thresholds.Set("cold", 3, 4);   // Warning
        thresholds.Set("toxic", 1, 3);  // Safe

        Assert.Equal(HighlightSeverity.Danger, thresholds.GetSeverity("hot"));
        Assert.Equal(HighlightSeverity.Warning, thresholds.GetSeverity("cold"));
        Assert.Equal(HighlightSeverity.Safe, thresholds.GetSeverity("toxic"));
    }

    [Fact]
    public void ZeroCurrent_ZeroThreshold_ReturnsDanger()
    {
        var thresholds = new StatusThresholds();
        thresholds.Set("hot", 0, 0);
        Assert.Equal(HighlightSeverity.Danger, thresholds.GetSeverity("hot"));
    }

    [Fact]
    public void Threshold1_Current0_ReturnsWarning()
    {
        // threshold-1 = 0, current = 0 >= 0
        var thresholds = new StatusThresholds();
        thresholds.Set("hot", 0, 1);
        Assert.Equal(HighlightSeverity.Warning, thresholds.GetSeverity("hot"));
    }

    // --- ComputeViolationHighlights tests ---

    [Fact]
    public void ViolationHighlights_EmptyCells_ReturnsEmpty()
    {
        var violation = new ViolationEntry(
            ViolationType.VolatileExplosion,
            "test violation",
            HighlightSeverity.Danger,
            new List<Vector2I>(),
            AspectIds.Volatile);

        var highlights = HighlightEngine.ComputeViolationHighlights(violation);
        Assert.True(highlights.IsEmpty);
    }

    [Fact]
    public void ViolationHighlights_SingleCell_AllEdges()
    {
        var cells = new List<Vector2I> { new(2, 3) };
        var violation = new ViolationEntry(
            ViolationType.VolatileExplosion,
            "Explosion here",
            HighlightSeverity.Danger,
            cells,
            AspectIds.Volatile);

        var highlights = HighlightEngine.ComputeViolationHighlights(violation);
        var entries = highlights.GetByLayer(HighlightLayer.Interaction);

        Assert.Single(entries);
        Assert.Equal(new Vector2I(2, 3), entries[0].Position);
        Assert.Equal(CellEdge.All, entries[0].Edges);
        Assert.Equal(HighlightSeverity.Danger, entries[0].Severity);
        Assert.Equal(AspectIds.Volatile, entries[0].AspectId);
        Assert.Equal("Explosion here", entries[0].Reason);
    }

    [Fact]
    public void ViolationHighlights_AdjacentCells_InternalEdgesExcluded()
    {
        // Two horizontally adjacent cells — internal shared edge excluded
        var cells = new List<Vector2I> { new(1, 1), new(2, 1) };
        var violation = new ViolationEntry(
            ViolationType.BagTooHot,
            "Bag 1: too hot",
            HighlightSeverity.Warning,
            cells,
            AspectIds.Hot);

        var highlights = HighlightEngine.ComputeViolationHighlights(violation);
        var entries = highlights.GetByLayer(HighlightLayer.Interaction);

        Assert.Equal(2, entries.Count);

        // Left cell (1,1) should NOT have Right edge (shared with 2,1)
        var left = entries.First(e => e.Position.Equals(new Vector2I(1, 1)));
        Assert.False(left.Edges.HasFlag(CellEdge.Right));
        Assert.True(left.Edges.HasFlag(CellEdge.Left));

        // Right cell (2,1) should NOT have Left edge (shared with 1,1)
        var right = entries.First(e => e.Position.Equals(new Vector2I(2, 1)));
        Assert.False(right.Edges.HasFlag(CellEdge.Left));
        Assert.True(right.Edges.HasFlag(CellEdge.Right));
    }

    [Fact]
    public void ViolationHighlights_PreservesSeverityAndAspect()
    {
        var cells = new List<Vector2I> { new(0, 0) };
        var violation = new ViolationEntry(
            ViolationType.FragileCrushed,
            "Crushed",
            HighlightSeverity.Danger,
            cells,
            AspectIds.Fragile);

        var highlights = HighlightEngine.ComputeViolationHighlights(violation);
        var entry = highlights.GetByLayer(HighlightLayer.Interaction)[0];

        Assert.Equal(HighlightSeverity.Danger, entry.Severity);
        Assert.Equal(AspectIds.Fragile, entry.AspectId);
    }
}
