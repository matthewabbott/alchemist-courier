using Godot;
using CoreVec = AlchemistCourier.Core.Grid.Vector2I;

namespace AlchemistCourier.Game.Editor;

/// <summary>
/// A Godot Control for visually editing bool[,] shape matrices.
/// Displays an NxN grid of clickable cells that toggle on/off.
/// </summary>
public partial class ShapeEditorWidget : Control
{
    private const int DefaultMaxRows = 6;
    private const int DefaultMaxCols = 6;
    private const int DefaultCellPixelSize = 24;
    private const int GridLineWidth = 1;

    private static readonly Color ActiveCellColor = new(0.9f, 0.9f, 0.95f);
    private static readonly Color InactiveCellColor = new(0.2f, 0.2f, 0.25f);
    private static readonly Color HoverColor = new(0.5f, 0.5f, 0.55f);
    private static readonly Color LineColor = new(0.35f, 0.35f, 0.4f);

    private int _maxRows;
    private int _maxCols;
    private int _cellPixelSize;
    private bool[,] _shape = null!;
    private ColorRect[,] _cellRects = null!;
    private CoreVec? _hoverPos;

    [Signal]
    public delegate void ShapeChangedEventHandler();

    public void Initialize(int maxRows = DefaultMaxRows, int maxCols = DefaultMaxCols, int cellPixelSize = DefaultCellPixelSize)
    {
        _maxRows = maxRows;
        _maxCols = maxCols;
        _cellPixelSize = cellPixelSize;
        _shape = new bool[_maxRows, _maxCols];
        _cellRects = new ColorRect[_maxCols, _maxRows];

        int pixelWidth = _maxCols * _cellPixelSize;
        int pixelHeight = _maxRows * _cellPixelSize;
        CustomMinimumSize = new Vector2(pixelWidth, pixelHeight);
        Size = new Vector2(pixelWidth, pixelHeight);

        CreateCells();
        CreateGridLines();
        RefreshDisplay();
    }

    public void SetShape(bool[,] shape)
    {
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);
        _shape = new bool[_maxRows, _maxCols];
        for (int r = 0; r < rows && r < _maxRows; r++)
        {
            for (int c = 0; c < cols && c < _maxCols; c++)
            {
                _shape[r, c] = shape[r, c];
            }
        }
        RefreshDisplay();
    }

    public bool[,] GetShape()
    {
        // Return a trimmed copy — find the bounding box of true cells
        int minRow = _maxRows, maxRow = -1, minCol = _maxCols, maxCol = -1;
        for (int r = 0; r < _maxRows; r++)
        {
            for (int c = 0; c < _maxCols; c++)
            {
                if (_shape[r, c])
                {
                    if (r < minRow) minRow = r;
                    if (r > maxRow) maxRow = r;
                    if (c < minCol) minCol = c;
                    if (c > maxCol) maxCol = c;
                }
            }
        }

        if (maxRow < 0) return new bool[0, 0];

        int rows = maxRow - minRow + 1;
        int cols = maxCol - minCol + 1;
        var result = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                result[r, c] = _shape[minRow + r, minCol + c];
            }
        }
        return result;
    }

    public void Clear()
    {
        _shape = new bool[_maxRows, _maxCols];
        RefreshDisplay();
        EmitSignal(SignalName.ShapeChanged);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            HandleMouseMotion();
        }
        else if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed
                 && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            HandleClick();
        }
    }

    private void HandleMouseMotion()
    {
        var localPos = GetLocalMousePosition();
        var gridPos = PixelToGrid(localPos);

        bool inside = localPos.X >= 0 && localPos.Y >= 0
                   && localPos.X < Size.X && localPos.Y < Size.Y
                   && gridPos.Col >= 0 && gridPos.Col < _maxCols
                   && gridPos.Row >= 0 && gridPos.Row < _maxRows;

        if (inside)
        {
            if (_hoverPos == null || _hoverPos.Value.Col != gridPos.Col || _hoverPos.Value.Row != gridPos.Row)
            {
                var prevHover = _hoverPos;
                _hoverPos = gridPos;
                if (prevHover != null)
                    UpdateCellColor(prevHover.Value.Col, prevHover.Value.Row);
                UpdateCellColor(gridPos.Col, gridPos.Row);
            }
        }
        else if (_hoverPos != null)
        {
            var prev = _hoverPos.Value;
            _hoverPos = null;
            UpdateCellColor(prev.Col, prev.Row);
        }
    }

    private void HandleClick()
    {
        if (_hoverPos == null) return;
        int col = _hoverPos.Value.Col;
        int row = _hoverPos.Value.Row;
        _shape[row, col] = !_shape[row, col];
        UpdateCellColor(col, row);
        EmitSignal(SignalName.ShapeChanged);
    }

    private void CreateCells()
    {
        for (int row = 0; row < _maxRows; row++)
        {
            for (int col = 0; col < _maxCols; col++)
            {
                var rect = new ColorRect
                {
                    Position = new Vector2(col * _cellPixelSize, row * _cellPixelSize),
                    Size = new Vector2(_cellPixelSize, _cellPixelSize),
                    Color = InactiveCellColor,
                };
                AddChild(rect);
                _cellRects[col, row] = rect;
            }
        }
    }

    private void CreateGridLines()
    {
        int pixelWidth = _maxCols * _cellPixelSize;
        int pixelHeight = _maxRows * _cellPixelSize;

        for (int row = 0; row <= _maxRows; row++)
        {
            var line = new ColorRect
            {
                Position = new Vector2(0, row * _cellPixelSize),
                Size = new Vector2(pixelWidth, GridLineWidth),
                Color = LineColor,
                ZIndex = 1,
            };
            AddChild(line);
        }

        for (int col = 0; col <= _maxCols; col++)
        {
            var line = new ColorRect
            {
                Position = new Vector2(col * _cellPixelSize, 0),
                Size = new Vector2(GridLineWidth, pixelHeight),
                Color = LineColor,
                ZIndex = 1,
            };
            AddChild(line);
        }
    }

    private void RefreshDisplay()
    {
        for (int row = 0; row < _maxRows; row++)
        {
            for (int col = 0; col < _maxCols; col++)
            {
                UpdateCellColor(col, row);
            }
        }
    }

    private void UpdateCellColor(int col, int row)
    {
        bool isActive = _shape[row, col];
        bool isHovered = _hoverPos != null && _hoverPos.Value.Col == col && _hoverPos.Value.Row == row;

        Color color;
        if (isHovered && !isActive)
            color = HoverColor;
        else if (isActive)
            color = ActiveCellColor;
        else
            color = InactiveCellColor;

        _cellRects[col, row].Color = color;
    }

    private CoreVec PixelToGrid(Vector2 pixel)
        => new((int)(pixel.X / _cellPixelSize), (int)(pixel.Y / _cellPixelSize));
}
