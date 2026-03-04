using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Game.Inventory;

public partial class PuzzleScreen : Control
{
    private MetaGrid _metaGrid = null!;
    private List<Reagent> _trayReagents = null!;
    private Reagent? _selectedReagent;
    private int _selectedIndex = -1;

    private MetaGridView _gridView = null!;
    private ReagentTray _reagentTray = null!;
    private StatusBarPanel _statusBar = null!;
    private Label _statusLabel = null!;

    // --- Public state inspection ---
    public MetaGrid Grid => _metaGrid;
    public IReadOnlyList<Reagent> TrayReagents => _trayReagents.AsReadOnly();
    public Reagent? SelectedReagent => _selectedReagent;
    public MetaGridView GridView => _gridView;
    public StatusBarPanel StatusBar => _statusBar;

    public override void _Ready()
    {
        // Fill the viewport
        AnchorsPreset = (int)LayoutPreset.FullRect;
        SetAnchorsPreset(LayoutPreset.FullRect);

        // Create demo puzzle
        var (grid, reagents) = DemoPuzzle.Create();
        _metaGrid = grid;
        _trayReagents = reagents;

        BuildUI();
        WireSignals();
        UpdateStatus("Select a reagent from the tray, then click the grid to place it.");
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
        hbox.AddChild(trayScroll);

        // Status bar (cumulative aspect thresholds)
        vbox.AddChild(new HSeparator());
        _statusBar = new StatusBarPanel();
        _statusBar.Initialize(_metaGrid, ReactionRuleDatabase.GetMvpRules());
        vbox.AddChild(_statusBar);

        // Status label
        _statusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.65f));
        vbox.AddChild(_statusLabel);
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
        _selectedReagent = null;
        _selectedIndex = -1;
        _gridView.SetSelectedReagent(null);
        _reagentTray.ClearSelection();
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
        _selectedReagent = null;
        _selectedIndex = -1;
        _gridView.SetSelectedReagent(null);
        _reagentTray.ClearSelection();
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

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape } && _selectedReagent != null)
        {
            _selectedReagent = null;
            _selectedIndex = -1;
            _gridView.SetSelectedReagent(null);
            _reagentTray.ClearSelection();
            UpdateStatus("Cancelled. Select a reagent from the tray.");
        }
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
