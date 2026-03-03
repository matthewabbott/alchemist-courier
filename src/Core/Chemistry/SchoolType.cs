namespace AlchemistCourier.Core.Chemistry;

/// <summary>
/// The "school" (tradition/category) an aspect belongs to.
/// Each school is a thematic set of aspects with its own internal logic.
/// See docs/chemistry-system.md for detailed school descriptions.
/// </summary>
public enum SchoolType
{
    /// <summary>
    /// Physical/Chemistry school (MVP first).
    /// Hot, Cold, Volatile, Fragile, Toxic, Heavy, Sharp, Medicinal, Stable, Catalytic.
    /// </summary>
    Physical,

    /// <summary>
    /// Wuxing / Five Elements (post-MVP).
    /// Wood, Fire, Earth, Metal, Water with generating/overcoming cycles.
    /// </summary>
    Wuxing,

    /// <summary>
    /// Western Alchemy / The Great Work (post-MVP).
    /// Nigredo, Albedo, Citrinitas, Rubedo — stages of the Magnum Opus.
    /// </summary>
    WesternAlchemy,

    /// <summary>
    /// Astrology (post-MVP, TBD).
    /// Planetary associations, zodiacal affinities.
    /// </summary>
    Astrology,
}
