using System.Collections.Generic;
using System.Linq;
using AlchemistCourier.Core.Chemistry;

namespace AlchemistCourier.Core.Reagents;

/// <summary>
/// Canonical reagent catalog: ~24 unique items, each with one natural polyomino shape.
/// Each item maps to exactly one aspect. Items are designed art-first with unique identities.
/// </summary>
public static class ReagentCatalog
{
    /// <summary>Standard shape names in catalog order.</summary>
    public static readonly string[] ShapeNames =
    {
        "monomino", "domino", "i_tri", "l_tri", "i_tet", "o_tet", "t_tet", "s_tet", "l_tet"
    };

    /// <summary>Standard shape matrices matching ShapeNames order.</summary>
    private static readonly Dictionary<string, bool[,]> ShapeMatricesByName = new()
    {
        ["monomino"] = new bool[,] { { true } },
        ["domino"]   = new bool[,] { { true, true } },
        ["i_tri"]    = new bool[,] { { true, true, true } },
        ["l_tri"]    = new bool[,] { { true, false }, { true, true } },
        ["i_tet"]    = new bool[,] { { true }, { true }, { true }, { true } },
        ["o_tet"]    = new bool[,] { { true, true }, { true, true } },
        ["t_tet"]    = new bool[,] { { true, true, true }, { false, true, false } },
        ["s_tet"]    = new bool[,] { { false, true }, { true, true }, { true, false } },
        ["l_tet"]    = new bool[,] { { true, false }, { true, false }, { true, true } },
    };

    /// <summary>Item definitions: (id, displayName, shapeName, aspectId, flavorText).</summary>
    private static readonly (string id, string name, string shape, string aspectId, string flavor)[] Items =
    {
        // Hot (3 items)
        ("ember_seed",     "Ember Seed",      "monomino", AspectIds.Hot,      "A smoldering seed that radiates gentle warmth. Handle with tongs."),
        ("firevine",       "Firevine",         "domino",   AspectIds.Hot,      "A climbing vine that perpetually smolders, leaving scorch marks wherever it grows."),
        ("magma_slug",     "Magma Slug",       "l_tri",    AspectIds.Hot,      "A slow-moving alchemical slug whose trail glows like cooling lava."),

        // Cold (3 items)
        ("frozen_teardrop","Frozen Teardrop",  "monomino", AspectIds.Cold,     "A tear shed by a frost spirit, permanently frozen mid-fall."),
        ("snowflower",     "Snowflower",       "domino",   AspectIds.Cold,     "A blue snowflake-shaped bloom that never thaws, even in summer."),
        ("frost_fern",     "Frost Fern",       "i_tri",    AspectIds.Cold,     "Delicate frozen fronds that grow along windowpanes in alchemist's workshops."),

        // Volatile (3 items)
        ("spark_petal",    "Spark Petal",      "monomino", AspectIds.Volatile, "A single petal that sparks when disturbed. Keep away from heat sources."),
        ("burstbloom",     "Burstbloom",       "l_tri",    AspectIds.Volatile, "A bloom from the foothills that detonates when warmed. Prized for controlled demolition."),
        ("thunder_egg",    "Thunder Egg",      "o_tet",    AspectIds.Volatile, "A mineral egg that cracks with tremendous force when its internal pressure is released."),

        // Toxic (3 items)
        ("venom_drop",     "Venom Drop",       "monomino", AspectIds.Toxic,    "Concentrated essence from a venomous bloom. One drop is enough."),
        ("purple_creeper", "Purple Creeper",   "domino",   AspectIds.Toxic,    "A venomous trailing plant with poisonous sap that stains everything purple."),
        ("blight_moss",    "Blight Moss",      "t_tet",    AspectIds.Toxic,    "A creeping moss that poisons the soil wherever it takes root. Spreads aggressively."),

        // Heavy (3 items)
        ("lead_nugget",    "Lead Nugget",      "monomino", AspectIds.Heavy,    "A small lump of alchemical lead, deceptively heavy for its size."),
        ("lead_ingot",     "Lead Ingot",       "domino",   AspectIds.Heavy,    "Dense alchemical metal, prized for its weight in counterbalancing reactions."),
        ("anvil_shard",    "Anvil Shard",      "l_tet",    AspectIds.Heavy,    "A broken piece of an alchemist's anvil. Still useful as ballast."),

        // Stable (3 items)
        ("calm_stone",     "Calm Stone",       "monomino", AspectIds.Stable,   "A river stone imbued with stabilizing essence. Soothes nearby reactions."),
        ("nullflower",     "Nullflower",       "domino",   AspectIds.Stable,   "A calm blossom that absorbs reactive energy from its surroundings."),
        ("ironbark_root",  "Ironbark Root",    "i_tri",    AspectIds.Stable,   "A section of ironbark tree root. Its steadiness dampens chemical volatility."),

        // Sharp (3 items)
        ("glass_shard",    "Glass Shard",      "monomino", AspectIds.Sharp,    "A razor-sharp sliver of alchemical glass. Cuts through almost anything."),
        ("sharpstone",     "Sharpstone",       "domino",   AspectIds.Sharp,    "Jagged crystalline mineral with cutting edges that never dull."),
        ("razor_fern",     "Razor Fern",       "s_tet",    AspectIds.Sharp,    "A fern whose fronds are edged like saw blades. Even the wind avoids it."),

        // Fragile (3 items)
        ("crystal_tear",   "Crystal Tear",     "monomino", AspectIds.Fragile,  "A hollow crystal that shatters at the slightest pressure. Beautiful but doomed."),
        ("glasswork",      "Glasswork",        "domino",   AspectIds.Fragile,  "Delicate blown glass containing trapped starlight. Handle with extreme care."),
        ("gossamer_thread","Gossamer Thread",  "i_tet",    AspectIds.Fragile,  "Spun from spider silk and moonlight. Stronger than it looks, but not by much."),
    };

