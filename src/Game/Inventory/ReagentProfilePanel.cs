using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Game.Visual;

namespace AlchemistCourier.Game.Inventory;

/// <summary>
/// Popup panel showing detailed reagent information.
/// Appears on click, dismissed by clicking outside or pressing Escape.
/// </summary>
public partial class ReagentProfilePanel : PanelContainer
{
    private Tween? _fadeTween;
    private VBoxContainer _content = null!;
    private ProfileShapePreview _shapePreview = null!;
    private Label _nameLabel = null!;
    private Label _flavorLabel = null!;
    private VBoxContainer _aspectList = null!;

    private const float ViewportMargin = 8f;

    /// <summary>The reagent currently being displayed.</summary>
    public Reagent? CurrentReagent { get; private set; }

    /// <summary>Whether the profile panel is currently visible.</summary>
    public bool IsShowing => Visible && Modulate.A > 0;

    public ReagentProfilePanel()
    {
        Visible = false;
        Modulate = new Color(1, 1, 1, 0);

        // Receive input so clicks on the panel don't dismiss it
        MouseFilter = MouseFilterEnum.Stop;

        ZIndex = 100;

        CustomMinimumSize = new Vector2(240, 0);

        // Dark semi-transparent panel style (matches FloatingTooltip)
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

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 6);
        AddChild(_content);

        // Shape preview (24px per cell)
        _shapePreview = new ProfileShapePreview();
        _shapePreview.Visible = false;
        _content.AddChild(_shapePreview);

        // Name label (16pt)
        _nameLabel = new Label();
        _nameLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.95f, 0.95f));
        _nameLabel.AddThemeFontSizeOverride("font_size", 16);
        _nameLabel.Visible = false;
        _content.AddChild(_nameLabel);

        // Flavor text (12pt italic)
        _flavorLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(220, 0),
        };
        _flavorLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.55f));
        _flavorLabel.AddThemeFontSizeOverride("font_size", 12);
        _flavorLabel.Visible = false;
        _content.AddChild(_flavorLabel);

        // Aspect breakdown list
        _aspectList = new VBoxContainer();
        _aspectList.AddThemeConstantOverride("separation", 4);
        _aspectList.Visible = false;
        _content.AddChild(_aspectList);
    }

    /// <summary>
    /// Show the profile panel for a reagent, positioned near the given global position.
    /// </summary>
    public void ShowForReagent(Reagent reagent, Vector2 globalPosition)
    {
        CurrentReagent = reagent;

        // Shape preview
        _shapePreview.SetReagent(reagent);
        _shapePreview.Visible = true;

        // Name
        _nameLabel.Text = reagent.Definition.Name;
        _nameLabel.Visible = true;

        // Flavor text
        if (!string.IsNullOrEmpty(reagent.Definition.FlavorText))
        {
            _flavorLabel.Text = reagent.Definition.FlavorText;
            _flavorLabel.Visible = true;
        }
        else
        {
            _flavorLabel.Visible = false;
        }

        // Aspect breakdown with descriptions
        BuildAspectBreakdown(reagent);
        _aspectList.Visible = true;

        // Position near click, clamped to viewport
        Position = globalPosition;
        CallDeferred(MethodName.ClampToViewport);

        // Fade in
        _fadeTween?.Kill();
        _fadeTween = null;
        Visible = true;

        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(this, "modulate:a", 1.0f, GridConstants.TooltipFadeInDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    /// <summary>
    /// Hide the profile panel with a fade-out animation.
    /// </summary>
    public void HideProfile()
    {
        if (!Visible) return;

        _fadeTween?.Kill();
        _fadeTween = null;

        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(this, "modulate:a", 0.0f, GridConstants.TooltipFadeOutDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        _fadeTween.TweenCallback(Callable.From(() =>
        {
            Visible = false;
            CurrentReagent = null;
        }));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsShowing) return;

        // Dismiss on Escape
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            HideProfile();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Dismiss on click outside
        if (@event is InputEventMouseButton { Pressed: true } click)
        {
            var localPos = click.GlobalPosition - GlobalPosition;
            var panelRect = new Rect2(Vector2.Zero, Size);
            if (!panelRect.HasPoint(localPos))
            {
                HideProfile();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void ClampToViewport()
    {
        var viewportSize = GetViewportRect().Size;
        var panelSize = GetCombinedMinimumSize();

        var pos = Position;

        if (pos.X + panelSize.X > viewportSize.X - ViewportMargin)
            pos.X = viewportSize.X - ViewportMargin - panelSize.X;

        if (pos.X < ViewportMargin)
            pos.X = ViewportMargin;

        if (pos.Y + panelSize.Y > viewportSize.Y - ViewportMargin)
            pos.Y = viewportSize.Y - ViewportMargin - panelSize.Y;

        if (pos.Y < ViewportMargin)
            pos.Y = ViewportMargin;

        Position = pos;
    }

    private void BuildAspectBreakdown(Reagent reagent)
    {
        // Clear existing entries
        foreach (var child in _aspectList.GetChildren())
        {
            child.QueueFree();
        }

        // Aggregate aspects across all cells
        var aspectTotals = new Dictionary<string, (Aspect aspect, int totalMagnitude)>();
        var cellPositions = reagent.GetLocalCellPositions();
        foreach (var pos in cellPositions)
        {
            var aspects = reagent.GetAspectsAtLocalPosition(pos);
            foreach (var aspect in aspects.Aspects)
            {
                if (aspectTotals.TryGetValue(aspect.Id, out var existing))
                {
                    aspectTotals[aspect.Id] = (existing.aspect, existing.totalMagnitude + aspect.Magnitude);
                }
                else
                {
                    aspectTotals[aspect.Id] = (aspect, aspect.Magnitude);
                }
            }
        }

        foreach (var (id, (aspect, total)) in aspectTotals)
        {
            var entry = new VBoxContainer();
            entry.AddThemeConstantOverride("separation", 2);

            // Header row: icon + name + magnitude
            var headerRow = new HBoxContainer();
            headerRow.AddThemeConstantOverride("separation", 6);

            var icon = UIThemeLoader.CreateAspectIcon(id, 16);
            var iconCenter = new CenterContainer { CustomMinimumSize = new Vector2(16, 18) };
            iconCenter.AddChild(icon);
            headerRow.AddChild(iconCenter);

            var nameLabel = new Label
            {
                Text = $"{aspect.Name} x{total}",
            };
            nameLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.75f));
            nameLabel.AddThemeFontSizeOverride("font_size", 12);
            headerRow.AddChild(nameLabel);

            entry.AddChild(headerRow);

            // Description from metadata database
            var metadata = AspectMetadataDatabase.Get(id);
            if (metadata != null)
            {
                var descLabel = new Label
                {
                    Text = metadata.Description,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    CustomMinimumSize = new Vector2(210, 0),
                };
                descLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.5f));
                descLabel.AddThemeFontSizeOverride("font_size", 10);
                entry.AddChild(descLabel);

                // Constraint info if present
                if (!string.IsNullOrEmpty(metadata.ConstraintText))
                {
                    var constraintLabel = new Label
                    {
                        Text = metadata.ConstraintText,
                        AutowrapMode = TextServer.AutowrapMode.WordSmart,
                        CustomMinimumSize = new Vector2(210, 0),
                    };
                    constraintLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.65f, 0.3f));
                    constraintLabel.AddThemeFontSizeOverride("font_size", 10);
                    entry.AddChild(constraintLabel);
                }
            }

            _aspectList.AddChild(entry);
        }
    }
}

