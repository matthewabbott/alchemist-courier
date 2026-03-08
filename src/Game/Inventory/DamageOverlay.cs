using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Highlight;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Godot Control that renders tint overlays on cells affected by
/// damage violations (fragile crushed/fracture/pierced, volatile explosion).
/// Follows the HighlightOverlay pattern with pooled ColorRect children.
/// </summary>
public partial class DamageOverlay : Control
{
    private readonly List<ColorRect> _fillRects = new();

    /// <summary>Number of rendered damage fill elements, for testing.</summary>
    public int RenderedCount => _fillRects.Count;

    /// <summary>
    /// Render damage indicators for the given violation entries.
    /// Renders fragile and volatile explosion violation types; others are ignored.
    /// </summary>
    public void Render(List<ViolationEntry> violations)
    {
        Clear();

        var damagedPositions = new HashSet<CoreVec>();

        foreach (var violation in violations)
        {
            if (!IsDamageViolation(violation.Type))
                continue;

            foreach (var cell in violation.AffectedCells)
            {
                if (damagedPositions.Add(cell))
                {
                    var pixelPos = GridCoordinates.GridToPixel(cell);
                    AddDamageFill(pixelPos, GetDamageColor(violation.Type));
                }
            }
        }
    }

    /// <summary>
    /// Clears all rendered damage visuals.
    /// </summary>
    public void Clear()
    {
        foreach (var rect in _fillRects)
            rect.QueueFree();
        _fillRects.Clear();
    }

    private void AddDamageFill(Vector2 pixelPos, Color color)
    {
        float cs = GridConstants.CellSize;
        var rect = new ColorRect
        {
            Position = pixelPos,
            Size = new Vector2(cs, cs),
            Color = color,
            ZIndex = 1,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(rect);
        _fillRects.Add(rect);
    }

    private static bool IsDamageViolation(ViolationType type)
    {
        return type == ViolationType.FragileCrushed
            || type == ViolationType.FragileThermalFracture
            || type == ViolationType.FragilePierced
            || type == ViolationType.VolatileExplosion;
    }

    private static Color GetDamageColor(ViolationType type)
    {
        return type switch
        {
            ViolationType.FragileCrushed => new Color(0.9f, 0.15f, 0.15f, 0.35f),
            ViolationType.FragileThermalFracture => new Color(0.85f, 0.2f, 0.3f, 0.35f),
            ViolationType.FragilePierced => new Color(0.9f, 0.1f, 0.1f, 0.35f),
            ViolationType.VolatileExplosion => new Color(0.95f, 0.5f, 0.1f, 0.4f),
            _ => new Color(0.9f, 0.2f, 0.2f, 0.35f),
        };
    }
}
