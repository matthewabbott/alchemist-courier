using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Action that places a reagent at a position with a given rotation.
/// </summary>
public class PlaceReagentAction : IGridAction
{
    public Reagent Reagent { get; }
    public Vector2I Position { get; }
    public int Rotation { get; }

    private int _previousRotation;

    public PlaceReagentAction(Reagent reagent, Vector2I position, int rotation)
    {
        Reagent = reagent;
        Position = position;
        Rotation = rotation;
    }

    public bool Execute(MetaGrid grid)
    {
        _previousRotation = Reagent.Rotation;
        Reagent.Rotation = Rotation;
        if (!grid.PlaceReagent(Reagent, Position))
        {
            Reagent.Rotation = _previousRotation;
            return false;
        }
        return true;
    }

    public bool Undo(MetaGrid grid)
    {
        if (!grid.RemoveReagent(Reagent))
            return false;
        Reagent.Rotation = _previousRotation;
        return true;
    }
}
