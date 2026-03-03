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

    public static Aspect Hot(int magnitude = 1) => new("hot", "Hot", SchoolType.Physical, magnitude);
    public static Aspect Cold(int magnitude = 1) => new("cold", "Cold", SchoolType.Physical, magnitude);
    public static Aspect Volatile(int magnitude = 1) => new("volatile", "Volatile", SchoolType.Physical, magnitude);
    public static Aspect Fragile(int magnitude = 1) => new("fragile", "Fragile", SchoolType.Physical, magnitude);
    public static Aspect Toxic(int magnitude = 1) => new("toxic", "Toxic", SchoolType.Physical, magnitude);
    public static Aspect Heavy(int magnitude = 1) => new("heavy", "Heavy", SchoolType.Physical, magnitude);
    public static Aspect Sharp(int magnitude = 1) => new("sharp", "Sharp", SchoolType.Physical, magnitude);
    public static Aspect Medicinal(int magnitude = 1) => new("medicinal", "Medicinal", SchoolType.Physical, magnitude);
    public static Aspect Stable(int magnitude = 1) => new("stable", "Stable", SchoolType.Physical, magnitude);
    public static Aspect Catalytic(int magnitude = 1) => new("catalytic", "Catalytic", SchoolType.Physical, magnitude);
}
