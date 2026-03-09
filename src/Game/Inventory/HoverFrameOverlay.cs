using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Highlight;
using AlchemistCourier.Core.Reagents;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Godot Control that renders an aspect-themed perimeter frame over a reagent.
/// Loads frame textures from art/sprites/frames/dev/ and displays them with a
/// sinusoidal pulse animation. Supports three modes: hover (default pulse),
/// selection (shifted alpha range), and drag (steady alpha).
/// Alpha is applied via the node's Modulate property, affecting all children uniformly.
/// </summary>
public partial class HoverFrameOverlay : Control
{
    private const string FrameBasePath = "res://art/sprites/frames/dev/";

    private Reagent? _currentReagent;
    private bool _isVisible;
    private float _pulseTime;
    private bool _hasChildren;

    // Mode state
    private bool _isSelected;
    private bool _isDragging;

    // Fade state
    private bool _isFadingOut;
    private float _fadeTimer;

    // Texture cache
    private static readonly Dictionary<string, Texture2D?> _textureCache = new();

    /// <summary>Whether the overlay is currently showing a frame.</summary>
    public bool IsShowingFrame => _isVisible && _hasChildren;

    /// <summary>The reagent currently being highlighted, or null.</summary>
    public Reagent? CurrentReagent => _currentReagent;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Process(double delta)
    {
        if (_isFadingOut)
        {
            _fadeTimer -= (float)delta;
            if (_fadeTimer <= 0)
            {
                _isFadingOut = false;
                ClearChildren();
                return;
            }
            float fadeProgress = _fadeTimer / GridConstants.FrameFadeDuration;
            ApplyAlpha(fadeProgress * GetCurrentAlpha());
            return;
        }

        if (!_isVisible || !_hasChildren) return;

        _pulseTime += (float)delta;
        ApplyAlpha(GetCurrentAlpha());
    }

    /// <summary>
    /// Show a hover frame for the given reagent at its grid pixel position.
    /// </summary>
    public void ShowFrame(Reagent reagent, Vector2 gridPixelPosition)
    {
        _isFadingOut = false;
        _isSelected = false;
        _isDragging = false;

        if (_currentReagent == reagent && _isVisible)
            return;

        ClearChildren();
        _currentReagent = reagent;
        _isVisible = true;
        _pulseTime = 0;

        CreateFrameVisuals(reagent, gridPixelPosition);
    }

    /// <summary>
    /// Show frame in selected mode (shifted alpha range).
    /// </summary>
    public void ShowSelected(Reagent reagent, Vector2 gridPixelPosition)
    {
        ShowFrame(reagent, gridPixelPosition);
        _isSelected = true;
        _isDragging = false;
    }

    /// <summary>
    /// Switch to drag mode (steady alpha, no pulse).
    /// </summary>
    public void SetDragging(bool dragging)
    {
        _isDragging = dragging;
    }

    /// <summary>
    /// Fade out and hide the frame.
    /// </summary>
    public void HideFrame()
    {
        if (!_isVisible && !_isFadingOut) return;

        if (_hasChildren)
        {
            _isFadingOut = true;
            _fadeTimer = GridConstants.FrameFadeDuration;
        }
        else
        {
            ClearChildren();
        }

        _isVisible = false;
        _currentReagent = null;
        _isSelected = false;
        _isDragging = false;
    }

    private void CreateFrameVisuals(Reagent reagent, Vector2 gridPixelPosition)
    {
        var aspectId = reagent.Definition.PrimaryAspectId;
        if (aspectId == null) return;

        var shapeName = ReagentCatalog.GetShapeName(reagent.Definition);
        if (shapeName == null) return;

        // Include rotation in texture lookup so rotated shapes get correct frames
        var texture = LoadFrameTexture(aspectId, shapeName, reagent.Rotation);
        if (texture != null)
        {
            var rect = new TextureRect
            {
                Texture = texture,
                Position = gridPixelPosition,
                Size = new Vector2(texture.GetWidth(), texture.GetHeight()),
                StretchMode = TextureRect.StretchModeEnum.KeepAspect,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            AddChild(rect);
            _hasChildren = true;
        }
        else
        {
            // Edge fallback uses GetPerimeterEdges which is rotation-aware
            CreateEdgeFallback(reagent, gridPixelPosition, aspectId);
        }
    }

    private void CreateEdgeFallback(Reagent reagent, Vector2 gridPixelPosition, string aspectId)
    {
        var perimeterEdges = HoverFrameResolver.GetPerimeterEdges(reagent);
        var color = GridConstants.GetAspectColor(aspectId);
        float cs = GridConstants.CellSize;
        const float edgeWidth = 4f;

        foreach (var (localPos, edges) in perimeterEdges)
        {
            var cellPixel = gridPixelPosition + new Vector2(localPos.Col * cs, localPos.Row * cs);

            if (edges.HasFlag(CellEdge.Top))
                AddEdgeRect(cellPixel, new Vector2(cs, edgeWidth), color);
            if (edges.HasFlag(CellEdge.Bottom))
                AddEdgeRect(cellPixel + new Vector2(0, cs - edgeWidth), new Vector2(cs, edgeWidth), color);
            if (edges.HasFlag(CellEdge.Left))
                AddEdgeRect(cellPixel, new Vector2(edgeWidth, cs), color);
            if (edges.HasFlag(CellEdge.Right))
                AddEdgeRect(cellPixel + new Vector2(cs - edgeWidth, 0), new Vector2(edgeWidth, cs), color);
        }

        _hasChildren = GetChildCount() > 0;
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
    }

    private float GetCurrentAlpha()
    {
        if (_isDragging)
            return GridConstants.FrameDragAlpha;

        float min, max;
        if (_isSelected)
        {
            min = GridConstants.FrameSelectedAlphaMin;
            max = GridConstants.FrameSelectedAlphaMax;
        }
        else
        {
            min = GridConstants.FramePulseAlphaMin;
            max = GridConstants.FramePulseAlphaMax;
        }

        // Sinusoidal oscillation between min and max
        float t = (Mathf.Sin(_pulseTime * Mathf.Tau / GridConstants.FramePulsePeriod) + 1f) * 0.5f;
        return min + (max - min) * t;
    }

    private void ApplyAlpha(float alpha)
    {
        Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(alpha, 0f, 1f));
    }

    private void ClearChildren()
    {
        foreach (var child in GetChildren())
        {
            if (child is Node node)
                node.QueueFree();
        }
        _hasChildren = false;
        _currentReagent = null;
    }

    private static Texture2D? LoadFrameTexture(string aspectId, string shapeName, int rotation)
    {
        // Try rotation-specific key first: e.g., "fire_L_r1"
        string rotatedKey = $"{aspectId}_{shapeName}_r{rotation}";
        if (_textureCache.TryGetValue(rotatedKey, out var cached))
            return cached;

        string path = $"{FrameBasePath}{rotatedKey}.png";
        Texture2D? texture = null;
        if (ResourceLoader.Exists(path))
            texture = GD.Load<Texture2D>(path);

        // Backward compat: for rotation 0, also try base key without suffix
        if (texture == null && rotation == 0)
        {
            string baseKey = $"{aspectId}_{shapeName}";
            string basePath = $"{FrameBasePath}{baseKey}.png";
            if (ResourceLoader.Exists(basePath))
                texture = GD.Load<Texture2D>(basePath);
        }

        _textureCache[rotatedKey] = texture;
        return texture;
    }

    /// <summary>Clears the texture cache (for testing or scene changes).</summary>
    public static void ClearCache()
    {
        _textureCache.Clear();
    }
}
