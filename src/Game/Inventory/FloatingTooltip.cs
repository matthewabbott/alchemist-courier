using Godot;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Base class for floating tooltip panels that appear on hover.
/// Handles fade in/out animation, viewport-edge clamping, and debounce integration.
/// Tooltips use absolute positioning outside the layout tree so they don't cause reflow.
/// </summary>
public partial class FloatingTooltip : PanelContainer
{
    private Tween? _fadeTween;
    private bool _isShowingAnimated;
    protected VBoxContainer Content = null!;

    /// <summary>Margin from viewport edges when clamping position.</summary>
    private const float ViewportMargin = 8f;

    /// <summary>Offset from the anchor point to avoid overlapping the hovered element.</summary>
    protected virtual Vector2 AnchorOffset => new(16, 0);

    public FloatingTooltip()
    {
        // Start invisible
        Visible = false;
        Modulate = new Color(1, 1, 1, 0);

        // Don't intercept mouse events — tooltips are display-only
        MouseFilter = MouseFilterEnum.Ignore;

        // High Z-index to render above everything
        ZIndex = 100;

        // Dark semi-transparent panel style (matches ItemDetailPanel)
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.08f, 0.95f),
            BorderColor = new Color(0.5f, 0.45f, 0.35f, 0.7f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
        };
        AddThemeStyleboxOverride("panel", style);

        // Content container for subclasses to populate
        Content = new VBoxContainer();
        Content.AddThemeConstantOverride("separation", 6);
        AddChild(Content);
    }

    /// <summary>
    /// Show the tooltip anchored near the given position, clamped to viewport bounds.
    /// Fades in over GridConstants.TooltipFadeInDuration.
    /// </summary>
    public void ShowAt(Vector2 anchorPosition)
    {
        // Kill any running fade animation
        _fadeTween?.Kill();
        _fadeTween = null;

        // Position with offset, then clamp
        Position = anchorPosition + AnchorOffset;
        ClampToViewport();

        Visible = true;
        _isShowingAnimated = true;

        // Fade in
        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(this, "modulate:a", 1.0f, GridConstants.TooltipFadeInDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    /// <summary>
    /// Hide the tooltip with a fade-out animation.
    /// </summary>
    public void HideAnimated()
    {
        if (!_isShowingAnimated && !Visible) return;

        _fadeTween?.Kill();
        _fadeTween = null;
        _isShowingAnimated = false;

        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(this, "modulate:a", 0.0f, GridConstants.TooltipFadeOutDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        _fadeTween.TweenCallback(Callable.From(() =>
        {
            Visible = false;
        }));
    }

    /// <summary>
    /// Immediately hide without animation.
    /// </summary>
    public void HideImmediate()
    {
        _fadeTween?.Kill();
        _fadeTween = null;
        _isShowingAnimated = false;
        Visible = false;
        Modulate = new Color(1, 1, 1, 0);
    }

    /// <summary>Whether the tooltip is currently shown or fading in.</summary>
    public bool IsShowingOrFading => _isShowingAnimated || Visible;

    /// <summary>
    /// Reposition the tooltip to stay within viewport bounds.
    /// Call after window resize while tooltip is visible.
    /// </summary>
    public void ClampToViewport()
    {
        var viewportSize = GetViewportRect().Size;
        var tooltipSize = GetCombinedMinimumSize();

        var pos = Position;

        // If tooltip goes off the right edge, flip to the left of the anchor
        if (pos.X + tooltipSize.X > viewportSize.X - ViewportMargin)
        {
            pos.X = pos.X - tooltipSize.X - AnchorOffset.X * 2;
        }

        // Clamp to left edge
        if (pos.X < ViewportMargin)
        {
            pos.X = ViewportMargin;
        }

        // If tooltip goes off the bottom, shift up
        if (pos.Y + tooltipSize.Y > viewportSize.Y - ViewportMargin)
        {
            pos.Y = viewportSize.Y - ViewportMargin - tooltipSize.Y;
        }

        // Clamp to top edge
        if (pos.Y < ViewportMargin)
        {
            pos.Y = ViewportMargin;
        }

        Position = pos;
    }
}
