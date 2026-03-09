using System.Collections.Generic;

namespace AlchemistCourier.Core.Highlight;

/// <summary>
/// Tracks current vs threshold values for cumulative aspects (e.g., Heat, Cold, Toxicity)
/// and returns severity levels for status bar and highlight rendering.
/// </summary>
public class StatusThresholds
{
    private readonly Dictionary<string, (int Current, int Threshold)> _entries = new();

    public void Set(string aspectId, int current, int threshold)
    {
        _entries[aspectId] = (current, threshold);
    }

    public HighlightSeverity GetSeverity(string aspectId)
    {
        if (!_entries.TryGetValue(aspectId, out var entry))
            return HighlightSeverity.Info;

        if (entry.Current >= entry.Threshold)
            return HighlightSeverity.Danger;

        if (entry.Current >= entry.Threshold - 1)
            return HighlightSeverity.Warning;

        return HighlightSeverity.Safe;
    }
}
