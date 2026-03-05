using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Highlight;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Game.Inventory;

public enum ConfirmResult
{
    None,
    NotAllPlaced,
    HasReactions,
    Win,
}

public partial class PuzzleScreen : Control
{
    private MetaGrid _metaGrid = null!;
    private List<Reagent> _trayReagents = null!;
    private Reagent? _selectedReagent;
    private int _selectedIndex = -1;

    private MetaGridView _gridView = null!;
    private ReagentTray _reagentTray = null!;
    private StatusBarPanel _statusBar = null!;
    private ObjectivePanel _objectivePanel = null!;
    private Label _statusLabel = null!;
    private Button _confirmButton = null!;
    private Button _resetButton = null!;
    private Button _primerButton = null!;
    private AspectPrimerPanel _primerPanel = null!;
    private PanelContainer _resultOverlay = null!;
    private Label _resultLabel = null!;
    private Label _dayLabel = null!;
    private Label _streakLabel = null!;
    private PuzzleType _puzzleType = PuzzleType.FitAllSafely;
    private ConfirmResult _lastConfirmResult = ConfirmResult.None;
    private List<ReactionResult> _lastReactions = new();

    /// <summary>
    /// Set before adding to scene tree to load a generated puzzle instead of the demo.
    /// </summary>
    public PuzzleState? PuzzleData { get; set; }

    /// <summary>
    /// Optional header text for the day indicator (e.g., "Day 42"). Set before _Ready().
    /// </summary>
    public string? HeaderDayText { get; set; }

    /// <summary>
    /// Optional header text for streak display (e.g., "Streak: 7"). Set before _Ready().
    /// </summary>
    public string? HeaderStreakText { get; set; }

    // --- Public state inspection ---
    public MetaGrid Grid => _metaGrid;
    public IReadOnlyList<Reagent> TrayReagents => _trayReagents.AsReadOnly();
    public Reagent? SelectedReagent => _selectedReagent;
    public MetaGridView GridView => _gridView;
    public StatusBarPanel StatusBar => _statusBar;
    public ObjectivePanel Objective => _objectivePanel;
    public ConfirmResult LastConfirmResult => _lastConfirmResult;
    public IReadOnlyList<ReactionResult> LastReactions => _lastReactions.AsReadOnly();
    public AspectPrimerPanel PrimerPanel => _primerPanel;

    /// <summary>Fired after ConfirmPuzzle() with the result.</summary>
    public event System.Action<ConfirmResult>? PuzzleConfirmed;

    public override void _Ready()
    {
        // Fill the viewport
        AnchorsPreset = (int)LayoutPreset.FullRect;
        SetAnchorsPreset(LayoutPreset.FullRect);

        if (PuzzleData != null)
        {
            InitializeFromPuzzleState(PuzzleData);
        }
        else
        {
            var (grid, reagents) = DemoPuzzle.Create();
            _metaGrid = grid;
            _trayReagents = reagents;
        }

        BuildUI();
        WireSignals();
        UpdateStatusThresholds();
        UpdateStatus("Select a reagent from the tray, then click the grid to place it.");
    }

    private void InitializeFromPuzzleState(PuzzleState state)
    {
        _puzzleType = state.PuzzleType;
        _metaGrid = PuzzleSolver.ReconstructGrid(state);
        _trayReagents = PuzzleSolver.ReconstructReagents(state);

        // Pre-place locked reagents at their solution positions
        foreach (var placement in state.Solution)
        {
            if (placement.ReagentIndex < 0 || placement.ReagentIndex >= _trayReagents.Count)
                continue;
            var reagent = _trayReagents[placement.ReagentIndex];
            if (!reagent.IsLocked) continue;

            reagent.Rotation = placement.Rotation;
            var pos = new Core.Grid.Vector2I(placement.PositionCol, placement.PositionRow);
            if (_metaGrid.CanPlaceReagent(reagent, pos))
            {
                _metaGrid.PlaceReagent(reagent, pos);
            }
        }
    }

    private void BuildUI()
    {
        // Background
        var bg = new ColorRect
        {
            Color = new Color(0.1f, 0.1f, 0.12f),
            AnchorsPreset = (int)LayoutPreset.FullRect,
        };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Main layout
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(vbox);

        // ---- Header bar ----
        var headerPanel = new PanelContainer();
        var headerStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.12f, 0.15f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            BorderColor = new Color(0.25f, 0.25f, 0.3f),
            BorderWidthBottom = 1,
        };
        headerPanel.AddThemeStyleboxOverride("panel", headerStyle);
        vbox.AddChild(headerPanel);

