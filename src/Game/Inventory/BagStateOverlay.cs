using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Godot Control that renders animated thermal tint overlays on bag cells
/// to indicate hot/cold imbalance. Too hot = red/orange flicker, too cold = blue flicker.
/// Balanced bags have no overlay.
/// </summary>
public partial class BagStateOverlay : Control
{
    private static readonly Color HotTintColor = new(0.9f, 0.25f, 0.1f);
    private static readonly Color ColdTintColor = new(0.15f, 0.35f, 0.9f);
    private const float PulsePeriod = 2.0f;
    private const float AlphaMin = 0.08f;
    private const float AlphaMax = 0.25f;

    private readonly List<ColorRect> _tintRects = new();
    private readonly List<BagTintGroup> _groups = new();
    private float _elapsed;

    /// <summary>Number of rendered tint elements, for testing.</summary>
    public int RenderedCount => _tintRects.Count;

    /// <summary>Number of bags with active thermal tint, for testing.</summary>
    public int ActiveGroupCount => _groups.Count;

    /// <summary>
    /// Render thermal tint overlays for bags with hot/cold imbalance.
    /// Call on every placement/removal to update.
    /// </summary>
    public void Render(List<BagBalanceViolation> violations)
    {
        Clear();

        foreach (var violation in violations)
        {
            bool isHot = violation.Imbalance > 0;
            var baseColor = isHot ? HotTintColor : ColdTintColor;
            var rects = new List<ColorRect>();

            foreach (var cell in violation.Bag.OwnedCells)
            {
                var pixelPos = GridCoordinates.GridToPixel(cell.Position);
                var rect = CreateTintRect(pixelPos, baseColor);
                AddChild(rect);
                _tintRects.Add(rect);
                rects.Add(rect);
            }

            _groups.Add(new BagTintGroup(baseColor, rects));
        }
    }

    /// <summary>
    /// Clears all rendered tint visuals.
    /// </summary>
    public void Clear()
    {
        foreach (var rect in _tintRects)
            rect.QueueFree();
        _tintRects.Clear();
        _groups.Clear();
    }

    public override void _Process(double delta)
    {
        if (_groups.Count == 0)
            return;

        _elapsed += (float)delta;

        float t = Mathf.Sin(_elapsed * Mathf.Tau / PulsePeriod);
        float alpha = AlphaMin + (AlphaMax - AlphaMin) * (t * 0.5f + 0.5f);

        foreach (var group in _groups)
        {
            var color = new Color(group.BaseColor.R, group.BaseColor.G, group.BaseColor.B, alpha);
            foreach (var rect in group.Rects)
            {
                rect.Color = color;
            }
        }
    }

    private static ColorRect CreateTintRect(Godot.Vector2 pixelPos, Color baseColor)
    {
        float cs = GridConstants.CellSize;
        return new ColorRect
        {
            Position = pixelPos,
            Size = new Godot.Vector2(cs, cs),
            Color = new Color(baseColor.R, baseColor.G, baseColor.B, AlphaMin),
            ZIndex = 1,
            MouseFilter = MouseFilterEnum.Ignore,
        };
    }

    private readonly record struct BagTintGroup(Color BaseColor, List<ColorRect> Rects);
}
