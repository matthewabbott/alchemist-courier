using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Game.Inventory;
using Godot;

namespace AlchemistCourier.Game.Testing;

public static class TestActions
{
    public static bool SelectAndPlace(PuzzleScreen screen, int reagentIndex, int col, int row)
    {
        screen.SelectReagent(reagentIndex);
        if (screen.SelectedReagent == null)
        {
            GD.Print($"[ACTION] select_reagent index={reagentIndex} result=failed");
            return false;
        }
        GD.Print($"[ACTION] select_reagent index={reagentIndex} name={screen.SelectedReagent.Definition.Name}");

        bool placed = screen.PlaceSelectedAt(col, row);
        GD.Print($"[ACTION] place col={col} row={row} result={(placed ? "success" : "failed")}");
        return placed;
    }

    public static bool SelectRotateAndPlace(PuzzleScreen screen, int reagentIndex, int rotations, int col, int row)
    {
        screen.SelectReagent(reagentIndex);
        if (screen.SelectedReagent == null)
        {
            GD.Print($"[ACTION] select_reagent index={reagentIndex} result=failed");
            return false;
        }
        GD.Print($"[ACTION] select_reagent index={reagentIndex} name={screen.SelectedReagent.Definition.Name}");

        for (int i = 0; i < rotations; i++)
        {
            screen.RotateSelected();
        }
        GD.Print($"[ACTION] rotate count={rotations}");

        bool placed = screen.PlaceSelectedAt(col, row);
        GD.Print($"[ACTION] place col={col} row={row} result={(placed ? "success" : "failed")}");
        return placed;
    }

    public static bool RemoveAt(PuzzleScreen screen, int col, int row)
    {
        bool removed = screen.RemoveReagentAt(col, row);
        GD.Print($"[ACTION] remove col={col} row={row} result={(removed ? "success" : "failed")}");
        return removed;
    }

    public static string GetStateString(PuzzleScreen screen)
    {
        var grid = screen.Grid;
        int placed = grid.PlacedReagents.Count;
        int unplaced = 0;
        foreach (var r in screen.TrayReagents)
        {
            if (!r.IsPlaced) unplaced++;
        }
        return $"grid={grid.Width}x{grid.Height} bags={grid.PlacedBags.Count} placed={placed} unplaced={unplaced}";
    }
}
