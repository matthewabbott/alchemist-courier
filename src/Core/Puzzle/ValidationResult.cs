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
    public IReadOnlyList<BagBalanceViolation> BalanceViolations { get; }
    public int Score { get; }

    private ValidationResult(
        ConfirmResult result,
        IReadOnlyList<ReactionResult> reactions,
        IReadOnlyList<BagBalanceViolation> balanceViolations,
        int score)
    {
        Result = result;
        Reactions = reactions;
        BalanceViolations = balanceViolations;
        Score = score;
    }

    public static ValidationResult NotAllPlaced(int unplacedCount)
    {
        return new ValidationResult(ConfirmResult.NotAllPlaced, new List<ReactionResult>(),
            new List<BagBalanceViolation>(), 0);
    }

    public static ValidationResult HasReactions(List<ReactionResult> reactions)
    {
        return new ValidationResult(ConfirmResult.HasReactions, reactions,
            new List<BagBalanceViolation>(), 0);
    }

    public static ValidationResult HotColdImbalance(List<BagBalanceViolation> violations)
    {
        return new ValidationResult(ConfirmResult.HotColdImbalance, new List<ReactionResult>(),
            violations, 0);
    }

    public static ValidationResult Win(int score = 0)
    {
        return new ValidationResult(ConfirmResult.Win, new List<ReactionResult>(),
            new List<BagBalanceViolation>(), score);
    }
}
