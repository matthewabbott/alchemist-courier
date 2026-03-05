using System.Collections.Generic;
using System.Linq;
using AlchemistCourier.Core.Puzzle;

namespace AlchemistCourier.Core.Chemistry;

/// <summary>
/// Static provider of reaction rules for the MVP Physical/Chemistry school.
/// Rules are derived from docs/chemistry-system.md.
/// </summary>
public static class ReactionRuleDatabase
{
    /// <summary>
    /// Returns all MVP reaction rules for the Physical/Chemistry school.
    /// </summary>
    public static List<ReactionRule> GetMvpRules()
    {
        return new List<ReactionRule>
        {
            // Volatile net-hot explosion: for each Volatile cell, sum adjacent Hot - Cold magnitudes
            new ReactionRule(
                name: "Explosion",
                triggerAspects: new List<string> { AspectIds.Volatile },
                sensitivity: SensitivityTier.Adjacent,
                granularity: Granularity.PerCell,
                threshold: 0,
                resultType: ReactionResultType.Explosion),

            // Hot + Cold (same bag) = Neutralization
            new ReactionRule(
                name: "Neutralization",
                triggerAspects: new List<string> { AspectIds.Hot, AspectIds.Cold },
                sensitivity: SensitivityTier.SameBag,
                granularity: Granularity.PerItem,
                threshold: 0,
                resultType: ReactionResultType.Neutralization),

            // Sharp + Fragile (adjacent) = Breakage
            new ReactionRule(
                name: "Breakage",
                triggerAspects: new List<string> { AspectIds.Sharp, AspectIds.Fragile },
                sensitivity: SensitivityTier.Adjacent,
                granularity: Granularity.PerCell,
                threshold: 0,
                resultType: ReactionResultType.Breakage),

            // Toxic + Toxic (same bag) = Toxic Cloud
            new ReactionRule(
                name: "Toxic Cloud",
                triggerAspects: new List<string> { AspectIds.Toxic, AspectIds.Toxic },
                sensitivity: SensitivityTier.SameBag,
                granularity: Granularity.PerItem,
                threshold: 2,
                resultType: ReactionResultType.ToxicCloud),

            // Cumulative Hot > threshold = Overheating
            new ReactionRule(
                name: "Overheating",
                triggerAspects: new List<string> { AspectIds.Hot },
                sensitivity: SensitivityTier.WholeInventory,
                granularity: Granularity.PerCell,
                threshold: 4,
                resultType: ReactionResultType.Overheating),

            // Cumulative Cold > threshold = Hypothermia
            new ReactionRule(
                name: "Hypothermia",
                triggerAspects: new List<string> { AspectIds.Cold },
                sensitivity: SensitivityTier.WholeInventory,
                granularity: Granularity.PerCell,
                threshold: 4,
                resultType: ReactionResultType.Hypothermia),

            // Cumulative Toxic > threshold = Poisoning
            new ReactionRule(
                name: "Poisoning",
                triggerAspects: new List<string> { AspectIds.Toxic },
                sensitivity: SensitivityTier.WholeInventory,
                granularity: Granularity.PerCell,
                threshold: 3,
                resultType: ReactionResultType.Poisoning),
        };
    }

    /// <summary>
    /// Returns only rules whose trigger aspects are all available at the given complexity level.
    /// </summary>
    public static List<ReactionRule> GetRulesForComplexity(ComplexityLevel level)
    {
        return ComplexityFilter.GetRules(level);
    }

    /// <summary>
    /// Returns all rules whose trigger aspects match the given pair of aspect IDs.
    /// A rule matches if its TriggerAspects are a subset of (or equal to) the given pair.
    /// For single-aspect rules (cumulative), matches if the aspect is one of the given pair.
    /// For same-aspect rules (e.g., toxic+toxic), matches only if both IDs are the same.
    /// </summary>
    public static List<ReactionRule> GetRulesForAspects(string aspect1, string aspect2)
    {
        var rules = GetMvpRules();
        var result = new List<ReactionRule>();
        var querySet = new HashSet<string> { aspect1, aspect2 };

        foreach (var rule in rules)
        {
            if (rule.TriggerAspects.Count == 1)
            {
                // Single-aspect cumulative rule: matches if either aspect matches
                if (aspect1 == rule.TriggerAspects[0] || aspect2 == rule.TriggerAspects[0])
                {
                    result.Add(rule);
                }
            }
            else if (rule.TriggerAspects.Count == 2)
            {
                if (rule.TriggerAspects[0] == rule.TriggerAspects[1])
                {
                    // Same-aspect rule (e.g., toxic+toxic): both query aspects must match
                    if (aspect1 == rule.TriggerAspects[0] && aspect2 == rule.TriggerAspects[0])
                    {
                        result.Add(rule);
                    }
                }
                else
                {
                    // Distinct-aspect rule: query pair must contain both trigger aspects
                    if (querySet.IsSupersetOf(rule.TriggerAspects))
                    {
                        result.Add(rule);
                    }
                }
            }
        }

        return result;
    }
}
