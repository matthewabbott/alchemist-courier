using System;
using System.Collections.Generic;
using System.Linq;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Chemistry;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Generates puzzles using the disassemble-a-solution algorithm.
/// All randomness is driven by SeededRandom for deterministic output.
/// </summary>
public class PuzzleGenerator
{
    private const int MaxRetries = 50;
    private const int MetaGridPadding = 2;

    /// <summary>Available aspect IDs for the MVP Physical/Chemistry school.</summary>
    private static readonly string[] MvpAspects = { "hot", "cold", "volatile", "stable", "heavy", "toxic" };

    /// <summary>
    /// Predefined non-rectangular bag shapes for higher difficulties.
    /// Each is a bool[rows, cols] matrix. Grouped by cell count.
    /// </summary>
    private static readonly bool[][,] NonRectBagShapes =
    {
        // 6-cell L
        new bool[,] {
            { true, false, false },
            { true, false, false },
            { true, true,  true  },
        },
        // 6-cell T
        new bool[,] {
            { true, true, true },
            { false, true, false },
            { false, true, false },
        },
        // 7-cell hourglass
        new bool[,] {
            { true, true, true },
            { false, true, false },
            { true, true, true },
        },
        // 8-cell S-step
        new bool[,] {
            { true, true, false },
            { true, true, false },
            { false, true, true },
            { false, true, true },
        },
        // 8-cell plus
        new bool[,] {
            { false, true, false },
            { true,  true, true  },
            { true,  true, true  },
            { false, true, false },
        },
        // 9-cell U
        new bool[,] {
            { true, false, true },
            { true, false, true },
            { true, false, true },
            { true, true,  true },
        },
        // 10-cell notched rectangle
        new bool[,] {
            { true,  true, false },
            { true,  true, true  },
            { true,  true, true  },
            { false, true, true  },
        },
        // 12-cell chunky L
        new bool[,] {
            { true,  true, false, false },
            { true,  true, false, false },
            { true,  true, true,  true  },
            { true,  true, true,  true  },
        },
    };

    /// <summary>
    /// Predefined reagent shapes: each is a bool[rows, cols] matrix.
    /// Ordered by cell count (1-cell, 2-cell, 3-cell, 4-cell).
    /// </summary>
    private static readonly bool[][,] ReagentShapes =
    {
        // 1-cell
        new bool[,] { { true } },
        // 2-cell horizontal
        new bool[,] { { true, true } },
        // 2-cell vertical
        new bool[,] { { true }, { true } },
        // 3-cell horizontal bar
        new bool[,] { { true, true, true } },
        // 3-cell vertical bar
        new bool[,] { { true }, { true }, { true } },
        // 3-cell L
        new bool[,] { { true, false }, { true, true } },
        // 4-cell square
        new bool[,] { { true, true }, { true, true } },
        // 4-cell T
        new bool[,] { { true, true, true }, { false, true, false } },
        // 4-cell L
        new bool[,] { { true, false }, { true, false }, { true, true } },
        // 4-cell S
        new bool[,] { { false, true }, { true, true }, { true, false } },
        // 4-cell I
        new bool[,] { { true }, { true }, { true }, { true } },
    };

    /// <summary>
    /// Generate a puzzle from a seed and difficulty parameters.
    /// Returns a fully populated PuzzleState with bags, reagents, and a valid solution.
    /// </summary>
    public PuzzleState Generate(int seed, DifficultyParams difficulty)
    {
        var rng = new SeededRandom(seed);
        var rules = ReactionRuleDatabase.GetMvpRules();

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var result = TryGenerate(rng, difficulty, rules);
            if (result != null)
            {
                result.Seed = seed;
                result.Difficulty = difficulty switch
                {
                    _ when difficulty.BagCount == 1 && difficulty.MaxAspectCount == 0 => DifficultyLevel.Tutorial1,
                    _ => InferDifficulty(difficulty),
                };
                result.PuzzleType = PuzzleType.FitAllSafely;
                return result;
            }
        }

