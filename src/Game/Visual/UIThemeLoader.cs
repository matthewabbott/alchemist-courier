using Godot;
using System.Collections.Generic;
using AlchemistCourier.Game.Inventory;

namespace AlchemistCourier.Game.Visual;

/// <summary>
/// Loads UI textures from art/sprites/ui/ with fallback to procedural StyleBoxFlat
/// when sprite files are missing. Caches loaded resources to avoid reloading.
///
/// Density conventions (mixel avoidance):
/// - Sliced Kit 01 elements (buttons, panels, bags) are 1x density.
/// - Tileable panel textures and decorations are 2x density.
/// - Do NOT mix scale factors within a single panel or component.
/// </summary>
public static class UIThemeLoader
{
    private const string PanelPath = "res://art/sprites/ui/panels/";
    private const string TilePath = "res://art/sprites/ui/tiles/";
    private const string DecorationPath = "res://art/sprites/ui/decorations/";
    private const string SlicedPath = "res://art/sprites/ui/sliced/";

    private const int PanelMargin = 16;
    private const int ButtonMargin = 4;
    private const int ButtonContentMarginH = 12;
    private const int ButtonContentMarginV = 4;

    private static readonly Dictionary<string, StyleBox> _styleBoxCache = new();
    private static readonly Dictionary<string, Texture2D?> _textureCache = new();

    /// <summary>
    /// Returns StyleBoxTexture with 9-patch margins from panels/{name}.png,
    /// or StyleBoxFlat with dark background if not found.
    /// Panel textures are 2x density.
    /// </summary>
    public static StyleBox GetPanelStyleBox(string name)
    {
        string cacheKey = $"panel:{name}";
        if (_styleBoxCache.TryGetValue(cacheKey, out var cached))
            return cached;

        string path = $"{PanelPath}{name}.png";
        if (ResourceLoader.Exists(path))
        {
            var tex = GD.Load<Texture2D>(path);
            if (tex != null)
            {
                var styleBox = new StyleBoxTexture();
                styleBox.Texture = tex;
                styleBox.TextureMarginLeft = PanelMargin;
                styleBox.TextureMarginTop = PanelMargin;
                styleBox.TextureMarginRight = PanelMargin;
                styleBox.TextureMarginBottom = PanelMargin;
                styleBox.AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Tile;
                styleBox.AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Tile;
                _styleBoxCache[cacheKey] = styleBox;
                return styleBox;
            }
        }

        GD.PushWarning($"UIThemeLoader: Panel texture not found: {path}");
        var fallback = new StyleBoxFlat();
        fallback.BgColor = new Color(0.12f, 0.1f, 0.08f);
        fallback.SetCornerRadiusAll(2);
        _styleBoxCache[cacheKey] = fallback;
        return fallback;
    }

    /// <summary>
    /// Returns raw Texture2D from panels/{name}.png or null if not found.
    /// Useful for tiling backgrounds where a StyleBox is not needed.
    /// </summary>
    public static Texture2D? GetPanelTexture(string name)
        => LoadTexture("panel", PanelPath, name);

    /// <summary>
    /// Returns Texture2D from tiles/{aspectName}.png or null if not found.
    /// Aspect tiles are 32x32 at base resolution.
    /// </summary>
    public static Texture2D? GetAspectTileTexture(string aspectName)
        => LoadTexture("tile", TilePath, aspectName);

    /// <summary>
    /// Returns Texture2D from decorations/{name}.png or null if not found.
    /// Decoration textures are 2x density.
    /// </summary>
    public static Texture2D? GetDecorationTexture(string name)
        => LoadTexture("decoration", DecorationPath, name);

