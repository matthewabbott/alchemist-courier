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
    public void FragileInGravityBag_ProducesWarningHighlight()
    {
        var grid = CreateGrid();
        var bag = new Bag(CreateGravityBagDef(3, 3));
        grid.PlaceBag(bag, new Vector2I(0, 0));

        var fragile = new Reagent(CreateFragileReagentDef(new bool[,] { { true } }));
        var highlights = HighlightEngine.ComputeConstraintHighlights(grid, fragile, new Vector2I(1, 2));

        Assert.False(highlights.IsEmpty);
        var constraintHighlights = highlights.GetByLayer(HighlightLayer.Constraint);
        Assert.Single(constraintHighlights);

        var h = constraintHighlights[0];
        Assert.Equal(new Vector2I(1, 2), h.Position);
        Assert.Equal(HighlightSeverity.Warning, h.Severity);
        Assert.Equal(AspectIds.Fragile, h.AspectId);
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
}
