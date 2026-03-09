using System.Collections.Generic;
using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Core.Highlight;

public static class ContiguousBoundary
{
    public static Dictionary<Vector2I, CellEdge> Compute(IReadOnlyCollection<Vector2I> cellPositions)
    {
        var result = new Dictionary<Vector2I, CellEdge>();
        if (cellPositions.Count == 0)
            return result;

        var positionSet = new HashSet<Vector2I>(cellPositions);

        foreach (var pos in positionSet)
        {
            var edges = CellEdge.None;

            if (!positionSet.Contains(pos + Vector2I.Up))
                edges |= CellEdge.Top;
            if (!positionSet.Contains(pos + Vector2I.Down))
                edges |= CellEdge.Bottom;
            if (!positionSet.Contains(pos + Vector2I.Left))
                edges |= CellEdge.Left;
            if (!positionSet.Contains(pos + Vector2I.Right))
                edges |= CellEdge.Right;

            result[pos] = edges;
        }

        return result;
    }
}
