using Godot;
using AlchemistCourier.Core.Reagents;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Tools;

/// <summary>
/// Standalone tool for assigning polyomino shapes to candidate sprite PNGs.
/// Load a PNG, overlay a clickable grid, click cells to define the shape,
/// and see the auto-detected canonical shape name.
/// </summary>
public partial class SpriteAssigner : Control
{
    private const int SpritePreviewSize = 256;
    private const int GridLineWidth = 1;

    private static readonly Color ActiveCellColor = new(0.3f, 0.8f, 0.4f, 0.45f);
    private static readonly Color HoverColor = new(0.5f, 0.5f, 0.55f, 0.3f);
    private static readonly Color GridLineColor = new(0.6f, 0.6f, 0.65f, 0.6f);
    private static readonly Color HeaderBgColor = new(0.12f, 0.12f, 0.16f);
    private static readonly Color PanelBgColor = new(0.1f, 0.1f, 0.13f);
    private static readonly Color LabelColor = new(0.75f, 0.75f, 0.8f);
    private static readonly Color GoldColor = new(0.9f, 0.85f, 0.7f);
    private static readonly Color ButtonColor = new(0.25f, 0.35f, 0.5f);
    private static readonly Color StatusGoodColor = new(0.4f, 0.85f, 0.5f);
    private static readonly Color StatusNeutralColor = new(0.65f, 0.65f, 0.7f);

    // Sprite display
    private TextureRect _spriteDisplay = null!;
    private Label _filenameLabel = null!;
    private string? _loadedFilePath;

    // Grid overlay
    private Control _gridOverlay = null!;
    private ColorRect[,] _cellRects = null!;
    private int _gridResolution = 6;
    private bool[,] _cells = null!;
    private CoreVec? _hoverPos;

    // Resolution selector
    private OptionButton _resolutionDropdown = null!;

