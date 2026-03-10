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
using AlchemistCourier.Game.Animation;
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

    // Bag border nodes (ColorRect fallback + NinePatchRect textured)
    private readonly List<ColorRect> _bagBorders = new();
    private readonly List<NinePatchRect> _bagBorderPatches = new();

    // Hinted reagent border nodes
    private readonly List<ColorRect> _hintedBorders = new();

    // Locked reagent border nodes
    private readonly List<ColorRect> _lockedBorders = new();

    // Ghost overlay nodes (hover preview)
    private readonly List<ColorRect> _ghostCells = new();

    // Per-cell aspect tile overlays (one TextureRect per occupied cell with a tile sprite)
    private readonly List<TextureRect> _aspectTiles = new();

    // Sprite state renderers (per placed reagent, keyed by reagent instance)
    private readonly Dictionary<Reagent, SpriteStateRenderer> _spriteRenderers = new();

    // Highlight overlay
    private HighlightOverlay _highlightOverlay = null!;
    private bool _eagleSenseActive;

    // Damage overlay (fragile violation indicators)
    private DamageOverlay _damageOverlay = null!;
    private StatusThresholds? _statusThresholds;

    // Bag state overlay (thermal tint for hot/cold imbalance)
    private BagStateOverlay _bagStateOverlay = null!;

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

    // Falling animation
    private FallingAnimator? _fallingAnimator;
    private CoreVec? _effectivePlacePos; // May differ from _hoverGridPos for Heavy falling

    // Cell mount overlay (aspect-tinted frames around ALL placed reagent cells)
    private CellMountOverlay _cellMountOverlay = null!;

    // Hover frame overlay
    private HoverFrameOverlay _hoverFrameOverlay = null!;

    // Hover tracking for placed reagents
    private Reagent? _hoveredPlacedReagent;

    // Optional hover debounce (set by WebDemoScreen, null = instant behavior for PuzzleScreen)
    private HoverDebounce? _hoverDebounce;

    // Rejection tooltip state
    private PanelContainer? _rejectionTooltip;
    private Label? _rejectionLabel;
    private float _tooltipTimer;
    private float _flashTimer;
    private string? _lastRejectionReason;
    private string? _lastRejectionAspectId;

    private const float TooltipDuration = 2.0f;
    private const float FlashDuration = 0.3f;

    // Bag selection / movement state
    private Bag? _selectedBag;
    private bool _isDraggingBag;
    private CoreVec? _bagDragStartGridPos;
    private readonly List<ColorRect> _bagGhostCells = new();
    private readonly List<ColorRect> _selectedBagBorders = new();

    [Signal]
    public delegate void ReagentPlacedEventHandler();

    [Signal]
    public delegate void ReagentRemovedEventHandler();

    [Signal]
    public delegate void HoveredReagentChangedEventHandler();

    [Signal]
    public delegate void RejectionFlashEventHandler(string aspectId);

    [Signal]
    public delegate void PlacedReagentClickedEventHandler();

    [Signal]
    public delegate void BagSelectedEventHandler();

    [Signal]
    public delegate void BagMovedEventHandler();

    [Signal]
    public delegate void BagRotatedEventHandler();

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

        _cellMountOverlay = new CellMountOverlay
        {
            ZIndex = 2,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_cellMountOverlay);

        _hoverFrameOverlay = new HoverFrameOverlay
        {
            ZIndex = 3,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_hoverFrameOverlay);

        _highlightOverlay = new HighlightOverlay
        {
            ZIndex = 4,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_highlightOverlay);

        _bagStateOverlay = new BagStateOverlay
        {
            ZIndex = 2,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_bagStateOverlay);

        _damageOverlay = new DamageOverlay
        {
            ZIndex = 5,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_damageOverlay);

        _fallingAnimator = new FallingAnimator
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_fallingAnimator);

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

    /// <summary>
    /// Set an optional hover debounce for delayed hover-off behavior.
    /// When set, mouse-leave from the grid delays clearing the hovered reagent.
    /// When null (default), behavior is instant (backward compatible with PuzzleScreen).
    /// </summary>
    public void SetHoverDebounce(HoverDebounce debounce)
    {
        _hoverDebounce = debounce;
        _hoverDebounce.OnHide += () => UpdateHoveredReagent(null);
    }

    /// <summary>The highlight overlay (for testing).</summary>
    public HighlightOverlay Highlights => _highlightOverlay;

    /// <summary>The hover frame overlay (for testing).</summary>
    public HoverFrameOverlay HoverFrame => _hoverFrameOverlay;

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

    /// <summary>The currently selected bag (for testing and info column).</summary>
    public Bag? SelectedBag => _selectedBag;

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
        UpdateHoveredReagent(position);
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

    // Gravity arrow color
    private static readonly Color GravityArrowColor = new(0.6f, 0.3f, 0.9f, 0.7f);

    public void RefreshDisplay()
    {
        // Clear old bag borders
        foreach (var border in _bagBorders)
            border.QueueFree();
        _bagBorders.Clear();
        foreach (var patch in _bagBorderPatches)
            patch.QueueFree();
        _bagBorderPatches.Clear();

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

        // Render per-cell aspect tiles for placed reagents
        UpdateAspectTiles();

        // Draw cell mount frames around all placed reagent cells
        _cellMountOverlay.Render(_metaGrid);

        // Draw bag borders
        CreateBagBorders(bagIndexMap);

        // Draw hinted reagent borders
        CreateHintedBorders();

        // Draw locked reagent borders
        CreateLockedBorders();

        // Update bag thermal state indicators (hot/cold imbalance)
        UpdateBagStateOverlay();

        // Update damage indicators (fragile + volatile explosion)
        UpdateDamageOverlay();

        // Update selected bag highlight borders
        UpdateSelectedBagBorders();

        // Trigger _Draw() for gravity arrows
        QueueRedraw();
    }

    private void CreateBagBorders(Dictionary<Bag, int> bagIndexMap)
    {
        foreach (var bag in _metaGrid.PlacedBags)
        {
            if (!bag.IsPlaced) continue;

            var themeName = GetBagThemeName(bag);
            var texture = UIThemeLoader.GetBagThemeTexture(themeName);

            if (texture != null)
            {
                CreateTexturedBagBorder(bag, texture);
            }
            else
            {
                CreateColorRectBagBordersForBag(bag);
            }
        }
    }

    /// <summary>
    /// Maps a bag's properties to a theme texture name for distinct visual framing.
    /// </summary>
    private static string GetBagThemeName(Bag bag)
    {
        // Priority order: most distinctive property wins
        if (bag.HasProperty(BagPropertyType.Reinforced))
            return "ornate";
        if (bag.HasProperty(BagPropertyType.Null))
            return "ornate";
        if (bag.HasProperty(BagPropertyType.Insulated))
            return "warm";
        if (bag.HasProperty(BagPropertyType.Gravity))
            return "brass";
        return "default";
    }

    private void CreateTexturedBagBorder(Bag bag, Texture2D texture)
    {
        var positions = bag.GetCurrentMetaGridPositions();
        if (positions.Count == 0) return;

        int minCol = int.MaxValue, maxCol = int.MinValue;
        int minRow = int.MaxValue, maxRow = int.MinValue;
        foreach (var p in positions)
        {
            if (p.Col < minCol) minCol = p.Col;
            if (p.Col > maxCol) maxCol = p.Col;
            if (p.Row < minRow) minRow = p.Row;
            if (p.Row > maxRow) maxRow = p.Row;
        }

        float cs = GridConstants.CellSize;
        int pad = GridConstants.BagBorderPadding;
        int margin = GridConstants.BagBorderPatchMargin;
        float minPatchSize = margin * 2;

        float pixelX = minCol * cs - pad;
        float pixelY = minRow * cs - pad;
        float pixelW = (maxCol - minCol + 1) * cs + pad * 2;
        float pixelH = (maxRow - minRow + 1) * cs + pad * 2;

        // Center-expand if bag area is smaller than NinePatch margins require
        if (pixelW < minPatchSize)
        {
            float deficit = minPatchSize - pixelW;
            pixelX -= deficit / 2f;
            pixelW = minPatchSize;
        }
        if (pixelH < minPatchSize)
        {
            float deficit = minPatchSize - pixelH;
            pixelY -= deficit / 2f;
            pixelH = minPatchSize;
        }

        var patch = new NinePatchRect
        {
            Texture = texture,
            Position = new Vector2(pixelX, pixelY),
            Size = new Vector2(pixelW, pixelH),
            PatchMarginLeft = margin,
            PatchMarginRight = margin,
            PatchMarginTop = margin,
            PatchMarginBottom = margin,
            AxisStretchHorizontal = NinePatchRect.AxisStretchMode.Tile,
            AxisStretchVertical = NinePatchRect.AxisStretchMode.Tile,
            Modulate = new Color(1f, 1f, 1f, GridConstants.BagBorderTextureAlpha),
            ZIndex = 1,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(patch);
        _bagBorderPatches.Add(patch);
    }

    private void CreateColorRectBagBordersForBag(Bag bag)
    {
        var positions = bag.GetCurrentMetaGridPositions();
        var posSet = new HashSet<CoreVec>(positions);

        foreach (var pos in positions)
        {
            var pixelPos = GridCoordinates.GridToPixel(pos);

            foreach (var dir in CoreVec.CardinalDirections)
            {
                var neighborPos = pos + dir;
                if (posSet.Contains(neighborPos)) continue;

                var border = CreateBorderRectWithStyle(pixelPos, dir, GridConstants.BagBorderWidth, GridConstants.BagBorderColor);
                AddChild(border);
                _bagBorders.Add(border);
            }
        }
    }

    private void CreateHintedBorders()
    {
        CreateReagentBorders(_hintedBorders, r => r.IsHinted, 2, GridConstants.HintedBorderColor);
    }

    private void CreateLockedBorders()
    {
        CreateReagentBorders(_lockedBorders, r => r.IsLocked, 2, GridConstants.LockedBorderColor);
    }

    /// <summary>
    /// Computes bag thermal imbalance and renders animated tint overlays.
    /// Called from RefreshDisplay() so overlays update on every placement/removal.
    /// </summary>
    private void UpdateBagStateOverlay()
    {
        var violations = ChemistryEngine.GetBagBalanceViolations(_metaGrid);
        _bagStateOverlay.Render(violations);
    }

    /// <summary>Number of bag state tint elements currently rendered, for testing.</summary>
    public int BagStateTintCount => _bagStateOverlay.RenderedCount;

    /// <summary>Number of cell mount frame elements currently rendered, for testing.</summary>
    public int CellMountCount => _cellMountOverlay.RenderedCount;

    /// <summary>Number of per-cell aspect tile TextureRects currently rendered, for testing.</summary>
    public int AspectTileCount => _aspectTiles.Count;

    /// <summary>Number of bags with active thermal tint, for testing.</summary>
    public int BagStateActiveGroupCount => _bagStateOverlay.ActiveGroupCount;

    /// <summary>
    /// Computes damage violations (fragile + volatile) and renders overlays on affected cells.
    /// Called from RefreshDisplay() so overlays update on every placement/removal.
    /// </summary>
    private void UpdateDamageOverlay()
    {
        if (_reactionRules == null)
        {
            _damageOverlay.Clear();
            return;
        }

        var violations = ViolationTracker.ComputeAll(_metaGrid, _reactionRules);
        _damageOverlay.Render(violations);
    }

    /// <summary>Number of damage overlay elements currently rendered, for testing.</summary>
    public int DamageOverlayCount => _damageOverlay.RenderedCount;

    /// <summary>
    /// Pulse-highlights all violation cells (3 red flashes over 1s), then calls onComplete.
    /// Used when the player confirms with violations present.
    /// </summary>
    public void PulseViolations(System.Action? onComplete)
    {
        if (_reactionRules == null)
        {
            onComplete?.Invoke();
            return;
        }

        var violations = ViolationTracker.ComputeAll(_metaGrid, _reactionRules);
        _damageOverlay.PulseViolations(violations, onComplete);
    }

    public override void _Draw()
    {
        if (_metaGrid == null) return;

        foreach (var bag in _metaGrid.PlacedBags)
        {
            if (!bag.HasProperty(BagPropertyType.Gravity) || !bag.IsPlaced)
                continue;

            DrawGravityArrow(bag);
        }
    }

    private void DrawGravityArrow(Bag bag)
    {
        // Compute the bag's pixel bounding box from its placed cells
        var positions = bag.GetCurrentMetaGridPositions();
        if (positions.Count == 0) return;

        int minCol = int.MaxValue, maxCol = int.MinValue;
        int minRow = int.MaxValue, maxRow = int.MinValue;
        foreach (var pos in positions)
        {
            if (pos.Col < minCol) minCol = pos.Col;
            if (pos.Col > maxCol) maxCol = pos.Col;
            if (pos.Row < minRow) minRow = pos.Row;
            if (pos.Row > maxRow) maxRow = pos.Row;
        }

        float cs = GridConstants.CellSize;
        float bagPixelX = minCol * cs;
        float bagPixelY = minRow * cs;
        float bagPixelW = (maxCol - minCol + 1) * cs;
        float bagPixelH = (maxRow - minRow + 1) * cs;

        // Center of the bag in pixel coords
        float cx = bagPixelX + bagPixelW / 2f;
        float cy = bagPixelY + bagPixelH / 2f;

        // Arrow size: about 2 cells tall, 1.2 cells wide
        float arrowLength = cs * 1.6f;
        float arrowWidth = cs * 1.0f;

        // Build triangle points based on gravity direction
        // The arrow points IN the direction of gravity (where things fall toward)
        Godot.Vector2[] points;
        var dir = bag.GravityDirection;

        switch (dir)
        {
            case GravityDirection.Down:
                points = new Godot.Vector2[]
                {
                    new(cx, cy + arrowLength / 2f),            // tip (bottom)
                    new(cx - arrowWidth / 2f, cy - arrowLength / 2f), // top-left
                    new(cx + arrowWidth / 2f, cy - arrowLength / 2f), // top-right
                };
                break;
            case GravityDirection.Up:
                points = new Godot.Vector2[]
                {
                    new(cx, cy - arrowLength / 2f),            // tip (top)
                    new(cx + arrowWidth / 2f, cy + arrowLength / 2f), // bottom-right
                    new(cx - arrowWidth / 2f, cy + arrowLength / 2f), // bottom-left
                };
                break;
            case GravityDirection.Left:
                points = new Godot.Vector2[]
                {
                    new(cx - arrowLength / 2f, cy),            // tip (left)
                    new(cx + arrowLength / 2f, cy - arrowWidth / 2f), // top-right
                    new(cx + arrowLength / 2f, cy + arrowWidth / 2f), // bottom-right
                };
                break;
            case GravityDirection.Right:
                points = new Godot.Vector2[]
                {
                    new(cx + arrowLength / 2f, cy),            // tip (right)
                    new(cx - arrowLength / 2f, cy + arrowWidth / 2f), // bottom-left
                    new(cx - arrowLength / 2f, cy - arrowWidth / 2f), // top-left
                };
                break;
            default:
                return;
        }

        var colors = new Color[] { GravityArrowColor, GravityArrowColor, GravityArrowColor };
        DrawPolygon(points, colors);
    }

    private void CreateReagentBorders(List<ColorRect> borderList, System.Func<Reagent, bool> predicate, float borderWidth, Color color)
    {
        foreach (var border in borderList)
        {
            border.QueueFree();
        }
        borderList.Clear();

        for (int col = 0; col < _metaGrid.Width; col++)
        {
            for (int row = 0; row < _metaGrid.Height; row++)
            {
                var cell = _metaGrid.GetCell(col, row)!;
                if (!cell.IsOccupied) continue;

                var reagent = (Reagent)cell.OccupyingReagent!;
                if (!predicate(reagent)) continue;

                var pos = new CoreVec(col, row);
                var pixelPos = GridCoordinates.GridToPixel(pos);

                foreach (var dir in CoreVec.CardinalDirections)
                {
                    var neighborPos = pos + dir;
                    var neighbor = _metaGrid.GetCell(neighborPos);
                    bool isBoundary = neighbor == null || neighbor.OccupyingReagent != reagent;

                    if (!isBoundary) continue;

                    var border = CreateBorderRectWithStyle(pixelPos, dir, borderWidth, color);
                    AddChild(border);
                    borderList.Add(border);
                }
            }
        }
    }

    private static ColorRect CreateBorderRectWithStyle(Vector2 cellPixelPos, CoreVec direction, float borderWidth, Color color)
    {
        float cs = GridConstants.CellSize;
        Vector2 pos;
        Vector2 size;

        if (direction.Row == -1)
        {
            pos = cellPixelPos;
            size = new Vector2(cs, borderWidth);
        }
        else if (direction.Row == 1)
        {
            pos = cellPixelPos + new Vector2(0, cs - borderWidth);
            size = new Vector2(cs, borderWidth);
        }
        else if (direction.Col == -1)
        {
            pos = cellPixelPos;
            size = new Vector2(borderWidth, cs);
        }
        else
        {
            pos = cellPixelPos + new Vector2(cs - borderWidth, 0);
            size = new Vector2(borderWidth, cs);
        }

        return new ColorRect
        {
            Position = pos,
            Size = size,
            Color = color,
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
            var spriteSheet = ReagentSpriteSheet.GetOrLoad(catalogId, spec, reagent.Definition.PrimaryAspectId);
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
    /// Creates per-cell TextureRect nodes showing aspect tile sprites for each occupied cell.
    /// Falls back to the existing ColorRect color when no tile texture is available.
    /// </summary>
    private void UpdateAspectTiles()
    {
        // Remove old tiles
        foreach (var tile in _aspectTiles)
            tile.QueueFree();
        _aspectTiles.Clear();

        for (int col = 0; col < _metaGrid.Width; col++)
        {
            for (int row = 0; row < _metaGrid.Height; row++)
            {
                var cell = _metaGrid.GetCell(col, row)!;
                if (!cell.IsOccupied) continue;

                var reagent = (Reagent)cell.OccupyingReagent!;
                var localPos = new CoreVec(col, row) - reagent.PlacedPosition!.Value;
                var aspects = reagent.GetAspectsAtLocalPosition(localPos);
                if (aspects.IsEmpty || aspects.Aspects.Count == 0) continue;

                var aspectId = aspects.Aspects[0].Id;
                var texture = UIThemeLoader.GetAspectTileTexture(aspectId);
                if (texture == null) continue;

                var tileRect = new TextureRect
                {
                    Texture = texture,
                    Position = GridCoordinates.GridToPixel(new CoreVec(col, row)),
                    Size = new Vector2(GridConstants.CellSize, GridConstants.CellSize),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspect,
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                AddChild(tileRect);
                _aspectTiles.Add(tileRect);
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
            // Bag and reagent selection are mutually exclusive
            if (_selectedBag != null)
                DeselectBag();
            // Hide hover frame when a reagent is selected for placement
            _hoverFrameOverlay.HideFrame();
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
            _hoverDebounce?.OnMouseEntered();
            UpdateHoveredReagent(gridPos);

            // Update bag ghost preview during drag
            if (_isDraggingBag)
                UpdateBagGhostPreview();
        }
        else if (_mouseInsideGrid)
        {
            _mouseInsideGrid = false;
            _hoverGridPos = null;
            ClearGhost();
            ClearBagGhost();
            if (_hoverDebounce != null)
            {
                // Debounced: delay clearing hovered reagent
                _hoverDebounce.OnMouseExited();
            }
            else
            {
                // Instant (backward compatible)
                UpdateHoveredReagent(null);
            }
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

            // Update hover frame overlay
            if (hoveredReagent != null && hoveredReagent.PlacedPosition.HasValue)
            {
                var pixelPos = GridCoordinates.GridToPixel(hoveredReagent.PlacedPosition.Value);
                _hoverFrameOverlay.ShowFrame(hoveredReagent, pixelPos);
            }
            else
            {
                _hoverFrameOverlay.HideFrame();
            }

            EmitSignal(SignalName.HoveredReagentChanged);
        }
    }

    private void HandleMouseClick(InputEventMouseButton mouseBtn)
    {
        if (!_mouseInsideGrid || _hoverGridPos == null) return;

        if (mouseBtn.ButtonIndex == MouseButton.Left)
        {
            // Bag interaction takes priority when a bag is selected or dragging
            if (_selectedBag != null)
            {
                HandleBagLeftClick();
                return;
            }
            HandleLeftClick();
        }
        else if (mouseBtn.ButtonIndex == MouseButton.Right)
        {
            // Bag right-click: rotate selected bag
            if (_selectedBag != null)
            {
                HandleBagRightClick();
                return;
            }

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
        if (key.Keycode == Key.Escape)
        {
            if (_selectedBag != null)
            {
                DeselectBag();
                return;
            }
            if (_selectedReagent != null)
            {
                _selectedReagent = null;
                ClearGhost();
                return;
            }
            return;
        }

        if (_selectedReagent == null) return;

        if (key.Keycode == Key.R)
        {
            _selectedReagent.RotateClockwise();
            UpdateGhostPreview();
        }
    }

    private void UpdateGhostPreview()
    {
        ClearGhost();
        _currentWarningLevel = PlacementWarningLevel.Invalid;
        _effectivePlacePos = null;
        if (_selectedReagent == null || _hoverGridPos == null) return;

        // Calculate effective placement position: all reagents in gravity bags fall, not just Heavy
        var effectivePos = _hoverGridPos.Value;
        var fallPos = _metaGrid.CalculateFallPosition(_selectedReagent, _hoverGridPos.Value);
        if (fallPos != _hoverGridPos.Value && _metaGrid.CanPlaceReagent(_selectedReagent, fallPos))
            effectivePos = fallPos;
        _effectivePlacePos = effectivePos;

        var positions = _selectedReagent.GetMetaGridPositions(effectivePos);
        _isPlacementValid = _metaGrid.CanPlaceReagent(_selectedReagent, effectivePos);

        Color ghostColor;
        if (!_isPlacementValid)
        {
            _currentWarningLevel = PlacementWarningLevel.Invalid;
            ghostColor = GridConstants.InvalidPlacement;
        }
        else if (_reactionRules != null && _reactionRules.Count > 0)
        {
            var reactions = ChemistryEngine.EvaluateProspective(
                _metaGrid, _selectedReagent, effectivePos, _reactionRules);

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
        if (_selectedReagent == null || _effectivePlacePos == null || _reactionRules == null)
            return;

        var placePos = _effectivePlacePos.Value;
        var highlights = new HighlightSet();

        // Constraint highlights (Sharp on edge, Fragile in gravity, etc.)
        var constraintHighlights = HighlightEngine.ComputeConstraintHighlights(
            _metaGrid, _selectedReagent, placePos);
        highlights.AddRange(constraintHighlights.GetByLayer(HighlightLayer.Constraint));

        // Ghost constraint highlights (rendered on ghost cells themselves)
        var ghostConstraints = HighlightEngine.ComputeGhostConstraintHighlights(
            _metaGrid, _selectedReagent, placePos);
        highlights.AddRange(ghostConstraints.GetByLayer(HighlightLayer.GhostOverlay));

        // Interaction highlights (adjacent reactive aspects)
        if (_isPlacementValid)
        {
            var interactionHighlights = HighlightEngine.ComputeInteractionHighlights(
                _metaGrid, _selectedReagent, placePos, _reactionRules, _statusThresholds);
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
        if (_hoverGridPos == null) return;

        if (_selectedReagent == null)
        {
            // Left-click on a placed reagent with nothing selected → emit signal
            var clickedReagent = _metaGrid.GetReagentAtCell(_hoverGridPos.Value);
            if (clickedReagent != null)
            {
                EmitSignal(SignalName.PlacedReagentClicked);
                return;
            }

            // Left-click on an empty bag cell → select the bag
            var cell = _metaGrid.GetCell(_hoverGridPos.Value);
            if (cell != null && cell.IsInBag && !cell.IsOccupied)
            {
                SelectBag((Bag)cell.BagOwner!);
                return;
            }
            return;
        }

        if (_isPlacementValid)
        {
            var placePos = _effectivePlacePos ?? _hoverGridPos.Value;
            var clickPos = _hoverGridPos.Value;
            bool isFalling = placePos != clickPos;

            if (_actionHistory != null)
            {
                var action = new PlaceReagentAction(_selectedReagent, placePos, _selectedReagent.Rotation);
                if (!_actionHistory.Execute(action, _metaGrid)) return;
            }
            else
            {
                _metaGrid.PlaceReagent(_selectedReagent, placePos);
            }

            var placedReagent = _selectedReagent;
            _selectedReagent = null;
            ClearGhost();
            HideRejectionTooltip();
            RefreshDisplay();

            if (isFalling && _fallingAnimator != null)
            {
                // Temporarily hide the destination cells so the falling overlay is visible
                HideCellsForReagent(placedReagent, placePos);
                _fallingAnimator.PlayFall(placedReagent, clickPos, placePos, () =>
                {
                    // Restore the destination cells after animation
                    RefreshDisplay();
                });
            }

            EmitSignal(SignalName.ReagentPlaced);
        }
        else
        {
            ShowRejectionForCurrentPosition();
        }
    }

    /// <summary>
    /// Temporarily hides the cell colors for a placed reagent by setting them to the bag empty tint.
    /// Used during falling animations so the overlay is visible over the destination.
    /// </summary>
    private void HideCellsForReagent(Reagent reagent, CoreVec position)
    {
        var positions = reagent.GetMetaGridPositions(position);
        var bagIndexMap = new Dictionary<Core.Bags.Bag, int>();
        for (int i = 0; i < _metaGrid.PlacedBags.Count; i++)
            bagIndexMap[_metaGrid.PlacedBags[i]] = i;

        foreach (var pos in positions)
        {
            if (pos.Col < 0 || pos.Col >= _metaGrid.Width ||
                pos.Row < 0 || pos.Row >= _metaGrid.Height)
                continue;

            var bag = _metaGrid.GetBagAtCell(pos);
            int bagIdx = bag != null ? bagIndexMap.GetValueOrDefault(bag, 0) : 0;
            _cellRects[pos.Col, pos.Row].Color = GridConstants.GetBagTint(bagIdx);
        }

        // Also hide sprite renderer for this reagent during animation
        if (_spriteRenderers.TryGetValue(reagent, out var renderer))
            renderer.Visible = false;
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
        _hoverDebounce?.Process((float)delta);

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

    // --- Bag selection / movement ---

    /// <summary>Programmatically select a bag (or null to deselect).</summary>
    public void SelectBag(Bag? bag)
    {
        if (_selectedBag == bag) return;

        _selectedBag = bag;
        _isDraggingBag = false;
        _bagDragStartGridPos = null;
        ClearBagGhost();
        UpdateSelectedBagBorders();
        EmitSignal(SignalName.BagSelected);
    }

    /// <summary>Deselect the currently selected bag.</summary>
    public void DeselectBag()
    {
        if (_selectedBag == null) return;
        _selectedBag = null;
        _isDraggingBag = false;
        _bagDragStartGridPos = null;
        ClearBagGhost();
        ClearSelectedBagBorders();
        EmitSignal(SignalName.BagSelected);
    }

    private void ClearBagGhost()
    {
        foreach (var ghost in _bagGhostCells)
            ghost.QueueFree();
        _bagGhostCells.Clear();
    }

    private void ClearSelectedBagBorders()
    {
        foreach (var border in _selectedBagBorders)
            border.QueueFree();
        _selectedBagBorders.Clear();
    }

    private void UpdateSelectedBagBorders()
    {
        ClearSelectedBagBorders();
        if (_selectedBag == null || !_selectedBag.IsPlaced) return;

        var positions = new HashSet<CoreVec>();
        foreach (var pos in _selectedBag.GetMetaGridPositions(_selectedBag.PositionOnMetaGrid!.Value))
            positions.Add(pos);

        foreach (var pos in positions)
        {
            var pixelPos = GridCoordinates.GridToPixel(pos);
            foreach (var dir in CoreVec.CardinalDirections)
            {
                var neighborPos = pos + dir;
                bool isBoundary = !positions.Contains(neighborPos);
                if (!isBoundary) continue;

                var border = CreateBorderRectWithStyle(
                    pixelPos, dir, GridConstants.SelectedBagBorderWidth, GridConstants.SelectedBagBorderColor);
                border.ZIndex = 6;
                AddChild(border);
                _selectedBagBorders.Add(border);
            }
        }
    }

    private void UpdateBagGhostPreview()
    {
        ClearBagGhost();
        if (_selectedBag == null || !_isDraggingBag || _hoverGridPos == null) return;

        // Compute the new bag origin based on the drag delta
        var newOrigin = ComputeBagDragOrigin();
        if (newOrigin == null) return;

        var positions = _selectedBag.GetMetaGridPositions(newOrigin.Value);

        // Check if this move would be valid
        bool valid = _metaGrid.CanMoveBag(_selectedBag, newOrigin.Value);
        var ghostColor = valid ? GridConstants.BagGhostValid : GridConstants.BagGhostInvalid;

        foreach (var pos in positions)
        {
            if (!_metaGrid.IsInBounds(pos)) continue;
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
            _bagGhostCells.Add(ghost);
        }
    }

    private CoreVec? ComputeBagDragOrigin()
    {
        if (_selectedBag == null || _bagDragStartGridPos == null || _hoverGridPos == null) return null;
        var origin = _selectedBag.PositionOnMetaGrid!.Value;
        var delta = _hoverGridPos.Value - _bagDragStartGridPos.Value;
        return origin + delta;
    }

    private void HandleBagLeftClick()
    {
        if (_selectedBag == null || _hoverGridPos == null) return;

        if (_isDraggingBag)
        {
            // Release drag — attempt to move the bag
            var newOrigin = ComputeBagDragOrigin();
            if (newOrigin != null && newOrigin.Value != _selectedBag.PositionOnMetaGrid!.Value)
            {
                if (_metaGrid.MoveBag(_selectedBag, newOrigin.Value))
                {
                    _isDraggingBag = false;
                    _bagDragStartGridPos = null;
                    ClearBagGhost();
                    RefreshDisplay();
                    UpdateSelectedBagBorders();
                    EmitSignal(SignalName.BagMoved);
                    return;
                }
                else
                {
                    ShowRejectionTooltip("Cannot move bag here", _hoverGridPos.Value);
                }
            }

            // Cancel drag on same-position click or invalid move
            _isDraggingBag = false;
            _bagDragStartGridPos = null;
            ClearBagGhost();
            return;
        }

        // Check if clicking on the selected bag to start dragging
        var cell = _metaGrid.GetCell(_hoverGridPos.Value);
        if (cell != null && cell.IsInBag && cell.BagOwner == _selectedBag)
        {
            _isDraggingBag = true;
            _bagDragStartGridPos = _hoverGridPos.Value;
            return;
        }

        // Clicking outside the selected bag — check if selecting a different bag
        if (cell != null && cell.IsInBag && !cell.IsOccupied)
        {
            var bag = (Bag)cell.BagOwner!;
            SelectBag(bag);
            return;
        }

        // Clicking on a reagent or outside any bag — deselect
        DeselectBag();
    }

    private void HandleBagRightClick()
    {
        if (_selectedBag == null) return;

        // Cancel drag if active
        if (_isDraggingBag)
        {
            _isDraggingBag = false;
            _bagDragStartGridPos = null;
            ClearBagGhost();
            return;
        }

        // Rotate the selected bag
        if (_metaGrid.RotateBag(_selectedBag))
        {
            RefreshDisplay();
            UpdateSelectedBagBorders();
            EmitSignal(SignalName.BagRotated);
        }
        else
        {
            if (_hoverGridPos != null)
                ShowRejectionTooltip("Cannot rotate bag", _hoverGridPos.Value);
        }
    }
}
