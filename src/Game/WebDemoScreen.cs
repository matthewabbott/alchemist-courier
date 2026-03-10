using System.Collections.Generic;
using System.Linq;
using Godot;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Highlight;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Game.Animation;
using AlchemistCourier.Game.Inventory;
using AlchemistCourier.Game.Visual;

namespace AlchemistCourier.Game;

/// <summary>
/// Web demo scene: a polished single-puzzle showcase with floating tooltips,
/// hover debounce, and an optional tutorial overlay. No menu, no save system.
/// </summary>
public partial class WebDemoScreen : Control
{
    private MetaGrid _metaGrid = null!;
    private List<Reagent> _trayReagents = null!;
    private Reagent? _selectedReagent;
    private int _selectedIndex = -1;
    private readonly ActionHistory _actionHistory = new();
    private int _puzzleSeed;

    // UI components (reused from existing codebase)
    private MetaGridView _gridView = null!;
    private ReagentTray _reagentTray = null!;
    private StatusBarPanel _statusBar = null!;
    private InfoColumnManager _infoColumnManager = null!;
    private Label _statusLabel = null!;
    private Button _confirmButton = null!;
    private Button _resetButton = null!;
    private Button _undoButton = null!;
    private Button _redoButton = null!;
    private Button _hintButton = null!;
    private Label _hintCountLabel = null!;
    private ResultOverlayPanel _resultOverlay = null!;
    private ReactionAnimator _reactionAnimator = null!;
    private HintOrchestrator? _hintOrchestrator;
    private HintState _hintState = new HintState(0);
    private PuzzleType _puzzleType = PuzzleType.FitAllSafely;

    // Floating tooltips (web demo specific)
    private FloatingDetailTooltip _detailTooltip = null!;
    private FloatingAspectTooltip _aspectTooltip = null!;
    private Control _tooltipLayer = null!;

    // Responsive layout
    private Control _infoSpacer = null!;
    private Label _violationBadge = null!;

    // State
    private string? _legendSelectedAspect;
    private int _selectedViolationIndex = -1;
    private ConfirmResult _lastConfirmResult = ConfirmResult.None;
    private List<ReactionResult> _lastReactions = new();

    // Decoration overlays
    private PanelContainer _headerPanel = null!;
    private TextureRect?[]? _cornerFlourishes;

