namespace AlchemistCourier.Core.Chemistry;

/// <summary>
/// Extended metadata for an aspect, used by the legend panel and tooltips.
/// </summary>
public class AspectMetadata
{
    /// <summary>Aspect identifier (matches AspectIds constants).</summary>
    public string Id { get; }

    /// <summary>Human-readable display name (e.g., "Hot").</summary>
    public string Name { get; }

    /// <summary>One-sentence description of what this aspect does.</summary>
    public string Description { get; }

    /// <summary>
    /// Placement constraint text (e.g., "Cannot place on bag edges" for Sharp).
    /// Null for aspects with no placement constraints.
    /// </summary>
    public string? ConstraintText { get; }

    /// <summary>Which school this aspect belongs to.</summary>
    public SchoolType School { get; }

    public AspectMetadata(string id, string name, string description, SchoolType school, string? constraintText = null)
    {
        Id = id;
        Name = name;
        Description = description;
        School = school;
        ConstraintText = constraintText;
    }
}
