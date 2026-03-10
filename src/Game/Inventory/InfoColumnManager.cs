using System.Collections.Generic;
using System.Linq;
using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Game.Visual;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Manages the info column sidebar with contextual views.
/// Only one view is visible at a time:
/// (a) PuzzleOverview (default) — Objective + AspectLegend + Violations
/// (b) ReagentDetail — shape preview, name, flavor, aspect breakdown
/// (c) AspectDetail — single aspect info, description, reactions, which reagents have it
/// </summary>
public partial class InfoColumnManager : VBoxContainer
{
    // Back button (hidden in default view)
    private Button _backButton = null!;

    // View containers — only one visible at a time
    private VBoxContainer _overviewView = null!;
    private VBoxContainer _reagentDetailView = null!;
    private VBoxContainer _aspectDetailView = null!;

    // Overview sub-panels (owned by this manager)
    private ObjectivePanel _objectivePanel = null!;
    private AspectLegendPanel _legendPanel = null!;
    private ViolationListPanel _violationPanel = null!;

    // Reagent detail widgets
    private ProfileShapePreview _rdShapePreview = null!;
    private Label _rdNameLabel = null!;
    private Label _rdFlavorLabel = null!;
    private VBoxContainer _rdAspectList = null!;

    // Aspect detail widgets
    private Label _adNameLabel = null!;
    private Control _adIcon = null!;
    private CenterContainer _adIconCenter = null!;
    private Label _adDescriptionLabel = null!;
    private Label _adConstraintLabel = null!;
    private VBoxContainer _adRulesList = null!;
    private Label _adReagentsHeader = null!;
    private VBoxContainer _adReagentsList = null!;

    // Context
    private List<Reagent> _trayReagents = new();
    private List<ReactionRule> _rules = new();

    // Public access to sub-panels for signal wiring and refresh
    public ObjectivePanel ObjectivePanel => _objectivePanel;
    public AspectLegendPanel LegendPanel => _legendPanel;
    public ViolationListPanel ViolationPanel => _violationPanel;

    /// <summary>Signal emitted when the Back button is pressed.</summary>
    [Signal]
    public delegate void BackPressedEventHandler();

    public InfoColumnManager()
    {
        SizeFlagsVertical = SizeFlags.ShrinkBegin;
        AddThemeConstantOverride("separation", 10);
        CustomMinimumSize = new Vector2(160, 0);

        BuildBackButton();
        BuildOverviewView();
        BuildReagentDetailView();
        BuildAspectDetailView();

        // Default: show overview
        ShowPuzzleOverview();
    }

    /// <summary>
    /// Initialize with puzzle context. Must be called after construction.
    /// </summary>
    public void Initialize(
        PuzzleType puzzleType,
        MetaGrid metaGrid,
        List<ReactionRule> rules,
        List<Reagent> trayReagents)
    {
        _rules = rules;
        _trayReagents = trayReagents;

        _objectivePanel.Initialize(puzzleType);
        _legendPanel.SetPuzzleContext(rules, trayReagents);
        _violationPanel.Initialize(metaGrid, rules);
    }

    /// <summary>Show the default overview: Objective + Legend + Violations.</summary>
    public void ShowPuzzleOverview()
    {
        _backButton.Visible = false;
        _overviewView.Visible = true;
        _reagentDetailView.Visible = false;
        _aspectDetailView.Visible = false;
    }

    /// <summary>Alias for ShowPuzzleOverview.</summary>
    public void ShowDefault() => ShowPuzzleOverview();

    /// <summary>Show detailed info for a specific reagent.</summary>
    public void ShowReagentDetail(Reagent reagent)
    {
        PopulateReagentDetail(reagent);

        _backButton.Visible = true;
        _overviewView.Visible = false;
        _reagentDetailView.Visible = true;
        _aspectDetailView.Visible = false;
    }

    /// <summary>Show detailed info for a specific aspect.</summary>
    public void ShowAspectDetail(string aspectId)
    {
        PopulateAspectDetail(aspectId);

        _backButton.Visible = true;
        _overviewView.Visible = false;
        _reagentDetailView.Visible = false;
        _aspectDetailView.Visible = true;
    }

    /// <summary>Whether the overview (default) view is currently shown.</summary>
    public bool IsShowingOverview => _overviewView.Visible;

    // --- Build methods ---

