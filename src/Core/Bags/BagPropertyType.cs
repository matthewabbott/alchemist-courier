namespace AlchemistCourier.Core.Bags;

/// <summary>
/// Special properties that a bag can have, affecting reagents stored within it.
/// See docs/core-mechanics.md for detailed descriptions of each property.
/// </summary>
public enum BagPropertyType
{
    /// <summary>No special property — a normal bag.</summary>
    None,

    /// <summary>Suppresses/nullifies aspects of reagents stored in this bag's cells.</summary>
    Null,

    /// <summary>Reagents cannot be moved or removed after placement.</summary>
    Locked,

    /// <summary>Reagents "fall" to the bottom — top cells must be supported.</summary>
    Gravity,

    /// <summary>Resistant to damage from explosions and other destructive effects.</summary>
    Reinforced,

    /// <summary>Easily damaged — explosions may destroy cells more readily.</summary>
    Fragile,

    /// <summary>Raises reaction thresholds for reagents in this bag (harder to trigger reactions).</summary>
    Insulated,

    /// <summary>Can be placed anywhere on the meta-grid (doesn't need adjacency to character).</summary>
    Levitating,
}
