using System.Collections.Generic;
using System.Linq;
using Godot;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Core.Visual;
using AlchemistCourier.Game.Animation;
using AlchemistCourier.Game.Editor;
using AlchemistCourier.Game.Inventory;
using AlchemistCourier.Game.Visual;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Tools;

/// <summary>
/// Sandbox tool with two modes:
/// Catalog tab: scrollable grid showing all reagent sprites grouped by aspect, filterable.
/// Playground tab: free-form 12x12 grid where the user can draw bags, place reagents, and trigger reactions.
/// </summary>
public partial class SandboxScreen : Control
{
    private const int PreviewCellSize = 32;
    private const int LargePreviewCellSize = 64;
    private const int PlaygroundGridSize = 12;

    private static readonly Color BgColor = new(0.12f, 0.12f, 0.16f);
    private static readonly Color HeaderColor = new(0.9f, 0.85f, 0.7f);
    private static readonly Color SubHeaderColor = new(0.7f, 0.75f, 0.85f);
    private static readonly Color LabelColor = new(0.75f, 0.75f, 0.75f);
    private static readonly Color CardBgColor = new(0.18f, 0.18f, 0.22f);
    private static readonly Color ActiveTabColor = new(0.3f, 0.3f, 0.4f);
    private static readonly Color InactiveTabColor = new(0.18f, 0.18f, 0.22f);
    private static readonly Color SelectedCardColor = new(0.3f, 0.35f, 0.5f);

    // Catalog mode state
    private VisualState _currentState = VisualState.Normal;
    private string? _aspectFilter;
    private string? _shapeFilter;

    private VBoxContainer _catalogContent = null!;
    private Label _countLabel = null!;
    private HBoxContainer _stateButtons = null!;
    private OptionButton _aspectDropdown = null!;
    private OptionButton _shapeDropdown = null!;

    private readonly List<(SpriteStateRenderer renderer, ReagentSpriteSheet sheet)> _spriteRenderers = new();
    private readonly List<(ColorRect rect, string aspectId)> _fallbackCells = new();

    // Tab management
    private Control _catalogContainer = null!;
    private Control? _playgroundContainer;
    private Button _tabCatalog = null!;
    private Button _tabPlayground = null!;
    private VBoxContainer _root = null!;
    private bool _isCatalogActive = true;

    // Playground state
    private MetaGrid? _playGrid;
    private MetaGridView? _playGridView;
    private BagEditorPanel? _bagEditor;
    private ActionHistory? _actionHistory;
    private ReactionAnimator? _reactionAnimator;
    private StatusBarPanel? _playStatusBar;
    private Reagent? _selectedReagent;
    private Button? _undoButton;
    private Button? _redoButton;
    private Button? _confirmButton;
    private Label? _playStatusLabel;
    private PanelContainer? _selectedCard;
    private readonly List<Reagent> _playgroundReagents = new();

    /// <summary>Grid view for testing.</summary>
    public MetaGridView? PlayGridView => _playGridView;

    /// <summary>MetaGrid for testing.</summary>
    public MetaGrid? PlayGrid => _playGrid;

    /// <summary>Action history for testing.</summary>
    public ActionHistory? PlayActionHistory => _actionHistory;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect { Color = BgColor, LayoutMode = 1 };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        _root = new VBoxContainer { LayoutMode = 2 };
        _root.SetAnchorsPreset(LayoutPreset.FullRect);
        _root.AddThemeConstantOverride("separation", 0);
        AddChild(_root);

