using System;
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

    private Tween? _pulseTween;

    /// <summary>Number of rendered damage fill elements, for testing.</summary>
    public int RenderedCount => _fillRects.Count;

    /// <summary>Whether a pulse animation is currently playing.</summary>
    public bool IsPulsing => _pulseTween?.IsRunning() == true;

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
    /// Renders violation cells and pulses them 3 times (red flash) over 1 second,
    /// then calls onComplete. Used when the player confirms with active violations.
    /// </summary>
    public void PulseViolations(List<ViolationEntry> violations, Action? onComplete)
    {
        Render(violations);

        if (_fillRects.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        _pulseTween?.Kill();

        // Brighten all fill rects for each pulse
        var baseAlpha = Modulate.A;
        _pulseTween = CreateTween();
        _pulseTween.SetLoops(3);
        _pulseTween.TweenProperty(this, "modulate:a", 1.0f, 0.167f)
            .From(0.2f)
            .SetEase(Tween.EaseType.Out);
        _pulseTween.TweenProperty(this, "modulate:a", 0.2f, 0.167f)
            .SetEase(Tween.EaseType.In);

        _pulseTween.Finished += () =>
        {
            Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, baseAlpha);
            onComplete?.Invoke();
        };
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
