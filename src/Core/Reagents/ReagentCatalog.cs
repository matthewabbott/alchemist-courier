using System.Collections.Generic;
using System.Linq;
using AlchemistCourier.Core.Chemistry;

namespace AlchemistCourier.Core.Reagents;

/// <summary>
/// Canonical reagent catalog: 8 species × 9 shapes = 72 single-aspect reagent definitions.
/// Each species maps to exactly one aspect. Each shape is a standard polyomino variant.
/// </summary>
public static class ReagentCatalog
{
    /// <summary>Standard shape names in catalog order.</summary>
    public static readonly string[] ShapeNames =
    {
        "monomino", "domino", "i_tri", "l_tri", "i_tet", "o_tet", "t_tet", "s_tet", "l_tet"
    };

    /// <summary>Standard shape matrices matching ShapeNames order.</summary>
    private static readonly bool[][,] ShapeMatrices =
    {
        // monomino (1 cell)
        new bool[,] { { true } },
        // domino (2 cells)
        new bool[,] { { true, true } },
        // i_tri - I-bar triomino (3 cells)
        new bool[,] { { true, true, true } },
        // l_tri - L-bend triomino (3 cells)
        new bool[,] { { true, false }, { true, true } },
        // i_tet - I tetromino (4 cells)
        new bool[,] { { true }, { true }, { true }, { true } },
        // o_tet - O tetromino / square (4 cells)
        new bool[,] { { true, true }, { true, true } },
        // t_tet - T tetromino (4 cells)
        new bool[,] { { true, true, true }, { false, true, false } },
        // s_tet - S tetromino (4 cells)
        new bool[,] { { false, true }, { true, true }, { true, false } },
        // l_tet - L tetromino (4 cells)
        new bool[,] { { true, false }, { true, false }, { true, true } },
    };

    /// <summary>Species definitions: (speciesId, displayName, aspectId, flavorText).</summary>
    private static readonly (string id, string name, string aspectId, string flavor)[] Species =
    {
        ("firevine",   "Firevine",       AspectIds.Hot,      "A climbing vine that perpetually smolders."),
        ("snowflower", "Snowflower",     AspectIds.Cold,     "A blue snowflake-shaped bloom that never thaws."),
        ("burstbloom", "Burstbloom",     AspectIds.Volatile, "A heat-sensitive alchemical bloom from foothills."),
        ("creeper",    "Purple Creeper", AspectIds.Toxic,    "A venomous trailing plant with poisonous sap."),
        ("leadingot",  "Lead Ingot",     AspectIds.Heavy,    "Dense alchemical metal, prized for its weight."),
        ("nullflower", "Nullflower",     AspectIds.Stable,   "A calm, dampening blossom that soothes reactions."),
        ("sharpstone", "Sharpstone",     AspectIds.Sharp,    "Jagged crystalline mineral with cutting edges."),
        ("glass",      "Glasswork",      AspectIds.Fragile,  "Delicate blown glass, beautiful but vulnerable."),
    };

    /// <summary>Display names for shapes (for UI/tooltip use).</summary>
    private static readonly Dictionary<string, string> ShapeDisplayNames = new()
    {
        ["monomino"] = "Monomino",
        ["domino"]   = "Domino",
        ["i_tri"]    = "I-Bar",
        ["l_tri"]    = "L-Bend",
        ["i_tet"]    = "I-Piece",
        ["o_tet"]    = "Square",
        ["t_tet"]    = "T-Piece",
        ["s_tet"]    = "S-Piece",
        ["l_tet"]    = "L-Piece",
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

        foreach (var (speciesId, speciesName, aspectId, flavor) in Species)
        {
            for (int i = 0; i < ShapeNames.Length; i++)
            {
                string shapeName = ShapeNames[i];
                string id = $"{speciesId}_{shapeName}";
                string displayName = $"{speciesName} {ShapeDisplayNames[shapeName]}";
                bool[,] shape = CloneShape(ShapeMatrices[i]);

                int cellCount = ShapeUtil.CellCount(shape);
                var aspect = Aspect.FromId(aspectId);
                var aspectSet = new AspectSet(new[] { aspect });
                var aspectsPerCell = new List<AspectSet>();
                for (int c = 0; c < cellCount; c++)
                    aspectsPerCell.Add(aspectSet);

                var def = new ReagentDefinition(
                    id: id,
                    name: displayName,
                    shape: shape,
                    weight: cellCount,
                    value: cellCount,
                    rarity: cellCount <= 2 ? Rarity.Common : Rarity.Uncommon,
                    aspectsPerCell: aspectsPerCell,
                    flavorText: flavor
                );

                _byId[id] = def;
                _all.Add(def);

                if (!_byAspect.ContainsKey(aspectId))
                    _byAspect[aspectId] = new List<ReagentDefinition>();
                _byAspect[aspectId].Add(def);

                if (!_byShape.ContainsKey(shapeName))
                    _byShape[shapeName] = new List<ReagentDefinition>();
                _byShape[shapeName].Add(def);
            }
        }
    }

    /// <summary>Get all 72 catalog definitions.</summary>
    public static IReadOnlyList<ReagentDefinition> GetAll() => _all;

    /// <summary>Get a specific definition by species and shape name. Returns null if not found.</summary>
    public static ReagentDefinition? Get(string speciesId, string shapeName)
    {
        string id = $"{speciesId}_{shapeName}";
        return _byId.TryGetValue(id, out var def) ? def : null;
    }

    /// <summary>Get a definition by its full ID (e.g., "firevine_domino"). Returns null if not found.</summary>
    public static ReagentDefinition? GetById(string id)
    {
        return _byId.TryGetValue(id, out var def) ? def : null;
    }

    /// <summary>Get all definitions for a given aspect (e.g., AspectIds.Hot returns all 9 Firevine shapes).</summary>
    public static IReadOnlyList<ReagentDefinition> GetByAspect(string aspectId)
    {
        return _byAspect.TryGetValue(aspectId, out var list) ? list : new List<ReagentDefinition>();
    }

    /// <summary>Get all definitions for a given shape name (e.g., "domino" returns 8 species).</summary>
    public static IReadOnlyList<ReagentDefinition> GetByShape(string shapeName)
    {
        return _byShape.TryGetValue(shapeName, out var list) ? list : new List<ReagentDefinition>();
    }

    /// <summary>Get the shape matrix for a given shape name. Returns null if not found.</summary>
    public static bool[,]? GetShapeMatrix(string shapeName)
    {
        int idx = System.Array.IndexOf(ShapeNames, shapeName);
        return idx >= 0 ? CloneShape(ShapeMatrices[idx]) : null;
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

    private static bool[,] CloneShape(bool[,] source)
    {
        int rows = source.GetLength(0);
        int cols = source.GetLength(1);
        var clone = new bool[rows, cols];
        System.Array.Copy(source, clone, source.Length);
        return clone;
    }
}