        BuildHeaderBar();
        BuildCatalogContainer();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isCatalogActive && @event is InputEventKey key && key.Pressed)
        {
            if (key.Keycode == Key.Z && key.CtrlPressed)
            {
                OnUndo();
                GetViewport().SetInputAsHandled();
            }
            else if (key.Keycode == Key.Y && key.CtrlPressed)
            {
                OnRedo();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    // ─── Header ───────────────────────────────────────────────────────

    private void BuildHeaderBar()
    {
        var headerMargin = new MarginContainer { LayoutMode = 2 };
        headerMargin.AddThemeConstantOverride("margin_left", 16);
        headerMargin.AddThemeConstantOverride("margin_right", 16);
        headerMargin.AddThemeConstantOverride("margin_top", 10);
        headerMargin.AddThemeConstantOverride("margin_bottom", 6);

        var headerBar = new HBoxContainer { LayoutMode = 2 };
        headerBar.AddThemeConstantOverride("separation", 16);

        var title = new Label { Text = "Sandbox", LayoutMode = 2 };
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeColorOverride("font_color", HeaderColor);
        headerBar.AddChild(title);

        _tabCatalog = CreateTabButton("Catalog", true);
        _tabCatalog.Pressed += () => SwitchTab(true);
        _tabPlayground = CreateTabButton("Playground", false);
        _tabPlayground.Pressed += () => SwitchTab(false);
        headerBar.AddChild(_tabCatalog);
        headerBar.AddChild(_tabPlayground);

        var spacer = new Control { LayoutMode = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        headerBar.AddChild(spacer);

        _countLabel = new Label { Text = "0 items", LayoutMode = 2 };
        _countLabel.AddThemeFontSizeOverride("font_size", 14);
        _countLabel.AddThemeColorOverride("font_color", LabelColor);
        headerBar.AddChild(_countLabel);

        headerMargin.AddChild(headerBar);
        _root.AddChild(headerMargin);
    }

    private void SwitchTab(bool catalog)
    {
        _isCatalogActive = catalog;
        UpdateTabStyles(_tabCatalog, catalog);
        UpdateTabStyles(_tabPlayground, !catalog);
        _countLabel.Visible = catalog;

        _catalogContainer.Visible = catalog;

        if (!catalog)
        {
            if (_playgroundContainer == null)
                BuildPlaygroundContainer();
            _playgroundContainer!.Visible = true;
        }
        else if (_playgroundContainer != null)
        {
            _playgroundContainer.Visible = false;
        }
    }

    private void UpdateTabStyles(Button btn, bool active)
    {
        var style = new StyleBoxFlat
        {
            BgColor = active ? ActiveTabColor : InactiveTabColor,
        };
        style.SetCornerRadiusAll(4);
        style.SetContentMarginAll(6);
        btn.AddThemeStyleboxOverride("normal", style);
        btn.AddThemeStyleboxOverride("hover", style);
        btn.AddThemeStyleboxOverride("pressed", style);
    }

    // ─── Catalog Tab ──────────────────────────────────────────────────

    private void BuildCatalogContainer()
    {
        _catalogContainer = new VBoxContainer { LayoutMode = 2, SizeFlagsVertical = SizeFlags.ExpandFill };
        ((VBoxContainer)_catalogContainer).AddThemeConstantOverride("separation", 0);

        // Filter bar
        var filterMargin = new MarginContainer { LayoutMode = 2 };
        filterMargin.AddThemeConstantOverride("margin_left", 16);
        filterMargin.AddThemeConstantOverride("margin_right", 16);
        filterMargin.AddThemeConstantOverride("margin_bottom", 6);

        var filterBar = new HBoxContainer { LayoutMode = 2 };
        filterBar.AddThemeConstantOverride("separation", 12);

        var aspectLabel = new Label { Text = "Aspect:", LayoutMode = 2 };
        aspectLabel.AddThemeFontSizeOverride("font_size", 13);
        aspectLabel.AddThemeColorOverride("font_color", LabelColor);
        filterBar.AddChild(aspectLabel);

        _aspectDropdown = new OptionButton { LayoutMode = 2 };
        _aspectDropdown.AddItem("All");
        string[] aspectIds = { AspectIds.Hot, AspectIds.Cold, AspectIds.Volatile, AspectIds.Toxic,
                               AspectIds.Heavy, AspectIds.Stable, AspectIds.Sharp, AspectIds.Fragile };
        foreach (var id in aspectIds)
            _aspectDropdown.AddItem(id);
        _aspectDropdown.ItemSelected += OnAspectFilterChanged;
        filterBar.AddChild(_aspectDropdown);

        var shapeLabel = new Label { Text = "Shape:", LayoutMode = 2 };
        shapeLabel.AddThemeFontSizeOverride("font_size", 13);
        shapeLabel.AddThemeColorOverride("font_color", LabelColor);
        filterBar.AddChild(shapeLabel);

        _shapeDropdown = new OptionButton { LayoutMode = 2 };
        _shapeDropdown.AddItem("All");
        foreach (var name in ReagentCatalog.ShapeNames)
            _shapeDropdown.AddItem(name);
        _shapeDropdown.ItemSelected += OnShapeFilterChanged;
        filterBar.AddChild(_shapeDropdown);

        var stateLabel = new Label { Text = "State:", LayoutMode = 2 };
        stateLabel.AddThemeFontSizeOverride("font_size", 13);
        stateLabel.AddThemeColorOverride("font_color", LabelColor);
        filterBar.AddChild(stateLabel);

        _stateButtons = new HBoxContainer { LayoutMode = 2 };
        _stateButtons.AddThemeConstantOverride("separation", 4);
        var states = new[]
        {
            VisualState.Normal, VisualState.Heated, VisualState.Cooled,
            VisualState.ThermalStress, VisualState.Crushed,
            VisualState.Breaking, VisualState.Exploding
        };
        foreach (var state in states)
        {
            var btn = new Button
            {
                Text = state.ToString(),
                LayoutMode = 2,
                ToggleMode = true,
                ButtonPressed = state == VisualState.Normal,
                Flat = true
            };
            btn.AddThemeFontSizeOverride("font_size", 11);
            btn.Pressed += () => OnStateToggled(state, btn);
            _stateButtons.AddChild(btn);
        }
        filterBar.AddChild(_stateButtons);

        filterMargin.AddChild(filterBar);
        _catalogContainer.AddChild(filterMargin);

        // Scrollable catalog content
        var scrollContainer = new ScrollContainer
        {
            LayoutMode = 2,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };

        _catalogContent = new VBoxContainer
        {
            LayoutMode = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _catalogContent.AddThemeConstantOverride("separation", 16);
        scrollContainer.AddChild(_catalogContent);

        var contentMargin = new MarginContainer
        {
            LayoutMode = 2,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        contentMargin.AddThemeConstantOverride("margin_left", 16);
        contentMargin.AddThemeConstantOverride("margin_right", 16);
        contentMargin.AddThemeConstantOverride("margin_bottom", 16);
        contentMargin.AddChild(scrollContainer);
        _catalogContainer.AddChild(contentMargin);

        _root.AddChild(_catalogContainer);
        BuildCatalog();
    }

    // ─── Playground Tab ───────────────────────────────────────────────

    private void BuildPlaygroundContainer()
    {
        _actionHistory = new ActionHistory();
        _playGrid = new MetaGrid(PlaygroundGridSize, PlaygroundGridSize);

        // Add a default 4x4 Normal bag at origin
        var defaultBagDef = new BagDefinition("bag_0", "Bag 1",
            new bool[,] { { true, true, true, true },
                          { true, true, true, true },
                          { true, true, true, true },
                          { true, true, true, true } });
        var defaultBag = new Bag(defaultBagDef);
        _playGrid.PlaceBag(defaultBag, new CoreVec(0, 0));

        _playgroundContainer = new VBoxContainer
        {
            LayoutMode = 2,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        ((VBoxContainer)_playgroundContainer).AddThemeConstantOverride("separation", 0);

        // Main content: HBox with grid (left) + sidebar (right)
        var mainHBox = new HBoxContainer
        {
            LayoutMode = 2,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        mainHBox.AddThemeConstantOverride("separation", 8);

        // Left: grid area with margin
        var gridMargin = new MarginContainer { LayoutMode = 2 };
        gridMargin.AddThemeConstantOverride("margin_left", 16);
        gridMargin.AddThemeConstantOverride("margin_top", 8);
        gridMargin.AddThemeConstantOverride("margin_bottom", 8);

        _playGridView = new MetaGridView();
        _playGridView.Initialize(_playGrid);
        _playGridView.SetReactionRules(ReactionRuleDatabase.GetMvpRules());
        _playGridView.SetActionHistory(_actionHistory);
        _playGridView.ReagentPlaced += OnPlaygroundReagentPlaced;
        _playGridView.ReagentRemoved += OnPlaygroundReagentRemoved;

        gridMargin.AddChild(_playGridView);
        mainHBox.AddChild(gridMargin);

        // Right: sidebar with bag editor + reagent picker
        var sidebar = new VBoxContainer
        {
            LayoutMode = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Godot.Vector2(240, 0)
        };
        sidebar.AddThemeConstantOverride("separation", 4);

        var sideScroll = new ScrollContainer
        {
            LayoutMode = 2,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };

        var sideContent = new VBoxContainer
        {
            LayoutMode = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        sideContent.AddThemeConstantOverride("separation", 8);

        // Bag editor section
        _bagEditor = new BagEditorPanel();
        _bagEditor.Initialize();
        // Set initial bag matching the default
        var initialBag = PuzzleBag.FromMatrix(new bool[,] {
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true } }, 0, 0);
        _bagEditor.SetBags(new List<PuzzleBag> { initialBag });

        sideContent.AddChild(_bagEditor);

        // Apply Bags button
        var applyBagsBtn = new Button
        {
            Text = "Apply Bags",
            LayoutMode = 2,
            CustomMinimumSize = new Godot.Vector2(0, 34)
        };
        ApplyButtonStyle(applyBagsBtn, new Color(0.3f, 0.5f, 0.3f));
        applyBagsBtn.Pressed += OnApplyBags;
        sideContent.AddChild(applyBagsBtn);

        sideContent.AddChild(new HSeparator { LayoutMode = 2 });

        // Reagent catalog picker
        BuildReagentPicker(sideContent);

        sideScroll.AddChild(sideContent);
        sidebar.AddChild(sideScroll);
        mainHBox.AddChild(sidebar);

        _playgroundContainer.AddChild(mainHBox);

        // Status bar
        _playStatusBar = new StatusBarPanel();
        _playStatusBar.Initialize(_playGrid, ReactionRuleDatabase.GetMvpRules());
        var statusMargin = new MarginContainer { LayoutMode = 2 };
        statusMargin.AddThemeConstantOverride("margin_left", 16);
        statusMargin.AddThemeConstantOverride("margin_right", 16);
        statusMargin.AddChild(_playStatusBar);
        _playgroundContainer.AddChild(statusMargin);

        // Bottom button bar
        BuildPlaygroundButtonBar();

        // Reaction animator
        _reactionAnimator = new ReactionAnimator
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        _reactionAnimator.SetContext(_playGridView, _playGrid);
        _reactionAnimator.AllAnimationsComplete += OnReactionAnimationsComplete;
        _playGridView.AddChild(_reactionAnimator);

        _root.AddChild(_playgroundContainer);
    }

    private void BuildReagentPicker(VBoxContainer parent)
    {
        var pickerHeader = new Label { Text = "Reagents", LayoutMode = 2 };
        pickerHeader.AddThemeColorOverride("font_color", HeaderColor);
        pickerHeader.AddThemeFontSizeOverride("font_size", 14);
        parent.AddChild(pickerHeader);

        var hint = new Label { Text = "Click to select, then click grid to place", LayoutMode = 2 };
        hint.AddThemeFontSizeOverride("font_size", 10);
        hint.AddThemeColorOverride("font_color", LabelColor);
        parent.AddChild(hint);

        var allItems = ReagentCatalog.GetAll();
        var groups = allItems
            .GroupBy(d => GetPrimaryAspectId(d))
            .OrderBy(g => GetAspectSortOrder(g.Key));

        foreach (var group in groups)
        {
            // Aspect group header
            var headerBox = new HBoxContainer { LayoutMode = 2 };
            headerBox.AddThemeConstantOverride("separation", 6);

            var icon = new ColorRect
            {
                Color = GridConstants.GetAspectColor(group.Key),
                CustomMinimumSize = new Godot.Vector2(12, 12),
                LayoutMode = 2
            };
            headerBox.AddChild(icon);

            var label = new Label
            {
                Text = CapitalizeFirst(group.Key),
                LayoutMode = 2
            };
            label.AddThemeFontSizeOverride("font_size", 12);
            label.AddThemeColorOverride("font_color", SubHeaderColor);
            headerBox.AddChild(label);
            parent.AddChild(headerBox);

            // Reagent cards in a flow
            var flow = new HFlowContainer { LayoutMode = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            flow.AddThemeConstantOverride("h_separation", 4);
            flow.AddThemeConstantOverride("v_separation", 4);

            foreach (var def in group)
            {
                var card = CreatePickerCard(def);
                flow.AddChild(card);
            }
            parent.AddChild(flow);
        }
    }

    private PanelContainer CreatePickerCard(ReagentDefinition def)
    {
        var panel = new PanelContainer { LayoutMode = 2 };
        panel.AddThemeStyleboxOverride("panel", CreateCardStyle(CardBgColor));

        var vbox = new VBoxContainer { LayoutMode = 2 };
        vbox.AddThemeConstantOverride("separation", 2);

        // Small colored shape preview
        var preview = CreateSmallShapePreview(def, 6);
        vbox.AddChild(preview);

        var nameLabel = new Label
        {
            Text = def.Name,
            LayoutMode = 2,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 9);
        nameLabel.AddThemeColorOverride("font_color", Colors.White);
        vbox.AddChild(nameLabel);

        panel.AddChild(vbox);

        // Make clickable
        panel.GuiInput += (ev) =>
        {
            if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                OnPickerCardClicked(def, panel);
        };

        return panel;
    }

    private Control CreateSmallShapePreview(ReagentDefinition def, int cellPx)
    {
        int rows = def.ShapeRows;
        int cols = def.ShapeCols;
        var color = GridConstants.GetAspectColor(GetPrimaryAspectId(def, ""));

        var container = new Control
        {
            LayoutMode = 2,
            CustomMinimumSize = new Godot.Vector2(cols * cellPx, rows * cellPx)
        };

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!def.Shape[r, c]) continue;
                var rect = new ColorRect
                {
                    Color = color,
                    Position = new Godot.Vector2(c * cellPx, r * cellPx),
                    Size = new Godot.Vector2(cellPx - 1, cellPx - 1)
                };
                container.AddChild(rect);
            }
        }
        return container;
    }

    private void OnPickerCardClicked(ReagentDefinition def, PanelContainer card)
    {
        // Deselect previous
        if (_selectedCard != null)
            _selectedCard.AddThemeStyleboxOverride("panel", CreateCardStyle(CardBgColor));

        // Create a new reagent instance from the definition
        _selectedReagent = new Reagent(def);
        _playgroundReagents.Add(_selectedReagent);
        _playGridView?.SetSelectedReagent(_selectedReagent);

        // Highlight selected card
        card.AddThemeStyleboxOverride("panel", CreateCardStyle(SelectedCardColor));
        _selectedCard = card;

        UpdatePlayStatus($"Selected: {def.Name}  |  R = rotate  |  Click grid to place  |  Esc = cancel");
    }

    private void OnApplyBags()
    {
        if (_bagEditor == null || _playGridView == null) return;

        var bags = _bagEditor.GetBags();
        if (bags.Count == 0)
        {
            UpdatePlayStatus("Add at least one bag before applying.");
            return;
        }

        // Clear everything and rebuild
        _playgroundReagents.Clear();
        _selectedReagent = null;
        _selectedCard = null;
        _actionHistory?.Clear();

        _playGrid = new MetaGrid(PlaygroundGridSize, PlaygroundGridSize);

        int bagIndex = 0;
        foreach (var puzzleBag in bags)
        {
            var shape = puzzleBag.ToShapeMatrix();
            var props = new List<BagPropertyType>();
            foreach (var propName in puzzleBag.Properties)
            {
                if (System.Enum.TryParse<BagPropertyType>(propName, out var pt))
                    props.Add(pt);
            }
            var bagDef = new BagDefinition($"bag_{bagIndex}", $"Bag {bagIndex + 1}", shape, props);
            var bag = new Bag(bagDef);
            var pos = new CoreVec(puzzleBag.PositionCol, puzzleBag.PositionRow);
            if (!_playGrid.PlaceBag(bag, pos))
            {
                UpdatePlayStatus($"Bag {bagIndex + 1} could not be placed at ({puzzleBag.PositionCol}, {puzzleBag.PositionRow}).");
            }
            bagIndex++;
        }

        // Reinitialize the grid view
        RebuildGridView();
        UpdatePlayStatus($"Applied {bagIndex} bag(s). Select a reagent to place.");
    }

    private void RebuildGridView()
    {
        if (_playGridView == null || _playGrid == null) return;

        var parent = _playGridView.GetParent();
        _playGridView.QueueFree();

        _playGridView = new MetaGridView();
        _playGridView.Initialize(_playGrid);
        _playGridView.SetReactionRules(ReactionRuleDatabase.GetMvpRules());
        _playGridView.SetActionHistory(_actionHistory!);
        _playGridView.ReagentPlaced += OnPlaygroundReagentPlaced;
        _playGridView.ReagentRemoved += OnPlaygroundReagentRemoved;
        parent?.AddChild(_playGridView);

        _reactionAnimator?.SetContext(_playGridView, _playGrid);

        _playStatusBar?.Initialize(_playGrid, ReactionRuleDatabase.GetMvpRules());
    }

    // ─── Playground Signals ───────────────────────────────────────────

    private void OnPlaygroundReagentPlaced()
    {
        ClearPlaygroundSelection();
        RefreshPlayground();
        UpdatePlayStatus("Placed reagent. Select another or press Confirm.");
    }

    private void OnPlaygroundReagentRemoved()
    {
        RefreshPlayground();
        UpdatePlayStatus("Removed reagent.");
    }

    private void ClearPlaygroundSelection()
    {
        _selectedReagent = null;
        _playGridView?.SetSelectedReagent(null);
        if (_selectedCard != null)
        {
            _selectedCard.AddThemeStyleboxOverride("panel", CreateCardStyle(CardBgColor));
            _selectedCard = null;
        }
    }

    private void RefreshPlayground()
    {
        _playGridView?.RefreshDisplay();
        _playStatusBar?.Refresh();
        UpdateUndoRedoButtons();
    }

    // ─── Playground Button Bar ────────────────────────────────────────

    private void BuildPlaygroundButtonBar()
    {
        var barMargin = new MarginContainer { LayoutMode = 2 };
        barMargin.AddThemeConstantOverride("margin_left", 16);
        barMargin.AddThemeConstantOverride("margin_right", 16);
        barMargin.AddThemeConstantOverride("margin_top", 4);
        barMargin.AddThemeConstantOverride("margin_bottom", 8);

        var barVBox = new VBoxContainer { LayoutMode = 2 };
        barVBox.AddThemeConstantOverride("separation", 4);

        var buttonBar = new HBoxContainer { LayoutMode = 2 };
        buttonBar.AddThemeConstantOverride("separation", 8);

        _confirmButton = new Button { Text = "Confirm", LayoutMode = 2, CustomMinimumSize = new Godot.Vector2(90, 34) };
        ApplyButtonStyle(_confirmButton, new Color(0.3f, 0.5f, 0.3f));
        _confirmButton.Pressed += OnConfirm;
        buttonBar.AddChild(_confirmButton);

        var resetBtn = new Button { Text = "Reset", LayoutMode = 2, CustomMinimumSize = new Godot.Vector2(90, 34) };
        ApplyButtonStyle(resetBtn, new Color(0.5f, 0.3f, 0.3f));
        resetBtn.Pressed += OnReset;
        buttonBar.AddChild(resetBtn);

        _undoButton = new Button { Text = "Undo", LayoutMode = 2, CustomMinimumSize = new Godot.Vector2(70, 34), Disabled = true };
        ApplyButtonStyle(_undoButton, new Color(0.3f, 0.3f, 0.4f));
        _undoButton.Pressed += OnUndo;
        buttonBar.AddChild(_undoButton);

        _redoButton = new Button { Text = "Redo", LayoutMode = 2, CustomMinimumSize = new Godot.Vector2(70, 34), Disabled = true };
        ApplyButtonStyle(_redoButton, new Color(0.3f, 0.3f, 0.4f));
        _redoButton.Pressed += OnRedo;
        buttonBar.AddChild(_redoButton);

        barVBox.AddChild(buttonBar);

        _playStatusLabel = new Label { Text = "Select a reagent from the sidebar to place.", LayoutMode = 2 };
        _playStatusLabel.AddThemeFontSizeOverride("font_size", 12);
        _playStatusLabel.AddThemeColorOverride("font_color", LabelColor);
        barVBox.AddChild(_playStatusLabel);

        barMargin.AddChild(barVBox);
        _playgroundContainer!.AddChild(barMargin);
    }

    private void OnConfirm()
    {
        if (_playGrid == null || _reactionAnimator == null) return;

        var reactions = ChemistryEngine.Evaluate(_playGrid, ReactionRuleDatabase.GetMvpRules());
        if (reactions.Count == 0)
        {
            UpdatePlayStatus("No reactions detected. Everything is stable!");
        }
        else
        {
            UpdatePlayStatus($"{reactions.Count} reaction(s) detected! Playing animations...");
            _confirmButton!.Disabled = true;
            _reactionAnimator.PlayReactions(reactions);
        }
    }

    private void OnReactionAnimationsComplete()
    {
        _confirmButton!.Disabled = false;
        UpdatePlayStatus("Reaction animations complete.");
    }

    private void OnReset()
    {
        if (_playGrid == null) return;
        ClearPlaygroundSelection();

        for (int i = _playGrid.PlacedReagents.Count - 1; i >= 0; i--)
        {
            var reagent = _playGrid.PlacedReagents[i];
            _playGrid.RemoveReagent(reagent);
            reagent.Rotation = 0;
        }
        _playgroundReagents.Clear();
        _actionHistory?.Clear();

        RefreshPlayground();
        UpdatePlayStatus("Grid cleared. Select a reagent to begin.");
    }

    private void OnUndo()
    {
        if (_actionHistory == null || _playGrid == null) return;
        if (_actionHistory.Undo(_playGrid))
            RefreshPlayground();
    }

    private void OnRedo()
    {
        if (_actionHistory == null || _playGrid == null) return;
        if (_actionHistory.Redo(_playGrid))
            RefreshPlayground();
    }

    private void UpdateUndoRedoButtons()
    {
        if (_undoButton != null) _undoButton.Disabled = !(_actionHistory?.CanUndo ?? false);
        if (_redoButton != null) _redoButton.Disabled = !(_actionHistory?.CanRedo ?? false);
    }

    private void UpdatePlayStatus(string text)
    {
        if (_playStatusLabel != null) _playStatusLabel.Text = text;
    }

    private static void ApplyButtonStyle(Button btn, Color bgColor)
    {
        btn.Flat = true;
        var style = new StyleBoxFlat { BgColor = bgColor };
        style.SetCornerRadiusAll(4);
        style.SetContentMarginAll(6);
        btn.AddThemeStyleboxOverride("normal", style);

        var hoverStyle = new StyleBoxFlat { BgColor = bgColor.Lightened(0.15f) };
        hoverStyle.SetCornerRadiusAll(4);
        hoverStyle.SetContentMarginAll(6);
        btn.AddThemeStyleboxOverride("hover", hoverStyle);

        var pressedStyle = new StyleBoxFlat { BgColor = bgColor.Darkened(0.1f) };
        pressedStyle.SetCornerRadiusAll(4);
        pressedStyle.SetContentMarginAll(6);
        btn.AddThemeStyleboxOverride("pressed", pressedStyle);

        var disabledStyle = new StyleBoxFlat { BgColor = bgColor.Darkened(0.4f) };
        disabledStyle.SetCornerRadiusAll(4);
        disabledStyle.SetContentMarginAll(6);
        btn.AddThemeStyleboxOverride("disabled", disabledStyle);
    }

    // ─── Catalog Mode Handlers ────────────────────────────────────────

    private Button CreateTabButton(string text, bool active)
    {
        var btn = new Button
        {
            Text = text,
            LayoutMode = 2,
            Flat = true
        };
        btn.AddThemeFontSizeOverride("font_size", 14);

        var style = new StyleBoxFlat();
        style.BgColor = active ? ActiveTabColor : InactiveTabColor;
        style.SetCornerRadiusAll(4);
        style.SetContentMarginAll(6);
        btn.AddThemeStyleboxOverride("normal", style);
        btn.AddThemeStyleboxOverride("hover", style);
        btn.AddThemeStyleboxOverride("pressed", style);

        return btn;
    }

    private void OnAspectFilterChanged(long index)
    {
        _aspectFilter = index == 0 ? null : _aspectDropdown.GetItemText((int)index);
        RebuildCatalog();
    }

    private void OnShapeFilterChanged(long index)
    {
        _shapeFilter = index == 0 ? null : _shapeDropdown.GetItemText((int)index);
        RebuildCatalog();
    }

    private void OnStateToggled(VisualState state, Button pressed)
    {
        _currentState = state;

        foreach (var child in _stateButtons.GetChildren())
        {
            if (child is Button btn)
                btn.ButtonPressed = btn == pressed;
        }

        foreach (var (renderer, _) in _spriteRenderers)
            renderer.SetState(state);

        foreach (var (rect, aspectId) in _fallbackCells)
            rect.Color = GetFallbackColor(aspectId, state);
    }

    private void RebuildCatalog()
    {
        foreach (var child in _catalogContent.GetChildren())
            child.QueueFree();

        _spriteRenderers.Clear();
        _fallbackCells.Clear();

        BuildCatalog();
    }

    private void BuildCatalog()
    {
        var allItems = ReagentCatalog.GetAll();

        var filtered = allItems.Where(d =>
        {
            if (_aspectFilter != null)
            {
                if (GetPrimaryAspectId(d) != _aspectFilter) return false;
            }

            if (_shapeFilter != null)
            {
                var shapeName = ReagentCatalog.GetShapeName(d);
                if (shapeName != _shapeFilter) return false;
            }

            return true;
        }).ToList();

        _countLabel.Text = $"{filtered.Count} item{(filtered.Count != 1 ? "s" : "")}";

        if (filtered.Count == 0)
        {
            var empty = new Label
            {
                Text = "No items match the current filters.",
                LayoutMode = 2,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            empty.AddThemeFontSizeOverride("font_size", 16);
            empty.AddThemeColorOverride("font_color", LabelColor);
            _catalogContent.AddChild(empty);
            return;
        }

        var groups = filtered
            .GroupBy(d => GetPrimaryAspectId(d))
            .OrderBy(g => GetAspectSortOrder(g.Key));

        foreach (var group in groups)
        {
            AddAspectGroup(group.Key, group.ToList());
        }
    }

    private void AddAspectGroup(string aspectId, List<ReagentDefinition> items)
    {
        var headerBox = new HBoxContainer { LayoutMode = 2 };
        headerBox.AddThemeConstantOverride("separation", 8);

        var icon = new ColorRect
        {
            Color = GridConstants.GetAspectColor(aspectId),
            CustomMinimumSize = new Godot.Vector2(16, 16),
            LayoutMode = 2
        };
        headerBox.AddChild(icon);

        var header = new Label
        {
            Text = $"{CapitalizeFirst(aspectId)} ({items.Count})",
            LayoutMode = 2
        };
        header.AddThemeFontSizeOverride("font_size", 16);
        header.AddThemeColorOverride("font_color", SubHeaderColor);
        headerBox.AddChild(header);

        _catalogContent.AddChild(headerBox);

        var flow = new HFlowContainer
        {
            LayoutMode = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        flow.AddThemeConstantOverride("h_separation", 12);
        flow.AddThemeConstantOverride("v_separation", 12);

        foreach (var def in items)
        {
            var card = CreateReagentCard(def);
            flow.AddChild(card);
        }

        _catalogContent.AddChild(flow);
    }

    private PanelContainer CreateReagentCard(ReagentDefinition def)
    {
        var panel = new PanelContainer { LayoutMode = 2 };
        panel.AddThemeStyleboxOverride("panel", CreateCardStyle(CardBgColor, 4, 8));

        var vbox = new VBoxContainer { LayoutMode = 2 };
        vbox.AddThemeConstantOverride("separation", 4);

        var nameLabel = new Label
        {
            Text = def.Name,
            LayoutMode = 2,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        nameLabel.AddThemeColorOverride("font_color", Colors.White);
        vbox.AddChild(nameLabel);

        var previewContainer = CreateSpritePreview(def, PreviewCellSize);
        vbox.AddChild(previewContainer);

        var largePreview = CreateSpritePreview(def, LargePreviewCellSize);
        vbox.AddChild(largePreview);

        var shapeName = ReagentCatalog.GetShapeName(def) ?? "custom";
        var infoLabel = new Label
        {
            Text = $"{shapeName}  ({def.Id})",
            LayoutMode = 2,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        infoLabel.AddThemeFontSizeOverride("font_size", 10);
        infoLabel.AddThemeColorOverride("font_color", LabelColor);
        vbox.AddChild(infoLabel);

        int cellCount = ShapeUtil.CellCount(def.Shape);
        var detailLabel = new Label
        {
            Text = $"{cellCount} cell{(cellCount != 1 ? "s" : "")} · {def.Rarity}",
            LayoutMode = 2,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        detailLabel.AddThemeFontSizeOverride("font_size", 10);
        detailLabel.AddThemeColorOverride("font_color", LabelColor);
        vbox.AddChild(detailLabel);

        panel.AddChild(vbox);
        return panel;
    }

    private Control CreateSpritePreview(ReagentDefinition def, int cellSize)
    {
        var spec = SpriteSheetSpec.DefaultReagentSpec(cellSize, cellSize);
        var sheet = ReagentSpriteSheet.GetOrLoad(def.Id, spec, def.PrimaryAspectId);

        if (sheet != null)
        {
            var renderer = new SpriteStateRenderer();
            renderer.Initialize(sheet, cellSize, cellSize);
            renderer.SetState(_currentState);
            _spriteRenderers.Add((renderer, sheet));
            return renderer;
        }

        return CreateFallbackShapePreview(def, cellSize);
    }

    private Control CreateFallbackShapePreview(ReagentDefinition def, int cellSize)
    {
        int rows = def.ShapeRows;
        int cols = def.ShapeCols;
        int cellPx = cellSize / System.Math.Max(rows, cols);
        if (cellPx < 4) cellPx = 4;

        var container = new Control
        {
            LayoutMode = 2,
            CustomMinimumSize = new Godot.Vector2(cols * cellPx, rows * cellPx)
        };

        var primaryAspect = GetPrimaryAspectId(def, "");

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!def.Shape[r, c]) continue;

                var cellRect = new ColorRect
                {
                    Color = GetFallbackColor(primaryAspect, _currentState),
                    Position = new Godot.Vector2(c * cellPx, r * cellPx),
                    Size = new Godot.Vector2(cellPx - 1, cellPx - 1)
                };
                container.AddChild(cellRect);
                _fallbackCells.Add((cellRect, primaryAspect));
            }
        }

        return container;
    }

    private static Color GetFallbackColor(string aspectId, VisualState state)
    {
        var baseColor = GridConstants.GetAspectColor(aspectId);
        return state switch
        {
            VisualState.Heated => baseColor.Lerp(new Color(1f, 0.4f, 0.1f), 0.35f),
            VisualState.Cooled => baseColor.Lerp(new Color(0.2f, 0.4f, 0.9f), 0.35f),
            VisualState.ThermalStress => baseColor.Lerp(new Color(0.8f, 0.2f, 0.8f), 0.4f),
            VisualState.Crushed => baseColor.Darkened(0.4f),
            VisualState.Breaking => baseColor.Lerp(new Color(0.5f, 0.5f, 0.5f), 0.5f),
            VisualState.Exploding => baseColor.Lerp(new Color(1f, 0.8f, 0.2f), 0.5f),
            _ => baseColor,
        };
    }

    private static string GetPrimaryAspectId(ReagentDefinition def, string fallback = "unknown")
    {
        if (def.AspectsPerCell.Count > 0)
            return def.AspectsPerCell[0].Aspects.FirstOrDefault()?.Id ?? fallback;
        return fallback;
    }

    private static StyleBoxFlat CreateCardStyle(Color bgColor, int cornerRadius = 3, int contentMargin = 4)
    {
        var style = new StyleBoxFlat { BgColor = bgColor };
        style.SetCornerRadiusAll(cornerRadius);
        style.SetContentMarginAll(contentMargin);
        return style;
    }

    private static int GetAspectSortOrder(string aspectId) => aspectId switch
    {
        AspectIds.Hot => 0,
        AspectIds.Cold => 1,
        AspectIds.Volatile => 2,
        AspectIds.Toxic => 3,
        AspectIds.Heavy => 4,
        AspectIds.Stable => 5,
        AspectIds.Sharp => 6,
        AspectIds.Fragile => 7,
        _ => 99,
    };

    private static string CapitalizeFirst(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpperInvariant(s[0]) + s[1..];
    }
}
