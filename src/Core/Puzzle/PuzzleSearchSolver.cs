using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Backtracking constraint-satisfaction solver for puzzles.
/// Finds valid placements for all reagents such that no overlaps,
/// all cells within bags, and zero negative chemistry reactions.
/// </summary>
public static class PuzzleSearchSolver
{
    /// <summary>
    /// Find one valid solution for the puzzle, or null if unsolvable/timeout.
    /// </summary>
    public static List<Placement>? Solve(PuzzleState state, CancellationToken? ct = null)
    {
        var token = ct ?? CancellationToken.None;
        var grid = PuzzleSolver.ReconstructGrid(state);
        var reagents = PuzzleSolver.ReconstructReagents(state);
        var rules = ReactionRuleDatabase.GetMvpRules();

        // Pre-place locked reagents
        var lockedIndices = new HashSet<int>();
        for (int i = 0; i < state.Reagents.Count; i++)
        {
            if (!state.Reagents[i].IsLocked)
                continue;

            // Find the solution placement for this locked reagent
            var solutionPlacement = state.Solution.Find(p => p.ReagentIndex == i);
            if (solutionPlacement == null)
                continue;

            var reagent = reagents[i];
            reagent.Rotation = solutionPlacement.Rotation;
            var pos = new Vector2I(solutionPlacement.PositionCol, solutionPlacement.PositionRow);
            if (!grid.CanPlaceReagent(reagent, pos))
                return null; // Locked reagent can't be placed — unsolvable
            grid.PlaceReagent(reagent, pos);
            lockedIndices.Add(i);
        }

        // Order unlocked reagents by MRV heuristic: most constrained first
        var unlockedIndexed = reagents
            .Select((r, i) => (reagent: r, index: i))
            .Where(x => !lockedIndices.Contains(x.index))
            .OrderByDescending(x => x.reagent.HasAspect(AspectIds.Heavy) ? 1 : 0)
            .ThenByDescending(x => x.reagent.HasAspect(AspectIds.Sharp) ? 1 : 0)
            .ThenByDescending(x => x.reagent.Definition.CellCount)
            .ToList();

        var placements = new List<Placement>();

        // Add locked placements
        foreach (var idx in lockedIndices)
        {
            var sp = state.Solution.Find(p => p.ReagentIndex == idx)!;
            placements.Add(new Placement
            {
                ReagentIndex = idx,
                PositionCol = sp.PositionCol,
                PositionRow = sp.PositionRow,
                Rotation = sp.Rotation,
            });
        }

        if (Backtrack(grid, rules, unlockedIndexed, 0, placements, token))
            return placements;

        return null;
    }

    /// <summary>
    /// Count the number of valid solutions up to maxCount.
    /// </summary>
    public static int CountSolutions(PuzzleState state, int maxCount, CancellationToken? ct = null)
    {
        var token = ct ?? CancellationToken.None;
        var grid = PuzzleSolver.ReconstructGrid(state);
        var reagents = PuzzleSolver.ReconstructReagents(state);
        var rules = ReactionRuleDatabase.GetMvpRules();

        // Pre-place locked reagents
        var lockedIndices = new HashSet<int>();
        for (int i = 0; i < state.Reagents.Count; i++)
        {
            if (!state.Reagents[i].IsLocked)
                continue;

            var solutionPlacement = state.Solution.Find(p => p.ReagentIndex == i);
            if (solutionPlacement == null)
                continue;

            var reagent = reagents[i];
            reagent.Rotation = solutionPlacement.Rotation;
            var pos = new Vector2I(solutionPlacement.PositionCol, solutionPlacement.PositionRow);
            if (!grid.CanPlaceReagent(reagent, pos))
                return 0;
            grid.PlaceReagent(reagent, pos);
            lockedIndices.Add(i);
        }

        var unlockedIndexed = reagents
            .Select((r, i) => (reagent: r, index: i))
            .Where(x => !lockedIndices.Contains(x.index))
            .OrderByDescending(x => x.reagent.HasAspect(AspectIds.Heavy) ? 1 : 0)
            .ThenByDescending(x => x.reagent.HasAspect(AspectIds.Sharp) ? 1 : 0)
            .ThenByDescending(x => x.reagent.Definition.CellCount)
            .ToList();

        int count = 0;
        CountBacktrack(grid, rules, unlockedIndexed, 0, maxCount, ref count, token);
        return count;
    }

    private static bool Backtrack(
        MetaGrid grid,
        List<ReactionRule> rules,
        List<(Reagent reagent, int index)> ordered,
        int depth,
        List<Placement> placements,
        CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return false;

        if (depth == ordered.Count)
            return true; // All reagents placed successfully

        var (reagent, originalIndex) = ordered[depth];

        for (int rot = 0; rot < 4; rot++)
        {
            reagent.Rotation = rot;

            for (int col = 0; col < grid.Width; col++)
            {
                for (int row = 0; row < grid.Height; row++)
                {
                    if (ct.IsCancellationRequested)
                        return false;

                    var pos = new Vector2I(col, row);
                    if (!grid.CanPlaceReagent(reagent, pos))
                        continue;

                    var reactions = ChemistryEngine.EvaluateProspective(grid, reagent, pos, rules);
                    if (reactions.Count > 0)
                        continue;

                    // Commit placement
                    grid.PlaceReagent(reagent, pos);
                    placements.Add(new Placement
                    {
                        ReagentIndex = originalIndex,
                        PositionCol = col,
                        PositionRow = row,
                        Rotation = rot,
                    });

                    if (Backtrack(grid, rules, ordered, depth + 1, placements, ct))
                        return true;

                    // Backtrack
                    grid.RemoveReagent(reagent);
                    placements.RemoveAt(placements.Count - 1);
                }
            }
        }

        return false;
    }

    private static void CountBacktrack(
        MetaGrid grid,
        List<ReactionRule> rules,
        List<(Reagent reagent, int index)> ordered,
        int depth,
        int maxCount,
        ref int count,
        CancellationToken ct)
    {
        if (ct.IsCancellationRequested || count >= maxCount)
            return;

        if (depth == ordered.Count)
        {
            count++;
            return;
        }

        var (reagent, originalIndex) = ordered[depth];

        for (int rot = 0; rot < 4; rot++)
        {
            reagent.Rotation = rot;

            for (int col = 0; col < grid.Width; col++)
            {
                for (int row = 0; row < grid.Height; row++)
                {
                    if (ct.IsCancellationRequested || count >= maxCount)
                        return;

                    var pos = new Vector2I(col, row);
                    if (!grid.CanPlaceReagent(reagent, pos))
                        continue;

                    var reactions = ChemistryEngine.EvaluateProspective(grid, reagent, pos, rules);
                    if (reactions.Count > 0)
                        continue;

                    grid.PlaceReagent(reagent, pos);

                    CountBacktrack(grid, rules, ordered, depth + 1, maxCount, ref count, ct);

                    grid.RemoveReagent(reagent);
                }
            }
        }
    }
}
