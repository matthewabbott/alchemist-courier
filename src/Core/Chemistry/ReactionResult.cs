using System.Collections.Generic;
using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Core.Chemistry;

/// <summary>
/// The outcome of a triggered reaction: what happened, how severe, where, and why.
/// </summary>
public class ReactionResult
{
    /// <summary>What type of reaction occurred.</summary>
    public ReactionResultType Type { get; }

    /// <summary>Severity/intensity of the reaction.</summary>
    public float Magnitude { get; }

    /// <summary>Grid cells affected by this reaction.</summary>
    public List<Vector2I> AffectedCells { get; }

    /// <summary>The rule that triggered this result.</summary>
    public ReactionRule SourceRule { get; }

    public ReactionResult(
        ReactionResultType type,
        float magnitude,
        List<Vector2I> affectedCells,
        ReactionRule sourceRule)
    {
        Type = type;
        Magnitude = magnitude;
        AffectedCells = affectedCells;
        SourceRule = sourceRule;
    }
}
