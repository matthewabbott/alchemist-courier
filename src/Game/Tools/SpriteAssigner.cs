using Godot;
using System;
using System.Text.Json;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Game.Inventory;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Tools;

/// <summary>
/// Standalone tool for assigning polyomino shapes to candidate sprite PNGs.
/// Load a PNG, overlay a clickable grid, click cells to define the shape,
/// set aspect/metadata, preview at game size, and export as JSON catalog entry.
/// </summary>
public partial class SpriteAssigner : Control
{
    private const int SpritePreviewSize = 256;
    private const int MiniPreviewCellSize = 32;
    private const int GridLineWidth = 1;

    private static readonly Color ActiveCellColor = new(0.3f, 0.8f, 0.4f, 0.45f);
    private static readonly Color HoverColor = new(0.5f, 0.5f, 0.55f, 0.3f);
    private static readonly Color GridLineColor = new(0.6f, 0.6f, 0.65f, 0.6f);
    private static readonly Color HeaderBgColor = new(0.12f, 0.12f, 0.16f);
    private static readonly Color PanelBgColor = new(0.1f, 0.1f, 0.13f);
    private static readonly Color LabelColor = new(0.75f, 0.75f, 0.8f);
    private static readonly Color GoldColor = new(0.9f, 0.85f, 0.7f);
    private static readonly Color ButtonColor = new(0.25f, 0.35f, 0.5f);
    private static readonly Color ExportButtonColor = new(0.25f, 0.45f, 0.3f);
    private static readonly Color StatusGoodColor = new(0.4f, 0.85f, 0.5f);
    private static readonly Color StatusNeutralColor = new(0.65f, 0.65f, 0.7f);

    private static readonly string[] AspectOptions =
    {
        "", // None
        AspectIds.Hot,
        AspectIds.Cold,
        AspectIds.Volatile,
        AspectIds.Stable,
        AspectIds.Heavy,
        AspectIds.Toxic,
        AspectIds.Sharp,
        AspectIds.Fragile,
        AspectIds.Catalytic,
        AspectIds.Medicinal,
    };

    private static readonly string[] AspectDisplayNames =
    {
        "(None)",
        "Hot",
        "Cold",
        "Volatile",
        "Stable",
        "Heavy",
        "Toxic",
        "Sharp",
        "Fragile",
        "Catalytic",
        "Medicinal",
    };

    private static readonly string[] RarityOptions = { "Common", "Uncommon", "Rare", "VeryRare", "Quest" };

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

    // Metadata fields (SA.2)
    private LineEdit _itemNameField = null!;
    private OptionButton _aspectDropdown = null!;
    private OptionButton _rarityDropdown = null!;
    private TextEdit _flavorTextField = null!;

    // 32px preview (SA.2)
    private Control _miniPreviewContainer = null!;

    // Export (SA.2)
    private string _exportDirectory = "";

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

        // Content: HBox with sprite area (left), controls (center), metadata+preview (right)
        var contentHBox = new HBoxContainer();
        contentHBox.AddThemeConstantOverride("separation", 16);
        contentHBox.SizeFlagsVertical = SizeFlags.ExpandFill;
        mainVBox.AddChild(contentHBox);

        BuildSpriteArea(contentHBox);
        BuildControlsPanel(contentHBox);
        BuildMetadataPanel(contentHBox);

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

        var exportButton = CreateStyledButton("Export JSON", ExportButtonColor);
        exportButton.Pressed += OnExportPressed;
        headerHBox.AddChild(exportButton);

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
        panel.CustomMinimumSize = new Vector2(180, 0);
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

