using System.Collections.Generic;
using System.Linq;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Chemistry;

/// <summary>
/// Static database of aspect metadata for all known aspects.
/// Used by the legend panel and tooltips.
/// </summary>
public static class AspectMetadataDatabase
{
    private static readonly Dictionary<string, AspectMetadata> Entries = BuildEntries();

    private static Dictionary<string, AspectMetadata> BuildEntries()
    {
        var list = new AspectMetadata[]
        {
            new(AspectIds.Hot, "Hot", "Generates heat that can trigger explosions and cause overheating.", SchoolType.Physical),
            new(AspectIds.Cold, "Cold", "Generates cold that can cause hypothermia but shields against heat near Volatile.", SchoolType.Physical),
            new(AspectIds.Volatile, "Volatile", "Explodes when adjacent net heat is positive.", SchoolType.Physical),
            new(AspectIds.Stable, "Stable", "Raises thresholds of adjacent reactions, acting as a universal buffer.", SchoolType.Physical),
            new(AspectIds.Heavy, "Heavy", "Falls to the bottom of Gravity bags; must be below non-Heavy reagents.", SchoolType.Physical,
                constraintText: "Must be at or below non-Heavy reagents in Gravity bags."),
            new(AspectIds.Toxic, "Toxic", "Contributes to cumulative poisoning; two Toxic items in the same bag create a cloud.", SchoolType.Physical),
            new(AspectIds.Sharp, "Sharp", "Cuts through containers and breaks fragile items on contact.", SchoolType.Physical,
                constraintText: "Cannot be placed on edge cells of non-Reinforced bags."),
            new(AspectIds.Fragile, "Fragile", "Breaks from crushing (heavy resting on it), thermal fracture (adjacent hot and cold), piercing (sharp contact), or adjacent volatile reactions.", SchoolType.Physical),
            new(AspectIds.Catalytic, "Catalytic", "Lowers thresholds of adjacent reactions, making them easier to trigger.", SchoolType.Physical),
            new(AspectIds.Medicinal, "Medicinal", "Passively raises cumulative thresholds across the whole inventory.", SchoolType.Physical),
        };

        var dict = new Dictionary<string, AspectMetadata>(list.Length);
        foreach (var entry in list)
            dict[entry.Id] = entry;
        return dict;
    }

    /// <summary>
    /// Returns the metadata for a specific aspect, or null if not found.
    /// </summary>
    public static AspectMetadata? Get(string aspectId)
    {
        return Entries.TryGetValue(aspectId, out var metadata) ? metadata : null;
    }

    /// <summary>
    /// Returns metadata for all known aspects.
    /// </summary>
    public static IReadOnlyList<AspectMetadata> GetAll()
    {
        return Entries.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Returns metadata for only the aspects present in the given reagents
    /// or referenced by the given reaction rules.
    /// </summary>
    public static IReadOnlyList<AspectMetadata> GetForPuzzle(List<ReactionRule> rules, List<Reagent> reagents)
    {
        var relevantIds = new HashSet<string>();

        // Collect aspects from reaction rule triggers
        foreach (var rule in rules)
        {
            foreach (var aspectId in rule.TriggerAspects)
                relevantIds.Add(aspectId);
        }

        // Collect aspects from reagent cells
        foreach (var reagent in reagents)
        {
            foreach (var aspectSet in reagent.Definition.AspectsPerCell)
            {
                foreach (var aspect in aspectSet.Aspects)
                    relevantIds.Add(aspect.Id);
            }
        }

        var result = new List<AspectMetadata>();
        foreach (var id in relevantIds)
        {
            var metadata = Get(id);
            if (metadata != null)
                result.Add(metadata);
        }

        return result.AsReadOnly();
    }
}
