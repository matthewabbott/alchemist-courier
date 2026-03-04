using System.Collections.Generic;
using System.Linq;
using Xunit;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Tests.Chemistry;

public class ChemistryEngineTests
{
    private static List<ReactionRule> AllRules => ReactionRuleDatabase.GetMvpRules();

    /// <summary>Helper: create a 1-cell reagent with the given aspects.</summary>
    private static Reagent MakeSingleCellReagent(string id, params Aspect[] aspects)
    {
        var def = new ReagentDefinition(id, id, new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { new AspectSet(aspects) });
        return new Reagent(def);
    }

    /// <summary>Helper: create a 2-cell horizontal reagent with one aspect set per cell.</summary>
    private static Reagent MakeDominoReagent(string id, AspectSet cell0, AspectSet cell1)
    {
        var def = new ReagentDefinition(id, id, new bool[,] { { true, true } },
            aspectsPerCell: new List<AspectSet> { cell0, cell1 });
        return new Reagent(def);
    }

    /// <summary>Helper: set up a grid with a bag and place reagents.</summary>
    private static MetaGrid SetupGrid(int width, int height, params (Reagent reagent, Vector2I pos)[] placements)
    {
        var grid = new MetaGrid(width, height);
        var bagDef = new BagDefinition("test", "Test Bag",
            CreateFilledShape(height, width));
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        foreach (var (reagent, pos) in placements)
            grid.PlaceReagent(reagent, pos);

        return grid;
    }

    /// <summary>Helper: set up a grid with two separate bags.</summary>
    private static MetaGrid SetupTwoBagGrid(
        int width, int height,
        BagDefinition bagDef1, Vector2I bagPos1,
        BagDefinition bagDef2, Vector2I bagPos2,
        params (Reagent reagent, Vector2I pos)[] placements)
    {
        var grid = new MetaGrid(width, height);
        var bag1 = new Bag(bagDef1);
        var bag2 = new Bag(bagDef2);
        grid.PlaceBag(bag1, bagPos1);
        grid.PlaceBag(bag2, bagPos2);

        foreach (var (reagent, pos) in placements)
            grid.PlaceReagent(reagent, pos);

        return grid;
    }

    private static bool[,] CreateFilledShape(int rows, int cols)
    {
        var shape = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                shape[r, c] = true;
        return shape;
    }

    // --- Empty grid ---

    [Fact]
    public void Evaluate_EmptyGrid_ReturnsNoReactions()
    {
        var grid = new MetaGrid(5, 5);
        var results = ChemistryEngine.Evaluate(grid, AllRules);
        Assert.Empty(results);
    }

