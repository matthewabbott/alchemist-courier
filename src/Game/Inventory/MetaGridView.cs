using System.Collections.Generic;
using System.Linq;
using Godot;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Highlight;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Core.Visual;
using AlchemistCourier.Game.Visual;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Classification of how safe a prospective placement is.
/// </summary>
public enum PlacementWarningLevel
{
    /// <summary>Placement is valid, no reactions triggered, reagent has no reactive aspects.</summary>
    Safe,
    /// <summary>Placement is valid, no reactions triggered, but reagent has reactive aspects.</summary>
    Warning,
    /// <summary>Placement is valid but would trigger negative reactions.</summary>
    Danger,
    /// <summary>Placement is invalid (out of bounds, occupied, no bag).</summary>
    Invalid,
}

public partial class MetaGridView : Control
{
    private MetaGrid _metaGrid = null!;

    // Cell visuals — one ColorRect per grid position
    private ColorRect[,] _cellRects = null!;

    // Grid line nodes
    private readonly List<ColorRect> _gridLines = new();

    // Bag border nodes
    private readonly List<ColorRect> _bagBorders = new();

    // Hinted reagent border nodes
    private readonly List<ColorRect> _hintedBorders = new();

    // Locked reagent border nodes
    private readonly List<ColorRect> _lockedBorders = new();

    // Ghost overlay nodes (hover preview)
    private readonly List<ColorRect> _ghostCells = new();

    // Sprite state renderers (per placed reagent, keyed by reagent instance)
    private readonly Dictionary<Reagent, SpriteStateRenderer> _spriteRenderers = new();

    // Highlight overlay
    private HighlightOverlay _highlightOverlay = null!;
    private bool _eagleSenseActive;
    private StatusThresholds? _statusThresholds;

    // Chemistry warning state
    private List<ReactionRule>? _reactionRules;
    private HashSet<string>? _reactiveAspectIds;
    private PlacementWarningLevel _currentWarningLevel;

    // Action history for undo/redo
    private ActionHistory? _actionHistory;

    // Interaction state
    private Reagent? _selectedReagent;
    private CoreVec? _hoverGridPos;
    private bool _isPlacementValid;
    private bool _mouseInsideGrid;

    // Hover tracking for placed reagents
    private Reagent? _hoveredPlacedReagent;

    // Rejection tooltip state
    private PanelContainer? _rejectionTooltip;
    private Label? _rejectionLabel;
    private float _tooltipTimer;
    private float _flashTimer;
    private string? _lastRejectionReason;
    private string? _lastRejectionAspectId;

    private const float TooltipDuration = 2.0f;
    private const float FlashDuration = 0.3f;

    [Signal]
    public delegate void ReagentPlacedEventHandler();

    [Signal]
    public delegate void ReagentRemovedEventHandler();

    [Signal]
    public delegate void HoveredReagentChangedEventHandler();

    [Signal]
    public delegate void RejectionFlashEventHandler(string aspectId);

    public void Initialize(MetaGrid metaGrid)
    {
        _metaGrid = metaGrid;
        _cellRects = new ColorRect[metaGrid.Width, metaGrid.Height];

        int pixelWidth = metaGrid.Width * GridConstants.CellSize;
        int pixelHeight = metaGrid.Height * GridConstants.CellSize;
        CustomMinimumSize = new Vector2(pixelWidth, pixelHeight);
        Size = new Vector2(pixelWidth, pixelHeight);

        CreateCellNodes();
        CreateGridLines();

        _highlightOverlay = new HighlightOverlay
        {
            ZIndex = 4,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_highlightOverlay);

        CreateRejectionTooltip();
        RefreshDisplay();
    }

    public void SetReactionRules(List<ReactionRule> rules)
    {
        _reactionRules = rules;
        _reactiveAspectIds = new HashSet<string>();
        foreach (var rule in rules)
            foreach (var id in rule.TriggerAspects)
                _reactiveAspectIds.Add(id);
    }

    /// <summary>Current warning level of the ghost preview.</summary>
    public PlacementWarningLevel CurrentWarningLevel => _currentWarningLevel;

    /// <summary>The placed reagent currently being hovered over (if any).</summary>
    public Reagent? HoveredPlacedReagent => _hoveredPlacedReagent;

