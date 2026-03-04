namespace AlchemistCourier.Core.Chemistry;

/// <summary>
/// Whether a reaction counts individual cells or distinct reagent items.
/// </summary>
public enum Granularity
{
    /// <summary>Count individual cells with the aspect.</summary>
    PerCell,

    /// <summary>Count distinct reagents (items) with the aspect.</summary>
    PerItem
}
