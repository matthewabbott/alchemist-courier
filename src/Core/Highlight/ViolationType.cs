namespace AlchemistCourier.Core.Highlight;

/// <summary>
/// Categories of live placement violations (J.3).
/// These aggregate all possible issues a player might need to address.
/// </summary>
public enum ViolationType
{
    /// <summary>Hot/cold imbalance in a bag (I.2).</summary>
    BagTooHot,

    /// <summary>Cold/hot imbalance in a bag (I.2).</summary>
    BagTooCold,

    /// <summary>Fragile being crushed by a Heavy object (D.2).</summary>
    FragileCrushed,

    /// <summary>Fragile experiencing thermal fracture from hot+cold adjacency (D.2).</summary>
    FragileThermalFracture,

    /// <summary>Fragile adjacent to Sharp — piercing (existing).</summary>
    FragilePierced,

    /// <summary>Volatile reagent adjacent to net-hot — about to explode.</summary>
    VolatileExplosion,

    /// <summary>Cumulative overheating across whole inventory.</summary>
    Overheating,

    /// <summary>Cumulative hypothermia across whole inventory.</summary>
    Hypothermia,
}