    /// <summary>The highlight overlay (for testing).</summary>
    public HighlightOverlay Highlights => _highlightOverlay;

    /// <summary>Set the highlight overlay content directly.</summary>
    public void SetHighlights(HighlightSet highlights)
    {
        _highlightOverlay.Render(highlights, _eagleSenseActive, _metaGrid.Width, _metaGrid.Height);
    }

    /// <summary>Enable or disable eagle sense mode (dim overlay behind highlights).</summary>
    public void SetEagleSense(bool active)
    {
        _eagleSenseActive = active;
    }

    /// <summary>Current status thresholds.</summary>
    public StatusThresholds? StatusThresholds => _statusThresholds;

    /// <summary>Set status thresholds for interaction highlight severity.</summary>
    public void SetStatusThresholds(StatusThresholds? thresholds)
    {
        _statusThresholds = thresholds;
    }

    /// <summary>Set the action history for undo/redo support.</summary>
    public void SetActionHistory(ActionHistory history)
    {
        _actionHistory = history;
    }

    /// <summary>Last rejection tooltip reason text (for testing).</summary>
    public string? LastRejectionReason => _lastRejectionReason;

    /// <summary>Whether the rejection tooltip is currently visible.</summary>
    public bool IsTooltipVisible => _rejectionTooltip?.Visible ?? false;

    /// <summary>Programmatically set hover position (for testing).</summary>
    public void SetHoverPosition(CoreVec position)
    {
        _hoverGridPos = position;
        _mouseInsideGrid = true;
        UpdateGhostPreview();
    }

    /// <summary>Simulate a left-click at the current hover position (for testing).</summary>
    public void SimulateLeftClick()
    {
        if (!_mouseInsideGrid || _hoverGridPos == null) return;
        HandleLeftClick();
    }

    private void CreateCellNodes()
    {
        for (int row = 0; row < _metaGrid.Height; row++)
        {
            for (int col = 0; col < _metaGrid.Width; col++)
            {
                var rect = new ColorRect
                {
                    Position = GridCoordinates.GridToPixel(new CoreVec(col, row)),
                    Size = new Vector2(GridConstants.CellSize, GridConstants.CellSize),
                    Color = GridConstants.EmptyCell,
                };
                AddChild(rect);
                _cellRects[col, row] = rect;
            }
        }
    }

    private void CreateGridLines()
    {
        int pixelWidth = _metaGrid.Width * GridConstants.CellSize;
        int pixelHeight = _metaGrid.Height * GridConstants.CellSize;

        // Horizontal lines
        for (int row = 0; row <= _metaGrid.Height; row++)
        {
            var line = new ColorRect
            {
                Position = new Vector2(0, row * GridConstants.CellSize),
                Size = new Vector2(pixelWidth, GridConstants.GridLineWidth),
                Color = GridConstants.GridLineColor,
                ZIndex = 1,
            };
            AddChild(line);
            _gridLines.Add(line);
        }

        // Vertical lines
        for (int col = 0; col <= _metaGrid.Width; col++)
        {
            var line = new ColorRect
            {
                Position = new Vector2(col * GridConstants.CellSize, 0),
                Size = new Vector2(GridConstants.GridLineWidth, pixelHeight),
                Color = GridConstants.GridLineColor,
                ZIndex = 1,
            };
            AddChild(line);
            _gridLines.Add(line);
        }
    }

    public void RefreshDisplay()
    {
        // Clear old bag borders
        foreach (var border in _bagBorders)
        {
            border.QueueFree();
        }
        _bagBorders.Clear();

        // Build a bag-to-index map for tinting
        var bagIndexMap = new Dictionary<Bag, int>();
        for (int i = 0; i < _metaGrid.PlacedBags.Count; i++)
        {
            bagIndexMap[_metaGrid.PlacedBags[i]] = i;
        }

        // Color each cell
        for (int col = 0; col < _metaGrid.Width; col++)
        {
            for (int row = 0; row < _metaGrid.Height; row++)
            {
                var cell = _metaGrid.GetCell(col, row)!;
                var rect = _cellRects[col, row];

                if (!cell.IsInBag)
                {
                    rect.Color = GridConstants.EmptyCell;
                }
                else if (cell.IsOccupied)
                {
                    // Color by the occupying reagent's aspect at this cell
                    var reagent = (Reagent)cell.OccupyingReagent!;
                    var localPos = new CoreVec(col, row) - reagent.PlacedPosition!.Value;
                    rect.Color = GridConstants.GetDominantColor(reagent, localPos);
                }
                else
                {
                    // Empty bag cell — tint by bag index
                    var bag = (Bag)cell.BagOwner!;
                    int bagIdx = bagIndexMap.GetValueOrDefault(bag, 0);
                    rect.Color = GridConstants.GetBagTint(bagIdx);
                }
            }
        }

        // Update sprite renderers for placed reagents
        UpdateSpriteRenderers();

        // Draw bag borders
        CreateBagBorders(bagIndexMap);

        // Draw hinted reagent borders
        CreateHintedBorders();

        // Draw locked reagent borders
        CreateLockedBorders();
    }

