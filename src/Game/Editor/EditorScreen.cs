using System;
using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Game.Inventory;

namespace AlchemistCourier.Game.Editor;

/// <summary>
/// Main puzzle editor screen. Builds UI procedurally following PuzzleScreen patterns.
/// Left side: MetaGridView showing current puzzle state.
/// Right side: BagEditorPanel and ReagentEditorPanel in a scrollable column.
/// Bottom bar: New, Save, Load buttons and puzzle name field.
/// </summary>
public partial class EditorScreen : Control
{
    private static readonly Color HeaderColor = new(0.9f, 0.85f, 0.7f);
    private static readonly Color LabelColor = new(0.75f, 0.75f, 0.7f);
    private static readonly Color StatusColor = new(0.55f, 0.55f, 0.5f);

    private MetaGridView _gridView = null!;
    private BagEditorPanel _bagEditor = null!;
    private ReagentEditorPanel _reagentEditor = null!;
    private SolverPanel _solverPanel = null!;
    private LineEdit _nameField = null!;
    private Label _statusLabel = null!;

    private MetaGrid _metaGrid = null!;
    private PuzzleState _currentState = new();
    private string? _currentFilePath;

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

        var gridCenter = new CenterContainer();
        _gridView = new MetaGridView();
        _gridView.Initialize(_metaGrid);
        gridCenter.AddChild(_gridView);
        leftColumn.AddChild(gridCenter);

        // Solver panel
        _solverPanel = new SolverPanel();
        _solverPanel.Initialize(_gridView, () => _currentState);
        leftColumn.AddChild(_solverPanel);

        // Right: editor panels in a scrollable column
        var rightScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(300, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        content.AddChild(rightScroll);

        var rightColumn = new VBoxContainer();
        rightColumn.AddThemeConstantOverride("separation", 12);
        rightColumn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        rightScroll.AddChild(rightColumn);

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
        vbox.AddChild(new HSeparator());
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

        var title = new Label { Text = "Puzzle Editor" };
        title.AddThemeColorOverride("font_color", HeaderColor);
        title.AddThemeFontSizeOverride("font_size", 18);
        headerHBox.AddChild(title);
    }

    private void BuildBottomBar(VBoxContainer parent)
    {
        var bar = new HBoxContainer();
        bar.Alignment = BoxContainer.AlignmentMode.Center;
        bar.AddThemeConstantOverride("separation", 10);
        parent.AddChild(bar);

        // Puzzle name field
        var nameLabel = new Label { Text = "Name:" };
        nameLabel.AddThemeColorOverride("font_color", LabelColor);
        nameLabel.AddThemeFontSizeOverride("font_size", 12);
        bar.AddChild(nameLabel);

        _nameField = new LineEdit
        {
            PlaceholderText = "Untitled Puzzle",
            CustomMinimumSize = new Vector2(180, 0),
        };
        bar.AddChild(_nameField);

        // Buttons
        var newBtn = CreateStyledButton("New", new Color(0.35f, 0.35f, 0.4f));
        newBtn.Pressed += OnNewPressed;
        bar.AddChild(newBtn);

        var saveBtn = CreateStyledButton("Save", new Color(0.25f, 0.45f, 0.35f));
        saveBtn.Pressed += OnSavePressed;
        bar.AddChild(saveBtn);

        var loadBtn = CreateStyledButton("Load", new Color(0.3f, 0.35f, 0.45f));
        loadBtn.Pressed += OnLoadPressed;
        bar.AddChild(loadBtn);
    }

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

    private void RefreshGridView()
    {
        if (_gridView == null) return;

        // Re-create the grid view with the new MetaGrid
        var parent = _gridView.GetParent();
        if (parent != null)
        {
            parent.RemoveChild(_gridView);
            _gridView.QueueFree();
        }

        _gridView = new MetaGridView();
        _gridView.Initialize(_metaGrid);
        parent?.AddChild(_gridView);

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
