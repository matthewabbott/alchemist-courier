using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Chemistry;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Floating tooltip showing details about a single aspect.
/// Shows aspect icon, name, description, constraint text, and relevant reaction rules.
/// </summary>
public partial class FloatingAspectTooltip : FloatingTooltip
{
    private string? _currentAspectId;
    private readonly HoverDebounce _debounce;
    private List<ReactionRule> _allRules = new();

    protected override Vector2 AnchorOffset => new(-220, -10);

    public string? CurrentAspectId => _currentAspectId;

    public FloatingAspectTooltip()
    {
        _debounce = new HoverDebounce(GridConstants.HoverDebounceDelay);
        _debounce.OnHide += () => HideAnimated();
    }

    /// <summary>
    /// Set the reaction rules so the tooltip can show relevant rules per aspect.
    /// </summary>
    public void SetReactionRules(List<ReactionRule> rules)
    {
        _allRules = rules;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        _debounce.Process((float)delta);
    }

    /// <summary>
    /// Show tooltip for an aspect at the given anchor position.
    /// Pass null aspectId to trigger a debounced hide.
    /// </summary>
    public void ShowForAspect(string? aspectId, Vector2 anchorPosition)
    {
        if (aspectId == null)
        {
            _debounce.OnMouseExited();
            return;
        }

        _debounce.OnMouseEntered();

        if (aspectId != _currentAspectId)
        {
            _currentAspectId = aspectId;
            RebuildContent(aspectId);
        }

        ShowAt(anchorPosition);
    }

    /// <summary>Force immediate hide without debounce.</summary>
    public void ForceHide()
    {
        _debounce.Reset();
        _currentAspectId = null;
        HideAnimated();
    }

    private void RebuildContent(string aspectId)
    {
        // Clear existing content
        foreach (var child in Content.GetChildren())
        {
            child.QueueFree();
        }

        var meta = AspectMetadataDatabase.Get(aspectId);
        if (meta == null) return;

        // Header row: colored icon + name
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 8);

        var icon = new ColorRect
        {
            CustomMinimumSize = new Vector2(18, 18),
            Color = GridConstants.GetAspectColor(aspectId),
        };
        var iconCenter = new CenterContainer { CustomMinimumSize = new Vector2(18, 20) };
        iconCenter.AddChild(icon);
        headerRow.AddChild(iconCenter);

        var nameLabel = new Label { Text = meta.Name };
        nameLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.95f, 0.95f));
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        headerRow.AddChild(nameLabel);

        Content.AddChild(headerRow);

        // Description
        var descLabel = new Label
        {
            Text = meta.Description,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(200, 0),
        };
        descLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.65f));
        descLabel.AddThemeFontSizeOverride("font_size", 11);
        Content.AddChild(descLabel);

        // Constraint text
        if (!string.IsNullOrEmpty(meta.ConstraintText))
        {
            var constraintLabel = new Label
            {
                Text = $"Constraint: {meta.ConstraintText}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(200, 0),
            };
            constraintLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.65f, 0.3f));
            constraintLabel.AddThemeFontSizeOverride("font_size", 11);
            Content.AddChild(constraintLabel);
        }

        // Relevant reaction rules
        var relevantRules = GetRulesForAspect(aspectId);
        if (relevantRules.Count > 0)
        {
            var rulesHeader = new Label { Text = "Reactions:" };
            rulesHeader.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.75f));
            rulesHeader.AddThemeFontSizeOverride("font_size", 11);
            Content.AddChild(rulesHeader);

            foreach (var rule in relevantRules)
            {
                var ruleLabel = new Label
                {
                    Text = $"  {AspectFormatHelpers.FormatRule(rule)}",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    CustomMinimumSize = new Vector2(200, 0),
                };
                ruleLabel.AddThemeColorOverride("font_color", AspectFormatHelpers.GetRuleColor(rule.ResultType));
                ruleLabel.AddThemeFontSizeOverride("font_size", 11);
                Content.AddChild(ruleLabel);
            }
        }
    }

    private List<ReactionRule> GetRulesForAspect(string aspectId)
    {
        var result = new List<ReactionRule>();
        foreach (var rule in _allRules)
        {
            if (rule.TriggerAspects.Contains(aspectId))
                result.Add(rule);
        }
        return result;
    }

}
