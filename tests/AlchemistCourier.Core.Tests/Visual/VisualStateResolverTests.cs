using System.Collections.Generic;
using Xunit;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Core.Visual;

namespace AlchemistCourier.Core.Tests.Visual;

public class VisualStateResolverTests
{
    /// <summary>Helper: create a 1-cell reagent with the given aspects.</summary>
    private static Reagent MakeSingleCellReagent(string id, params Aspect[] aspects)
    {
        var def = new ReagentDefinition(id, id, new bool[,] { { true } },
            aspectsPerCell: new List<AspectSet> { new AspectSet(aspects) });
        return new Reagent(def);
    }

    /// <summary>Helper: set up a grid with a full bag and place reagents.</summary>
    private static MetaGrid SetupGrid(int width, int height, params (Reagent reagent, Vector2I pos)[] placements)
    {
        var grid = new MetaGrid(width, height);
        var shape = new bool[height, width];
        for (int r = 0; r < height; r++)
            for (int c = 0; c < width; c++)
                shape[r, c] = true;
        var bagDef = new BagDefinition("test", "Test Bag", shape);
        var bag = new Bag(bagDef);
        grid.PlaceBag(bag, new Vector2I(0, 0));

        foreach (var (reagent, pos) in placements)
            grid.PlaceReagent(reagent, pos);

        return grid;
    }

    // --- Basic cases ---