    private void BuildMetadataPanel(HBoxContainer parent)
    {
        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        parent.AddChild(scroll);

        var panel = new VBoxContainer();
        panel.AddThemeConstantOverride("separation", 8);
        panel.CustomMinimumSize = new Vector2(220, 0);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(panel);

        // --- Metadata section ---
        var metaHeader = new Label { Text = "Item Metadata" };
        metaHeader.AddThemeColorOverride("font_color", GoldColor);
        metaHeader.AddThemeFontSizeOverride("font_size", 15);
        panel.AddChild(metaHeader);

        // Item name
        AddFieldLabel(panel, "Item Name:");
        _itemNameField = new LineEdit { PlaceholderText = "e.g. firevine" };
        _itemNameField.CustomMinimumSize = new Vector2(0, 28);
        panel.AddChild(_itemNameField);

        // Aspect dropdown
        AddFieldLabel(panel, "Aspect:");
        _aspectDropdown = new OptionButton();
        for (int i = 0; i < AspectOptions.Length; i++)
            _aspectDropdown.AddItem(AspectDisplayNames[i], i);
        _aspectDropdown.Selected = 0;
        _aspectDropdown.ItemSelected += _ => RefreshMiniPreview();
        panel.AddChild(_aspectDropdown);

        // Rarity dropdown
        AddFieldLabel(panel, "Rarity:");
        _rarityDropdown = new OptionButton();
        for (int i = 0; i < RarityOptions.Length; i++)
            _rarityDropdown.AddItem(RarityOptions[i], i);
        _rarityDropdown.Selected = 0; // Common
        panel.AddChild(_rarityDropdown);

        // Flavor text
        AddFieldLabel(panel, "Flavor Text:");
        _flavorTextField = new TextEdit();
        _flavorTextField.CustomMinimumSize = new Vector2(0, 60);
        _flavorTextField.PlaceholderText = "A short thematic description...";
        panel.AddChild(_flavorTextField);

        panel.AddChild(new HSeparator());

        // --- 32px preview section ---
        var previewHeader = new Label { Text = "Game Preview (32px/cell)" };
        previewHeader.AddThemeColorOverride("font_color", GoldColor);
        previewHeader.AddThemeFontSizeOverride("font_size", 15);
        panel.AddChild(previewHeader);

        _miniPreviewContainer = new Control
        {
            CustomMinimumSize = new Vector2(MiniPreviewCellSize * 4, MiniPreviewCellSize * 4),
        };
        // Dark background for the preview area
        var previewBg = new ColorRect
        {
            Color = new Color(0.08f, 0.08f, 0.1f),
            Position = Vector2.Zero,
            Size = new Vector2(MiniPreviewCellSize * 4, MiniPreviewCellSize * 4),
        };
        _miniPreviewContainer.AddChild(previewBg);
        panel.AddChild(_miniPreviewContainer);

        var previewNote = new Label
        {
            Text = "Shows how the item looks at actual game cell size.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        previewNote.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        previewNote.AddThemeFontSizeOverride("font_size", 10);
        panel.AddChild(previewNote);
    }

    private static void AddFieldLabel(VBoxContainer parent, string text)
    {
        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", LabelColor);
        label.AddThemeFontSizeOverride("font_size", 12);
        parent.AddChild(label);
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
        RefreshMiniPreview();
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

    // --- 32px mini preview ---

    private void RefreshMiniPreview()
    {
        // Clear previous preview children (keep the background at index 0)
        while (_miniPreviewContainer.GetChildCount() > 1)
        {
            var child = _miniPreviewContainer.GetChild(_miniPreviewContainer.GetChildCount() - 1);
            _miniPreviewContainer.RemoveChild(child);
            child.QueueFree();
        }

        var shape = GetTrimmedShape();
        if (shape == null) return;

        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);

        // Get the aspect color for cell tinting
        string selectedAspectId = GetSelectedAspectId();
        Color aspectColor = string.IsNullOrEmpty(selectedAspectId)
            ? new Color(0.6f, 0.55f, 0.5f)
            : GridConstants.GetAspectColor(selectedAspectId);

        // Offset to center the preview
        float totalWidth = cols * MiniPreviewCellSize;
        float totalHeight = rows * MiniPreviewCellSize;
        float maxArea = MiniPreviewCellSize * 4;
        float offsetX = (maxArea - totalWidth) / 2f;
        float offsetY = (maxArea - totalHeight) / 2f;
        if (offsetX < 0) offsetX = 0;
        if (offsetY < 0) offsetY = 0;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!shape[r, c]) continue;

                var cellRect = new ColorRect
                {
                    Position = new Vector2(offsetX + c * MiniPreviewCellSize, offsetY + r * MiniPreviewCellSize),
                    Size = new Vector2(MiniPreviewCellSize - 1, MiniPreviewCellSize - 1),
                    Color = aspectColor,
                };
                _miniPreviewContainer.AddChild(cellRect);
            }
        }
    }

    private string GetSelectedAspectId()
    {
        int idx = _aspectDropdown.Selected;
        if (idx < 0 || idx >= AspectOptions.Length) return "";
        return AspectOptions[idx];
    }

    private Rarity GetSelectedRarity()
    {
        int idx = _rarityDropdown.Selected;
        return idx switch
        {
            1 => Rarity.Uncommon,
            2 => Rarity.Rare,
            3 => Rarity.VeryRare,
            4 => Rarity.Quest,
            _ => Rarity.Common,
        };
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

    // --- Export ---

    private void OnExportPressed()
    {
        var shape = GetTrimmedShape();
        if (shape == null)
        {
            _statusLabel.Text = "Cannot export: no cells selected.";
            return;
        }

        string itemName = _itemNameField.Text.Trim();
        if (string.IsNullOrEmpty(itemName))
        {
            _statusLabel.Text = "Cannot export: item name is required.";
            return;
        }

        // Build the JSON catalog entry
        string json = BuildExportJson(itemName, shape);

        // Open a folder select dialog for the output directory
        var dialog = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new string[] { "*.json ; JSON Files" },
            Title = "Export Catalog Entry",
            CurrentFile = $"{SanitizeFilename(itemName)}.json",
        };

        if (!string.IsNullOrEmpty(_exportDirectory))
            dialog.CurrentDir = _exportDirectory;

        dialog.FileSelected += path =>
        {
            SaveExportJson(path, json);
            _exportDirectory = System.IO.Path.GetDirectoryName(path) ?? "";
            dialog.QueueFree();

            // Batch workflow: clear selection but keep PNG loaded
            ClearGridKeepImage();
        };
        dialog.Canceled += () => dialog.QueueFree();

        AddChild(dialog);
        dialog.PopupCentered(new Godot.Vector2I(700, 500));
    }

    private string BuildExportJson(string itemName, bool[,] shape)
    {
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);

        // Convert bool[,] to bool[][] (jagged) for JSON
        var jagged = new bool[rows][];
        for (int r = 0; r < rows; r++)
        {
            jagged[r] = new bool[cols];
            for (int c = 0; c < cols; c++)
                jagged[r][c] = shape[r, c];
        }

        string aspectId = GetSelectedAspectId();
        string rarity = GetSelectedRarity().ToString();
        string flavorText = _flavorTextField.Text.Trim();
        string shapeName = MatchCanonicalShape(shape) ?? "custom";

        var entry = new
        {
            id = SanitizeId(itemName),
            name = itemName,
            shape = jagged,
            shape_name = shapeName,
            aspect = string.IsNullOrEmpty(aspectId) ? (string?)null : aspectId,
            rarity,
            flavor_text = string.IsNullOrEmpty(flavorText) ? (string?)null : flavorText,
            sprite_source = _loadedFilePath,
            exported_at = DateTime.UtcNow.ToString("o"),
        };

        return JsonSerializer.Serialize(entry, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });
    }

    private void SaveExportJson(string path, string json)
    {
        try
        {
            System.IO.File.WriteAllText(path, json);
            _statusLabel.Text = $"Exported: {System.IO.Path.GetFileName(path)}";
            _statusLabel.AddThemeColorOverride("font_color", StatusGoodColor);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Export failed: {ex.Message}";
            _statusLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.3f, 0.3f));
        }
    }

    private void ClearGridKeepImage()
    {
        // Batch workflow: clear grid and metadata but keep the PNG loaded
        _cells = new bool[_gridResolution, _gridResolution];
        _hoverPos = null;
        RefreshGridDisplay();
        UpdateShapeInfo();
        RefreshMiniPreview();
        _itemNameField.Text = "";
        _aspectDropdown.Selected = 0;
        _rarityDropdown.Selected = 0;
        _flavorTextField.Text = "";
    }

    private static string SanitizeId(string name)
    {
        // Convert display name to snake_case ID
        var sb = new System.Text.StringBuilder(name.Length);
        for (int i = 0; i < name.Length; i++)
        {
            char ch = name[i];
            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToLowerInvariant(ch));
            else if (ch == ' ' || ch == '-' || ch == '_')
                sb.Append('_');
        }
        return sb.ToString();
    }

    private static string SanitizeFilename(string name)
    {
        return SanitizeId(name);
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
        RefreshMiniPreview();
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
        RefreshMiniPreview();
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

    /// <summary>Currently selected aspect ID (empty string if None).</summary>
    public string SelectedAspectId => GetSelectedAspectId();

    /// <summary>Currently selected rarity.</summary>
    public Rarity SelectedRarity => GetSelectedRarity();

    /// <summary>Current item name text.</summary>
    public string ItemName => _itemNameField?.Text ?? "";

    /// <summary>Current flavor text.</summary>
    public string FlavorText => _flavorTextField?.Text ?? "";

    /// <summary>Last export directory used.</summary>
    public string ExportDirectory => _exportDirectory;

    /// <summary>Programmatically toggle a cell (for testing).</summary>
    public void ToggleCell(int col, int row)
    {
        if (col < 0 || col >= _gridResolution || row < 0 || row >= _gridResolution) return;
        _cells[row, col] = !_cells[row, col];
        UpdateCellColor(col, row);
        UpdateShapeInfo();
        RefreshMiniPreview();
    }

    /// <summary>Programmatically load a sprite from path (for testing).</summary>
    public void LoadSpriteFromPath(string path)
    {
        LoadSprite(path);
    }

    /// <summary>Programmatically set the item name (for testing).</summary>
    public void SetItemName(string name)
    {
        _itemNameField.Text = name;
    }

    /// <summary>Programmatically set the aspect by index (for testing).</summary>
    public void SetAspectIndex(int index)
    {
        if (index >= 0 && index < AspectOptions.Length)
        {
            _aspectDropdown.Selected = index;
            RefreshMiniPreview();
        }
    }

    /// <summary>Programmatically set the rarity by index (for testing).</summary>
    public void SetRarityIndex(int index)
    {
        if (index >= 0 && index < RarityOptions.Length)
            _rarityDropdown.Selected = index;
    }

    /// <summary>Programmatically set the flavor text (for testing).</summary>
    public void SetFlavorText(string text)
    {
        _flavorTextField.Text = text;
    }

    /// <summary>Build export JSON string for the current state (for testing).</summary>
    public string? BuildExportJsonForTesting()
    {
        var shape = GetTrimmedShape();
        if (shape == null) return null;
        string itemName = _itemNameField.Text.Trim();
        if (string.IsNullOrEmpty(itemName)) return null;
        return BuildExportJson(itemName, shape);
    }

    /// <summary>Programmatically export to a specific path (for testing).</summary>
    public bool ExportToPath(string path)
    {
        var shape = GetTrimmedShape();
        if (shape == null) return false;
        string itemName = _itemNameField.Text.Trim();
        if (string.IsNullOrEmpty(itemName)) return false;

        string json = BuildExportJson(itemName, shape);
        try
        {
            System.IO.File.WriteAllText(path, json);
            _exportDirectory = System.IO.Path.GetDirectoryName(path) ?? "";
            ClearGridKeepImage();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
