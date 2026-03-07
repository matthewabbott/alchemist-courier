using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// A reversible action that can be executed and undone on a MetaGrid.
/// </summary>
public interface IGridAction
{
    /// <summary>Execute the action on the grid.</summary>
    /// <returns>True if the action succeeded.</returns>
    bool Execute(MetaGrid grid);

    /// <summary>Undo the action, restoring the grid to its previous state.</summary>
    /// <returns>True if the undo succeeded.</returns>
    bool Undo(MetaGrid grid);
}