    [Fact]
    public void Evaluate_SingleReagentNoThresholdMet_ReturnsNoReactions()
    {
        var reagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var grid = SetupGrid(5, 5, (reagent, new Vector2I(1, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // Hot magnitude 1 is below overheating threshold of 4
        Assert.Empty(results);
    }

    // --- Adjacent: Hot + Volatile = Explosion ---

    [Fact]
    public void Evaluate_HotAndVolatileAdjacent_ReturnsExplosion()
    {
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        // Place adjacent (horizontally)
        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(1, 1)),
            (volatileReagent, new Vector2I(2, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_HotAndVolatileSeparatedByGap_NoExplosion()
    {
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        // Place with a gap between them
        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)),
            (volatileReagent, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_HotAndVolatileDiagonal_NoExplosion()
    {
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        // Place diagonally (not cardinal adjacent)
        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)),
            (volatileReagent, new Vector2I(1, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_Explosion_AffectedCellsIncludeBothReagentCells()
    {
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(1, 1)),
            (volatileReagent, new Vector2I(2, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        var explosion = results.Single(r => r.Type == ReactionResultType.Explosion);

        Assert.Contains(new Vector2I(1, 1), explosion.AffectedCells);
        Assert.Contains(new Vector2I(2, 1), explosion.AffectedCells);
    }

    // --- Adjacent: Sharp + Fragile = Breakage ---

    [Fact]
    public void Evaluate_SharpAndFragileAdjacent_ReturnsBreakage()
    {
        var sharpReagent = MakeSingleCellReagent("sharp1", Aspect.Sharp());
        var fragileReagent = MakeSingleCellReagent("frag1", Aspect.Fragile());

        var grid = SetupGrid(5, 5,
            (sharpReagent, new Vector2I(1, 1)),
            (fragileReagent, new Vector2I(1, 2)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Breakage);
    }

    // --- SameBag: Hot + Cold = Neutralization ---

    [Fact]
    public void Evaluate_HotAndColdInSameBag_ReturnsNeutralization()
    {
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var coldReagent = MakeSingleCellReagent("cold1", Aspect.Cold());

        // Both in same bag, but not adjacent (diagonally separated)
        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)),
            (coldReagent, new Vector2I(2, 2)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Neutralization);
    }

    [Fact]
    public void Evaluate_HotAndColdInDifferentBags_NoNeutralization()
    {
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var coldReagent = MakeSingleCellReagent("cold1", Aspect.Cold());

        // Two separate bags
        var bag1Def = new BagDefinition("bag1", "Bag1", CreateFilledShape(2, 2));
        var bag2Def = new BagDefinition("bag2", "Bag2", CreateFilledShape(2, 2));

        var grid = SetupTwoBagGrid(6, 2,
            bag1Def, new Vector2I(0, 0),
            bag2Def, new Vector2I(3, 0),
            (hotReagent, new Vector2I(0, 0)),
            (coldReagent, new Vector2I(3, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Neutralization);
    }

    // --- SameBag: Toxic + Toxic = ToxicCloud (threshold 2) ---

    [Fact]
    public void Evaluate_TwoToxicInSameBag_ReturnsToxicCloud()
    {
        var toxic1 = MakeSingleCellReagent("tox1", Aspect.Toxic());
        var toxic2 = MakeSingleCellReagent("tox2", Aspect.Toxic());

        var grid = SetupGrid(5, 5,
            (toxic1, new Vector2I(0, 0)),
            (toxic2, new Vector2I(1, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.ToxicCloud);
    }

    [Fact]
    public void Evaluate_OneToxicInBag_NoToxicCloud()
    {
        var toxic1 = MakeSingleCellReagent("tox1", Aspect.Toxic());

        var grid = SetupGrid(5, 5,
            (toxic1, new Vector2I(0, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.ToxicCloud);
    }

    // --- WholeInventory: Cumulative overheating ---

    [Fact]
    public void Evaluate_CumulativeHotExceedsThreshold_ReturnsOverheating()
    {
        // 4 cells with Hot aspect = threshold of 4 met
        var hot1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var hot2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var hot3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var hot4 = MakeSingleCellReagent("h4", Aspect.Hot());

        var grid = SetupGrid(5, 5,
            (hot1, new Vector2I(0, 0)),
            (hot2, new Vector2I(1, 0)),
            (hot3, new Vector2I(2, 0)),
            (hot4, new Vector2I(3, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Overheating);
    }

    [Fact]
    public void Evaluate_CumulativeHotBelowThreshold_NoOverheating()
    {
        // 3 cells with Hot = below threshold of 4
        var hot1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var hot2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var hot3 = MakeSingleCellReagent("h3", Aspect.Hot());

        var grid = SetupGrid(5, 5,
            (hot1, new Vector2I(0, 0)),
            (hot2, new Vector2I(1, 0)),
            (hot3, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Overheating);
    }

    [Fact]
    public void Evaluate_HighMagnitudeHot_TriggersOverheating()
    {
        // One reagent with Hot magnitude 4 = meets threshold
        var hotReagent = MakeSingleCellReagent("h1", Aspect.Hot(4));

        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Overheating);
    }

    // --- WholeInventory: Cumulative poisoning ---

    [Fact]
    public void Evaluate_CumulativeToxicExceedsThreshold_ReturnsPoisoning()
    {
        // 3 cells with Toxic = threshold of 3 met
        var t1 = MakeSingleCellReagent("t1", Aspect.Toxic());
        var t2 = MakeSingleCellReagent("t2", Aspect.Toxic());
        var t3 = MakeSingleCellReagent("t3", Aspect.Toxic());

        var grid = SetupGrid(5, 5,
            (t1, new Vector2I(0, 0)),
            (t2, new Vector2I(1, 0)),
            (t3, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Poisoning);
    }

    // --- Mixed scenario: multiple reactions ---

    [Fact]
    public void Evaluate_MixedScenario_MultipleReactionsTriggered()
    {
        // Hot + Volatile adjacent → Explosion
        // 4x Hot cumulative → Overheating
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());
        var hot2 = MakeSingleCellReagent("hot2", Aspect.Hot());
        var hot3 = MakeSingleCellReagent("hot3", Aspect.Hot());
        var hot4 = MakeSingleCellReagent("hot4", Aspect.Hot());

        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)),
            (volatileReagent, new Vector2I(1, 0)),
            (hot2, new Vector2I(0, 1)),
            (hot3, new Vector2I(0, 2)),
            (hot4, new Vector2I(0, 3)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
        Assert.Contains(results, r => r.Type == ReactionResultType.Overheating);
    }

    // --- Dedup: adjacent pair only reported once ---

    [Fact]
    public void Evaluate_AdjacentPair_ReportedOnlyOnce()
    {
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(1, 1)),
            (volatileReagent, new Vector2I(2, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Single(results.Where(r => r.Type == ReactionResultType.Explosion));
    }

    // --- Multi-cell reagent adjacency ---

    [Fact]
    public void Evaluate_MultiCellReagentAdjacentToVolatile_Explosion()
    {
        // 2-cell hot domino next to a volatile cell
        var hotDomino = MakeDominoReagent("hot_dom",
            new AspectSet(Aspect.Hot()), new AspectSet(Aspect.Hot()));
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        // Hot domino at (0,0)-(1,0), volatile at (2,0) — adjacent to right cell of domino
        var grid = SetupGrid(5, 5,
            (hotDomino, new Vector2I(0, 0)),
            (volatileReagent, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
    }

    // --- Overheating magnitude tracks total ---

    [Fact]
    public void Evaluate_OverheatingResult_MagnitudeReflectsTotalHotCount()
    {
        var h1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var h2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var h3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var h4 = MakeSingleCellReagent("h4", Aspect.Hot());
        var h5 = MakeSingleCellReagent("h5", Aspect.Hot());

        var grid = SetupGrid(5, 5,
            (h1, new Vector2I(0, 0)),
            (h2, new Vector2I(1, 0)),
            (h3, new Vector2I(2, 0)),
            (h4, new Vector2I(3, 0)),
            (h5, new Vector2I(4, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        var overheating = results.Single(r => r.Type == ReactionResultType.Overheating);

        Assert.Equal(5f, overheating.Magnitude);
    }

    // --- SourceRule reference ---

    [Fact]
    public void Evaluate_ExplosionResult_SourceRuleIsExplosionRule()
    {
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)),
            (volatileReagent, new Vector2I(1, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        var explosion = results.Single(r => r.Type == ReactionResultType.Explosion);

        Assert.Equal("Explosion", explosion.SourceRule.Name);
    }

    // --- No rules = no reactions ---

    [Fact]
    public void Evaluate_EmptyRules_ReturnsNothing()
    {
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)),
            (volatileReagent, new Vector2I(1, 0)));

        var results = ChemistryEngine.Evaluate(grid, new List<ReactionRule>());

        Assert.Empty(results);
    }

    // --- Safe reagents produce no reactions ---

    [Fact]
    public void Evaluate_SafeReagents_NoReactions()
    {
        // Stable and Medicinal have no interaction rules
        var stable = MakeSingleCellReagent("s1", Aspect.Stable());
        var medicinal = MakeSingleCellReagent("m1", Aspect.Medicinal());

        var grid = SetupGrid(5, 5,
            (stable, new Vector2I(0, 0)),
            (medicinal, new Vector2I(1, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Empty(results);
    }

    // --- Hypothermia cumulative ---

    [Fact]
    public void Evaluate_CumulativeColdExceedsThreshold_ReturnsHypothermia()
    {
        var c1 = MakeSingleCellReagent("c1", Aspect.Cold());
        var c2 = MakeSingleCellReagent("c2", Aspect.Cold());
        var c3 = MakeSingleCellReagent("c3", Aspect.Cold());
        var c4 = MakeSingleCellReagent("c4", Aspect.Cold());

        var grid = SetupGrid(5, 5,
            (c1, new Vector2I(0, 0)),
            (c2, new Vector2I(1, 0)),
            (c3, new Vector2I(2, 0)),
            (c4, new Vector2I(3, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Hypothermia);
    }
}
