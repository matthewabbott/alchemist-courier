using System;
using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Game.Inventory;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Animation;

/// <summary>
/// Plays a falling animation when a Heavy reagent is placed in a Gravity bag
/// and the final resting position differs from the drop position.
/// Creates temporary colored overlays at the drop position and tweens them
/// down to the final position, then self-destructs.
/// </summary>
public partial class FallingAnimator : Control
{
    private const float FallDuration = 0.3f;

    [Signal]
    public delegate void FallingStartedEventHandler();

    [Signal]
    public delegate void FallingCompleteEventHandler();

    /// <summary>Whether a falling animation is currently playing.</summary>
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// Plays a falling animation for a reagent from its drop position to its final resting position.
    /// Creates colored overlays matching the reagent's cell colors and tweens them downward.
    /// </summary>
    /// <param name="reagent">The reagent that fell.</param>
    /// <param name="fromPos">The grid position where the user dropped it.</param>
    /// <param name="toPos">The grid position where it came to rest.</param>
    /// <param name="onComplete">Optional callback when the animation finishes.</param>
    public void PlayFall(Reagent reagent, CoreVec fromPos, CoreVec toPos, Action? onComplete = null)
    {
        if (fromPos == toPos)
        {
            onComplete?.Invoke();
            return;
        }

        IsPlaying = true;
        EmitSignal(SignalName.FallingStarted);

        var shape = reagent.GetRotatedShape();
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);
        float pixelDeltaY = (toPos.Row - fromPos.Row) * GridConstants.CellSize;

        var overlays = new List<ColorRect>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!shape[r, c]) continue;

                var cellGridPos = new CoreVec(fromPos.Col + c, fromPos.Row + r);
                var cellPixel = GridCoordinates.GridToPixel(cellGridPos);
                var localPos = new CoreVec(c, r);
                var color = GridConstants.GetDominantColor(reagent, localPos);

                var rect = new ColorRect
                {
                    Position = cellPixel,
                    Size = new Vector2(GridConstants.CellSize, GridConstants.CellSize),
                    Color = color,
                    ZIndex = 6, // Above highlights and other overlays
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                AddChild(rect);
                overlays.Add(rect);
            }
        }

        if (overlays.Count == 0)
        {
            IsPlaying = false;
            onComplete?.Invoke();
            return;
        }

        var tween = CreateTween();
        tween.SetParallel(true);

        foreach (var rect in overlays)
        {
            tween.TweenProperty(rect, "position:y", rect.Position.Y + pixelDeltaY, FallDuration)
                 .SetTrans(Tween.TransitionType.Bounce)
                 .SetEase(Tween.EaseType.Out);
        }

        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() =>
        {
            foreach (var rect in overlays)
                rect.QueueFree();

            IsPlaying = false;
            EmitSignal(SignalName.FallingComplete);
            onComplete?.Invoke();
        }));
    }
}
