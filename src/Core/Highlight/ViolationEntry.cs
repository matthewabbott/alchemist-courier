using System.Collections.Generic;
using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Core.Highlight;

/// <summary>
/// A single live violation entry for the violations panel (J.3).
/// Represents one issue in the current grid state that would cause
/// failure at confirm time.
/// </summary>
public class ViolationEntry
{
    /// <summary>Category of this violation.</summary>
    public ViolationType Type { get; }

    /// <summary>Human-readable description (e.g., "Bag 1: too hot (+2)").</summary>
    public string Description { get; }

    /// <summary>Severity for visual rendering.</summary>
    public HighlightSeverity Severity { get; }

    /// <summary>Grid cells affected by this violation (for highlighting on click).</summary>
    public IReadOnlyList<Vector2I> AffectedCells { get; }

    /// <summary>
    /// Optional aspect ID associated with this violation (for highlight coloring).
    /// </summary>
    public string? AspectId { get; }

    public ViolationEntry(
        ViolationType type,
        string description,
        HighlightSeverity severity,
        IReadOnlyList<Vector2I> affectedCells,
        string? aspectId = null)
    {
        Type = type;
        Description = description;
        Severity = severity;
        AffectedCells = affectedCells;
        AspectId = aspectId;
    }
}
