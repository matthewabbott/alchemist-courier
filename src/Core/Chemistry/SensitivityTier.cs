namespace AlchemistCourier.Core.Chemistry;

/// <summary>
/// How close the triggering aspects must be for a reaction to occur.
/// </summary>
public enum SensitivityTier
{
    /// <summary>Aspects must be on cardinally adjacent cells.</summary>
    Adjacent,

    /// <summary>Aspects must be in the same bag (any position).</summary>
    SameBag,

    /// <summary>Aspects can be anywhere across the whole inventory.</summary>
    WholeInventory
}
