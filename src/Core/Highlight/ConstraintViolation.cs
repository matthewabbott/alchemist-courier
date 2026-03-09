using System.Collections.Generic;
using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Core.Highlight;

public class ConstraintViolation
{
    public ConstraintType Type { get; }
    public List<Vector2I> AffectedCells { get; }
    public string Reason { get; }
    public string? AspectId { get; }

    public ConstraintViolation(ConstraintType type, List<Vector2I> affectedCells, string reason, string? aspectId = null)
    {
        Type = type;
        AffectedCells = affectedCells;
        Reason = reason;
        AspectId = aspectId;
    }
}
