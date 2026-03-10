using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Visual;

namespace AlchemistCourier.Game.Visual;

/// <summary>
/// Wraps a Texture2D sprite sheet + SpriteSheetSpec for a specific reagent species+shape.
/// Provides AtlasTexture lookup by visual state and frame.
/// </summary>
public class ReagentSpriteSheet
{
    private const string SpriteBasePath = "res://art/sprites/reagents/";

    public Texture2D Texture { get; }
    public SpriteSheetSpec Spec { get; }
    public string ReagentId { get; }

    public ReagentSpriteSheet(Texture2D texture, SpriteSheetSpec spec, string reagentId)
    {
        Texture = texture;
        Spec = spec;
        ReagentId = reagentId;
    }

    /// <summary>
    /// True if the loaded texture is a single tile (not a full sprite sheet).
    /// Single-tile textures show the same image for all visual states.
    /// </summary>
    public bool IsSingleTile => Texture.GetWidth() <= Spec.CellWidth
                                && Texture.GetHeight() <= Spec.CellHeight;

    /// <summary>
    /// Returns an AtlasTexture for the given visual state and frame,
    /// or null if the state/frame is invalid.
    /// </summary>
    public AtlasTexture? GetAtlasTexture(VisualState state, int frame = 0)
    {
        // Single-tile textures use the whole image for all states
        if (IsSingleTile)
        {
            return new AtlasTexture
            {
                Atlas = Texture,
                Region = new Rect2(0, 0, Texture.GetWidth(), Texture.GetHeight()),
            };
        }

        var region = Spec.GetRegion(state, frame);
        if (region == null) return null;

        var (x, y, w, h) = region.Value;
        var atlas = new AtlasTexture
        {
            Atlas = Texture,
            Region = new Rect2(x, y, w, h),
        };
        return atlas;
    }

    /// <summary>
    /// Tries to load a sprite sheet from the standard path:
    /// res://art/sprites/reagents/{reagentId}.png
    /// Returns null if the file doesn't exist.
    /// </summary>
    public static ReagentSpriteSheet? TryLoad(string reagentId, SpriteSheetSpec spec)
    {
        return TryLoad(reagentId, spec, null);
    }

    /// <summary>
    /// Tries to load a sprite sheet with optional aspect-based monomino fallback.
    /// </summary>
    public static ReagentSpriteSheet? TryLoad(string reagentId, SpriteSheetSpec spec, string? aspectId)
    {
        var texture = SpriteSwapper.LoadReagentTexture(reagentId, aspectId);
        if (texture == null) return null;

        return new ReagentSpriteSheet(texture, spec, reagentId);
    }

    // Cache of loaded sprite sheets by reagent ID
    private static readonly Dictionary<string, ReagentSpriteSheet?> _cache = new();

    /// <summary>
    /// Loads a sprite sheet with caching. Returns null if no sprite exists.
    /// </summary>
    public static ReagentSpriteSheet? GetOrLoad(string reagentId, SpriteSheetSpec spec)
    {
        return GetOrLoad(reagentId, spec, null);
    }

    /// <summary>
    /// Loads a sprite sheet with caching and optional aspect-based monomino fallback.
    /// </summary>
    public static ReagentSpriteSheet? GetOrLoad(string reagentId, SpriteSheetSpec spec, string? aspectId)
    {
        if (_cache.TryGetValue(reagentId, out var cached))
            return cached;

        var sheet = TryLoad(reagentId, spec, aspectId);
        _cache[reagentId] = sheet;
        return sheet;
    }

    /// <summary>Clears the sprite sheet cache.</summary>
    public static void ClearCache()
    {
        _cache.Clear();
    }
}
