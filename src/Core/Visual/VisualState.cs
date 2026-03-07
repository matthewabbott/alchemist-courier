namespace AlchemistCourier.Core.Visual;

/// <summary>
/// Visual states for renderable game objects.
/// Determines which sprite sheet row / visual treatment to display.
/// </summary>
public enum VisualState
{
    /// <summary>Default appearance — no environmental influence.</summary>
    Normal,

    /// <summary>Adjacent to Hot aspect(s), no Cold — shows warmth indicators.</summary>
    Heated,

    /// <summary>Adjacent to Cold aspect(s), no Hot — shows frost indicators.</summary>
    Cooled,

    /// <summary>Adjacent to BOTH Hot and Cold — Fragile-specific danger state.</summary>
    ThermalStress,

    /// <summary>Heavy aspect directly above — Fragile-specific danger state.</summary>
    Crushed,

    /// <summary>Volatile reaction triggered — animated destruction.</summary>
    Exploding,

    /// <summary>Fragile breakage triggered — animated shattering.</summary>
    Breaking,
}
