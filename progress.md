# Progress Log — Alchemist Courier MVP

Append-only log of Ralph loop iterations. Each completed task gets an entry below.

---

<!-- Ralph loop appends entries here -->

## 1.1 — Add adjacency detection to MetaGrid
- Date: 2026-03-03
- Commit: 25af2e8
- Notes: Added `GetAdjacentReagents(Reagent)` method to MetaGrid using HashSet for deduplication. Leverages existing `GetAdjacentCells(Vector2I)` and checks `OccupyingReagent` on each neighbor cell. 10 new unit tests covering empty neighbors, two adjacent reagents, diagonal-only (not adjacent), self-exclusion, multiple neighbors, multi-cell reagents, deduplication, and edge cells. All 57 tests pass (47 existing + 10 new).

## 1.2 — Add ReactionRule and ReactionResult data models
- Date: 2026-03-03
- Commit: 99f82b7
- Notes: Pure data models — no logic. Created 5 files: ReactionRule (trigger aspects, sensitivity, granularity, threshold, result type), ReactionResult (type, magnitude, affected cells, source rule), and enums SensitivityTier (Adjacent/SameBag/WholeInventory), Granularity (PerCell/PerItem), ReactionResultType (Explosion/Neutralization/Breakage/etc). Also added ToxicCloud to ReactionResultType for the Toxic+Toxic same-bag rule needed by task 1.3. Build succeeds, all 57 tests still pass.

## 1.3 — Add reaction rule database
- Date: 2026-03-03
- Commit: d79c247
- Notes: Static ReactionRuleDatabase with GetMvpRules() returning 7 Physical/Chemistry school rules: Explosion (Hot+Volatile, Adjacent, PerCell, threshold 0), Neutralization (Hot+Cold, SameBag, PerItem, threshold 0), Breakage (Sharp+Fragile, Adjacent, PerCell, threshold 0), Toxic Cloud (Toxic+Toxic, SameBag, PerItem, threshold 2), Overheating (Hot cumulative, WholeInventory, threshold 4), Hypothermia (Cold cumulative, WholeInventory, threshold 4), Poisoning (Toxic cumulative, WholeInventory, threshold 3). GetRulesForAspects() lookup handles three cases: distinct-aspect pairs (uses set superset check), same-aspect pairs like toxic+toxic, and single-aspect cumulative rules. 14 new tests, all 71 pass.

## 1.4 — Add ChemistryEngine evaluator
- Date: 2026-03-03
- Commit: ad14bca
- Notes: Static ChemistryEngine.Evaluate(MetaGrid, List<ReactionRule>) processes all three sensitivity tiers. Adjacent: iterates placed reagents, checks each cell's neighbors for matching trigger aspect pairs (both orderings), deduplicates reagent pairs via HashSet. SameBag: for each bag, collects distinct reagents, checks all pairs for distinct-aspect rules and counts items for same-aspect rules (toxic+toxic threshold). WholeInventory: sums aspect magnitudes (PerCell) or counts reagents (PerItem) across entire inventory, checks against threshold. Helper methods: ReagentHasAspect scans all cells, OrderPair normalizes for dedup. 23 new tests covering empty grid, adjacent explosion/gap/diagonal, breakage, neutralization same/different bags, toxic cloud, cumulative overheating/hypothermia/poisoning, mixed multi-reaction, multi-cell reagents, magnitude tracking, source rule references, safe reagents, empty rules. All 94 tests pass (71 existing + 23 new).

## 1.5 — Add prospective evaluation
- Date: 2026-03-03
- Commit: 0c84964
- Notes: Added EvaluateProspective(MetaGrid, Reagent, Vector2I, List<ReactionRule>) to ChemistryEngine. Uses place-evaluate-remove pattern with try/finally to guarantee grid cleanup. Returns empty list if placement is invalid (out of bounds, occupied, no bag). 6 new tests: safe spot returns empty, adjacent to volatile returns explosion, grid unchanged after call (reagent count, cell occupancy, IsPlaced all restored), invalid position returns empty, cumulative threshold crossing detected prospectively, occupied position returns empty. All 100 tests pass (94 existing + 6 new).

