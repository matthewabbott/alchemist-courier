using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Game.Visual;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Persistent panel showing details about the last-hovered reagent.
/// Book of Hours pattern: always visible, always in the same place.
/// Shows enlarged shape preview, name, flavor text, and aspect breakdown.
/// </summary>
public partial class ItemDetailPanel : PanelContainer
{
    private VBoxContainer _content = null!;
    private DetailShapePreview _shapePreview = null!;
    private Label _nameLabel = null!;
    private Label _flavorLabel = null!;
    private VBoxContainer _aspectList = null!;
    private Label _placeholderLabel = null!;
    private Reagent? _currentReagent;

    /// <summary>The reagent currently being displayed (null = placeholder).</summary>
    public Reagent? CurrentReagent => _currentReagent;

    public ItemDetailPanel()
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.1f, 0.95f),
            BorderColor = new Color(0.5f, 0.45f, 0.35f, 0.6f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
        };
        AddThemeStyleboxOverride("panel", style);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 6);
        AddChild(_content);

        // Header
        var header = new Label { Text = "INSPECT" };
        header.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        header.AddThemeFontSizeOverride("font_size", 14);
        _content.AddChild(header);

        // Shape preview (16px per cell)
        _shapePreview = new DetailShapePreview();
        _shapePreview.Visible = false;
        _content.AddChild(_shapePreview);

        // Name label
        _nameLabel = new Label();
        _nameLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.95f, 0.95f));
        _nameLabel.AddThemeFontSizeOverride("font_size", 14);
        _nameLabel.Visible = false;
        _content.AddChild(_nameLabel);

        // Flavor text
        _flavorLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(180, 0),
        };
        _flavorLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.55f));
        _flavorLabel.AddThemeFontSizeOverride("font_size", 11);
        _flavorLabel.Visible = false;
        _content.AddChild(_flavorLabel);

        // Aspect breakdown list
        _aspectList = new VBoxContainer();
        _aspectList.AddThemeConstantOverride("separation", 2);
        _aspectList.Visible = false;
        _content.AddChild(_aspectList);

        // Placeholder
        _placeholderLabel = new Label
        {
            Text = "Hover over a reagent to inspect",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(180, 0),
        };
        _placeholderLabel.AddThemeColorOverride("font_color", new Color(0.45f, 0.45f, 0.4f));
        _placeholderLabel.AddThemeFontSizeOverride("font_size", 11);
        _content.AddChild(_placeholderLabel);
    }

    /// <summary>
    /// Update the panel to show a reagent's details, or null for placeholder state.
    /// </summary>
    public void ShowReagent(Reagent? reagent)
    {
        _currentReagent = reagent;

        if (reagent == null)
        {
            _shapePreview.Visible = false;
            _nameLabel.Visible = false;
            _flavorLabel.Visible = false;
            _aspectList.Visible = false;
            _placeholderLabel.Visible = true;
            return;
        }

        _placeholderLabel.Visible = false;

        // Shape preview
        _shapePreview.SetReagent(reagent);
        _shapePreview.Visible = true;

        // Name
        _nameLabel.Text = reagent.Definition.Name;
        _nameLabel.Visible = true;

        // Flavor text
        if (!string.IsNullOrEmpty(reagent.Definition.FlavorText))
        {
            _flavorLabel.Text = reagent.Definition.FlavorText;
            _flavorLabel.Visible = true;
        }
        else
        {
            _flavorLabel.Visible = false;
        }

        // Aspect breakdown
        BuildAspectBreakdown(reagent);
        _aspectList.Visible = true;
    }

    private void BuildAspectBreakdown(Reagent reagent)
    {
        // Clear existing entries
        foreach (var child in _aspectList.GetChildren())
        {
            child.QueueFree();
        }

        // Aggregate aspects across all cells: sum magnitudes per unique aspect ID
        var aspectTotals = reagent.GetAggregatedAspects();

        foreach (var (id, (aspect, total)) in aspectTotals)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);

            // Colored square
            var icon = new ColorRect
            {
                CustomMinimumSize = new Vector2(12, 12),
                Color = GridConstants.GetAspectColor(id),
            };
            var iconCenter = new CenterContainer { CustomMinimumSize = new Vector2(12, 14) };
            iconCenter.AddChild(icon);
            row.AddChild(iconCenter);

            // Aspect name + magnitude
            var label = new Label
            {
                Text = $"{aspect.Name} x{total}",
            };
            label.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.75f));
            label.AddThemeFontSizeOverride("font_size", 11);
            row.AddChild(label);

            _aspectList.AddChild(row);
        }
    }
}

/// <summary>
/// Enlarged shape preview for the detail panel (16px per cell).
/// </summary>
public partial class DetailShapePreview : Control
{
    private const int CellSize = 16;
    private Reagent? _reagent;

    public void SetReagent(Reagent reagent)
    {
        _reagent = reagent;
        var shape = reagent.Definition.Shape;
        int cols = shape.GetLength(1);
        int rows = shape.GetLength(0);
        CustomMinimumSize = new Vector2(cols * CellSize, rows * CellSize);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_reagent == null) return;

        var cellPositions = _reagent.Definition.GetLocalCellPositions();
        var aspects = _reagent.Definition.AspectsPerCell;

        for (int i = 0; i < cellPositions.Count; i++)
        {
            var pos = cellPositions[i];
            var rect = new Rect2(pos.Col * CellSize, pos.Row * CellSize, CellSize, CellSize);

            string? aspectId = null;
            if (i < aspects.Count && !aspects[i].IsEmpty)
                aspectId = aspects[i].Aspects[0].Id;

            if (aspectId != null)
            {
                var texture = UIThemeLoader.GetAspectTileTexture(aspectId);
                if (texture != null)
                {
                    DrawTextureRect(texture, rect, false);
                    continue;
                }
            }

            var color = aspectId != null ? GridConstants.GetAspectColor(aspectId) : new Color(0.6f, 0.55f, 0.5f);
            DrawRect(rect, color);
            DrawRect(rect, new Color(0, 0, 0, 0.3f), false, 1);
        }
    }
}
