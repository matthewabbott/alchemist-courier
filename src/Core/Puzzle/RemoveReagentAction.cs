using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Action that removes a reagent, storing its previous position and rotation for undo.
/// </summary>
public class RemoveReagentAction : IGridAction
{
    public Reagent Reagent { get; }
    public Vector2I PreviousPosition { get; }
    public int PreviousRotation { get; }

    public RemoveReagentAction(Reagent reagent)
    {
        Reagent = reagent;
        PreviousPosition = reagent.PlacedPosition ?? new Vector2I(0, 0);
        PreviousRotation = reagent.Rotation;
    }

    public bool Execute(MetaGrid grid)
    {
        return grid.RemoveReagent(Reagent);
    }

    public bool Undo(MetaGrid grid)
    {
        Reagent.Rotation = PreviousRotation;
        return grid.PlaceReagent(Reagent, PreviousPosition);
    }
}
