using Godot;
using System.Collections.Generic;
using AlchemistCourier.Core.Puzzle;

namespace AlchemistCourier.Game.Editor;

/// <summary>
/// Panel for editing bags in the puzzle editor.
/// Displays a list of bags with shape editors, position controls, and property toggles.
/// </summary>
public partial class BagEditorPanel : VBoxContainer
{
    private static readonly string[] EditableProperties =
        { "Null", "Gravity", "Locked", "Insulated", "Reinforced", "Fragile" };

    private static readonly Color HeaderColor = new(0.9f, 0.85f, 0.7f);
    private static readonly Color LabelColor = new(0.75f, 0.75f, 0.7f);

    private Label _countLabel = null!;
    private VBoxContainer _bagList = null!;
    private readonly List<BagEntry> _entries = new();

    [Signal]
    public delegate void BagsChangedEventHandler();

    public void Initialize()
    {
        AddThemeConstantOverride("separation", 6);

        // Header
        var header = new Label { Text = "Bags" };
        header.AddThemeColorOverride("font_color", HeaderColor);
        header.AddThemeFontSizeOverride("font_size", 14);
        AddChild(header);

        // Add button row
        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", 8);
        AddChild(toolbar);

        var addBtn = new Button { Text = "Add Bag" };
        addBtn.Pressed += OnAddBag;
        toolbar.AddChild(addBtn);

        _countLabel = new Label { Text = "0 bags" };
        _countLabel.AddThemeColorOverride("font_color", LabelColor);
        _countLabel.AddThemeFontSizeOverride("font_size", 12);
        toolbar.AddChild(_countLabel);

        // Scrollable bag list
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 200),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(scroll);

