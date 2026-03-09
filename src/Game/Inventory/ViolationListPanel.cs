using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Highlight;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Panel displaying a live list of current placement violations.
/// Updates on every reagent place/remove. Each entry is clickable
/// to emit ViolationClicked for GO.12 highlight integration.
/// </summary>
public partial class ViolationListPanel : PanelContainer
{
    [Signal]
    public delegate void ViolationClickedEventHandler(int violationIndex);

    private MetaGrid _grid = null!;
    private List<ReactionRule> _rules = null!;
    private VBoxContainer _content = null!;
    private Label _headerLabel = null!;
    private List<ViolationEntry> _currentViolations = new();

    /// <summary>Current violations for external inspection.</summary>
    public IReadOnlyList<ViolationEntry> CurrentViolations => _currentViolations;

    public void Initialize(MetaGrid grid, List<ReactionRule> rules)
    {
        _grid = grid;
        _rules = rules;
        BuildUI();
        Refresh();
    }

    private void BuildUI()
    {
        CustomMinimumSize = new Vector2(0, 80);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.13f, 0.9f),
            ContentMarginLeft = 8, ContentMarginRight = 8,
            ContentMarginTop = 6, ContentMarginBottom = 6,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            BorderColor = new Color(0.25f, 0.25f, 0.3f),
            BorderWidthBottom = 1,
        };
        AddThemeStyleboxOverride("panel", style);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 4);
        AddChild(_content);

        _headerLabel = new Label();
        _headerLabel.AddThemeFontSizeOverride("font_size", 13);
        _content.AddChild(_headerLabel);
    }

    /// <summary>
    /// Recompute violations from current grid state and rebuild the entry list.
    /// </summary>
    public void Refresh()
    {
        _currentViolations = ViolationTracker.ComputeAll(_grid, _rules);

        // Remove old entries (keep header)
        for (int i = _content.GetChildCount() - 1; i >= 1; i--)
        {
            var child = _content.GetChild(i);
            _content.RemoveChild(child);
            child.QueueFree();
        }

        if (_currentViolations.Count == 0)
        {
            _headerLabel.Text = "Issues";
            _headerLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.7f, 0.4f));
            var noIssues = new Label { Text = "No issues" };
            noIssues.AddThemeColorOverride("font_color", new Color(0.4f, 0.7f, 0.4f));
            noIssues.AddThemeFontSizeOverride("font_size", 12);
            _content.AddChild(noIssues);
        }
        else
        {
            _headerLabel.Text = $"Issues ({_currentViolations.Count})";
            _headerLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.7f, 0.3f));

            for (int i = 0; i < _currentViolations.Count; i++)
            {
                var entry = _currentViolations[i];
                var entryContainer = BuildEntryRow(entry, i);
                _content.AddChild(entryContainer);
            }
        }
    }

    private Control BuildEntryRow(ViolationEntry entry, int index)
    {
        var btn = new Button();
        btn.Alignment = HorizontalAlignment.Left;
        btn.Text = entry.Description;
        btn.ClipText = true;

        var textColor = entry.Severity switch
        {
            HighlightSeverity.Danger => new Color(0.95f, 0.3f, 0.25f),
            HighlightSeverity.Warning => new Color(0.95f, 0.8f, 0.25f),
            _ => new Color(0.7f, 0.7f, 0.7f),
        };

        // Use flat transparent buttons for clickable entries
        foreach (var (overrideName, alpha) in new[] { ("normal", 0.0f), ("hover", 0.15f), ("pressed", 0.25f) })
        {
            var style = new StyleBoxFlat
            {
                BgColor = new Color(textColor.R, textColor.G, textColor.B, alpha),
                ContentMarginLeft = 6, ContentMarginRight = 6,
                ContentMarginTop = 2, ContentMarginBottom = 2,
                CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
                CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
            };
            btn.AddThemeStyleboxOverride(overrideName, style);
        }

        btn.AddThemeColorOverride("font_color", textColor);
        btn.AddThemeColorOverride("font_hover_color", textColor.Lightened(0.2f));
        btn.AddThemeColorOverride("font_pressed_color", textColor.Lightened(0.3f));
        btn.AddThemeFontSizeOverride("font_size", 11);

        var capturedIndex = index;
        btn.Pressed += () => EmitSignal(SignalName.ViolationClicked, capturedIndex);

        return btn;
    }
}