        var headerHBox = new HBoxContainer();
        headerPanel.AddChild(headerHBox);

        var title = new Label
        {
            Text = "Alchemist Courier",
        };
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        title.AddThemeFontSizeOverride("font_size", 18);
        headerHBox.AddChild(title);

        // Spacer pushes day/streak to the right
        headerHBox.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        _dayLabel = new Label
        {
            Text = HeaderDayText ?? "",
        };
        _dayLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.65f));
        _dayLabel.AddThemeFontSizeOverride("font_size", 14);
        headerHBox.AddChild(_dayLabel);

        if (!string.IsNullOrEmpty(HeaderDayText) && !string.IsNullOrEmpty(HeaderStreakText))
        {
            headerHBox.AddChild(new Control { CustomMinimumSize = new Vector2(16, 0) });
        }

        _streakLabel = new Label
        {
            Text = HeaderStreakText ?? "",
        };
        _streakLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.75f, 0.4f));
        _streakLabel.AddThemeFontSizeOverride("font_size", 14);
        headerHBox.AddChild(_streakLabel);

        // ---- Grid + tray side by side ----
        var hbox = new HBoxContainer();
        hbox.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(hbox);

        // Grid view (centered vertically)
        var gridCenter = new CenterContainer();
        _gridView = new MetaGridView();
        _gridView.Initialize(_metaGrid);
        _gridView.SetReactionRules(ReactionRuleDatabase.GetMvpRules());
        gridCenter.AddChild(_gridView);
        hbox.AddChild(gridCenter);

        // Spacer
        hbox.AddChild(new Control { CustomMinimumSize = new Vector2(20, 0) });

        // Right column: tray + objective
        var rightColumn = new VBoxContainer();
        rightColumn.SizeFlagsVertical = SizeFlags.ExpandFill;
        rightColumn.AddThemeConstantOverride("separation", 10);
        hbox.AddChild(rightColumn);

        // Tray
        var trayScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(200, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _reagentTray = new ReagentTray();
        _reagentTray.Initialize(_trayReagents);
        _reagentTray.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        trayScroll.AddChild(_reagentTray);
        rightColumn.AddChild(trayScroll);

        // Objective panel
        _objectivePanel = new ObjectivePanel();
        _objectivePanel.Initialize(_puzzleType);
        rightColumn.AddChild(_objectivePanel);

        // Aspect interaction primer (toggleable)
        _primerPanel = new AspectPrimerPanel();
        _primerPanel.Initialize(ReactionRuleDatabase.GetMvpRules());
        rightColumn.AddChild(_primerPanel);

        // ---- Bottom section ----
        var bottomSeparator = new HSeparator();
        vbox.AddChild(bottomSeparator);

        // Status bar (cumulative aspect thresholds)
        _statusBar = new StatusBarPanel();
        _statusBar.Initialize(_metaGrid, ReactionRuleDatabase.GetMvpRules());
        vbox.AddChild(_statusBar);

        // Button bar
        var buttonBar = new HBoxContainer();
        buttonBar.Alignment = BoxContainer.AlignmentMode.Center;
        buttonBar.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(buttonBar);

        _confirmButton = CreateStyledButton("Confirm", new Color(0.25f, 0.5f, 0.3f));
        _confirmButton.Pressed += OnConfirmPressed;
        buttonBar.AddChild(_confirmButton);

        _resetButton = CreateStyledButton("Reset", new Color(0.35f, 0.3f, 0.28f));
        _resetButton.Pressed += OnResetPressed;
        buttonBar.AddChild(_resetButton);

        _primerButton = CreateStyledButton("Reactions", new Color(0.3f, 0.3f, 0.4f));
        _primerButton.Pressed += OnPrimerPressed;
        buttonBar.AddChild(_primerButton);

        // Status label (subtle hint text)
        _statusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.5f));
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(_statusLabel);

        // Result overlay — positioned as a centered overlay on the main area
        _resultOverlay = new PanelContainer();
        _resultOverlay.Visible = false;
        _resultOverlay.SetAnchorsPreset(LayoutPreset.Center);
        _resultOverlay.GrowHorizontal = GrowDirection.Both;
        _resultOverlay.GrowVertical = GrowDirection.Both;
        var overlayStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.08f, 0.94f),
            ContentMarginLeft = 32,
            ContentMarginRight = 32,
            ContentMarginTop = 20,
            ContentMarginBottom = 20,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            BorderColor = new Color(0.4f, 0.4f, 0.35f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
        };
        _resultOverlay.AddThemeStyleboxOverride("panel", overlayStyle);
        _resultLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _resultLabel.AddThemeFontSizeOverride("font_size", 18);
        _resultOverlay.AddChild(_resultLabel);
        AddChild(_resultOverlay);
    }

    private static Button CreateStyledButton(string text, Color bgColor)
    {
        var button = new Button { Text = text };
        button.CustomMinimumSize = new Vector2(100, 34);
        var normal = new StyleBoxFlat
        {
            BgColor = bgColor,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        };
        button.AddThemeStyleboxOverride("normal", normal);
        var hover = new StyleBoxFlat
        {
            BgColor = bgColor.Lightened(0.15f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        };
        button.AddThemeStyleboxOverride("hover", hover);
        var pressed = new StyleBoxFlat
        {
            BgColor = bgColor.Darkened(0.15f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        };
        button.AddThemeStyleboxOverride("pressed", pressed);
        return button;
    }

    private void WireSignals()
    {
        _reagentTray.ReagentSelected += OnReagentSelected;
        _gridView.ReagentPlaced += OnReagentPlaced;
        _gridView.ReagentRemoved += OnReagentRemoved;
    }

    private void OnReagentSelected(int index)
    {
        if (index < 0 || index >= _trayReagents.Count) return;

        _selectedReagent = _trayReagents[index];
        _selectedIndex = index;
        _selectedReagent.Rotation = 0;
        _gridView.SetSelectedReagent(_selectedReagent);
        _reagentTray.HighlightIndex(index);
        UpdateStatus($"Placing: {_selectedReagent.Definition.Name}  |  R / Right-click = rotate  |  Left-click = place  |  Esc = cancel");
    }

    private void OnReagentPlaced()
    {
        var name = _selectedReagent?.Definition.Name ?? "Reagent";
        ClearSelectionState();
        _reagentTray.RefreshDisplay();
        _gridView.RefreshDisplay();
        _statusBar.Refresh();
        UpdateStatusThresholds();

        int remaining = CountUnplaced();
        if (remaining == 0)
        {
            UpdateStatus($"Placed {name}. All reagents placed! Right-click a reagent on the grid to remove it.");
        }
        else
        {
            UpdateStatus($"Placed {name}. {remaining} remaining. Select another from the tray.");
        }
    }

    private void OnReagentRemoved()
    {
        _reagentTray.RefreshDisplay();
        _gridView.RefreshDisplay();
        _statusBar.Refresh();
        UpdateStatusThresholds();
        UpdateStatus("Removed reagent. Select one from the tray to place it.");
    }

    // --- Programmatic control (for testing, tutorials, AI) ---

    private void ClearSelectionState()
    {
        _selectedReagent = null;
        _selectedIndex = -1;
        _gridView.SetSelectedReagent(null);
        _reagentTray.ClearSelection();
    }

    public void SelectReagent(int index)
    {
        OnReagentSelected(index);
    }

    public bool PlaceSelectedAt(int col, int row)
    {
        if (_selectedReagent == null) return false;
        var pos = new Core.Grid.Vector2I(col, row);
        if (!_metaGrid.CanPlaceReagent(_selectedReagent, pos)) return false;
        _metaGrid.PlaceReagent(_selectedReagent, pos);
        OnReagentPlaced();
        return true;
    }

    public void RotateSelected()
    {
        if (_selectedReagent == null) return;
        _selectedReagent.RotateClockwise();
        _gridView.SetSelectedReagent(_selectedReagent);
    }

    public void CancelSelection()
    {
        ClearSelectionState();
        UpdateStatus("Cancelled.");
    }

    public bool RemoveReagentAt(int col, int row)
    {
        var pos = new Core.Grid.Vector2I(col, row);
        var reagent = _metaGrid.GetReagentAtCell(pos);
        if (reagent == null) return false;
        _metaGrid.RemoveReagent(reagent);
        reagent.Rotation = 0;
        _reagentTray.RefreshDisplay();
        _gridView.RefreshDisplay();
        _statusBar.Refresh();
        UpdateStatus($"Removed {reagent.Definition.Name}.");
        return true;
    }

    public void ResetPuzzle()
    {
        ClearSelectionState();

        // Remove all non-locked placed reagents
        for (int i = _metaGrid.PlacedReagents.Count - 1; i >= 0; i--)
        {
            var reagent = _metaGrid.PlacedReagents[i];
            if (reagent.IsLocked) continue;
            _metaGrid.RemoveReagent(reagent);
            reagent.Rotation = 0;
        }

        _lastConfirmResult = ConfirmResult.None;
        _lastReactions.Clear();
        HideResultOverlay();
        _reagentTray.RefreshDisplay();
        _gridView.RefreshDisplay();
        _statusBar.Refresh();
        UpdateStatus("Puzzle reset. Select a reagent from the tray to begin.");
    }

    public ConfirmResult ConfirmPuzzle()
    {
        ClearSelectionState();

        if (CountUnplaced() > 0)
        {
            _lastConfirmResult = ConfirmResult.NotAllPlaced;
            _lastReactions.Clear();
            HideResultOverlay();
            UpdateStatus($"Cannot confirm: {CountUnplaced()} reagent(s) not yet placed.");
            PuzzleConfirmed?.Invoke(_lastConfirmResult);
            return _lastConfirmResult;
        }

        // Evaluate chemistry
        var rules = ReactionRuleDatabase.GetMvpRules();
        _lastReactions = ChemistryEngine.Evaluate(_metaGrid, rules);

        if (_lastReactions.Count > 0)
        {
            _lastConfirmResult = ConfirmResult.HasReactions;
            ShowResultOverlay(false, _lastReactions);
            UpdateStatus("Reactions detected! Rearrange reagents to avoid them.");
            PuzzleConfirmed?.Invoke(_lastConfirmResult);
            return _lastConfirmResult;
        }

        _lastConfirmResult = ConfirmResult.Win;
        ShowResultOverlay(true, _lastReactions);
        UpdateStatus("All reagents placed safely! Puzzle complete!");
        PuzzleConfirmed?.Invoke(_lastConfirmResult);
        return _lastConfirmResult;
    }

    public void TogglePrimer()
    {
        _primerPanel.Toggle();
    }

    private void OnResetPressed()
    {
        ResetPuzzle();
    }

    private void OnConfirmPressed()
    {
        ConfirmPuzzle();
    }

    private void OnPrimerPressed()
    {
        TogglePrimer();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape } && _selectedReagent != null)
        {
            ClearSelectionState();
            UpdateStatus("Cancelled. Select a reagent from the tray.");
        }
    }

    private void ShowResultOverlay(bool isWin, List<ReactionResult> reactions)
    {
        if (isWin)
        {
            _resultLabel.Text = "SUCCESS — All reagents placed safely!";
            _resultLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.9f, 0.3f));
        }
        else
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine("FAILED — Reactions triggered:");
            foreach (var r in reactions)
            {
                lines.AppendLine($"  - {FormatReactionType(r.Type)}");
            }
            _resultLabel.Text = lines.ToString().TrimEnd();
            _resultLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.3f, 0.3f));
        }
        _resultOverlay.Visible = true;
    }

    private void HideResultOverlay()
    {
        _resultOverlay.Visible = false;
    }

    private static string FormatReactionType(ReactionResultType type)
    {
        return type switch
        {
            ReactionResultType.Explosion => "Explosion!",
            ReactionResultType.Neutralization => "Neutralization",
            ReactionResultType.Breakage => "Breakage",
            ReactionResultType.Overheating => "Overheating",
            ReactionResultType.Hypothermia => "Hypothermia",
            ReactionResultType.Poisoning => "Poisoning",
            ReactionResultType.ToxicCloud => "Toxic Cloud",
            _ => type.ToString(),
        };
    }

    private void UpdateStatus(string text)
    {
        _statusLabel.Text = text;
    }

    private int CountUnplaced()
    {
        int count = 0;
        foreach (var r in _trayReagents)
        {
            if (!r.IsPlaced) count++;
        }
        return count;
    }

    private void UpdateStatusThresholds()
    {
        var thresholds = new StatusThresholds();
        var rules = ReactionRuleDatabase.GetMvpRules();
        foreach (var rule in rules)
        {
            if (rule.Sensitivity != SensitivityTier.WholeInventory || rule.TriggerAspects.Count != 1)
                continue;
            var aspectId = rule.TriggerAspects[0];
            int current = _statusBar.GetAspectTotal(aspectId);
            thresholds.Set(aspectId, current, rule.Threshold);
        }
        _gridView.SetStatusThresholds(thresholds);
    }
}
