using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Game.Visual;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Sidebar panel showing puzzle-relevant aspects with collapsible detail entries.
/// Replaces AspectPrimerPanel with richer per-aspect information.
/// </summary>
public partial class AspectLegendPanel : PanelContainer
{
    private VBoxContainer _content = null!;
    private readonly Dictionary<string, AspectLegendEntry> _entries = new();
    private List<ReactionRule> _rules = new();
    private bool _isCompact;

    [Signal]
    public delegate void AspectSelectedEventHandler(string aspectId);

    public void SetPuzzleContext(List<ReactionRule> rules, List<Reagent> reagents)
    {
        _rules = rules;
        ClipContents = true;
        CustomMinimumSize = new Vector2(0, 120);

        // Parchment texture with fallback to dark background
        AddThemeStyleboxOverride("panel", UIThemeLoader.GetPanelStyleBox("parchment_plain"));

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 2);
        AddChild(_content);

        // Header
        var header = new Label { Text = "ASPECTS" };
        header.AddThemeColorOverride("font_color", new Color(0.35f, 0.25f, 0.1f));
        header.AddThemeFontSizeOverride("font_size", 14);
        _content.AddChild(header);

        // Filter to puzzle-relevant aspects
        var aspects = AspectMetadataDatabase.GetForPuzzle(rules, reagents);

        foreach (var meta in aspects)
        {
            var relevantRules = GetRulesForAspect(meta.Id, rules);
            var entry = new AspectLegendEntry(meta, relevantRules);
            entry.Clicked += () => OnEntryClicked(meta.Id);
            _entries[meta.Id] = entry;
            _content.AddChild(entry);
        }
    }

    /// <summary>
    /// Toggle compact mode: icon + name only, no expandable details.
    /// Used at narrower viewport widths.
    /// </summary>
    public void SetCompactMode(bool compact)
    {
        if (_isCompact == compact) return;
        _isCompact = compact;
        foreach (var entry in _entries.Values)
            entry.SetCompactMode(compact);
    }

    /// <summary>Whether the panel is currently in compact mode.</summary>
    public bool IsCompactMode => _isCompact;

    /// <summary>Number of aspect entries displayed.</summary>
    public int EntryCount => _entries.Count;

    /// <summary>Returns whether a specific aspect entry is expanded.</summary>
    public bool IsExpanded(string aspectId)
    {
        return _entries.TryGetValue(aspectId, out var entry) && entry.IsExpanded;
    }

    /// <summary>
    /// Visually highlight the given aspect entries (e.g., when hovering a reagent on the grid).
    /// </summary>
    public void HighlightAspects(HashSet<string> aspectIds)
    {
        foreach (var (id, entry) in _entries)
        {
            entry.SetHighlighted(aspectIds.Contains(id));
        }
    }

    /// <summary>Clear all entry highlights.</summary>
    public void ClearHighlight()
    {
        foreach (var entry in _entries.Values)
        {
            entry.SetHighlighted(false);
        }
    }

    /// <summary>
    /// Briefly flash an aspect entry (e.g., on rejection tooltip).
    /// </summary>
    public void FlashAspect(string aspectId)
    {
        if (_entries.TryGetValue(aspectId, out var entry))
        {
            entry.Flash();
        }
    }

    /// <summary>Programmatically toggle an entry's expanded state (for testing).</summary>
    public void ToggleEntry(string aspectId)
    {
        if (_entries.TryGetValue(aspectId, out var entry))
        {
            entry.ToggleExpand();
        }
    }

    private void OnEntryClicked(string aspectId)
    {
        EmitSignal(SignalName.AspectSelected, aspectId);
    }

    private static List<ReactionRule> GetRulesForAspect(string aspectId, List<ReactionRule> allRules)
    {
        var result = new List<ReactionRule>();
        foreach (var rule in allRules)
        {
            if (rule.TriggerAspects.Contains(aspectId))
                result.Add(rule);
        }
        return result;
    }
}