## 2.1 — Add SeededRandom deterministic RNG
- Date: 2026-03-03
- Commit: b8a965d
- Notes: Simple wrapper around System.Random. Constructor takes int seed, exposes Seed property. Methods: Next(), Next(max), Next(min,max), NextFloat(), Shuffle<T>(List<T>) using Fisher-Yates. Placed at src/Core/SeededRandom.cs with namespace AlchemistCourier.Core. 10 new tests covering determinism (same seed = same sequence), different seeds diverge, range bounds for Next(max)/Next(min,max)/NextFloat(), shuffle determinism, element preservation, and edge cases (empty/single-element lists). All 110 tests pass (100 existing + 10 new).

## 2.2 — Add PuzzleState and DifficultyParams data models
- Date: 2026-03-03
- Commit: ed96186
- Notes: Created 4 source files under src/Core/Puzzle/: DifficultyLevel enum (Tutorial1-5, Easy, Medium, Hard), PuzzleType enum (FitAllSafely, MaximizeValue, MeetRequirement), DifficultyParams (7 generation params + ForLevel() factory with values from MVP spec table), and PuzzleState with PuzzleBag/PuzzleReagent/Placement nested DTOs. Shapes stored as bool[][] jagged arrays for JSON serialization with ToShapeMatrix()/FromMatrix() helpers for 2D conversion. Positions stored as separate Col/Row int properties to avoid Vector2I serialization issues. Bag properties stored as string list (flexible for future property types). Used System.Text.Json (built into net8.0) for serialization — no extra deps needed. 12 new tests covering JSON round-trip for all types, field preservation, shape conversion, DifficultyParams factory and constructor. All 122 tests pass (110 existing + 12 new).

## Code Review — 2026-03-03
- Reviewed commits: b970518..0975041 (tasks 1.5, 2.1)
- Findings: 0 (P0: 0, P1: 0, P2: 0)
- Fixed: 0, Tasks created: 0

## 2.3 — Add PuzzleGenerator
- Date: 2026-03-03
- Commit: 5f43156
- Notes: Implemented disassemble-a-solution algorithm in PuzzleGenerator.Generate(seed, DifficultyParams). Steps: (1) generate rectangular bag shapes from difficulty params, (2) generate reagent shapes (1-4 cells from a pool of 11 polyomino shapes), (3) assign aspects per cell based on MaxAspectCount, (4) build MetaGrid with bags placed left-to-right with padding, (5) place reagents largest-first trying all rotations and shuffled candidate positions, using ChemistryEngine.EvaluateProspective to reject placements that cause reactions, (6) record solution and build PuzzleState. Retries up to 50 times if placement fails. 16 new tests covering determinism, solution validity (no reactions when applied), all reagents placed, within bag bounds, no overlaps, multiple difficulty levels, Tutorial1 has no aspects, metadata correctness. All 138 tests pass (122 existing + 16 new).

## 2.4 — Add PuzzleSolver / validator
- Date: 2026-03-03
- Commit: 8fb9262
- Notes: Static PuzzleSolver with two public methods: ValidateSolution(PuzzleState) validates PuzzleState.Placements, IsSolvable(PuzzleState) validates PuzzleState.Solution. Core ValidatePlacements checks: (1) all reagents placed exactly once (count match + HashSet for duplicate detection + range check), (2) no overlaps and all within bag bounds (via CanPlaceReagent sequential placement), (3) no negative reactions (ChemistryEngine.Evaluate). Includes ReconstructGrid/ReconstructReagents internal helpers for building runtime objects from serialized PuzzleState. 11 new tests: valid placements pass, empty placements fail, missing reagent fails, overlapping placements fail, out-of-bag placement fails, reactive placement (hot+volatile adjacent) fails, IsSolvable on generated puzzles passes, empty solution fails, multiple difficulties pass, duplicate reagent index fails, invalid reagent index fails. All 149 tests pass (138 existing + 11 new).
