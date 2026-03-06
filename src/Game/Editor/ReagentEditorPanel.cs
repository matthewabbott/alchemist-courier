using Godot;
using System.Collections.Generic;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Puzzle;

namespace AlchemistCourier.Game.Editor;

/// <summary>
/// Panel for editing reagents in the puzzle editor.
/// Displays a list of reagents with shape editors, per-cell aspect pickers, and property controls.
/// </summary>
public partial class ReagentEditorPanel : VBoxContainer
{
    private static readonly Color HeaderColor = new(0.9f, 0.85f, 0.7f);
    private static readonly Color LabelColor = new(0.75f, 0.75f, 0.7f);

    /// <summary>All available aspect IDs for the dropdown, including "None".</summary>
    private static readonly string[] AvailableAspects =
    {
        "", // None
        AspectIds.Hot,
        AspectIds.Cold,
        AspectIds.Volatile,
        AspectIds.Stable,
        AspectIds.Heavy,
        AspectIds.Toxic,
        AspectIds.Sharp,
        AspectIds.Fragile,
        AspectIds.Catalytic,
        AspectIds.Medicinal,
    };

    private Label _countLabel = null!;
    private VBoxContainer _reagentList = null!;
    private readonly List<ReagentEntry> _entries = new();

    [Signal]
    public delegate void ReagentsChangedEventHandler();

    public void Initialize()
    {
        AddThemeConstantOverride("separation", 6);

        // Header
        var header = new Label { Text = "Reagents" };
        header.AddThemeColorOverride("font_color", HeaderColor);
        header.AddThemeFontSizeOverride("font_size", 14);
        AddChild(header);

        // Add button row
        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", 8);
        AddChild(toolbar);

        var addBtn = new Button { Text = "Add Reagent" };
        addBtn.Pressed += OnAddReagent;
        toolbar.AddChild(addBtn);

        _countLabel = new Label { Text = "0 reagents" };
        _countLabel.AddThemeColorOverride("font_color", LabelColor);
        _countLabel.AddThemeFontSizeOverride("font_size", 12);
        toolbar.AddChild(_countLabel);

        // Scrollable reagent list
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 200),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(scroll);