        _bagList = new VBoxContainer();
        _bagList.AddThemeConstantOverride("separation", 8);
        _bagList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_bagList);

        UpdateCountLabel();
    }

    public List<PuzzleBag> GetBags()
    {
        var bags = new List<PuzzleBag>();
        foreach (var entry in _entries)
            bags.Add(entry.ToPuzzleBag());
        return bags;
    }

    public void SetBags(List<PuzzleBag> bags)
    {
        ClearEntries();
        foreach (var bag in bags)
            AddBagEntry(bag);
        UpdateCountLabel();
    }

    private void OnAddBag()
    {
        // Default: 3x3 bag with all cells active, at position (0,0)
        var defaultShape = new bool[3, 3];
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                defaultShape[r, c] = true;

        var bag = PuzzleBag.FromMatrix(defaultShape, 0, 0);
        AddBagEntry(bag);
        UpdateCountLabel();
        EmitSignal(SignalName.BagsChanged);
    }

    private void AddBagEntry(PuzzleBag bag)
    {
        var entry = new BagEntry();
        entry.Initialize(bag, _entries.Count);
        entry.Changed += OnBagEntryChanged;
        entry.RemoveRequested += () => OnRemoveBag(entry);
        _entries.Add(entry);
        _bagList.AddChild(entry);
    }

    private void OnRemoveBag(BagEntry entry)
    {
        _entries.Remove(entry);
        _bagList.RemoveChild(entry);
        entry.QueueFree();
        // Re-number remaining entries
        for (int i = 0; i < _entries.Count; i++)
            _entries[i].SetIndex(i);
        UpdateCountLabel();
        EmitSignal(SignalName.BagsChanged);
    }

    private void OnBagEntryChanged()
    {
        EmitSignal(SignalName.BagsChanged);
    }

    private void ClearEntries()
    {
        foreach (var entry in _entries)
        {
            _bagList.RemoveChild(entry);
            entry.QueueFree();
        }
        _entries.Clear();
    }

    private void UpdateCountLabel()
    {
        _countLabel.Text = _entries.Count == 1 ? "1 bag" : $"{_entries.Count} bags";
    }

    /// <summary>
    /// A collapsible panel for a single bag, containing shape editor, position, and properties.
    /// </summary>
    private partial class BagEntry : PanelContainer
    {
        private Label _headerLabel = null!;
        private VBoxContainer _body = null!;
        private ShapeEditorWidget _shapeEditor = null!;
        private SpinBox _colSpin = null!;
        private SpinBox _rowSpin = null!;
        private readonly Dictionary<string, CheckBox> _propChecks = new();
        private bool _collapsed;
        private int _index;

        [Signal]
        public delegate void ChangedEventHandler();

        [Signal]
        public delegate void RemoveRequestedEventHandler();

        public void Initialize(PuzzleBag bag, int index)
        {
            _index = index;

            var style = new StyleBoxFlat
            {
                BgColor = new Color(0.12f, 0.12f, 0.15f, 0.95f),
                BorderColor = new Color(0.4f, 0.38f, 0.32f, 0.5f),
                BorderWidthBottom = 1,
                BorderWidthTop = 1,
                BorderWidthLeft = 1,
                BorderWidthRight = 1,
                ContentMarginLeft = 8,
                ContentMarginRight = 8,
                ContentMarginTop = 6,
                ContentMarginBottom = 6,
                CornerRadiusBottomLeft = 3,
                CornerRadiusBottomRight = 3,
                CornerRadiusTopLeft = 3,
                CornerRadiusTopRight = 3,
            };
            AddThemeStyleboxOverride("panel", style);

            var root = new VBoxContainer();
            root.AddThemeConstantOverride("separation", 4);
            AddChild(root);

            // Header row (click to collapse)
            var headerRow = new HBoxContainer();
            headerRow.AddThemeConstantOverride("separation", 8);
            root.AddChild(headerRow);

            _headerLabel = new Label { Text = $"Bag {_index + 1}" };
            _headerLabel.AddThemeColorOverride("font_color", HeaderColor);
            _headerLabel.AddThemeFontSizeOverride("font_size", 13);
            _headerLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            headerRow.AddChild(_headerLabel);

            var collapseBtn = new Button { Text = "▼", CustomMinimumSize = new Vector2(28, 0) };
            collapseBtn.Pressed += ToggleCollapse;
            headerRow.AddChild(collapseBtn);

            var removeBtn = new Button { Text = "Remove" };
            removeBtn.Pressed += () => EmitSignal(SignalName.RemoveRequested);
            headerRow.AddChild(removeBtn);

            // Body (collapsible content)
            _body = new VBoxContainer();
            _body.AddThemeConstantOverride("separation", 6);
            root.AddChild(_body);

            BuildShapeSection(bag);
            BuildPositionSection(bag);
            BuildPropertiesSection(bag);
        }

        public void SetIndex(int index)
        {
            _index = index;
            _headerLabel.Text = $"Bag {_index + 1}";
        }

        public PuzzleBag ToPuzzleBag()
        {
            var shape = _shapeEditor.GetShape();
            var props = new List<string>();
            foreach (var (name, check) in _propChecks)
            {
                if (check.ButtonPressed)
                    props.Add(name);
            }
            return PuzzleBag.FromMatrix(shape, (int)_colSpin.Value, (int)_rowSpin.Value, props);
        }

        private void ToggleCollapse()
        {
            _collapsed = !_collapsed;
            _body.Visible = !_collapsed;
        }

        private void BuildShapeSection(PuzzleBag bag)
        {
            var label = new Label { Text = "Shape" };
            label.AddThemeColorOverride("font_color", LabelColor);
            label.AddThemeFontSizeOverride("font_size", 12);
            _body.AddChild(label);

            _shapeEditor = new ShapeEditorWidget();
            _shapeEditor.Initialize(maxRows: 6, maxCols: 6, cellPixelSize: 20);
            var shapeMatrix = bag.ToShapeMatrix();
            if (shapeMatrix.GetLength(0) > 0)
                _shapeEditor.SetShape(shapeMatrix);
            _shapeEditor.ShapeChanged += () => EmitSignal(SignalName.Changed);
            _body.AddChild(_shapeEditor);
        }

        private void BuildPositionSection(PuzzleBag bag)
        {
            var label = new Label { Text = "Position" };
            label.AddThemeColorOverride("font_color", LabelColor);
            label.AddThemeFontSizeOverride("font_size", 12);
            _body.AddChild(label);

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            _body.AddChild(row);

            var colLabel = new Label { Text = "Col:" };
            colLabel.AddThemeColorOverride("font_color", LabelColor);
            colLabel.AddThemeFontSizeOverride("font_size", 11);
            row.AddChild(colLabel);

            _colSpin = new SpinBox
            {
                MinValue = 0,
                MaxValue = 20,
                Step = 1,
                Value = bag.PositionCol,
                CustomMinimumSize = new Vector2(60, 0),
            };
            _colSpin.ValueChanged += _ => EmitSignal(SignalName.Changed);
            row.AddChild(_colSpin);

            var rowLabel = new Label { Text = "Row:" };
            rowLabel.AddThemeColorOverride("font_color", LabelColor);
            rowLabel.AddThemeFontSizeOverride("font_size", 11);
            row.AddChild(rowLabel);

            _rowSpin = new SpinBox
            {
                MinValue = 0,
                MaxValue = 20,
                Step = 1,
                Value = bag.PositionRow,
                CustomMinimumSize = new Vector2(60, 0),
            };
            _rowSpin.ValueChanged += _ => EmitSignal(SignalName.Changed);
            row.AddChild(_rowSpin);
        }

        private void BuildPropertiesSection(PuzzleBag bag)
        {
            var label = new Label { Text = "Properties" };
            label.AddThemeColorOverride("font_color", LabelColor);
            label.AddThemeFontSizeOverride("font_size", 12);
            _body.AddChild(label);

            var grid = new GridContainer { Columns = 2 };
            grid.AddThemeConstantOverride("h_separation", 12);
            grid.AddThemeConstantOverride("v_separation", 2);
            _body.AddChild(grid);

            foreach (var prop in EditableProperties)
            {
                var check = new CheckBox { Text = prop };
                check.ButtonPressed = bag.Properties.Contains(prop);
                check.Toggled += _ => EmitSignal(SignalName.Changed);
                grid.AddChild(check);
                _propChecks[prop] = check;
            }
        }
    }
}