/// <summary>
/// Enlarged shape preview for the profile panel (24px per cell).
/// </summary>
public partial class ProfileShapePreview : Control
{
    private const int CellSize = 24;
    private Reagent? _reagent;

    public void SetReagent(Reagent reagent)
    {
        _reagent = reagent;
        var shape = reagent.Definition.Shape;
        int cols = shape.GetLength(1);
        int rows = shape.GetLength(0);
        CustomMinimumSize = new Vector2(cols * CellSize, rows * CellSize);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_reagent == null) return;

        var cellPositions = _reagent.Definition.GetLocalCellPositions();
        var aspects = _reagent.Definition.AspectsPerCell;

        for (int i = 0; i < cellPositions.Count; i++)
        {
            var pos = cellPositions[i];
            var rect = new Rect2(pos.Col * CellSize, pos.Row * CellSize, CellSize, CellSize);

            string? aspectId = null;
            if (i < aspects.Count && !aspects[i].IsEmpty)
                aspectId = aspects[i].Aspects[0].Id;

            if (aspectId != null)
            {
                var texture = UIThemeLoader.GetAspectTileTexture(aspectId);
                if (texture != null)
                {
                    DrawTextureRect(texture, rect, false);
                    continue;
                }
            }

            var color = aspectId != null ? GridConstants.GetAspectColor(aspectId) : new Color(0.6f, 0.55f, 0.5f);
            DrawRect(rect, color);
            DrawRect(rect, new Color(0, 0, 0, 0.3f), false, 1);
        }
    }
}
