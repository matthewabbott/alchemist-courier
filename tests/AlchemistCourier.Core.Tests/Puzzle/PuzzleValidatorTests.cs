using System.Collections.Generic;
using Xunit;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Tests.Puzzle;

public class PuzzleValidatorTests
{
    /// <summary>
    /// Helper: create a simple grid with one bag and place inert reagents in it.
    /// Returns grid with all reagents placed (no reactions).
    /// </summary>
    private (MetaGrid grid, List<Reagent> reagents) SetupSafelyPlacedGrid()
    {
        var grid = new MetaGrid(6, 6);
        var bagShape = new bool[,] { { true, true, true } };
        var bagDef = new BagDefinition("bag", "Bag", bagShape);
        grid.PlaceBag(new Bag(bagDef), new Vector2I(0, 0));

        // Inert reagents (no aspects) — will never react
        var r1 = new Reagent(new ReagentDefinition("r1", "R1", new bool[,] { { true } }));
        var r2 = new Reagent(new ReagentDefinition("r2", "R2", new bool[,] { { true } }));

        grid.PlaceReagent(r1, new Vector2I(0, 0));
        grid.PlaceReagent(r2, new Vector2I(1, 0));

        return (grid, new List<Reagent> { r1, r2 });
    }

    /// <summary>
    /// Helper: create a grid with reagents that are NOT placed.
    /// </summary>
    private (MetaGrid grid, List<Reagent> reagents) SetupUnplacedGrid()
    {
        var grid = new MetaGrid(6, 6);
        var bagShape = new bool[,] { { true, true, true } };
        var bagDef = new BagDefinition("bag", "Bag", bagShape);
        grid.PlaceBag(new Bag(bagDef), new Vector2I(0, 0));

        var r1 = new Reagent(new ReagentDefinition("r1", "R1", new bool[,] { { true } }));
        var r2 = new Reagent(new ReagentDefinition("r2", "R2", new bool[,] { { true } }));

        return (grid, new List<Reagent> { r1, r2 });
    }

    /// <summary>
    /// Helper: create a grid with two reagents in the same bag that react (Volatile + Hot → Explosion).
    /// </summary>
    private (MetaGrid grid, List<Reagent> reagents) SetupReactiveGrid()
    {
        var grid = new MetaGrid(6, 6);
        var bagShape = new bool[,] { { true, true } };
        var bagDef = new BagDefinition("bag", "Bag", bagShape);
        grid.PlaceBag(new Bag(bagDef), new Vector2I(0, 0));

        var volatileAspects = new List<AspectSet> { new AspectSet() };
        volatileAspects[0].Add(Aspect.FromId(AspectIds.Volatile));
        var hotAspects = new List<AspectSet> { new AspectSet() };
        hotAspects[0].Add(Aspect.FromId(AspectIds.Hot));

        var r1 = new Reagent(new ReagentDefinition("r1", "R1", new bool[,] { { true } }, aspectsPerCell: volatileAspects));
        var r2 = new Reagent(new ReagentDefinition("r2", "R2", new bool[,] { { true } }, aspectsPerCell: hotAspects));

        grid.PlaceReagent(r1, new Vector2I(0, 0));
        grid.PlaceReagent(r2, new Vector2I(1, 0));

        return (grid, new List<Reagent> { r1, r2 });
    }

    // --- FitAllSafely tests ---