    [Fact]
    public void Resolve_UnplacedReagent_ReturnsEmpty()
    {
        var reagent = MakeSingleCellReagent("test", Aspect.Hot());
        var grid = new MetaGrid(5, 5);
        var result = VisualStateResolver.Resolve(reagent, grid);
        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_IsolatedReagent_ReturnsNormal()
    {
        var reagent = MakeSingleCellReagent("test", Aspect.Fragile());
        var grid = SetupGrid(5, 5, (reagent, new Vector2I(2, 2)));

        var result = VisualStateResolver.Resolve(reagent, grid);

        Assert.Single(result);
        Assert.Equal(VisualState.Normal, result[new Vector2I(2, 2)]);
    }

    // --- Heated ---

    [Fact]
    public void Resolve_AdjacentToHot_ReturnsHeated()
    {
        var glass = MakeSingleCellReagent("glass", Aspect.Fragile());
        var fire = MakeSingleCellReagent("fire", Aspect.Hot());
        var grid = SetupGrid(5, 5,
            (glass, new Vector2I(1, 1)),
            (fire, new Vector2I(2, 1)));

        var result = VisualStateResolver.Resolve(glass, grid);

        Assert.Equal(VisualState.Heated, result[new Vector2I(1, 1)]);
    }

    [Fact]
    public void Resolve_NonFragileAdjacentToHot_ReturnsHeated()
    {
        var stone = MakeSingleCellReagent("stone", Aspect.Heavy());
        var fire = MakeSingleCellReagent("fire", Aspect.Hot());
        var grid = SetupGrid(5, 5,
            (stone, new Vector2I(1, 1)),
            (fire, new Vector2I(2, 1)));

        var result = VisualStateResolver.Resolve(stone, grid);

        Assert.Equal(VisualState.Heated, result[new Vector2I(1, 1)]);
    }

    // --- Cooled ---

    [Fact]
    public void Resolve_AdjacentToCold_ReturnsCooled()
    {
        var glass = MakeSingleCellReagent("glass", Aspect.Fragile());
        var ice = MakeSingleCellReagent("ice", Aspect.Cold());
        var grid = SetupGrid(5, 5,
            (glass, new Vector2I(1, 1)),
            (ice, new Vector2I(1, 2)));

        var result = VisualStateResolver.Resolve(glass, grid);

        Assert.Equal(VisualState.Cooled, result[new Vector2I(1, 1)]);
    }

    // --- ThermalStress ---

    [Fact]
    public void Resolve_FragileAdjacentToHotAndCold_ReturnsThermalStress()
    {
        var glass = MakeSingleCellReagent("glass", Aspect.Fragile());
        var fire = MakeSingleCellReagent("fire", Aspect.Hot());
        var ice = MakeSingleCellReagent("ice", Aspect.Cold());
        var grid = SetupGrid(5, 5,
            (glass, new Vector2I(1, 1)),
            (fire, new Vector2I(2, 1)),
            (ice, new Vector2I(0, 1)));

        var result = VisualStateResolver.Resolve(glass, grid);

        Assert.Equal(VisualState.ThermalStress, result[new Vector2I(1, 1)]);
    }

    [Fact]
    public void Resolve_NonFragileAdjacentToHotAndCold_ReturnsNormal()
    {
        // Non-fragile reagents don't get ThermalStress.
        // When both Hot and Cold are adjacent and reagent is NOT fragile,
        // neither Heated nor Cooled wins — it's a wash → Normal.
        var stone = MakeSingleCellReagent("stone", Aspect.Heavy());
        var fire = MakeSingleCellReagent("fire", Aspect.Hot());
        var ice = MakeSingleCellReagent("ice", Aspect.Cold());
        var grid = SetupGrid(5, 5,
            (stone, new Vector2I(1, 1)),
            (fire, new Vector2I(2, 1)),
            (ice, new Vector2I(0, 1)));

        var result = VisualStateResolver.Resolve(stone, grid);

        // Both Hot and Cold adjacent to non-fragile → neither Heated nor Cooled
        Assert.Equal(VisualState.Normal, result[new Vector2I(1, 1)]);
    }

    // --- Crushed ---

    [Fact]
    public void Resolve_FragileWithHeavyAbove_ReturnsCrushed()
    {
        var glass = MakeSingleCellReagent("glass", Aspect.Fragile());
        var anvil = MakeSingleCellReagent("anvil", Aspect.Heavy());
        // Anvil at row 1, glass at row 2 — Heavy is directly above
        var grid = SetupGrid(5, 5,
            (anvil, new Vector2I(1, 1)),
            (glass, new Vector2I(1, 2)));

        var result = VisualStateResolver.Resolve(glass, grid);

        Assert.Equal(VisualState.Crushed, result[new Vector2I(1, 2)]);
    }

    [Fact]
    public void Resolve_NonFragileWithHeavyAbove_ReturnsNormal()
    {
        // Non-fragile reagents are not affected by Heavy above
        var stone = MakeSingleCellReagent("stone", Aspect.Toxic());
        var anvil = MakeSingleCellReagent("anvil", Aspect.Heavy());
        var grid = SetupGrid(5, 5,
            (anvil, new Vector2I(1, 1)),
            (stone, new Vector2I(1, 2)));

        var result = VisualStateResolver.Resolve(stone, grid);

        Assert.Equal(VisualState.Normal, result[new Vector2I(1, 2)]);
    }

    [Fact]
    public void Resolve_FragileWithHeavyBelow_ReturnsNormal()
    {
        // Heavy must be ABOVE, not below
        var glass = MakeSingleCellReagent("glass", Aspect.Fragile());
        var anvil = MakeSingleCellReagent("anvil", Aspect.Heavy());
        // Glass at row 1, anvil at row 2 — Heavy is below, not above
        var grid = SetupGrid(5, 5,
            (glass, new Vector2I(1, 1)),
            (anvil, new Vector2I(1, 2)));

        var result = VisualStateResolver.Resolve(glass, grid);

        Assert.Equal(VisualState.Normal, result[new Vector2I(1, 1)]);
    }

    [Fact]
    public void Resolve_FragileWithHeavyBeside_NotCrushed()
    {
        // Heavy must be directly above, not left/right
        var glass = MakeSingleCellReagent("glass", Aspect.Fragile());
        var anvil = MakeSingleCellReagent("anvil", Aspect.Heavy());
        var grid = SetupGrid(5, 5,
            (glass, new Vector2I(1, 1)),
            (anvil, new Vector2I(2, 1)));

        var result = VisualStateResolver.Resolve(glass, grid);

        // Heavy to the side is not Crushed, but it's not Hot/Cold either
        Assert.Equal(VisualState.Normal, result[new Vector2I(1, 1)]);
    }

    // --- Crushed takes priority over Heated ---

    [Fact]
    public void Resolve_CrushedTakesPriorityOverHeated()
    {
        // When Fragile has Heavy above AND Hot adjacent, Crushed wins
        var glass = MakeSingleCellReagent("glass", Aspect.Fragile());
        var anvil = MakeSingleCellReagent("anvil", Aspect.Heavy());
        var fire = MakeSingleCellReagent("fire", Aspect.Hot());
        var grid = SetupGrid(5, 5,
            (anvil, new Vector2I(1, 1)),
            (glass, new Vector2I(1, 2)),
            (fire, new Vector2I(2, 2)));

        var result = VisualStateResolver.Resolve(glass, grid);

        Assert.Equal(VisualState.Crushed, result[new Vector2I(1, 2)]);
    }

    // --- Diagonal neighbors don't affect state ---

    [Fact]
    public void Resolve_DiagonalHot_ReturnsNormal()
    {
        var glass = MakeSingleCellReagent("glass", Aspect.Fragile());
        var fire = MakeSingleCellReagent("fire", Aspect.Hot());
        // Fire at (2,2), glass at (1,1) — diagonal, not cardinal
        var grid = SetupGrid(5, 5,
            (glass, new Vector2I(1, 1)),
            (fire, new Vector2I(2, 2)));

        var result = VisualStateResolver.Resolve(glass, grid);

        Assert.Equal(VisualState.Normal, result[new Vector2I(1, 1)]);
    }

    // --- Multi-cell reagent with per-cell states ---

    [Fact]
    public void Resolve_MultiCellReagent_PerCellStates()
    {
        // 2-cell domino: cell at (1,1) and (2,1)
        // Hot neighbor at (0,1) — only (1,1) is adjacent to it
        var def = new ReagentDefinition("domino", "domino", new bool[,] { { true, true } },
            aspectsPerCell: new List<AspectSet> { new(Aspect.Fragile()), new(Aspect.Fragile()) });
        var domino = new Reagent(def);
        var fire = MakeSingleCellReagent("fire", Aspect.Hot());
        var grid = SetupGrid(5, 5,
            (domino, new Vector2I(1, 1)),
            (fire, new Vector2I(0, 1)));

        var result = VisualStateResolver.Resolve(domino, grid);

        Assert.Equal(2, result.Count);
        Assert.Equal(VisualState.Heated, result[new Vector2I(1, 1)]); // Adjacent to fire
        Assert.Equal(VisualState.Normal, result[new Vector2I(2, 1)]); // Not adjacent to fire
    }

    // --- Same reagent cells are not neighbors of each other ---

    [Fact]
    public void Resolve_SelfAdjacency_Ignored()
    {
        // A domino with both Hot and Fragile on each cell should not
        // heat itself — only OTHER reagents affect visual state
        var def = new ReagentDefinition("hotfrag", "hotfrag", new bool[,] { { true, true } },
            aspectsPerCell: new List<AspectSet>
            {
                new(Aspect.Hot(), Aspect.Fragile()),
                new(Aspect.Hot(), Aspect.Fragile()),
            });
        var reagent = new Reagent(def);
        var grid = SetupGrid(5, 5, (reagent, new Vector2I(1, 1)));

        var result = VisualStateResolver.Resolve(reagent, grid);

        // Own cells don't influence visual state
        Assert.Equal(VisualState.Normal, result[new Vector2I(1, 1)]);
        Assert.Equal(VisualState.Normal, result[new Vector2I(2, 1)]);
    }

    // --- Edge of grid ---

    [Fact]
    public void Resolve_AtGridEdge_ReturnsNormal()
    {
        // Reagent at corner (0,0) — neighbors outside grid bounds don't cause issues
        var glass = MakeSingleCellReagent("glass", Aspect.Fragile());
        var grid = SetupGrid(5, 5, (glass, new Vector2I(0, 0)));

        var result = VisualStateResolver.Resolve(glass, grid);

        Assert.Equal(VisualState.Normal, result[new Vector2I(0, 0)]);
    }

    // --- No aspect reagent adjacent ---

    [Fact]
    public void Resolve_AdjacentReagentWithNoAspects_ReturnsNormal()
    {
        var glass = MakeSingleCellReagent("glass", Aspect.Fragile());
        var empty = new Reagent(ReagentDefinition.SingleCell); // No aspects
        var grid = SetupGrid(5, 5,
            (glass, new Vector2I(1, 1)),
            (empty, new Vector2I(2, 1)));

        var result = VisualStateResolver.Resolve(glass, grid);

        Assert.Equal(VisualState.Normal, result[new Vector2I(1, 1)]);
    }
}
