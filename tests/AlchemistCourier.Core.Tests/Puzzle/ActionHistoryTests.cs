using Xunit;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Tests.Puzzle;

public class ActionHistoryTests
{
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

    // --- ActionHistory basic state ---

    [Fact]
    public void NewHistory_CannotUndoOrRedo()
    {
        var history = new ActionHistory();
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(0, history.RedoCount);
    }

    // --- PlaceReagentAction ---

    [Fact]
    public void Execute_PlaceReagent_PlacesOnGrid()
    {
        var (grid, _) = CreateGridWithBag();
        var reagent = MakeReagent();
        var history = new ActionHistory();

        var result = history.Execute(new PlaceReagentAction(reagent, new Vector2I(0, 0), 0), grid);

        Assert.True(result);
        Assert.True(reagent.IsPlaced);
        Assert.Equal(new Vector2I(0, 0), reagent.PlacedPosition);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Execute_PlaceReagent_InvalidPosition_ReturnsFalse()
    {
        var (grid, _) = CreateGridWithBag();
        var reagent = MakeReagent();
        var history = new ActionHistory();

        // Position (5, 5) is outside the bag area
        var result = history.Execute(new PlaceReagentAction(reagent, new Vector2I(5, 5), 0), grid);

        Assert.False(result);
        Assert.False(reagent.IsPlaced);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Undo_PlaceReagent_RemovesFromGrid()
    {
        var (grid, _) = CreateGridWithBag();
        var reagent = MakeReagent();
        var history = new ActionHistory();

        history.Execute(new PlaceReagentAction(reagent, new Vector2I(1, 1), 0), grid);
        var result = history.Undo(grid);

        Assert.True(result);
        Assert.False(reagent.IsPlaced);
        Assert.Null(reagent.PlacedPosition);
        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void Redo_PlaceReagent_PlacesAgain()
    {
        var (grid, _) = CreateGridWithBag();
        var reagent = MakeReagent();
        var history = new ActionHistory();

        history.Execute(new PlaceReagentAction(reagent, new Vector2I(1, 1), 0), grid);
        history.Undo(grid);
        var result = history.Redo(grid);

        Assert.True(result);
        Assert.True(reagent.IsPlaced);
        Assert.Equal(new Vector2I(1, 1), reagent.PlacedPosition);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    // --- RemoveReagentAction ---

    [Fact]
    public void Execute_RemoveReagent_RemovesFromGrid()
    {
        var (grid, _) = CreateGridWithBag();
        var reagent = MakeReagent();
        grid.PlaceReagent(reagent, new Vector2I(0, 0));
        var history = new ActionHistory();

        var result = history.Execute(new RemoveReagentAction(reagent), grid);

        Assert.True(result);
        Assert.False(reagent.IsPlaced);
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void Undo_RemoveReagent_RestoresPosition()
    {
        var (grid, _) = CreateGridWithBag();
        var reagent = MakeReagent();
        grid.PlaceReagent(reagent, new Vector2I(2, 2));
        var history = new ActionHistory();

        history.Execute(new RemoveReagentAction(reagent), grid);
        var result = history.Undo(grid);

        Assert.True(result);
        Assert.True(reagent.IsPlaced);
        Assert.Equal(new Vector2I(2, 2), reagent.PlacedPosition);
    }

    // --- Rotation tracking ---

    [Fact]
    public void PlaceReagentAction_SetsRotation_UndoRestoresPrevious()
    {
        var (grid, _) = CreateGridWithBag();
        var reagent = MakeReagent();
        reagent.Rotation = 1; // Start at 90 degrees
        var history = new ActionHistory();

        history.Execute(new PlaceReagentAction(reagent, new Vector2I(0, 0), 2), grid);
        Assert.Equal(2, reagent.Rotation);

        history.Undo(grid);
        Assert.Equal(1, reagent.Rotation); // Restored to previous rotation
    }

    [Fact]
    public void RemoveReagentAction_UndoRestoresRotation()
    {
        var (grid, _) = CreateGridWithBag();
        var shape = new bool[,] { { true, true } }; // domino
        var reagent = MakeReagent("r", shape);
        reagent.Rotation = 1; // rotated 90 CW => vertical
        grid.PlaceReagent(reagent, new Vector2I(0, 0));
        var history = new ActionHistory();

        history.Execute(new RemoveReagentAction(reagent), grid);
        reagent.Rotation = 3; // Change rotation while removed

        history.Undo(grid);
        Assert.Equal(1, reagent.Rotation); // Restored to rotation at time of removal
        Assert.True(reagent.IsPlaced);
    }

    // --- Multiple actions ---

    [Fact]
    public void MultipleActions_UndoInReverseOrder()
    {
        var (grid, _) = CreateGridWithBag();
        var r1 = MakeReagent("r1");
        var r2 = MakeReagent("r2");
        var history = new ActionHistory();

        history.Execute(new PlaceReagentAction(r1, new Vector2I(0, 0), 0), grid);
        history.Execute(new PlaceReagentAction(r2, new Vector2I(1, 0), 0), grid);

        Assert.Equal(2, history.UndoCount);

        history.Undo(grid); // Undoes r2
        Assert.True(r1.IsPlaced);
        Assert.False(r2.IsPlaced);

        history.Undo(grid); // Undoes r1
        Assert.False(r1.IsPlaced);
        Assert.False(r2.IsPlaced);
    }

    [Fact]
    public void NewAction_ClearsRedoStack()
    {
        var (grid, _) = CreateGridWithBag();
        var r1 = MakeReagent("r1");
        var r2 = MakeReagent("r2");
        var history = new ActionHistory();

        history.Execute(new PlaceReagentAction(r1, new Vector2I(0, 0), 0), grid);
        history.Undo(grid);
        Assert.True(history.CanRedo);

        // New action should clear redo
        history.Execute(new PlaceReagentAction(r2, new Vector2I(1, 0), 0), grid);
        Assert.False(history.CanRedo);
        Assert.Equal(0, history.RedoCount);
    }

    // --- Clear ---

    [Fact]
    public void Clear_EmptiesBothStacks()
    {
        var (grid, _) = CreateGridWithBag();
        var reagent = MakeReagent();
        var history = new ActionHistory();

        history.Execute(new PlaceReagentAction(reagent, new Vector2I(0, 0), 0), grid);
        history.Undo(grid);
        // Now we have items on redo stack
        Assert.True(history.CanRedo);

        history.Execute(new PlaceReagentAction(reagent, new Vector2I(0, 0), 0), grid);
        // Now we have items on undo stack
        Assert.True(history.CanUndo);

        history.Clear();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(0, history.RedoCount);
    }

    // --- Edge cases ---

    [Fact]
    public void Undo_EmptyStack_ReturnsFalse()
    {
        var (grid, _) = CreateGridWithBag();
        var history = new ActionHistory();

        Assert.False(history.Undo(grid));
    }

    [Fact]
    public void Redo_EmptyStack_ReturnsFalse()
    {
        var (grid, _) = CreateGridWithBag();
        var history = new ActionHistory();

        Assert.False(history.Redo(grid));
    }

    [Fact]
    public void PlaceAndRemove_FullRoundTrip()
    {
        var (grid, _) = CreateGridWithBag();
        var reagent = MakeReagent();
        var history = new ActionHistory();

        // Place, then remove via action
        history.Execute(new PlaceReagentAction(reagent, new Vector2I(0, 0), 0), grid);
        history.Execute(new RemoveReagentAction(reagent), grid);
        Assert.False(reagent.IsPlaced);

        // Undo remove -> back to placed
        history.Undo(grid);
        Assert.True(reagent.IsPlaced);
        Assert.Equal(new Vector2I(0, 0), reagent.PlacedPosition);

        // Undo place -> back to unplaced
        history.Undo(grid);
        Assert.False(reagent.IsPlaced);

        // Redo place -> placed again
        history.Redo(grid);
        Assert.True(reagent.IsPlaced);

        // Redo remove -> removed again
        history.Redo(grid);
        Assert.False(reagent.IsPlaced);
    }

    [Fact]
    public void FailedExecute_DoesNotAffectStacks()
    {
        var (grid, _) = CreateGridWithBag();
        var r1 = MakeReagent("r1");
        var r2 = MakeReagent("r2");
        var history = new ActionHistory();

        history.Execute(new PlaceReagentAction(r1, new Vector2I(0, 0), 0), grid);
        // Try to place r2 at same position (occupied) - should fail
        var result = history.Execute(new PlaceReagentAction(r2, new Vector2I(0, 0), 0), grid);

        Assert.False(result);
        Assert.Equal(1, history.UndoCount); // Only r1's action on stack
        Assert.False(r2.IsPlaced);
    }

    [Fact]
    public void LockedReagent_RemoveActionFails()
    {
        var (grid, _) = CreateGridWithBag();
        var reagent = MakeReagent();
        grid.PlaceReagent(reagent, new Vector2I(0, 0));
        reagent.IsLocked = true;
        var history = new ActionHistory();

        var result = history.Execute(new RemoveReagentAction(reagent), grid);

        Assert.False(result);
        Assert.True(reagent.IsPlaced); // Still placed
        Assert.Equal(0, history.UndoCount); // Not on stack
    }

    [Fact]
    public void GridUnchangedAfterUndoRedo_CellOccupancy()
    {
        var (grid, _) = CreateGridWithBag();
        var reagent = MakeReagent();
        var history = new ActionHistory();

        // Place
        history.Execute(new PlaceReagentAction(reagent, new Vector2I(1, 1), 0), grid);
        Assert.Equal(reagent, grid.GetReagentAtCell(new Vector2I(1, 1)));

        // Undo
        history.Undo(grid);
        Assert.Null(grid.GetReagentAtCell(new Vector2I(1, 1)));

        // Redo
        history.Redo(grid);
        Assert.Equal(reagent, grid.GetReagentAtCell(new Vector2I(1, 1)));
    }
}
