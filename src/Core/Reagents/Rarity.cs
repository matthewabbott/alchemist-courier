namespace AlchemistCourier.Core.Reagents;

/// <summary>
/// Rarity tier of a reagent. Correlates with shape complexity and aspect count.
/// See docs/core-mechanics.md for shape size guidelines per rarity.
/// </summary>
public enum Rarity
{
    Common,     // 1-2 cells, 1-2 aspects
    Uncommon,   // 2-3 cells, 2-3 aspects
    Rare,       // 3-4 cells, 3-4 aspects
    VeryRare,   // 4-6+ cells, 4+ aspects
    Quest,      // Variable — unique shapes and aspects
}
