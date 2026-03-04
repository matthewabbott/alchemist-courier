using System;
using System.Collections.Generic;
using System.Linq;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Validates puzzle solutions by reconstructing the grid and checking all placement constraints.
/// </summary>
public static class PuzzleSolver
{
    /// <summary>
    /// Validates the current player placements (PuzzleState.Placements).
    /// Checks: all reagents placed, locked reagents unmoved, no overlaps, all within bag bounds, no negative reactions.
    /// </summary>
    public static bool ValidateSolution(PuzzleState state)
    {
        // Verify locked reagents are at their solution positions
        if (!ValidateLockedPositions(state))
            return false;

        return ValidatePlacements(state, state.Placements);
    }

    /// <summary>
    /// Checks whether the stored solution is valid by applying it to a reconstructed grid.
    /// </summary>
    public static bool IsSolvable(PuzzleState state)
    {
        return ValidatePlacements(state, state.Solution);
    }

    /// <summary>
    /// Core validation: reconstructs the grid, applies placements, and checks all constraints.
    /// </summary>
    private static bool ValidatePlacements(PuzzleState state, List<Placement> placements)
    {
        if (state.Reagents.Count == 0)
            return false;

        // Check all reagents are placed exactly once
        if (placements.Count != state.Reagents.Count)
            return false;

        var placedIndices = new HashSet<int>();
        foreach (var p in placements)
        {
            if (p.ReagentIndex < 0 || p.ReagentIndex >= state.Reagents.Count)
                return false;
            if (!placedIndices.Add(p.ReagentIndex))
                return false; // Duplicate reagent index
        }

        // Reconstruct grid and reagents
        var grid = ReconstructGrid(state);
        var reagents = ReconstructReagents(state);

        // Check overlaps and bag bounds by placing reagents one at a time
        foreach (var placement in placements)
        {
            var reagent = reagents[placement.ReagentIndex];
            reagent.Rotation = placement.Rotation;
            var pos = new Vector2I(placement.PositionCol, placement.PositionRow);

            // CanPlaceReagent checks: in bounds, in bag, not occupied
            if (!grid.CanPlaceReagent(reagent, pos))
                return false;

            grid.PlaceReagent(reagent, pos);
        }

        // Check for negative reactions
        var rules = ReactionRuleDatabase.GetMvpRules();
        var reactions = ChemistryEngine.Evaluate(grid, rules);
        return reactions.Count == 0;
    }

    /// <summary>
    /// Reconstruct a MetaGrid from a PuzzleState's bag definitions.
    /// </summary>
    internal static MetaGrid ReconstructGrid(PuzzleState state)
    {
        int maxCol = 0, maxRow = 0;
        foreach (var bag in state.Bags)
        {
            var shape = bag.ToShapeMatrix();
            int endCol = bag.PositionCol + shape.GetLength(1);
            int endRow = bag.PositionRow + shape.GetLength(0);
            if (endCol > maxCol) maxCol = endCol;
            if (endRow > maxRow) maxRow = endRow;
        }

        var grid = new MetaGrid(maxCol + 2, maxRow + 2);

        for (int i = 0; i < state.Bags.Count; i++)
        {
            var puzzleBag = state.Bags[i];
            var shape = puzzleBag.ToShapeMatrix();
            var properties = ParseBagProperties(puzzleBag.Properties);
            var def = new BagDefinition($"bag_{i}", $"Bag {i}", shape, properties);
            var bag = new Bag(def);
            grid.PlaceBag(bag, new Vector2I(puzzleBag.PositionCol, puzzleBag.PositionRow));
        }

        return grid;
    }

    /// <summary>
    /// Parse bag property strings back to BagPropertyType enum values.
    /// </summary>
    private static List<BagPropertyType> ParseBagProperties(List<string> propertyStrings)
    {
        var properties = new List<BagPropertyType>();
        foreach (var s in propertyStrings)
        {
            if (Enum.TryParse<BagPropertyType>(s, out var prop))
                properties.Add(prop);
        }
        return properties;
    }

    /// <summary>
    /// Verify that all locked reagents in Placements match their Solution positions exactly.
    /// </summary>
    private static bool ValidateLockedPositions(PuzzleState state)
    {
        // Build lookup: reagentIndex -> solution placement
        var solutionByReagent = new Dictionary<int, Placement>();
        foreach (var sp in state.Solution)
            solutionByReagent[sp.ReagentIndex] = sp;

        foreach (var placement in state.Placements)
        {
            if (placement.ReagentIndex < 0 || placement.ReagentIndex >= state.Reagents.Count)
                continue; // Will be caught by ValidatePlacements

            if (!state.Reagents[placement.ReagentIndex].IsLocked)
                continue;

            if (!solutionByReagent.TryGetValue(placement.ReagentIndex, out var solutionPlacement))
                return false; // Locked reagent not in solution

            if (placement.PositionCol != solutionPlacement.PositionCol ||
                placement.PositionRow != solutionPlacement.PositionRow ||
                placement.Rotation != solutionPlacement.Rotation)
                return false; // Locked reagent was moved
        }

        return true;
    }

    /// <summary>
    /// Reconstruct runtime Reagent objects from a PuzzleState's reagent definitions.
    /// </summary>
    internal static List<Reagent> ReconstructReagents(PuzzleState state)
    {
        var reagents = new List<Reagent>();
        for (int i = 0; i < state.Reagents.Count; i++)
        {
            var pr = state.Reagents[i];
            var shape = pr.ToShapeMatrix();
            var aspects = new List<AspectSet>();
            foreach (var cellAspects in pr.AspectsPerCell)
            {
                var set = new AspectSet();
                foreach (var id in cellAspects)
                {
                    set.Add(Aspect.FromId(id));
                }
                aspects.Add(set);
            }

            var def = new ReagentDefinition($"reagent_{i}", $"Reagent {i}", shape, aspectsPerCell: aspects);
            var reagent = new Reagent(def) { IsLocked = pr.IsLocked };
            reagents.Add(reagent);
        }
        return reagents;
    }

}
