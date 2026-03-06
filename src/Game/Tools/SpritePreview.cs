using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

namespace AlchemistCourier.Game.Tools;

/// <summary>
/// Development tool for previewing all game sprites in art/sprites/.
/// Displays sprites in a scrollable grid at 1x and 2x zoom, grouped by subdirectory,
/// with toggleable background colors for transparency testing.
/// </summary>
public partial class SpritePreview : Control
{
    private enum BackgroundMode { DarkGray, White, Checkerboard }

    private static readonly Color DarkGrayBg = new(0.2f, 0.2f, 0.2f);
    private static readonly Color WhiteBg = new(1f, 1f, 1f);
    private static readonly Color CheckerLight = new(0.85f, 0.85f, 0.85f);
    private static readonly Color CheckerDark = new(0.65f, 0.65f, 0.65f);

    private BackgroundMode _bgMode = BackgroundMode.DarkGray;
    private ColorRect _background = null!;
    private VBoxContainer _contentContainer = null!;
    private Label _totalCountLabel = null!;
    private Button _bgToggleButton = null!;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        // Background fill
        _background = new ColorRect
        {
            Color = DarkGrayBg,
            LayoutMode = 1
        };
        _background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_background);

        // Top bar with title and controls
        var topBar = new HBoxContainer { LayoutMode = 2 };
        topBar.AddThemeConstantOverride("separation", 16);

        var title = new Label
        {
            Text = "Sprite Preview Tool",
            LayoutMode = 2
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeColorOverride("font_color", Colors.White);
        topBar.AddChild(title);

        _bgToggleButton = new Button
        {
            Text = "BG: Dark Gray",
            LayoutMode = 2
        };
        _bgToggleButton.Pressed += OnBgToggle;
        topBar.AddChild(_bgToggleButton);

        _totalCountLabel = new Label
        {
            Text = "Total: 0 sprites",
            LayoutMode = 2
        };
        _totalCountLabel.AddThemeColorOverride("font_color", Colors.White);
        topBar.AddChild(_totalCountLabel);

        // Scrollable content area
        var scrollContainer = new ScrollContainer
        {
            LayoutMode = 2,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };

        _contentContainer = new VBoxContainer
        {
            LayoutMode = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _contentContainer.AddThemeConstantOverride("separation", 16);
        scrollContainer.AddChild(_contentContainer);

        // Root layout
        var root = new VBoxContainer { LayoutMode = 2 };
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 8);

        var topMargin = new MarginContainer { LayoutMode = 2 };
        topMargin.AddThemeConstantOverride("margin_left", 16);
        topMargin.AddThemeConstantOverride("margin_right", 16);
        topMargin.AddThemeConstantOverride("margin_top", 12);
        topMargin.AddChild(topBar);
        root.AddChild(topMargin);

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

        AddChild(root);

        LoadSprites();
    }

    private void LoadSprites()
    {
        var spritesDir = ProjectSettings.GlobalizePath("res://art/sprites");

        if (!Directory.Exists(spritesDir))
        {
            ShowPlaceholder("No art/sprites/ directory found.\nPlace .png files in art/sprites/ to preview them here.");
            return;
        }

        var pngFiles = Directory.GetFiles(spritesDir, "*.png", SearchOption.AllDirectories);
        if (pngFiles.Length == 0)
        {
            ShowPlaceholder("art/sprites/ directory is empty.\nPlace .png files in art/sprites/ to preview them here.");
            return;
        }

        // Group by subdirectory relative to art/sprites/
        var groups = new SortedDictionary<string, List<string>>();
        foreach (var file in pngFiles)
        {
            var relativePath = Path.GetRelativePath(spritesDir, file).Replace('\\', '/');
            var dir = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? "";
            var groupName = string.IsNullOrEmpty(dir) ? "(root)" : dir;

            if (!groups.ContainsKey(groupName))
                groups[groupName] = new List<string>();
            groups[groupName].Add(file);
        }

        int totalCount = 0;
        foreach (var (groupName, files) in groups)
        {
            files.Sort();
            totalCount += files.Count;
            AddSpriteGroup(groupName, files, spritesDir);
        }

        _totalCountLabel.Text = $"Total: {totalCount} sprite{(totalCount != 1 ? "s" : "")}";
    }

    private void ShowPlaceholder(string message)
    {
        var label = new Label
        {
            Text = message,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            LayoutMode = 2,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_color", Colors.LightGray);
        _contentContainer.AddChild(label);
    }

    private void AddSpriteGroup(string groupName, List<string> files, string spritesDir)
    {
        // Group header
        var header = new Label
        {
            Text = $"— {groupName} ({files.Count}) —",
            LayoutMode = 2
        };
        header.AddThemeFontSizeOverride("font_size", 18);
        header.AddThemeColorOverride("font_color", new Color(0.8f, 0.9f, 1f));
        _contentContainer.AddChild(header);

        // Sprite grid using FlowContainer for wrapping
        var flow = new HFlowContainer
        {
            LayoutMode = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        flow.AddThemeConstantOverride("h_separation", 12);
        flow.AddThemeConstantOverride("v_separation", 12);

        foreach (var filePath in files)
        {
            var relativePath = Path.GetRelativePath(spritesDir, filePath).Replace('\\', '/');
            var resPath = $"res://art/sprites/{relativePath}";
            var texture = GD.Load<Texture2D>(resPath);
            if (texture == null) continue;

            var card = CreateSpriteCard(texture, Path.GetFileName(filePath));
            flow.AddChild(card);
        }

        _contentContainer.AddChild(flow);
    }

    private VBoxContainer CreateSpriteCard(Texture2D texture, string filename)
    {
        var card = new VBoxContainer { LayoutMode = 2 };
        card.AddThemeConstantOverride("separation", 4);

        // 1x preview
        var label1x = new Label { Text = "1x", LayoutMode = 2 };
        label1x.AddThemeFontSizeOverride("font_size", 11);
        label1x.AddThemeColorOverride("font_color", Colors.Gray);
        card.AddChild(label1x);

        var sprite1x = new TextureRect
        {
            Texture = texture,
            CustomMinimumSize = new Godot.Vector2(32, 32),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspect,
            LayoutMode = 2
        };
        card.AddChild(sprite1x);

        // 2x preview
        var label2x = new Label { Text = "2x", LayoutMode = 2 };
        label2x.AddThemeFontSizeOverride("font_size", 11);
        label2x.AddThemeColorOverride("font_color", Colors.Gray);
        card.AddChild(label2x);

        var sprite2x = new TextureRect
        {
            Texture = texture,
            CustomMinimumSize = new Godot.Vector2(64, 64),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspect,
            LayoutMode = 2
        };
        card.AddChild(sprite2x);

        // Filename label
        var nameLabel = new Label
        {
            Text = filename,
            LayoutMode = 2,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 11);
        nameLabel.AddThemeColorOverride("font_color", Colors.LightGray);
        card.AddChild(nameLabel);

        return card;
    }

    private void OnBgToggle()
    {
        _bgMode = _bgMode switch
        {
            BackgroundMode.DarkGray => BackgroundMode.White,
            BackgroundMode.White => BackgroundMode.Checkerboard,
            BackgroundMode.Checkerboard => BackgroundMode.DarkGray,
            _ => BackgroundMode.DarkGray
        };

        switch (_bgMode)
        {
            case BackgroundMode.DarkGray:
                _background.Color = DarkGrayBg;
                _bgToggleButton.Text = "BG: Dark Gray";
                UpdateLabelColors(Colors.White, Colors.LightGray);
                break;
            case BackgroundMode.White:
                _background.Color = WhiteBg;
                _bgToggleButton.Text = "BG: White";
                UpdateLabelColors(Colors.Black, Colors.DarkGray);
                break;
            case BackgroundMode.Checkerboard:
                _background.Color = CheckerLight;
                _bgToggleButton.Text = "BG: Checker";
                UpdateLabelColors(Colors.Black, Colors.DarkGray);
                QueueRedraw();
                break;
        }
    }

    private void UpdateLabelColors(Color primaryColor, Color secondaryColor)
    {
        // Update all label colors in the tree to match background
        foreach (var node in GetAllDescendants(this))
        {
            if (node is Label label)
            {
                var fontSize = label.GetThemeFontSize("font_size");
                // Headers use larger font, secondary labels use smaller
                if (fontSize >= 18)
                    label.AddThemeColorOverride("font_color", primaryColor);
                else
                    label.AddThemeColorOverride("font_color", secondaryColor);
            }
        }
    }

    public override void _Draw()
    {
        if (_bgMode != BackgroundMode.Checkerboard) return;

        // Draw checkerboard pattern on top of background
        int checkerSize = 8;
        var size = GetRect().Size;
        for (int y = 0; y < size.Y; y += checkerSize)
        {
            for (int x = 0; x < size.X; x += checkerSize)
            {
                bool isDark = ((x / checkerSize) + (y / checkerSize)) % 2 == 1;
                if (isDark)
                {
                    DrawRect(new Rect2(x, y, checkerSize, checkerSize), CheckerDark);
                }
            }
        }
    }

    private static IEnumerable<Node> GetAllDescendants(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            yield return child;
            foreach (var descendant in GetAllDescendants(child))
                yield return descendant;
        }
    }
}
