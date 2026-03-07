using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Game.Inventory;

public static class GridConstants
{
    public const int CellSize = 32;
    public const int GridLineWidth = 1;
    public const int BagBorderWidth = 2;

    // Base colors
    public static readonly Color EmptyCell = new(0.15f, 0.15f, 0.2f);
    public static readonly Color BagCellEmpty = new(0.35f, 0.35f, 0.4f);
    public static readonly Color BagBorderColor = new(0.6f, 0.6f, 0.65f);
    public static readonly Color GridLineColor = new(0.25f, 0.25f, 0.3f);
    public static readonly Color ValidPlacement = new(0.2f, 0.8f, 0.2f, 0.45f);
    public static readonly Color WarningPlacement = new(0.95f, 0.75f, 0.1f, 0.45f);
    public static readonly Color InvalidPlacement = new(0.8f, 0.2f, 0.2f, 0.45f);

    // Highlight overlay
    public const int HighlightEdgeWidth = 3;
    public static readonly Color DimOverlayColor = new(0f, 0f, 0f, 0.6f);

    // Hint system
    public static readonly Color HintedBorderColor = new(0.55f, 0.45f, 0.75f);

    // Locked reagent indicator
    public static readonly Color LockedBorderColor = new(0.7f, 0.6f, 0.3f);

    // Hover frame overlay
    public const float FramePulseAlphaMin = 0.25f;
    public const float FramePulseAlphaMax = 0.55f;
    public const float FramePulsePeriod = 1.5f;
    public const float FrameSelectedAlphaMin = 0.35f;
    public const float FrameSelectedAlphaMax = 0.65f;
    public const float FrameDragAlpha = 0.50f;
    public const float FrameFadeDuration = 0.15f;

    // Bag hue offsets — each placed bag gets a slightly different tint
    private static readonly Color[] BagTints = new[]
    {
        new Color(0.38f, 0.36f, 0.42f), // slightly purple-gray
        new Color(0.36f, 0.40f, 0.38f), // slightly green-gray
        new Color(0.40f, 0.37f, 0.35f), // slightly warm gray
        new Color(0.35f, 0.38f, 0.42f), // slightly blue-gray
        new Color(0.40f, 0.35f, 0.40f), // slightly mauve
    };

    public static Color GetBagTint(int bagIndex)
    {
        return BagTints[bagIndex % BagTints.Length];
    }

    public static Color GetAspectColor(string aspectId) => aspectId switch
    {
        AspectIds.Hot       => new Color(0.9f, 0.25f, 0.15f),
        AspectIds.Cold      => new Color(0.2f, 0.45f, 0.9f),
        AspectIds.Volatile  => new Color(0.95f, 0.6f, 0.15f),
        AspectIds.Toxic     => new Color(0.2f, 0.75f, 0.25f),
        AspectIds.Fragile   => new Color(0.7f, 0.7f, 0.9f),
        AspectIds.Heavy     => new Color(0.5f, 0.4f, 0.3f),
        AspectIds.Sharp     => new Color(0.8f, 0.8f, 0.8f),
        AspectIds.Medicinal => new Color(0.9f, 0.5f, 0.7f),
        AspectIds.Stable    => new Color(0.6f, 0.6f, 0.6f),
        AspectIds.Catalytic => new Color(0.9f, 0.85f, 0.2f),
        _                   => new Color(0.7f, 0.7f, 0.7f),
    };

    public static Color GetDominantColor(Reagent reagent, Core.Grid.Vector2I localPos)
    {
        var aspects = reagent.GetAspectsAtLocalPosition(localPos);
        if (aspects.IsEmpty)
            return new Color(0.6f, 0.55f, 0.5f); // neutral tan
        return GetAspectColor(aspects.Aspects[0].Id);
    }
}
