using System.Collections.Generic;

namespace AlchemistCourier.Core.Visual;

/// <summary>
/// Defines the layout of a sprite sheet: which row holds each visual state
/// and how many animation frames that state has.
/// </summary>
public class SpriteSheetSpec
{
    /// <summary>Maps each visual state to its row index in the sprite sheet.</summary>
    public IReadOnlyDictionary<VisualState, int> StateRowMap { get; }

    /// <summary>Maps each visual state to its number of animation frames.</summary>
    public IReadOnlyDictionary<VisualState, int> FrameCounts { get; }

    /// <summary>Width of one frame in pixels (covers the shape's bounding box).</summary>
    public int CellWidth { get; }

    /// <summary>Height of one frame in pixels (covers the shape's bounding box).</summary>
    public int CellHeight { get; }

    /// <summary>Total number of rows in the sprite sheet.</summary>
    public int TotalRows { get; }

    /// <summary>Maximum frame count across all states (determines sheet width).</summary>
    public int MaxFrames { get; }

    public SpriteSheetSpec(
        Dictionary<VisualState, int> stateRowMap,
        Dictionary<VisualState, int> frameCounts,
        int cellWidth,
        int cellHeight)
    {
        StateRowMap = stateRowMap;
        FrameCounts = frameCounts;
        CellWidth = cellWidth;
        CellHeight = cellHeight;

        TotalRows = 0;
        MaxFrames = 1;
        foreach (var kvp in stateRowMap)
        {
            if (kvp.Value + 1 > TotalRows)
                TotalRows = kvp.Value + 1;
        }
        foreach (var kvp in frameCounts)
        {
            if (kvp.Value > MaxFrames)
                MaxFrames = kvp.Value;
        }
    }

    /// <summary>
    /// Returns the pixel region (x, y, width, height) for a given state and frame index.
    /// Returns null if the state is not in this spec or the frame index is out of range.
    /// </summary>
    public (int X, int Y, int Width, int Height)? GetRegion(VisualState state, int frame = 0)
    {
        if (!StateRowMap.TryGetValue(state, out int row))
            return null;

        int frameCount = FrameCounts.TryGetValue(state, out int fc) ? fc : 1;
        if (frame < 0 || frame >= frameCount)
            return null;

        return (frame * CellWidth, row * CellHeight, CellWidth, CellHeight);
    }

    /// <summary>
    /// Returns the total pixel width of the sprite sheet.
    /// </summary>
    public int SheetWidth => MaxFrames * CellWidth;

    /// <summary>
    /// Returns the total pixel height of the sprite sheet.
    /// </summary>
    public int SheetHeight => TotalRows * CellHeight;

    /// <summary>
    /// Creates the default reagent sprite sheet spec: 7 rows (one per VisualState),
    /// static states get 1 frame, animated states (Breaking, Exploding) get 4 frames.
    /// </summary>
    public static SpriteSheetSpec DefaultReagentSpec(int cellWidth, int cellHeight)
    {
        var stateRowMap = new Dictionary<VisualState, int>
        {
            [VisualState.Normal] = 0,
            [VisualState.Heated] = 1,
            [VisualState.Cooled] = 2,
            [VisualState.ThermalStress] = 3,
            [VisualState.Crushed] = 4,
            [VisualState.Breaking] = 5,
            [VisualState.Exploding] = 6,
        };

        var frameCounts = new Dictionary<VisualState, int>
        {
            [VisualState.Normal] = 1,
            [VisualState.Heated] = 1,
            [VisualState.Cooled] = 1,
            [VisualState.ThermalStress] = 1,
            [VisualState.Crushed] = 1,
            [VisualState.Breaking] = 4,
            [VisualState.Exploding] = 4,
        };

        return new SpriteSheetSpec(stateRowMap, frameCounts, cellWidth, cellHeight);
    }
}
