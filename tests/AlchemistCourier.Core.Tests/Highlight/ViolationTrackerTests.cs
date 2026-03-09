using System.Collections.Generic;
using System.Linq;
using Xunit;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Highlight;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Tests.Highlight;

public class ViolationTrackerTests
{
    private static List<ReactionRule> AllRules => ReactionRuleDatabase.GetMvpRules();

    private static Reagent MakeSingleCellReagent(string id, params Aspect[] aspects)
    {
        var def = new ReagentDefinition(id, id, new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { new AspectSet(aspects) });
        return new Reagent(def);
    }

    private static bool[,] CreateFilledShape(int rows, int cols)
    {
        var shape = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                shape[r, c] = true;
        return shape;
    }

    private static MetaGrid SetupGrid(int width, int height,
        params (Reagent reagent, Vector2I pos)[] placements)
    {
        var grid = new MetaGrid(width, height);
        var bagDef = new BagDefinition("test", "Test Bag", CreateFilledShape(height, width));
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        foreach (var (reagent, pos) in placements)
            grid.PlaceReagent(reagent, pos);

        return grid;
    }

    // --- Empty / Clean state ---

    [Fact]
    public void ComputeAll_EmptyGrid_ReturnsNoViolations()
    {
        var grid = SetupGrid(3, 3);
        var violations = ViolationTracker.ComputeAll(grid, AllRules);
        Assert.Empty(violations);
    }

    [Fact]
    public void ComputeAll_InertReagents_ReturnsNoViolations()
    {
        var r1 = MakeSingleCellReagent("r1");
        var r2 = MakeSingleCellReagent("r2");

        var grid = SetupGrid(3, 1,
            (r1, new Vector2I(0, 0)),
            (r2, new Vector2I(2, 0)));

        var violations = ViolationTracker.ComputeAll(grid, AllRules);
        Assert.Empty(violations);
    }

    // --- Bag balance violations (I.2) ---

    [Fact]
    public void ComputeAll_BagTooHot_ReturnsBalanceViolation()
    {
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var inert = MakeSingleCellReagent("inert1");

        var grid = SetupGrid(3, 1,
            (hot, new Vector2I(0, 0)),
            (inert, new Vector2I(2, 0)));

        var violations = ViolationTracker.ComputeAll(grid, AllRules);
        Assert.Contains(violations, v => v.Type == ViolationType.BagTooHot);
        var hotViolation = violations.First(v => v.Type == ViolationType.BagTooHot);
        Assert.Equal(AspectIds.Hot, hotViolation.AspectId);
        Assert.Contains("too hot", hotViolation.Description);
    }

    [Fact]
    public void ComputeAll_BagTooCold_ReturnsBalanceViolation()
    {
        var cold = MakeSingleCellReagent("cold1", Aspect.Cold());
        var inert = MakeSingleCellReagent("inert1");

        var grid = SetupGrid(3, 1,
            (cold, new Vector2I(0, 0)),
            (inert, new Vector2I(2, 0)));

        var violations = ViolationTracker.ComputeAll(grid, AllRules);
        Assert.Contains(violations, v => v.Type == ViolationType.BagTooCold);
        var coldViolation = violations.First(v => v.Type == ViolationType.BagTooCold);
        Assert.Equal(AspectIds.Cold, coldViolation.AspectId);
        Assert.Contains("too cold", coldViolation.Description);
    }

    [Fact]
    public void ComputeAll_BalancedBag_NoBalanceViolation()
    {
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var cold = MakeSingleCellReagent("cold1", Aspect.Cold());

        var grid = SetupGrid(3, 1,
            (hot, new Vector2I(0, 0)),
            (cold, new Vector2I(2, 0)));

        var violations = ViolationTracker.ComputeAll(grid, AllRules);
        Assert.DoesNotContain(violations, v =>
            v.Type == ViolationType.BagTooHot || v.Type == ViolationType.BagTooCold);
    }

    // --- Volatile explosion ---

    [Fact]
    public void ComputeAll_VolatileNextToHot_ReturnsExplosionViolation()
    {
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatile_ = MakeSingleCellReagent("vol1", Aspect.FromId(AspectIds.Volatile));

        var grid = SetupGrid(3, 1,
            (hot, new Vector2I(0, 0)),
            (volatile_, new Vector2I(1, 0)));

        var violations = ViolationTracker.ComputeAll(grid, AllRules);
        Assert.Contains(violations, v => v.Type == ViolationType.VolatileExplosion);
    }

    [Fact]
    public void ComputeAll_VolatileNotAdjacentToHot_NoExplosion()
    {
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatile_ = MakeSingleCellReagent("vol1", Aspect.FromId(AspectIds.Volatile));

        var grid = SetupGrid(5, 1,
            (hot, new Vector2I(0, 0)),
            (volatile_, new Vector2I(4, 0)));

        var violations = ViolationTracker.ComputeAll(grid, AllRules);
        Assert.DoesNotContain(violations, v => v.Type == ViolationType.VolatileExplosion);
    }

    // --- Fragile piercing ---

    [Fact]
    public void ComputeAll_SharpNextToFragile_ReturnsPiercedViolation()
    {
        // Sharp on non-edge cell requires a larger bag
        var sharp = MakeSingleCellReagent("sharp1", Aspect.FromId(AspectIds.Sharp));
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());

