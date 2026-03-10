using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Core.Visual;
using AlchemistCourier.Game.Inventory;
using AlchemistCourier.Game.Visual;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Animation;

/// <summary>
/// Plays reaction animations on the grid when a puzzle confirmation detects reactions.
/// Spawns temporary visual nodes at affected cells and sequences animations before
/// the result overlay is shown.
/// </summary>
public partial class ReactionAnimator : Control
{
    private const float AnimationFps = 8f;
    private const float FrameDuration = 1f / AnimationFps;

    // Per-reaction type animation durations (seconds)
    private const float ExplosionDuration = 0.6f;
    private const float BreakageDuration = 0.5f;
    private const float ToxicCloudDuration = 0.5f;
    private const float GenericDuration = 0.4f;

    private MetaGridView? _gridView;
    private Core.Grid.MetaGrid? _metaGrid;
    private bool _playing;
    private int _pendingAnimations;

    [Signal]
    public delegate void AllAnimationsCompleteEventHandler();

    [Signal]
    public delegate void ExplosionStartedEventHandler();

    [Signal]
    public delegate void BreakageStartedEventHandler();

    [Signal]
    public delegate void ToxicCloudStartedEventHandler();

    /// <summary>Whether animations are currently playing.</summary>
    public bool IsPlaying => _playing;

    /// <summary>
    /// Sets the grid view and grid references needed for positioning animations.
    /// </summary>
    public void SetContext(MetaGridView gridView, Core.Grid.MetaGrid metaGrid)
    {
        _gridView = gridView;
        _metaGrid = metaGrid;
    }

    /// <summary>
    /// Plays reaction animations for the given results. Emits AllAnimationsComplete when done.
    /// Independent reactions play in parallel; the signal fires after all finish.
    /// </summary>
    public void PlayReactions(List<ReactionResult> reactions)
    {
        if (reactions.Count == 0)
        {
            CallDeferred(MethodName.EmitAllComplete);
            return;
        }

        _playing = true;
        _pendingAnimations = reactions.Count;

        foreach (var reaction in reactions)
        {
            PlaySingleReaction(reaction);
        }
    }

    private void PlaySingleReaction(ReactionResult reaction)
    {
        switch (reaction.Type)
        {
            case ReactionResultType.Explosion:
                EmitSignal(SignalName.ExplosionStarted);
                PlayExplosion(reaction);
                break;
            case ReactionResultType.Breakage:
                EmitSignal(SignalName.BreakageStarted);
                PlayBreakage(reaction);
                break;
            case ReactionResultType.ToxicCloud:
                EmitSignal(SignalName.ToxicCloudStarted);
                PlayToxicCloud(reaction);
                break;
            default:
                // Cumulative reactions (Overheating, Hypothermia, Poisoning, etc.)
                // use a generic flash animation
                PlayGenericReaction(reaction);
                break;
        }
    }