    private void CreateBagBorders(Dictionary<Bag, int> bagIndexMap)
    {
        for (int col = 0; col < _metaGrid.Width; col++)
        {
            for (int row = 0; row < _metaGrid.Height; row++)
            {
                var cell = _metaGrid.GetCell(col, row)!;
                if (!cell.IsInBag) continue;

                var pos = new CoreVec(col, row);
                var pixelPos = GridCoordinates.GridToPixel(pos);

                // Check each cardinal direction for bag boundary
                foreach (var dir in CoreVec.CardinalDirections)
                {
                    var neighborPos = pos + dir;
                    var neighbor = _metaGrid.GetCell(neighborPos);
                    bool isBoundary = neighbor == null || !neighbor.IsInBag || neighbor.BagOwner != cell.BagOwner;

                    if (!isBoundary) continue;

                    var border = CreateBorderRect(pixelPos, dir);
                    AddChild(border);
                    _bagBorders.Add(border);
                }
            }
        }
    }

    private void CreateHintedBorders()
    {
        foreach (var border in _hintedBorders)
        {
            border.QueueFree();
        }
        _hintedBorders.Clear();

        for (int col = 0; col < _metaGrid.Width; col++)
        {
            for (int row = 0; row < _metaGrid.Height; row++)
            {
                var cell = _metaGrid.GetCell(col, row)!;
                if (!cell.IsOccupied) continue;

                var reagent = (Reagent)cell.OccupyingReagent!;
                if (!reagent.IsHinted) continue;

                var pos = new CoreVec(col, row);
                var pixelPos = GridCoordinates.GridToPixel(pos);

                // Draw border on edges where adjacent cell is not the same reagent
                foreach (var dir in CoreVec.CardinalDirections)
                {
                    var neighborPos = pos + dir;
                    var neighbor = _metaGrid.GetCell(neighborPos);
                    bool isBoundary = neighbor == null || neighbor.OccupyingReagent != reagent;

                    if (!isBoundary) continue;

                    var border = CreateHintedBorderRect(pixelPos, dir);
                    AddChild(border);
                    _hintedBorders.Add(border);
                }
            }
        }
    }

    private void CreateLockedBorders()
    {
        foreach (var border in _lockedBorders)
        {
            border.QueueFree();
        }
        _lockedBorders.Clear();

        for (int col = 0; col < _metaGrid.Width; col++)
        {
            for (int row = 0; row < _metaGrid.Height; row++)
            {
                var cell = _metaGrid.GetCell(col, row)!;
                if (!cell.IsOccupied) continue;

                var reagent = (Reagent)cell.OccupyingReagent!;
                if (!reagent.IsLocked) continue;

                var pos = new CoreVec(col, row);
                var pixelPos = GridCoordinates.GridToPixel(pos);

                foreach (var dir in CoreVec.CardinalDirections)
                {
                    var neighborPos = pos + dir;
                    var neighbor = _metaGrid.GetCell(neighborPos);
                    bool isBoundary = neighbor == null || neighbor.OccupyingReagent != reagent;

                    if (!isBoundary) continue;

                    var border = CreateLockedBorderRect(pixelPos, dir);
                    AddChild(border);
                    _lockedBorders.Add(border);
                }
            }
        }
    }

