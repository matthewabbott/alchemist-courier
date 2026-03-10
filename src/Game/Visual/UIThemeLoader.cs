using Godot;
using System.Collections.Generic;

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
    /// Returns Texture2D from tiles/{aspectName}.png or null if not found.
    /// Aspect tiles are 32x32 at base resolution.
    /// </summary>
    public static Texture2D? GetAspectTileTexture(string aspectName)
    {
        string cacheKey = $"tile:{aspectName}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        string path = $"{TilePath}{aspectName}.png";
        if (ResourceLoader.Exists(path))
        {
            var tex = GD.Load<Texture2D>(path);
            if (tex != null)
            {
                _textureCache[cacheKey] = tex;
                return tex;
            }
        }

        GD.PushWarning($"UIThemeLoader: Aspect tile texture not found: {path}");
        _textureCache[cacheKey] = null;
        return null;
    }

    /// <summary>
    /// Returns Texture2D from decorations/{name}.png or null if not found.
    /// Decoration textures are 2x density.
    /// </summary>
    public static Texture2D? GetDecorationTexture(string name)
    {
        string cacheKey = $"decoration:{name}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        string path = $"{DecorationPath}{name}.png";
        if (ResourceLoader.Exists(path))
        {
            var tex = GD.Load<Texture2D>(path);
            if (tex != null)
            {
                _textureCache[cacheKey] = tex;
                return tex;
            }
        }

        GD.PushWarning($"UIThemeLoader: Decoration texture not found: {path}");
        _textureCache[cacheKey] = null;
        return null;
    }

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
    {
        string cacheKey = $"bag:{bagType}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        string path = $"{SlicedPath}bag_{bagType}.png";
        if (ResourceLoader.Exists(path))
        {
            var tex = GD.Load<Texture2D>(path);
            if (tex != null)
            {
                _textureCache[cacheKey] = tex;
                return tex;
            }
        }

        GD.PushWarning($"UIThemeLoader: Bag theme texture not found: {path}");
        _textureCache[cacheKey] = null;
        return null;
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
