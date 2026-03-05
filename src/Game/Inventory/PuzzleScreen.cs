using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Chemistry;
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
    private PuzzleType _puzzleType = PuzzleType.FitAllSafely;
    private ConfirmResult _lastConfirmResult = ConfirmResult.None;
    private List<ReactionResult> _lastReactions = new();

    /// <summary>
    /// Set before adding to scene tree to load a generated puzzle instead of the demo.
    /// </summary>
    public PuzzleState? PuzzleData { get; set; }

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
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        AddChild(margin);

        var vbox = new VBoxContainer();
        margin.AddChild(vbox);

        // Title
        var title = new Label
        {
            Text = "Alchemist Courier — Grid Prototype",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        title.AddThemeFontSizeOverride("font_size", 20);
        vbox.AddChild(title);

        vbox.AddChild(new HSeparator());

        // Grid + tray side by side
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
        hbox.AddChild(new Control { CustomMinimumSize = new Vector2(24, 0) });

        // Right column: tray + objective
        var rightColumn = new VBoxContainer();
        rightColumn.SizeFlagsVertical = SizeFlags.ExpandFill;
        rightColumn.AddThemeConstantOverride("separation", 12);
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

        // Status bar (cumulative aspect thresholds)
        vbox.AddChild(new HSeparator());
        _statusBar = new StatusBarPanel();
        _statusBar.Initialize(_metaGrid, ReactionRuleDatabase.GetMvpRules());
        vbox.AddChild(_statusBar);

        // Button bar
        var buttonBar = new HBoxContainer();
        buttonBar.Alignment = BoxContainer.AlignmentMode.Center;
        buttonBar.AddThemeConstantOverride("separation", 16);
        vbox.AddChild(buttonBar);

        _resetButton = new Button { Text = "Reset" };
        _resetButton.CustomMinimumSize = new Vector2(100, 32);
        _resetButton.Pressed += OnResetPressed;
        buttonBar.AddChild(_resetButton);

        _confirmButton = new Button { Text = "Confirm" };
        _confirmButton.CustomMinimumSize = new Vector2(100, 32);
        _confirmButton.Pressed += OnConfirmPressed;
        buttonBar.AddChild(_confirmButton);

        _primerButton = new Button { Text = "Reactions" };
        _primerButton.CustomMinimumSize = new Vector2(100, 32);
        _primerButton.Pressed += OnPrimerPressed;
        buttonBar.AddChild(_primerButton);

        // Status label
        _statusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.65f));
        vbox.AddChild(_statusLabel);

        // Result overlay (hidden until confirm)
        _resultOverlay = new PanelContainer();
        _resultOverlay.Visible = false;
        var overlayStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.1f, 0.92f),
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 16,
            ContentMarginBottom = 16,
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
        _resultLabel.AddThemeFontSizeOverride("font_size", 16);
        _resultOverlay.AddChild(_resultLabel);
        vbox.AddChild(_resultOverlay);
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
            return _lastConfirmResult;
        }

        _lastConfirmResult = ConfirmResult.Win;
        ShowResultOverlay(true, _lastReactions);
        UpdateStatus("All reagents placed safely! Puzzle complete!");
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
}
