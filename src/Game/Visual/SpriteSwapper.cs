using Godot;

namespace AlchemistCourier.Game.Visual;

/// <summary>
/// Loads sprites with a custom-first fallback chain.
/// Checks art/sprites/custom/ before art/sprites/reagents/ for reagent textures,
/// and art/sprites/custom/frames/ before art/sprites/frames/dev/ for frame textures.
/// </summary>
public static class SpriteSwapper
{
    private const string CustomReagentPath = "res://art/sprites/custom/";
    private const string CustomMonominoPath = "res://art/sprites/custom/monomino_";
    private const string DefaultReagentPath = "res://art/sprites/reagents/";
    private const string CustomFramePath = "res://art/sprites/custom/frames/";
    private const string DefaultFramePath = "res://art/sprites/frames/dev/";
    private const string CustomBagBorderPath = "res://art/sprites/custom/bags/";
    private const string SlicedBagBorderPath = "res://art/sprites/ui/sliced/";

    /// <summary>
    /// Tries to load a reagent sprite texture, checking custom path first.
    /// Returns null if no texture found at either location.
    /// </summary>
    public static Texture2D? LoadReagentTexture(string reagentId)
    {
        return LoadReagentTexture(reagentId, null);
    }

    /// <summary>
    /// Tries to load a reagent sprite texture with optional aspect-based monomino fallback.
    /// Check order: custom/{reagentId}.png → custom/monomino_{aspectId}.png → reagents/{reagentId}.png.
    /// </summary>
    public static Texture2D? LoadReagentTexture(string reagentId, string? aspectId)
    {
        // Try custom sprite first
        string customPath = $"{CustomReagentPath}{reagentId}.png";
        if (ResourceLoader.Exists(customPath))
        {
            var tex = GD.Load<Texture2D>(customPath);
            if (tex != null) return tex;
        }

        // Try monomino aspect sprite if aspectId is provided
        if (aspectId != null)
        {
            string monominoPath = $"{CustomMonominoPath}{aspectId}.png";
            if (ResourceLoader.Exists(monominoPath))
            {
                var tex = GD.Load<Texture2D>(monominoPath);
                if (tex != null) return tex;
            }
        }

        // Fall back to default (dev-generated) sprite
        string defaultPath = $"{DefaultReagentPath}{reagentId}.png";
        if (ResourceLoader.Exists(defaultPath))
        {
            var tex = GD.Load<Texture2D>(defaultPath);
            if (tex != null) return tex;
        }

        return null;
    }

    /// <summary>
    /// Tries to load a frame texture, checking custom path first.
    /// Returns null if no texture found at either location.
    /// </summary>
    public static Texture2D? LoadFrameTexture(string filename)
    {
        string customPath = $"{CustomFramePath}{filename}";
        if (ResourceLoader.Exists(customPath))
        {
            var tex = GD.Load<Texture2D>(customPath);
            if (tex != null) return tex;
        }

        string defaultPath = $"{DefaultFramePath}{filename}";
        if (ResourceLoader.Exists(defaultPath))
        {
            var tex = GD.Load<Texture2D>(defaultPath);
            if (tex != null) return tex;
        }

        return null;
    }

    /// <summary>
    /// Checks whether a custom reagent sprite exists for the given ID.
    /// </summary>
    public static bool HasCustomReagentSprite(string reagentId)
    {
        return ResourceLoader.Exists($"{CustomReagentPath}{reagentId}.png");
    }

    /// <summary>
    /// Checks whether a custom monomino sprite exists for the given aspect.
    /// </summary>
    public static bool HasMonominoSprite(string aspectId)
    {
        return ResourceLoader.Exists($"{CustomMonominoPath}{aspectId}.png");
    }

    /// <summary>
    /// Checks whether a custom frame texture exists for the given filename.
    /// </summary>
    public static bool HasCustomFrameTexture(string filename)
    {
        return ResourceLoader.Exists($"{CustomFramePath}{filename}");
    }

    /// <summary>
    /// Tries to load a bag border texture.
    /// Check order: custom/bags/{name}.png → ui/sliced/{name}.png.
    /// </summary>
    public static Texture2D? LoadBagBorderTexture(string name)
    {
        string customPath = $"{CustomBagBorderPath}{name}.png";
        if (ResourceLoader.Exists(customPath))
        {
            var tex = GD.Load<Texture2D>(customPath);
            if (tex != null) return tex;
        }

        string slicedPath = $"{SlicedBagBorderPath}{name}.png";
        if (ResourceLoader.Exists(slicedPath))
        {
            var tex = GD.Load<Texture2D>(slicedPath);
            if (tex != null) return tex;
        }

        return null;
    }
}
