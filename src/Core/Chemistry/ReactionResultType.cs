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
    ToxicCloud,

    /// <summary>
    /// D.2: Fragile breaks when adjacent to BOTH hot and cold cells simultaneously.
    /// </summary>
    ThermalFracture,

    /// <summary>
    /// D.2: Fragile breaks when a Heavy object's bearing cells rest directly on it.
    /// </summary>
    Crushing,
}