    private static readonly Dictionary<string, ReagentDefinition> _byId;
    private static readonly Dictionary<string, List<ReagentDefinition>> _byAspect;
    private static readonly Dictionary<string, List<ReagentDefinition>> _byShape;
    private static readonly List<ReagentDefinition> _all;

    static ReagentCatalog()
    {
        _byId = new Dictionary<string, ReagentDefinition>();
        _byAspect = new Dictionary<string, List<ReagentDefinition>>();
        _byShape = new Dictionary<string, List<ReagentDefinition>>();
        _all = new List<ReagentDefinition>();

        foreach (var (itemId, displayName, shapeName, aspectId, flavor) in Items)
        {
            bool[,] shape = CloneShape(ShapeMatricesByName[shapeName]);

            int cellCount = ShapeUtil.CellCount(shape);
            var aspect = Aspect.FromId(aspectId);
            var aspectSet = new AspectSet(new[] { aspect });
            var aspectsPerCell = new List<AspectSet>();
            for (int c = 0; c < cellCount; c++)
                aspectsPerCell.Add(aspectSet);

            var rarity = cellCount <= 2 ? Rarity.Common
                       : cellCount == 3 ? Rarity.Uncommon
                       : Rarity.Rare;

            var def = new ReagentDefinition(
                id: itemId,
                name: displayName,
                shape: shape,
                weight: cellCount,
                value: cellCount,
                rarity: rarity,
                aspectsPerCell: aspectsPerCell,
                flavorText: flavor
            );

            _byId[itemId] = def;
            _all.Add(def);

            if (!_byAspect.ContainsKey(aspectId))
                _byAspect[aspectId] = new List<ReagentDefinition>();
            _byAspect[aspectId].Add(def);

            if (!_byShape.ContainsKey(shapeName))
                _byShape[shapeName] = new List<ReagentDefinition>();
            _byShape[shapeName].Add(def);
        }
    }

    /// <summary>Get all catalog definitions.</summary>
    public static IReadOnlyList<ReagentDefinition> GetAll() => _all;

    /// <summary>Get a definition by its full ID (e.g., "firevine"). Returns null if not found.</summary>
    public static ReagentDefinition? GetById(string id)
    {
        return _byId.TryGetValue(id, out var def) ? def : null;
    }

    /// <summary>Get all definitions for a given aspect.</summary>
    public static IReadOnlyList<ReagentDefinition> GetByAspect(string aspectId)
    {
        return _byAspect.TryGetValue(aspectId, out var list) ? list : new List<ReagentDefinition>();
    }

    /// <summary>Get all definitions for a given shape name.</summary>
    public static IReadOnlyList<ReagentDefinition> GetByShape(string shapeName)
    {
        return _byShape.TryGetValue(shapeName, out var list) ? list : new List<ReagentDefinition>();
    }

    /// <summary>Get the shape matrix for a given shape name. Returns null if not found.</summary>
    public static bool[,]? GetShapeMatrix(string shapeName)
    {
        return ShapeMatricesByName.TryGetValue(shapeName, out var shape) ? CloneShape(shape) : null;
    }

    /// <summary>Look up the matching catalog entry for a given aspect and shape matrix. Returns null if no match.</summary>
    public static ReagentDefinition? FindMatch(string aspectId, bool[,] shape)
    {
        if (!_byAspect.TryGetValue(aspectId, out var candidates))
            return null;

        int targetRows = shape.GetLength(0);
        int targetCols = shape.GetLength(1);

        foreach (var def in candidates)
        {
            if (def.ShapeRows != targetRows || def.ShapeCols != targetCols)
                continue;

            bool match = true;
            for (int r = 0; r < targetRows && match; r++)
                for (int c = 0; c < targetCols && match; c++)
                    if (def.Shape[r, c] != shape[r, c])
                        match = false;

            if (match)
                return def;
        }

        return null;
    }

    /// <summary>
    /// Get the shape name for a given item definition.
    /// Looks up by matching the definition's shape matrix against known shapes.
    /// Returns null if the shape doesn't match any standard shape.
    /// </summary>
    public static string? GetShapeName(ReagentDefinition def)
    {
        foreach (var shapeName in ShapeNames)
        {
            var canonical = ShapeMatricesByName[shapeName];
            if (canonical.GetLength(0) != def.ShapeRows || canonical.GetLength(1) != def.ShapeCols)
                continue;

            bool match = true;
            for (int r = 0; r < def.ShapeRows && match; r++)
                for (int c = 0; c < def.ShapeCols && match; c++)
                    if (canonical[r, c] != def.Shape[r, c])
                        match = false;

            if (match)
                return shapeName;
        }
        return null;
    }

    private static bool[,] CloneShape(bool[,] source)
    {
        int rows = source.GetLength(0);
        int cols = source.GetLength(1);
        var clone = new bool[rows, cols];
        System.Array.Copy(source, clone, source.Length);
        return clone;
    }
}
