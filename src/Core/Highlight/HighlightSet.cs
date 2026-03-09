using System.Collections.Generic;
using System.Linq;
using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Core.Highlight;

public class HighlightSet
{
    private readonly List<CellHighlight> _highlights = new();

    public int Count => _highlights.Count;
    public bool IsEmpty => _highlights.Count == 0;

    public void Add(CellHighlight highlight)
    {
        _highlights.Add(highlight);
    }

    public void AddRange(IEnumerable<CellHighlight> highlights)
    {
        _highlights.AddRange(highlights);
    }

    public void Clear()
    {
        _highlights.Clear();
    }

    public IReadOnlyList<CellHighlight> GetAtPosition(Vector2I position)
    {
        return _highlights.Where(h => h.Position == position).ToList();
    }

    public IReadOnlyList<CellHighlight> GetByLayer(HighlightLayer layer)
    {
        return _highlights.Where(h => h.Layer == layer).ToList();
    }

    public IReadOnlyList<string> GetReferencedAspects()
    {
        return _highlights
            .Where(h => h.AspectId != null)
            .Select(h => h.AspectId!)
            .Distinct()
            .ToList();
    }
}