    private void BuildBackButton()
    {
        _backButton = new Button { Text = "< Back" };
        _backButton.CustomMinimumSize = new Vector2(60, 24);
        _backButton.Visible = false;

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.15f, 0.18f, 0.8f),
            ContentMarginLeft = 6, ContentMarginRight = 6,
            ContentMarginTop = 2, ContentMarginBottom = 2,
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
        };
        _backButton.AddThemeStyleboxOverride("normal", style);

        var hoverStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.2f, 0.25f, 0.9f),
            ContentMarginLeft = 6, ContentMarginRight = 6,
            ContentMarginTop = 2, ContentMarginBottom = 2,
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
        };
        _backButton.AddThemeStyleboxOverride("hover", hoverStyle);

        _backButton.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.65f));
        _backButton.AddThemeFontSizeOverride("font_size", 11);
        _backButton.Pressed += OnBackPressed;
        AddChild(_backButton);
    }

    private void BuildOverviewView()
    {
        _overviewView = new VBoxContainer();
        _overviewView.AddThemeConstantOverride("separation", 10);
        AddChild(_overviewView);

        var objectiveWrapper = new PanelContainer();
        objectiveWrapper.AddThemeStyleboxOverride("panel", UIThemeLoader.GetPanelStyleBox("parchment_plain"));
        _overviewView.AddChild(objectiveWrapper);

        _objectivePanel = new ObjectivePanel();
        objectiveWrapper.AddChild(_objectivePanel);

        _legendPanel = new AspectLegendPanel();
        _overviewView.AddChild(_legendPanel);

        _violationPanel = new ViolationListPanel();
        _overviewView.AddChild(_violationPanel);
    }

    private void BuildReagentDetailView()
    {
        _reagentDetailView = new VBoxContainer();
        _reagentDetailView.AddThemeConstantOverride("separation", 6);
        _reagentDetailView.Visible = false;
        AddChild(_reagentDetailView);

        // Header
        var header = new Label { Text = "REAGENT" };
        header.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        header.AddThemeFontSizeOverride("font_size", 14);
        _reagentDetailView.AddChild(header);

        // Shape preview
        _rdShapePreview = new ProfileShapePreview();
        _reagentDetailView.AddChild(_rdShapePreview);

        // Name
        _rdNameLabel = new Label();
        _rdNameLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.95f, 0.95f));
        _rdNameLabel.AddThemeFontSizeOverride("font_size", 14);
        _reagentDetailView.AddChild(_rdNameLabel);

        // Flavor text
        _rdFlavorLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(140, 0),
        };
        _rdFlavorLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.55f));
        _rdFlavorLabel.AddThemeFontSizeOverride("font_size", 11);
        _reagentDetailView.AddChild(_rdFlavorLabel);

        // Aspect breakdown
        _rdAspectList = new VBoxContainer();
        _rdAspectList.AddThemeConstantOverride("separation", 4);
        _reagentDetailView.AddChild(_rdAspectList);
    }

    private void BuildAspectDetailView()
    {
        _aspectDetailView = new VBoxContainer();
        _aspectDetailView.AddThemeConstantOverride("separation", 6);
        _aspectDetailView.Visible = false;
        AddChild(_aspectDetailView);

        // Header row: icon + name
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 8);
        _aspectDetailView.AddChild(headerRow);

        _adIconCenter = new CenterContainer { CustomMinimumSize = new Vector2(32, 32) };
        _adIcon = new ColorRect { CustomMinimumSize = new Vector2(32, 32) };
        _adIconCenter.AddChild(_adIcon);
        headerRow.AddChild(_adIconCenter);

        _adNameLabel = new Label();
        _adNameLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        _adNameLabel.AddThemeFontSizeOverride("font_size", 14);
        headerRow.AddChild(_adNameLabel);

        // Description
        _adDescriptionLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(140, 0),
        };
        _adDescriptionLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.65f));
        _adDescriptionLabel.AddThemeFontSizeOverride("font_size", 11);
        _aspectDetailView.AddChild(_adDescriptionLabel);

        // Constraint text
        _adConstraintLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(140, 0),
        };
        _adConstraintLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.65f, 0.3f));
        _adConstraintLabel.AddThemeFontSizeOverride("font_size", 11);
        _aspectDetailView.AddChild(_adConstraintLabel);

        // Reactions
        _adRulesList = new VBoxContainer();
        _adRulesList.AddThemeConstantOverride("separation", 2);
        _aspectDetailView.AddChild(_adRulesList);

        // Reagents that have this aspect
        _adReagentsHeader = new Label { Text = "Found in:" };
        _adReagentsHeader.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.65f));
        _adReagentsHeader.AddThemeFontSizeOverride("font_size", 11);
        _aspectDetailView.AddChild(_adReagentsHeader);

        _adReagentsList = new VBoxContainer();
        _adReagentsList.AddThemeConstantOverride("separation", 2);
        _aspectDetailView.AddChild(_adReagentsList);
    }

    // --- Populate methods ---

    private void PopulateReagentDetail(Reagent reagent)
    {
        _rdShapePreview.SetReagent(reagent);
        _rdNameLabel.Text = reagent.Definition.Name;

        if (!string.IsNullOrEmpty(reagent.Definition.FlavorText))
        {
            _rdFlavorLabel.Text = reagent.Definition.FlavorText;
            _rdFlavorLabel.Visible = true;
        }
        else
        {
            _rdFlavorLabel.Visible = false;
        }

        // Clear and rebuild aspect list
        foreach (var child in _rdAspectList.GetChildren())
            child.QueueFree();

        var aspectTotals = new Dictionary<string, (Aspect aspect, int totalMagnitude)>();
        foreach (var pos in reagent.GetLocalCellPositions())
        {
            foreach (var aspect in reagent.GetAspectsAtLocalPosition(pos).Aspects)
            {
                if (aspectTotals.TryGetValue(aspect.Id, out var existing))
                    aspectTotals[aspect.Id] = (existing.aspect, existing.totalMagnitude + aspect.Magnitude);
                else
                    aspectTotals[aspect.Id] = (aspect, aspect.Magnitude);
            }
        }

        foreach (var (id, (aspect, total)) in aspectTotals)
        {
            var entry = new VBoxContainer();
            entry.AddThemeConstantOverride("separation", 2);

            var headerRow = new HBoxContainer();
            headerRow.AddThemeConstantOverride("separation", 6);

            Control icon;
            var rdTileTex = UIThemeLoader.GetAspectTileTexture(id);
            if (rdTileTex != null)
            {
                icon = new TextureRect
                {
                    Texture = rdTileTex,
                    CustomMinimumSize = new Vector2(32, 32),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspect,
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                };
            }
            else
            {
                icon = new ColorRect
                {
                    CustomMinimumSize = new Vector2(32, 32),
                    Color = GridConstants.GetAspectColor(id),
                };
            }
            var iconCenter = new CenterContainer { CustomMinimumSize = new Vector2(32, 32) };
            iconCenter.AddChild(icon);
            headerRow.AddChild(iconCenter);

            var nameLabel = new Label { Text = $"{aspect.Name} x{total}" };
            nameLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.75f));
            nameLabel.AddThemeFontSizeOverride("font_size", 12);
            headerRow.AddChild(nameLabel);

            entry.AddChild(headerRow);

            var metadata = AspectMetadataDatabase.Get(id);
            if (metadata != null)
            {
                var descLabel = new Label
                {
                    Text = metadata.Description,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    CustomMinimumSize = new Vector2(130, 0),
                };
                descLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.5f));
                descLabel.AddThemeFontSizeOverride("font_size", 10);
                entry.AddChild(descLabel);

                if (!string.IsNullOrEmpty(metadata.ConstraintText))
                {
                    var constraintLabel = new Label
                    {
                        Text = metadata.ConstraintText,
                        AutowrapMode = TextServer.AutowrapMode.WordSmart,
                        CustomMinimumSize = new Vector2(130, 0),
                    };
                    constraintLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.65f, 0.3f));
                    constraintLabel.AddThemeFontSizeOverride("font_size", 10);
                    entry.AddChild(constraintLabel);
                }
            }

            _rdAspectList.AddChild(entry);
        }
    }

    private void PopulateAspectDetail(string aspectId)
    {
        var metadata = AspectMetadataDatabase.Get(aspectId);
        if (metadata == null)
        {
            _adNameLabel.Text = Capitalize(aspectId);
            RebuildAspectIcon(aspectId);
            _adDescriptionLabel.Text = "";
            _adConstraintLabel.Visible = false;
            ClearContainer(_adRulesList);
            ClearContainer(_adReagentsList);
            _adReagentsHeader.Visible = false;
            return;
        }

        _adNameLabel.Text = metadata.Name;
        RebuildAspectIcon(aspectId);
        _adDescriptionLabel.Text = metadata.Description;

        if (!string.IsNullOrEmpty(metadata.ConstraintText))
        {
            _adConstraintLabel.Text = $"Constraint: {metadata.ConstraintText}";
            _adConstraintLabel.Visible = true;
        }
        else
        {
            _adConstraintLabel.Visible = false;
        }

        // Reactions involving this aspect
        ClearContainer(_adRulesList);
        var relevantRules = _rules.Where(r => r.TriggerAspects.Contains(aspectId)).ToList();
        if (relevantRules.Count > 0)
        {
            var rulesHeader = new Label { Text = "Reactions:" };
            rulesHeader.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.65f));
            rulesHeader.AddThemeFontSizeOverride("font_size", 11);
            _adRulesList.AddChild(rulesHeader);

            foreach (var rule in relevantRules)
            {
                var ruleLabel = new Label
                {
                    Text = $"  {FormatRule(rule)}",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    CustomMinimumSize = new Vector2(130, 0),
                };
                ruleLabel.AddThemeColorOverride("font_color", GetRuleColor(rule.ResultType));
                ruleLabel.AddThemeFontSizeOverride("font_size", 11);
                _adRulesList.AddChild(ruleLabel);
            }
        }

        // Reagents that have this aspect
        ClearContainer(_adReagentsList);
        var reagentsWithAspect = _trayReagents
            .Where(r => r.GetLocalCellPositions()
                .Any(pos => r.GetAspectsAtLocalPosition(pos).Aspects
                    .Any(a => a.Id == aspectId)))
            .ToList();

        if (reagentsWithAspect.Count > 0)
        {
            _adReagentsHeader.Visible = true;
            foreach (var reagent in reagentsWithAspect)
            {
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 6);

                var dot = new ColorRect
                {
                    CustomMinimumSize = new Vector2(8, 8),
                    Color = GridConstants.GetAspectColor(aspectId),
                };
                var dotCenter = new CenterContainer { CustomMinimumSize = new Vector2(8, 12) };
                dotCenter.AddChild(dot);
                row.AddChild(dotCenter);

                var placed = reagent.PlacedPosition.HasValue ? " (placed)" : "";
                var label = new Label { Text = $"{reagent.Definition.Name}{placed}" };
                label.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.7f));
                label.AddThemeFontSizeOverride("font_size", 11);
                row.AddChild(label);

                _adReagentsList.AddChild(row);
            }
        }
        else
        {
            _adReagentsHeader.Visible = false;
        }
    }

    private void OnBackPressed()
    {
        ShowPuzzleOverview();
        EmitSignal(SignalName.BackPressed);
    }

    private void RebuildAspectIcon(string aspectId)
    {
        _adIcon.QueueFree();
        var tileTex = UIThemeLoader.GetAspectTileTexture(aspectId);
        if (tileTex != null)
        {
            _adIcon = new TextureRect
            {
                Texture = tileTex,
                CustomMinimumSize = new Vector2(32, 32),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspect,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            };
        }
        else
        {
            _adIcon = new ColorRect
            {
                CustomMinimumSize = new Vector2(32, 32),
                Color = GridConstants.GetAspectColor(aspectId),
            };
        }
        _adIconCenter.AddChild(_adIcon);
    }

    // --- Helpers ---

    private static void ClearContainer(Control container)
    {
        foreach (var child in container.GetChildren())
            child.QueueFree();
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];

    private static string FormatRule(ReactionRule rule)
    {
        var aspects = rule.TriggerAspects.Count == 1
            ? Capitalize(rule.TriggerAspects[0])
            : $"{Capitalize(rule.TriggerAspects[0])} + {Capitalize(rule.TriggerAspects[1])}";

        var sensitivity = rule.Sensitivity switch
        {
            SensitivityTier.Adjacent => "adjacent",
            SensitivityTier.SameBag => "same bag",
            SensitivityTier.WholeInventory => "cumulative",
            _ => rule.Sensitivity.ToString(),
        };
        var threshold = rule.Threshold > 0 ? $" >{rule.Threshold}" : "";
        return $"{aspects} ({sensitivity}{threshold}) = {rule.Name}";
    }

    private static Color GetRuleColor(ReactionResultType type) => type switch
    {
        ReactionResultType.Explosion => new Color(1.0f, 0.4f, 0.3f),
        ReactionResultType.Breakage => new Color(0.9f, 0.6f, 0.3f),
        ReactionResultType.ToxicCloud => new Color(0.5f, 0.85f, 0.4f),
        ReactionResultType.Neutralization => new Color(0.5f, 0.7f, 1.0f),
        ReactionResultType.Overheating => new Color(1.0f, 0.5f, 0.3f),
        ReactionResultType.Hypothermia => new Color(0.4f, 0.7f, 1.0f),
        ReactionResultType.Poisoning => new Color(0.6f, 0.9f, 0.4f),
        _ => new Color(0.75f, 0.75f, 0.7f),
    };
}
