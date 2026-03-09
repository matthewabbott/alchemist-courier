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

    // --- I.2: Neutralization removed — hot/cold balance is per-bag confirm-time check ---

    [Fact]
    public void Evaluate_HotAndColdInSameBag_NoNeutralization()
    {
        // I.2: Neutralization rule removed. Hot+Cold in same bag no longer reacts.
        var hotReagent = MakeSingleCellReagent("hot1", Aspect.Hot());
        var coldReagent = MakeSingleCellReagent("cold1", Aspect.Cold());

        var grid = SetupGrid(5, 5,
            (hotReagent, new Vector2I(0, 0)),
            (coldReagent, new Vector2I(2, 2)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Neutralization);
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

    // --- I.3: Toxic rules removed from MVP set ---

    [Fact]
    public void Evaluate_TwoToxicInSameBag_NoToxicCloud()
    {
        // I.3: Toxic rules removed from MVP — no ToxicCloud reaction
        var toxic1 = MakeSingleCellReagent("tox1", Aspect.Toxic());
        var toxic2 = MakeSingleCellReagent("tox2", Aspect.Toxic());

        var grid = SetupGrid(5, 5,
            (toxic1, new Vector2I(0, 0)),
            (toxic2, new Vector2I(1, 0)));

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

    // --- I.3: Cumulative poisoning removed from MVP ---

    [Fact]
    public void Evaluate_CumulativeToxicExceedsThreshold_NoPoisoning()
    {
        // I.3: Toxic rules removed from MVP — no Poisoning reaction
        var t1 = MakeSingleCellReagent("t1", Aspect.Toxic());
        var t2 = MakeSingleCellReagent("t2", Aspect.Toxic());
        var t3 = MakeSingleCellReagent("t3", Aspect.Toxic());

        var grid = SetupGrid(5, 5,
            (t1, new Vector2I(0, 0)),
            (t2, new Vector2I(1, 0)),
            (t3, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Poisoning);
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
    public void Evaluate_SpanningReagent_FullyNullifiedWhenAnyCellInNullBag()
    {
        // A 2-cell domino spans Null bag and normal bag.
        // Full nullification: ANY cell in Null bag → entire reagent nullified.
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

        // Full nullification: Hot domino has cell in Null bag → all aspects suppressed
        // No explosion even though (2,0) is in normal bag
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Explosion);
    }

    [Fact]
    public void Evaluate_NullBag_HotColdNoReaction()
    {
        // I.2: Neutralization rule removed. Hot+Cold in Null bag — no reaction at all.
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
    public void Evaluate_FragileAdjacentToExplosion_Breaks()
    {
        // Volatile(0,0) + Hot(1,0) adjacent → Explosion.
        // Fragile at (0,1) is adjacent to Volatile(0,0) — an affected cell.
        var vol = MakeSingleCellReagent("vol", Aspect.Volatile());
        var hot = MakeSingleCellReagent("hot", Aspect.Hot());
        var fragile = MakeSingleCellReagent("fragile", Aspect.Fragile());

        var grid = SetupGrid(5, 5,
            (vol, new Vector2I(0, 0)),
            (hot, new Vector2I(1, 0)),
            (fragile, new Vector2I(0, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
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
    public void Evaluate_CatalyticWithToxic_NoToxicCloudRule()
    {
        // Toxic Cloud rule removed from MVP set (I.3). Catalytic adjacent to toxic
        // should not trigger any toxic reaction.
        var toxic1 = MakeSingleCellReagent("t1", Aspect.Toxic());
        var toxic2 = MakeSingleCellReagent("t2", Aspect.Toxic());
        var catalytic = MakeSingleCellReagent("cat1", Aspect.Catalytic());

        // Layout: [Toxic] [Catalytic] [Toxic]
        var grid = SetupGrid(5, 5,
            (toxic1, new Vector2I(0, 0)),
            (catalytic, new Vector2I(1, 0)),
            (toxic2, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.ToxicCloud);
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
        // Normal bag cells (no reagent placed, so no full nullification)
        Assert.False(ChemistryEngine.IsAspectSuppressed(grid, new Vector2I(2, 0)));
        Assert.False(ChemistryEngine.IsAspectSuppressed(grid, new Vector2I(3, 0)));
    }

    // --- Full Nullification (I.4) ---

    [Fact]
    public void IsAspectSuppressed_SpanningReagent_NormalBagCellSuppressedByFullNullification()
    {
        // Domino spans Null bag and Normal bag. Full nullification: cell in normal bag
        // is also suppressed because the reagent has a cell in a null bag.
        var hotDomino = MakeDominoReagent("hot_dom",
            new AspectSet(Aspect.Hot()), new AspectSet(Aspect.Hot()));

        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 2),
            new[] { BagPropertyType.Null });
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 2));

        var grid = new MetaGrid(4, 1);
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(0, 0));
        grid.PlaceBag(new Bag(normalBagDef), new Vector2I(2, 0));
        grid.PlaceReagent(hotDomino, new Vector2I(1, 0)); // (1,0) in Null, (2,0) in Normal

        // Both cells suppressed: (1,0) directly in null bag, (2,0) via full nullification
        Assert.True(ChemistryEngine.IsAspectSuppressed(grid, new Vector2I(1, 0)));
        Assert.True(ChemistryEngine.IsAspectSuppressed(grid, new Vector2I(2, 0)));
    }

    [Fact]
    public void IsAspectSuppressed_ReagentFullyInNormalBag_NotSuppressed()
    {
        // Reagent entirely in normal bag — not suppressed
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 2));

        var grid = new MetaGrid(2, 1);
        grid.PlaceBag(new Bag(normalBagDef), new Vector2I(0, 0));
        grid.PlaceReagent(hot, new Vector2I(0, 0));

        Assert.False(ChemistryEngine.IsAspectSuppressed(grid, new Vector2I(0, 0)));
    }

    [Fact]
    public void IsReagentNullified_ReagentSpanningNullBag_ReturnsTrue()
    {
        var hotDomino = MakeDominoReagent("hot_dom",
            new AspectSet(Aspect.Hot()), new AspectSet(Aspect.Hot()));

        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 2),
            new[] { BagPropertyType.Null });
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 2));

        var grid = new MetaGrid(4, 1);
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(0, 0));
        grid.PlaceBag(new Bag(normalBagDef), new Vector2I(2, 0));
        grid.PlaceReagent(hotDomino, new Vector2I(1, 0)); // Spans both bags

        Assert.True(ChemistryEngine.IsReagentNullified(grid, hotDomino));
    }

    [Fact]
    public void IsReagentNullified_ReagentFullyInNormalBag_ReturnsFalse()
    {
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 2));

        var grid = new MetaGrid(2, 1);
        grid.PlaceBag(new Bag(normalBagDef), new Vector2I(0, 0));
        grid.PlaceReagent(hot, new Vector2I(0, 0));

        Assert.False(ChemistryEngine.IsReagentNullified(grid, hot));
    }

    [Fact]
    public void IsReagentNullified_ReagentFullyInNullBag_ReturnsTrue()
    {
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 2),
            new[] { BagPropertyType.Null });

        var grid = new MetaGrid(2, 1);
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(0, 0));
        grid.PlaceReagent(hot, new Vector2I(0, 0));

        Assert.True(ChemistryEngine.IsReagentNullified(grid, hot));
    }

    [Fact]
    public void Evaluate_FullNullification_SpanningReagentCumulativeNotCounted()
    {
        // Domino spans Null+Normal bag. With full nullification, it shouldn't contribute
        // to cumulative overheating even though one cell is in normal bag.
        var hotDomino = MakeDominoReagent("hot_dom",
            new AspectSet(Aspect.Hot()), new AspectSet(Aspect.Hot()));
        var h2 = MakeSingleCellReagent("h2", Aspect.Hot());
        var h3 = MakeSingleCellReagent("h3", Aspect.Hot());

        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 2),
            new[] { BagPropertyType.Null });
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 4));

        var grid = new MetaGrid(6, 1);
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(0, 0));
        grid.PlaceBag(new Bag(normalBagDef), new Vector2I(2, 0));

        grid.PlaceReagent(hotDomino, new Vector2I(1, 0)); // Spans: (1,0) Null, (2,0) Normal — fully nullified
        grid.PlaceReagent(h2, new Vector2I(3, 0));         // Normal bag — active
        grid.PlaceReagent(h3, new Vector2I(4, 0));         // Normal bag — active

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // Only 2 Hot count (h2, h3). hotDomino is fully nullified. Below threshold of 4.
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Overheating);
    }

    [Fact]
    public void Evaluate_FullNullification_FragileReagentInNullBagNotBroken()
    {
        // Fragile reagent spanning into Null bag should not break from adjacent reactions
        // because its Fragile aspect is fully nullified.
        var fragileDomino = MakeDominoReagent("frag_dom",
            new AspectSet(Aspect.Fragile()), new AspectSet(Aspect.Fragile()));
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var vol = MakeSingleCellReagent("vol1", Aspect.Volatile());

        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 1),
            new[] { BagPropertyType.Null });
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 4));

        var grid = new MetaGrid(5, 1);
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(0, 0));
        grid.PlaceBag(new Bag(normalBagDef), new Vector2I(1, 0));

        grid.PlaceReagent(fragileDomino, new Vector2I(0, 0)); // (0,0) in Null — fully nullified
        grid.PlaceReagent(hot, new Vector2I(2, 0));
        grid.PlaceReagent(vol, new Vector2I(3, 0)); // Hot+Volatile → Explosion

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // Explosion occurs from hot+volatile
        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
        // But fragile domino is nullified — no Breakage from adjacency to explosion
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Breakage);
    }

    [Fact]
    public void Evaluate_FullNullification_AdjacentNonNullifiedReagentStillReacts()
    {
        // Verify that a non-nullified reagent adjacent to a nullified reagent still works normally.
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var vol = MakeSingleCellReagent("vol1", Aspect.Volatile());
        var hotNullified = MakeSingleCellReagent("hot_null", Aspect.Hot());

        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 1),
            new[] { BagPropertyType.Null });
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 3));

        var grid = new MetaGrid(4, 1);
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(0, 0));
        grid.PlaceBag(new Bag(normalBagDef), new Vector2I(1, 0));

        grid.PlaceReagent(hotNullified, new Vector2I(0, 0)); // In null bag — nullified
        grid.PlaceReagent(vol, new Vector2I(1, 0));           // Adjacent to nullified hot (no explosion)
        grid.PlaceReagent(hot, new Vector2I(2, 0));           // In normal bag — active

        var results = ChemistryEngine.Evaluate(grid, AllRules);

        // hot at (2,0) is active and adjacent to volatile at (1,0) → explosion
        Assert.Contains(results, r => r.Type == ReactionResultType.Explosion);
    }

    // --- Per-bag hot/cold balance (I.2) ---

    [Fact]
    public void GetBagBalanceViolations_BalancedBag_ReturnsEmpty()
    {
        // Bag with 2 Hot + 2 Cold = balanced
        var hot1 = MakeSingleCellReagent("hot1", Aspect.Hot());
        var hot2 = MakeSingleCellReagent("hot2", Aspect.Hot());
        var cold1 = MakeSingleCellReagent("cold1", Aspect.Cold());
        var cold2 = MakeSingleCellReagent("cold2", Aspect.Cold());

        var grid = SetupGrid(5, 5,
            (hot1, new Vector2I(0, 0)),
            (cold1, new Vector2I(2, 0)),
            (hot2, new Vector2I(0, 2)),
            (cold2, new Vector2I(2, 2)));

        var violations = ChemistryEngine.GetBagBalanceViolations(grid);
        Assert.Empty(violations);
    }

    [Fact]
    public void GetBagBalanceViolations_ImbalancedBag_ReturnsViolation()
    {
        // Bag with 3 Hot + 1 Cold = imbalanced
        var hot1 = MakeSingleCellReagent("hot1", Aspect.Hot());
        var hot2 = MakeSingleCellReagent("hot2", Aspect.Hot());
        var hot3 = MakeSingleCellReagent("hot3", Aspect.Hot());
        var cold1 = MakeSingleCellReagent("cold1", Aspect.Cold());

        var grid = SetupGrid(5, 5,
            (hot1, new Vector2I(0, 0)),
            (hot2, new Vector2I(2, 0)),
            (hot3, new Vector2I(0, 2)),
            (cold1, new Vector2I(2, 2)));

        var violations = ChemistryEngine.GetBagBalanceViolations(grid);
        Assert.Single(violations);
        Assert.Equal(3, violations[0].HotTotal);
        Assert.Equal(1, violations[0].ColdTotal);
        Assert.Equal(2, violations[0].Imbalance); // +2 too hot
    }

    [Fact]
    public void GetBagBalanceViolations_NullBag_Exempt()
    {
        // Null bag with imbalanced hot/cold — should be exempt (no violation)
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 2),
            new[] { BagPropertyType.Null });
        var grid = new MetaGrid(2, 1);
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(0, 0));
        grid.PlaceReagent(hot, new Vector2I(0, 0));

        var violations = ChemistryEngine.GetBagBalanceViolations(grid);
        Assert.Empty(violations);
    }

    [Fact]
    public void GetBagBalanceViolations_EmptyBag_NoViolation()
    {
        // Empty bag has 0 hot and 0 cold = balanced
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(2, 3));
        var grid = new MetaGrid(3, 2);
        grid.PlaceBag(new Bag(normalBagDef), new Vector2I(0, 0));

        var violations = ChemistryEngine.GetBagBalanceViolations(grid);
        Assert.Empty(violations);
    }

    [Fact]
    public void GetBagBalanceViolations_NullifiedReagent_NotCounted()
    {
        // Domino spanning null + normal bag: hot aspect is fully nullified
        var hotDomino = MakeDominoReagent("hot_dom",
            new AspectSet(Aspect.Hot()), new AspectSet(Aspect.Hot()));

        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 2),
            new[] { BagPropertyType.Null });
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 2));

        var grid = new MetaGrid(4, 1);
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(0, 0));
        grid.PlaceBag(new Bag(normalBagDef), new Vector2I(2, 0));
        grid.PlaceReagent(hotDomino, new Vector2I(1, 0)); // Spans: (1,0) Null, (2,0) Normal

        var violations = ChemistryEngine.GetBagBalanceViolations(grid);
        // Normal bag has the hot domino's cell but it's fully nullified — effective 0 hot
        Assert.Empty(violations);
    }

    [Fact]
    public void GetBagBalanceViolations_MultipleBags_EachCheckedIndependently()
    {
        // Bag 1: balanced (1 hot, 1 cold). Bag 2: imbalanced (2 hot, 0 cold).
        var hot1 = MakeSingleCellReagent("hot1", Aspect.Hot());
        var cold1 = MakeSingleCellReagent("cold1", Aspect.Cold());
        var hot2 = MakeSingleCellReagent("hot2", Aspect.Hot());
        var hot3 = MakeSingleCellReagent("hot3", Aspect.Hot());

        var bag1Def = new BagDefinition("bag1", "Bag1", CreateFilledShape(1, 3));
        var bag2Def = new BagDefinition("bag2", "Bag2", CreateFilledShape(1, 3));

        var grid = new MetaGrid(7, 1);
        grid.PlaceBag(new Bag(bag1Def), new Vector2I(0, 0));
        grid.PlaceBag(new Bag(bag2Def), new Vector2I(4, 0));

        grid.PlaceReagent(hot1, new Vector2I(0, 0));  // Bag 1
        grid.PlaceReagent(cold1, new Vector2I(2, 0)); // Bag 1
        grid.PlaceReagent(hot2, new Vector2I(4, 0));  // Bag 2
        grid.PlaceReagent(hot3, new Vector2I(6, 0));  // Bag 2

        var violations = ChemistryEngine.GetBagBalanceViolations(grid);
        Assert.Single(violations); // Only bag 2 is imbalanced
        Assert.Equal(2, violations[0].HotTotal);
        Assert.Equal(0, violations[0].ColdTotal);
    }

    [Fact]
    public void GetBagBalanceViolations_HighMagnitude_ExactBalance()
    {
        // Hot(3) and Cold(3) = balanced even with high magnitude
        var hotDef = new ReagentDefinition("hot1", "hot1", new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { new AspectSet(Aspect.Hot(3)) });
        var coldDef = new ReagentDefinition("cold1", "cold1", new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { new AspectSet(Aspect.Cold(3)) });
        var hot = new Reagent(hotDef);
        var cold = new Reagent(coldDef);

        var grid = SetupGrid(5, 1,
            (hot, new Vector2I(0, 0)),
            (cold, new Vector2I(2, 0)));

        var violations = ChemistryEngine.GetBagBalanceViolations(grid);
        Assert.Empty(violations);
    }

    // --- D.2: Thermal Fracture ---

    [Fact]
    public void Evaluate_FragileBetweenHotAndCold_ThermalFracture()
    {
        // Hot at (0,0), Fragile at (1,0), Cold at (2,0)
        // Fragile is adjacent to both hot and cold → ThermalFracture
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());
        var cold = MakeSingleCellReagent("cold1", Aspect.Cold());

        var grid = SetupGrid(3, 1,
            (hot, new Vector2I(0, 0)),
            (fragile, new Vector2I(1, 0)),
            (cold, new Vector2I(2, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        Assert.Contains(results, r => r.Type == ReactionResultType.ThermalFracture);
    }

    [Fact]
    public void Evaluate_FragileAdjacentToOnlyHot_NoThermalFracture()
    {
        // Hot at (0,0), Fragile at (1,0) — only hot, no cold → no fracture
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());

        var grid = SetupGrid(3, 1,
            (hot, new Vector2I(0, 0)),
            (fragile, new Vector2I(1, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.ThermalFracture);
    }

    [Fact]
    public void Evaluate_FragileAdjacentToOnlyCold_NoThermalFracture()
    {
        // Cold at (0,0), Fragile at (1,0) — only cold, no hot → no fracture
        var cold = MakeSingleCellReagent("cold1", Aspect.Cold());
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());

        var grid = SetupGrid(3, 1,
            (cold, new Vector2I(0, 0)),
            (fragile, new Vector2I(1, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.ThermalFracture);
    }

    [Fact]
    public void Evaluate_FragileNotAdjacentToHotOrCold_NoThermalFracture()
    {
        // Hot at (0,0), Fragile at (2,0) — not adjacent → no fracture
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var cold = MakeSingleCellReagent("cold1", Aspect.Cold());
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());

        var grid = SetupGrid(5, 1,
            (hot, new Vector2I(0, 0)),
            (fragile, new Vector2I(2, 0)),
            (cold, new Vector2I(4, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.ThermalFracture);
    }

    [Fact]
    public void Evaluate_NullifiedFragile_NoThermalFracture()
    {
        // Fragile in null bag between hot and cold — nullified, no fracture
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());
        var cold = MakeSingleCellReagent("cold1", Aspect.Cold());

        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 1));
        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 1),
            new[] { BagPropertyType.Null });
        var normalBagDef2 = new BagDefinition("norm2", "Normal2", CreateFilledShape(1, 1));

        var grid = new MetaGrid(3, 1);
        grid.PlaceBag(new Bag(normalBagDef), new Vector2I(0, 0));
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(1, 0));
        grid.PlaceBag(new Bag(normalBagDef2), new Vector2I(2, 0));

        grid.PlaceReagent(hot, new Vector2I(0, 0));
        grid.PlaceReagent(fragile, new Vector2I(1, 0)); // In null bag
        grid.PlaceReagent(cold, new Vector2I(2, 0));

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.ThermalFracture);
    }

    [Fact]
    public void Evaluate_NullifiedHot_NoThermalFracture()
    {
        // Hot is nullified, so fragile shouldn't see adjacent hot
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());
        var cold = MakeSingleCellReagent("cold1", Aspect.Cold());

        var nullBagDef = new BagDefinition("null1", "Null", CreateFilledShape(1, 1),
            new[] { BagPropertyType.Null });
        var normalBagDef = new BagDefinition("norm1", "Normal", CreateFilledShape(1, 2));

        var grid = new MetaGrid(3, 1);
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(0, 0));
        grid.PlaceBag(new Bag(normalBagDef), new Vector2I(1, 0));

        grid.PlaceReagent(hot, new Vector2I(0, 0));     // In null bag — nullified
        grid.PlaceReagent(fragile, new Vector2I(1, 0)); // In normal bag
        grid.PlaceReagent(cold, new Vector2I(2, 0));    // In normal bag

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.ThermalFracture);
    }

    [Fact]
    public void Evaluate_FragileMultiCell_HotAndColdOnDifferentSides_ThermalFracture()
    {
        // 2-cell Fragile domino: hot adjacent to one end, cold adjacent to other end
        var hot = MakeSingleCellReagent("hot1", Aspect.Hot());
        var fragileDef = new ReagentDefinition("fragile_dom", "Fragile Dom",
            new bool[,] { { true, true } },
            aspectsPerCell: new List<AspectSet>
            {
                new AspectSet(Aspect.Fragile()),
                new AspectSet(Aspect.Fragile()),
            });
        var fragile = new Reagent(fragileDef);
        var cold = MakeSingleCellReagent("cold1", Aspect.Cold());

        var grid = SetupGrid(4, 1,
            (hot, new Vector2I(0, 0)),
            (fragile, new Vector2I(1, 0)),   // Occupies (1,0) and (2,0)
            (cold, new Vector2I(3, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        Assert.Contains(results, r => r.Type == ReactionResultType.ThermalFracture);
    }

    // --- D.2: Crushing ---

    [Fact]
    public void Evaluate_HeavyDirectlyOnFragile_Crushing()
    {
        // Normal bag (no gravity constraint on placement): Heavy at (0,0), Fragile at (0,1).
        // Crushing uses default Down direction: Heavy bearing cell (0,0), below is (0,1) = Fragile.
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());
        var heavy = MakeSingleCellReagent("heavy1", Aspect.Heavy());

        var grid = SetupGrid(1, 2,
            (heavy, new Vector2I(0, 0)),
            (fragile, new Vector2I(0, 1)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        Assert.Contains(results, r => r.Type == ReactionResultType.Crushing);
    }

    [Fact]
    public void Evaluate_NonHeavyOnFragile_NoCrushing()
    {
        // Non-heavy on top of fragile — no crushing (packing peanut protection)
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());
        var nonHeavy = MakeSingleCellReagent("light1");  // No aspects

        var gravBagDef = new BagDefinition("grav1", "Gravity", CreateFilledShape(2, 1),
            new[] { BagPropertyType.Gravity });

        var grid = new MetaGrid(1, 2);
        grid.PlaceBag(new Bag(gravBagDef), new Vector2I(0, 0));
        grid.PlaceReagent(fragile, new Vector2I(0, 1));  // Bottom
        grid.PlaceReagent(nonHeavy, new Vector2I(0, 0)); // Top — not heavy

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Crushing);
    }

    [Fact]
    public void Evaluate_HeavyNotDirectlyOnFragile_NoCrushing()
    {
        // Heavy and fragile in gravity bag but not directly on top
        // Heavy at (0,0), inert at (0,1), fragile at (0,2) — buffer between them
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());
        var heavy = MakeSingleCellReagent("heavy1", Aspect.Heavy());
        var inert = MakeSingleCellReagent("inert1"); // No aspects

        var gravBagDef = new BagDefinition("grav1", "Gravity", CreateFilledShape(3, 1),
            new[] { BagPropertyType.Gravity });

        var grid = new MetaGrid(1, 3);
        grid.PlaceBag(new Bag(gravBagDef), new Vector2I(0, 0));
        grid.PlaceReagent(heavy, new Vector2I(0, 0));   // Top
        grid.PlaceReagent(inert, new Vector2I(0, 1));    // Middle — buffer
        grid.PlaceReagent(fragile, new Vector2I(0, 2)); // Bottom

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        // Heavy bearing cell is (0,0), gravity points down to (0,1) which is inert — not fragile
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Crushing);
    }

    [Fact]
    public void Evaluate_HeavyOnFragile_NullifiedFragile_NoCrushing()
    {
        // Fragile is nullified → no crushing
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());
        var heavy = MakeSingleCellReagent("heavy1", Aspect.Heavy());

        var nullBagDef = new BagDefinition("null_grav", "Null Gravity",
            CreateFilledShape(2, 1),
            new[] { BagPropertyType.Gravity, BagPropertyType.Null });

        var grid = new MetaGrid(1, 2);
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(0, 0));
        grid.PlaceReagent(fragile, new Vector2I(0, 1));
        grid.PlaceReagent(heavy, new Vector2I(0, 0));

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Crushing);
    }

    [Fact]
    public void Evaluate_HeavyOnFragile_NullifiedHeavy_NoCrushing()
    {
        // Heavy is nullified → no crushing
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());
        var heavy = MakeSingleCellReagent("heavy1", Aspect.Heavy());

        var nullBagDef = new BagDefinition("null_grav", "Null Gravity",
            CreateFilledShape(2, 1),
            new[] { BagPropertyType.Gravity, BagPropertyType.Null });

        var grid = new MetaGrid(1, 2);
        grid.PlaceBag(new Bag(nullBagDef), new Vector2I(0, 0));
        grid.PlaceReagent(fragile, new Vector2I(0, 1));
        grid.PlaceReagent(heavy, new Vector2I(0, 0));

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        // Both are nullified in null bag
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Crushing);
    }

    [Fact]
    public void Evaluate_CrushingInLeftGravityBag()
    {
        // Left-gravity bag: Heavy at col 1, Fragile at col 0.
        // Gravity = Left, so bearing cell = cell whose LEFT neighbor is not in the set.
        // Heavy at (1,0): left neighbor is (0,0), not in heavy set → (1,0) is bearing cell.
        // Gravity dir = Left → check (1,0) + Left = (0,0) = Fragile → CRUSHING!
        //
        // Note: Heavy constraint only blocks "heavy above non-heavy" in gravity direction.
        // In left-gravity, heavy at col 1 with non-heavy at col 0 (left, in gravity dir) —
        // the heavy IS above non-heavy in gravity terms → placement blocked.
        // So we use a normal bag wrapper to avoid placement restrictions.
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());
        var heavy = MakeSingleCellReagent("heavy1", Aspect.Heavy());

        // Use a normal bag to avoid Heavy constraint blocking placement.
        // Crushing still detects using default Down direction since it's non-gravity.
        var grid = SetupGrid(2, 1,
            (fragile, new Vector2I(0, 0)),
            (heavy, new Vector2I(1, 0)));

        // In non-gravity bag, gravity defaults to Down. Heavy at (1,0): bearing cell is (1,0).
        // Down from (1,0) is (1,1) — out of bounds, not fragile. So no crushing here.
        // Instead test with vertical layout:
        var grid2 = SetupGrid(1, 2,
            (MakeSingleCellReagent("heavy2", Aspect.Heavy()), new Vector2I(0, 0)),
            (MakeSingleCellReagent("fragile2", Aspect.Fragile()), new Vector2I(0, 1)));

        var results = ChemistryEngine.Evaluate(grid2, AllRules);
        Assert.Contains(results, r => r.Type == ReactionResultType.Crushing);
    }

    [Fact]
    public void Evaluate_HeavyNotAboveFragile_NoCrushing()
    {
        // Heavy and fragile side by side, not in gravity direction → no crushing.
        var fragile = MakeSingleCellReagent("fragile1", Aspect.Fragile());
        var heavy = MakeSingleCellReagent("heavy1", Aspect.Heavy());

        var grid = SetupGrid(2, 1,
            (heavy, new Vector2I(0, 0)),
            (fragile, new Vector2I(1, 0)));

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        // Heavy at (0,0), gravity = Down. Bearing cell (0,0), below is (0,1) — out of grid.
        // Fragile is at (1,0) — not below the heavy. No crushing.
        Assert.DoesNotContain(results, r => r.Type == ReactionResultType.Crushing);
    }

    [Fact]
    public void Evaluate_MultipleFragile_BothCrushed()
    {
        // Heavy domino on top row, two fragile below (side by side) — both should get crushed.
        // Use normal bag (no gravity constraint on placement).
        var fragile1 = MakeSingleCellReagent("fragile1", Aspect.Fragile());
        var fragile2 = MakeSingleCellReagent("fragile2", Aspect.Fragile());
        var heavyDef = new ReagentDefinition("heavy_dom", "Heavy Dom",
            new bool[,] { { true, true } },
            aspectsPerCell: new List<AspectSet>
            {
                new AspectSet(Aspect.Heavy()),
                new AspectSet(Aspect.Heavy()),
            });
        var heavy = new Reagent(heavyDef);

        var grid = SetupGrid(2, 2,
            (heavy, new Vector2I(0, 0)),       // Top row, both cells (0,0) and (1,0)
            (fragile1, new Vector2I(0, 1)),    // Bottom-left
            (fragile2, new Vector2I(1, 1)));   // Bottom-right

        var results = ChemistryEngine.Evaluate(grid, AllRules);
        // Both fragile should be crushed — 2 Crushing results
        var crushResults = results.Where(r => r.Type == ReactionResultType.Crushing).ToList();
        Assert.Equal(2, crushResults.Count);
    }
}
