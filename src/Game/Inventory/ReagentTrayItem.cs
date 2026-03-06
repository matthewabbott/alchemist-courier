using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Game.Inventory;

public partial class ReagentTrayItem : PanelContainer
{
    private Reagent _reagent = null!;
    private int _index;
    private bool _isSelected;

    public int ReagentIndex => _index;
    public Reagent Reagent => _reagent;
    private ShapePreview _preview = null!;
    private Label _label = null!;
    private StyleBoxFlat _normalStyle = null!;
    private StyleBoxFlat _selectedStyle = null!;
    private ColorRect? _hintDot;

    [Signal]
    public delegate void ClickedEventHandler(int index);

    [Signal]
    public delegate void HoveredEventHandler(int index);

    [Signal]
    public delegate void UnhoveredEventHandler(int index);

    public void Initialize(Reagent reagent, int index)
    {
        _reagent = reagent;
        _index = index;

        // Styles
        _normalStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.2f, 0.25f),
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
        };
        _selectedStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.25f, 0.35f, 0.25f),
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderColor = new Color(0.4f, 0.8f, 0.4f),
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
        };
        AddThemeStyleboxOverride("panel", _normalStyle);

        var hbox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill };
        AddChild(hbox);

        // Mini shape preview
        _preview = new ShapePreview();
        _preview.SetReagent(reagent);
        hbox.AddChild(_preview);

        // Spacer
        var spacer = new Control { CustomMinimumSize = new Vector2(8, 0) };
        hbox.AddChild(spacer);

        // Name label
        _label = new Label
        {
            Text = reagent.Definition.Name,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        _label.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        hbox.AddChild(_label);

        // Hint indicator dot (hidden by default, shown for snap-back availability)
        _hintDot = new ColorRect
        {
            CustomMinimumSize = new Vector2(6, 6),
            Color = GridConstants.HintedBorderColor,
            Visible = false,
        };
        var hintDotCenter = new CenterContainer { CustomMinimumSize = new Vector2(10, 8) };
        hintDotCenter.AddChild(_hintDot);
        hbox.AddChild(hintDotCenter);

        // Aspect color dots — small colored squares showing which aspects the reagent has
        var aspectDots = BuildAspectDots(reagent);
        if (aspectDots != null)
        {
            hbox.AddChild(new Control { CustomMinimumSize = new Vector2(4, 0) });
            hbox.AddChild(aspectDots);
        }

        MouseDefaultCursorShape = CursorShape.PointingHand;
        MouseEntered += () => EmitSignal(SignalName.Hovered, _index);
        MouseExited += () => EmitSignal(SignalName.Unhovered, _index);
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        AddThemeStyleboxOverride("panel", selected ? _selectedStyle : _normalStyle);
    }

    public void SetHintIndicator(bool wasHinted)
    {
        if (_hintDot == null) return;
        _hintDot.Visible = wasHinted;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            EmitSignal(SignalName.Clicked, _index);
            AcceptEvent();
        }
    }

    private static HBoxContainer? BuildAspectDots(Reagent reagent)
    {
        // Collect unique aspect IDs across all cells
        var seen = new HashSet<string>();
        var uniqueIds = new List<string>();
        var cellPositions = reagent.GetLocalCellPositions();
        foreach (var pos in cellPositions)
        {
            var aspects = reagent.GetAspectsAtLocalPosition(pos);
            foreach (var aspect in aspects.Aspects)
            {
                if (seen.Add(aspect.Id))
                    uniqueIds.Add(aspect.Id);
            }
        }

        if (uniqueIds.Count == 0) return null;

        var hbox = new HBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        hbox.AddThemeConstantOverride("separation", 2);

        foreach (var id in uniqueIds)
        {
            var dot = new ColorRect
            {
                CustomMinimumSize = new Vector2(6, 6),
                Color = GridConstants.GetAspectColor(id),
            };
            var center = new CenterContainer { CustomMinimumSize = new Vector2(6, 8) };
            center.AddChild(dot);
            hbox.AddChild(center);
        }

        return hbox;
    }
}

/// <summary>
/// Draws a small shape preview for a reagent (8px per cell).
/// </summary>
public partial class ShapePreview : Control
{
    private const int MiniCellSize = 8;
    private Reagent? _reagent;

    public void SetReagent(Reagent reagent)
    {
        _reagent = reagent;
        var shape = reagent.Definition.Shape;
        int cols = shape.GetLength(1);
        int rows = shape.GetLength(0);
        CustomMinimumSize = new Vector2(cols * MiniCellSize, rows * MiniCellSize);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_reagent == null) return;

        var shape = _reagent.Definition.Shape;
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);
        var cellPositions = _reagent.Definition.GetLocalCellPositions();

        for (int i = 0; i < cellPositions.Count; i++)
        {
            var pos = cellPositions[i];
            var aspects = _reagent.Definition.AspectsPerCell;
            Color color;
            if (i < aspects.Count && !aspects[i].IsEmpty)
            {
                color = GridConstants.GetAspectColor(aspects[i].Aspects[0].Id);
            }
            else
            {
                color = new Color(0.6f, 0.55f, 0.5f);
            }

            var rect = new Rect2(pos.Col * MiniCellSize, pos.Row * MiniCellSize, MiniCellSize, MiniCellSize);
            DrawRect(rect, color);
            DrawRect(rect, new Color(0, 0, 0, 0.3f), false, 1);
        }
    }
}
