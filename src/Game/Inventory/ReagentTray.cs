using System.Collections.Generic;
using Godot;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Game.Inventory;

public partial class ReagentTray : VBoxContainer
{
    private List<Reagent> _allReagents = new();
    private readonly List<ReagentTrayItem> _items = new();

    [Signal]
    public delegate void ReagentSelectedEventHandler(int reagentIndex);

    [Signal]
    public delegate void ReagentHoveredEventHandler(int reagentIndex);

    [Signal]
    public delegate void ReagentUnhoveredEventHandler(int reagentIndex);

    public void Initialize(List<Reagent> reagents)
    {
        _allReagents = reagents;

        // Header
        var header = new Label
        {
            Text = "Reagents",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        header.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.7f));
        AddChild(header);

        // Separator
        AddChild(new HSeparator());

        BuildItems();
    }

    public void RefreshDisplay()
    {
        foreach (var item in _items)
        {
            item.Clicked -= OnItemClicked;
            item.Hovered -= OnItemHovered;
            item.Unhovered -= OnItemUnhovered;
            item.QueueFree();
        }
        _items.Clear();
        BuildItems();
    }

    public void ClearSelection()
    {
        foreach (var item in _items)
        {
            item.SetSelected(false);
        }
    }

    public void HighlightIndex(int index)
    {
        foreach (var item in _items)
        {
            item.SetSelected(item.ReagentIndex == index);
        }
    }

    private void BuildItems()
    {
        for (int i = 0; i < _allReagents.Count; i++)
        {
            var reagent = _allReagents[i];
            if (reagent.IsPlaced) continue;

            var item = new ReagentTrayItem();
            AddChild(item);
            item.Initialize(reagent, i);
            item.Clicked += OnItemClicked;
            item.Hovered += OnItemHovered;
            item.Unhovered += OnItemUnhovered;
            _items.Add(item);
        }
    }

    private void OnItemClicked(int index)
    {
        HighlightIndex(index);
        EmitSignal(SignalName.ReagentSelected, index);
    }

    private void OnItemHovered(int index)
    {
        EmitSignal(SignalName.ReagentHovered, index);
    }

    private void OnItemUnhovered(int index)
    {
        EmitSignal(SignalName.ReagentUnhovered, index);
    }
}
