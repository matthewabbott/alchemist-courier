namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Result of confirming/validating a puzzle attempt.
/// </summary>
public enum ConfirmResult
{
    None,
    NotAllPlaced,
    HasReactions,
    Win,
}
