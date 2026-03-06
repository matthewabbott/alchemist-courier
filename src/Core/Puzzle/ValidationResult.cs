using System.Collections.Generic;
using AlchemistCourier.Core.Chemistry;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Result of evaluating a puzzle attempt through PuzzleValidator.
/// </summary>
public class ValidationResult
{
    public ConfirmResult Result { get; }
    public IReadOnlyList<ReactionResult> Reactions { get; }
    public int Score { get; }

    private ValidationResult(ConfirmResult result, IReadOnlyList<ReactionResult> reactions, int score)
    {
        Result = result;
        Reactions = reactions;
        Score = score;
    }

    public static ValidationResult NotAllPlaced(int unplacedCount)
    {
        return new ValidationResult(ConfirmResult.NotAllPlaced, new List<ReactionResult>(), 0);
    }

    public static ValidationResult HasReactions(List<ReactionResult> reactions)
    {
        return new ValidationResult(ConfirmResult.HasReactions, reactions, 0);
    }

    public static ValidationResult Win(int score = 0)
    {
        return new ValidationResult(ConfirmResult.Win, new List<ReactionResult>(), score);
    }
}
