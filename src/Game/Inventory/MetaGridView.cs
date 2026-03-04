using System.Collections.Generic;
using System.Linq;
using Godot;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;
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

    // Ghost overlay nodes (hover preview)
    private readonly List<ColorRect> _ghostCells = new();

    // Chemistry warning state
    private List<ReactionRule>? _reactionRules;
    private HashSet<string>? _reactiveAspectIds;
    private PlacementWarningLevel _currentWarningLevel;

    // Interaction state
    private Reagent? _selectedReagent;
    private CoreVec? _hoverGridPos;
    private bool _isPlacementValid;
    private bool _mouseInsideGrid;

    [Signal]
    public delegate void ReagentPlacedEventHandler();

    [Signal]
    public delegate void ReagentRemovedEventHandler();

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

    /// <summary>Programmatically set hover position (for testing).</summary>
    public void SetHoverPosition(CoreVec position)
    {
        _hoverGridPos = position;
        _mouseInsideGrid = true;
        UpdateGhostPreview();
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

        // Draw bag borders
        CreateBagBorders(bagIndexMap);
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
        }
        else if (_mouseInsideGrid)
        {
            _mouseInsideGrid = false;
            _hoverGridPos = null;
            ClearGhost();
        }
    }

    private void HandleMouseClick(InputEventMouseButton mouseBtn)
    {
        if (!_mouseInsideGrid || _hoverGridPos == null) return;

        if (mouseBtn.ButtonIndex == MouseButton.Left)
        {
            if (_selectedReagent != null && _isPlacementValid)
            {
                _metaGrid.PlaceReagent(_selectedReagent, _hoverGridPos.Value);
                _selectedReagent = null;
                ClearGhost();
                RefreshDisplay();
                EmitSignal(SignalName.ReagentPlaced);
            }
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
                    _metaGrid.RemoveReagent(reagent);
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

    private void ClearGhost()
    {
        foreach (var ghost in _ghostCells)
        {
            ghost.QueueFree();
        }
        _ghostCells.Clear();
    }
}