        throw new InvalidOperationException(
            $"Failed to generate a valid puzzle after {MaxRetries} attempts (seed={seed}).");
    }

    private PuzzleState? TryGenerate(SeededRandom rng, DifficultyParams difficulty, List<ReactionRule> rules)
    {
        // Step 1: Generate bag shapes and compute total capacity
        var bagShapes = GenerateBagShapes(rng, difficulty);
        int totalBagCells = bagShapes.Sum(b => CellCount(b));

        // Step 1b: Assign special properties to bags
        var bagProperties = AssignBagProperties(rng, difficulty, bagShapes);

        // Step 2: Determine how many reagent cells we need
        int targetReagentCells = (int)(totalBagCells / (1f + difficulty.SpareSpacePercent));
        targetReagentCells = Math.Max(targetReagentCells, difficulty.ReagentCount);

        // Step 3: Generate reagent shapes that fit within the target cell count
        var reagentShapes = GenerateReagentShapes(rng, difficulty, targetReagentCells);
        int totalReagentCells = reagentShapes.Sum(r => CellCount(r.shape));

        // If reagents need more cells than bags provide, fail this attempt
        if (totalReagentCells > totalBagCells)
            return null;

        // Step 4: Build the meta-grid and place bags
        var (grid, bagPositions) = BuildMetaGridWithBags(rng, bagShapes, bagProperties);
        if (grid == null)
            return null;

        // Step 5: Assign aspects to reagents
        var aspectAssignments = AssignAspects(rng, difficulty, reagentShapes);

        // Step 6: Create runtime objects and try to place all reagents validly
        var reagentDefs = new List<ReagentDefinition>();
        var reagents = new List<Reagent>();
        for (int i = 0; i < reagentShapes.Count; i++)
        {
            var (shape, _) = reagentShapes[i];
            var aspects = aspectAssignments[i];
            var def = new ReagentDefinition(
                id: $"reagent_{i}",
                name: $"Reagent {i}",
                shape: shape,
                aspectsPerCell: aspects);
            reagentDefs.Add(def);
            reagents.Add(new Reagent(def));
        }

        // Step 7: Place reagents one by one, validating with ChemistryEngine
        var placements = PlaceReagentsValidly(rng, grid, reagents, rules);
        if (placements == null)
            return null;

        // Step 8: Build the PuzzleState
        var state = new PuzzleState();
        state.Bags = new List<PuzzleBag>();
        for (int i = 0; i < bagShapes.Count; i++)
        {
            var propStrings = bagProperties[i].Select(p => p.ToString()).ToList();
            state.Bags.Add(PuzzleBag.FromMatrix(
                bagShapes[i],
                bagPositions[i].Col,
                bagPositions[i].Row,
                propStrings));
        }

        state.Reagents = new List<PuzzleReagent>();
        for (int i = 0; i < reagentDefs.Count; i++)
        {
            var def = reagentDefs[i];
            var aspectsPerCell = new List<List<string>>();
            foreach (var aspectSet in def.AspectsPerCell)
            {
                aspectsPerCell.Add(aspectSet.Aspects.Select(a => a.Id).ToList());
            }
            state.Reagents.Add(PuzzleReagent.FromMatrix(
                def.Shape,
                aspectsPerCell));
        }

        state.Solution = placements;

        // Step 9: Lock some reagents in place as pre-placed constraints
        LockSolutionReagents(rng, difficulty, state);

        state.Placements = new List<Placement>();
        return state;
    }

    /// <summary>
    /// When AllowSpecialBags is true, lock 1-2 solution reagents in place.
    /// These become fixed constraints the player must work around.
    /// Locked reagents are added to Placements so they start pre-placed.
    /// </summary>
    private static void LockSolutionReagents(SeededRandom rng, DifficultyParams difficulty, PuzzleState state)
    {
        if (!difficulty.AllowSpecialBags || state.Solution.Count < 3)
            return;

        int lockCount = rng.Next(1, Math.Min(3, state.Solution.Count - 1));

        // Shuffle solution indices and pick the first lockCount
        var indices = Enumerable.Range(0, state.Solution.Count).ToList();
        rng.Shuffle(indices);

        for (int i = 0; i < lockCount; i++)
        {
            int solutionIdx = indices[i];
            state.Reagents[state.Solution[solutionIdx].ReagentIndex].IsLocked = true;
        }
    }

    private List<bool[,]> GenerateBagShapes(SeededRandom rng, DifficultyParams difficulty)
    {
        var bags = new List<bool[,]>();
        for (int i = 0; i < difficulty.BagCount; i++)
        {
            int targetCells = rng.Next(difficulty.BagMinSize, difficulty.BagMaxSize + 1);
            var shape = PickBagShape(rng, targetCells, difficulty.AllowSpecialBags);
            bags.Add(shape);
        }
        return bags;
    }

    private static bool[,] PickBagShape(SeededRandom rng, int targetCells, bool allowNonRect)
    {
        if (allowNonRect && rng.NextFloat() < 0.4f)
        {
            var matching = NonRectBagShapes.Where(s => CellCount(s) == targetCells).ToArray();
            if (matching.Length > 0)
                return matching[rng.Next(matching.Length)];
        }
        return GenerateRectangularBag(rng, targetCells);
    }

    private static bool[,] GenerateRectangularBag(SeededRandom rng, int targetCells)
    {
        // Generate a rectangular bag close to the target cell count.
        // Find factor pairs and pick one randomly.
        var factorPairs = new List<(int rows, int cols)>();
        for (int r = 1; r <= targetCells; r++)
        {
            if (targetCells % r == 0)
            {
                int c = targetCells / r;
                if (r <= 6 && c <= 6) // Keep bags manageable
                    factorPairs.Add((r, c));
            }
        }

        // If no exact factor pairs fit, use closest rectangle
        if (factorPairs.Count == 0)
        {
            int side = (int)Math.Ceiling(Math.Sqrt(targetCells));
            factorPairs.Add((side, (targetCells + side - 1) / side));
        }

        var (rows, cols) = factorPairs[rng.Next(factorPairs.Count)];
        var shape = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                shape[r, c] = true;
        return shape;
    }

    private List<(bool[,] shape, int cellCount)> GenerateReagentShapes(
        SeededRandom rng, DifficultyParams difficulty, int targetCells)
    {
        var result = new List<(bool[,] shape, int cellCount)>();
        int cellsRemaining = targetCells;

        // Determine max reagent size based on difficulty
        int maxReagentCells = difficulty.MaxAspectCount <= 1 ? 2 : 4;

        for (int i = 0; i < difficulty.ReagentCount && cellsRemaining > 0; i++)
        {
            bool isLast = (i == difficulty.ReagentCount - 1);
            int maxCells = Math.Min(maxReagentCells, cellsRemaining);
            if (maxCells <= 0) break;

            // If this is the last reagent, use exactly the remaining cells (up to max)
            int targetSize = isLast ? Math.Min(cellsRemaining, maxCells) : rng.Next(1, maxCells + 1);

            var shape = PickReagentShape(rng, targetSize);
            int actual = CellCount(shape);
            cellsRemaining -= actual;
            result.Add((shape, actual));
        }

        return result;
    }

    private static bool[,] PickReagentShape(SeededRandom rng, int targetCells)
    {
        // Filter shapes that match the target cell count
        var matching = ReagentShapes.Where(s => CellCount(s) == targetCells).ToArray();
        if (matching.Length > 0)
            return matching[rng.Next(matching.Length)];

        // Fallback: find closest shapes
        var closest = ReagentShapes
            .OrderBy(s => Math.Abs(CellCount(s) - targetCells))
            .Take(3)
            .ToArray();
        return closest[rng.Next(closest.Length)];
    }

    private List<List<AspectSet>> AssignAspects(
        SeededRandom rng, DifficultyParams difficulty,
        List<(bool[,] shape, int cellCount)> reagentShapes)
    {
        var result = new List<List<AspectSet>>();

        // For each reagent, assign 0..MaxAspectCount aspects spread across its cells
        for (int i = 0; i < reagentShapes.Count; i++)
        {
            var (shape, cellCount) = reagentShapes[i];
            var cellAspects = new List<AspectSet>();

            if (difficulty.MaxAspectCount == 0)
            {
                // No aspects — fill all cells with empty sets
                for (int c = 0; c < cellCount; c++)
                    cellAspects.Add(AspectSet.Empty);
            }
            else
            {
                // Pick how many distinct aspect types this reagent gets (1..MaxAspectCount)
                int numAspectTypes = rng.Next(1, difficulty.MaxAspectCount + 1);
                var chosenAspects = PickDistinctAspects(rng, numAspectTypes);

                for (int c = 0; c < cellCount; c++)
                {
                    // Each cell gets one aspect (or none with some probability)
                    if (rng.NextFloat() < 0.7f && chosenAspects.Count > 0)
                    {
                        var aspectId = chosenAspects[rng.Next(chosenAspects.Count)];
                        var aspect = Aspect.FromId(aspectId);
                        cellAspects.Add(new AspectSet(aspect));
                    }
                    else
                    {
                        cellAspects.Add(AspectSet.Empty);
                    }
                }
            }

            result.Add(cellAspects);
        }

        return result;
    }

    private static List<string> PickDistinctAspects(SeededRandom rng, int count)
    {
        var available = new List<string>(MvpAspects);
        rng.Shuffle(available);
        return available.Take(Math.Min(count, available.Count)).ToList();
    }

    private (MetaGrid? grid, List<Vector2I> positions) BuildMetaGridWithBags(
        SeededRandom rng, List<bool[,]> bagShapes, List<List<BagPropertyType>> bagProperties)
    {
        // Calculate meta-grid size: sum of bag widths + padding
        int totalWidth = bagShapes.Sum(b => b.GetLength(1)) + MetaGridPadding * (bagShapes.Count + 1);
        int maxHeight = bagShapes.Max(b => b.GetLength(0)) + MetaGridPadding * 2;

        var grid = new MetaGrid(totalWidth, maxHeight);
        var positions = new List<Vector2I>();

        // Place bags left-to-right with padding
        int currentCol = MetaGridPadding;
        for (int i = 0; i < bagShapes.Count; i++)
        {
            var shape = bagShapes[i];
            int bagRows = shape.GetLength(0);
            int bagCols = shape.GetLength(1);

            // Center vertically
            int row = (maxHeight - bagRows) / 2;
            var pos = new Vector2I(currentCol, row);

            var def = new BagDefinition($"bag_{i}", $"Bag {i}", shape, bagProperties[i]);
            var bag = new Bag(def);
            if (!grid.PlaceBag(bag, pos))
                return (null, positions);

            positions.Add(pos);
            currentCol += bagCols + MetaGridPadding;
        }

        return (grid, positions);
    }

    private List<Placement>? PlaceReagentsValidly(
        SeededRandom rng, MetaGrid grid, List<Reagent> reagents, List<ReactionRule> rules)
    {
        var placements = new List<Placement>();

        // Sort: Heavy reagents first (so they land at the bottom of Gravity bags),
        // then largest-first for better packing
        var indexed = reagents
            .Select((r, i) => (reagent: r, index: i))
            .OrderByDescending(x => HasHeavyAspect(x.reagent) ? 1 : 0)
            .ThenByDescending(x => x.reagent.Definition.CellCount)
            .ToList();

        foreach (var (reagent, originalIndex) in indexed)
        {
            bool placed = false;

            // Try multiple rotations
            for (int rot = 0; rot < 4 && !placed; rot++)
            {
                reagent.Rotation = rot;

                // Collect all valid positions (bag cells only)
                var candidates = GetCandidatePositions(grid, reagent);
                rng.Shuffle(candidates);

                foreach (var pos in candidates)
                {
                    if (!grid.CanPlaceReagent(reagent, pos))
                        continue;

                    // Check if placing here causes any negative reactions
                    var reactions = ChemistryEngine.EvaluateProspective(grid, reagent, pos, rules);
                    if (reactions.Count == 0)
                    {
                        grid.PlaceReagent(reagent, pos);
                        placements.Add(new Placement
                        {
                            ReagentIndex = originalIndex,
                            PositionCol = pos.Col,
                            PositionRow = pos.Row,
                            Rotation = rot,
                        });
                        placed = true;
                        break;
                    }
                }
            }

            if (!placed)
                return null; // Could not place this reagent safely
        }

        return placements;
    }

    private static List<Vector2I> GetCandidatePositions(MetaGrid grid, Reagent reagent)
    {
        var candidates = new List<Vector2I>();
        for (int col = 0; col < grid.Width; col++)
        {
            for (int row = 0; row < grid.Height; row++)
            {
                var pos = new Vector2I(col, row);
                if (grid.CanPlaceReagent(reagent, pos))
                    candidates.Add(pos);
            }
        }
        return candidates;
    }

    private static List<List<BagPropertyType>> AssignBagProperties(
        SeededRandom rng, DifficultyParams difficulty, List<bool[,]> bagShapes)
    {
        var result = new List<List<BagPropertyType>>();
        for (int i = 0; i < bagShapes.Count; i++)
        {
            var props = new List<BagPropertyType>();
            if (difficulty.AllowSpecialBags)
            {
                // ~30% chance each bag is a Gravity bag
                if (rng.NextFloat() < 0.3f)
                {
                    props.Add(BagPropertyType.Gravity);
                }
            }
            result.Add(props);
        }
        return result;
    }

    private static int CellCount(bool[,] shape)
    {
        int count = 0;
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (shape[r, c]) count++;
        return count;
    }

    private static bool HasHeavyAspect(Reagent reagent)
    {
        foreach (var aspectSet in reagent.Definition.AspectsPerCell)
        {
            if (aspectSet.Has("heavy"))
                return true;
        }
        return false;
    }

    private static DifficultyLevel InferDifficulty(DifficultyParams p) => p.BagCount switch
    {
        1 when p.MaxAspectCount == 0 => DifficultyLevel.Tutorial1,
        1 when p.MaxAspectCount == 1 => DifficultyLevel.Tutorial2,
        2 when p.ReagentCount <= 5 && !p.AllowSpecialBags => DifficultyLevel.Tutorial3,
        2 when p.AllowSpecialBags => DifficultyLevel.Tutorial4,
        2 => DifficultyLevel.Easy,
        3 => DifficultyLevel.Medium,
        _ => DifficultyLevel.Hard,
    };
}
