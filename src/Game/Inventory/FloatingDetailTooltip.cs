using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Floating tooltip showing reagent details (shape preview, name, flavor, aspect breakdown).
/// Replaces the inline ItemDetailPanel for the web demo's hover-based UI.
/// </summary>
public partial class FloatingDetailTooltip : FloatingTooltip
{
    private DetailShapePreview _shapePreview = null!;
    private Label _nameLabel = null!;
    private Label _flavorLabel = null!;
    private VBoxContainer _aspectList = null!;
    private Reagent? _currentReagent;
    private readonly HoverDebounce _debounce;

    protected override Vector2 AnchorOffset => new(20, -10);

    public Reagent? CurrentReagent => _currentReagent;

    public FloatingDetailTooltip()
    {
        _debounce = new HoverDebounce(GridConstants.HoverDebounceDelay);
        _debounce.OnHide += () => HideAnimated();

        // Shape preview
        _shapePreview = new DetailShapePreview();
        _shapePreview.Visible = false;
        Content.AddChild(_shapePreview);

        // Name
        _nameLabel = new Label();
        _nameLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.95f, 0.95f));
        _nameLabel.AddThemeFontSizeOverride("font_size", 14);
        _nameLabel.Visible = false;
        Content.AddChild(_nameLabel);

        // Flavor text
        _flavorLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(200, 0),
        };
        _flavorLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.55f));
        _flavorLabel.AddThemeFontSizeOverride("font_size", 11);
        _flavorLabel.Visible = false;
        Content.AddChild(_flavorLabel);

        // Aspect breakdown
        _aspectList = new VBoxContainer();
        _aspectList.AddThemeConstantOverride("separation", 2);
        _aspectList.Visible = false;
        Content.AddChild(_aspectList);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        _debounce.Process((float)delta);
    }

    /// <summary>
    /// Show tooltip for a reagent at the given anchor position.
    /// Pass null to trigger a debounced hide.
    /// </summary>
    public void ShowForReagent(Reagent? reagent, Vector2 anchorPosition)
    {
        if (reagent == null)
        {
            // Start debounced hide (user may re-hover soon)
            _debounce.OnMouseExited();
            return;
        }

        // Cancel any pending hide
        _debounce.OnMouseEntered();

        // Only rebuild content if reagent changed
        if (reagent != _currentReagent)
        {
            _currentReagent = reagent;
            RebuildContent(reagent);
        }

        ShowAt(anchorPosition);
    }

    /// <summary>
    /// Force immediate hide without debounce (e.g., on reagent selection).
    /// </summary>
    public void ForceHide()
    {
        _debounce.Reset();
        _currentReagent = null;
        HideAnimated();
    }

    private void RebuildContent(Reagent reagent)
    {
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

        // Aggregate aspects across all cells
        var aspectTotals = new Dictionary<string, (Aspect aspect, int totalMagnitude)>();
        var cellPositions = reagent.GetLocalCellPositions();
        foreach (var pos in cellPositions)
        {
            var aspects = reagent.GetAspectsAtLocalPosition(pos);
            foreach (var aspect in aspects.Aspects)
            {
                if (aspectTotals.TryGetValue(aspect.Id, out var existing))
                {
                    aspectTotals[aspect.Id] = (existing.aspect, existing.totalMagnitude + aspect.Magnitude);
                }
                else
                {
                    aspectTotals[aspect.Id] = (aspect, aspect.Magnitude);
                }
            }
        }

        foreach (var (id, (aspect, total)) in aspectTotals)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);

            var icon = new ColorRect
            {
                CustomMinimumSize = new Vector2(12, 12),
                Color = GridConstants.GetAspectColor(id),
            };
            var iconCenter = new CenterContainer { CustomMinimumSize = new Vector2(12, 14) };
            iconCenter.AddChild(icon);
            row.AddChild(iconCenter);

            var label = new Label { Text = $"{aspect.Name} x{total}" };
            label.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.75f));
            label.AddThemeFontSizeOverride("font_size", 11);
            row.AddChild(label);

            _aspectList.AddChild(row);
        }
    }
}
