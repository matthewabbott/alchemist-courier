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

    // --- Net-hot: Volatile with Cold as heat shield ---

    [Fact]
    public void Evaluate_VolatileWithHotAndColdBalanced_NoExplosion()
    {
        // Hot(1) + Cold(1) adjacent to Volatile → net_hot = 1 - 1 = 0 → no explosion
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var coldReagent = MakeSingleCellReagent("cold1", Aspect.Cold());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        // Layout: [Hot] [Volatile] [Cold] — both Hot and Cold adjacent to Volatile
        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)),
            (volatileReagent, new Vector2I(1, 0)),
            (coldReagent, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_VolatileWithMoreHotThanCold_Explodes()
    {
        // Hot(1) + Hot(1) + Cold(1) adjacent to Volatile → net_hot = 2 - 1 = 1 → explosion
        var hot1 = MakeSingleCellReagent("hot1", Aspect.Hot());
        var hot2 = MakeSingleCellReagent("hot2", Aspect.Hot());
        var coldReagent = MakeSingleCellReagent("cold1", Aspect.Cold());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        // Layout:       [Hot2]
        //         [Hot1] [Volatile] [Cold]
        var grid = SetupGrid(5, 5,
            (hot1, new Vector2I(0, 1)),
            (volatileReagent, new Vector2I(1, 1)),
            (coldReagent, new Vector2I(2, 1)),
            (hot2, new Vector2I(1, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_ColdAsHeatShield_PreventsExplosion()
    {
        // Cold adjacent to Volatile neutralizes 1 Hot → net_hot = 0 → safe
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var coldReagent = MakeSingleCellReagent("cold1", Aspect.Cold());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        // Layout: [Hot] [Volatile] [Cold]
        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)),
            (volatileReagent, new Vector2I(1, 0)),
            (coldReagent, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_VolatileWithNoHotNeighbors_NoExplosion()
    {
        // Volatile alone — no Hot neighbors → net_hot = 0 → safe
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        var grid = SetupGrid(5, 5,
            (volatileReagent, new Vector2I(2, 2)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_VolatileNetHot_StableRaisesThreshold()
    {
        // Volatile with Hot(1) adjacent + Stable(1) adjacent → net_hot=1, effectiveThreshold=0+1=1
        // 1 > 1 is false → suppressed
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());
        var stableReagent = MakeSingleCellReagent("stb1", Aspect.Stable());

        // Layout: [Hot] [Volatile] [Stable]
        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)),
            (volatileReagent, new Vector2I(1, 0)),
            (stableReagent, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_VolatileNetHot_NullSuppressedHotDoesNotContribute()
    {
        // Hot in Null bag adjacent to Volatile → Hot is suppressed → net_hot = 0
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 2),
            new[] { BagPropertyType.Null });
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 2));

        var grid = SetupNullBagGrid(4, 1,
            nullBagDef, new Vector2I(0, 0),
            normalBagDef, new Vector2I(2, 0),
            (hotReagent, new Vector2I(1, 0)),      // Hot in Null bag
            (volatileReagent, new Vector2I(2, 0))); // Volatile in Normal bag

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_VolatileNetHot_HighMagnitudeHotOvercomesCold()
    {
        // Hot(3) adjacent + Cold(1) adjacent to Volatile → net_hot = 3 - 1 = 2 → explosion
        var def = new ReagentDefinition("hot1", "hot1", new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { new AspectSet(Aspect.Hot(3)) });
        var hotReagent = new Reagent(def);
        var coldReagent = MakeSingleCellReagent("cold1", Aspect.Cold());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        // Layout:        [Cold]
        //         [Hot3] [Volatile]
        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 1)),
            (volatileReagent, new Vector2I(1, 1)),
            (coldReagent, new Vector2I(1, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
        var explosion = results.Single(r => r.Type == ReactionResultType.Explosion);
        Assert.Equal(2f, explosion.Magnitude);
    }

    [Fact]
    public void Evaluate_VolatileNetHot_SameReagentHotDoesNotSelfExplode()
    {
        // A reagent with both Hot and Volatile cells adjacent should NOT self-explode.
        // Only Hot cells from OTHER reagents contribute to the net-hot calculation.
        var blastJelly = MakeDominoReagent("blast",
            new AspectSet(Aspect.Hot()),
            new AspectSet(Aspect.Volatile()));

        var grid = SetupGrid(5, 5,
            (blastJelly, new Vector2I(1, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Explosion);
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

    // =====================================================
    // EvaluateProspective tests
    // =====================================================

    [Fact]
    public void EvaluateProspective_SafeSpot_ReturnsEmpty()
    {
        var stable = MakeSingleCellReagent("s1", Aspect.Stable());
        var prospective = MakeSingleCellReagent("s2", Aspect.Stable());

        var grid = SetupGrid(5, 5, (stable, new Vector2I(0, 0)));

        var results = ChemistryEngine.EvaluateProspective(
            grid, prospective, new Vector2I(2, 2), AllRules);

        Assert.Empty(results);
    }

    [Fact]
    public void EvaluateProspective_AdjacentToVolatile_ReturnsExplosion()
    {
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());

        var grid = SetupGrid(5, 5, (volatileReagent, new Vector2I(1, 1)));

        var results = ChemistryEngine.EvaluateProspective(
            grid, hotReagent, new Vector2I(2, 1), AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void EvaluateProspective_GridUnchangedAfterCall()
    {
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());

        var grid = SetupGrid(5, 5, (volatileReagent, new Vector2I(1, 1)));

        // Record state before
        int reagentCountBefore = grid.PlacedReagents.Count;

        ChemistryEngine.EvaluateProspective(
            grid, hotReagent, new Vector2I(2, 1), AllRules);

        // Grid should be unchanged
        Assert.Equal(reagentCountBefore, grid.PlacedReagents.Count);
        Assert.DoesNotContain(hotReagent, grid.PlacedReagents);
        Assert.Null(grid.GetReagentAtCell(new Vector2I(2, 1)));
        Assert.False(hotReagent.IsPlaced);
    }

    [Fact]
    public void EvaluateProspective_InvalidPosition_ReturnsEmpty()
    {
        var reagent = MakeSingleCellReagent("r1", Aspect.Hot());

        // Grid with no bags — no valid placement positions
        var grid = new MetaGrid(5, 5);

        var results = ChemistryEngine.EvaluateProspective(
            grid, reagent, new Vector2I(0, 0), AllRules);

        Assert.Empty(results);
    }

    [Fact]
    public void EvaluateProspective_CumulativeWouldExceedThreshold_ReturnsReaction()
    {
        // 3 hot already placed; prospective 4th would trigger overheating (threshold 4)
        var h1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var h2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var h3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var h4 = MakeSingleCellReagent("h4", Aspect.Hot());

        var grid = SetupGrid(5, 5,
            (h1, new Vector2I(0, 0)),
            (h2, new Vector2I(1, 0)),
            (h3, new Vector2I(2, 0)));

        // Before: no overheating
        var before = ChemistryEngine.Evaluate(grid, AllRules);
        Assert.DoesNotContain(before, r => r.Type == ReactionResultType.Overheating);

        // Prospective: would cause overheating
        var results = ChemistryEngine.EvaluateProspective(
            grid, h4, new Vector2I(3, 0), AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Overheating);

        // After: still no overheating (grid unchanged)
        var after = ChemistryEngine.Evaluate(grid, AllRules);
        Assert.DoesNotContain(after, r => r.Type == ReactionResultType.Overheating);
    }

    [Fact]
    public void EvaluateProspective_OccupiedPosition_ReturnsEmpty()
    {
        var existing = MakeSingleCellReagent("r1", Aspect.Hot());
        var prospective = MakeSingleCellReagent("r2", Aspect.Volatile());

        var grid = SetupGrid(5, 5, (existing, new Vector2I(1, 1)));

        // Can't place on an already-occupied cell
        var results = ChemistryEngine.EvaluateProspective(
            grid, prospective, new Vector2I(1, 1), AllRules);

        Assert.Empty(results);
    }

    // =====================================================
    // Stable buffering tests
    // =====================================================

    [Fact]
    public void Evaluate_StableAdjacentToHotVolatilePair_SuppressesExplosion()
    {
        // Layout: [Hot] [Volatile] [Stable]  — Stable is adjacent to Volatile
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());
        var stableReagent = MakeSingleCellReagent("stb1", Aspect.Stable());

        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)),
            (volatileReagent, new Vector2I(1, 0)),
            (stableReagent, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_StableNotAdjacentToReactingPair_DoesNotSuppress()
    {
        // Layout: [Hot] [Volatile]  ...  [Stable] — Stable is far away
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());
        var stableReagent = MakeSingleCellReagent("stb1", Aspect.Stable());

        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)),
            (volatileReagent, new Vector2I(1, 0)),
            (stableReagent, new Vector2I(4, 4)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_MultipleStableCellsStack()
    {
        // Two Stable reagents adjacent to Sharp+Fragile pair — magnitudes stack
        var sharpReagent = MakeSingleCellReagent("sharp1", Aspect.Sharp());
        var fragileReagent = MakeSingleCellReagent("frag1", Aspect.Fragile());
        var stable1 = MakeSingleCellReagent("stb1", Aspect.Stable());
        var stable2 = MakeSingleCellReagent("stb2", Aspect.Stable());

        // Layout (row 0): [Stable1] [Sharp] [Fragile] [Stable2]
        var grid = SetupGrid(5, 5,
            (stable1, new Vector2I(0, 0)),
            (sharpReagent, new Vector2I(1, 0)),
            (fragileReagent, new Vector2I(2, 0)),
            (stable2, new Vector2I(3, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // Both Stable cells contribute — stableBuffer=2 > threshold=0
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Breakage);
    }

    [Fact]
    public void Evaluate_StableAdjacentToOneSideOfPair_SuppressesExplosion()
    {
        // Stable is adjacent only to the Hot cell, not the Volatile cell
        // Layout (col): [Stable] at (0,1), [Hot] at (1,1), [Volatile] at (2,1)
        var stableReagent = MakeSingleCellReagent("stb1", Aspect.Stable());
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        var grid = SetupGrid(5, 5,
            (stableReagent, new Vector2I(0, 1)),
            (hotReagent, new Vector2I(1, 1)),
            (volatileReagent, new Vector2I(2, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_StableWithHigherMagnitude_StillSuppresses()
    {
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());
        // Stable with magnitude 3
        var def = new ReagentDefinition("stb1", "stb1", new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { new AspectSet(Aspect.Stable(3)) });
        var stableReagent = new Reagent(def);

        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(1, 1)),
            (volatileReagent, new Vector2I(2, 1)),
            (stableReagent, new Vector2I(3, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_StableBuffersCumulativeOverheating()
    {
        // 4 Hot cells meet overheating threshold (4), but Stable adjacent raises threshold to 5
        var h1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var h2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var h3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var h4 = MakeSingleCellReagent("h4", Aspect.Hot());
        var stable = MakeSingleCellReagent("stb1", Aspect.Stable());

        // Stable adjacent to one of the hot cells
        var grid = SetupGrid(5, 5,
            (h1, new Vector2I(0, 0)),
            (h2, new Vector2I(1, 0)),
            (h3, new Vector2I(2, 0)),
            (h4, new Vector2I(3, 0)),
            (stable, new Vector2I(0, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Overheating);
    }

    [Fact]
    public void Evaluate_StableDoesNotAffectOwnReaction()
    {
        // Stable + Medicinal adjacent should still produce no reaction
        // (they don't have reaction rules, so this verifies stable doesn't break anything)
        var stable = MakeSingleCellReagent("stb1", Aspect.Stable());
        var medicinal = MakeSingleCellReagent("med1", Aspect.Medicinal());

        var grid = SetupGrid(5, 5,
            (stable, new Vector2I(0, 0)),
            (medicinal, new Vector2I(1, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Empty(results);
    }

    // =====================================================
    // Stable buffering boundary tests
    // =====================================================

    [Fact]
    public void Evaluate_StableBuffer0_AdjacentExplosionStillFires()
    {
        // No stable present — stableBuffer=0 has no effect
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(1, 1)),
            (volatileReagent, new Vector2I(2, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_StableBuffer0_SameBagNeutralizationStillFires()
    {
        // No stable present — SameBag distinct-pair fires normally
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var coldReagent = MakeSingleCellReagent("cold1", Aspect.Cold());

        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)),
            (coldReagent, new Vector2I(2, 2)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Neutralization);
    }

    [Fact]
    public void Evaluate_StableBufferSuppressesSameBagNeutralization()
    {
        // Stable adjacent to Hot+Cold pair in same bag should suppress neutralization
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var coldReagent = MakeSingleCellReagent("cold1", Aspect.Cold());
        var stableReagent = MakeSingleCellReagent("stb1", Aspect.Stable());

        // Layout: [Hot] [Stable] [Cold] — Stable adjacent to both
        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)),
            (stableReagent, new Vector2I(1, 0)),
            (coldReagent, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Neutralization);
    }

    [Fact]
    public void Evaluate_StableBufferEqualToCount_PreventsCumulativeReaction()
    {
        // 3 Toxic reagents (threshold=3) + Stable(3) adjacent → effectiveThreshold=6 → no poisoning
        var t1 = MakeSingleCellReagent("t1", Aspect.Toxic());
        var t2 = MakeSingleCellReagent("t2", Aspect.Toxic());
        var t3 = MakeSingleCellReagent("t3", Aspect.Toxic());
        var def = new ReagentDefinition("stb1", "stb1", new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { new AspectSet(Aspect.Stable(3)) });
        var stableReagent = new Reagent(def);

        // Stable adjacent to t1
        var grid = SetupGrid(5, 5,
            (t1, new Vector2I(0, 0)),
            (t2, new Vector2I(1, 0)),
            (t3, new Vector2I(2, 0)),
            (stableReagent, new Vector2I(0, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // effectiveThreshold = 3 + 3 = 6, totalMagnitude = 3, 3 < 6 → no poisoning
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Poisoning);
    }

    [Fact]
    public void Evaluate_StableRaisesThresholdByExactMagnitude_CumulativeBoundary()
    {
        // 5 Hot reagents (magnitude=5) + Stable(1) adjacent → effectiveThreshold=4+1=5
        // 5 >= 5 → still fires (stable only raised threshold by 1, not enough)
        var h1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var h2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var h3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var h4 = MakeSingleCellReagent("h4", Aspect.Hot());
        var h5 = MakeSingleCellReagent("h5", Aspect.Hot());
        var stable = MakeSingleCellReagent("stb1", Aspect.Stable());

        // Stable adjacent to h1
        var grid = SetupGrid(6, 5,
            (h1, new Vector2I(0, 0)),
            (h2, new Vector2I(1, 0)),
            (h3, new Vector2I(2, 0)),
            (h4, new Vector2I(3, 0)),
            (h5, new Vector2I(4, 0)),
            (stable, new Vector2I(0, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // effectiveThreshold = 4 + 1 = 5, totalMagnitude = 5, 5 >= 5 → fires
        Assert.Contains(results, r => r.Type == ReactionResultType.Overheating);
    }

    [Fact]
    public void Evaluate_StableRaisesThresholdByExactMagnitude_CumulativeJustSuppressed()
    {
        // 4 Hot reagents (magnitude=4) + Stable(1) adjacent → effectiveThreshold=4+1=5
        // 4 < 5 → suppressed (stable raised threshold just above the count)
        var h1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var h2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var h3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var h4 = MakeSingleCellReagent("h4", Aspect.Hot());
        var stable = MakeSingleCellReagent("stb1", Aspect.Stable());

        // Stable adjacent to h1
        var grid = SetupGrid(5, 5,
            (h1, new Vector2I(0, 0)),
            (h2, new Vector2I(1, 0)),
            (h3, new Vector2I(2, 0)),
            (h4, new Vector2I(3, 0)),
            (stable, new Vector2I(0, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // effectiveThreshold = 4 + 1 = 5, totalMagnitude = 4, 4 < 5 → no overheating
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Overheating);
    }

    // =====================================================
    // Null bag aspect suppression tests
    // =====================================================

    /// <summary>Helper: set up a grid with a Null bag and a normal bag side by side.</summary>
    private static MetaGrid SetupNullBagGrid(
        int width, int height,
        BagDefinition nullBagDef, Vector2I nullBagPos,
        BagDefinition normalBagDef, Vector2I normalBagPos,
        params (Reagent reagent, Vector2I pos)[] placements)
    {
        var grid = new MetaGrid(width, height);
        var nullBag = new Bag(nullBagDef);
        var normalBag = new Bag(normalBagDef);
        grid.PlaceBag(nullBag, nullBagPos);
        grid.PlaceBag(normalBag, normalBagPos);

        foreach (var (reagent, pos) in placements)
            grid.PlaceReagent(reagent, pos);

        return grid;
    }

    [Fact]
    public void Evaluate_ReagentInNullBag_AdjacentExplosionSuppressed()
    {
        // Hot in Null bag, Volatile in normal bag — adjacent but Hot is suppressed
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 3),
            new[] { BagPropertyType.Null });
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 3));

        // Null bag at (0,0), normal bag at (3,0) — adjacent at boundary
        var grid = SetupNullBagGrid(6, 1,
            nullBagDef, new Vector2I(0, 0),
            normalBagDef, new Vector2I(3, 0),
            (hotReagent, new Vector2I(2, 0)),     // Hot in Null bag, rightmost cell
            (volatileReagent, new Vector2I(3, 0))); // Volatile in normal bag, leftmost cell

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // Hot is suppressed by Null bag — no explosion
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_ReagentInNormalBag_AdjacentExplosionNotSuppressed()
    {
        // Both in normal bags — explosion should still fire
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        var bag1Def = new BagDefinition("bag1", "Bag1", CreateFilledShape(1, 3));
        var bag2Def = new BagDefinition("bag2", "Bag2", CreateFilledShape(1, 3));

        var grid = SetupTwoBagGrid(6, 1,
            bag1Def, new Vector2I(0, 0),
            bag2Def, new Vector2I(3, 0),
            (hotReagent, new Vector2I(2, 0)),
            (volatileReagent, new Vector2I(3, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_SpanningReagent_NullBagCellsSuppressed_NormalBagCellsActive()
    {
        // A 2-cell domino spans Null bag and normal bag.
        // Cell in Null bag is suppressed, cell in normal bag retains aspects.
        var hotDomino = MakeDominoReagent("hot_dom",
            new AspectSet(Aspect.Hot()), new AspectSet(Aspect.Hot()));
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());

        // Null bag: cols 0-1, Normal bag: cols 2-3
        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 2),
            new[] { BagPropertyType.Null });
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 2));

        var grid = SetupNullBagGrid(4, 1,
            nullBagDef, new Vector2I(0, 0),
            normalBagDef, new Vector2I(2, 0),
            (hotDomino, new Vector2I(1, 0)),       // Spans: (1,0) in Null, (2,0) in Normal
            (volatileReagent, new Vector2I(3, 0))); // Adjacent to Hot at (2,0) in Normal

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // Hot at (2,0) is in normal bag — still active — should trigger explosion with Volatile
        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_NullBag_SameBagNeutralizationSuppressed()
    {
        // Hot and Cold in same Null bag — no neutralization because aspects are suppressed
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var coldReagent = MakeSingleCellReagent("cold1", Aspect.Cold());

        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(2, 3),
            new[] { BagPropertyType.Null });

        var grid = new MetaGrid(3, 2);
        var nullBag = new Bag(nullBagDef);
        grid.PlaceBag(nullBag, new Vector2I(0, 0));
        grid.PlaceReagent(hotReagent, new Vector2I(0, 0));
        grid.PlaceReagent(coldReagent, new Vector2I(2, 0));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Neutralization);
    }

    [Fact]
    public void Evaluate_NullBag_CumulativeOverheatingSuppressed()
    {
        // 4 Hot reagents in a Null bag — cumulative threshold should not be reached
        var h1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var h2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var h3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var h4 = MakeSingleCellReagent("h4", Aspect.Hot());

        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 5),
            new[] { BagPropertyType.Null });

        var grid = new MetaGrid(5, 1);
        var nullBag = new Bag(nullBagDef);
        grid.PlaceBag(nullBag, new Vector2I(0, 0));
        grid.PlaceReagent(h1, new Vector2I(0, 0));
        grid.PlaceReagent(h2, new Vector2I(1, 0));
        grid.PlaceReagent(h3, new Vector2I(2, 0));
        grid.PlaceReagent(h4, new Vector2I(3, 0));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Overheating);
    }

    [Fact]
    public void Evaluate_NullBag_ToxicCloudSuppressed()
    {
        // 2 Toxic reagents in a Null bag — toxic cloud should not trigger
        var t1 = MakeSingleCellReagent("t1", Aspect.Toxic());
        var t2 = MakeSingleCellReagent("t2", Aspect.Toxic());

        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 3),
            new[] { BagPropertyType.Null });

        var grid = new MetaGrid(3, 1);
        var nullBag = new Bag(nullBagDef);
        grid.PlaceBag(nullBag, new Vector2I(0, 0));
        grid.PlaceReagent(t1, new Vector2I(0, 0));
        grid.PlaceReagent(t2, new Vector2I(1, 0));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.ToxicCloud);
    }

    [Fact]
    public void Evaluate_NullBag_StableBufferingAlsoSuppressed()
    {
        // Stable in Null bag adjacent to Hot+Volatile in normal bag — Stable is suppressed,
        // so it should NOT buffer the explosion
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var volatileReagent = MakeSingleCellReagent("vol1", Aspect.Volatile());
        var stableReagent = MakeSingleCellReagent("stb1", Aspect.Stable());

        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 1),
            new[] { BagPropertyType.Null });
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 3));

        // Null bag at (0,0) with Stable, Normal bag at (1,0) with Hot at (1,0) and Volatile at (2,0)
        var grid = SetupNullBagGrid(4, 1,
            nullBagDef, new Vector2I(0, 0),
            normalBagDef, new Vector2I(1, 0),
            (stableReagent, new Vector2I(0, 0)),
            (hotReagent, new Vector2I(1, 0)),
            (volatileReagent, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // Stable is suppressed — explosion should still fire
        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_MixedBags_CumulativeSkipsNullBagCells()
    {
        // 3 Hot in normal bag + 1 Hot in Null bag — only 3 count, below threshold of 4
        var h1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var h2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var h3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var h4Null = MakeSingleCellReagent("h4", Aspect.Hot());

        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 4));
        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 1),
            new[] { BagPropertyType.Null });

        var grid = new MetaGrid(5, 1);
        var normalBag = new Bag(normalBagDef);
        var nullBag = new Bag(nullBagDef);
        grid.PlaceBag(normalBag, new Vector2I(0, 0));
        grid.PlaceBag(nullBag, new Vector2I(4, 0));

        grid.PlaceReagent(h1, new Vector2I(0, 0));
        grid.PlaceReagent(h2, new Vector2I(1, 0));
        grid.PlaceReagent(h3, new Vector2I(2, 0));
        grid.PlaceReagent(h4Null, new Vector2I(4, 0)); // In Null bag

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // Only 3 Hot counted (h4 suppressed) — below threshold of 4
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Overheating);
    }

    // --- Locked Reagent Aspect Activity ---

    [Fact]
    public void Evaluate_LockedReagentAspectsAreActive()
    {
        // Locked reagents' aspects participate in reactions just like normal reagents
        var hot = MakeSingleCellReagent("hot", Aspect.FromId("hot"));
        hot.IsLocked = true;
        var vol = MakeSingleCellReagent("vol", Aspect.FromId("volatile"));

        var grid = SetupGrid(5, 1,
            (hot, new Vector2I(0, 0)),
            (vol, new Vector2I(1, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
    }

    // --- Fragile Universal Vulnerability ---

    [Fact]
    public void Evaluate_FragileAdjacentToExplosion_BreaksFromExplosion()
    {
        // Hot(0,0) + Volatile(1,0) = Explosion. Fragile at (2,0) is adjacent to Volatile.
        var hot = MakeSingleCellReagent("hot", Aspect.Hot());
        var vol = MakeSingleCellReagent("vol", Aspect.Volatile());
        var fragile = MakeSingleCellReagent("fragile", Aspect.Fragile());

        var grid = SetupGrid(5, 1,
            (hot, new Vector2I(0, 0)),
            (vol, new Vector2I(1, 0)),
            (fragile, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
        Assert.Contains(results, r =>
            r.Type == ReactionResultType.Breakage &&
            r.AffectedCells.Contains(new Vector2I(2, 0)));
    }

    [Fact]
    public void Evaluate_FragileAdjacentToNeutralization_Breaks()
    {
        // Hot(0,0) + Cold(1,0) same bag = Neutralization.
        // Fragile at (0,1) is adjacent to Hot(0,0) — an affected cell.
        var hot = MakeSingleCellReagent("hot", Aspect.Hot());
        var cold = MakeSingleCellReagent("cold", Aspect.Cold());
        var fragile = MakeSingleCellReagent("fragile", Aspect.Fragile());

        var grid = SetupGrid(5, 5,
            (hot, new Vector2I(0, 0)),
            (cold, new Vector2I(1, 0)),
            (fragile, new Vector2I(0, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Neutralization);
        Assert.Contains(results, r =>
            r.Type == ReactionResultType.Breakage &&
            r.AffectedCells.Contains(new Vector2I(0, 1)));
    }

    [Fact]
    public void Evaluate_FragileNotAdjacentToReaction_DoesNotBreak()
    {
        // Hot(0,0) + Volatile(1,0) = Explosion. Fragile at (4,4) is far away.
        var hot = MakeSingleCellReagent("hot", Aspect.Hot());
        var vol = MakeSingleCellReagent("vol", Aspect.Volatile());
        var fragile = MakeSingleCellReagent("fragile", Aspect.Fragile());

        var grid = SetupGrid(5, 5,
            (hot, new Vector2I(0, 0)),
            (vol, new Vector2I(1, 0)),
            (fragile, new Vector2I(4, 4)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
        // Fragile should NOT break — it's far from the explosion
        Assert.DoesNotContain(results, r =>
            r.Type == ReactionResultType.Breakage &&
            r.AffectedCells.Contains(new Vector2I(4, 4)));
    }

    [Fact]
    public void Evaluate_FragileNoReactions_DoesNotBreak()
    {
        // Just a Fragile reagent next to a Hot reagent (no Volatile). No reactions trigger.
        var hot = MakeSingleCellReagent("hot", Aspect.Hot());
        var fragile = MakeSingleCellReagent("fragile", Aspect.Fragile());

        var grid = SetupGrid(5, 5,
            (hot, new Vector2I(0, 0)),
            (fragile, new Vector2I(1, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Breakage);
    }

    [Fact]
    public void Evaluate_FragileAlreadyBrokenBySharp_NoDuplicateBreakage()
    {
        // Sharp(0,0) + Fragile(1,0) = Breakage from pair rule.
        // Also have Hot(2,0) + Volatile(3,0) = Explosion adjacent to Fragile.
        // Fragile should only have one Breakage (from Sharp+Fragile pair),
        // NOT a duplicate from the vulnerability second pass.
        var sharp = MakeSingleCellReagent("sharp", Aspect.Sharp());
        var fragile = MakeSingleCellReagent("fragile", Aspect.Fragile());
        var hot = MakeSingleCellReagent("hot", Aspect.Hot());
        var vol = MakeSingleCellReagent("vol", Aspect.Volatile());

        // Sharp(0,0), Fragile(1,0), Hot(2,0), Volatile(3,0)
        // Sharp+Fragile adjacent = Breakage; Hot+Volatile adjacent via net-hot = Explosion
        // Fragile is adjacent to both the Sharp+Fragile reaction cells AND the explosion cells
        var grid = SetupGrid(5, 1,
            (sharp, new Vector2I(0, 0)),
            (fragile, new Vector2I(1, 0)),
            (hot, new Vector2I(2, 0)),
            (vol, new Vector2I(3, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
        // Should have exactly one Breakage involving the Fragile reagent
        var breakages = results.Where(r =>
            r.Type == ReactionResultType.Breakage &&
            r.AffectedCells.Contains(new Vector2I(1, 0))).ToList();
        Assert.Single(breakages);
    }

    [Fact]
    public void Evaluate_MultipleFragileAdjacentToSameReaction_AllBreak()
    {
        // Hot(1,1) + Volatile(2,1) = Explosion.
        // Fragile1(0,1) adjacent to Hot. Fragile2(3,1) adjacent to Volatile.
        // Both should break.
        var hot = MakeSingleCellReagent("hot", Aspect.Hot());
        var vol = MakeSingleCellReagent("vol", Aspect.Volatile());
        var fragile1 = MakeSingleCellReagent("fragile1", Aspect.Fragile());
        var fragile2 = MakeSingleCellReagent("fragile2", Aspect.Fragile());

        var grid = SetupGrid(5, 5,
            (hot, new Vector2I(1, 1)),
            (vol, new Vector2I(2, 1)),
            (fragile1, new Vector2I(0, 1)),
            (fragile2, new Vector2I(3, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
        Assert.Contains(results, r =>
            r.Type == ReactionResultType.Breakage &&
            r.AffectedCells.Contains(new Vector2I(0, 1)));
        Assert.Contains(results, r =>
            r.Type == ReactionResultType.Breakage &&
            r.AffectedCells.Contains(new Vector2I(3, 1)));
    }

    [Fact]
    public void Evaluate_FragileAdjacentToCumulativeOverheating_Breaks()
    {
        // 4 Hot cells trigger Overheating (threshold=4, PerCell).
        // Fragile adjacent to one of the Hot reagents should break.
        var hot1 = MakeSingleCellReagent("hot1", Aspect.Hot());
        var hot2 = MakeSingleCellReagent("hot2", Aspect.Hot());
        var hot3 = MakeSingleCellReagent("hot3", Aspect.Hot());
        var hot4 = MakeSingleCellReagent("hot4", Aspect.Hot());
        var fragile = MakeSingleCellReagent("fragile", Aspect.Fragile());

        var grid = SetupGrid(5, 5,
            (hot1, new Vector2I(0, 0)),
            (hot2, new Vector2I(1, 0)),
            (hot3, new Vector2I(2, 0)),
            (hot4, new Vector2I(3, 0)),
            (fragile, new Vector2I(0, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Overheating);
        // Fragile at (0,1) is adjacent to hot1 at (0,0) which is an affected cell
        Assert.Contains(results, r =>
            r.Type == ReactionResultType.Breakage &&
            r.AffectedCells.Contains(new Vector2I(0, 1)));
    }

    // --- Catalytic threshold reduction tests ---

    [Fact]
    public void Evaluate_CatalyticLowersCumulativeThreshold_CausesOverheating()
    {
        // 3 Hot reagents (magnitude=3) normally below overheating threshold (4).
        // Catalytic(1) adjacent lowers effective threshold to 3: 3 >= 3 → fires
        var hot1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var hot2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var hot3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var catalytic = MakeSingleCellReagent("cat1", Aspect.Catalytic());

        // Catalytic adjacent to h1
        var grid = SetupGrid(5, 5,
            (hot1, new Vector2I(0, 0)),
            (hot2, new Vector2I(2, 0)),
            (hot3, new Vector2I(4, 0)),
            (catalytic, new Vector2I(0, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // effectiveThreshold = max(0, 4 + 0 - 1) = 3, totalMagnitude = 3, 3 >= 3 → fires
        Assert.Contains(results, r => r.Type == ReactionResultType.Overheating);
    }

    [Fact]
    public void Evaluate_CatalyticAndStableCancelOut()
    {
        // 3 Hot reagents (magnitude=3), Stable(1) + Catalytic(1) adjacent
        // effectiveThreshold = max(0, 4 + 1 - 1) = 4 → same as base, no overheating
        var hot1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var hot2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var hot3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var stable = MakeSingleCellReagent("stb1", Aspect.Stable());
        var catalytic = MakeSingleCellReagent("cat1", Aspect.Catalytic());

        // Stable and Catalytic both adjacent to h1
        var grid = SetupGrid(5, 5,
            (hot1, new Vector2I(1, 1)),
            (hot2, new Vector2I(3, 0)),
            (hot3, new Vector2I(4, 0)),
            (stable, new Vector2I(0, 1)),
            (catalytic, new Vector2I(1, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Overheating);
    }

    [Fact]
    public void Evaluate_MultipleCatalyticCellsStack()
    {
        // 2 Hot reagents (magnitude=2) below overheating threshold (4).
        // Two Catalytic(1) reagents adjacent → catalytic reduction = 2
        // effectiveThreshold = max(0, 4 + 0 - 2) = 2, magnitude = 2, 2 >= 2 → fires
        var hot1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var hot2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var cat1 = MakeSingleCellReagent("cat1", Aspect.Catalytic());
        var cat2 = MakeSingleCellReagent("cat2", Aspect.Catalytic());

        // Both catalytic adjacent to hot cells
        var grid = SetupGrid(5, 5,
            (hot1, new Vector2I(0, 0)),
            (hot2, new Vector2I(2, 0)),
            (cat1, new Vector2I(0, 1)),
            (cat2, new Vector2I(2, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Overheating);
    }

    [Fact]
    public void Evaluate_CatalyticAloneNearNonThresholdReaction_NoEffect()
    {
        // Sharp + Fragile adjacent (threshold=0) with Catalytic adjacent.
        // Catalytic can't reduce threshold below 0, so breakage still fires normally.
        // Sharp must be on interior cell (not edge) due to Sharp edge constraint.
        var sharp = MakeSingleCellReagent("sh1", Aspect.Sharp());
        var fragile = MakeSingleCellReagent("fr1", Aspect.Fragile());
        var catalytic = MakeSingleCellReagent("cat1", Aspect.Catalytic());

        // Layout row 1: [Catalytic] [Sharp] [Fragile]
        var grid = SetupGrid(5, 5,
            (catalytic, new Vector2I(0, 1)),
            (sharp, new Vector2I(1, 1)),
            (fragile, new Vector2I(2, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Breakage);
    }

    [Fact]
    public void Evaluate_CatalyticCannotReduceThresholdBelowZero()
    {
        // Catalytic adjacent to a pair rule (threshold=0) — threshold stays 0
        // Breakage rule has threshold=0; catalytic can't lower it further
        var sharp = MakeSingleCellReagent("sh1", Aspect.Sharp());
        var fragile = MakeSingleCellReagent("fr1", Aspect.Fragile());
        var catalytic = MakeSingleCellReagent("cat1", Aspect.Catalytic(3));

        // Layout row 1: [Catalytic(3)] [Sharp] [Fragile]
        var grid = SetupGrid(5, 5,
            (catalytic, new Vector2I(0, 1)),
            (sharp, new Vector2I(1, 1)),
            (fragile, new Vector2I(2, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // effectiveThreshold = max(0, 0 + 0 - 3) = 0 — still fires
        Assert.Contains(results, r => r.Type == ReactionResultType.Breakage);
    }

    [Fact]
    public void Evaluate_CatalyticCountersStableSuppression_Adjacent()
    {
        // Sharp + Fragile adjacent, Stable(1) suppresses. Catalytic(1) counters Stable.
        // effectiveThreshold = max(0, 0 + 1 - 1) = 0 → breakage fires again
        var sharp = MakeSingleCellReagent("sh1", Aspect.Sharp());
        var fragile = MakeSingleCellReagent("fr1", Aspect.Fragile());
        var stable = MakeSingleCellReagent("stb1", Aspect.Stable());
        var catalytic = MakeSingleCellReagent("cat1", Aspect.Catalytic());

        // Layout row 1: [Stable] [Sharp] [Fragile] [Catalytic]
        var grid = SetupGrid(5, 5,
            (stable, new Vector2I(0, 1)),
            (sharp, new Vector2I(1, 1)),
            (fragile, new Vector2I(2, 1)),
            (catalytic, new Vector2I(3, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // Without catalytic, Stable would suppress. With catalytic, it fires.
        Assert.Contains(results, r => r.Type == ReactionResultType.Breakage);
    }

    [Fact]
    public void Evaluate_CatalyticCountersStableSuppression_SameBag()
    {
        // Hot + Cold in same bag (Neutralization, threshold=0), Stable(1) suppresses.
        // Catalytic(1) adjacent counters Stable → neutralization fires again.
        var hot = MakeSingleCellReagent("h1", Aspect.Hot());
        var cold = MakeSingleCellReagent("c1", Aspect.Cold());
        var stable = MakeSingleCellReagent("stb1", Aspect.Stable());
        var catalytic = MakeSingleCellReagent("cat1", Aspect.Catalytic());

        // Layout: [Stable] [Hot] [Cold] [Catalytic]
        var grid = SetupGrid(5, 5,
            (stable, new Vector2I(0, 0)),
            (hot, new Vector2I(1, 0)),
            (cold, new Vector2I(2, 0)),
            (catalytic, new Vector2I(3, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Neutralization);
    }

    [Fact]
    public void Evaluate_CatalyticNotAdjacentToReaction_NoEffect()
    {
        // 3 Hot reagents (magnitude=3) below threshold (4).
        // Catalytic placed far away — not adjacent to any affected cell.
        var hot1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var hot2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var hot3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var catalytic = MakeSingleCellReagent("cat1", Aspect.Catalytic());

        var grid = SetupGrid(6, 6,
            (hot1, new Vector2I(0, 0)),
            (hot2, new Vector2I(2, 0)),
            (hot3, new Vector2I(4, 0)),
            (catalytic, new Vector2I(5, 5)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // Catalytic is too far away — no reduction
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Overheating);
    }

    [Fact]
    public void Evaluate_CatalyticLowersToxicCloudThreshold_SameBagSameAspect()
    {
        // Toxic Cloud requires 2 Toxic in same bag (threshold=2). Catalytic(1) adjacent
        // lowers effectiveThreshold to max(0, 2 - 1) = 1. But withAspect.Count >= 2 guard
        // still requires 2 Toxic reagents, so this doesn't change behavior for 1 Toxic.
        // With 2 Toxic: effectiveThreshold = 1, count = 2, 2 >= 1 && 2 >= 2 → fires (same as baseline)
        var toxic1 = MakeSingleCellReagent("t1", Aspect.Toxic());
        var toxic2 = MakeSingleCellReagent("t2", Aspect.Toxic());
        var catalytic = MakeSingleCellReagent("cat1", Aspect.Catalytic());

        // Layout: [Toxic] [Catalytic] [Toxic]
        var grid = SetupGrid(5, 5,
            (toxic1, new Vector2I(0, 0)),
            (catalytic, new Vector2I(1, 0)),
            (toxic2, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.ToxicCloud);
    }

    // --- Medicinal passive threshold reduction tests ---

    [Fact]
    public void Evaluate_MedicinalRaisesOverheatingThreshold()
    {
        // 4 Hot (magnitude=4) meets overheating threshold (4). Medicinal(1) raises to 5.
        // 4 < 5 → no overheating
        var h1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var h2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var h3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var h4 = MakeSingleCellReagent("h4", Aspect.Hot());
        var med = MakeSingleCellReagent("med1", Aspect.Medicinal());

        var grid = SetupGrid(5, 5,
            (h1, new Vector2I(0, 0)),
            (h2, new Vector2I(1, 0)),
            (h3, new Vector2I(2, 0)),
            (h4, new Vector2I(3, 0)),
            (med, new Vector2I(4, 4))); // Not adjacent — Medicinal is passive/global

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Overheating);
    }

    [Fact]
    public void Evaluate_MedicinalPreventsPoisoning()
    {
        // 3 Toxic (magnitude=3) meets poisoning threshold (3). Medicinal(1) raises to 4.
        // 3 < 4 → no poisoning
        var t1 = MakeSingleCellReagent("t1", Aspect.Toxic());
        var t2 = MakeSingleCellReagent("t2", Aspect.Toxic());
        var t3 = MakeSingleCellReagent("t3", Aspect.Toxic());
        var med = MakeSingleCellReagent("med1", Aspect.Medicinal());

        var grid = SetupGrid(5, 5,
            (t1, new Vector2I(0, 0)),
            (t2, new Vector2I(1, 0)),
            (t3, new Vector2I(2, 0)),
            (med, new Vector2I(4, 4)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Poisoning);
    }

    [Fact]
    public void Evaluate_MultipleMedicinalStack()
    {
        // 5 Hot (magnitude=5) exceeds overheating threshold (4).
        // Two Medicinal(1) reagents raise threshold to 6. 5 < 6 → no overheating
        var h1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var h2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var h3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var h4 = MakeSingleCellReagent("h4", Aspect.Hot());
        var h5 = MakeSingleCellReagent("h5", Aspect.Hot());
        var med1 = MakeSingleCellReagent("med1", Aspect.Medicinal());
        var med2 = MakeSingleCellReagent("med2", Aspect.Medicinal());

        var grid = SetupGrid(7, 5,
            (h1, new Vector2I(0, 0)),
            (h2, new Vector2I(1, 0)),
            (h3, new Vector2I(2, 0)),
            (h4, new Vector2I(3, 0)),
            (h5, new Vector2I(4, 0)),
            (med1, new Vector2I(5, 4)),
            (med2, new Vector2I(6, 4)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // effectiveThreshold = 4 + 0 + 2 - 0 = 6, magnitude = 5, 5 < 6 → no overheating
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Overheating);
    }

    [Fact]
    public void Evaluate_MedicinalDoesNotAffectAdjacentReactions()
    {
        // Medicinal should NOT affect Adjacent-tier reactions (e.g., explosions)
        // Hot + Volatile adjacent should still explode even with Medicinal in inventory
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var vol = MakeSingleCellReagent("vol1", Aspect.Volatile());
        var med = MakeSingleCellReagent("med1", Aspect.Medicinal());

        var grid = SetupGrid(5, 5,
            (hot, new Vector2I(0, 0)),
            (vol, new Vector2I(1, 0)),
            (med, new Vector2I(4, 4)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_MedicinalDoesNotAffectSameBagReactions()
    {
        // Medicinal should NOT affect SameBag-tier reactions (e.g., neutralization)
        // Hot + Cold in same bag should still neutralize even with Medicinal
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var cold = MakeSingleCellReagent("cold1", Aspect.Cold());
        var med = MakeSingleCellReagent("med1", Aspect.Medicinal());

        var grid = SetupGrid(5, 5,
            (hot, new Vector2I(0, 0)),
            (cold, new Vector2I(2, 0)),
            (med, new Vector2I(4, 4)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Neutralization);
    }

    [Fact]
    public void Evaluate_MedicinalInNullBag_Suppressed()
    {
        // Medicinal in a Null bag should not contribute to threshold relief
        // 4 Hot meets overheating threshold (4), Medicinal in Null bag has no effect
        var h1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var h2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var h3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var h4 = MakeSingleCellReagent("h4", Aspect.Hot());
        var med = MakeSingleCellReagent("med1", Aspect.Medicinal());

        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 5));
        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 1),
            new[] { BagPropertyType.Null });

        var grid = new MetaGrid(6, 1);
        grid.PlaceBag(new Bag(normalBagDef), new Vector2I(0, 0));
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(5, 0));
        grid.PlaceReagent(h1, new Vector2I(0, 0));
        grid.PlaceReagent(h2, new Vector2I(1, 0));
        grid.PlaceReagent(h3, new Vector2I(2, 0));
        grid.PlaceReagent(h4, new Vector2I(3, 0));
        grid.PlaceReagent(med, new Vector2I(5, 0)); // In Null bag

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // Medicinal is suppressed — overheating still fires
        Assert.Contains(results, r => r.Type == ReactionResultType.Overheating);
    }

    [Fact]
    public void Evaluate_MedicinalHighMagnitude_PreventsMultipleCumulative()
    {
        // Medicinal(3) raises all cumulative thresholds by 3
        // Hot=4 (threshold 4+3=7), Toxic=3 (threshold 3+3=6) — both prevented
        var h1 = MakeSingleCellReagent("h1", Aspect.Hot());
        var h2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var h3 = MakeSingleCellReagent("h3", Aspect.Hot());
        var h4 = MakeSingleCellReagent("h4", Aspect.Hot());
        var t1 = MakeSingleCellReagent("t1", Aspect.Toxic());
        var t2 = MakeSingleCellReagent("t2", Aspect.Toxic());
        var t3 = MakeSingleCellReagent("t3", Aspect.Toxic());
        var def = new ReagentDefinition("med1", "med1", new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { new AspectSet(Aspect.Medicinal(3)) });
        var med = new Reagent(def);

        var grid = SetupGrid(8, 5,
            (h1, new Vector2I(0, 0)),
            (h2, new Vector2I(1, 0)),
            (h3, new Vector2I(2, 0)),
            (h4, new Vector2I(3, 0)),
            (t1, new Vector2I(4, 0)),
            (t2, new Vector2I(5, 0)),
            (t3, new Vector2I(6, 0)),
            (med, new Vector2I(7, 4)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Overheating);
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Poisoning);
    }

    // --- IsAspectSuppressed (public API) ---

    [Fact]
    public void IsAspectSuppressed_NullBagCell_ReturnsTrue()
    {
        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 3),
            new[] { BagPropertyType.Null });
        var grid = new MetaGrid(3, 1);
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(0, 0));

        Assert.True(ChemistryEngine.IsAspectSuppressed(grid, new Vector2I(0, 0)));
        Assert.True(ChemistryEngine.IsAspectSuppressed(grid, new Vector2I(1, 0)));
        Assert.True(ChemistryEngine.IsAspectSuppressed(grid, new Vector2I(2, 0)));
    }

    [Fact]
    public void IsAspectSuppressed_NormalBagCell_ReturnsFalse()
    {
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 3));
        var grid = new MetaGrid(3, 1);
        grid.PlaceBag(new Bag(normalBagDef), new Vector2I(0, 0));

        Assert.False(ChemistryEngine.IsAspectSuppressed(grid, new Vector2I(0, 0)));
        Assert.False(ChemistryEngine.IsAspectSuppressed(grid, new Vector2I(1, 0)));
    }

    [Fact]
    public void IsAspectSuppressed_SpanningGrid_NullAndNormalBags()
    {
        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 2),
            new[] { BagPropertyType.Null });
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 2));
        var grid = new MetaGrid(4, 1);
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(0, 0));
        grid.PlaceBag(new Bag(normalBagDef), new Vector2I(2, 0));

        // Null bag cells
        Assert.True(ChemistryEngine.IsAspectSuppressed(grid, new Vector2I(0, 0)));
        Assert.True(ChemistryEngine.IsAspectSuppressed(grid, new Vector2I(1, 0)));
        // Normal bag cells
        Assert.False(ChemistryEngine.IsAspectSuppressed(grid, new Vector2I(2, 0)));
        Assert.False(ChemistryEngine.IsAspectSuppressed(grid, new Vector2I(3, 0)));
    }
}