    /// <summary>
    /// Returns StyleBoxTexture from sliced/button_{state}.png with 9-patch margins,
    /// or StyleBoxFlat fallback. Button textures are 1x density.
    /// </summary>
    public static StyleBox GetButtonStyleBox(string state)
    {
        string cacheKey = $"button:{state}";
        if (_styleBoxCache.TryGetValue(cacheKey, out var cached))
            return cached;

        string path = $"{SlicedPath}button_{state}.png";
        if (ResourceLoader.Exists(path))
        {
            var tex = GD.Load<Texture2D>(path);
            if (tex != null)
            {
                var styleBox = new StyleBoxTexture();
                styleBox.Texture = tex;
                styleBox.TextureMarginLeft = ButtonMargin;
                styleBox.TextureMarginTop = ButtonMargin;
                styleBox.TextureMarginRight = ButtonMargin;
                styleBox.TextureMarginBottom = ButtonMargin;
                styleBox.ContentMarginLeft = ButtonContentMarginH;
                styleBox.ContentMarginRight = ButtonContentMarginH;
                styleBox.ContentMarginTop = ButtonContentMarginV;
                styleBox.ContentMarginBottom = ButtonContentMarginV;
                _styleBoxCache[cacheKey] = styleBox;
                return styleBox;
            }
        }

        GD.PushWarning($"UIThemeLoader: Button texture not found: {path}");
        var fallback = new StyleBoxFlat();
        fallback.BgColor = state switch
        {
            "hover" => new Color(0.35f, 0.3f, 0.25f),
            "pressed" => new Color(0.2f, 0.17f, 0.12f),
            _ => new Color(0.28f, 0.24f, 0.18f),
        };
        fallback.SetCornerRadiusAll(2);
        _styleBoxCache[cacheKey] = fallback;
        return fallback;
    }

    /// <summary>
    /// Returns Texture2D from sliced/bag_{bagType}.png or null if not found.
    /// Bag textures are 1x density, intended for NinePatchRect rendering.
    /// </summary>
    public static Texture2D? GetBagThemeTexture(string bagType)
        => LoadTexture("bag", SlicedPath, $"bag_{bagType}");

    private static Texture2D? LoadTexture(string category, string basePath, string name)
    {
        string cacheKey = $"{category}:{name}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        string path = $"{basePath}{name}.png";
        if (ResourceLoader.Exists(path))
        {
            var tex = GD.Load<Texture2D>(path);
            if (tex != null)
            {
                _textureCache[cacheKey] = tex;
                return tex;
            }
        }

        GD.PushWarning($"UIThemeLoader: {category} texture not found: {path}");
        _textureCache[cacheKey] = null;
        return null;
    }

    /// <summary>
    /// Returns a StyleBox for the scrollbar grabber (thumb) matching the Kit 01 palette.
    /// Uses a warm brown tone consistent with button textures.
    /// </summary>
    public static StyleBox GetScrollbarGrabberStyleBox(string state = "normal")
    {
        string cacheKey = $"scrollbar_grabber:{state}";
        if (_styleBoxCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var color = state switch
        {
            "highlight" => new Color(0.5f, 0.4f, 0.3f),
            "pressed" => new Color(0.35f, 0.28f, 0.2f),
            _ => new Color(0.42f, 0.34f, 0.25f),
        };

        var style = new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2,
        };
        _styleBoxCache[cacheKey] = style;
        return style;
    }

    /// <summary>
    /// Returns a StyleBox for the scrollbar track background.
    /// </summary>
    public static StyleBox GetScrollbarTrackStyleBox()
    {
        string cacheKey = "scrollbar_track";
        if (_styleBoxCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.12f, 0.1f, 0.6f),
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2,
        };
        _styleBoxCache[cacheKey] = style;
        return style;
    }

    /// <summary>
    /// Creates an aspect tile icon: TextureRect if sprite found, ColorRect fallback.
    /// </summary>
    public static Control CreateAspectIcon(string aspectId, float size)
    {
        var tileTex = GetAspectTileTexture(aspectId);
        if (tileTex != null)
        {
            return new TextureRect
            {
                Texture = tileTex,
                CustomMinimumSize = new Vector2(size, size),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspect,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            };
        }
        return new ColorRect
        {
            CustomMinimumSize = new Vector2(size, size),
            Color = GridConstants.GetAspectColor(aspectId),
        };
    }

    /// <summary>
    /// Clears all cached textures and style boxes.
    /// </summary>
    public static void ClearCache()
    {
        _styleBoxCache.Clear();
        _textureCache.Clear();
    }
}
