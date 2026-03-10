using System.Collections.Generic;
using System.Linq;
using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Core.Visual;
using AlchemistCourier.Game.Inventory;
using AlchemistCourier.Game.Testing;
using AlchemistCourier.Game.Visual;

namespace AlchemistCourier.Game.Tools;

/// <summary>
/// Asset preview tool for viewing reagent sprites, bag borders, and UI panels.
/// Shows monomino tiles at 1x/2x/4x zoom plus sample polyomino with frame/mount system.
/// Supports dropdown to switch between preview categories and screenshot capture.
/// </summary>
public partial class AssetPreview : Control
{
    private enum PreviewCategory { Reagents, BagBorders, Background }

    private PreviewCategory _category = PreviewCategory.Reagents;
    private OptionButton _categoryDropdown = null!;
    private VBoxContainer _contentContainer = null!;
    private ColorRect _background = null!;
    private Label _statusLabel = null!;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        _background = new ColorRect { Color = new Color(0.12f, 0.12f, 0.15f) };
        _background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_background);

        var root = new VBoxContainer();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 8);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        margin.AddChild(root);
        AddChild(margin);

        BuildToolbar(root);
        BuildContent(root);

        RefreshContent();
    }

    private void BuildToolbar(VBoxContainer root)
    {
        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", 12);

        var title = new Label { Text = "Asset Preview" };
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        toolbar.AddChild(title);

        _categoryDropdown = new OptionButton();
        _categoryDropdown.AddItem("Reagent Sprites", 0);
        _categoryDropdown.AddItem("Bag Borders", 1);
        _categoryDropdown.AddItem("Background", 2);
        _categoryDropdown.Selected = 0;
        _categoryDropdown.ItemSelected += OnCategoryChanged;
        toolbar.AddChild(_categoryDropdown);

        var screenshotBtn = CreateButton("Screenshot", new Color(0.3f, 0.5f, 0.3f));
        screenshotBtn.Pressed += OnScreenshot;
        toolbar.AddChild(screenshotBtn);

        _statusLabel = new Label { Text = "" };
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
        _statusLabel.AddThemeColorOverride("font_color", Colors.LightGray);
        _statusLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Right;
        toolbar.AddChild(_statusLabel);

        root.AddChild(toolbar);

        var sep = new HSeparator();
        root.AddChild(sep);
    }

    private void BuildContent(VBoxContainer root)
    {
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };

        _contentContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _contentContainer.AddThemeConstantOverride("separation", 16);
        scroll.AddChild(_contentContainer);
        root.AddChild(scroll);
    }

    private void RefreshContent()
    {
        foreach (var child in _contentContainer.GetChildren())
            if (child is Node n) n.QueueFree();

        switch (_category)
        {
            case PreviewCategory.Reagents:
                BuildReagentPreview();
                break;
            case PreviewCategory.BagBorders:
                BuildBagBorderPreview();
                break;
            case PreviewCategory.Background:
                BuildBackgroundPreview();
                break;
        }
    }

    private void BuildReagentPreview()
    {
        var allDefs = ReagentCatalog.GetAll();
        var grouped = allDefs
            .GroupBy(d => d.PrimaryAspectId ?? "unknown")
            .OrderBy(g => g.Key);

        int totalCount = 0;
        int customCount = 0;

        foreach (var group in grouped)
        {
            var aspectId = group.Key;
            var aspectColor = GridConstants.GetAspectColor(aspectId);

            // Group header
            var header = new HBoxContainer();
            header.AddThemeConstantOverride("separation", 8);

            var colorSwatch = new ColorRect
            {
                CustomMinimumSize = new Vector2(16, 16),
                Color = aspectColor
            };
            header.AddChild(colorSwatch);

            var headerLabel = new Label
            {
                Text = $"{char.ToUpper(aspectId[0])}{aspectId[1..]} ({group.Count()} items)"
            };
            headerLabel.AddThemeFontSizeOverride("font_size", 16);
            headerLabel.AddThemeColorOverride("font_color", aspectColor);
            header.AddChild(headerLabel);

            _contentContainer.AddChild(header);

            // Item cards in a flow container
            var flow = new HFlowContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            flow.AddThemeConstantOverride("h_separation", 16);
            flow.AddThemeConstantOverride("v_separation", 12);

            foreach (var def in group)
            {
                totalCount++;
                bool hasCustom = SpriteSwapper.HasCustomReagentSprite(def.Id);
                if (hasCustom) customCount++;

                var card = BuildReagentCard(def, hasCustom);
                flow.AddChild(card);
            }

            _contentContainer.AddChild(flow);
        }

        _statusLabel.Text = $"{totalCount} reagents, {customCount} custom sprites";
    }

    private VBoxContainer BuildReagentCard(ReagentDefinition def, bool hasCustom)
    {
        var card = new VBoxContainer();
        card.AddThemeConstantOverride("separation", 4);
        card.CustomMinimumSize = new Vector2(160, 0);

        // Name + source indicator
        var nameRow = new HBoxContainer();
        nameRow.AddThemeConstantOverride("separation", 4);

        var nameLabel = new Label { Text = def.Name };
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        nameLabel.AddThemeColorOverride("font_color", Colors.White);
        nameRow.AddChild(nameLabel);

        if (hasCustom)
        {
            var badge = new Label { Text = "[custom]" };
            badge.AddThemeFontSizeOverride("font_size", 10);
            badge.AddThemeColorOverride("font_color", new Color(0.4f, 0.9f, 0.4f));
            nameRow.AddChild(badge);
        }

        card.AddChild(nameRow);

        var idLabel = new Label { Text = def.Id };
        idLabel.AddThemeFontSizeOverride("font_size", 10);
        idLabel.AddThemeColorOverride("font_color", Colors.Gray);
        card.AddChild(idLabel);

        // Zoom previews
        var zoomRow = new HBoxContainer();
        zoomRow.AddThemeConstantOverride("separation", 8);

        var spec = SpriteSheetSpec.DefaultReagentSpec(32, 32);
        var spriteSheet = ReagentSpriteSheet.GetOrLoad(def.Id, spec);

        // 1x (32px), 2x (64px), 4x (128px)
        int[] zooms = { 1, 2, 4 };
        foreach (int zoom in zooms)
        {
            var zoomPanel = new VBoxContainer();
            zoomPanel.AddThemeConstantOverride("separation", 2);

            var zoomLabel = new Label { Text = $"{zoom}x" };
            zoomLabel.AddThemeFontSizeOverride("font_size", 9);
            zoomLabel.AddThemeColorOverride("font_color", Colors.DarkGray);
            zoomPanel.AddChild(zoomLabel);

            int pixelSize = 32 * zoom;

            if (spriteSheet != null)
            {
                var atlas = spriteSheet.GetAtlasTexture(VisualState.Normal);
                if (atlas != null)
                {
                    var rect = new TextureRect
                    {
                        Texture = atlas,
                        CustomMinimumSize = new Vector2(pixelSize, pixelSize),
                        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspect,
                        TextureFilter = CanvasItem.TextureFilterEnum.Nearest
                    };
                    zoomPanel.AddChild(rect);
                }
                else
                {
                    zoomPanel.AddChild(CreatePlaceholderRect(pixelSize, def));
                }
            }
            else
            {
                zoomPanel.AddChild(CreatePlaceholderRect(pixelSize, def));
            }

            zoomRow.AddChild(zoomPanel);
        }

        card.AddChild(zoomRow);

        // Sample polyomino (show shape at 2x with frame system)
        var shapeName = ReagentCatalog.GetShapeName(def);
        if (shapeName != null && shapeName != "monomino")
        {
            var polyLabel = new Label { Text = $"Shape: {shapeName}" };
            polyLabel.AddThemeFontSizeOverride("font_size", 10);
            polyLabel.AddThemeColorOverride("font_color", Colors.DarkGray);
            card.AddChild(polyLabel);

            var polyPanel = BuildPolyominoPreview(def, shapeName, spec);
            card.AddChild(polyPanel);
        }

        return card;
    }

    private Control BuildPolyominoPreview(ReagentDefinition def, string shapeName, SpriteSheetSpec spec)
    {
        var container = new Control();
        int cellSize = 24;
        int rows = def.ShapeRows;
        int cols = def.ShapeCols;
        container.CustomMinimumSize = new Vector2(cols * cellSize + 4, rows * cellSize + 4);

        var spriteSheet = ReagentSpriteSheet.GetOrLoad(def.Id, spec);
        var aspectColor = GridConstants.GetAspectColor(def.PrimaryAspectId ?? "unknown");

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!def.Shape[r, c]) continue;

                var pos = new Vector2(c * cellSize + 2, r * cellSize + 2);

                if (spriteSheet != null)
                {
                    var atlas = spriteSheet.GetAtlasTexture(VisualState.Normal);
                    if (atlas != null)
                    {
                        var rect = new TextureRect
                        {
                            Texture = atlas,
                            Position = pos,
                            Size = new Vector2(cellSize, cellSize),
                            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                            MouseFilter = MouseFilterEnum.Ignore
                        };
                        container.AddChild(rect);
                        continue;
                    }
                }

                // Fallback: colored rectangle
                var colorRect = new ColorRect
                {
                    Position = pos,
                    Size = new Vector2(cellSize - 1, cellSize - 1),
                    Color = aspectColor,
                    MouseFilter = MouseFilterEnum.Ignore
                };
                container.AddChild(colorRect);
            }
        }

        return container;
    }

    private ColorRect CreatePlaceholderRect(int pixelSize, ReagentDefinition def)
    {
        var aspectColor = GridConstants.GetAspectColor(def.PrimaryAspectId ?? "unknown");
        return new ColorRect
        {
            CustomMinimumSize = new Vector2(pixelSize, pixelSize),
            Color = aspectColor with { A = 0.6f }
        };
    }

    private void BuildBagBorderPreview()
    {
        var label = new Label
        {
            Text = "Bag border preview.\n\nDrop tileable border PNGs into art/sprites/custom/bags/\nto see them rendered here.",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", Colors.LightGray);
        _contentContainer.AddChild(label);

        // Show a sample 3x3 bag grid with current (solid color) borders
        var sampleLabel = new Label { Text = "Sample 3x3 Bag (current rendering):" };
        sampleLabel.AddThemeFontSizeOverride("font_size", 14);
        sampleLabel.AddThemeColorOverride("font_color", Colors.White);
        _contentContainer.AddChild(sampleLabel);

        var bagPanel = new Control { CustomMinimumSize = new Vector2(200, 200) };
        int cellSize = 48;
        var bagColor = new Color(0.25f, 0.22f, 0.20f);
        var borderColor = new Color(0.45f, 0.40f, 0.35f);

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                var pos = new Vector2(c * cellSize + 16, r * cellSize + 8);
                var cell = new ColorRect
                {
                    Position = pos,
                    Size = new Vector2(cellSize - 1, cellSize - 1),
                    Color = bagColor,
                    MouseFilter = MouseFilterEnum.Ignore
                };
                bagPanel.AddChild(cell);

                // Border on outer edges
                if (r == 0) AddBorderLine(bagPanel, pos, new Vector2(cellSize, 2), borderColor);
                if (r == 2) AddBorderLine(bagPanel, pos + new Vector2(0, cellSize - 2), new Vector2(cellSize, 2), borderColor);
                if (c == 0) AddBorderLine(bagPanel, pos, new Vector2(2, cellSize), borderColor);
                if (c == 2) AddBorderLine(bagPanel, pos + new Vector2(cellSize - 2, 0), new Vector2(2, cellSize), borderColor);
            }
        }

        _contentContainer.AddChild(bagPanel);
        _statusLabel.Text = "Bag border art: placeholder rectangles";
    }

    private static void AddBorderLine(Control parent, Vector2 pos, Vector2 size, Color color)
    {
        var line = new ColorRect
        {
            Position = pos,
            Size = size,
            Color = color,
            MouseFilter = MouseFilterEnum.Ignore
        };
        parent.AddChild(line);
    }

    private void BuildBackgroundPreview()
    {
        var label = new Label
        {
            Text = "Background art preview.\n\nDrop background PNGs into art/sprites/custom/backgrounds/\nto see them rendered here.",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", Colors.LightGray);
        _contentContainer.AddChild(label);

        _statusLabel.Text = "No background art loaded";
    }

    private void OnCategoryChanged(long index)
    {
        _category = (PreviewCategory)index;
        RefreshContent();
    }

    private void OnScreenshot()
    {
        var path = ScreenshotHelper.CaptureViewport(GetViewport(), "asset_preview.png");
        if (!string.IsNullOrEmpty(path))
            _statusLabel.Text = $"Screenshot saved: {path}";
    }

    private static Button CreateButton(string text, Color color)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(100, 34)
        };
        btn.AddThemeStyleboxOverride("normal", new StyleBoxFlat
        {
            BgColor = color,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3
        });
        return btn;
    }
}
