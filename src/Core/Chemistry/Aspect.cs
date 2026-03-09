using System;

namespace AlchemistCourier.Core.Chemistry;

/// <summary>
/// A single alchemical aspect (property) that can be present on a reagent cell.
/// Aspects have an id, display name, school, and magnitude.
/// </summary>
public class Aspect : IEquatable<Aspect>
{
    /// <summary>Unique identifier (e.g., "hot", "cold", "volatile").</summary>
    public string Id { get; }

    /// <summary>Display name (e.g., "Hot", "Cold", "Volatile").</summary>
    public string Name { get; }

    /// <summary>Which school this aspect belongs to.</summary>
    public SchoolType School { get; }

    /// <summary>
    /// Magnitude — how "much" of this aspect is present. Default 1.
    /// Higher magnitude means stronger effect / more contribution to thresholds.
    /// </summary>
    public int Magnitude { get; }

    public Aspect(string id, string name, SchoolType school, int magnitude = 1)
    {
        Id = id;
        Name = name;
        School = school;
        Magnitude = magnitude;
    }

    /// <summary>Create a copy of this aspect with a different magnitude.</summary>
    public Aspect WithMagnitude(int magnitude) => new(Id, Name, School, magnitude);

    public bool Equals(Aspect? other) => other != null && Id == other.Id && Magnitude == other.Magnitude;
    public override bool Equals(object? obj) => obj is Aspect other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Id, Magnitude);
    public override string ToString() => Magnitude > 1 ? $"{Name}({Magnitude})" : Name;

    // --- Standard Physical/Chemistry aspects (MVP) ---

    public static Aspect Hot(int magnitude = 1) => new(AspectIds.Hot, "Hot", SchoolType.Physical, magnitude);
    public static Aspect Cold(int magnitude = 1) => new(AspectIds.Cold, "Cold", SchoolType.Physical, magnitude);
    public static Aspect Volatile(int magnitude = 1) => new(AspectIds.Volatile, "Volatile", SchoolType.Physical, magnitude);
    public static Aspect Fragile(int magnitude = 1) => new(AspectIds.Fragile, "Fragile", SchoolType.Physical, magnitude);
    public static Aspect Toxic(int magnitude = 1) => new(AspectIds.Toxic, "Toxic", SchoolType.Physical, magnitude);
    public static Aspect Heavy(int magnitude = 1) => new(AspectIds.Heavy, "Heavy", SchoolType.Physical, magnitude);
    public static Aspect Sharp(int magnitude = 1) => new(AspectIds.Sharp, "Sharp", SchoolType.Physical, magnitude);
    public static Aspect Medicinal(int magnitude = 1) => new(AspectIds.Medicinal, "Medicinal", SchoolType.Physical, magnitude);
    public static Aspect Stable(int magnitude = 1) => new(AspectIds.Stable, "Stable", SchoolType.Physical, magnitude);
    public static Aspect Catalytic(int magnitude = 1) => new(AspectIds.Catalytic, "Catalytic", SchoolType.Physical, magnitude);

    /// <summary>
    /// Creates an Aspect from its string ID (e.g., "hot", "cold", "volatile").
    /// Falls back to a generic Physical aspect for unknown IDs.
    /// </summary>
    public static Aspect FromId(string id) => id switch
    {
        AspectIds.Hot => Hot(),
        AspectIds.Cold => Cold(),
        AspectIds.Volatile => Volatile(),
        AspectIds.Fragile => Fragile(),
        AspectIds.Toxic => Toxic(),
        AspectIds.Sharp => Sharp(),
        AspectIds.Heavy => Heavy(),
        AspectIds.Medicinal => Medicinal(),
        AspectIds.Stable => Stable(),
        AspectIds.Catalytic => Catalytic(),
        _ => new Aspect(id, id, SchoolType.Physical),
    };
}
