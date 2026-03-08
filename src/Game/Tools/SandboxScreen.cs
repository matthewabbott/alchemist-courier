using System.Collections.Generic;
using System.Linq;
using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Core.Visual;
using AlchemistCourier.Game.Inventory;
using AlchemistCourier.Game.Visual;

namespace AlchemistCourier.Game.Tools;

/// <summary>
/// Sandbox tool for viewing all catalog reagents with sprite previews and visual state toggling.
/// Catalog tab: scrollable grid showing all reagent sprites grouped by aspect, filterable.
/// Playground tab is deferred to SB.2.
/// </summary>
public partial class SandboxScreen : Control
{
    private const int PreviewCellSize = 32;
    private const int LargePreviewCellSize = 64;

    private static readonly Color BgColor = new(0.12f, 0.12f, 0.16f);
    private static readonly Color HeaderColor = new(0.9f, 0.85f, 0.7f);
    private static readonly Color SubHeaderColor = new(0.7f, 0.75f, 0.85f);
    private static readonly Color LabelColor = new(0.75f, 0.75f, 0.75f);
    private static readonly Color CardBgColor = new(0.18f, 0.18f, 0.22f);
    private static readonly Color ActiveTabColor = new(0.3f, 0.3f, 0.4f);
    private static readonly Color InactiveTabColor = new(0.18f, 0.18f, 0.22f);

    private VisualState _currentState = VisualState.Normal;
    private string? _aspectFilter;
    private string? _shapeFilter;

    private VBoxContainer _catalogContent = null!;
    private Label _countLabel = null!;
    private HBoxContainer _stateButtons = null!;
    private OptionButton _aspectDropdown = null!;
    private OptionButton _shapeDropdown = null!;

    // Tracked for visual state updates
    private readonly List<(SpriteStateRenderer renderer, ReagentSpriteSheet sheet)> _spriteRenderers = new();
    private readonly List<(ColorRect rect, string aspectId)> _fallbackCells = new();

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect { Color = BgColor, LayoutMode = 1 };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var root = new VBoxContainer { LayoutMode = 2 };
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 0);
        AddChild(root);

        // Header bar
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

        // Tab bar (Catalog | Playground)
        var tabCatalog = CreateTabButton("Catalog", true);
        var tabPlayground = CreateTabButton("Playground", false);
        tabPlayground.Disabled = true;
        tabPlayground.TooltipText = "Coming soon (SB.2)";
        headerBar.AddChild(tabCatalog);
        headerBar.AddChild(tabPlayground);

        // Spacer
        var spacer = new Control { LayoutMode = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        headerBar.AddChild(spacer);

        _countLabel = new Label { Text = "0 items", LayoutMode = 2 };
        _countLabel.AddThemeFontSizeOverride("font_size", 14);
        _countLabel.AddThemeColorOverride("font_color", LabelColor);
        headerBar.AddChild(_countLabel);

        headerMargin.AddChild(headerBar);
        root.AddChild(headerMargin);

        // Filter bar
        var filterMargin = new MarginContainer { LayoutMode = 2 };
        filterMargin.AddThemeConstantOverride("margin_left", 16);
        filterMargin.AddThemeConstantOverride("margin_right", 16);
        filterMargin.AddThemeConstantOverride("margin_bottom", 6);

        var filterBar = new HBoxContainer { LayoutMode = 2 };
        filterBar.AddThemeConstantOverride("separation", 12);

        // Aspect filter
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

        // Shape filter
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

        // Visual state toggle
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
        root.AddChild(filterMargin);

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
        root.AddChild(contentMargin);

        BuildCatalog();
    }

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

        // Update toggle states
        foreach (var child in _stateButtons.GetChildren())
        {
            if (child is Button btn)
                btn.ButtonPressed = btn == pressed;
        }

        // Update all renderers
        foreach (var (renderer, _) in _spriteRenderers)
            renderer.SetState(state);

        // Update fallback cells
        foreach (var (rect, aspectId) in _fallbackCells)
            rect.Color = GetFallbackColor(aspectId, state);
    }

    private void RebuildCatalog()
    {
        // Remove all children
        foreach (var child in _catalogContent.GetChildren())
            child.QueueFree();

        _spriteRenderers.Clear();
        _fallbackCells.Clear();

        BuildCatalog();
    }

    private void BuildCatalog()
    {
        var allItems = ReagentCatalog.GetAll();

        // Apply filters
        var filtered = allItems.Where(d =>
        {
            if (_aspectFilter != null)
            {
                var primaryAspect = d.AspectsPerCell.Count > 0
                    ? d.AspectsPerCell[0].Aspects.FirstOrDefault()?.Id
                    : null;
                if (primaryAspect != _aspectFilter) return false;
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

        // Group by aspect
        var groups = filtered
            .GroupBy(d => d.AspectsPerCell.Count > 0
                ? d.AspectsPerCell[0].Aspects.FirstOrDefault()?.Id ?? "unknown"
                : "unknown")
            .OrderBy(g => GetAspectSortOrder(g.Key));

        foreach (var group in groups)
        {
            AddAspectGroup(group.Key, group.ToList());
        }
    }

    private void AddAspectGroup(string aspectId, List<ReagentDefinition> items)
    {
        // Aspect header with colored icon
        var headerBox = new HBoxContainer { LayoutMode = 2 };
        headerBox.AddThemeConstantOverride("separation", 8);

        var icon = new ColorRect
        {
            Color = GridConstants.GetAspectColor(aspectId),
            CustomMinimumSize = new Vector2(16, 16),
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

        // Item cards in a flow container
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
        // Card background
        var panel = new PanelContainer { LayoutMode = 2 };
        var style = new StyleBoxFlat
        {
            BgColor = CardBgColor,
        };
        style.SetCornerRadiusAll(4);
        style.SetContentMarginAll(8);
        panel.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer { LayoutMode = 2 };
        vbox.AddThemeConstantOverride("separation", 4);

        // Reagent name
        var nameLabel = new Label
        {
            Text = def.Name,
            LayoutMode = 2,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        nameLabel.AddThemeColorOverride("font_color", Colors.White);
        vbox.AddChild(nameLabel);

        // 1x sprite preview (or colored rectangles fallback)
        var previewContainer = CreateSpritePreview(def, PreviewCellSize);
        vbox.AddChild(previewContainer);

        // 2x sprite preview
        var largePreview = CreateSpritePreview(def, LargePreviewCellSize);
        vbox.AddChild(largePreview);

        // Shape name + ID
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

        // Cell count + rarity
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
        var sheet = ReagentSpriteSheet.GetOrLoad(def.Id, spec);

        if (sheet != null)
        {
            var renderer = new SpriteStateRenderer();
            renderer.Initialize(sheet, cellSize, cellSize);
            renderer.SetState(_currentState);
            _spriteRenderers.Add((renderer, sheet));
            return renderer;
        }

        // Fallback: colored rectangles for the shape
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
            CustomMinimumSize = new Vector2(cols * cellPx, rows * cellPx)
        };

        string primaryAspect = def.AspectsPerCell.Count > 0
            ? def.AspectsPerCell[0].Aspects.FirstOrDefault()?.Id ?? ""
            : "";

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!def.Shape[r, c]) continue;

                var cellRect = new ColorRect
                {
                    Color = GetFallbackColor(primaryAspect, _currentState),
                    Position = new Vector2(c * cellPx, r * cellPx),
                    Size = new Vector2(cellPx - 1, cellPx - 1)
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