    // Status
    private Label _cellCountLabel = null!;
    private Label _shapeNameLabel = null!;
    private Label _statusLabel = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        BuildUI();
    }

    private void BuildUI()
    {
        // Dark background
        var bg = new ColorRect { Color = PanelBgColor };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        AddChild(margin);

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(mainVBox);

        // Header
        BuildHeader(mainVBox);

        // Content: HBox with sprite area (left) and controls (right)
        var contentHBox = new HBoxContainer();
        contentHBox.AddThemeConstantOverride("separation", 16);
        contentHBox.SizeFlagsVertical = SizeFlags.ExpandFill;
        mainVBox.AddChild(contentHBox);

        BuildSpriteArea(contentHBox);
        BuildControlsPanel(contentHBox);

        // Status bar at bottom
        BuildStatusBar(mainVBox);
    }

    private void BuildHeader(VBoxContainer parent)
    {
        var headerPanel = new PanelContainer();
        var headerStyle = new StyleBoxFlat { BgColor = HeaderBgColor };
        headerStyle.SetContentMarginAll(8);
        headerPanel.AddThemeStyleboxOverride("panel", headerStyle);
        parent.AddChild(headerPanel);

        var headerHBox = new HBoxContainer();
        headerHBox.AddThemeConstantOverride("separation", 12);
        headerPanel.AddChild(headerHBox);

        var titleLabel = new Label { Text = "Sprite Assigner" };
        titleLabel.AddThemeColorOverride("font_color", GoldColor);
        titleLabel.AddThemeFontSizeOverride("font_size", 20);
        headerHBox.AddChild(titleLabel);

        headerHBox.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        var loadButton = CreateStyledButton("Load PNG", ButtonColor);
        loadButton.Pressed += OnLoadPressed;
        headerHBox.AddChild(loadButton);

        var clearButton = CreateStyledButton("Clear Grid", new Color(0.4f, 0.3f, 0.3f));
        clearButton.Pressed += OnClearGridPressed;
        headerHBox.AddChild(clearButton);
    }

    private void BuildSpriteArea(HBoxContainer parent)
    {
        var spriteContainer = new VBoxContainer();
        spriteContainer.AddThemeConstantOverride("separation", 6);
        parent.AddChild(spriteContainer);

        _filenameLabel = new Label { Text = "No file loaded" };
        _filenameLabel.AddThemeColorOverride("font_color", LabelColor);
        _filenameLabel.AddThemeFontSizeOverride("font_size", 12);
        spriteContainer.AddChild(_filenameLabel);

        // Sprite + grid overlay container
        var spriteFrame = new Control
        {
            CustomMinimumSize = new Vector2(SpritePreviewSize, SpritePreviewSize),
        };
        spriteContainer.AddChild(spriteFrame);

        // Sprite image
        _spriteDisplay = new TextureRect
        {
            Position = Vector2.Zero,
            Size = new Vector2(SpritePreviewSize, SpritePreviewSize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        spriteFrame.AddChild(_spriteDisplay);

        // Grid overlay on top
        _gridOverlay = new Control
        {
            Position = Vector2.Zero,
            Size = new Vector2(SpritePreviewSize, SpritePreviewSize),
        };
        spriteFrame.AddChild(_gridOverlay);

        InitializeGrid();
    }

    private void BuildControlsPanel(HBoxContainer parent)
    {
        var panel = new VBoxContainer();
        panel.AddThemeConstantOverride("separation", 12);
        panel.CustomMinimumSize = new Vector2(200, 0);
        parent.AddChild(panel);

        // Resolution selector
        var resLabel = new Label { Text = "Grid Resolution:" };
        resLabel.AddThemeColorOverride("font_color", LabelColor);
        resLabel.AddThemeFontSizeOverride("font_size", 13);
        panel.AddChild(resLabel);

        _resolutionDropdown = new OptionButton();
        _resolutionDropdown.AddItem("4 x 4", 4);
        _resolutionDropdown.AddItem("6 x 6", 6);
        _resolutionDropdown.AddItem("8 x 8", 8);
        _resolutionDropdown.Selected = 1; // Default 6x6
        _resolutionDropdown.ItemSelected += OnResolutionChanged;
        panel.AddChild(_resolutionDropdown);

        panel.AddChild(new HSeparator());

        // Cell count
        var countHeader = new Label { Text = "Cell Count:" };
        countHeader.AddThemeColorOverride("font_color", LabelColor);
        countHeader.AddThemeFontSizeOverride("font_size", 13);
        panel.AddChild(countHeader);

        _cellCountLabel = new Label { Text = "0" };
        _cellCountLabel.AddThemeColorOverride("font_color", StatusNeutralColor);
        _cellCountLabel.AddThemeFontSizeOverride("font_size", 16);
        panel.AddChild(_cellCountLabel);

        // Shape name
        var shapeHeader = new Label { Text = "Detected Shape:" };
        shapeHeader.AddThemeColorOverride("font_color", LabelColor);
        shapeHeader.AddThemeFontSizeOverride("font_size", 13);
        panel.AddChild(shapeHeader);

        _shapeNameLabel = new Label { Text = "-" };
        _shapeNameLabel.AddThemeColorOverride("font_color", StatusNeutralColor);
        _shapeNameLabel.AddThemeFontSizeOverride("font_size", 16);
        panel.AddChild(_shapeNameLabel);

        // Instructions
        panel.AddChild(new HSeparator());
        var instrLabel = new Label
        {
            Text = "Click cells on the sprite to define the shape.\nThe shape is auto-matched against canonical polyominoes.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        instrLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        instrLabel.AddThemeFontSizeOverride("font_size", 11);
        panel.AddChild(instrLabel);
    }

    private void BuildStatusBar(VBoxContainer parent)
    {
        var statusPanel = new PanelContainer();
        var statusStyle = new StyleBoxFlat { BgColor = HeaderBgColor };
        statusStyle.SetContentMarginAll(6);
        statusPanel.AddThemeStyleboxOverride("panel", statusStyle);
        parent.AddChild(statusPanel);

        _statusLabel = new Label { Text = "Load a PNG to begin." };
        _statusLabel.AddThemeColorOverride("font_color", LabelColor);
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
        statusPanel.AddChild(_statusLabel);
    }

    // --- Grid management ---

    private void InitializeGrid()
    {
        _cells = new bool[_gridResolution, _gridResolution];
        _cellRects = new ColorRect[_gridResolution, _gridResolution];
        _hoverPos = null;
        RebuildGridOverlay();
    }

    private void RebuildGridOverlay()
    {
        // Clear existing children
        foreach (var child in _gridOverlay.GetChildren())
        {
            child.QueueFree();
        }

        float cellSize = (float)SpritePreviewSize / _gridResolution;

        // Create cells
        _cellRects = new ColorRect[_gridResolution, _gridResolution];
        for (int row = 0; row < _gridResolution; row++)
        {
            for (int col = 0; col < _gridResolution; col++)
            {
                var rect = new ColorRect
                {
                    Position = new Vector2(col * cellSize, row * cellSize),
                    Size = new Vector2(cellSize, cellSize),
                    Color = Colors.Transparent,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                _gridOverlay.AddChild(rect);
                _cellRects[col, row] = rect;
            }
        }

        // Grid lines
        for (int i = 0; i <= _gridResolution; i++)
        {
            // Horizontal
            var hLine = new ColorRect
            {
                Position = new Vector2(0, i * cellSize),
                Size = new Vector2(SpritePreviewSize, GridLineWidth),
                Color = GridLineColor,
                ZIndex = 1,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _gridOverlay.AddChild(hLine);

            // Vertical
            var vLine = new ColorRect
            {
                Position = new Vector2(i * cellSize, 0),
                Size = new Vector2(GridLineWidth, SpritePreviewSize),
                Color = GridLineColor,
                ZIndex = 1,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _gridOverlay.AddChild(vLine);
        }

        RefreshGridDisplay();
    }

    private void RefreshGridDisplay()
    {
        for (int row = 0; row < _gridResolution; row++)
        {
            for (int col = 0; col < _gridResolution; col++)
            {
                UpdateCellColor(col, row);
            }
        }
    }

    private void UpdateCellColor(int col, int row)
    {
        bool isActive = _cells[row, col];
        bool isHovered = _hoverPos != null && _hoverPos.Value.Col == col && _hoverPos.Value.Row == row;

        Color color;
        if (isActive)
            color = ActiveCellColor;
        else if (isHovered)
            color = HoverColor;
        else
            color = Colors.Transparent;

        _cellRects[col, row].Color = color;
    }

    private CoreVec PixelToGrid(Vector2 localPos)
    {
        float cellSize = (float)SpritePreviewSize / _gridResolution;
        return new CoreVec((int)(localPos.X / cellSize), (int)(localPos.Y / cellSize));
    }

    private bool IsInsideGrid(Vector2 localPos, CoreVec gridPos)
    {
        return localPos.X >= 0 && localPos.Y >= 0
            && localPos.X < SpritePreviewSize && localPos.Y < SpritePreviewSize
            && gridPos.Col >= 0 && gridPos.Col < _gridResolution
            && gridPos.Row >= 0 && gridPos.Row < _gridResolution;
    }

    // --- Input handling ---

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            HandleMouseMotion();
        }
        else if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed
                 && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            HandleClick();
        }
    }

    private void HandleMouseMotion()
    {
        var localPos = _gridOverlay.GetLocalMousePosition();
        var gridPos = PixelToGrid(localPos);

        if (IsInsideGrid(localPos, gridPos))
        {
            if (_hoverPos == null || _hoverPos.Value.Col != gridPos.Col || _hoverPos.Value.Row != gridPos.Row)
            {
                var prevHover = _hoverPos;
                _hoverPos = gridPos;
                if (prevHover != null)
                    UpdateCellColor(prevHover.Value.Col, prevHover.Value.Row);
                UpdateCellColor(gridPos.Col, gridPos.Row);
            }
        }
        else if (_hoverPos != null)
        {
            var prev = _hoverPos.Value;
            _hoverPos = null;
            UpdateCellColor(prev.Col, prev.Row);
        }
    }

    private void HandleClick()
    {
        // Check if click is within the grid overlay
        var localPos = _gridOverlay.GetLocalMousePosition();
        var gridPos = PixelToGrid(localPos);
        if (!IsInsideGrid(localPos, gridPos)) return;

        int col = gridPos.Col;
        int row = gridPos.Row;
        _cells[row, col] = !_cells[row, col];
        UpdateCellColor(col, row);
        UpdateShapeInfo();
    }

    // --- Shape detection ---

    private void UpdateShapeInfo()
    {
        // Count active cells
        int cellCount = 0;
        for (int r = 0; r < _gridResolution; r++)
            for (int c = 0; c < _gridResolution; c++)
                if (_cells[r, c]) cellCount++;

        _cellCountLabel.Text = cellCount.ToString();

        if (cellCount == 0)
        {
            _shapeNameLabel.Text = "-";
            _shapeNameLabel.AddThemeColorOverride("font_color", StatusNeutralColor);
            _statusLabel.Text = _loadedFilePath != null ? "Click cells to define shape." : "Load a PNG to begin.";
            return;
        }

        // Extract trimmed shape
        var trimmedShape = GetTrimmedShape();
        if (trimmedShape == null)
        {
            _shapeNameLabel.Text = "?";
            return;
        }

        // Try to match against canonical shapes via ReagentCatalog
        string? shapeName = MatchCanonicalShape(trimmedShape);

        if (shapeName != null)
        {
            _shapeNameLabel.Text = shapeName;
            _shapeNameLabel.AddThemeColorOverride("font_color", StatusGoodColor);
            _statusLabel.Text = $"Shape matched: {shapeName} ({cellCount} cells)";
        }
        else
        {
            _shapeNameLabel.Text = "custom";
            _shapeNameLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.65f, 0.3f));
            _statusLabel.Text = $"Custom shape ({cellCount} cells) — no canonical match";
        }
    }

    private bool[,]? GetTrimmedShape()
    {
        int minRow = _gridResolution, maxRow = -1, minCol = _gridResolution, maxCol = -1;
        for (int r = 0; r < _gridResolution; r++)
        {
            for (int c = 0; c < _gridResolution; c++)
            {
                if (_cells[r, c])
                {
                    if (r < minRow) minRow = r;
                    if (r > maxRow) maxRow = r;
                    if (c < minCol) minCol = c;
                    if (c > maxCol) maxCol = c;
                }
            }
        }

        if (maxRow < 0) return null;

        int rows = maxRow - minRow + 1;
        int cols = maxCol - minCol + 1;
        var result = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                result[r, c] = _cells[minRow + r, minCol + c];

        return result;
    }

    /// <summary>
    /// Match a trimmed shape against all canonical shapes in all 4 rotations.
    /// </summary>
    private static string? MatchCanonicalShape(bool[,] shape)
    {
        foreach (var shapeName in ReagentCatalog.ShapeNames)
        {
            var canonical = ReagentCatalog.GetShapeMatrix(shapeName);
            if (canonical == null) continue;

            // Try all 4 rotations
            var rotated = canonical;
            for (int rotation = 0; rotation < 4; rotation++)
            {
                if (ShapesMatch(shape, rotated))
                    return shapeName;
                rotated = ShapeUtil.Rotate90Clockwise(rotated);
            }
        }
        return null;
    }

    private static bool ShapesMatch(bool[,] a, bool[,] b)
    {
        int aRows = a.GetLength(0), aCols = a.GetLength(1);
        int bRows = b.GetLength(0), bCols = b.GetLength(1);
        if (aRows != bRows || aCols != bCols) return false;

        for (int r = 0; r < aRows; r++)
            for (int c = 0; c < aCols; c++)
                if (a[r, c] != b[r, c]) return false;
        return true;
    }

    // --- File loading ---

    private void OnLoadPressed()
    {
        var dialog = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new string[] { "*.png ; PNG Images", "*.jpg ; JPEG Images" },
            Title = "Load Sprite PNG",
        };

        dialog.FileSelected += path =>
        {
            LoadSprite(path);
            dialog.QueueFree();
        };
        dialog.Canceled += () => dialog.QueueFree();

        AddChild(dialog);
        dialog.PopupCentered(new Godot.Vector2I(700, 500));
    }

    private void LoadSprite(string path)
    {
        var image = new Image();
        var error = image.Load(path);
        if (error != Error.Ok)
        {
            _statusLabel.Text = $"Failed to load: {error}";
            return;
        }

        var texture = ImageTexture.CreateFromImage(image);
        _spriteDisplay.Texture = texture;
        _loadedFilePath = path;
        _filenameLabel.Text = System.IO.Path.GetFileName(path);
        _statusLabel.Text = $"Loaded: {System.IO.Path.GetFileName(path)} ({image.GetWidth()}x{image.GetHeight()})";

        // Clear grid when loading a new image
        ClearGrid();
    }

    // --- Button handlers ---

    private void OnClearGridPressed()
    {
        ClearGrid();
    }

    private void ClearGrid()
    {
        _cells = new bool[_gridResolution, _gridResolution];
        _hoverPos = null;
        RefreshGridDisplay();
        UpdateShapeInfo();
    }

    private void OnResolutionChanged(long index)
    {
        int newRes = _resolutionDropdown.GetItemId((int)index);
        if (newRes == _gridResolution) return;

        _gridResolution = newRes;
        _cells = new bool[_gridResolution, _gridResolution];
        _hoverPos = null;
        RebuildGridOverlay();
        UpdateShapeInfo();
    }

    // --- UI helpers ---

    private static Button CreateStyledButton(string text, Color bgColor)
    {
        var button = new Button { Text = text };
        button.CustomMinimumSize = new Vector2(100, 32);

        var normalStyle = new StyleBoxFlat { BgColor = bgColor };
        normalStyle.SetCornerRadiusAll(4);
        normalStyle.SetContentMarginAll(6);
        button.AddThemeStyleboxOverride("normal", normalStyle);

        var hoverStyle = new StyleBoxFlat { BgColor = bgColor.Lightened(0.15f) };
        hoverStyle.SetCornerRadiusAll(4);
        hoverStyle.SetContentMarginAll(6);
        button.AddThemeStyleboxOverride("hover", hoverStyle);

        var pressedStyle = new StyleBoxFlat { BgColor = bgColor.Darkened(0.15f) };
        pressedStyle.SetCornerRadiusAll(4);
        pressedStyle.SetContentMarginAll(6);
        button.AddThemeStyleboxOverride("pressed", pressedStyle);

        return button;
    }

    // --- Public API for testing ---

    /// <summary>Currently loaded file path.</summary>
    public string? LoadedFilePath => _loadedFilePath;

    /// <summary>Current grid resolution (4, 6, or 8).</summary>
    public int GridResolution => _gridResolution;

    /// <summary>Number of active cells in the grid.</summary>
    public int ActiveCellCount
    {
        get
        {
            int count = 0;
            for (int r = 0; r < _gridResolution; r++)
                for (int c = 0; c < _gridResolution; c++)
                    if (_cells[r, c]) count++;
            return count;
        }
    }

    /// <summary>Get the trimmed shape matrix, or null if no cells active.</summary>
    public bool[,]? CurrentShape => GetTrimmedShape();

    /// <summary>Get the auto-detected shape name, or null if no match.</summary>
    public string? DetectedShapeName
    {
        get
        {
            var shape = GetTrimmedShape();
            return shape != null ? MatchCanonicalShape(shape) : null;
        }
    }

    /// <summary>Programmatically toggle a cell (for testing).</summary>
    public void ToggleCell(int col, int row)
    {
        if (col < 0 || col >= _gridResolution || row < 0 || row >= _gridResolution) return;
        _cells[row, col] = !_cells[row, col];
        UpdateCellColor(col, row);
        UpdateShapeInfo();
    }

    /// <summary>Programmatically load a sprite from path (for testing).</summary>
    public void LoadSpriteFromPath(string path)
    {
        LoadSprite(path);
    }
}
