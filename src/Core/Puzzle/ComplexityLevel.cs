namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Controls mechanical depth — which aspects, bag types, and reaction rules are available.
/// Independent of scale/difficulty (a Hard daily puzzle uses Simple complexity).
/// See docs/chemistry-system.md for the full complexity model.
/// </summary>
public enum ComplexityLevel
{
    /// <summary>
    /// 6 aspects (Hot, Cold, Volatile, Stable, Heavy, Toxic).
    /// 4 bag types (Normal, Null, Gravity, Locked).
    /// Used for all standalone/daily puzzles regardless of difficulty.
    /// </summary>
    Simple,

    /// <summary>
    /// Adds 4 aspects (Sharp, Fragile, Catalytic, Medicinal).
    /// Adds 3 bag types (Reinforced, Insulated, Fragile).
    /// Used in sequential modes (roguelite mid-game, curated campaigns).
    /// </summary>
    Intermediate,

    /// <summary>
    /// Adds 2+ aspects (Magnetic, Absorbent).
    /// Adds bag types (Levitating).
    /// Used in roguelite late-game and special events.
    /// </summary>
    Advanced,
}
