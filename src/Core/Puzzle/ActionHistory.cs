using System.Collections.Generic;
using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Manages undo/redo stacks for grid actions.
/// </summary>
public class ActionHistory
{
    private readonly Stack<IGridAction> _undoStack = new();
    private readonly Stack<IGridAction> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    /// <summary>
    /// Execute an action and push it onto the undo stack.
    /// Clears the redo stack (new action invalidates redo history).
    /// </summary>
    /// <returns>True if the action executed successfully.</returns>
    public bool Execute(IGridAction action, MetaGrid grid)
    {
        if (!action.Execute(grid))
            return false;

        _undoStack.Push(action);
        _redoStack.Clear();
        return true;
    }

    /// <summary>
    /// Undo the most recent action.
    /// </summary>
    /// <returns>True if an action was undone, false if nothing to undo.</returns>
    public bool Undo(MetaGrid grid)
    {
        if (_undoStack.Count == 0)
            return false;

        var action = _undoStack.Pop();
        if (!action.Undo(grid))
        {
            // If undo fails, push it back so state is consistent
            _undoStack.Push(action);
            return false;
        }

        _redoStack.Push(action);
        return true;
    }

    /// <summary>
    /// Redo the most recently undone action.
    /// </summary>
    /// <returns>True if an action was redone, false if nothing to redo.</returns>
    public bool Redo(MetaGrid grid)
    {
        if (_redoStack.Count == 0)
            return false;

        var action = _redoStack.Pop();
        if (!action.Execute(grid))
        {
            // If redo fails, push it back so state is consistent
            _redoStack.Push(action);
            return false;
        }

        _undoStack.Push(action);
        return true;
    }

    /// <summary>
    /// Clear both undo and redo stacks.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