/// <summary>
/// A single collapsible entry in the aspect legend panel.
/// Shows a colored icon + name when collapsed, plus description, constraint, and rules when expanded.
/// </summary>
public partial class AspectLegendEntry : VBoxContainer
{
    private readonly AspectMetadata _metadata;
    private readonly List<ReactionRule> _rules;
    private HBoxContainer _header = null!;
    private Control _icon = null!;
    private Label _nameLabel = null!;
    private VBoxContainer _detail = null!;
    private bool _isExpanded;
    private bool _isHighlighted;
    private bool _isAnimating;
    private bool _isCompact;
    private StyleBoxFlat _normalBg = null!;
    private StyleBoxFlat _highlightBg = null!;
    private PanelContainer _headerPanel = null!;
    private Tween? _expandTween;

    public event System.Action? Clicked;

    public bool IsExpanded => _isExpanded;

    public AspectLegendEntry(AspectMetadata metadata, List<ReactionRule> rules)
    {
        _metadata = metadata;
        _rules = rules;
        BuildEntry();
    }

    private void BuildEntry()
    {
        AddThemeConstantOverride("separation", 0);

        // Background panel for the clickable header row
        _normalBg = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0),
            ContentMarginLeft = 4,
            ContentMarginRight = 4,
            ContentMarginTop = 2,
            ContentMarginBottom = 2,
        };
        _highlightBg = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.3f, 0.15f, 0.5f),
            ContentMarginLeft = 4,
            ContentMarginRight = 4,
            ContentMarginTop = 2,
            ContentMarginBottom = 2,
        };

        _headerPanel = new PanelContainer();
        _headerPanel.CustomMinimumSize = new Vector2(0, 44);
        _headerPanel.AddThemeStyleboxOverride("panel", _normalBg);
        AddChild(_headerPanel);

        _header = new HBoxContainer();
        _header.AddThemeConstantOverride("separation", 6);
        _headerPanel.AddChild(_header);

        // Aspect tile sprite (fallback to colored square) — 32x32 native size
        var iconContainer = new CenterContainer { CustomMinimumSize = new Vector2(32, 32) };
        _icon = UIThemeLoader.CreateAspectIcon(_metadata.Id, 32);
        iconContainer.AddChild(_icon);
        _header.AddChild(iconContainer);

        // Aspect name
        _nameLabel = new Label { Text = _metadata.Name };
        _nameLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.15f, 0.08f));
        _nameLabel.AddThemeFontSizeOverride("font_size", 12);
        _header.AddChild(_nameLabel);

        // Make header clickable via GuiInput
        _headerPanel.GuiInput += OnHeaderInput;
        _headerPanel.MouseDefaultCursorShape = CursorShape.PointingHand;

        // Detail container (hidden by default)
        _detail = new VBoxContainer();
        _detail.AddThemeConstantOverride("separation", 2);
        _detail.Visible = false;
        AddChild(_detail);

        // Build detail content
        BuildDetail();
    }

    private void BuildDetail()
    {
        var marginContainer = new MarginContainer();
        marginContainer.AddThemeConstantOverride("margin_left", 22);
        _detail.AddChild(marginContainer);

        var detailVBox = new VBoxContainer();
        detailVBox.AddThemeConstantOverride("separation", 2);
        marginContainer.AddChild(detailVBox);

        // Description
        var descLabel = new Label
        {
            Text = _metadata.Description,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(180, 0),
        };
        descLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.25f, 0.15f));
        descLabel.AddThemeFontSizeOverride("font_size", 11);
        detailVBox.AddChild(descLabel);

        // Constraint text (if present)
        if (!string.IsNullOrEmpty(_metadata.ConstraintText))
        {
            var constraintLabel = new Label
            {
                Text = $"Constraint: {_metadata.ConstraintText}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(180, 0),
            };
            constraintLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.35f, 0.1f));
            constraintLabel.AddThemeFontSizeOverride("font_size", 11);
            detailVBox.AddChild(constraintLabel);
        }

        // Relevant reaction rules
        if (_rules.Count > 0)
        {
            var rulesHeader = new Label { Text = "Reactions:" };
            rulesHeader.AddThemeColorOverride("font_color", new Color(0.35f, 0.3f, 0.2f));
            rulesHeader.AddThemeFontSizeOverride("font_size", 11);
            detailVBox.AddChild(rulesHeader);

            foreach (var rule in _rules)
            {
                var ruleLabel = new Label
                {
                    Text = $"  {AspectFormatHelpers.FormatRule(rule)}",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    CustomMinimumSize = new Vector2(180, 0),
                };
                ruleLabel.AddThemeColorOverride("font_color", AspectFormatHelpers.GetRuleColor(rule.ResultType));
                ruleLabel.AddThemeFontSizeOverride("font_size", 11);
                detailVBox.AddChild(ruleLabel);
            }
        }
    }

    public void ToggleExpand()
    {
        if (_isCompact) return;
        _isExpanded = !_isExpanded;

        // Kill any in-progress animation
        _expandTween?.Kill();
        _expandTween = null;

        if (_isExpanded)
        {
            // Expanding: make visible, measure natural height, tween from 0
            _detail.Visible = true;
            ClipContents = true;

            // Temporarily remove size constraint to measure natural height
            _detail.CustomMinimumSize = new Vector2(0, 0);
            var naturalHeight = _detail.GetCombinedMinimumSize().Y;

            // Start from zero height and tween to natural
            _detail.CustomMinimumSize = new Vector2(0, 0);
            _isAnimating = true;
            _expandTween = CreateTween();
            _expandTween.TweenProperty(_detail, "custom_minimum_size:y", naturalHeight, 0.2)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
            _expandTween.TweenCallback(Callable.From(() =>
            {
                _isAnimating = false;
                ClipContents = false;
                // Clear minimum size so layout can be natural
                _detail.CustomMinimumSize = new Vector2(0, 0);
            }));
        }
        else
        {
            // Collapsing: tween height to 0, then hide
            ClipContents = true;
            var currentHeight = _detail.GetCombinedMinimumSize().Y;
            _detail.CustomMinimumSize = new Vector2(0, currentHeight);
            _isAnimating = true;
            _expandTween = CreateTween();
            _expandTween.TweenProperty(_detail, "custom_minimum_size:y", 0.0f, 0.15)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
            _expandTween.TweenCallback(Callable.From(() =>
            {
                _isAnimating = false;
                _detail.Visible = false;
                ClipContents = false;
                _detail.CustomMinimumSize = new Vector2(0, 0);
            }));
        }
    }

    public void SetCompactMode(bool compact)
    {
        if (_isCompact == compact) return;
        _isCompact = compact;
        if (compact && _isExpanded)
        {
            // Collapse immediately without animation
            _expandTween?.Kill();
            _expandTween = null;
            _isExpanded = false;
            _isAnimating = false;
            _detail.Visible = false;
            _detail.CustomMinimumSize = new Vector2(0, 0);
            ClipContents = false;
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (_isHighlighted == highlighted) return;
        _isHighlighted = highlighted;
        _headerPanel.AddThemeStyleboxOverride("panel", highlighted ? _highlightBg : _normalBg);
        if (highlighted)
        {
            _icon.CustomMinimumSize = new Vector2(34, 34);
        }
        else
        {
            _icon.CustomMinimumSize = new Vector2(32, 32);
        }
    }

    public void Flash()
    {
        // Briefly highlight then restore after a short delay
        var originalBg = _isHighlighted ? _highlightBg : _normalBg;
        var flashBg = new StyleBoxFlat
        {
            BgColor = new Color(0.4f, 0.35f, 0.15f, 0.7f),
            ContentMarginLeft = 4,
            ContentMarginRight = 4,
            ContentMarginTop = 2,
            ContentMarginBottom = 2,
        };
        _headerPanel.AddThemeStyleboxOverride("panel", flashBg);

        var timer = GetTree().CreateTimer(0.4);
        timer.Timeout += () => _headerPanel.AddThemeStyleboxOverride("panel", originalBg);
    }

    private void OnHeaderInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            ToggleExpand();
            Clicked?.Invoke();
        }
    }

}