        _reagentList = new VBoxContainer();
        _reagentList.AddThemeConstantOverride("separation", 8);
        _reagentList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_reagentList);

        UpdateCountLabel();
    }

    public List<PuzzleReagent> GetReagents()
    {
        var reagents = new List<PuzzleReagent>();
        foreach (var entry in _entries)
            reagents.Add(entry.ToPuzzleReagent());
        return reagents;
    }

    public void SetReagents(List<PuzzleReagent> reagents)
    {
        ClearEntries();
        foreach (var reagent in reagents)
            AddReagentEntry(reagent);
        UpdateCountLabel();
    }

    private void OnAddReagent()
    {
        // Default: 1x1 reagent with one cell active, no aspects
        var defaultShape = new bool[1, 1];
        defaultShape[0, 0] = true;

        var reagent = PuzzleReagent.FromMatrix(defaultShape);
        AddReagentEntry(reagent);
        UpdateCountLabel();
        EmitSignal(SignalName.ReagentsChanged);
    }

    private void AddReagentEntry(PuzzleReagent reagent)
    {
        var entry = new ReagentEntry();
        entry.Initialize(reagent, _entries.Count);
        entry.Changed += OnReagentEntryChanged;
        entry.RemoveRequested += () => OnRemoveReagent(entry);
        _entries.Add(entry);
        _reagentList.AddChild(entry);
    }

    private void OnRemoveReagent(ReagentEntry entry)
    {
        _entries.Remove(entry);
        _reagentList.RemoveChild(entry);
        entry.QueueFree();
        for (int i = 0; i < _entries.Count; i++)
            _entries[i].SetIndex(i);
        UpdateCountLabel();
        EmitSignal(SignalName.ReagentsChanged);
    }

    private void OnReagentEntryChanged()
    {
        EmitSignal(SignalName.ReagentsChanged);
    }

    private void ClearEntries()
    {
        foreach (var entry in _entries)
        {
            _reagentList.RemoveChild(entry);
            entry.QueueFree();
        }
        _entries.Clear();
    }

    private void UpdateCountLabel()
    {
        _countLabel.Text = _entries.Count == 1 ? "1 reagent" : $"{_entries.Count} reagents";
    }

    /// <summary>
    /// A collapsible panel for a single reagent, containing shape editor,
    /// per-cell aspect pickers, weight/value controls, and IsLocked toggle.
    /// </summary>
    private partial class ReagentEntry : PanelContainer
    {
        private Label _headerLabel = null!;
        private VBoxContainer _body = null!;
        private ShapeEditorWidget _shapeEditor = null!;
        private VBoxContainer _aspectContainer = null!;
        private SpinBox _weightSpin = null!;
        private SpinBox _valueSpin = null!;
        private CheckBox _lockedCheck = null!;
        private bool _collapsed;
        private int _index;
        private readonly List<AspectCellRow> _aspectRows = new();

        [Signal]
        public delegate void ChangedEventHandler();

        [Signal]
        public delegate void RemoveRequestedEventHandler();

        public void Initialize(PuzzleReagent reagent, int index)
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

            _headerLabel = new Label { Text = $"Reagent {_index + 1}" };
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

            BuildShapeSection(reagent);
            BuildAspectSection(reagent);
            BuildPropertiesSection(reagent);
        }

        public void SetIndex(int index)
        {
            _index = index;
            _headerLabel.Text = $"Reagent {_index + 1}";
        }

        public PuzzleReagent ToPuzzleReagent()
        {
            var shape = _shapeEditor.GetShape();
            var aspectsPerCell = new List<List<string>>();
            foreach (var row in _aspectRows)
            {
                aspectsPerCell.Add(row.GetAspects());
            }
            return PuzzleReagent.FromMatrix(
                shape,
                aspectsPerCell,
                (float)_weightSpin.Value,
                (int)_valueSpin.Value,
                _lockedCheck.ButtonPressed);
        }

        private void ToggleCollapse()
        {
            _collapsed = !_collapsed;
            _body.Visible = !_collapsed;
        }

        private void BuildShapeSection(PuzzleReagent reagent)
        {
            var label = new Label { Text = "Shape" };
            label.AddThemeColorOverride("font_color", LabelColor);
            label.AddThemeFontSizeOverride("font_size", 12);
            _body.AddChild(label);

            _shapeEditor = new ShapeEditorWidget();
            _shapeEditor.Initialize(maxRows: 4, maxCols: 4, cellPixelSize: 20);
            var shapeMatrix = reagent.ToShapeMatrix();
            if (shapeMatrix.GetLength(0) > 0)
                _shapeEditor.SetShape(shapeMatrix);
            _shapeEditor.ShapeChanged += OnShapeChanged;
            _body.AddChild(_shapeEditor);
        }

        private void BuildAspectSection(PuzzleReagent reagent)
        {
            var label = new Label { Text = "Aspects (per cell)" };
            label.AddThemeColorOverride("font_color", LabelColor);
            label.AddThemeFontSizeOverride("font_size", 12);
            _body.AddChild(label);

            _aspectContainer = new VBoxContainer();
            _aspectContainer.AddThemeConstantOverride("separation", 2);
            _body.AddChild(_aspectContainer);

            // Count true cells in the shape
            var shapeMatrix = reagent.ToShapeMatrix();
            int trueCellCount = CountTrueCells(shapeMatrix);

            for (int i = 0; i < trueCellCount; i++)
            {
                var aspects = (i < reagent.AspectsPerCell.Count)
                    ? reagent.AspectsPerCell[i]
                    : new List<string>();
                AddAspectRow(i, aspects);
            }
        }

        private void BuildPropertiesSection(PuzzleReagent reagent)
        {
            var label = new Label { Text = "Properties" };
            label.AddThemeColorOverride("font_color", LabelColor);
            label.AddThemeFontSizeOverride("font_size", 12);
            _body.AddChild(label);

            var propsRow = new HBoxContainer();
            propsRow.AddThemeConstantOverride("separation", 12);
            _body.AddChild(propsRow);

            // Weight
            var weightLabel = new Label { Text = "Weight:" };
            weightLabel.AddThemeColorOverride("font_color", LabelColor);
            weightLabel.AddThemeFontSizeOverride("font_size", 11);
            propsRow.AddChild(weightLabel);

            _weightSpin = new SpinBox
            {
                MinValue = 0,
                MaxValue = 100,
                Step = 0.5,
                Value = reagent.Weight,
                CustomMinimumSize = new Vector2(60, 0),
            };
            _weightSpin.ValueChanged += _ => EmitSignal(SignalName.Changed);
            propsRow.AddChild(_weightSpin);

            // Value
            var valueLabel = new Label { Text = "Value:" };
            valueLabel.AddThemeColorOverride("font_color", LabelColor);
            valueLabel.AddThemeFontSizeOverride("font_size", 11);
            propsRow.AddChild(valueLabel);

            _valueSpin = new SpinBox
            {
                MinValue = 0,
                MaxValue = 100,
                Step = 1,
                Value = reagent.Value,
                CustomMinimumSize = new Vector2(60, 0),
            };
            _valueSpin.ValueChanged += _ => EmitSignal(SignalName.Changed);
            propsRow.AddChild(_valueSpin);

            // Locked
            _lockedCheck = new CheckBox { Text = "Locked" };
            _lockedCheck.ButtonPressed = reagent.IsLocked;
            _lockedCheck.Toggled += _ => EmitSignal(SignalName.Changed);
            propsRow.AddChild(_lockedCheck);
        }

        private void OnShapeChanged()
        {
            SyncAspectRowsToShape();
            EmitSignal(SignalName.Changed);
        }

        private void SyncAspectRowsToShape()
        {
            var shape = _shapeEditor.GetShape();
            int trueCellCount = CountTrueCells(shape);

            // Add rows if we have fewer than needed
            while (_aspectRows.Count < trueCellCount)
            {
                AddAspectRow(_aspectRows.Count, new List<string>());
            }

            // Remove rows if we have more than needed
            while (_aspectRows.Count > trueCellCount)
            {
                var lastRow = _aspectRows[^1];
                _aspectRows.RemoveAt(_aspectRows.Count - 1);
                _aspectContainer.RemoveChild(lastRow);
                lastRow.QueueFree();
            }

            // Update labels
            for (int i = 0; i < _aspectRows.Count; i++)
                _aspectRows[i].SetCellIndex(i);
        }

        private void AddAspectRow(int cellIndex, List<string> aspects)
        {
            var row = new AspectCellRow();
            row.Initialize(cellIndex, aspects);
            row.Changed += () => EmitSignal(SignalName.Changed);
            _aspectRows.Add(row);
            _aspectContainer.AddChild(row);
        }

        private static int CountTrueCells(bool[,] shape)
        {
            int count = 0;
            int rows = shape.GetLength(0);
            int cols = shape.GetLength(1);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (shape[r, c])
                        count++;
            return count;
        }
    }

    /// <summary>
    /// A row representing one true cell in a reagent's shape, with an aspect dropdown.
    /// </summary>
    private partial class AspectCellRow : HBoxContainer
    {
        private Label _cellLabel = null!;
        private OptionButton _aspectPicker = null!;

        [Signal]
        public delegate void ChangedEventHandler();

        public void Initialize(int cellIndex, List<string> aspects)
        {
            AddThemeConstantOverride("separation", 6);

            _cellLabel = new Label { Text = $"Cell {cellIndex}:" };
            _cellLabel.AddThemeColorOverride("font_color", LabelColor);
            _cellLabel.AddThemeFontSizeOverride("font_size", 11);
            _cellLabel.CustomMinimumSize = new Vector2(50, 0);
            AddChild(_cellLabel);

            _aspectPicker = new OptionButton
            {
                CustomMinimumSize = new Vector2(100, 0),
            };

            // Populate dropdown
            for (int i = 0; i < AvailableAspects.Length; i++)
            {
                string aspectId = AvailableAspects[i];
                string displayName = string.IsNullOrEmpty(aspectId) ? "(None)" : GetAspectDisplayName(aspectId);
                _aspectPicker.AddItem(displayName, i);
            }

            // Set current selection
            string currentAspect = (aspects.Count > 0) ? aspects[0] : "";
            int selectedIndex = System.Array.IndexOf(AvailableAspects, currentAspect);
            if (selectedIndex < 0) selectedIndex = 0;
            _aspectPicker.Selected = selectedIndex;

            _aspectPicker.ItemSelected += _ => EmitSignal(SignalName.Changed);
            AddChild(_aspectPicker);
        }

        public void SetCellIndex(int cellIndex)
        {
            _cellLabel.Text = $"Cell {cellIndex}:";
        }

        public List<string> GetAspects()
        {
            int selected = _aspectPicker.Selected;
            if (selected < 0 || selected >= AvailableAspects.Length)
                return new List<string>();

            string aspectId = AvailableAspects[selected];
            if (string.IsNullOrEmpty(aspectId))
                return new List<string>();

            return new List<string> { aspectId };
        }

        private static string GetAspectDisplayName(string aspectId)
        {
            var metadata = AspectMetadataDatabase.Get(aspectId);
            return metadata?.Name ?? aspectId;
        }
    }
}
