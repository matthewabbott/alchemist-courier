using System;
using System.Collections.Generic;
using Godot;
using AlchemistCourier.Game.Testing;
using AlchemistCourier.Game.Visual;

namespace AlchemistCourier.Game.Tools;

/// <summary>
/// Renders individual UI components in isolation for sprite verification.
/// Auto-cycles through all component types on launch, taking screenshots.
/// Supports toggling between sprite textures and procedural fallback.
/// </summary>
public partial class ComponentInspector : Control
{
    private enum ComponentType
    {
        HeaderPanel,
        Button,
        InfoPanel,
        AspectEntry,
        StatusBar,
        DecorationCorner,
        DecorationSeparator,
        DecorationSigil,
        BagFrame,
    }

    private static readonly string[] ComponentNames =
    {
        "HeaderPanel", "Button", "InfoPanel", "AspectEntry", "StatusBar",
        "DecorationCorner", "DecorationSeparator", "DecorationSigil", "BagFrame"
    };

    private ComponentType _selected = ComponentType.HeaderPanel;
    private bool _useSprites = true;
    private OptionButton _dropdown = null!;
    private CheckButton _spriteToggle = null!;
    private Control _previewArea = null!;
    private Label _statusLabel = null!;
    private ColorRect _background = null!;

    private bool _autoCycleRunning;
    private int _autoCycleIndex;
    private int _autoCycleFrameWait;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        _background = new ColorRect { Color = new Color(0.15f, 0.14f, 0.16f) };
        _background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_background);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        AddChild(margin);

        var root = new VBoxContainer();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 12);
        margin.AddChild(root);

        BuildToolbar(root);

        _previewArea = new CenterContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        root.AddChild(_previewArea);

        RefreshPreview();

        // Start auto-cycle on next frame
        _autoCycleRunning = true;
        _autoCycleIndex = 0;
        _autoCycleFrameWait = 3; // wait frames before first screenshot
    }

    public override void _Process(double delta)
    {
        if (!_autoCycleRunning) return;

        if (_autoCycleFrameWait > 0)
        {
            _autoCycleFrameWait--;
            return;
        }

        if (_autoCycleIndex < ComponentNames.Length)
        {
            _selected = (ComponentType)_autoCycleIndex;
            _dropdown.Selected = _autoCycleIndex;
            UIThemeLoader.ClearCache();
            RefreshPreview();

            var name = ComponentNames[_autoCycleIndex];
            GD.Print($"[COMPONENT] {name}");

            bool spriteLoaded = CheckSpriteLoaded(_selected);
            GD.Print($"[SPRITE_LOADED] {spriteLoaded.ToString().ToLower()}");

            // Take screenshot on next frame (after render)
            _autoCycleFrameWait = 2;
            CallDeferred(MethodName.TakeAutoCycleScreenshot, name);

            _autoCycleIndex++;
        }
        else
        {
            _autoCycleRunning = false;
            GD.Print("[INSPECTOR_DONE]");
        }
    }

    private void TakeAutoCycleScreenshot(string name)
    {
        var path = ScreenshotHelper.CaptureViewport(GetViewport(), $"component_{name}.png");
        if (!string.IsNullOrEmpty(path))
            _statusLabel.Text = $"Screenshot: {name}";
    }

    private void BuildToolbar(VBoxContainer root)
    {
        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", 12);

        var title = new Label { Text = "Component Inspector" };
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        toolbar.AddChild(title);

        _dropdown = new OptionButton();
        for (int i = 0; i < ComponentNames.Length; i++)
            _dropdown.AddItem(ComponentNames[i], i);
        _dropdown.Selected = 0;
        _dropdown.ItemSelected += OnComponentChanged;
        toolbar.AddChild(_dropdown);

        _spriteToggle = new CheckButton { Text = "Use Sprites", ButtonPressed = true };
        _spriteToggle.Toggled += OnSpriteToggled;
        toolbar.AddChild(_spriteToggle);

        var screenshotBtn = new Button
        {
            Text = "Screenshot",
            CustomMinimumSize = new Vector2(100, 34)
        };
        screenshotBtn.AddThemeStyleboxOverride("normal", new StyleBoxFlat
        {
            BgColor = new Color(0.3f, 0.5f, 0.3f),
            ContentMarginLeft = 8, ContentMarginRight = 8,
            ContentMarginTop = 4, ContentMarginBottom = 4,
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3
        });
        screenshotBtn.Pressed += OnScreenshot;
        toolbar.AddChild(screenshotBtn);

        _statusLabel = new Label { Text = "" };
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
        _statusLabel.AddThemeColorOverride("font_color", Colors.LightGray);
        _statusLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Right;
        toolbar.AddChild(_statusLabel);

        root.AddChild(toolbar);
        root.AddChild(new HSeparator());
    }

    private void RefreshPreview()
    {
        foreach (var child in _previewArea.GetChildren())
            if (child is Node n) n.QueueFree();

        var component = BuildComponent(_selected);
        _previewArea.AddChild(component);
    }

    private Control BuildComponent(ComponentType type)
    {
        return type switch
        {
            ComponentType.HeaderPanel => BuildHeaderPanel(),
            ComponentType.Button => BuildButtonPreview(),
            ComponentType.InfoPanel => BuildInfoPanel(),
            ComponentType.AspectEntry => BuildAspectEntry(),
            ComponentType.StatusBar => BuildStatusBar(),
            ComponentType.DecorationCorner => BuildDecoration("corner_flourish", "Corner Flourish"),
            ComponentType.DecorationSeparator => BuildDecoration("separator", "Separator"),
            ComponentType.DecorationSigil => BuildDecoration("title_sigil", "Title Sigil"),
            ComponentType.BagFrame => BuildBagFrame(),
            _ => new Label { Text = "Unknown component" }
        };
    }

    private Control BuildHeaderPanel()
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 8);
        container.CustomMinimumSize = new Vector2(400, 0);

        AddSectionLabel(container, "Header Panel");

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(400, 60) };
        if (_useSprites)
        {
            panel.AddThemeStyleboxOverride("panel", UIThemeLoader.GetPanelStyleBox("parchment_bordered"));
        }
        else
        {
            panel.AddThemeStyleboxOverride("panel", MakeFallbackStyleBox(new Color(0.12f, 0.1f, 0.08f)));
        }

        var label = new Label
        {
            Text = "ALCHEMIST COURIER",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        panel.AddChild(label);
        container.AddChild(panel);

        // Also show dark_leather variant
        var panel2 = new PanelContainer { CustomMinimumSize = new Vector2(400, 60) };
        if (_useSprites)
        {
            panel2.AddThemeStyleboxOverride("panel", UIThemeLoader.GetPanelStyleBox("dark_leather"));
        }
        else
        {
            panel2.AddThemeStyleboxOverride("panel", MakeFallbackStyleBox(new Color(0.15f, 0.12f, 0.1f)));
        }

        var label2 = new Label
        {
            Text = "Dark Leather Variant",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        label2.AddThemeFontSizeOverride("font_size", 14);
        label2.AddThemeColorOverride("font_color", Colors.LightGray);
        panel2.AddChild(label2);
        container.AddChild(panel2);

        return container;
    }

    private Control BuildButtonPreview()
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 12);
        container.CustomMinimumSize = new Vector2(400, 0);

        AddSectionLabel(container, "Buttons");

        // Standard buttons
        var row1 = new HBoxContainer();
        row1.AddThemeConstantOverride("separation", 12);

        string[] states = { "normal", "hover", "pressed" };
        foreach (var state in states)
        {
            var btn = new Button
            {
                Text = $"Button ({state})",
                CustomMinimumSize = new Vector2(120, 40)
            };
            if (_useSprites)
            {
                btn.AddThemeStyleboxOverride("normal", UIThemeLoader.GetButtonStyleBox(state));
            }
            else
            {
                btn.AddThemeStyleboxOverride("normal", MakeFallbackStyleBox(state switch
                {
                    "hover" => new Color(0.35f, 0.3f, 0.25f),
                    "pressed" => new Color(0.2f, 0.17f, 0.12f),
                    _ => new Color(0.28f, 0.24f, 0.18f)
                }));
            }
            row1.AddChild(btn);
        }
        container.AddChild(row1);

        // Alt buttons
        var altLabel = new Label { Text = "Alt variants:" };
        altLabel.AddThemeFontSizeOverride("font_size", 12);
        altLabel.AddThemeColorOverride("font_color", Colors.Gray);
        container.AddChild(altLabel);

        var row2 = new HBoxContainer();
        row2.AddThemeConstantOverride("separation", 12);
        string[] altStates = { "alt_normal", "alt_hover", "alt_pressed" };
        foreach (var state in altStates)
        {
            var btn = new Button
            {
                Text = state.Replace("alt_", "Alt "),
                CustomMinimumSize = new Vector2(120, 40)
            };
            if (_useSprites)
            {
                btn.AddThemeStyleboxOverride("normal", UIThemeLoader.GetButtonStyleBox(state));
            }
            else
            {
                btn.AddThemeStyleboxOverride("normal", MakeFallbackStyleBox(new Color(0.25f, 0.22f, 0.18f)));
            }
            row2.AddChild(btn);
        }
        container.AddChild(row2);

        return container;
    }

    private Control BuildInfoPanel()
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 8);
        container.CustomMinimumSize = new Vector2(400, 0);

        AddSectionLabel(container, "Info Panel (parchment_plain)");

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(400, 100) };
        if (_useSprites)
        {
            panel.AddThemeStyleboxOverride("panel", UIThemeLoader.GetPanelStyleBox("parchment_plain"));
        }
        else
        {
            panel.AddThemeStyleboxOverride("panel", MakeFallbackStyleBox(new Color(0.12f, 0.1f, 0.08f)));
        }

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);

        var text = new Label
        {
            Text = "Place all reagents without\ntriggering reactions.\n\nThis is sample info panel content.",
            AutowrapMode = TextServer.AutowrapMode.Word,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        text.AddThemeFontSizeOverride("font_size", 13);
        text.AddThemeColorOverride("font_color", new Color(0.85f, 0.8f, 0.7f));
        margin.AddChild(text);
        panel.AddChild(margin);
        container.AddChild(panel);

        return container;
    }

    private Control BuildAspectEntry()
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 8);
        container.CustomMinimumSize = new Vector2(400, 0);

        AddSectionLabel(container, "Aspect Tiles (32x32)");

        string[] aspects = { "hot", "cold", "volatile", "toxic", "fragile", "sharp", "heavy", "stable" };

        var flow = new HFlowContainer();
        flow.AddThemeConstantOverride("h_separation", 16);
        flow.AddThemeConstantOverride("v_separation", 12);

        foreach (var aspect in aspects)
        {
            var entry = new VBoxContainer();
            entry.AddThemeConstantOverride("separation", 4);

            var aspectColor = Inventory.GridConstants.GetAspectColor(aspect);

            if (_useSprites)
            {
                entry.AddChild(UIThemeLoader.CreateAspectIcon(aspect, 48));
            }
            else
            {
                entry.AddChild(MakeColorSwatch(48, aspectColor));
            }

            var nameLabel = new Label { Text = aspect };
            nameLabel.AddThemeFontSizeOverride("font_size", 11);
            nameLabel.AddThemeColorOverride("font_color", aspectColor);
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            entry.AddChild(nameLabel);

            flow.AddChild(entry);
        }

        container.AddChild(flow);
        return container;
    }

    private Control BuildStatusBar()
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 8);
        container.CustomMinimumSize = new Vector2(400, 0);

        AddSectionLabel(container, "Status Bar (sample values)");

        // Build a mock status bar showing aspect accumulation bars
        var barContainer = new HBoxContainer();
        barContainer.AddThemeConstantOverride("separation", 16);

        (string name, float current, float max, Color color)[] bars =
        {
            ("Heat", 2, 4, new Color(0.9f, 0.25f, 0.15f)),
            ("Cold", 1, 4, new Color(0.2f, 0.45f, 0.9f)),
            ("Toxicity", 3, 3, new Color(0.2f, 0.75f, 0.25f)),
        };

        foreach (var (name, current, max, color) in bars)
        {
            var bar = new VBoxContainer();
            bar.AddThemeConstantOverride("separation", 2);

            var label = new Label { Text = name };
            label.AddThemeFontSizeOverride("font_size", 12);
            label.AddThemeColorOverride("font_color", color);
            bar.AddChild(label);

            var barBg = new Control { CustomMinimumSize = new Vector2(100, 14) };

            var bg = new ColorRect
            {
                Size = new Vector2(100, 14),
                Color = new Color(0.1f, 0.1f, 0.1f)
            };
            barBg.AddChild(bg);

            float ratio = current / max;
            Color fillColor = ratio >= 1.0f ? new Color(0.9f, 0.2f, 0.15f) :
                              ratio >= 0.75f ? new Color(0.9f, 0.8f, 0.2f) : color;
            var fill = new ColorRect
            {
                Size = new Vector2(100 * ratio, 14),
                Color = fillColor
            };
            barBg.AddChild(fill);

            bar.AddChild(barBg);

            var valLabel = new Label { Text = $"{current}/{max}" };
            valLabel.AddThemeFontSizeOverride("font_size", 10);
            valLabel.AddThemeColorOverride("font_color", Colors.LightGray);
            bar.AddChild(valLabel);

            barContainer.AddChild(bar);
        }

        container.AddChild(barContainer);
        return container;
    }

    private Control BuildDecoration(string assetName, string displayName)
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 8);

        AddSectionLabel(container, displayName);

        if (_useSprites)
        {
            var tex = UIThemeLoader.GetDecorationTexture(assetName);
            if (tex != null)
            {
                var rect = new TextureRect
                {
                    Texture = tex,
                    CustomMinimumSize = new Vector2(128, 128),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspect,
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest
                };
                container.AddChild(rect);
            }
            else
            {
                container.AddChild(MakePlaceholder($"Missing: {assetName}.png"));
            }
        }
        else
        {
            container.AddChild(MakePlaceholder($"Procedural fallback ({displayName})"));
        }

        return container;
    }

    private Control BuildBagFrame()
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 12);
        container.CustomMinimumSize = new Vector2(500, 0);

        AddSectionLabel(container, "Bag Frames (NinePatchRect)");

        string[] bagTypes = { "default", "ornate", "warm", "brass" };

        var flow = new HFlowContainer();
        flow.AddThemeConstantOverride("h_separation", 16);
        flow.AddThemeConstantOverride("v_separation", 12);

        foreach (var bagType in bagTypes)
        {
            var entry = new VBoxContainer();
            entry.AddThemeConstantOverride("separation", 4);

            if (_useSprites)
            {
                var tex = UIThemeLoader.GetBagThemeTexture(bagType);
                if (tex != null)
                {
                    var ninePatch = new NinePatchRect
                    {
                        Texture = tex,
                        CustomMinimumSize = new Vector2(120, 120),
                        PatchMarginLeft = 8,
                        PatchMarginTop = 8,
                        PatchMarginRight = 8,
                        PatchMarginBottom = 8,
                        TextureFilter = CanvasItem.TextureFilterEnum.Nearest
                    };
                    entry.AddChild(ninePatch);
                }
                else
                {
                    entry.AddChild(MakeColorSwatch(120, new Color(0.25f, 0.22f, 0.2f)));
                }
            }
            else
            {
                var rect = new ColorRect
                {
                    CustomMinimumSize = new Vector2(120, 120),
                    Color = new Color(0.25f, 0.22f, 0.2f)
                };
                entry.AddChild(rect);
            }

            var label = new Label { Text = bagType };
            label.AddThemeFontSizeOverride("font_size", 11);
            label.AddThemeColorOverride("font_color", Colors.LightGray);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            entry.AddChild(label);

            flow.AddChild(entry);
        }

        container.AddChild(flow);
        return container;
    }

    private bool CheckSpriteLoaded(ComponentType type)
    {
        return type switch
        {
            ComponentType.HeaderPanel => ResourceLoader.Exists("res://art/sprites/ui/panels/parchment_bordered.png"),
            ComponentType.Button => ResourceLoader.Exists("res://art/sprites/ui/sliced/button_normal.png"),
            ComponentType.InfoPanel => ResourceLoader.Exists("res://art/sprites/ui/panels/parchment_plain.png"),
            ComponentType.AspectEntry => ResourceLoader.Exists("res://art/sprites/ui/tiles/hot.png"),
            ComponentType.StatusBar => false, // Procedural only — no dedicated sprite
            ComponentType.DecorationCorner => ResourceLoader.Exists("res://art/sprites/ui/decorations/corner_flourish.png"),
            ComponentType.DecorationSeparator => ResourceLoader.Exists("res://art/sprites/ui/decorations/separator.png"),
            ComponentType.DecorationSigil => ResourceLoader.Exists("res://art/sprites/ui/decorations/title_sigil.png"),
            ComponentType.BagFrame => ResourceLoader.Exists("res://art/sprites/ui/sliced/bag_default.png"),
            _ => false
        };
    }

    private void OnComponentChanged(long index)
    {
        _selected = (ComponentType)index;
        UIThemeLoader.ClearCache();
        RefreshPreview();
    }

    private void OnSpriteToggled(bool pressed)
    {
        _useSprites = pressed;
        UIThemeLoader.ClearCache();
        RefreshPreview();
    }

    private void OnScreenshot()
    {
        var name = ComponentNames[(int)_selected];
        var path = ScreenshotHelper.CaptureViewport(GetViewport(), $"component_{name}.png");
        if (!string.IsNullOrEmpty(path))
            _statusLabel.Text = $"Screenshot saved: {path}";
    }

    private static void AddSectionLabel(VBoxContainer parent, string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        parent.AddChild(label);
    }

    private static StyleBoxFlat MakeFallbackStyleBox(Color bgColor)
    {
        var sb = new StyleBoxFlat();
        sb.BgColor = bgColor;
        sb.SetCornerRadiusAll(2);
        return sb;
    }

    private static ColorRect MakeColorSwatch(int size, Color color)
    {
        return new ColorRect
        {
            CustomMinimumSize = new Vector2(size, size),
            Color = color
        };
    }

    private static Control MakePlaceholder(string text)
    {
        var box = new PanelContainer { CustomMinimumSize = new Vector2(200, 100) };
        box.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.18f, 0.16f),
            BorderColor = new Color(0.4f, 0.35f, 0.3f),
            BorderWidthBottom = 1, BorderWidthTop = 1,
            BorderWidthLeft = 1, BorderWidthRight = 1
        });

        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", Colors.DarkGray);
        box.AddChild(label);

        return box;
    }
}