        // Use a reinforced bag so Sharp can be placed anywhere
        var bagDef = new BagDefinition("reinforced", "Reinforced",
            CreateFilledShape(1, 3),
            new[] { BagPropertyType.Reinforced });
        var grid = new MetaGrid(3, 1);
        grid.PlaceBag(new Bag(bagDef), new Vector2I(0, 0));
        grid.PlaceReagent(sharp, new Vector2I(1, 0));   // Center (not edge)
        grid.PlaceReagent(fragile, new Vector2I(2, 0)); // Adjacent

        var violations = ViolationTracker.ComputeAll(grid, AllRules);
        Assert.Contains(violations, v => v.Type == ViolationType.FragilePierced);
    }

    // --- Thermal fracture ---

    [Fact]
    public void ComputeAll_FragileBetweenHotAndCold_ReturnsThermalFracture()
    {
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());
        var cold = MakeSingleCellReagent("cold1", Aspect.Cold());

        var grid = SetupGrid(3, 1,
            (hot, new Vector2I(0, 0)),
            (fragile, new Vector2I(1, 0)),
            (cold, new Vector2I(2, 0)));

        var violations = ViolationTracker.ComputeAll(grid, AllRules);
        Assert.Contains(violations, v => v.Type == ViolationType.FragileThermalFracture);
    }

    // --- Crushing ---

    [Fact]
    public void ComputeAll_HeavyOnFragile_ReturnsCrushing()
    {
        var heavy = MakeSingleCellReagent("heavy1", Aspect.Heavy());
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());

        var grid = SetupGrid(1, 2,
            (heavy, new Vector2I(0, 0)),
            (fragile, new Vector2I(0, 1)));

        var violations = ViolationTracker.ComputeAll(grid, AllRules);
        Assert.Contains(violations, v => v.Type == ViolationType.FragileCrushed);
    }

    // --- Cumulative overheating ---

    [Fact]
    public void ComputeAll_CumulativeHotExceedsThreshold_ReturnsOverheating()
    {
        // Overheating threshold = 4. Place 5 hot reagents.
        var h1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var h2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var h3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var h4 = MakeSingleCellReagent("h4", Aspect.Hot());
        var h5 = MakeSingleCellReagent("h5", Aspect.Hot());

        var grid = SetupGrid(5, 1,
            (h1, new Vector2I(0, 0)),
            (h2, new Vector2I(1, 0)),
            (h3, new Vector2I(2, 0)),
            (h4, new Vector2I(3, 0)),
            (h5, new Vector2I(4, 0)));

        var violations = ViolationTracker.ComputeAll(grid, AllRules);
        Assert.Contains(violations, v => v.Type == ViolationType.Overheating);
    }

    [Fact]
    public void ComputeAll_CumulativeHotBelowThreshold_NoOverheating()
    {
        // Threshold = 4. Place 3 hot reagents.
        var h1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var h2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var h3 = MakeSingleCellReagent("h3", Aspect.Hot());

        var grid = SetupGrid(5, 1,
            (h1, new Vector2I(0, 0)),
            (h2, new Vector2I(1, 0)),
            (h3, new Vector2I(2, 0)));

        var violations = ViolationTracker.ComputeAll(grid, AllRules);
        Assert.DoesNotContain(violations, v => v.Type == ViolationType.Overheating);
    }

    // --- Multiple violations ---

    [Fact]
    public void ComputeAll_MultipleViolationTypes_AllReported()
    {
        // Hot-only bag (balance violation) + volatile+hot explosion
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatile_ = MakeSingleCellReagent("vol1", Aspect.FromId(AspectIds.Volatile));

        var grid = SetupGrid(3, 1,
            (hot, new Vector2I(0, 0)),
            (volatile_, new Vector2I(1, 0)));

        var violations = ViolationTracker.ComputeAll(grid, AllRules);

        // Should have both balance violation (hot without cold) AND explosion
        Assert.Contains(violations, v => v.Type == ViolationType.BagTooHot);
        Assert.Contains(violations, v => v.Type == ViolationType.VolatileExplosion);
    }

    // --- Violation entry properties ---

    [Fact]
    public void ViolationEntry_HasCorrectSeverity()
    {
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatile_ = MakeSingleCellReagent("vol1", Aspect.FromId(AspectIds.Volatile));

        var grid = SetupGrid(2, 1,
            (hot, new Vector2I(0, 0)),
            (volatile_, new Vector2I(1, 0)));

        var violations = ViolationTracker.ComputeAll(grid, AllRules);
        var explosion = violations.First(v => v.Type == ViolationType.VolatileExplosion);
        Assert.Equal(HighlightSeverity.Danger, explosion.Severity);

        var balance = violations.First(v => v.Type == ViolationType.BagTooHot);
        Assert.Equal(HighlightSeverity.Warning, balance.Severity);
    }

    [Fact]
    public void ViolationEntry_AffectedCellsNotEmpty()
    {
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatile_ = MakeSingleCellReagent("vol1", Aspect.FromId(AspectIds.Volatile));

        var grid = SetupGrid(2, 1,
            (hot, new Vector2I(0, 0)),
            (volatile_, new Vector2I(1, 0)));

        var violations = ViolationTracker.ComputeAll(grid, AllRules);
        foreach (var v in violations)
        {
            Assert.NotEmpty(v.AffectedCells);
        }
    }
}