    private ColorRect CreateLockedBorderRect(Vector2 cellPixelPos, CoreVec direction)
    {
        float cs = GridConstants.CellSize;
        const float bw = 2;
        Vector2 pos;
        Vector2 size;

        if (direction.Row == -1)
        {
            pos = cellPixelPos;
            size = new Vector2(cs, bw);
        }
        else if (direction.Row == 1)
        {
            pos = cellPixelPos + new Vector2(0, cs - bw);
            size = new Vector2(cs, bw);
        }
        else if (direction.Col == -1)
        {
            pos = cellPixelPos;
            size = new Vector2(bw, cs);
        }
        else
        {
            pos = cellPixelPos + new Vector2(cs - bw, 0);
            size = new Vector2(bw, cs);
        }

        return new ColorRect
        {
            Position = pos,
            Size = size,
            Color = GridConstants.LockedBorderColor,
            ZIndex = 2,
        };
    }

    private void UpdateSpriteRenderers()
    {
        // Collect currently placed reagents
        var placedReagents = new HashSet<Reagent>();
        for (int col = 0; col < _metaGrid.Width; col++)
        {
            for (int row = 0; row < _metaGrid.Height; row++)
            {
                var cell = _metaGrid.GetCell(col, row)!;
                if (cell.IsOccupied)
                    placedReagents.Add((Reagent)cell.OccupyingReagent!);
            }
        }

        // Remove renderers for reagents no longer placed
        var toRemove = new List<Reagent>();
        foreach (var kvp in _spriteRenderers)
        {
            if (!placedReagents.Contains(kvp.Key))
            {
                kvp.Value.QueueFree();
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var r in toRemove)
            _spriteRenderers.Remove(r);

        // Create or update renderers for placed reagents
        foreach (var reagent in placedReagents)
        {
            var catalogId = reagent.Definition.Id;
            if (catalogId == null) continue;

            var rotatedShape = reagent.GetRotatedShape();
            int shapeCols = rotatedShape.GetLength(1);
            int shapeRows = rotatedShape.GetLength(0);
            int pixelW = shapeCols * GridConstants.CellSize;
            int pixelH = shapeRows * GridConstants.CellSize;

            var spec = SpriteSheetSpec.DefaultReagentSpec(pixelW, pixelH);
            var spriteSheet = ReagentSpriteSheet.GetOrLoad(catalogId, spec);
            if (spriteSheet == null) continue;

            // Resolve visual state: pick the most critical state across all cells
            var cellStates = VisualStateResolver.Resolve(reagent, _metaGrid);
            var displayState = PickDisplayState(cellStates);

            if (_spriteRenderers.TryGetValue(reagent, out var existing))
            {
                // Update position and state
                existing.Position = GridCoordinates.GridToPixel(reagent.PlacedPosition!.Value);
                existing.SetState(displayState);
            }
            else
            {
                // Create new renderer
                var renderer = new SpriteStateRenderer
                {
                    ZIndex = 2,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                renderer.Initialize(spriteSheet, pixelW, pixelH);
                renderer.Position = GridCoordinates.GridToPixel(reagent.PlacedPosition!.Value);
                renderer.SetState(displayState);
                AddChild(renderer);
                _spriteRenderers[reagent] = renderer;
            }
        }
    }

    /// <summary>
    /// Picks the most visually significant state from a per-cell state map.
    /// Priority: Crushed > ThermalStress > Heated > Cooled > Normal.
    /// Animated states (Exploding, Breaking) override all others.
    /// </summary>
    private static VisualState PickDisplayState(Dictionary<CoreVec, VisualState> cellStates)
    {
        var best = VisualState.Normal;
        foreach (var state in cellStates.Values)
        {
            if (StatePriority(state) > StatePriority(best))
                best = state;
        }
        return best;
    }

    private static int StatePriority(VisualState state) => state switch
    {
        VisualState.Exploding => 6,
        VisualState.Breaking => 5,
        VisualState.Crushed => 4,
        VisualState.ThermalStress => 3,
        VisualState.Heated => 2,
        VisualState.Cooled => 1,
        _ => 0,
    };

    private ColorRect CreateHintedBorderRect(Vector2 cellPixelPos, CoreVec direction)
    {
        float cs = GridConstants.CellSize;
        const float bw = 2;
        Vector2 pos;
        Vector2 size;

        if (direction.Row == -1)
        {
            pos = cellPixelPos;
            size = new Vector2(cs, bw);
        }
        else if (direction.Row == 1)
        {
            pos = cellPixelPos + new Vector2(0, cs - bw);
            size = new Vector2(cs, bw);
        }
        else if (direction.Col == -1)
        {
            pos = cellPixelPos;
            size = new Vector2(bw, cs);
        }
        else
        {
            pos = cellPixelPos + new Vector2(cs - bw, 0);
            size = new Vector2(bw, cs);
        }

        return new ColorRect
        {
            Position = pos,
            Size = size,
            Color = GridConstants.HintedBorderColor,
            ZIndex = 2,
        };
    }

    private ColorRect CreateBorderRect(Vector2 cellPixelPos, CoreVec direction)
    {
        float cs = GridConstants.CellSize;
        float bw = GridConstants.BagBorderWidth;
        Vector2 pos;
        Vector2 size;

        if (direction.Row == -1) // Up edge
        {
            pos = cellPixelPos;
            size = new Vector2(cs, bw);
        }
        else if (direction.Row == 1) // Down edge
        {
            pos = cellPixelPos + new Vector2(0, cs - bw);
            size = new Vector2(cs, bw);
        }
        else if (direction.Col == -1) // Left edge
        {
            pos = cellPixelPos;
            size = new Vector2(bw, cs);
        }
        else // Right edge
        {
            pos = cellPixelPos + new Vector2(cs - bw, 0);
            size = new Vector2(bw, cs);
        }

        return new ColorRect
        {
            Position = pos,
            Size = size,
            Color = GridConstants.BagBorderColor,
            ZIndex = 2,
        };
    }

    // --- Interaction ---

    public void SetSelectedReagent(Reagent? reagent)
    {
        _selectedReagent = reagent;
        if (reagent == null)
        {
            ClearGhost();
            _hoverGridPos = null;
        }
        else
        {
            UpdateGhostPreview();
        }
    }

    public Reagent? SelectedReagent => _selectedReagent;
    public MetaGrid Grid => _metaGrid;

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            HandleMouseMotion(motion);
        }
        else if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed)
        {
            HandleMouseClick(mouseBtn);
        }
        else if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            HandleKeyPress(key);
        }
    }

    private void HandleMouseMotion(InputEventMouseMotion motion)
    {
        HideRejectionTooltip();

        var localPos = GetLocalMousePosition();
        var gridPos = GridCoordinates.PixelToGrid(localPos);

        bool inside = _metaGrid.IsInBounds(gridPos) &&
                      localPos.X >= 0 && localPos.Y >= 0 &&
                      localPos.X < Size.X && localPos.Y < Size.Y;

        if (inside)
        {
            _mouseInsideGrid = true;
            _hoverGridPos = gridPos;
            UpdateGhostPreview();
            UpdateHoveredReagent(gridPos);
        }
        else if (_mouseInsideGrid)
        {
            _mouseInsideGrid = false;
            _hoverGridPos = null;
            ClearGhost();
            UpdateHoveredReagent(null);
        }
    }

    private void UpdateHoveredReagent(CoreVec? gridPos)
    {
        Reagent? hoveredReagent = null;
        if (gridPos.HasValue && _selectedReagent == null)
        {
            hoveredReagent = _metaGrid.GetReagentAtCell(gridPos.Value);
        }

        if (hoveredReagent != _hoveredPlacedReagent)
        {
            _hoveredPlacedReagent = hoveredReagent;
            EmitSignal(SignalName.HoveredReagentChanged);
        }
    }

    private void HandleMouseClick(InputEventMouseButton mouseBtn)
    {
        if (!_mouseInsideGrid || _hoverGridPos == null) return;

        if (mouseBtn.ButtonIndex == MouseButton.Left)
        {
            HandleLeftClick();
        }
        else if (mouseBtn.ButtonIndex == MouseButton.Right)
        {
            if (_selectedReagent != null)
            {
                _selectedReagent.RotateClockwise();
                UpdateGhostPreview();
            }
            else
            {
                // Try to pick up a placed reagent
                var reagent = _metaGrid.GetReagentAtCell(_hoverGridPos.Value);
                if (reagent != null)
                {
                    if (reagent.IsLocked)
                    {
                        ShowRejectionTooltip("Cannot remove — locked reagent", _hoverGridPos.Value);
                        return;
                    }
                    if (_actionHistory != null)
                    {
                        var action = new RemoveReagentAction(reagent);
                        if (!_actionHistory.Execute(action, _metaGrid)) return;
                    }
                    else
                    {
                        if (!_metaGrid.RemoveReagent(reagent)) return;
                    }
                    reagent.Rotation = 0;
                    RefreshDisplay();
                    EmitSignal(SignalName.ReagentRemoved);
                }
            }
        }
    }

    private void HandleKeyPress(InputEventKey key)
    {
        if (_selectedReagent == null) return;

        if (key.Keycode == Key.R)
        {
            _selectedReagent.RotateClockwise();
            UpdateGhostPreview();
        }
        else if (key.Keycode == Key.Escape)
        {
            _selectedReagent = null;
            ClearGhost();
        }
    }

    private void UpdateGhostPreview()
    {
        ClearGhost();
        _currentWarningLevel = PlacementWarningLevel.Invalid;
        if (_selectedReagent == null || _hoverGridPos == null) return;

        var positions = _selectedReagent.GetMetaGridPositions(_hoverGridPos.Value);
        _isPlacementValid = _metaGrid.CanPlaceReagent(_selectedReagent, _hoverGridPos.Value);

        Color ghostColor;
        if (!_isPlacementValid)
        {
            _currentWarningLevel = PlacementWarningLevel.Invalid;
            ghostColor = GridConstants.InvalidPlacement;
        }
        else if (_reactionRules != null && _reactionRules.Count > 0)
        {
            var reactions = ChemistryEngine.EvaluateProspective(
                _metaGrid, _selectedReagent, _hoverGridPos.Value, _reactionRules);

            if (reactions.Count > 0)
            {
                _currentWarningLevel = PlacementWarningLevel.Danger;
                ghostColor = GridConstants.InvalidPlacement;
            }
            else if (HasReactiveAspects(_selectedReagent))
            {
                _currentWarningLevel = PlacementWarningLevel.Warning;
                ghostColor = GridConstants.WarningPlacement;
            }
            else
            {
                _currentWarningLevel = PlacementWarningLevel.Safe;
                ghostColor = GridConstants.ValidPlacement;
            }
        }
        else
        {
            _currentWarningLevel = PlacementWarningLevel.Safe;
            ghostColor = GridConstants.ValidPlacement;
        }

        foreach (var pos in positions)
        {
            var pixelPos = GridCoordinates.GridToPixel(pos);
            var ghost = new ColorRect
            {
                Position = pixelPos,
                Size = new Vector2(GridConstants.CellSize, GridConstants.CellSize),
                Color = ghostColor,
                ZIndex = 3,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            AddChild(ghost);
            _ghostCells.Add(ghost);
        }

        // Compute and render highlight overlays
        ComputeAndRenderHighlights();
    }

    private void ComputeAndRenderHighlights()
    {
        if (_selectedReagent == null || _hoverGridPos == null || _reactionRules == null)
            return;

        var highlights = new HighlightSet();

        // Constraint highlights (Sharp on edge, Fragile in gravity, etc.)
        var constraintHighlights = HighlightEngine.ComputeConstraintHighlights(
            _metaGrid, _selectedReagent, _hoverGridPos.Value);
        highlights.AddRange(constraintHighlights.GetByLayer(HighlightLayer.Constraint));

        // Ghost constraint highlights (rendered on ghost cells themselves)
        var ghostConstraints = HighlightEngine.ComputeGhostConstraintHighlights(
            _metaGrid, _selectedReagent, _hoverGridPos.Value);
        highlights.AddRange(ghostConstraints.GetByLayer(HighlightLayer.GhostOverlay));

        // Interaction highlights (adjacent reactive aspects)
        if (_isPlacementValid)
        {
            var interactionHighlights = HighlightEngine.ComputeInteractionHighlights(
                _metaGrid, _selectedReagent, _hoverGridPos.Value, _reactionRules, _statusThresholds);
            highlights.AddRange(interactionHighlights.GetByLayer(HighlightLayer.Interaction));
        }

        if (!highlights.IsEmpty)
        {
            _highlightOverlay.Render(highlights, _eagleSenseActive, _metaGrid.Width, _metaGrid.Height);
        }
    }

    private bool HasReactiveAspects(Reagent reagent)
    {
        if (_reactiveAspectIds == null || _reactiveAspectIds.Count == 0) return false;

        foreach (var localPos in reagent.GetLocalCellPositions())
        {
            var aspects = reagent.GetAspectsAtLocalPosition(localPos);
            foreach (var aspect in aspects.Aspects)
            {
                if (_reactiveAspectIds.Contains(aspect.Id))
                    return true;
            }
        }
        return false;
    }

    private void HandleLeftClick()
    {
        if (_selectedReagent == null || _hoverGridPos == null) return;

        if (_isPlacementValid)
        {
            if (_actionHistory != null)
            {
                var action = new PlaceReagentAction(_selectedReagent, _hoverGridPos.Value, _selectedReagent.Rotation);
                if (!_actionHistory.Execute(action, _metaGrid)) return;
            }
            else
            {
                _metaGrid.PlaceReagent(_selectedReagent, _hoverGridPos.Value);
            }
            _selectedReagent = null;
            ClearGhost();
            HideRejectionTooltip();
            RefreshDisplay();
            EmitSignal(SignalName.ReagentPlaced);
        }
        else
        {
            ShowRejectionForCurrentPosition();
        }
    }

    private void ShowRejectionForCurrentPosition()
    {
        if (_selectedReagent == null || _hoverGridPos == null) return;

        var violations = HighlightEngine.GetConstraintViolations(
            _metaGrid, _selectedReagent, _hoverGridPos.Value);

        if (violations.Count == 0) return;

        var firstViolation = violations[0];
        _lastRejectionAspectId = firstViolation.AspectId;
        ShowRejectionTooltip(firstViolation.Reason, _hoverGridPos.Value, firstViolation.AspectId);

        // Flash the highlight overlay
        _flashTimer = FlashDuration;
        _highlightOverlay.Modulate = new Color(1.5f, 1.5f, 1.5f);

        // Emit signal for legend panel linking
        if (firstViolation.AspectId != null)
        {
            EmitSignal(SignalName.RejectionFlash, firstViolation.AspectId);
        }
    }

    private void CreateRejectionTooltip()
    {
        _rejectionTooltip = new PanelContainer
        {
            Visible = false,
            ZIndex = 10,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.9f),
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
        };
        _rejectionTooltip.AddThemeStyleboxOverride("panel", style);

        _rejectionLabel = new Label();
        _rejectionLabel.AddThemeFontSizeOverride("font_size", 11);
        _rejectionTooltip.AddChild(_rejectionLabel);

        AddChild(_rejectionTooltip);
    }

    private void ShowRejectionTooltip(string reason, CoreVec gridPos, string? aspectId = null)
    {
        if (_rejectionTooltip == null || _rejectionLabel == null) return;

        _lastRejectionReason = reason;
        _rejectionLabel.Text = reason;

        // Color text by the constraint's aspect (or default warm red)
        var textColor = aspectId != null
            ? GridConstants.GetAspectColor(aspectId)
            : new Color(0.95f, 0.6f, 0.5f);
        _rejectionLabel.AddThemeColorOverride("font_color", textColor);

        // Position below and to the right of the click cell
        var pixelPos = GridCoordinates.GridToPixel(gridPos);
        _rejectionTooltip.Position = pixelPos + new Vector2(GridConstants.CellSize + 4, GridConstants.CellSize + 4);

        _rejectionTooltip.Visible = true;
        _tooltipTimer = TooltipDuration;
    }

    private void HideRejectionTooltip()
    {
        if (_rejectionTooltip != null)
            _rejectionTooltip.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (_tooltipTimer > 0)
        {
            _tooltipTimer -= (float)delta;
            if (_tooltipTimer <= 0)
            {
                HideRejectionTooltip();
            }
        }

        if (_flashTimer > 0)
        {
            _flashTimer -= (float)delta;
            if (_flashTimer <= 0)
            {
                _highlightOverlay.Modulate = Colors.White;
            }
        }
    }

    private void ClearGhost()
    {
        foreach (var ghost in _ghostCells)
        {
            ghost.QueueFree();
        }
        _ghostCells.Clear();
        _highlightOverlay.Clear();
    }
}
