using Godot;
using AlchemistCourier.Core.Visual;

namespace AlchemistCourier.Game.Visual;

/// <summary>
/// Renders a single cell of a reagent sprite sheet, showing the correct region
/// for the current visual state. Handles animated states (Breaking, Exploding)
/// by advancing frames over time.
/// </summary>
public partial class SpriteStateRenderer : TextureRect
{
    private const float AnimationFps = 8f;
    private const float FrameDuration = 1f / AnimationFps;

    private ReagentSpriteSheet? _spriteSheet;
    private VisualState _currentState = VisualState.Normal;
    private int _currentFrame;
    private float _frameTimer;
    private bool _animating;

    [Signal]
    public delegate void AnimationFinishedEventHandler();

    /// <summary>Current visual state being rendered.</summary>
    public VisualState CurrentState => _currentState;

    /// <summary>Whether an animation is currently playing.</summary>
    public bool IsAnimating => _animating;

    /// <summary>
    /// Initializes the renderer with a sprite sheet and size.
    /// </summary>
    public void Initialize(ReagentSpriteSheet spriteSheet, int pixelWidth, int pixelHeight)
    {
        _spriteSheet = spriteSheet;
        CustomMinimumSize = new Vector2(pixelWidth, pixelHeight);
        Size = new Vector2(pixelWidth, pixelHeight);
        ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.Scale;
        SetState(VisualState.Normal);
    }

    /// <summary>
    /// Sets the visual state and updates the displayed sprite region.
    /// Animated states (Breaking, Exploding) start frame playback.
    /// </summary>
    public void SetState(VisualState state)
    {
        _currentState = state;
        _currentFrame = 0;
        _frameTimer = 0f;

        if (_spriteSheet == null) return;

        int frameCount = GetFrameCount(_spriteSheet.Spec, state);
        _animating = frameCount > 1;

        UpdateTexture();
    }

    public override void _Process(double delta)
    {
        if (!_animating || _spriteSheet == null) return;

        _frameTimer += (float)delta;
        if (_frameTimer < FrameDuration) return;

        _frameTimer -= FrameDuration;
        int frameCount = GetFrameCount(_spriteSheet.Spec, _currentState);
        _currentFrame++;

        if (_currentFrame >= frameCount)
        {
            _animating = false;
            _currentFrame = frameCount - 1;
            UpdateTexture();
            EmitSignal(SignalName.AnimationFinished);
            return;
        }

        UpdateTexture();
    }

    private static int GetFrameCount(SpriteSheetSpec spec, VisualState state)
    {
        return spec.FrameCounts.TryGetValue(state, out int count) ? count : 1;
    }

    private void UpdateTexture()
    {
        if (_spriteSheet == null)
        {
            Texture = null;
            return;
        }

        Texture = _spriteSheet.GetAtlasTexture(_currentState, _currentFrame);
    }
}
