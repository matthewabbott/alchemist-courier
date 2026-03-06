using System;
using System.Collections.Generic;
using System.Linq;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Evaluates puzzle win conditions for all PuzzleType values.
/// Pure C# — no Godot dependencies.
/// </summary>
public static class PuzzleValidator
{
    public static ValidationResult Evaluate(
        MetaGrid grid,
        IReadOnlyList<Reagent> trayReagents,
        PuzzleType puzzleType,
        List<ReactionRule> rules)
    {
        return puzzleType switch
        {
            PuzzleType.FitAllSafely => EvaluateFitAllSafely(grid, trayReagents, rules),
            PuzzleType.MaximizeValue => EvaluateMaximizeValue(grid, trayReagents, rules),
            PuzzleType.MeetRequirement => EvaluateMeetRequirement(grid, trayReagents, rules),
            _ => throw new ArgumentOutOfRangeException(nameof(puzzleType), puzzleType, "Unhandled PuzzleType"),
        };
    }

    private static ValidationResult EvaluateFitAllSafely(
        MetaGrid grid, IReadOnlyList<Reagent> trayReagents, List<ReactionRule> rules)
    {
        int unplacedCount = trayReagents.Count(r => !r.IsPlaced);
        if (unplacedCount > 0)
            return ValidationResult.NotAllPlaced(unplacedCount);

        var reactions = ChemistryEngine.Evaluate(grid, rules);
        if (reactions.Count > 0)
            return ValidationResult.HasReactions(reactions);

        return ValidationResult.Win();
    }

    private static ValidationResult EvaluateMaximizeValue(
        MetaGrid grid, IReadOnlyList<Reagent> trayReagents, List<ReactionRule> rules)
    {
        // MaximizeValue: skip unplaced check, evaluate reactions among placed reagents only
        var reactions = ChemistryEngine.Evaluate(grid, rules);
        if (reactions.Count > 0)
            return ValidationResult.HasReactions(reactions);

        int score = trayReagents.Where(r => r.IsPlaced).Sum(r => r.Definition.Value);
        return ValidationResult.Win(score);
    }

    private static ValidationResult EvaluateMeetRequirement(
        MetaGrid grid, IReadOnlyList<Reagent> trayReagents, List<ReactionRule> rules)
    {
        // TODO: Add requirement field check when MeetRequirement is fully implemented
        return EvaluateFitAllSafely(grid, trayReagents, rules);
    }
}
