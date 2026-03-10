using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Highlight;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Game.Visual;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Renders an alchemical frame (cell mount) around ALL placed reagent cells.
/// Only draws on external perimeter edges — internal edges between cells of
/// the same reagent are omitted. Tinted by each reagent's primary aspect color.
/// Loads cell_mount.png texture for monomino cells; uses colored edge rects for
/// multi-cell reagents where only perimeter edges should show.
/// </summary>
public partial class CellMountOverlay : Control
{
    private const string CellMountTexturePath = "res://art/sprites/custom/frames/cell_mount.png";

    private readonly List<Node> _visuals = new();
    private Texture2D? _cellMountTexture;
    private bool _textureChecked;

    /// <summary>Number of rendered visual elements, for testing.</summary>
    public int RenderedCount => _visuals.Count;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>
    /// Rebuild the cell mount overlay for all placed reagents on the grid.
    /// </summary>
    public void Render(MetaGrid grid)
    {
        Clear();
        LoadTextureIfNeeded();

        // Collect placed reagents (deduplicate since a reagent can span many cells)
        var reagents = new HashSet<Reagent>();
        for (int col = 0; col < grid.Width; col++)
        {
            for (int row = 0; row < grid.Height; row++)
            {
                var cell = grid.GetCell(col, row)!;
                if (cell.IsOccupied)
                    reagents.Add((Reagent)cell.OccupyingReagent!);
            }
        }

        foreach (var reagent in reagents)
        {
            if (reagent.PlacedPosition == null) continue;
            RenderReagentMount(reagent);
        }
    }

    /// <summary>
    /// Clears all rendered mount visuals.
    /// </summary>
    public void Clear()
    {
        foreach (var node in _visuals)
        {
            if (node is Node n)
                n.QueueFree();
        }
        _visuals.Clear();
    }

    private void RenderReagentMount(Reagent reagent)
    {
        var aspectId = reagent.Definition.PrimaryAspectId;
        var aspectColor = aspectId != null
            ? GridConstants.GetAspectColor(aspectId)
            : new Color(0.5f, 0.5f, 0.5f);

        var mountColor = new Color(aspectColor.R, aspectColor.G, aspectColor.B, GridConstants.CellMountAlpha);
        var anchor = reagent.PlacedPosition!.Value;

        // Get perimeter edges (only external boundaries)
        var perimeterEdges = HoverFrameResolver.GetPerimeterEdges(reagent);
        bool isMonomino = reagent.Definition.CellCount == 1;

        if (isMonomino && _cellMountTexture != null)
        {
            // Use the full cell mount texture for monominos
            RenderTextureMount(anchor, mountColor);
        }
        else
        {
            // Use edge-based rendering for multi-cell reagents
            RenderEdgeMount(anchor, perimeterEdges, mountColor);
        }
    }

    private void RenderTextureMount(CoreVec anchor, Color tintColor)
    {
        var pixelPos = GridCoordinates.GridToPixel(anchor);
        float cs = GridConstants.CellSize;

        var rect = new TextureRect
        {
            Texture = _cellMountTexture,
            Position = pixelPos,
            Size = new Vector2(cs, cs),
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Modulate = tintColor,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(rect);
        _visuals.Add(rect);
    }

    private void RenderEdgeMount(CoreVec anchor, Dictionary<CoreVec, CellEdge> perimeterEdges, Color color)
    {
        float cs = GridConstants.CellSize;

        foreach (var (localPos, edges) in perimeterEdges)
        {
            var gridPos = anchor + localPos;
            var pixelPos = GridCoordinates.GridToPixel(gridPos);

            float ew = GridConstants.CellMountEdgeWidth;
            if (edges.HasFlag(CellEdge.Top))
                AddEdgeRect(pixelPos, new Vector2(cs, ew), color);
            if (edges.HasFlag(CellEdge.Bottom))
                AddEdgeRect(pixelPos + new Vector2(0, cs - ew), new Vector2(cs, ew), color);
            if (edges.HasFlag(CellEdge.Left))
                AddEdgeRect(pixelPos, new Vector2(ew, cs), color);
            if (edges.HasFlag(CellEdge.Right))
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
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(rect);
        _visuals.Add(rect);
    }

    private void LoadTextureIfNeeded()
    {
        if (_textureChecked) return;
        _textureChecked = true;

        if (ResourceLoader.Exists(CellMountTexturePath))
            _cellMountTexture = GD.Load<Texture2D>(CellMountTexturePath);
    }
}
