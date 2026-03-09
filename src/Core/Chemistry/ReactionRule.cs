using System.Collections.Generic;

namespace AlchemistCourier.Core.Chemistry;

/// <summary>
/// Defines a single reaction rule: what aspects trigger it, how close they must be,
/// how they're counted, and what happens when it fires.
/// </summary>
public class ReactionRule
{
    /// <summary>Aspect IDs that must be co-present to trigger this rule.</summary>
    public List<string> TriggerAspects { get; }

    /// <summary>How close the triggering aspects must be.</summary>
    public SensitivityTier Sensitivity { get; }

    /// <summary>Whether to count cells or items.</summary>
    public Granularity Granularity { get; }

    /// <summary>
    /// Minimum cumulative magnitude to trigger (for cumulative rules).
    /// 0 means any contact triggers the reaction.
    /// </summary>
    public int Threshold { get; }

    /// <summary>What type of result this reaction produces.</summary>
    public ReactionResultType ResultType { get; }

    /// <summary>Human-readable name for this rule (e.g., "Explosion").</summary>
    public string Name { get; }

    public ReactionRule(
        string name,
        List<string> triggerAspects,
        SensitivityTier sensitivity,
        Granularity granularity,
        int threshold,
        ReactionResultType resultType)
    {
        Name = name;
        TriggerAspects = triggerAspects;
        Sensitivity = sensitivity;
        Granularity = granularity;
        Threshold = threshold;
        ResultType = resultType;
    }
}
