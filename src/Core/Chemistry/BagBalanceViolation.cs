using AlchemistCourier.Core.Bags;

namespace AlchemistCourier.Core.Chemistry;

/// <summary>
/// Represents a hot/cold balance violation within a single bag.
/// Each non-null bag must have equal total hot and cold magnitudes.
/// </summary>
public class BagBalanceViolation
{
    /// <summary>The bag with the imbalance.</summary>
    public Bag Bag { get; }

    /// <summary>Total hot magnitude in this bag.</summary>
    public int HotTotal { get; }

    /// <summary>Total cold magnitude in this bag.</summary>
    public int ColdTotal { get; }

    /// <summary>
    /// Positive = too hot, negative = too cold.
    /// </summary>
    public int Imbalance => HotTotal - ColdTotal;

    public BagBalanceViolation(Bag bag, int hotTotal, int coldTotal)
    {
        Bag = bag;
        HotTotal = hotTotal;
        ColdTotal = coldTotal;
    }
}
