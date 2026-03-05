using System.Collections.Generic;
using System.Linq;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Filters aspects, bag types, and reaction rules based on complexity tier.
/// See docs/chemistry-system.md for the full complexity model.
/// </summary>
public static class ComplexityFilter
{
    private static readonly string[] SimpleAspects =
    {
        AspectIds.Hot, AspectIds.Cold, AspectIds.Volatile,
        AspectIds.Stable, AspectIds.Heavy, AspectIds.Toxic,
    };

    private static readonly string[] IntermediateAspects =
    {
        AspectIds.Sharp, AspectIds.Fragile,
        AspectIds.Catalytic, AspectIds.Medicinal,
    };

    private static readonly string[] AdvancedAspects =
    {
        "magnetic", "absorbent",
    };

    private static readonly BagPropertyType[] SimpleBagTypes =
    {
        BagPropertyType.None, BagPropertyType.Null,
        BagPropertyType.Gravity, BagPropertyType.Locked,
    };

    private static readonly BagPropertyType[] IntermediateBagTypes =
    {
        BagPropertyType.Reinforced, BagPropertyType.Insulated,
        BagPropertyType.Fragile,
    };

    private static readonly BagPropertyType[] AdvancedBagTypes =
    {
        BagPropertyType.Levitating,
    };

    /// <summary>
    /// Returns the aspect IDs available at the given complexity level.
    /// Each tier includes all aspects from lower tiers.
    /// </summary>
    public static List<string> GetAspects(ComplexityLevel level)
    {
        var aspects = new List<string>(SimpleAspects);
        if (level >= ComplexityLevel.Intermediate)
            aspects.AddRange(IntermediateAspects);
        if (level >= ComplexityLevel.Advanced)
            aspects.AddRange(AdvancedAspects);
        return aspects;
    }

    /// <summary>
    /// Returns the bag property types available at the given complexity level.
    /// Each tier includes all bag types from lower tiers.
    /// </summary>
    public static List<BagPropertyType> GetBagTypes(ComplexityLevel level)
    {
        var types = new List<BagPropertyType>(SimpleBagTypes);
        if (level >= ComplexityLevel.Intermediate)
            types.AddRange(IntermediateBagTypes);
        if (level >= ComplexityLevel.Advanced)
            types.AddRange(AdvancedBagTypes);
        return types;
    }

    /// <summary>
    /// Returns only the reaction rules whose trigger aspects are all available
    /// at the given complexity level.
    /// </summary>
    public static List<ReactionRule> GetRules(ComplexityLevel level)
    {
        var availableAspects = new HashSet<string>(GetAspects(level));
        return ReactionRuleDatabase.GetMvpRules()
            .Where(r => r.TriggerAspects.All(a => availableAspects.Contains(a)))
            .ToList();
    }
}