    /// <summary>
    /// Explosion: flash white → orange/red burst → particle scatter → fade
    /// </summary>
    private void PlayExplosion(ReactionResult reaction)
    {
        // Try sprite-based animation first
        if (TrySpriteAnimation(reaction, VisualState.Exploding))
            return;

        // Procedural: create colored overlay rects and tween them
        var nodes = CreateCellOverlays(reaction.AffectedCells, new Color(1f, 1f, 1f, 0.9f));
        var tween = CreateTween();
        tween.SetParallel(true);

        foreach (var rect in nodes)
        {
            // Phase 1: flash white (0.1s)
            tween.TweenProperty(rect, "color", new Color(1f, 0.6f, 0.1f, 0.9f), 0.1f);
        }

        tween.SetParallel(false);
        tween.TweenInterval(0.1f);
        tween.SetParallel(true);

        foreach (var rect in nodes)
        {
            // Phase 2: orange/red burst + expand slightly (0.2s)
            tween.TweenProperty(rect, "color", new Color(0.9f, 0.25f, 0.1f, 0.8f), 0.2f);
            tween.TweenProperty(rect, "scale", new Vector2(1.15f, 1.15f), 0.2f)
                 .SetEase(Tween.EaseType.Out);
        }

        tween.SetParallel(false);
        tween.TweenInterval(0.2f);
        tween.SetParallel(true);

        foreach (var rect in nodes)
        {
            // Phase 3: scatter outward + fade (0.2s)
            tween.TweenProperty(rect, "modulate:a", 0f, 0.2f);
            tween.TweenProperty(rect, "scale", new Vector2(1.4f, 1.4f), 0.2f)
                 .SetEase(Tween.EaseType.Out);
        }

        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => CleanupAndComplete(nodes)));
    }

    /// <summary>
    /// Breakage: crack lines appear → fragments separate → scatter/fade
    /// </summary>
    private void PlayBreakage(ReactionResult reaction)
    {
        if (TrySpriteAnimation(reaction, VisualState.Breaking))
            return;

        // Procedural: lavender overlay with crack effect
        var baseColor = GridConstants.GetAspectColor(AspectIds.Fragile);
        var nodes = CreateCellOverlays(reaction.AffectedCells, new Color(baseColor.R, baseColor.G, baseColor.B, 0.7f));
        var tween = CreateTween();
        tween.SetParallel(true);

        foreach (var rect in nodes)
        {
            // Phase 1: crack lines (brighten) 0.15s
            tween.TweenProperty(rect, "color", new Color(1f, 1f, 1f, 0.8f), 0.15f);
        }

        tween.SetParallel(false);
        tween.TweenInterval(0.15f);
        tween.SetParallel(true);

        foreach (var rect in nodes)
        {
            // Phase 2: fragments separate (shrink + offset) 0.2s
            tween.TweenProperty(rect, "scale", new Vector2(0.7f, 0.7f), 0.2f)
                 .SetEase(Tween.EaseType.In);
            tween.TweenProperty(rect, "modulate:a", 0.3f, 0.2f);
        }

        tween.SetParallel(false);
        tween.TweenInterval(0.15f);
        tween.SetParallel(true);

        foreach (var rect in nodes)
        {
            // Phase 3: fade out
            tween.TweenProperty(rect, "modulate:a", 0f, 0.1f);
        }

        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => CleanupAndComplete(nodes)));
    }

    /// <summary>
    /// Toxic Cloud: green haze particles emanate → spread to adjacent cells
    /// </summary>
    private void PlayToxicCloud(ReactionResult reaction)
    {
        var baseColor = GridConstants.GetAspectColor(AspectIds.Toxic);
        var nodes = CreateCellOverlays(reaction.AffectedCells, new Color(baseColor.R, baseColor.G, baseColor.B, 0f));
        var tween = CreateTween();
        tween.SetParallel(true);

        foreach (var rect in nodes)
        {
            // Phase 1: green haze emanates (0.3s)
            tween.TweenProperty(rect, "color:a", 0.6f, 0.3f)
                 .SetEase(Tween.EaseType.Out);
        }

        tween.SetParallel(false);
        tween.TweenInterval(0.3f);
        tween.SetParallel(true);

        foreach (var rect in nodes)
        {
            // Phase 2: spread + fade (0.2s)
            tween.TweenProperty(rect, "scale", new Vector2(1.3f, 1.3f), 0.2f)
                 .SetEase(Tween.EaseType.Out);
            tween.TweenProperty(rect, "modulate:a", 0f, 0.2f);
        }

        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => CleanupAndComplete(nodes)));
    }

    /// <summary>
    /// Generic reaction animation for cumulative types (Overheating, Hypothermia, Poisoning, etc.)
    /// </summary>
    private void PlayGenericReaction(ReactionResult reaction)
    {
        var color = GetReactionColor(reaction.Type);
        var nodes = CreateCellOverlays(reaction.AffectedCells, new Color(color.R, color.G, color.B, 0f));
        var tween = CreateTween();
        tween.SetParallel(true);

        foreach (var rect in nodes)
        {
            // Flash the reaction color (0.2s in, 0.2s out)
            tween.TweenProperty(rect, "color:a", 0.7f, 0.2f)
                 .SetEase(Tween.EaseType.Out);
        }

        tween.SetParallel(false);
        tween.TweenInterval(0.2f);
        tween.SetParallel(true);

        foreach (var rect in nodes)
        {
            tween.TweenProperty(rect, "modulate:a", 0f, 0.2f);
        }

        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => CleanupAndComplete(nodes)));
    }

    /// <summary>
    /// Attempts to use sprite sheet animated states (Exploding/Breaking) for the reaction.
    /// Returns true if sprite animation was started, false if no sprite sheets available.
    /// </summary>
    private bool TrySpriteAnimation(ReactionResult reaction, VisualState targetState)
    {
        if (_gridView == null || _metaGrid == null) return false;

        // Find reagents at the affected cells
        var reagentsToAnimate = new HashSet<Reagent>();
        foreach (var cell in reaction.AffectedCells)
        {
            if (cell.Col < 0 || cell.Col >= _metaGrid.Width ||
                cell.Row < 0 || cell.Row >= _metaGrid.Height)
                continue;

            var gridCell = _metaGrid.GetCell(cell.Col, cell.Row);
            if (gridCell?.OccupyingReagent is Reagent r)
                reagentsToAnimate.Add(r);
        }

        if (reagentsToAnimate.Count == 0) return false;

        // Check if any have sprite sheets
        bool anyHasSprite = false;
        var spriteRenderers = new List<SpriteStateRenderer>();

        foreach (var reagent in reagentsToAnimate)
        {
            var catalogId = reagent.Definition.Id;
            if (catalogId == null) continue;

            var rotatedShape = reagent.GetRotatedShape();
            int pixelW = rotatedShape.GetLength(1) * GridConstants.CellSize;
            int pixelH = rotatedShape.GetLength(0) * GridConstants.CellSize;
            var spec = SpriteSheetSpec.DefaultReagentSpec(pixelW, pixelH);
            var spriteSheet = ReagentSpriteSheet.GetOrLoad(catalogId, spec, reagent.Definition.PrimaryAspectId);
            if (spriteSheet == null) continue;

            anyHasSprite = true;

            // Create a temporary SpriteStateRenderer for the animation
            var renderer = new SpriteStateRenderer
            {
                ZIndex = 5, // Above highlights
                MouseFilter = MouseFilterEnum.Ignore,
            };
            renderer.Initialize(spriteSheet, pixelW, pixelH);
            renderer.Position = GridCoordinates.GridToPixel(reagent.PlacedPosition!.Value);
            AddChild(renderer);
            renderer.SetState(targetState);
            spriteRenderers.Add(renderer);
        }

        if (!anyHasSprite) return false;

        // Wait for all sprite animations to finish, then clean up
        int remaining = spriteRenderers.Count;
        foreach (var renderer in spriteRenderers)
        {
            renderer.AnimationFinished += () =>
            {
                remaining--;
                // Fade out after animation finishes
                var fadeTween = CreateTween();
                fadeTween.TweenProperty(renderer, "modulate:a", 0f, 0.15f);
                fadeTween.TweenCallback(Callable.From(() =>
                {
                    renderer.QueueFree();
                    if (remaining <= 0)
                        OnAnimationComplete();
                }));
            };
        }

        return true;
    }

    private List<ColorRect> CreateCellOverlays(List<CoreVec> cells, Color color)
    {
        var rects = new List<ColorRect>();
        foreach (var cell in cells)
        {
            var pixelPos = GridCoordinates.GridToPixel(cell);
            var rect = new ColorRect
            {
                Position = pixelPos,
                Size = new Vector2(GridConstants.CellSize, GridConstants.CellSize),
                Color = color,
                ZIndex = 5, // Above highlights, below UI
                MouseFilter = MouseFilterEnum.Ignore,
                PivotOffset = new Vector2(GridConstants.CellSize / 2f, GridConstants.CellSize / 2f),
            };
            AddChild(rect);
            rects.Add(rect);
        }
        return rects;
    }

    private void CleanupAndComplete(List<ColorRect> nodes)
    {
        foreach (var node in nodes)
            node.QueueFree();
        OnAnimationComplete();
    }

    private void OnAnimationComplete()
    {
        _pendingAnimations--;
        if (_pendingAnimations <= 0)
        {
            _playing = false;
            _pendingAnimations = 0;
            EmitSignal(SignalName.AllAnimationsComplete);
        }
    }

    private void EmitAllComplete()
    {
        _playing = false;
        EmitSignal(SignalName.AllAnimationsComplete);
    }

    private static Color GetReactionColor(ReactionResultType type) => type switch
    {
        ReactionResultType.Explosion => GridConstants.GetAspectColor(AspectIds.Volatile),
        ReactionResultType.Breakage => GridConstants.GetAspectColor(AspectIds.Fragile),
        ReactionResultType.ToxicCloud => GridConstants.GetAspectColor(AspectIds.Toxic),
        ReactionResultType.Overheating => GridConstants.GetAspectColor(AspectIds.Hot),
        ReactionResultType.Hypothermia => GridConstants.GetAspectColor(AspectIds.Cold),
        ReactionResultType.Poisoning => GridConstants.GetAspectColor(AspectIds.Toxic),
        ReactionResultType.Neutralization => new Color(0.5f, 0.5f, 0.7f),
        _ => new Color(0.7f, 0.7f, 0.7f),
    };
}
