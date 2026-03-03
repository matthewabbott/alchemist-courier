using Godot;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Core.Grid;

namespace AlchemistCourier.Game.Inventory;

public partial class ReagentTrayItem : PanelContainer
{
    private Reagent _reagent = null!;
    private int _index;
    private bool _isSelected;

    public int ReagentIndex => _index;
    private ShapePreview _preview = null!;
    private Label _label = null!;
    private StyleBoxFlat _normalStyle = null!;
    private StyleBoxFlat _selectedStyle = null!;

    [Signal]
    public delegate void ClickedEventHandler(int index);

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

        MouseDefaultCursorShape = CursorShape.PointingHand;
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        AddThemeStyleboxOverride("panel", selected ? _selectedStyle : _normalStyle);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            EmitSignal(SignalName.Clicked, _index);
            AcceptEvent();
        }
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
