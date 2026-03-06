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
    private AspectLegendPanel _legendPanel = null!;
    private ItemDetailPanel _itemDetailPanel = null!;
    private Button _hintButton = null!;
    private Label _hintCountLabel = null!;
    private HintState _hintState = new HintState(0);
    private PanelContainer _resultOverlay = null!;
    private Label _resultLabel = null!;
    private Label _dayLabel = null!;
    private Label _streakLabel = null!;
    private PuzzleType _puzzleType = PuzzleType.FitAllSafely;
    private ConfirmResult _lastConfirmResult = ConfirmResult.None;
    private List<ReactionResult> _lastReactions = new();
    private bool _eagleSenseActive;
    private string? _legendSelectedAspect;

    /// <summary>
    /// Set before adding to scene tree to load a generated puzzle instead of the demo.
    /// </summary>
    public PuzzleState? PuzzleData { get; set; }

    /// <summary>Current hint state for testing and external access.</summary>
    public HintState HintState => _hintState;

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
    public AspectLegendPanel LegendPanel => _legendPanel;
    public ItemDetailPanel ItemDetail => _itemDetailPanel;
    public bool EagleSenseActive => _eagleSenseActive;
    public string? LegendSelectedAspect => _legendSelectedAspect;

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
        _hintState = new HintState(state.MaxHints);

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
        _reagentTray.SetHintState(_hintState);
        _reagentTray.Initialize(_trayReagents);
        _reagentTray.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        trayScroll.AddChild(_reagentTray);
        rightColumn.AddChild(trayScroll);

        // Objective panel
        _objectivePanel = new ObjectivePanel();
        _objectivePanel.Initialize(_puzzleType);
        rightColumn.AddChild(_objectivePanel);

        // Item detail panel (Book of Hours style — always visible, shows hovered reagent info)
        _itemDetailPanel = new ItemDetailPanel();
        rightColumn.AddChild(_itemDetailPanel);

        // Aspect legend panel (collapsible entries)
        _legendPanel = new AspectLegendPanel();
        _legendPanel.SetPuzzleContext(ReactionRuleDatabase.GetMvpRules(), _trayReagents);
        rightColumn.AddChild(_legendPanel);

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

        // Hint button + counter
        _hintButton = CreateStyledButton("Hint", new Color(0.4f, 0.35f, 0.55f));
        _hintButton.Pressed += OnHintPressed;
        buttonBar.AddChild(_hintButton);

        _hintCountLabel = new Label
        {
            Text = FormatHintCount(),
        };
        _hintCountLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.65f, 0.85f));
        _hintCountLabel.AddThemeFontSizeOverride("font_size", 12);
        buttonBar.AddChild(_hintCountLabel);

        // Hide hint button entirely when MaxHints == 0
        if (_hintState.MaxHints == 0)
        {
            _hintButton.Visible = false;
            _hintCountLabel.Visible = false;
        }

        _primerButton = CreateStyledButton("Aspects", new Color(0.3f, 0.3f, 0.4f));
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
        _reagentTray.ReagentHovered += OnTrayReagentHovered;
        _reagentTray.ReagentUnhovered += OnTrayReagentUnhovered;
        _gridView.ReagentPlaced += OnReagentPlaced;
        _gridView.ReagentRemoved += OnReagentRemoved;
        _gridView.HoveredReagentChanged += OnGridHoveredReagentChanged;
        _gridView.RejectionFlash += OnRejectionFlash;
        _legendPanel.AspectSelected += OnLegendAspectSelected;
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
        RefreshEagleSense();

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
        RefreshEagleSense();
        UpdateStatus("Removed reagent. Select one from the tray to place it.");
    }

    private void OnTrayReagentHovered(int index)
    {
        if (index >= 0 && index < _trayReagents.Count)
            _itemDetailPanel.ShowReagent(_trayReagents[index]);
    }

    private void OnTrayReagentUnhovered(int index)
    {
        _itemDetailPanel.ShowReagent(null);
    }

    private void OnGridHoveredReagentChanged()
    {
        var reagent = _gridView.HoveredPlacedReagent;
        _itemDetailPanel.ShowReagent(reagent);

        if (reagent != null)
        {
            var aspectIds = new HashSet<string>();
            foreach (var pos in reagent.GetLocalCellPositions())
            {
                foreach (var aspect in reagent.GetAspectsAtLocalPosition(pos).Aspects)
                    aspectIds.Add(aspect.Id);
            }
            _legendPanel.HighlightAspects(aspectIds);
        }
        else
        {
            _legendPanel.ClearHighlight();
        }
    }

    private void OnLegendAspectSelected(string aspectId)
    {
        if (_legendSelectedAspect == aspectId)
        {
            // Click same aspect again: clear
            _legendSelectedAspect = null;
            _gridView.SetHighlights(new Core.Highlight.HighlightSet());
        }
        else
        {
            _legendSelectedAspect = aspectId;
            var highlights = Core.Highlight.HighlightEngine.ComputeLegendHighlights(_metaGrid, aspectId);
            _gridView.SetHighlights(highlights);
        }
    }

    private void OnRejectionFlash(string aspectId)
    {
        _legendPanel.FlashAspect(aspectId);
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

        // Clear IsHinted on all reagents (preserve hint budget — no refund on reset)
        foreach (var reagent in _trayReagents)
            reagent.IsHinted = false;

        _lastConfirmResult = ConfirmResult.None;
        _lastReactions.Clear();
        HideResultOverlay();
        _reagentTray.RefreshDisplay();
        _gridView.RefreshDisplay();
        _statusBar.Refresh();
        UpdateHintButton();
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

    public void ToggleLegend()
    {
        _legendPanel.Visible = !_legendPanel.Visible;
    }

    public void ToggleEagleSense()
    {
        _eagleSenseActive = !_eagleSenseActive;
        _gridView.SetEagleSense(_eagleSenseActive);

        if (_eagleSenseActive)
        {
            var rules = ReactionRuleDatabase.GetMvpRules();
            var highlights = Core.Highlight.HighlightEngine.ComputeEagleSenseHighlights(
                _metaGrid, rules, _gridView.StatusThresholds);
            _gridView.SetHighlights(highlights);
            UpdateStatus("Eagle Sense active — all interactions visible. Press Tab to toggle off.");
        }
        else
        {
            _gridView.SetHighlights(new Core.Highlight.HighlightSet());
            UpdateStatus("Eagle Sense off.");
        }
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
        ToggleLegend();
    }

    private void OnHintPressed()
    {
        var result = UseHint();
        if (result == null)
        {
            UpdateStatus("No hint targets available — all reagents are placed.");
            return;
        }

        switch (result.Type)
        {
            case HintResultType.NoSolutionFound:
                UpdateStatus("No solution found for this reagent.");
                return;
            case HintResultType.NoHintsRemaining:
                UpdateStatus("No hints remaining.");
                return;
            case HintResultType.BlockedByLockedReagent:
                UpdateStatus("Hint blocked — a locked reagent occupies the target position.");
                return;
        }

        if (!result.IsSuccess)
        {
            UpdateStatus($"Hint failed: {result.Type}");
            return;
        }

        var reagentName = _trayReagents[result.ReagentIndex].Definition.Name;
        if (result.DislodgedReagentIndices.Count > 0)
        {
            UpdateStatus($"Hinted: {reagentName} placed at solution position. {result.DislodgedReagentIndices.Count} reagent(s) dislodged.");
        }
        else
        {
            UpdateStatus($"Hinted: {reagentName} placed at solution position.");
        }
    }

    /// <summary>Programmatic hint invocation for testing.</summary>
    public HintResult? UseHint()
    {
        if (PuzzleData == null) return null;

        var reagentIndex = HintEngine.PickNextHintTarget(PuzzleData, _hintState, _trayReagents);
        if (reagentIndex == null) return null;

        var solutionPlacement = HintEngine.GetSolutionPlacement(PuzzleData, reagentIndex.Value);
        if (solutionPlacement == null)
            return new HintResult(HintResultType.NoSolutionFound, reagentIndex.Value);

        bool isSnapBack = _hintState.WasHinted(reagentIndex.Value);

        if (!isSnapBack && !_hintState.HasHintsRemaining)
            return new HintResult(HintResultType.NoHintsRemaining, reagentIndex.Value);

        var result = HintEngine.ExecuteHint(_metaGrid, _trayReagents, reagentIndex.Value, solutionPlacement);

        if (result.IsSuccess && !isSnapBack)
        {
            _hintState.HintsUsed++;
            _hintState.HintedReagentIndices.Add(reagentIndex.Value);
        }

        if (result.IsSuccess)
        {
            ClearSelectionState();
            _reagentTray.RefreshDisplay();
            _gridView.RefreshDisplay();
            _statusBar.Refresh();
            UpdateStatusThresholds();
            RefreshEagleSense();
            UpdateHintButton();
        }

        return result;
    }

    private void UpdateHintButton()
    {
        _hintCountLabel.Text = FormatHintCount();

        // Enable when budget remains OR any unplaced reagent was previously hinted (snap-back)
        bool hasSnapBack = false;
        for (int i = 0; i < _trayReagents.Count; i++)
        {
            if (!_trayReagents[i].IsPlaced && _hintState.WasHinted(i))
            {
                hasSnapBack = true;
                break;
            }
        }
        _hintButton.Disabled = !_hintState.HasHintsRemaining && !hasSnapBack;
    }

    private string FormatHintCount()
    {
        return $"{_hintState.HintsUsed}/{_hintState.MaxHints}";
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true } keyEvent)
        {
            if (keyEvent.Keycode == Key.Escape && _selectedReagent != null)
            {
                ClearSelectionState();
                UpdateStatus("Cancelled. Select a reagent from the tray.");
            }
            else if (keyEvent.Keycode == Key.Tab)
            {
                ToggleEagleSense();
                GetViewport().SetInputAsHandled();
            }
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

    private void RefreshEagleSense()
    {
        if (!_eagleSenseActive) return;
        var rules = ReactionRuleDatabase.GetMvpRules();
        var highlights = Core.Highlight.HighlightEngine.ComputeEagleSenseHighlights(
            _metaGrid, rules, _gridView.StatusThresholds);
        _gridView.SetHighlights(highlights);
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
