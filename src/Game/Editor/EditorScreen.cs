using System;
using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Game.Inventory;

namespace AlchemistCourier.Game.Editor;

/// <summary>
/// Main puzzle editor screen. Builds UI procedurally following PuzzleScreen patterns.
/// Left side: MetaGridView showing current puzzle state.
/// Right side: BagEditorPanel and ReagentEditorPanel in a scrollable column.
/// Bottom bar: New, Save, Load buttons and puzzle name field.
/// Supports a Play mode toggle that lets the author test their puzzle interactively.
/// </summary>
public partial class EditorScreen : Control
{
    private static readonly Color HeaderColor = new(0.9f, 0.85f, 0.7f);
    private static readonly Color LabelColor = new(0.75f, 0.75f, 0.7f);
    private static readonly Color StatusColor = new(0.55f, 0.55f, 0.5f);

    // --- Editor mode UI ---
    private MetaGridView _gridView = null!;
    private BagEditorPanel _bagEditor = null!;
    private ReagentEditorPanel _reagentEditor = null!;
    private SolverPanel _solverPanel = null!;
    private LineEdit _nameField = null!;
    private Label _statusLabel = null!;
    private CenterContainer _gridCenter = null!;

    // Editor-only containers (hidden in play mode)
    private ScrollContainer _editorRightScroll = null!;
    private HBoxContainer _editorBottomBar = null!;
    private HSeparator _editorBottomSeparator = null!;

    // --- Play mode UI (created lazily) ---
    private bool _isPlayMode;
    private Button _playToggleButton = null!;
    private Label _headerTitle = null!;

    // Play mode containers
    private VBoxContainer? _playRightColumn;
    private ReagentTray? _playTray;
    private ObjectivePanel? _playObjective;
    private StatusBarPanel? _playStatusBar;
    private HBoxContainer? _playButtonBar;
    private HSeparator? _playBottomSeparator;

    // Play mode state
    private MetaGrid? _playMetaGrid;
    private List<Reagent>? _playReagents;
    private Reagent? _playSelectedReagent;

    // --- Shared state ---
    private MetaGrid _metaGrid = null!;
    private PuzzleState _currentState = new();
    private string? _currentFilePath;

    /// <summary>Whether the editor is currently in play mode.</summary>
    public bool IsPlayMode => _isPlayMode;

    /// <summary>Test accessor: play mode grid (null when not in play mode).</summary>
    internal MetaGrid? PlayGrid => _playMetaGrid;

