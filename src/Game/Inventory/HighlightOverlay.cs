using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Highlight;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Godot Control that renders a HighlightSet visually using ColorRect children.
/// Edge highlights are thin colored lines along cell edges; cell fills are semi-transparent rectangles.
/// </summary>
public partial class HighlightOverlay : Control
{
    private readonly List<ColorRect> _edgeRects = new();
    private readonly List<ColorRect> _fillRects = new();
    private ColorRect? _dimOverlay;

    private static readonly Dictionary<HighlightSeverity, float> SeverityAlpha = new()
    {
        { HighlightSeverity.Danger, 0.9f },
        { HighlightSeverity.Warning, 0.65f },
        { HighlightSeverity.Safe, 0.4f },
        { HighlightSeverity.Info, 0.5f },
    };

    /// <summary>Number of rendered visual elements (edges + fills), for testing.</summary>
    public int RenderedCount => _edgeRects.Count + _fillRects.Count;

    /// <summary>Whether the dim overlay is currently visible.</summary>
    public bool IsDimmed => _dimOverlay != null && _dimOverlay.Visible;

    /// <summary>
    /// Clears existing visuals and renders the given HighlightSet.
    /// </summary>
    /// <param name="highlights">The highlight set to render.</param>
    /// <param name="eagleSenseActive">If true, renders a full-size dim overlay behind all highlights.</param>
    /// <param name="gridWidth">Grid width in cells (for dim overlay sizing).</param>
    /// <param name="gridHeight">Grid height in cells (for dim overlay sizing).</param>
    public void Render(HighlightSet highlights, bool eagleSenseActive, int gridWidth, int gridHeight)
    {
        Clear();

        // Eagle sense dim overlay
        if (eagleSenseActive)
        {
            _dimOverlay = new ColorRect
            {
                Position = Vector2.Zero,
                Size = new Vector2(
                    gridWidth * GridConstants.CellSize,
                    gridHeight * GridConstants.CellSize),
                Color = GridConstants.DimOverlayColor,
                ZIndex = 0,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            AddChild(_dimOverlay);
        }

        if (highlights.IsEmpty) return;

        // Render each highlight
        // Use a set to track positions that have fills to avoid double-filling
        var filledPositions = new HashSet<CoreVec>();

        foreach (var layer in new[] { HighlightLayer.Constraint, HighlightLayer.Interaction, HighlightLayer.Legend, HighlightLayer.GhostOverlay })
        {
            var layerHighlights = highlights.GetByLayer(layer);
            foreach (var highlight in layerHighlights)
            {
                var baseColor = GetHighlightColor(highlight);
                var alpha = SeverityAlpha.GetValueOrDefault(highlight.Severity, 0.5f);
                var pixelPos = GridCoordinates.GridToPixel(highlight.Position);

                if (highlight.Edges != CellEdge.None)
                {
                    // Render edge segments
                    RenderEdges(highlight, baseColor, alpha, pixelPos);
                }
                else if (filledPositions.Add(highlight.Position))
                {
                    // Render cell fill (only once per position)
                    RenderFill(baseColor, alpha, pixelPos);
                }
            }
        }
    }

    /// <summary>
    /// Clears all rendered highlight visuals.
    /// </summary>
    public void Clear()
    {
        foreach (var rect in _edgeRects)
            rect.QueueFree();
        _edgeRects.Clear();

        foreach (var rect in _fillRects)
            rect.QueueFree();
        _fillRects.Clear();

        if (_dimOverlay != null)
        {
            _dimOverlay.QueueFree();
            _dimOverlay = null;
        }
    }

    private void RenderEdges(CellHighlight highlight, Color baseColor, float alpha, Vector2 pixelPos)
    {
        float cs = GridConstants.CellSize;
        float ew = GridConstants.HighlightEdgeWidth;
        var color = new Color(baseColor.R, baseColor.G, baseColor.B, alpha);

        if (highlight.Edges.HasFlag(CellEdge.Top))
        {
            AddEdgeRect(pixelPos, new Vector2(cs, ew), color);
        }
        if (highlight.Edges.HasFlag(CellEdge.Bottom))
        {
            AddEdgeRect(pixelPos + new Vector2(0, cs - ew), new Vector2(cs, ew), color);
        }
        if (highlight.Edges.HasFlag(CellEdge.Left))
        {
            AddEdgeRect(pixelPos, new Vector2(ew, cs), color);
        }
        if (highlight.Edges.HasFlag(CellEdge.Right))
        {
            AddEdgeRect(pixelPos + new Vector2(cs - ew, 0), new Vector2(ew, cs), color);
        }
    }

    private void AddEdgeRect(Vector2 pos, Vector2 size, Color color)
    {
        var rect = new ColorRect
        {
            Position = pos,
            Size = size,
            Color = color,
            ZIndex = 1,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(rect);
        _edgeRects.Add(rect);
    }

    private void RenderFill(Color baseColor, float alpha, Vector2 pixelPos)
    {
        float cs = GridConstants.CellSize;
        var color = new Color(baseColor.R, baseColor.G, baseColor.B, alpha * 0.4f);

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

    private static Color GetHighlightColor(CellHighlight highlight)
    {
        if (highlight.AspectId != null)
            return GridConstants.GetAspectColor(highlight.AspectId);

        // Fallback: color by severity
        return highlight.Severity switch
        {
            HighlightSeverity.Danger => new Color(0.9f, 0.2f, 0.2f),
            HighlightSeverity.Warning => new Color(0.95f, 0.75f, 0.1f),
            HighlightSeverity.Safe => new Color(0.2f, 0.8f, 0.2f),
            _ => new Color(0.7f, 0.7f, 0.7f),
        };
    }
}
