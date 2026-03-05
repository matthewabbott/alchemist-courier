using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Core.Highlight;

public class CellHighlight
{
    public Vector2I Position { get; }
    public CellEdge Edges { get; }
    public HighlightLayer Layer { get; }
    public HighlightSeverity Severity { get; }
    public string? AspectId { get; }
    public string? Reason { get; }

    public CellHighlight(
        Vector2I position,
        CellEdge edges,
        HighlightLayer layer,
        HighlightSeverity severity,
        string? aspectId = null,
        string? reason = null)
    {
        Position = position;
        Edges = edges;
        Layer = layer;
        Severity = severity;
        AspectId = aspectId;
        Reason = reason;
    }
}