    /// <summary>Test accessor: play mode reagent list (null when not in play mode).</summary>
    internal List<Reagent>? PlayReagentList => _playReagents;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        NewPuzzle();
        BuildUI();
    }

    /// <summary>Create a new empty puzzle with one default 3x3 bag.</summary>
    public void NewPuzzle()
    {
        var defaultShape = new bool[3, 3];
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                defaultShape[r, c] = true;

        _currentState = new PuzzleState
        {
            Seed = 0,
            Difficulty = DifficultyLevel.Easy,
            PuzzleType = PuzzleType.FitAllSafely,
            Bags = new List<PuzzleBag> { PuzzleBag.FromMatrix(defaultShape, 0, 0) },
            Reagents = new List<PuzzleReagent>(),
        };
        _currentFilePath = null;

        RebuildMetaGrid();

        if (_bagEditor != null)
        {
            _bagEditor.SetBags(_currentState.Bags);
            _reagentEditor.SetReagents(_currentState.Reagents);
            _solverPanel?.ClearResults();
            _nameField.Text = "";
            UpdateStatus("New puzzle created.");
        }
    }

    /// <summary>Save current puzzle to a file path using PuzzleFile.</summary>
    public void SavePuzzle(string path)
    {
        SyncStateFromEditors();

        var puzzleFile = new PuzzleFile
        {
            Name = string.IsNullOrWhiteSpace(_nameField.Text) ? "Untitled" : _nameField.Text.Trim(),
            Author = "Puzzle Editor",
            CreatedAt = DateTime.UtcNow,
            Puzzle = _currentState,
        };

        try
        {
            PuzzleFile.Save(puzzleFile, path);
            _currentFilePath = path;
            UpdateStatus($"Saved: {System.IO.Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Save failed: {ex.Message}");
        }
    }

    /// <summary>Load a puzzle from a file path using PuzzleFile.</summary>
    public void LoadPuzzle(string path)
    {
        PuzzleFile? puzzleFile;
        try
        {
            puzzleFile = PuzzleFile.Load(path);
        }
        catch (Exception ex)
        {
            UpdateStatus($"Load failed: {ex.Message}");
            return;
        }

        if (puzzleFile == null)
        {
            UpdateStatus("Load failed: invalid puzzle file.");
            return;
        }

        _currentState = puzzleFile.Puzzle;
        _currentFilePath = path;
        _nameField.Text = puzzleFile.Name;

        RebuildMetaGrid();
        _bagEditor.SetBags(_currentState.Bags);
        _reagentEditor.SetReagents(_currentState.Reagents);
        _solverPanel?.ClearResults();
        UpdateStatus($"Loaded: {puzzleFile.Name}");
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

        // Main margin
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
        BuildHeader(vbox);

        // ---- Content area: grid + editor panels side by side ----
        var content = new HBoxContainer();
        content.SizeFlagsVertical = SizeFlags.ExpandFill;
        content.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(content);

        // Left: MetaGridView centered + SolverPanel below
        var leftColumn = new VBoxContainer();
        leftColumn.AddThemeConstantOverride("separation", 8);
        content.AddChild(leftColumn);

        _gridCenter = new CenterContainer();
        _gridView = new MetaGridView();
        _gridView.Initialize(_metaGrid);
        _gridCenter.AddChild(_gridView);
        leftColumn.AddChild(_gridCenter);

        // Solver panel
        _solverPanel = new SolverPanel();
        _solverPanel.Initialize(_gridView, () => _currentState);
        leftColumn.AddChild(_solverPanel);

        // Right: editor panels in a scrollable column
        _editorRightScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(300, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        content.AddChild(_editorRightScroll);

        var rightColumn = new VBoxContainer();
        rightColumn.AddThemeConstantOverride("separation", 12);
        rightColumn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _editorRightScroll.AddChild(rightColumn);

        // Bag editor
        _bagEditor = new BagEditorPanel();
        _bagEditor.Initialize();
        _bagEditor.SetBags(_currentState.Bags);
        _bagEditor.BagsChanged += OnEditorDataChanged;
        rightColumn.AddChild(_bagEditor);

        // Separator
        rightColumn.AddChild(new HSeparator());

        // Reagent editor
        _reagentEditor = new ReagentEditorPanel();
        _reagentEditor.Initialize();
        _reagentEditor.SetReagents(_currentState.Reagents);
        _reagentEditor.ReagentsChanged += OnEditorDataChanged;
        rightColumn.AddChild(_reagentEditor);

        // ---- Bottom bar ----
        _editorBottomSeparator = new HSeparator();
        vbox.AddChild(_editorBottomSeparator);
        BuildBottomBar(vbox);

        // Status label
        _statusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _statusLabel.AddThemeColorOverride("font_color", StatusColor);
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(_statusLabel);
        UpdateStatus("Ready. Add bags and reagents to build a puzzle.");
    }

    private void BuildHeader(VBoxContainer parent)
    {
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
        parent.AddChild(headerPanel);

        var headerHBox = new HBoxContainer();
        headerPanel.AddChild(headerHBox);

        _headerTitle = new Label { Text = "Puzzle Editor" };
        _headerTitle.AddThemeColorOverride("font_color", HeaderColor);
        _headerTitle.AddThemeFontSizeOverride("font_size", 18);
        headerHBox.AddChild(_headerTitle);

        // Spacer
        headerHBox.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // Play/Edit toggle button
        _playToggleButton = CreateStyledButton("Play", new Color(0.3f, 0.45f, 0.55f));
        _playToggleButton.Pressed += OnPlayTogglePressed;
        headerHBox.AddChild(_playToggleButton);
    }

    private void BuildBottomBar(VBoxContainer parent)
    {
        _editorBottomBar = new HBoxContainer();
        _editorBottomBar.Alignment = BoxContainer.AlignmentMode.Center;
        _editorBottomBar.AddThemeConstantOverride("separation", 10);
        parent.AddChild(_editorBottomBar);

        // Puzzle name field
        var nameLabel = new Label { Text = "Name:" };
        nameLabel.AddThemeColorOverride("font_color", LabelColor);
        nameLabel.AddThemeFontSizeOverride("font_size", 12);
        _editorBottomBar.AddChild(nameLabel);

        _nameField = new LineEdit
        {
            PlaceholderText = "Untitled Puzzle",
            CustomMinimumSize = new Vector2(180, 0),
        };
        _editorBottomBar.AddChild(_nameField);

        // Buttons
        var newBtn = CreateStyledButton("New", new Color(0.35f, 0.35f, 0.4f));
        newBtn.Pressed += OnNewPressed;
        _editorBottomBar.AddChild(newBtn);

        var saveBtn = CreateStyledButton("Save", new Color(0.25f, 0.45f, 0.35f));
        saveBtn.Pressed += OnSavePressed;
        _editorBottomBar.AddChild(saveBtn);

        var loadBtn = CreateStyledButton("Load", new Color(0.3f, 0.35f, 0.45f));
        loadBtn.Pressed += OnLoadPressed;
        _editorBottomBar.AddChild(loadBtn);
    }

    // ---- Play Mode ----

    private void OnPlayTogglePressed()
    {
        if (_isPlayMode)
            ExitPlayMode();
        else
            EnterPlayMode();
    }

    /// <summary>Enter play mode: build a playable puzzle from the current editor state.</summary>
    public void EnterPlayMode()
    {
        if (_isPlayMode) return;

        SyncStateFromEditors();

        if (_currentState.Reagents.Count == 0)
        {
            UpdateStatus("Cannot play: no reagents defined.");
            return;
        }

        _isPlayMode = true;
        _playToggleButton.Text = "Back to Edit";
        _headerTitle.Text = "Puzzle Editor — Play Mode";

        // Build play-mode grid and reagents from current state
        _playMetaGrid = PuzzleSolver.ReconstructGrid(_currentState);
        _playReagents = PuzzleSolver.ReconstructReagents(_currentState);

        // Replace grid view with play-mode grid
        ReplaceGridView(_playMetaGrid);
        _gridView.SetReactionRules(ReactionRuleDatabase.GetMvpRules());

        // Hide editor panels
        _editorRightScroll.Visible = false;
        _solverPanel.Visible = false;
        _editorBottomBar.Visible = false;
        _editorBottomSeparator.Visible = false;

        // Build play mode UI
        BuildPlayModeUI();

        // Wire play-mode signals
        _playTray!.ReagentSelected += OnPlayReagentSelected;
        _gridView.ReagentPlaced += OnPlayReagentPlaced;
        _gridView.ReagentRemoved += OnPlayReagentRemoved;

        UpdateStatus("Play mode — select a reagent from the tray, then click the grid to place it.");
    }

    /// <summary>Exit play mode: discard placements and restore editor state.</summary>
    public void ExitPlayMode()
    {
        if (!_isPlayMode) return;

        _isPlayMode = false;
        _playToggleButton.Text = "Play";
        _headerTitle.Text = "Puzzle Editor";
        _playSelectedReagent = null;

        // Tear down play mode UI
        TearDownPlayModeUI();

        // Restore editor grid
        ReplaceGridView(_metaGrid);

        // Show editor panels
        _editorRightScroll.Visible = true;
        _solverPanel.Visible = true;
        _editorBottomBar.Visible = true;
        _editorBottomSeparator.Visible = true;

        _solverPanel.UpdateGridView(_gridView);

        // Discard play state
        _playMetaGrid = null;
        _playReagents = null;

        UpdateStatus("Back to edit mode. Placements discarded.");
    }

    private void BuildPlayModeUI()
    {
        // The parent of _editorRightScroll is the content HBoxContainer
        var contentHBox = _editorRightScroll.GetParent<HBoxContainer>();

        // Right column: tray + objective
        _playRightColumn = new VBoxContainer();
        _playRightColumn.SizeFlagsVertical = SizeFlags.ExpandFill;
        _playRightColumn.AddThemeConstantOverride("separation", 10);
        contentHBox.AddChild(_playRightColumn);

        // Reagent tray in a scroll container
        var trayScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(200, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _playTray = new ReagentTray();
        _playTray.Initialize(_playReagents!);
        _playTray.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        trayScroll.AddChild(_playTray);
        _playRightColumn.AddChild(trayScroll);

        // Objective panel
        _playObjective = new ObjectivePanel();
        _playObjective.Initialize(_currentState.PuzzleType);
        _playRightColumn.AddChild(_playObjective);

        // Bottom section: separator + status bar + buttons
        var vbox = _editorBottomSeparator.GetParent<VBoxContainer>();

        _playBottomSeparator = new HSeparator();
        // Insert before the status label (which is the last child)
        int statusIdx = _statusLabel.GetIndex();
        vbox.AddChild(_playBottomSeparator);
        vbox.MoveChild(_playBottomSeparator, statusIdx);

        _playStatusBar = new StatusBarPanel();
        _playStatusBar.Initialize(_playMetaGrid!, ReactionRuleDatabase.GetMvpRules());
        vbox.AddChild(_playStatusBar);
        vbox.MoveChild(_playStatusBar, statusIdx + 1);

        _playButtonBar = new HBoxContainer();
        _playButtonBar.Alignment = BoxContainer.AlignmentMode.Center;
        _playButtonBar.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(_playButtonBar);
        vbox.MoveChild(_playButtonBar, statusIdx + 2);

        var resetBtn = CreateStyledButton("Reset", new Color(0.35f, 0.3f, 0.28f));
        resetBtn.Pressed += OnPlayResetPressed;
        _playButtonBar.AddChild(resetBtn);
    }

    private void TearDownPlayModeUI()
    {
        if (_playRightColumn != null)
        {
            _playRightColumn.QueueFree();
            _playRightColumn = null;
            _playTray = null;
            _playObjective = null;
        }

        if (_playStatusBar != null)
        {
            _playStatusBar.QueueFree();
            _playStatusBar = null;
        }

        if (_playButtonBar != null)
        {
            _playButtonBar.QueueFree();
            _playButtonBar = null;
        }

        if (_playBottomSeparator != null)
        {
            _playBottomSeparator.QueueFree();
            _playBottomSeparator = null;
        }
    }

    private void OnPlayReagentSelected(int index)
    {
        if (_playReagents == null || index < 0 || index >= _playReagents.Count) return;

        _playSelectedReagent = _playReagents[index];
        _playSelectedReagent.Rotation = 0;
        _gridView.SetSelectedReagent(_playSelectedReagent);
        _playTray!.HighlightIndex(index);
        UpdateStatus($"Placing: {_playSelectedReagent.Definition.Name}  |  Right-click = rotate  |  Left-click = place  |  Esc = cancel");
    }

    private void OnPlayReagentPlaced()
    {
        var name = _playSelectedReagent?.Definition.Name ?? "Reagent";
        ClearPlaySelection();
        _playTray!.RefreshDisplay();
        _gridView.RefreshDisplay();
        _playStatusBar!.Refresh();

        int remaining = CountPlayUnplaced();
        if (remaining == 0)
            UpdateStatus($"Placed {name}. All reagents placed!");
        else
            UpdateStatus($"Placed {name}. {remaining} remaining.");
    }

    private void OnPlayReagentRemoved()
    {
        _playTray!.RefreshDisplay();
        _gridView.RefreshDisplay();
        _playStatusBar!.Refresh();
        UpdateStatus("Removed reagent. Select one from the tray to place it.");
    }

    private void OnPlayResetPressed()
    {
        if (_playMetaGrid == null || _playReagents == null) return;

        ClearPlaySelection();

        // Remove all placed reagents
        for (int i = _playMetaGrid.PlacedReagents.Count - 1; i >= 0; i--)
        {
            var reagent = _playMetaGrid.PlacedReagents[i];
            _playMetaGrid.RemoveReagent(reagent);
            reagent.Rotation = 0;
        }

        _playTray!.RefreshDisplay();
        _gridView.RefreshDisplay();
        _playStatusBar!.Refresh();
        UpdateStatus("Puzzle reset. Select a reagent from the tray.");
    }

    private void ClearPlaySelection()
    {
        _playSelectedReagent = null;
        _gridView.SetSelectedReagent(null);
        _playTray?.ClearSelection();
    }

    private int CountPlayUnplaced()
    {
        if (_playReagents == null) return 0;
        int count = 0;
        foreach (var r in _playReagents)
        {
            if (!r.IsPlaced) count++;
        }
        return count;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!_isPlayMode) return;
        if (@event is InputEventKey { Pressed: true } keyEvent)
        {
            if (keyEvent.Keycode == Key.Escape && _playSelectedReagent != null)
            {
                ClearPlaySelection();
                UpdateStatus("Cancelled. Select a reagent from the tray.");
            }
        }
    }

    // ---- Editor mode handlers ----

    private void OnNewPressed()
    {
        NewPuzzle();
        RefreshGridView();
    }

    private void OnSavePressed()
    {
        string puzzlesDir = ProjectSettings.GlobalizePath("user://puzzles");
        string name = string.IsNullOrWhiteSpace(_nameField.Text) ? "untitled" : _nameField.Text.Trim();
        string safeName = SanitizeFileName(name);
        string path = _currentFilePath ?? System.IO.Path.Combine(puzzlesDir, $"{safeName}.json");
        SavePuzzle(path);
    }

    private void OnLoadPressed()
    {
        // Open a native file dialog for loading
        var dialog = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Userdata,
            Filters = new string[] { "*.json ; Puzzle Files" },
            Title = "Load Puzzle",
        };
        dialog.FileSelected += path =>
        {
            LoadPuzzle(path);
            dialog.QueueFree();
        };
        dialog.Canceled += () => dialog.QueueFree();
        AddChild(dialog);
        dialog.PopupCentered(new Godot.Vector2I(600, 400));
    }

    private void OnEditorDataChanged()
    {
        SyncStateFromEditors();
        RebuildMetaGrid();
        RefreshGridView();
        _solverPanel?.ClearResults();
    }

    private void SyncStateFromEditors()
    {
        _currentState.Bags = _bagEditor.GetBags();
        _currentState.Reagents = _reagentEditor.GetReagents();
    }

    private void RebuildMetaGrid()
    {
        // Compute grid size from bags
        int maxCol = 0, maxRow = 0;
        foreach (var bag in _currentState.Bags)
        {
            var shape = bag.ToShapeMatrix();
            if (shape.GetLength(0) == 0) continue;
            int endCol = bag.PositionCol + shape.GetLength(1);
            int endRow = bag.PositionRow + shape.GetLength(0);
            if (endCol > maxCol) maxCol = endCol;
            if (endRow > maxRow) maxRow = endRow;
        }

        // Minimum 6x6 grid
        int width = Math.Max(maxCol + 2, 6);
        int height = Math.Max(maxRow + 2, 6);

        _metaGrid = new MetaGrid(width, height);

        for (int i = 0; i < _currentState.Bags.Count; i++)
        {
            var puzzleBag = _currentState.Bags[i];
            var shape = puzzleBag.ToShapeMatrix();
            if (shape.GetLength(0) == 0) continue;
            var properties = ParseBagProperties(puzzleBag.Properties);
            var def = new BagDefinition($"bag_{i}", $"Bag {i}", shape, properties);
            var bag = new Bag(def);
            _metaGrid.PlaceBag(bag, new Core.Grid.Vector2I(puzzleBag.PositionCol, puzzleBag.PositionRow));
        }
    }

    private void ReplaceGridView(MetaGrid grid)
    {
        if (_gridView != null)
        {
            _gridCenter.RemoveChild(_gridView);
            _gridView.QueueFree();
        }

        _gridView = new MetaGridView();
        _gridView.Initialize(grid);
        _gridCenter.AddChild(_gridView);
    }

    private void RefreshGridView()
    {
        if (_gridView == null) return;
        ReplaceGridView(_metaGrid);
        _solverPanel?.UpdateGridView(_gridView);
    }

    private void UpdateStatus(string message)
    {
        if (_statusLabel != null)
            _statusLabel.Text = message;
    }

    private static List<BagPropertyType> ParseBagProperties(List<string> propertyStrings)
    {
        var properties = new List<BagPropertyType>();
        foreach (var s in propertyStrings)
        {
            if (Enum.TryParse<BagPropertyType>(s, out var prop))
                properties.Add(prop);
        }
        return properties;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var sanitized = new System.Text.StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (Array.IndexOf(invalid, c) < 0)
                sanitized.Append(c);
            else
                sanitized.Append('_');
        }
        return sanitized.ToString();
    }

    private static Button CreateStyledButton(string text, Color bgColor)
    {
        var button = new Button { Text = text };
        var normal = new StyleBoxFlat
        {
            BgColor = bgColor,
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        };
        button.AddThemeStyleboxOverride("normal", normal);

        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = bgColor.Lightened(0.15f);
        button.AddThemeStyleboxOverride("hover", hover);

        var pressed = (StyleBoxFlat)normal.Duplicate();
        pressed.BgColor = bgColor.Darkened(0.1f);
        button.AddThemeStyleboxOverride("pressed", pressed);

        return button;
    }
}