    // Public inspection
    public MetaGridView GridView => _gridView;
    public ReagentTray ReagentTray => _reagentTray;
    public StatusBarPanel StatusBar => _statusBar;
    public ObjectivePanel Objective => _infoColumnManager.ObjectivePanel;
    public AspectLegendPanel LegendPanel => _infoColumnManager.LegendPanel;
    public ViolationListPanel ViolationPanel => _infoColumnManager.ViolationPanel;
    public ResultOverlayPanel ResultOverlay => _resultOverlay;
    public MetaGrid Grid => _metaGrid;
    public IReadOnlyList<Reagent> TrayReagents => _trayReagents.AsReadOnly();
    public FloatingDetailTooltip DetailTooltip => _detailTooltip;
    public FloatingAspectTooltip AspectTooltip => _aspectTooltip;
    public InfoColumnManager InfoColumn => _infoColumnManager;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        _puzzleSeed = (int)(Time.GetUnixTimeFromSystem() % int.MaxValue);
        GeneratePuzzle();
        BuildUI();
        WireSignals();
        UpdateStatusThresholds();
        UpdateStatus("Select a reagent from the tray, then click the grid to place it.");
    }

    private void GeneratePuzzle()
    {
        var generator = new PuzzleGenerator();
        var state = generator.Generate(_puzzleSeed, DifficultyParams.ForLevel(DifficultyLevel.Medium));

        _puzzleType = state.PuzzleType;
        _metaGrid = PuzzleSolver.ReconstructGrid(state);
        _trayReagents = PuzzleSolver.ReconstructReagents(state);
        _hintState = new HintState(state.MaxHints);
        _hintOrchestrator = new HintOrchestrator(state, _hintState, _metaGrid, _trayReagents);

        foreach (var placement in state.Solution)
        {
            if (placement.ReagentIndex < 0 || placement.ReagentIndex >= _trayReagents.Count)
                continue;
            var reagent = _trayReagents[placement.ReagentIndex];
            if (!reagent.IsLocked) continue;
            reagent.Rotation = placement.Rotation;
            var pos = new Core.Grid.Vector2I(placement.PositionCol, placement.PositionRow);
            if (_metaGrid.CanPlaceReagent(reagent, pos))
                _metaGrid.PlaceReagent(reagent, pos);
        }
    }

    private void BuildUI()
    {
        // Background
        var bg = new ColorRect
        {
            Color = new Color(0.1f, 0.1f, 0.12f),
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

        // ---- Header ----
        BuildHeader(vbox);

        // ---- Grid + sidebar ----
        var hbox = new HBoxContainer();
        hbox.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(hbox);

        BuildGridSection(hbox);
        _infoSpacer = new Control { CustomMinimumSize = new Vector2(20, 0) };
        hbox.AddChild(_infoSpacer);
        BuildInfoColumn(hbox);

        // ---- Bottom separator ----
        var sepTex = UIThemeLoader.GetDecorationTexture("separator");
        if (sepTex != null)
        {
            var sep = new TextureRect
            {
                Texture = sepTex,
                CustomMinimumSize = new Vector2(0, 20),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            vbox.AddChild(sep);
        }
        else
        {
            vbox.AddChild(new HSeparator());
        }

        BuildTrayStrip(vbox);

        _statusBar = new StatusBarPanel();
        _statusBar.Initialize(_metaGrid, ReactionRuleDatabase.GetMvpRules());
        vbox.AddChild(_statusBar);

        BuildButtonBar(vbox);

        _statusLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.5f));
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(_statusLabel);

        // ---- Overlays ----
        _resultOverlay = new ResultOverlayPanel();
        AddChild(_resultOverlay);

        BuildViolationBadge();
        BuildTooltipLayer();
        AddCornerFlourishes();
    }

    private void BuildHeader(VBoxContainer parent)
    {
        _headerPanel = new PanelContainer();
        _headerPanel.AddThemeStyleboxOverride("panel", UIThemeLoader.GetPanelStyleBox("parchment_bordered"));
        parent.AddChild(_headerPanel);

        var hbox = new HBoxContainer();
        _headerPanel.AddChild(hbox);

        // Title sigil (alchemist seal medallion)
        var sigilTex = UIThemeLoader.GetDecorationTexture("title_sigil");
        if (sigilTex != null)
        {
            var sigil = new TextureRect
            {
                Texture = sigilTex,
                CustomMinimumSize = new Vector2(28, 28),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            };
            hbox.AddChild(sigil);
        }

        var title = new Label { Text = "Alchemist Courier" };
        title.AddThemeColorOverride("font_color", new Color(0.3f, 0.2f, 0.1f));
        title.AddThemeFontSizeOverride("font_size", 18);
        hbox.AddChild(title);

        hbox.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // Subtle tutorial toggle (placeholder — WD.9 will flesh this out)
        var tutorialBtn = new Button { Text = "?", TooltipText = "Toggle Tutorial" };
        tutorialBtn.CustomMinimumSize = new Vector2(30, 30);
        var btnStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.45f, 0.35f, 0.2f, 0.5f),
            ContentMarginLeft = 4, ContentMarginRight = 4,
            ContentMarginTop = 2, ContentMarginBottom = 2,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
        };
        tutorialBtn.AddThemeStyleboxOverride("normal", btnStyle);
        tutorialBtn.AddThemeColorOverride("font_color", new Color(0.35f, 0.25f, 0.15f));
        tutorialBtn.AddThemeFontSizeOverride("font_size", 14);
        hbox.AddChild(tutorialBtn);
    }

    private void BuildGridSection(HBoxContainer parent)
    {
        var gridCenter = new CenterContainer();
        _gridView = new MetaGridView();
        _gridView.Initialize(_metaGrid);
        _gridView.SetReactionRules(ReactionRuleDatabase.GetMvpRules());
        _gridView.SetActionHistory(_actionHistory);

        // Set up hover debounce for smooth tooltip behavior
        var debounce = new HoverDebounce(GridConstants.HoverDebounceDelay);
        _gridView.SetHoverDebounce(debounce);

        _reactionAnimator = new ReactionAnimator { MouseFilter = MouseFilterEnum.Ignore };
        _reactionAnimator.SetContext(_gridView, _metaGrid);
        _gridView.AddChild(_reactionAnimator);

        gridCenter.AddChild(_gridView);
        parent.AddChild(gridCenter);
    }

    private void BuildInfoColumn(HBoxContainer parent)
    {
        _infoColumnManager = new InfoColumnManager();
        _infoColumnManager.Initialize(
            _puzzleType,
            _metaGrid,
            ReactionRuleDatabase.GetMvpRules(),
            _trayReagents);
        parent.AddChild(_infoColumnManager);
    }

    private void BuildTrayStrip(VBoxContainer parent)
    {
        var trayPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        trayPanel.AddThemeStyleboxOverride("panel", UIThemeLoader.GetPanelStyleBox("dark_leather"));
        parent.AddChild(trayPanel);

        var trayScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 50),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        trayPanel.AddChild(trayScroll);

        _reagentTray = new ReagentTray();
        _reagentTray.SetHintState(_hintState);
        _reagentTray.SetOrientation(true);
        _reagentTray.Initialize(_trayReagents);
        _reagentTray.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        trayScroll.AddChild(_reagentTray);
    }

    private void BuildButtonBar(VBoxContainer parent)
    {
        var buttonBar = new HBoxContainer();
        buttonBar.Alignment = BoxContainer.AlignmentMode.Center;
        buttonBar.AddThemeConstantOverride("separation", 12);
        parent.AddChild(buttonBar);

        _undoButton = CreateStyledButton("Undo", new Color(0.3f, 0.3f, 0.35f));
        _undoButton.Pressed += Undo;
        _undoButton.Disabled = true;
        buttonBar.AddChild(_undoButton);

        _redoButton = CreateStyledButton("Redo", new Color(0.3f, 0.3f, 0.35f));
        _redoButton.Pressed += Redo;
        _redoButton.Disabled = true;
        buttonBar.AddChild(_redoButton);

        _confirmButton = CreateStyledButton("Confirm", new Color(0.25f, 0.5f, 0.3f));
        _confirmButton.Pressed += OnConfirmPressed;
        buttonBar.AddChild(_confirmButton);

        _resetButton = CreateStyledButton("Reset", new Color(0.35f, 0.3f, 0.28f));
        _resetButton.Pressed += OnResetPressed;
        buttonBar.AddChild(_resetButton);

        _hintButton = CreateStyledButton("Hint", new Color(0.4f, 0.35f, 0.55f));
        _hintButton.Pressed += OnHintPressed;
        buttonBar.AddChild(_hintButton);

        _hintCountLabel = new Label
        {
            Text = _hintOrchestrator?.FormatHintCount() ?? $"{_hintState.HintsUsed}/{_hintState.MaxHints}",
        };
        _hintCountLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.65f, 0.85f));
        _hintCountLabel.AddThemeFontSizeOverride("font_size", 12);
        buttonBar.AddChild(_hintCountLabel);

        if (_hintState.MaxHints == 0)
        {
            _hintButton.Visible = false;
            _hintCountLabel.Visible = false;
        }
    }

    private void BuildTooltipLayer()
    {
        _tooltipLayer = new Control();
        _tooltipLayer.SetAnchorsPreset(LayoutPreset.FullRect);
        _tooltipLayer.MouseFilter = MouseFilterEnum.Ignore;
        _tooltipLayer.ZIndex = 100;
        AddChild(_tooltipLayer);

        _detailTooltip = new FloatingDetailTooltip();
        _tooltipLayer.AddChild(_detailTooltip);

        _aspectTooltip = new FloatingAspectTooltip();
        _aspectTooltip.SetReactionRules(ReactionRuleDatabase.GetMvpRules());
        _tooltipLayer.AddChild(_aspectTooltip);
    }

    private void BuildViolationBadge()
    {
        _violationBadge = new Label
        {
            Text = "",
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(28, 28),
        };
        var badgeStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.8f, 0.2f, 0.2f, 0.9f),
            ContentMarginLeft = 4, ContentMarginRight = 4,
            ContentMarginTop = 2, ContentMarginBottom = 2,
            CornerRadiusTopLeft = 14, CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14, CornerRadiusBottomRight = 14,
        };
        _violationBadge.AddThemeStyleboxOverride("normal", badgeStyle);
        _violationBadge.AddThemeColorOverride("font_color", new Color(1, 1, 1));
        _violationBadge.AddThemeFontSizeOverride("font_size", 11);
        AddChild(_violationBadge);
    }

    private void AddCornerFlourishes()
    {
        var tex = UIThemeLoader.GetDecorationTexture("corner_flourish");
        if (tex == null) return;

        _cornerFlourishes = new TextureRect[4];
        for (int i = 0; i < 4; i++)
        {
            var tr = new TextureRect
            {
                Texture = tex,
                CustomMinimumSize = new Vector2(32, 32),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                FlipH = i == 1 || i == 3,
                FlipV = i == 2 || i == 3,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _cornerFlourishes[i] = tr;
            AddChild(tr);
        }
        _headerPanel.Resized += UpdateFlourishPositions;
    }

    private void UpdateFlourishPositions()
    {
        if (_cornerFlourishes == null) return;

        var rect = _headerPanel.GetGlobalRect();
        var origin = GetGlobalPosition();
        float s = 32;
        float x0 = rect.Position.X - origin.X;
        float y0 = rect.Position.Y - origin.Y;
        float x1 = rect.End.X - origin.X;
        float y1 = rect.End.Y - origin.Y;

        _cornerFlourishes[0]!.Position = new Vector2(x0, y0);
        _cornerFlourishes[1]!.Position = new Vector2(x1 - s, y0);
        _cornerFlourishes[2]!.Position = new Vector2(x0, y1 - s);
        _cornerFlourishes[3]!.Position = new Vector2(x1 - s, y1 - s);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
            AdaptLayout();
    }

    private void AdaptLayout()
    {
        float width = Size.X;

        if (width >= 1280)
        {
            // Standard layout: info column at full width
            _infoColumnManager.Visible = true;
            _infoSpacer.Visible = true;
            _infoColumnManager.CustomMinimumSize = new Vector2(160, 0);
            _infoColumnManager.LegendPanel.SetCompactMode(false);
            _violationBadge.Visible = false;
        }
        else if (width >= 800)
        {
            // Narrower: info column shrinks, legend goes compact
            _infoColumnManager.Visible = true;
            _infoSpacer.Visible = true;
            _infoColumnManager.CustomMinimumSize = new Vector2(140, 0);
            _infoColumnManager.LegendPanel.SetCompactMode(true);
            _violationBadge.Visible = false;
        }
        else
        {
            // Narrow: info column hidden, violation badge on grid corner
            _infoColumnManager.Visible = false;
            _infoSpacer.Visible = false;
            _infoColumnManager.LegendPanel.SetCompactMode(true);
            UpdateViolationBadge();
        }

        UpdateFlourishPositions();
    }

    private void UpdateViolationBadge()
    {
        var violations = _infoColumnManager.ViolationPanel.CurrentViolations;
        int count = violations.Count;
        if (count > 0)
        {
            _violationBadge.Text = count.ToString();
            _violationBadge.Visible = true;

            // Position at top-right corner of grid
            var gridRect = _gridView.GetRect();
            _violationBadge.Position = new Vector2(
                gridRect.End.X - _violationBadge.Size.X - 4,
                gridRect.Position.Y + 4
            );
        }
        else
        {
            _violationBadge.Visible = false;
        }
    }

    // --- Signal Wiring ---

    private void WireSignals()
    {
        _reagentTray.ReagentSelected += OnReagentSelected;
        _reagentTray.ReagentHovered += OnTrayReagentHovered;
        _reagentTray.ReagentUnhovered += OnTrayReagentUnhovered;
        _gridView.ReagentPlaced += OnReagentPlaced;
        _gridView.ReagentRemoved += OnReagentRemoved;
        _gridView.HoveredReagentChanged += OnGridHoveredReagentChanged;
        _gridView.RejectionFlash += OnRejectionFlash;
        _gridView.PlacedReagentClicked += OnPlacedReagentClicked;
        _gridView.BagSelected += OnBagSelected;
        _gridView.BagMoved += OnBagMoved;
        _gridView.BagRotated += OnBagRotated;
        _infoColumnManager.LegendPanel.AspectSelected += OnLegendAspectSelected;
        _infoColumnManager.ViolationPanel.ViolationClicked += OnViolationClicked;
        _infoColumnManager.BackPressed += OnInfoColumnBack;
    }

    private void OnReagentSelected(int index)
    {
        if (index < 0 || index >= _trayReagents.Count) return;
        _selectedReagent = _trayReagents[index];
        _selectedIndex = index;
        _selectedReagent.Rotation = 0;
        _gridView.SetSelectedReagent(_selectedReagent);
        _reagentTray.HighlightIndex(index);
        _detailTooltip.ForceHide();

        // Show reagent detail in the info column
        _infoColumnManager.ShowReagentDetail(_selectedReagent);

        UpdateStatus($"Placing: {_selectedReagent.Definition.Name}  |  R = rotate  |  Click = place  |  Esc = cancel");
    }

    private void OnReagentPlaced()
    {
        var name = _selectedReagent?.Definition.Name ?? "Reagent";
        ClearSelectionState();
        RefreshAfterGridChange();
        int remaining = _trayReagents.Count(r => !r.PlacedPosition.HasValue);
        UpdateStatus(remaining == 0
            ? $"Placed {name}. All reagents placed! Press Confirm."
            : $"Placed {name}. {remaining} remaining.");
    }

    private void OnReagentRemoved()
    {
        RefreshAfterGridChange();
        UpdateStatus("Removed reagent. Select another from the tray.");
    }

    private void OnTrayReagentHovered(int index)
    {
        if (index < 0 || index >= _trayReagents.Count) return;
        var reagent = _trayReagents[index];
        var trayRect = _reagentTray.GetGlobalRect();
        // Position tooltip above the tray strip
        _detailTooltip.ShowForReagent(reagent, new Vector2(trayRect.Position.X + index * 90, trayRect.Position.Y - 120));
    }

    private void OnTrayReagentUnhovered(int index)
    {
        _detailTooltip.ShowForReagent(null, Vector2.Zero);
    }

    private void OnGridHoveredReagentChanged()
    {
        var reagent = _gridView.HoveredPlacedReagent;

        if (reagent != null && reagent.PlacedPosition.HasValue)
        {
            var pixelPos = GridCoordinates.GridToPixel(reagent.PlacedPosition.Value);
            var gridGlobalPos = _gridView.GlobalPosition;
            var shape = reagent.GetRotatedShape();
            var anchorX = gridGlobalPos.X + pixelPos.X + shape.GetLength(1) * GridConstants.CellSize;
            var anchorY = gridGlobalPos.Y + pixelPos.Y;
            _detailTooltip.ShowForReagent(reagent, new Vector2(anchorX, anchorY));

            // Highlight legend aspects
            var aspectIds = new HashSet<string>();
            foreach (var pos in reagent.GetLocalCellPositions())
                foreach (var aspect in reagent.GetAspectsAtLocalPosition(pos).Aspects)
                    aspectIds.Add(aspect.Id);
            _infoColumnManager.LegendPanel.HighlightAspects(aspectIds);
        }
        else
        {
            _detailTooltip.ShowForReagent(null, Vector2.Zero);
            _infoColumnManager.LegendPanel.ClearHighlight();
        }
    }

    private void OnPlacedReagentClicked()
    {
        var reagent = _gridView.HoveredPlacedReagent;
        if (reagent == null || !reagent.PlacedPosition.HasValue) return;

        _infoColumnManager.ShowReagentDetail(reagent);
    }

    private void OnBagSelected()
    {
        var bag = _gridView.SelectedBag;
        if (bag != null)
        {
            ClearSelectionState();
            UpdateStatus("Bag selected. Click+drag to move, right-click to rotate, Esc to deselect.");
        }
        else
        {
            UpdateStatus("Select a reagent from the tray.");
        }
    }

    private void OnBagMoved()
    {
        RefreshAfterGridChange();
        _reagentTray.RefreshDisplay();
        UpdateStatus("Bag moved. Reagents carried along.");
    }

    private void OnBagRotated()
    {
        RefreshAfterGridChange();
        _reagentTray.RefreshDisplay();
        UpdateStatus("Bag rotated.");
    }

    private void OnLegendAspectSelected(string aspectId)
    {
        // Clear violation highlight if active
        if (_selectedViolationIndex >= 0)
            ClearViolationHighlight();

        if (_legendSelectedAspect == aspectId)
        {
            _legendSelectedAspect = null;
            _gridView.SetHighlights(new HighlightSet());
            _aspectTooltip.ForceHide();
        }
        else
        {
            _legendSelectedAspect = aspectId;
            var highlights = HighlightEngine.ComputeLegendHighlights(_metaGrid, aspectId);
            _gridView.SetHighlights(highlights);

            // Show aspect detail in the info column
            _infoColumnManager.ShowAspectDetail(aspectId);
        }
    }

    private void OnInfoColumnBack()
    {
        ClearLegendAndViolationState();
    }

    private void ClearLegendAndViolationState()
    {
        if (_legendSelectedAspect != null)
        {
            _legendSelectedAspect = null;
            _gridView.SetHighlights(new HighlightSet());
            _aspectTooltip.ForceHide();
        }
        if (_selectedViolationIndex >= 0)
            ClearViolationHighlight();
    }

    private void OnViolationClicked(int violationIndex)
    {
        if (violationIndex == _selectedViolationIndex)
        {
            // Toggle off
            ClearViolationHighlight();
            return;
        }

        var violations = _infoColumnManager.ViolationPanel.CurrentViolations;
        if (violationIndex < 0 || violationIndex >= violations.Count)
            return;

        // Clear legend selection if active
        if (_legendSelectedAspect != null)
        {
            _legendSelectedAspect = null;
            _aspectTooltip.ForceHide();
        }

        _selectedViolationIndex = violationIndex;
        var violation = violations[violationIndex];

        // Highlight affected cells on grid
        var highlights = HighlightEngine.ComputeViolationHighlights(violation);
        _gridView.SetHighlights(highlights);

        // Highlight affected reagents in tray
        var affectedIndices = new HashSet<int>();
        foreach (var cell in violation.AffectedCells)
        {
            var reagent = _metaGrid.GetReagentAtCell(cell);
            if (reagent != null)
            {
                var idx = _trayReagents.IndexOf(reagent);
                if (idx >= 0)
                    affectedIndices.Add(idx);
            }
        }
        _reagentTray.HighlightIndices(affectedIndices);
    }

    private void ClearViolationHighlight()
    {
        if (_selectedViolationIndex < 0) return;
        _selectedViolationIndex = -1;
        _gridView.SetHighlights(new HighlightSet());
        _reagentTray.ClearSelection();
    }

    private void OnRejectionFlash(string aspectId)
    {
        _infoColumnManager.LegendPanel.FlashAspect(aspectId);
    }

    // --- Actions ---

    private void ClearSelectionState()
    {
        _selectedReagent = null;
        _selectedIndex = -1;
        _gridView.SetSelectedReagent(null);
        _reagentTray.ClearSelection();
    }

    private void OnConfirmPressed()
    {
        ClearSelectionState();

        var rules = ReactionRuleDatabase.GetMvpRules();
        var result = PuzzleValidator.Evaluate(_metaGrid, _trayReagents, _puzzleType, rules);
        _lastConfirmResult = result.Result;
        _lastReactions = result.Reactions.ToList();

        switch (_lastConfirmResult)
        {
            case ConfirmResult.NotAllPlaced:
                UpdateStatus("Cannot confirm: some reagents not yet placed.");
                break;
            case ConfirmResult.HasReactions:
                UpdateStatus("Reactions detected! Rearrange reagents to avoid them.");
                // Pulse violation cells, then play reaction animations, then show failure overlay
                _gridView.PulseViolations(() =>
                {
                    _reactionAnimator.AllAnimationsComplete += OnReactionAnimationsComplete;
                    _reactionAnimator.PlayReactions(_lastReactions);
                });
                break;
            case ConfirmResult.Win:
                _resultOverlay.ShowWin("Puzzle Complete!", result.Score);
                UpdateStatus("Well done! Press 'New Puzzle' for another challenge.");
                break;
        }
    }

    private void OnReactionAnimationsComplete()
    {
        _reactionAnimator.AllAnimationsComplete -= OnReactionAnimationsComplete;
        _resultOverlay.ShowFailure(_lastReactions);
        UpdateStatus("Try rearranging your reagents and confirm again.");
    }

    private void OnResetPressed()
    {
        ClearSelectionState();
        _resultOverlay.Hide();

        for (int i = _metaGrid.PlacedReagents.Count - 1; i >= 0; i--)
        {
            var reagent = _metaGrid.PlacedReagents[i];
            if (reagent.IsLocked) continue;
            _metaGrid.RemoveReagent(reagent);
            reagent.Rotation = 0;
        }
        foreach (var r in _trayReagents)
            r.IsHinted = false;

        _actionHistory.Clear();
        RefreshAfterGridChange();
        UpdateStatus("Puzzle reset. Select a reagent from the tray.");
    }

    private void OnHintPressed()
    {
        if (_hintOrchestrator == null) return;
        if (!_hintOrchestrator.HasHintsAvailable) return;

        var hint = _hintOrchestrator.ExecuteHint();
        if (hint == null) return;

        if (hint.IsSuccess)
        {
            // Hint modifies grid outside ActionHistory — clear to avoid inconsistent state
            _actionHistory.Clear();
            ClearSelectionState();
            RefreshAfterGridChange();
        }

        _hintCountLabel.Text = _hintOrchestrator.FormatHintCount();
        var reagentName = _trayReagents[hint.ReagentIndex].Definition.Name;
        UpdateStatus($"Hinted: {reagentName} placed at solution position.");
    }

    private void Undo()
    {
        if (!_actionHistory.CanUndo) return;
        ClearSelectionState();
        _actionHistory.Undo(_metaGrid);
        RefreshAfterGridChange();
        UpdateStatus("Undone.");
    }

    private void Redo()
    {
        if (!_actionHistory.CanRedo) return;
        ClearSelectionState();
        _actionHistory.Redo(_metaGrid);
        RefreshAfterGridChange();
        UpdateStatus("Redone.");
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            if (!_infoColumnManager.IsShowingOverview)
            {
                _infoColumnManager.ShowDefault();
                ClearLegendAndViolationState();
                GetViewport().SetInputAsHandled();
            }
            else if (_selectedViolationIndex >= 0)
            {
                ClearViolationHighlight();
                GetViewport().SetInputAsHandled();
            }
            else if (_legendSelectedAspect != null)
            {
                _legendSelectedAspect = null;
                _gridView.SetHighlights(new HighlightSet());
                _aspectTooltip.ForceHide();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    // --- Refresh helpers ---

    private void RefreshAfterGridChange()
    {
        ClearViolationHighlight();
        _gridView.RefreshDisplay();
        _reagentTray.RefreshDisplay();
        UpdateUndoRedoButtons();
        UpdateStatusThresholds();
        _infoColumnManager.ViolationPanel.Refresh();
        if (!_infoColumnManager.Visible)
            UpdateViolationBadge();
    }

    private void UpdateUndoRedoButtons()
    {
        _undoButton.Disabled = !_actionHistory.CanUndo;
        _redoButton.Disabled = !_actionHistory.CanRedo;
    }

    private void UpdateStatusThresholds()
    {
        _statusBar.Refresh();
    }

    private void UpdateStatus(string text)
    {
        _statusLabel.Text = text;
    }

    // --- Styled button (matches PuzzleScreen pattern) ---

    private static Button CreateStyledButton(string text, Color bgColor)
    {
        var button = new Button { Text = text };
        button.CustomMinimumSize = new Vector2(100, 44);

        foreach (var (overrideName, color) in new[]
        {
            ("normal", bgColor),
            ("hover", bgColor.Lightened(0.15f)),
            ("pressed", bgColor.Darkened(0.15f)),
            ("disabled", bgColor.Darkened(0.4f)),
        })
        {
            var style = new StyleBoxFlat
            {
                BgColor = color,
                ContentMarginLeft = 12, ContentMarginRight = 12,
                ContentMarginTop = 4, ContentMarginBottom = 4,
                CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
                CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            };
            button.AddThemeStyleboxOverride(overrideName, style);
        }
        button.AddThemeColorOverride("font_disabled_color", new Color(0.45f, 0.45f, 0.45f));
        return button;
    }
}