    [Fact]
    public void FitAllSafely_AllPlacedNoReactions_ReturnsWin()
    {
        var (grid, reagents) = SetupSafelyPlacedGrid();
        var rules = ReactionRuleDatabase.GetMvpRules();

        var result = PuzzleValidator.Evaluate(grid, reagents, PuzzleType.FitAllSafely, rules);

        Assert.Equal(ConfirmResult.Win, result.Result);
        Assert.Empty(result.Reactions);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void FitAllSafely_NotAllPlaced_ReturnsNotAllPlaced()
    {
        var (grid, reagents) = SetupUnplacedGrid();
        var rules = ReactionRuleDatabase.GetMvpRules();

        var result = PuzzleValidator.Evaluate(grid, reagents, PuzzleType.FitAllSafely, rules);

        Assert.Equal(ConfirmResult.NotAllPlaced, result.Result);
        Assert.Empty(result.Reactions);
    }

    [Fact]
    public void FitAllSafely_HasReactions_ReturnsHasReactions()
    {
        var (grid, reagents) = SetupReactiveGrid();
        var rules = ReactionRuleDatabase.GetMvpRules();

        var result = PuzzleValidator.Evaluate(grid, reagents, PuzzleType.FitAllSafely, rules);

        Assert.Equal(ConfirmResult.HasReactions, result.Result);
        Assert.NotEmpty(result.Reactions);
    }

    // --- MaximizeValue tests ---

    [Fact]
    public void MaximizeValue_PartialPlacement_WinWithScore()
    {
        var grid = new MetaGrid(6, 6);
        var bagShape = new bool[,] { { true, true, true } };
        var bagDef = new BagDefinition("bag", "Bag", bagShape);
        grid.PlaceBag(new Bag(bagDef), new Vector2I(0, 0));

        var r1 = new Reagent(new ReagentDefinition("r1", "R1", new bool[,] { { true } }, value: 5));
        var r2 = new Reagent(new ReagentDefinition("r2", "R2", new bool[,] { { true } }, value: 3));
        var r3 = new Reagent(new ReagentDefinition("r3", "R3", new bool[,] { { true } }, value: 7));

        grid.PlaceReagent(r1, new Vector2I(0, 0));
        grid.PlaceReagent(r2, new Vector2I(1, 0));
        // r3 is not placed

        var trayReagents = new List<Reagent> { r1, r2, r3 };
        var rules = ReactionRuleDatabase.GetMvpRules();

        var result = PuzzleValidator.Evaluate(grid, trayReagents, PuzzleType.MaximizeValue, rules);

        Assert.Equal(ConfirmResult.Win, result.Result);
        Assert.Equal(8, result.Score); // 5 + 3
    }

    [Fact]
    public void MaximizeValue_ReactionsPresent_Fails()
    {
        var (grid, reagents) = SetupReactiveGrid();
        var rules = ReactionRuleDatabase.GetMvpRules();

        var result = PuzzleValidator.Evaluate(grid, reagents, PuzzleType.MaximizeValue, rules);

        Assert.Equal(ConfirmResult.HasReactions, result.Result);
        Assert.NotEmpty(result.Reactions);
    }

    [Fact]
    public void MaximizeValue_NothingPlaced_WinsWithZeroScore()
    {
        var (grid, reagents) = SetupUnplacedGrid();
        var rules = ReactionRuleDatabase.GetMvpRules();

        var result = PuzzleValidator.Evaluate(grid, reagents, PuzzleType.MaximizeValue, rules);

        Assert.Equal(ConfirmResult.Win, result.Result);
        Assert.Equal(0, result.Score);
    }

    // --- MeetRequirement tests ---

    [Fact]
    public void MeetRequirement_DelegatesToFitAllSafely()
    {
        var (grid, reagents) = SetupSafelyPlacedGrid();
        var rules = ReactionRuleDatabase.GetMvpRules();

        var result = PuzzleValidator.Evaluate(grid, reagents, PuzzleType.MeetRequirement, rules);

        Assert.Equal(ConfirmResult.Win, result.Result);
        Assert.Empty(result.Reactions);
    }

    // --- Hot/Cold Balance (I.2) ---

    [Fact]
    public void FitAllSafely_ImbalancedHotCold_ReturnsHotColdImbalance()
    {
        // All reagents placed, no reactions, but hot/cold imbalanced → fails
        var grid = new MetaGrid(6, 6);
        var bagShape = new bool[,] { { true, true, true } };
        var bagDef = new BagDefinition("bag", "Bag", bagShape);
        grid.PlaceBag(new Bag(bagDef), new Vector2I(0, 0));

        var hotAspects = new List<AspectSet> { new AspectSet(Aspect.Hot()) };
        var r1 = new Reagent(new ReagentDefinition("r1", "R1", new bool[,] { { true } },
            aspectsPerCell: hotAspects));
        var r2 = new Reagent(new ReagentDefinition("r2", "R2", new bool[,] { { true } }));

        grid.PlaceReagent(r1, new Vector2I(0, 0));
        grid.PlaceReagent(r2, new Vector2I(2, 0)); // Non-adjacent to avoid reactions

        var result = PuzzleValidator.Evaluate(grid, new List<Reagent> { r1, r2 },
            PuzzleType.FitAllSafely, ReactionRuleDatabase.GetMvpRules());

        Assert.Equal(ConfirmResult.HotColdImbalance, result.Result);
        Assert.NotEmpty(result.BalanceViolations);
    }

    [Fact]
    public void FitAllSafely_BalancedHotCold_ReturnsWin()
    {
        // Hot(1) + Cold(1) in same bag, non-adjacent → balanced, no reactions, win
        var grid = new MetaGrid(6, 6);
        var bagShape = new bool[,] { { true, true, true } };
        var bagDef = new BagDefinition("bag", "Bag", bagShape);
        grid.PlaceBag(new Bag(bagDef), new Vector2I(0, 0));

        var hotAspects = new List<AspectSet> { new AspectSet(Aspect.Hot()) };
        var coldAspects = new List<AspectSet> { new AspectSet(Aspect.Cold()) };
        var r1 = new Reagent(new ReagentDefinition("r1", "R1", new bool[,] { { true } },
            aspectsPerCell: hotAspects));
        var r2 = new Reagent(new ReagentDefinition("r2", "R2", new bool[,] { { true } },
            aspectsPerCell: coldAspects));

        grid.PlaceReagent(r1, new Vector2I(0, 0));
        grid.PlaceReagent(r2, new Vector2I(2, 0)); // Non-adjacent

        var result = PuzzleValidator.Evaluate(grid, new List<Reagent> { r1, r2 },
            PuzzleType.FitAllSafely, ReactionRuleDatabase.GetMvpRules());

        Assert.Equal(ConfirmResult.Win, result.Result);
    }
}
