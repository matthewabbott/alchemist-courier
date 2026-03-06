using System.Collections.Generic;
using Xunit;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Tests.Puzzle;

public class HintEngineTests
{
    /// <summary>Create a 6x6 grid with a 4x4 bag at (0,0) for testing.</summary>
    private static (MetaGrid grid, Bag bag) CreateGridWithBag()
    {
        var grid = new MetaGrid(6, 6);
        var bagDef = new BagDefinition("test", "Test Bag", new bool[,]
        {
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
        });
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));
        return (grid, bag);
    }

    private static Reagent MakeReagent(string name = "r", bool[,]? shape = null)
    {
        shape ??= new bool[,] { { true } };
        var def = new ReagentDefinition(name, name, shape);
        return new Reagent(def);
    }

    private static PuzzleState MakeState(List<Placement>? solution = null)
    {
        return new PuzzleState
        {
            Seed = 1,
            Difficulty = DifficultyLevel.Easy,
            PuzzleType = PuzzleType.FitAllSafely,
            Solution = solution ?? new List<Placement>(),
        };
    }

    // --- GetSolutionPlacement ---

    [Fact]
    public void GetSolutionPlacement_ValidIndex_ReturnsPlacement()
    {
        var placement = new Placement { ReagentIndex = 2, PositionCol = 1, PositionRow = 3, Rotation = 1 };
        var state = MakeState(new List<Placement> { placement });

        var result = HintEngine.GetSolutionPlacement(state, 2);

        Assert.NotNull(result);
        Assert.Equal(2, result!.ReagentIndex);
        Assert.Equal(1, result.PositionCol);
        Assert.Equal(3, result.PositionRow);
        Assert.Equal(1, result.Rotation);
    }

    [Fact]
    public void GetSolutionPlacement_InvalidIndex_ReturnsNull()
    {
        var placement = new Placement { ReagentIndex = 0, PositionCol = 0, PositionRow = 0, Rotation = 0 };
        var state = MakeState(new List<Placement> { placement });

        var result = HintEngine.GetSolutionPlacement(state, 5);

        Assert.Null(result);
    }

    // --- ComputeDislodgeList ---

    [Fact]
    public void ComputeDislodgeList_EmptyTarget_ReturnsEmptyList()
    {
        var (grid, _) = CreateGridWithBag();
        var target = MakeReagent("target");
        var reagents = new List<Reagent> { target };
        var placement = new Placement { ReagentIndex = 0, PositionCol = 0, PositionRow = 0, Rotation = 0 };

        var result = HintEngine.ComputeDislodgeList(grid, reagents, target, placement);

        Assert.NotNull(result);
        Assert.Empty(result!);
    }

    [Fact]
    public void ComputeDislodgeList_OccupiedCells_ReturnsOccupants()
    {
        var (grid, _) = CreateGridWithBag();
        var target = MakeReagent("target");
        var blocker = MakeReagent("blocker");
        var reagents = new List<Reagent> { target, blocker };

        // Place blocker at (0,0)
        grid.PlaceReagent(blocker, new Vector2I(0, 0));

        // Target wants to go to (0,0) — blocker must be dislodged
        var placement = new Placement { ReagentIndex = 0, PositionCol = 0, PositionRow = 0, Rotation = 0 };

        var result = HintEngine.ComputeDislodgeList(grid, reagents, target, placement);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Contains(1, result); // blocker is index 1
    }

    [Fact]
    public void ComputeDislodgeList_LockedBlocker_ReturnsNull()
    {
        var (grid, _) = CreateGridWithBag();
        var target = MakeReagent("target");
        var locked = MakeReagent("locked");
        locked.IsLocked = true;
        var reagents = new List<Reagent> { target, locked };

        // Place locked reagent at (0,0)
        grid.PlaceReagent(locked, new Vector2I(0, 0));

        var placement = new Placement { ReagentIndex = 0, PositionCol = 0, PositionRow = 0, Rotation = 0 };

        var result = HintEngine.ComputeDislodgeList(grid, reagents, target, placement);

        Assert.Null(result); // Blocked
    }

    [Fact]
    public void ComputeDislodgeList_SelfExclusion_DoesNotDislodgeSelf()
    {
        var (grid, _) = CreateGridWithBag();
        var target = MakeReagent("target");
        var reagents = new List<Reagent> { target };

        // Place target at (0,0), then ask for dislodge at the same position
        grid.PlaceReagent(target, new Vector2I(0, 0));

        var placement = new Placement { ReagentIndex = 0, PositionCol = 0, PositionRow = 0, Rotation = 0 };

        var result = HintEngine.ComputeDislodgeList(grid, reagents, target, placement);

        Assert.NotNull(result);
        Assert.Empty(result!); // Target itself should not appear
    }

    // --- PickNextHintTarget ---

    [Fact]
    public void PickNextHintTarget_SnapBackPriority_ReturnsPreviouslyHintedFirst()
    {
        var state = MakeState();
        var hintState = new HintState(3);
        var r0 = MakeReagent("r0"); // not placed, not hinted
        var r1 = MakeReagent("r1"); // not placed, was hinted (snap-back candidate)
        var reagents = new List<Reagent> { r0, r1 };

        hintState.HintedReagentIndices.Add(1); // r1 was previously hinted

        var result = HintEngine.PickNextHintTarget(state, hintState, reagents);

        Assert.Equal(1, result); // Snap-back (free) takes priority
    }

    [Fact]
    public void PickNextHintTarget_NewHint_ReturnsFirstUnhinted()
    {
        var state = MakeState();
        var hintState = new HintState(3);
        var r0 = MakeReagent("r0");
        r0.PlacedPosition = new Vector2I(0, 0); // already placed — skip
        var r1 = MakeReagent("r1"); // not placed, not hinted — candidate
        var reagents = new List<Reagent> { r0, r1 };

        var result = HintEngine.PickNextHintTarget(state, hintState, reagents);

        Assert.Equal(1, result);
    }

    [Fact]
    public void PickNextHintTarget_AllPlaced_ReturnsNull()
    {
        var state = MakeState();
        var hintState = new HintState(3);
        var r0 = MakeReagent("r0");
        r0.PlacedPosition = new Vector2I(0, 0);
        var r1 = MakeReagent("r1");
        r1.PlacedPosition = new Vector2I(1, 0);
        var reagents = new List<Reagent> { r0, r1 };

        var result = HintEngine.PickNextHintTarget(state, hintState, reagents);

        Assert.Null(result);
    }

    // --- ExecuteHint ---

    [Fact]
    public void ExecuteHint_PlacesReagentAtSolutionPosition()
    {
        var (grid, _) = CreateGridWithBag();
        var target = MakeReagent("target");
        var reagents = new List<Reagent> { target };
        var placement = new Placement { ReagentIndex = 0, PositionCol = 2, PositionRow = 1, Rotation = 0 };

        var result = HintEngine.ExecuteHint(grid, reagents, 0, placement);

        Assert.True(result.IsSuccess);
        Assert.True(target.IsPlaced);
        Assert.Equal(new Vector2I(2, 1), target.PlacedPosition);
        Assert.Empty(result.DislodgedReagentIndices);
    }

    [Fact]
    public void ExecuteHint_DislodgesOccupants()
    {
        var (grid, _) = CreateGridWithBag();
        var target = MakeReagent("target");
        var blocker = MakeReagent("blocker");
        blocker.IsHinted = true; // Was previously hinted
        var reagents = new List<Reagent> { target, blocker };

        // Place blocker at (1,1)
        grid.PlaceReagent(blocker, new Vector2I(1, 1));

        // Hint target to (1,1) — must dislodge blocker
        var placement = new Placement { ReagentIndex = 0, PositionCol = 1, PositionRow = 1, Rotation = 0 };

        var result = HintEngine.ExecuteHint(grid, reagents, 0, placement);

        Assert.True(result.IsSuccess);
        Assert.True(target.IsPlaced);
        Assert.False(blocker.IsPlaced);
        Assert.Single(result.DislodgedReagentIndices);
        Assert.Contains(1, result.DislodgedReagentIndices);
    }

    [Fact]
    public void ExecuteHint_SetsIsHintedFlag()
    {
        var (grid, _) = CreateGridWithBag();
        var target = MakeReagent("target");
        var reagents = new List<Reagent> { target };
        var placement = new Placement { ReagentIndex = 0, PositionCol = 0, PositionRow = 0, Rotation = 0 };

        Assert.False(target.IsHinted);

        HintEngine.ExecuteHint(grid, reagents, 0, placement);

        Assert.True(target.IsHinted);
    }

    [Fact]
    public void ExecuteHint_SetsRotation()
    {
        var (grid, _) = CreateGridWithBag();
        // Use a domino (1x2) so rotation matters
        var def = new ReagentDefinition("dom", "Domino", new bool[,] { { true, true } });
        var target = new Reagent(def);
        Assert.Equal(0, target.Rotation);

        var reagents = new List<Reagent> { target };
        // Place with rotation=1 (becomes vertical 2x1)
        var placement = new Placement { ReagentIndex = 0, PositionCol = 0, PositionRow = 0, Rotation = 1 };

        var result = HintEngine.ExecuteHint(grid, reagents, 0, placement);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, target.Rotation);
    }

    [Fact]
    public void ExecuteHint_DislodgedReagentResetsRotationAndIsHinted()
    {
        var (grid, _) = CreateGridWithBag();
        var target = MakeReagent("target");
        var blocker = MakeReagent("blocker");
        blocker.Rotation = 2;
        blocker.IsHinted = true;
        var reagents = new List<Reagent> { target, blocker };

        grid.PlaceReagent(blocker, new Vector2I(0, 0));

        var placement = new Placement { ReagentIndex = 0, PositionCol = 0, PositionRow = 0, Rotation = 0 };

        HintEngine.ExecuteHint(grid, reagents, 0, placement);

        Assert.Equal(0, blocker.Rotation);
        Assert.False(blocker.IsHinted);
    }

    [Fact]
    public void ExecuteHint_BlockedByLocked_ReturnsBlockedResult()
    {
        var (grid, _) = CreateGridWithBag();
        var target = MakeReagent("target");
        var locked = MakeReagent("locked");
        locked.IsLocked = true;
        var reagents = new List<Reagent> { target, locked };

        grid.PlaceReagent(locked, new Vector2I(0, 0));

        var placement = new Placement { ReagentIndex = 0, PositionCol = 0, PositionRow = 0, Rotation = 0 };

        var result = HintEngine.ExecuteHint(grid, reagents, 0, placement);

        Assert.Equal(HintResultType.BlockedByLockedReagent, result.Type);
        Assert.False(result.IsSuccess);
        Assert.True(locked.IsPlaced); // Locked reagent should NOT be removed
    }
}
