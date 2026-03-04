namespace AlchemistCourier.Core.Chemistry;

/// <summary>
/// The type of consequence produced by a reaction.
/// </summary>
public enum ReactionResultType
{
    Explosion,
    Neutralization,
    Breakage,
    StatusEffect,
    Overheating,
    Hypothermia,
    Poisoning,
    ToxicCloud
}
