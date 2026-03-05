using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Displays cumulative aspect totals with threshold bars for WholeInventory rules.
/// Shows a labeled bar per tracked aspect (Hot, Cold, Toxic) with current/threshold text.
/// </summary>
public partial class StatusBarPanel : HBoxContainer
{
    private MetaGrid _grid = null!;
    private List<ReactionRule> _cumulativeRules = new();
    private readonly Dictionary<string, AspectBar> _bars = new();

    public void Initialize(MetaGrid grid, List<ReactionRule> rules)
    {
        _grid = grid;

        // Filter to WholeInventory cumulative rules only
        _cumulativeRules = new List<ReactionRule>();
        foreach (var rule in rules)
        {
            if (rule.Sensitivity == SensitivityTier.WholeInventory && rule.TriggerAspects.Count == 1)
                _cumulativeRules.Add(rule);
        }

        BuildBars();
        Refresh();
    }

    private void BuildBars()
    {
        // Spacing between bars
        AddThemeConstantOverride("separation", 24);

        foreach (var rule in _cumulativeRules)
        {
            var aspectId = rule.TriggerAspects[0];
            var bar = new AspectBar();
            bar.Initialize(aspectId, rule.Name, rule.Threshold, rule.Granularity);
            AddChild(bar);
            _bars[aspectId] = bar;
        }
    }

    /// <summary>
    /// Recalculate all cumulative totals from the current grid state and update the bars.
    /// Call this after each placement or removal.
    /// </summary>
    public void Refresh()
    {
        foreach (var rule in _cumulativeRules)
        {
            var aspectId = rule.TriggerAspects[0];
            int total = ComputeTotal(aspectId, rule.Granularity);
            if (_bars.TryGetValue(aspectId, out var bar))
                bar.SetValue(total);
        }
    }

    private int ComputeTotal(string aspectId, Granularity granularity)
    {
        int total = 0;

        if (granularity == Granularity.PerCell)
        {
            foreach (var reagent in _grid.PlacedReagents)
            {
                foreach (var pos in reagent.GetCurrentMetaGridPositions())
                {
                    if (ChemistryEngine.IsAspectSuppressed(_grid, pos))
                        continue;
                    total += reagent.GetAspectsAtMetaGridPosition(pos).GetMagnitude(aspectId);
                }
            }
        }
        else // PerItem
        {
            foreach (var reagent in _grid.PlacedReagents)
            {
                foreach (var pos in reagent.GetCurrentMetaGridPositions())
                {
                    if (ChemistryEngine.IsAspectSuppressed(_grid, pos))
                        continue;
                    if (reagent.GetAspectsAtMetaGridPosition(pos).Has(aspectId))
                    {
                        total++;
                        break;
                    }
                }
            }
        }

        return total;
    }

    /// <summary>Get the current value displayed for a specific aspect.</summary>
    public int GetAspectTotal(string aspectId)
    {
        return _bars.TryGetValue(aspectId, out var bar) ? bar.CurrentValue : 0;
    }

    /// <summary>Get the threshold for a specific aspect.</summary>
    public int GetAspectThreshold(string aspectId)
    {
        return _bars.TryGetValue(aspectId, out var bar) ? bar.Threshold : 0;
    }
}

/// <summary>
/// A single aspect bar: [Label] [colored bar] [current/threshold].
/// </summary>
public partial class AspectBar : HBoxContainer
{
    private string _aspectId = "";
    private int _threshold;
    private int _currentValue;
    private Label _valueLabel = null!;
    private ColorRect _fillRect = null!;
    private ColorRect _bgRect = null!;

    private const int BarWidth = 100;
    private const int BarHeight = 14;

    public int CurrentValue => _currentValue;
    public int Threshold => _threshold;

    public void Initialize(string aspectId, string ruleName, int threshold, Granularity granularity)
    {
        _aspectId = aspectId;
        _threshold = threshold;

        AddThemeConstantOverride("separation", 6);

        // Aspect label
        var label = new Label
        {
            Text = FormatLabel(aspectId),
            CustomMinimumSize = new Vector2(60, 0),
        };
        label.AddThemeColorOverride("font_color", GridConstants.GetAspectColor(aspectId));
        label.AddThemeFontSizeOverride("font_size", 13);
        AddChild(label);

        // Bar background (dark)
        var barContainer = new Control
        {
            CustomMinimumSize = new Vector2(BarWidth, BarHeight),
        };
        barContainer.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        AddChild(barContainer);

        _bgRect = new ColorRect
        {
            Color = new Color(0.15f, 0.15f, 0.2f),
            Size = new Vector2(BarWidth, BarHeight),
        };
        barContainer.AddChild(_bgRect);

        // Bar fill (colored)
        _fillRect = new ColorRect
        {
            Color = GridConstants.GetAspectColor(aspectId),
            Size = new Vector2(0, BarHeight),
        };
        barContainer.AddChild(_fillRect);

        // Value text
        _valueLabel = new Label
        {
            Text = $"0/{threshold}",
            CustomMinimumSize = new Vector2(36, 0),
        };
        _valueLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.65f));
        _valueLabel.AddThemeFontSizeOverride("font_size", 13);
        AddChild(_valueLabel);
    }

    public void SetValue(int value)
    {
        _currentValue = value;
        _valueLabel.Text = $"{value}/{_threshold}";

        // Compute fill width as proportion of threshold
        float ratio = _threshold > 0 ? Mathf.Clamp((float)value / _threshold, 0f, 1f) : 0f;
        _fillRect.Size = new Vector2(ratio * BarWidth, BarHeight);

        // Color shifts: normal → danger when at or above threshold
        if (value >= _threshold)
        {
            _fillRect.Color = new Color(0.9f, 0.2f, 0.15f); // Red for exceeded
            _valueLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.2f, 0.15f));
        }
        else if (value >= _threshold - 1 && _threshold > 1)
        {
            _fillRect.Color = new Color(0.95f, 0.75f, 0.1f); // Yellow for close
            _valueLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.75f, 0.1f));
        }
        else
        {
            _fillRect.Color = GridConstants.GetAspectColor(_aspectId);
            _valueLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.65f));
        }
    }

    private static string FormatLabel(string aspectId) => aspectId switch
    {
        AspectIds.Hot => "Heat:",
        AspectIds.Cold => "Cold:",
        AspectIds.Toxic => "Toxicity:",
        _ => $"{char.ToUpper(aspectId[0])}{aspectId[1..]}:",
    };
}
