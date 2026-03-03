using System.Collections.Generic;
using System.Linq;

namespace AlchemistCourier.Core.Chemistry;

/// <summary>
/// A collection of aspects, typically representing all aspects on a single cell of a reagent.
/// Provides query methods for checking presence and summing magnitudes.
/// </summary>
public class AspectSet
{
    private readonly List<Aspect> _aspects;

    /// <summary>All aspects in this set.</summary>
    public IReadOnlyList<Aspect> Aspects => _aspects.AsReadOnly();

    /// <summary>Number of distinct aspects in this set.</summary>
    public int Count => _aspects.Count;

    /// <summary>Whether this set has no aspects.</summary>
    public bool IsEmpty => _aspects.Count == 0;

    public AspectSet()
    {
        _aspects = new List<Aspect>();
    }

    public AspectSet(IEnumerable<Aspect> aspects)
    {
        _aspects = new List<Aspect>(aspects);
    }

    public AspectSet(params Aspect[] aspects)
    {
        _aspects = new List<Aspect>(aspects);
    }

    /// <summary>Check if this set contains an aspect with the given id.</summary>
    public bool Has(string aspectId) => _aspects.Any(a => a.Id == aspectId);

    /// <summary>Get the first aspect with the given id, or null.</summary>
    public Aspect? Get(string aspectId) => _aspects.FirstOrDefault(a => a.Id == aspectId);

    /// <summary>Get the magnitude of a specific aspect (0 if not present).</summary>
    public int GetMagnitude(string aspectId)
    {
        var aspect = Get(aspectId);
        return aspect?.Magnitude ?? 0;
    }

    /// <summary>Sum of all aspect magnitudes in this set.</summary>
    public int TotalMagnitude() => _aspects.Sum(a => a.Magnitude);

    /// <summary>Get all aspects belonging to a specific school.</summary>
    public List<Aspect> GetBySchool(SchoolType school) =>
        _aspects.Where(a => a.School == school).ToList();

    /// <summary>Add an aspect to this set.</summary>
    public void Add(Aspect aspect) => _aspects.Add(aspect);

    /// <summary>Remove an aspect by id. Returns true if found and removed.</summary>
    public bool Remove(string aspectId) => _aspects.RemoveAll(a => a.Id == aspectId) > 0;

    /// <summary>Create an empty aspect set.</summary>
    public static AspectSet Empty => new();

    public override string ToString()
    {
        return _aspects.Count == 0
            ? "[none]"
            : $"[{string.Join(", ", _aspects.Select(a => a.ToString()))}]";
    }
}
