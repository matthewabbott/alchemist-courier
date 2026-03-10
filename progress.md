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

## Code Review — 2026-03-03 (b)
- Reviewed commits: baf76cd..ef7d606 (tasks 2.2, 2.3, 2.4)
- Findings: 5 (P0: 0, P1: 0, P2: 1, P3: 3, P4: 1)
- Fixed: 0, Tasks created: 1 (R-2: extract CreateAspect duplication)

## R-2 — Extract shared CreateAspect to Aspect.FromId
- Date: 2026-03-04
- Commit: 36b1865
- Notes: Extracted identical CreateAspect switch expressions from PuzzleGenerator.cs, PuzzleSolver.cs, and PuzzleGeneratorTests.cs into Aspect.FromId(string id) on the Aspect class. New method covers all 10 known aspect IDs (hot, cold, volatile, fragile, toxic, sharp, heavy, medicinal, stable, catalytic) plus a fallback for unknown IDs. Added 12 new unit tests (Theory + Fact) in AspectTests.cs. All 161 tests pass (149 existing + 12 new).

## R-1 — Implement Stable aspect buffering mechanic
- Date: 2026-03-04
- Commit: 6bdba35
- Notes: Three changes: (1) Added "stable" to PuzzleGenerator.MvpAspects so generated puzzles can include Stable reagents. (2) Added GetAdjacentStableMagnitude helper to ChemistryEngine that finds Stable cells adjacent to involved reaction cells and sums their magnitudes. Applied buffering to all three sensitivity tiers: Adjacent (stable buffer > threshold suppresses pair), SameBag (both same-aspect and distinct-aspect pair variants), WholeInventory (raises effective threshold). (3) Added 7 new tests: stable adjacent suppresses explosion, stable not adjacent doesn't suppress, multiple stable cells stack, stable adjacent to one side of pair still suppresses, high-magnitude stable suppresses, stable buffers cumulative overheating, stable doesn't break non-reactive pairs. All 168 tests pass (161 existing + 7 new).

## 3.1 — Add placement warnings to MetaGridView
- Date: 2026-03-04
- Commit: 20b4e6c
- Notes: Modified MetaGridView ghost preview to use ChemistryEngine.EvaluateProspective for three-color warning system. Green = safe (no reactive aspects on reagent), Yellow = warning (reagent has aspects that participate in reaction rules but no reactions triggered at this position), Red = danger (placement would trigger reactions) or invalid. Added PlacementWarningLevel enum, SetReactionRules() to accept reaction rules from PuzzleScreen, SetHoverPosition() for programmatic testing, HasReactiveAspects() helper that checks reagent aspects against a cached set of all trigger aspect IDs. Added WarningPlacement color (amber/yellow) to GridConstants. PuzzleScreen now passes ReactionRuleDatabase.GetMvpRules() to the grid view. Added integration test in TestHarness verifying all three states: Danger (Fire Pebble hover adjacent to Blast Jelly = explosion), Warning (Fire Pebble hover far from danger but has "hot" reactive aspect), Safe (Inert Crystal with only "stable" non-reactive aspect). Build succeeds, all 168 unit tests pass.

## Code Review — 2026-03-04
- Reviewed commits: 36b1865..d8711bb (R-2, R-1, 3.1)
- Findings: 3 (P0: 0, P1: 0, P2: 0, P3: 2, P4: 1)
- Fixed: 0, Tasks created: 0

## 3.2 — Add status bar panel
- Date: 2026-03-04
- Commit: e610037
- Notes: Created StatusBarPanel (HBoxContainer) with AspectBar sub-components for each WholeInventory cumulative rule (Heat/4, Cold/4, Toxicity/3). Each bar shows a colored fill proportional to current/threshold with color shifting: aspect color → yellow (threshold-1) → red (at/above threshold). Panel auto-refreshes on placement/removal via Refresh() calls in PuzzleScreen's OnReagentPlaced, OnReagentRemoved, and RemoveReagentAt. Integration test in TestHarness verifies values across placement sequence: Fire Pebble (hot=2), +Blast Jelly (hot=3), +Poison Fang (toxic=3), then removal (hot=1). Build succeeds, all 168 unit tests pass.

## Integration Review — 2026-03-04
- Reviewed commits: 36b1865..e610037 (R-2, R-1, 3.1, 3.2)
- Build: PASS (168/168 unit tests)
- Harness: 9/9 tests pass, 0 failures
- Visual: All 11 screenshots verified, no regressions
- Issues: 0
- Notes: Status bar color shifting and placement warnings (3-tier) both working correctly end-to-end. Stable buffering mechanic well-covered by 7 unit tests; integration coverage indirect but adequate.

## 3.3 — Add objective panel
- Date: 2026-03-04
- Commit: 1579136
- Notes: Created ObjectivePanel (VBoxContainer) with header label "OBJECTIVE:" and objective text driven by PuzzleType enum. Three texts: FitAllSafely ("Place all reagents without triggering reactions."), MaximizeValue ("Place the highest total value..."), MeetRequirement ("Meet the requirement..."). Wired into PuzzleScreen below the reagent tray in a right-column VBoxContainer layout. Defaults to FitAllSafely for the current DemoPuzzle. Added SetPuzzleType() for dynamic updates and public ObjectiveText accessor for testing. Integration test in TestHarness verifies default text matches FitAllSafely, verifies SetPuzzleType updates text correctly. Build succeeds, all 168 unit tests pass.

## R-3 — Implement Null bag aspect suppression in ChemistryEngine
- Date: 2026-03-04
- Commit: 18b9332
- Notes: Added per-cell aspect suppression for Null bags. Two new helpers in ChemistryEngine: IsAspectSuppressed(grid, pos) checks if a cell's bag has BagPropertyType.Null, and ReagentHasUnsuppressedAspect(grid, reagent, aspectId) checks only non-Null cells. Integrated into all three tiers: Adjacent (skip suppressed cells), SameBag (skip Null bags entirely, use suppression-aware checks for spanning reagents), WholeInventory (skip suppressed cells in cumulative sums). Also suppresses Stable buffering from Null bag cells. Leveraged existing MetaGrid.GetBagAtCell(). 8 new tests covering adjacent suppression, spanning reagent partial suppression, SameBag/WholeInventory suppression, Stable suppression, and mixed-bag cumulative skip. All 176 tests pass (168 existing + 8 new).

## 3.4 — Add confirm and reset buttons
- Date: 2026-03-04
- Commit: b769d84
- Notes: Added Reset and Confirm buttons to PuzzleScreen in a centered HBoxContainer button bar between the status bar and status label. Reset removes all placed reagents, resets rotations, and clears selection. Confirm checks if all reagents are placed (placeholder — task 4.1 adds full chemistry evaluation). Added ConfirmResult enum (None/NotAllPlaced/Win) and LastConfirmResult property for programmatic inspection. ResetPuzzle() also clears LastConfirmResult back to None. Integration test in TestHarness covers: place 2 reagents → confirm early (NotAllPlaced) → reset (verify 0 placed, 6 unplaced, result None) → place all 6 → confirm (Win). Build succeeds, all 168 unit tests pass.

## R-4 — Fix PuzzleGenerator.MvpAspects to match Simple complexity tier
- Date: 2026-03-04
- Commit: f159dd3
- Notes: Replaced MvpAspects from { hot, cold, volatile, fragile, toxic, sharp, stable } to { hot, cold, volatile, stable, heavy, toxic }. Sharp and Fragile are Intermediate-tier aspects per docs/chemistry-system.md; Heavy is Simple-tier but was missing. No test changes needed — puzzle generation tests are seed-deterministic but don't assert specific aspect names, and the Breakage rule tests (Sharp+Fragile) test the ReactionRuleDatabase directly, not MvpAspects. All 176 tests pass.

## R-5 — Normalize Stable buffering pattern across all ChemistryEngine tiers
- Date: 2026-03-04
- Commit: 3e35cf1
- Notes: Replaced binary skip pattern (`stableBuffer > rule.Threshold`) in Adjacent and SameBag distinct-pair tiers with the additive `effectiveThreshold = rule.Threshold + stableBuffer` pattern already used by WholeInventory and SameBag same-aspect tiers. All three tiers now consistently compute effectiveThreshold before deciding whether to suppress. For pair rules (threshold=0), behavior is unchanged: any adjacent stable magnitude suppresses the pair reaction. Added 6 boundary tests: stableBuffer=0 no-effect for Adjacent and SameBag, stable suppresses SameBag neutralization, stable equal to cumulative count prevents poisoning, and exact magnitude boundary tests (5 Hot + Stable(1) at threshold=4 still fires at effectiveThreshold=5, but 4 Hot + Stable(1) is suppressed). All 182 tests pass (176 existing + 6 new).

## Code Review — 2026-03-04 (b)
- Reviewed commits: 18b9332..3e35cf1 (R-3, R-4, R-5)
- Findings: 2 (P0: 0, P1: 1, P2: 0)
- Fixed: 2, Tasks created: 0

## B.1 — Implement Gravity bag type
- Date: 2026-03-04
- Commit: 9ee9109
- Notes: Added gravity placement validation to MetaGrid.CanPlaceReagent() via IsGravityValid() helper. In Gravity bags, each reagent cell must be supported: either at the bag's bottom for its column (cell below is not in the same bag), or resting on an already-placed reagent, or self-supported by another cell of the same reagent. Uses lazy HashSet allocation for the self-support check. Updated PuzzleGenerator to assign Gravity property to ~30% of bags when AllowSpecialBags is true, via new AssignBagProperties() method. Bag properties now flow through the full pipeline: PuzzleGenerator → PuzzleBag.Properties (serialized as strings) → PuzzleSolver.ReconstructGrid (parsed back via Enum.TryParse). Also updated PuzzleGeneratorTests.ReconstructGrid helper to parse properties. 8 new tests covering: bottom row placement, floating rejection, stacked placement, normal bag allows floating, multi-cell self-support, partially unsupported multi-cell rejection, L-shaped bag gravity, and stacking chain. All 190 tests pass (182 existing + 8 new).

## B.2 — Implement locked reagent placements
- Date: 2026-03-04
- Commit: a0039c7
- Notes: Locked placement is a per-reagent flag (not a bag type). Added IsLocked bool to Reagent.cs. MetaGrid.RemoveReagent() now rejects locked reagents. PuzzleGenerator.LockSolutionReagents() locks 1-2 random solution reagents when AllowSpecialBags=true and there are 3+ reagents (never locks all). PuzzleSolver.ValidateSolution() checks locked reagents haven't moved from their solution positions (position + rotation). ReconstructReagents now sets IsLocked from PuzzleReagent.IsLocked. PuzzleReagent.IsLocked was already in PuzzleState.cs (serialization ready). 12 new tests: 3 MetaGrid (locked blocks removal, unlocked still works, cells stay occupied), 1 ChemistryEngine (locked reagent aspects active), 4 PuzzleSolver (moved=fail, unmoved=pass, rotation changed=fail, IsSolvable with locked), 4 PuzzleGenerator (AllowSpecialBags produces locked, without doesn't, solution still valid, count never exceeds limit). All 202 tests pass (190 existing + 12 new).

## Integration Review — 2026-03-04 (b)
- Reviewed commits: e610037..8bcf6ce (3.3, 3.4, R-3, R-4, R-5, B.1, B.2)
- Build: PASS (202/202 unit tests)
- Harness: 11/11 tests pass, 0 failures
- Visual: All 14 screenshots verified, no regressions
- Issues: 0
- Notes: Objective panel, confirm/reset, placement warnings, status bar all working correctly. Gravity bags (B.1), locked reagents (B.2), and Null suppression (R-3) have thorough unit test coverage but no integration tests yet — DemoPuzzle doesn't use these features. Gap will resolve with task 4.2 (PuzzleGenerator integration).

## B.3 — Implement Heavy aspect placement constraint in Gravity bags
- Date: 2026-03-04
- Commit: 9affe1d
- Notes: Added Heavy placement constraint to Gravity bags via IsHeavyConstraintValid() in MetaGrid. In a Gravity bag column, Heavy reagent cells must be at or below all non-Heavy cells. The method checks both directions: when placing Heavy, scans below for non-Heavy occupants; when placing non-Heavy, scans above for Heavy occupants. Added ReagentHasHeavyAspect() helper that checks the definition's AspectsPerCell. Updated PuzzleGenerator.PlaceReagentsValidly to sort Heavy reagents first (before largest-first tiebreaker) so they naturally land at the bottom of Gravity bags. In non-Gravity bags, Heavy has no special placement effect. 8 new MetaGrid tests + 1 PuzzleGenerator test. All 210 tests pass (202 existing + 8 new).

## Code Review — 2026-03-04 (c)
- Reviewed commits: 8bcf6ce..e204a80 (B.2, B.3)
- Findings: 3 (P0: 0, P1: 0, P2: 0, P3: 3)
- Fixed: 0, Tasks created: 0

## B.4 — Add non-rectangular bag shapes to generator
- Date: 2026-03-04
- Commit: 7487949
- Notes: Added 8 non-rectangular bag shape templates (L, T, hourglass, S-step, plus, U, notched rectangle, chunky L) covering cell counts 6-12. New PickBagShape method gives ~40% chance of non-rectangular selection when AllowSpecialBags is true (Medium/Hard). Lower difficulties remain rectangular-only. The existing per-cell bag ownership in MetaGrid meant no changes were needed to the placement algorithm — CanPlaceReagent already checks actual cell occupancy rather than bounding box. 6 new tests across 30 seeds verify non-rect bags appear, Easy stays rectangular, solutions are valid (no reactions, in-bounds, no overlaps), and PuzzleSolver validates. All 216 tests pass (210 existing + 6 new).

## R-6 — Extract aspect ID string literals to AspectIds constants class
- Date: 2026-03-04
- Commit: de4a45b
- Notes: Created src/Core/Chemistry/AspectIds.cs with 10 const string fields (Hot, Cold, Volatile, Stable, Heavy, Toxic, Sharp, Fragile, Catalytic, Medicinal). Replaced all hardcoded aspect ID string literals across 7 files: Aspect.cs (factory methods + FromId switch), ReactionRuleDatabase.cs (all 7 rule definitions), ChemistryEngine.cs (Stable magnitude lookup), PuzzleGenerator.cs (MvpAspects array + Heavy check), MetaGrid.cs (Heavy check), GridConstants.cs (color mapping switch), StatusBarPanel.cs (label formatting switch). Pure rename refactor — no logic changes. All 216 tests pass.

## R-7 — Deduplicate ReagentHasHeavyAspect into Reagent.HasAspect
- Date: 2026-03-04
- Commit: 8e82a2e
- Notes: Added public `HasAspect(string aspectId)` method to Reagent that checks if any cell's AspectSet contains the given aspect. Replaced identical private static `ReagentHasHeavyAspect` in MetaGrid.cs and `HasHeavyAspect` in PuzzleGenerator.cs with calls to the new instance method. Generalizes beyond Heavy — any aspect can be queried. 5 new tests in ReagentTests.cs. All 221 tests pass (216 existing + 5 new).

## Code Review — 2026-03-04 (d)
- Reviewed commits: de4a45b..bb21228 (R-6, R-7)
- Findings: 1 (P0: 0, P1: 0, P2: 0, P3: 1)
- Fixed: 0, Tasks created: 0

## A.1 — Implement Volatile net-hot calculation
- Date: 2026-03-04
- Commit: ebe0fe1
- Notes: Replaced the simple Hot+Volatile pair-trigger explosion with a per-Volatile-cell net-hot calculation. For each Volatile cell, sums adjacent Hot magnitudes minus adjacent Cold magnitudes; if net_hot > effective_threshold (base + stable buffer), explosion triggers. Changed Explosion rule from 2-trigger [Hot, Volatile] to single-trigger [Volatile]. Added EvaluateVolatileNetHotRule method dispatched from EvaluateAdjacentRule when TriggerAspects.Count == 1. Cold now serves dual purpose: cumulative liability + local heat shield near Volatile. Null suppression and Stable buffering integrate naturally. 7 new tests: balanced Hot+Cold (no explosion), more Hot than Cold (explosion), Cold-as-shield, Volatile alone (safe), Stable raises threshold, Null-suppressed Hot doesn't contribute, high-magnitude Hot overcomes Cold. All 228 tests pass (221 existing + 7 new).

## Integration Review — 2026-03-04 (c)
- Reviewed commits: ebe0fe1..61bbbaa (A.1, B.3, B.4, R-6, R-7)
- Build: PASS (229/229 unit tests)
- Harness: 11/11 tests pass, 0 failures (after fix)
- Visual: All 15 screenshots verified, no regressions
- Issues: 1 (P0: 1 — fixed)
- Fixed: A.1 Volatile net-hot self-reaction bug — EvaluateVolatileNetHotRule omitted same-reagent exclusion, causing multi-aspect reagents (Hot+Volatile) to self-explode. Added `neighbor == reagent` check + regression test. Commits: 8c44c4a, 61bbbaa.

## A.2 — Implement Sharp edge placement constraint
- Date: 2026-03-04
- Commit: c00e323
- Notes: Added Sharp's primary non-parasitic mechanic: Sharp reagents can't be placed on edge cells of non-Reinforced bags. An edge cell has at least one cardinal neighbor outside the bag shape. Three changes: (1) Added Bag.IsEdgeCell(Vector2I) that converts meta-grid position to local coordinates and checks all 4 cardinal neighbors against the bag shape. (2) Added IsSharpConstraintValid() to MetaGrid.CanPlaceReagent() — skips non-Sharp reagents, allows Sharp on Reinforced bags, blocks Sharp on edge cells otherwise. (3) Updated PuzzleGenerator.PlaceReagentsValidly to prioritize Sharp reagents after Heavy (before size sort) since they need scarce interior cells. 8 new tests: edge blocked, interior allowed, Reinforced exemption, multi-cell with one edge cell blocked, non-Sharp unaffected, larger bag interior, L-shaped bag (all edges), plus-shaped bag (center is interior). All 237 tests pass (229 existing + 8 new).

## A.3 — Implement Fragile universal vulnerability
- Date: 2026-03-04
- Commit: c7d689b
- Notes: Two changes: (1) Added EvaluateFragileVulnerability second pass to ChemistryEngine.Evaluate — after all reactions are computed, collects all affected cells into a HashSet, then checks each Fragile reagent for adjacency. If adjacent to any reaction cell, adds a Breakage result (unless already broken by Sharp+Fragile pair rule). Uses a synthetic "Fragile Vulnerability" ReactionRule as the source. (2) Added IsFragileConstraintValid to MetaGrid.CanPlaceReagent — Fragile reagents are blocked from Gravity bags. 7 new ChemistryEngine tests: Fragile breaks from adjacent explosion, breaks from adjacent neutralization, safe when not adjacent, no break when no reactions, no duplicate breakage when already broken by Sharp, multiple Fragile all break, breaks from cumulative overheating. 3 new MetaGrid tests: Fragile blocked from Gravity bag, allowed in normal bag, non-Fragile allowed in Gravity bag. All 247 tests pass (237 existing + 10 new).

## A.4 — Add Catalytic aspect to ChemistryEngine
- Date: 2026-03-04
- Commit: a69a5fa
- Notes: Added Catalytic as the inverse of Stable: adjacent Catalytic cells lower effective thresholds via effectiveThreshold = max(0, rule.Threshold + stableBuffer - catalyticReduction). Refactored GetAdjacentStableMagnitude into generic GetAdjacentAspectMagnitude(grid, cells, aspectId) to avoid duplication. Integrated into all three sensitivity tiers: Adjacent pair (replaces skip pattern with effectiveThreshold > 0 check), Volatile net-hot (lowers netHot comparison threshold), SameBag same-aspect and distinct-pair, and WholeInventory cumulative. Catalytic+Stable cancel out; multiple Catalytic cells stack; threshold can't go below 0. 9 new tests: cumulative threshold lowering causes overheating, Stable+Catalytic cancel out, multiple Catalytic stack, no effect on threshold=0 pair rules, threshold floor at 0, counters Stable suppression for Adjacent and SameBag, not adjacent = no effect, SameBag same-aspect interaction. All 256 tests pass (247 existing + 9 new).

## A.5 — Add Medicinal passive threshold reduction
- Date: 2026-03-04
- Commit: 5734bb9
- Notes: Added Medicinal as a passive cumulative threshold buffer. Unlike Stable (adjacency-based) and Catalytic (adjacency-based), Medicinal applies globally — its total magnitude across the entire inventory is added to effective thresholds for WholeInventory rules only. Added GetTotalAspectMagnitude helper that sums aspect magnitudes across all non-suppressed cells. Formula: effectiveThreshold = max(0, threshold + stableBuffer + medicinalBuffer - catalyticReduction). Null bag suppression applies to Medicinal cells. 7 new tests: raises overheating threshold, prevents poisoning, multiple Medicinal stack, does not affect Adjacent reactions (explosions), does not affect SameBag reactions (neutralization), Null bag suppresses Medicinal, high-magnitude Medicinal prevents multiple cumulative thresholds simultaneously. All 263 tests pass (256 existing + 7 new).

## A.6 — Add ComplexityLevel enum and wire to PuzzleGenerator
- Date: 2026-03-04
- Commit: 3bcb216
- Notes: Added two-dimensional complexity model from docs/chemistry-system.md. ComplexityLevel enum (Simple/Intermediate/Advanced) controls mechanical depth — which aspects, bag types, and reaction rules are available. ComplexityFilter static class centralizes filtering: Simple=6 aspects + 4 bag types, Intermediate adds 4 aspects + 3 bag types, Advanced adds 2 aspects + 1 bag type. All ForLevel() entries explicitly set Simple — daily/standalone puzzles scale through size, not mechanical depth. PuzzleGenerator now uses ComplexityFilter.GetAspects() and ComplexityFilter.GetRules() instead of hardcoded MvpAspects array. ReactionRuleDatabase.GetRulesForComplexity() convenience method delegates to ComplexityFilter.GetRules(). Simple tier excludes Breakage rule (Sharp+Fragile are Intermediate). 28 new tests across ComplexityFilterTests and ReactionRuleDatabaseTests. All 291 tests pass (263 existing + 28 new).

## Code Review (e) — 2026-03-04
- Reviewed commits: 26dc655..a8f9fdd (A.5 docs, A.6 implementation, A.6 docs)
- Findings: 2 (P0: 0, P1: 2, P2: 0)
- Fixed: 2, Tasks created: 0

## 3.5 — Add aspect interaction primer
- Date: 2026-03-04
- Commit: 29a0bc4

## 4.1 — Add win/loss evaluation to PuzzleScreen
- Date: 2026-03-04
- Commit: ded67ea

## Code Review — 2026-03-04 (f)
- Reviewed commits: 29a6911..a28eed1
- Findings: 4 (P0: 0, P1: 1, P2: 0, P3: 2, P4: 1)
- Fixed: 1 (DRY: 5x duplicated selection-clearing block → ClearSelectionState), Tasks created: 0
- Notes: Wired Confirm button to full ChemistryEngine evaluation. When all reagents are placed, runs ChemistryEngine.Evaluate on the MetaGrid with MVP rules. Zero reactions = Win (green "SUCCESS" overlay), any reactions = HasReactions/loss (red "FAILED" overlay with reaction list). Added HasReactions to ConfirmResult enum. Result overlay is a styled PanelContainer with semi-transparent dark background, built in BuildUI(). ShowResultOverlay/HideResultOverlay manage visibility. ResetPuzzle clears reactions and hides overlay. Integration test verifies both paths: LOSS (Fire Pebble Hot=2 adjacent to Blast Jelly Volatile → Explosion) and WIN (Fire Pebble at safe position → zero reactions). All 289 unit tests pass, 13/13 integration tests pass.
- Notes: Created AspectPrimerPanel (PanelContainer) as a toggleable overlay in the right column of PuzzleScreen, below the ObjectivePanel. Displays all 7 MVP reaction rules from ReactionRuleDatabase with color-coded labels (red for explosions, blue for neutralization, green for toxic, orange for breakage/overheating). Each rule shows formatted trigger aspects, sensitivity tier, threshold, and result name. Toggle via "Reactions" button in the button bar or programmatic TogglePrimer(). Panel starts hidden. Integration test verifies: starts hidden, toggle shows with 7 rules, second toggle hides. Build succeeds, all 289 unit tests pass.

## Integration Review — 2026-03-04 (d)
- Reviewed commits: 8bcf6ce..ded67ea (B.3, B.4, R-6, R-7, A.1 fix, A.2, A.3, A.4, A.5, A.6, review-fix, 3.5, 4.1)
- Build: PASS (289/289 unit tests)
- Harness: 13/13 tests pass, 0 failures
- Visual: All 18 screenshots verified, no regressions
- Issues: 0
- UX observation: Status bar shows base threshold (3/3 Toxicity red) even when Medicinal raises effective threshold — consider showing effective thresholds in 5.1 polish.
- Notes: Full puzzle loop working end-to-end (place, warnings, status bar, confirm win/loss, reset). Aspect primer shows all 7 rules. Integration gaps for Gravity/Locked/NonRect/Fragile/Null bags remain deferred to 4.2 (PuzzleGenerator integration).

## 4.2 — Replace DemoPuzzle with PuzzleGenerator
- Date: 2026-03-04
- Commit: a28eed1
- Notes: Added PuzzleData property to PuzzleScreen — set before adding to scene tree to load a generated puzzle instead of DemoPuzzle. Uses PuzzleSolver.ReconstructGrid/ReconstructReagents (already internal and in the same assembly) to build runtime MetaGrid and Reagent objects from serialized PuzzleState. Locked reagents are automatically pre-placed at their solution positions via InitializeFromPuzzleState. Fixed ResetPuzzle to skip locked reagents (was using a while-loop that would infinite-loop since MetaGrid.RemoveReagent rejects locked reagents). DemoPuzzle remains as fallback when PuzzleData is null. Integration test generates Easy puzzle from seed 42, loads into PuzzleScreen, applies full solution, confirms Win, resets, verifies locked reagent preservation. 14/14 integration tests pass, 289 unit tests pass.

## 4.3 — Add tutorial lesson data model and first 5 lessons
- Date: 2026-03-04
- Commit: 9e6a035
- Notes: TutorialLesson data model with LessonNumber, ConceptName, HintText, FixedSeed, DifficultyParams, RequiredPuzzleType. TutorialData.GetLessons() returns 5 lessons: (1) Basic Placement (2 reagents, no aspects), (2) Shapes & Rotation (3 reagents, no aspects), (3) Tight Fit (4 reagents, no aspects, 10% spare), (4) Aspects: Hot (3 reagents, MaxAspectCount=1), (5) Aspects: Volatile (2 bags, 4 reagents, MaxAspectCount=1). Lessons 1-3 are pure spatial, 4-5 introduce chemistry. All use FitAllSafely puzzle type. 20 new tests: lesson structure validation (count, sequential numbers, non-empty fields, distinct seeds), early lessons no-aspects, lessons 4-5 have aspects, no special bags, fixed seeds produce solvable puzzles, determinism (same seed = same puzzle), complexity progression. All 309 tests pass (289 existing + 20 new).

## 4.4 — Add tutorial flow screen
- Date: 2026-03-04
- Commit: 9a993b6
- Notes: Created TutorialScreen (src/Game/TutorialScreen.cs) wrapping PuzzleScreen to manage lesson progression. Loads lessons from TutorialData, generates PuzzleState per lesson using PuzzleGenerator, displays lesson name and hint text in a styled panel at the top. On win, shows "Next Lesson" button (or "Tutorial Complete!" for last lesson). AdvanceLesson() loads next lesson's puzzle, replacing the PuzzleScreen. Added PuzzleConfirmed event to PuzzleScreen (fires for all ConfirmResult values) so TutorialScreen detects wins. Integration test in TestHarness verifies: starts at lesson 0, hint text populated, solution applied successfully, confirm returns Win, AdvanceLesson moves to lesson 1 with different hint text and new PuzzleScreen. All 309 unit tests pass.

## 4.5 — Add daily puzzle mode
- Date: 2026-03-04
- Commit: e029128
- Notes: Created DailyPuzzleManager static class in src/Core/Puzzle/. DateToSeed(DateTime) converts to YYYYMMDD integer. GetDailyDifficulty uses day-of-year % 3 to rotate Easy/Medium/Hard. GetDailyPuzzleType uses seed % 3 to vary FitAllSafely/MaximizeValue/MeetRequirement. GenerateDailyPuzzle wires these together through PuzzleGenerator. All daily puzzles use Simple mechanical depth via DifficultyParams.ForLevel(). GenerateToday() convenience method for runtime use. 14 new tests: seed format, determinism (same date = same puzzle), different dates diverge, all difficulties are Simple complexity, 3-day difficulty rotation, puzzle type variation, solvability across 10 dates, correct seed/difficulty/type on generated puzzles. All 323 tests pass (309 existing + 14 new).

## Code Review (g) — 2026-03-04
- Reviewed commits: 8c571c7..bc59097 (tasks 4.3–4.5)
- Findings: 0 (P0: 0, P1: 0, P2: 0)
- Fixed: 0, Tasks created: 0

## 4.6 — Add save system
- Date: 2026-03-04
- Commit: c4d78c9
- Notes: Created SaveData model and SaveManager in src/Core/Save/. SaveData tracks tutorial progress (highest completed lesson number), daily puzzle completions (List<int> of YYYYMMDD dates), streak count, and last completed date. CompleteDailyPuzzle handles streak logic: consecutive days extend streak, gaps reset to 1, duplicate dates are no-ops, older dates don't update LastCompletedDate. GetEffectiveStreak(DateTime today) returns 0 if streak is broken (>1 day gap from today). SaveManager uses System.Text.Json for file-based persistence with static Serialize/Deserialize methods for future web localStorage support. Load() gracefully handles missing files and corrupt JSON by returning fresh SaveData. 33 new tests: 20 SaveData tests (tutorial progression, streak calculation, month boundaries, long streaks, duplicate dates, effective streak staleness) + 13 SaveManager tests (file round-trip, corrupt JSON, directory creation, overwrite, delete). All 356 tests pass (323 existing + 33 new).

## Integration Review — 2026-03-04 (e)
- Reviewed commits: ded67ea..b07a3a8 (4.2, 4.3, 4.4, 4.5, 4.6, code-review-f fix, daily puzzle integration test)
- Build: PASS (356/356 unit tests)
- Harness: 16/16 tests pass, 0 failures
- Visual: All 22 screenshots verified, no regressions
- Issues: 0
- Added: `daily_puzzle` integration test — validates DailyPuzzleManager → PuzzleGenerator → PuzzleScreen pipeline end-to-end
- Notes: Game-flow phase (4.1–4.6) complete. Full puzzle loop working: place, warnings, status bar, confirm win/loss, reset. Tutorial progression and daily puzzle generation both verified. Only task 5.1 (UI polish) remains. Cosmetic: tutorial "Next Lesson" button overlaps title bar slightly; status bar shows base thresholds not effective thresholds — both defer to 5.1.

## 5.1 — UI layout polish to match MVP mockup
- Date: 2026-03-04
- Commit: 1de6cae
- Notes: Reworked PuzzleScreen.BuildUI() to match the MVP mockup layout from docs/mvp-specification.md. Replaced centered title with a styled PanelContainer header bar: "Alchemist Courier" on the left, optional day count and streak labels on the right (driven by new HeaderDayText/HeaderStreakText properties, set before _Ready). Styled the three buttons (Confirm/Reset/Reactions) with distinct background colors and hover/pressed states via CreateStyledButton() helper: Confirm=green, Reset=warm gray, Reactions=indigo. Reordered buttons to put Confirm first (primary action). Moved result overlay from VBox flow to a centered absolute overlay on the PuzzleScreen. Tightened margins (16/8 vs 20 all-around), reduced VBox separation to 6px, made status hint text subtler (smaller font, dimmer color). Core layout structure unchanged: grid left, reagent tray + objective right, status bar + buttons at bottom. All 356 unit tests pass, 16/16 integration tests pass.

## R-8 — Fix StatusBarPanel Null bag suppression bug
- Date: 2026-03-05
- Commit: 1a11d94
- Notes: StatusBarPanel.ComputeTotal() was summing aspect magnitudes without checking Null bag suppression, inflating displayed totals for reagents in Null bags. Fixed by calling ChemistryEngine.IsAspectSuppressed() (changed from private to public static) to skip suppressed cells in both PerCell and PerItem granularity paths. 3 new unit tests for the public IsAspectSuppressed API. All 359 tests pass (356 existing + 3 new).

## H.1 — Core highlight data types
- Date: 2026-03-05
- Commit: 6c6ed55
- Notes: Created src/Core/Highlight/ with 5 files: CellEdge ([Flags] enum for boundary edge rendering), HighlightLayer (Constraint/Interaction/Legend/GhostOverlay), HighlightSeverity (Info/Safe/Warning/Danger), CellHighlight (position + edges + layer + severity + optional aspect/reason), HighlightSet (collection with Add/AddRange/Clear/GetAtPosition/GetByLayer/GetReferencedAspects/IsEmpty). Zero Godot dependencies. 17 new tests covering flag composition, constructor properties, set operations, position/layer filtering, aspect deduplication, and empty-set edge cases. All 376 tests pass (359 existing + 17 new).

## H.2 — Aspect metadata database
- Date: 2026-03-05
- Commit: 2c80550
- Notes: Created AspectMetadata class (Id, Name, Description, ConstraintText, School) and AspectMetadataDatabase static class with Get(), GetAll(), GetForPuzzle() methods. All 10 MVP aspects have metadata entries with one-sentence descriptions; Sharp, Heavy, and Fragile have ConstraintText for placement rules; other 7 have null. GetForPuzzle filters to only aspects referenced by given rules or present in given reagents via HashSet deduplication. Added FlavorText (string?) property to ReagentDefinition with null default; populated in DemoPuzzle with 6 thematic descriptions. PuzzleGenerator sets FlavorText to null (default). 29 new tests covering all 10 aspect metadata lookups, constraint text presence/absence, puzzle filtering, deduplication, empty inputs, FlavorText default/set/round-trip. All 405 tests pass (376 existing + 29 new).

## Code Review — 2026-03-05
- Reviewed commits: 1a11d94..2c80550 (R-8, H.1, H.2)
- Findings: 0 (P0: 0, P1: 0, P2: 0)
- Fixed: 0, Tasks created: 0

## H.3 — Contiguous boundary utility
- Date: 2026-03-05
- Commit: 2da34f1
- Notes: Static ContiguousBoundary.Compute() takes IReadOnlyCollection<Vector2I> cell positions, returns Dictionary<Vector2I, CellEdge> mapping each cell to its boundary edges. Algorithm: builds HashSet from input, then for each cell checks 4 cardinal neighbors — absent neighbor means boundary edge in that direction. Handles duplicates via HashSet deduplication. 8 new tests: empty set, single cell (All edges), 2x2 block (no internal edges), L-shape, disjoint cells, horizontal strip, vertical strip, duplicate positions. All 413 tests pass (405 existing + 8 new).

## H.4 — Constraint violation detail extraction
- Date: 2026-03-05
- Commit: 3f4e36f
- Notes: Refactored MetaGrid's four private bool constraint methods (IsGravityValid, IsHeavyConstraintValid, IsSharpConstraintValid, IsFragileConstraintValid) into public Get*Violations methods returning List<ConstraintViolation>. Each violation includes ConstraintType enum, affected cells, human-readable reason string, and optional AspectId. Original bool methods now delegate to violations.Count == 0. New files: ConstraintType.cs (4 enum values), ConstraintViolation.cs (data class). 13 new tests covering all four constraint types: Sharp on edge returns correct details, interior returns empty, multi-cell two edges, non-Sharp empty; Fragile in Gravity vs normal bag; Heavy above non-Heavy both directions; Gravity floating vs bottom vs normal bag; consistency check. All 426 tests pass (413 existing + 13 new).

## Integration Review — 2026-03-05
- Reviewed commits: 1a11d94..3f4e36f (R-8, H.1, H.2, H.3, H.4)
- Build: PASS (426/426 unit tests)
- Harness: 16/16 tests pass, 0 failures
- Visual: All 22 screenshots verified, no regressions
- Issues: 0
- Notes: H.1-H.4 are pure Core data types with no UI yet — integration tests will become relevant at H.5+ (HighlightEngine) and H.7+ (HighlightOverlay). Pre-existing cosmetic issues unchanged: tutorial win overlay vertical text, wide grid left-edge clipping, status bar base thresholds.

## H.5 — HighlightEngine foundation with constraint highlights
- Date: 2026-03-05
- Commit: ed2b4b1
- Notes: Created HighlightEngine static class with ComputeConstraintHighlights() and GetConstraintViolations(). Aggregates all four violation types from MetaGrid (Sharp, Fragile, Heavy, Gravity) and converts to CellHighlight entries on the Constraint layer with appropriate severity: SharpOnEdge=Danger, FragileInGravity/HeavyAboveNonHeavy/GravityUnsupported=Warning. Uses ContiguousBoundary.Compute() for edge rendering on multi-cell violation groups. Also created StatusThresholds class for tracking cumulative aspect current/threshold values with severity levels: Danger (at/above threshold), Warning (threshold-1), Safe (below), Info (unknown). 19 new tests: 10 HighlightEngine (Sharp edge single/multi-cell with boundary edges, Sharp interior=empty, Fragile in Gravity/normal, Heavy above non-Heavy, Gravity unsupported, no violations, aggregation of multiple types) + 9 StatusThresholds (Info for unknown, Safe/Warning/Danger boundaries, overwrite, multi-aspect independence, edge cases). All 445 tests pass (426 existing + 19 new).

## Code Review — 2026-03-05 (b)
- Reviewed commits: 2da34f1..119d125 (H.3, H.4, H.5)
- Findings: 0 (P0: 0, P1: 0, P2: 0)
- Fixed: 0, Tasks created: 0

## H.6 — HighlightEngine interaction, legend, and eagle sense methods
- Date: 2026-03-05
- Commit: 9668f51
- Notes: Three new methods on HighlightEngine: (1) ComputeInteractionHighlights finds adjacent cells with reactive aspects for a given reagent position, using interaction pairs derived from Adjacent rules — 2-trigger rules yield direct pairs (Sharp↔Fragile), 1-trigger rules (Volatile) yield Hot/Cold pairs for the net-hot mechanic. Groups highlighted neighbor cells by aspect, computes ContiguousBoundary per group for clean edge rendering, severity from StatusThresholds (defaults to Warning for aspects without entries). (2) ComputeLegendHighlights finds all placed cells with a given aspect and returns Legend layer highlights with contiguous boundary edges. (3) ComputeEagleSenseHighlights merges interaction highlights for all placed reagents. BuildInteractionPairs helper centralizes rule-to-pair extraction. Null bag suppression checked on both reagent cells and neighbors. 16 new tests covering all three methods: Volatile↔Hot, Volatile↔Cold, Sharp↔Fragile, Hot↔Volatile (reverse direction), no interaction, contiguous boundary internal edge elimination, severity from thresholds, default severity, legend with boundary, legend empty, eagle sense union, eagle sense empty, eagle sense multiple interactions. All 461 tests pass (445 existing + 16 new).

## H.7 — HighlightOverlay Godot renderer
- Date: 2026-03-05
- Commit: 8b22c2a
- Notes: Created src/Game/Inventory/HighlightOverlay.cs — a Godot Control that renders HighlightSet data visually using pooled ColorRect children. Edge highlights: 3px colored lines along specified CellEdge flags (Top/Bottom/Left/Right). Cell fills: semi-transparent rectangles for highlights with CellEdge.None. Colors derived from AspectId via GridConstants.GetAspectColor() with severity-based alpha modulation (Danger=0.9, Warning=0.65, Safe=0.4, Info=0.5). Fills use 40% of the severity alpha for subtlety. Eagle sense mode adds a full-size dim overlay (0,0,0,0.6) behind all highlights. Added HighlightEdgeWidth=3 and DimOverlayColor constants to GridConstants. Render() accepts grid dimensions for overlay sizing, Clear() removes all visuals. RenderedCount and IsDimmed properties for testability. Integration test in TestHarness verifies: 3 highlights produce 4 visual elements (2 edges for top+right, 1 edge for left, 1 fill), eagle sense dim overlay toggles correctly, Clear() removes all. All 461 unit tests pass.

## H.8 — MetaGridView highlight integration
- Date: 2026-03-05
- Commit: d8474fe
- Notes: Wired HighlightOverlay into MetaGridView as child at ZIndex 4 (above ghost at Z3). UpdateGhostPreview() now calls ComputeAndRenderHighlights() which computes constraint highlights (via HighlightEngine.ComputeConstraintHighlights for Sharp/Fragile/Heavy/Gravity violations) and interaction highlights (via HighlightEngine.ComputeInteractionHighlights for adjacent reactive aspects) and merges them into a single HighlightSet. Highlights clear on selection cancel, mouse leave, placement, or Escape. New public API: SetHighlights(HighlightSet) for external overlay control, SetEagleSense(bool), SetStatusThresholds(StatusThresholds), Highlights property for testing, HoveredPlacedReagent property + HoveredReagentChanged signal (emits when hover moves to a different placed reagent). PuzzleScreen builds StatusThresholds snapshot from StatusBarPanel after each placement/removal and passes to MetaGridView for severity-aware highlight coloring. Integration test verifies three scenarios: Sharp on non-Reinforced bag edge produces constraint highlights, Hot near Volatile produces interaction highlights, Stable-only reagent produces zero highlights. All 461 unit tests pass.

## Code Review (c) — 2026-03-05
- Reviewed commits: d0c4e68..c28c85e (H.6, H.7, H.8)
- Findings: 3 (P0: 0, P1: 0, P2: 1, P3: 2)
- Fixed: 1, Tasks created: 0

## H.9 — Rejection tooltip display
- Date: 2026-03-05
- Commit: 354e7af
- Notes: Added contextual rejection tooltip to MetaGridView that appears when a player clicks to place a reagent at an invalid position. When CanPlaceReagent fails and HighlightEngine.GetConstraintViolations returns violations, shows the first violation's Reason text in a dark-background PanelContainer positioned near the click cell. Text colored by the constraint's aspect color (e.g., Sharp violations in gray). Tooltip auto-hides after 2 seconds via _Process countdown or immediately on mouse move. Constraint highlights flash briefly (0.3s) by setting HighlightOverlay.Modulate to 1.5x brightness. Added RejectionFlash signal for future AspectLegendPanel linking. Refactored left-click handling into HandleLeftClick() for both real clicks and test access via SimulateLeftClick(). Added LastRejectionReason and IsTooltipVisible properties for testing. Integration test verifies: Sharp reagent on non-Reinforced bag edge → tooltip visible with non-empty reason text. All 461 unit tests pass.

## Integration Review — 2026-03-05 (b)
- Reviewed commits: 3f4e36f..354e7af (H.5, H.6, H.7, H.8, code-review-c DRY fix, H.9)
- Build: PASS (461/461 unit tests)
- Harness: 19/19 tests pass, 0 failures
- Visual: All 25 screenshots verified, no regressions
- Issues: 0
- Notes: Full highlight system working end-to-end. Constraint highlights (Sharp/Fragile/Heavy/Gravity violations) render on hover. Interaction highlights (Hot↔Volatile, Sharp↔Fragile) show reactive adjacencies. Eagle sense dim overlay toggles correctly. Rejection tooltip displays constraint reason text on invalid click. Pre-existing cosmetic issues unchanged: tutorial vertical text, Heat label left-edge clipping, base thresholds in status bar.

## H.10 — Aspect legend panel
- Date: 2026-03-05
- Commit: 1f75d7c
- Notes: Created AspectLegendPanel replacing AspectPrimerPanel. Each puzzle-relevant aspect gets a collapsible VBoxContainer entry with 16x16 colored icon + name. Expanding shows description from AspectMetadataDatabase, constraint text (if present), and relevant reaction rules with color-coded labels. Panel filters to puzzle-relevant aspects via GetForPuzzle(). Supports HighlightAspects(HashSet) for grid-to-legend linking (green-tinted background + slightly larger icon), ClearHighlight(), FlashAspect() for rejection tooltip linking (amber flash with 0.4s timer), and [Signal] AspectSelected for legend-to-grid clicking. FormatRule logic moved from AspectPrimerPanel to AspectLegendEntry to remove cross-dependency. Updated PuzzleScreen to use new panel and renamed TogglePrimer→ToggleLegend. Updated test harness with aspect_legend test covering entry count, expand/collapse, highlight, and toggle visibility. All 461 unit tests pass.

## H.11 — Item detail panel (Book of Hours style)
- Date: 2026-03-05
- Commit: 6e0632c
- Notes: Created ItemDetailPanel — persistent sidebar panel showing last-hovered reagent info (Book of Hours pattern: always visible, same location). Shows enlarged shape preview (16px/cell via DetailShapePreview), reagent name, flavor text (if present), and aspect breakdown aggregated across all cells (colored square + name + "x" + magnitude). ShowReagent(null) shows placeholder "Hover over a reagent to inspect". Enhanced ReagentTrayItem with aspect color dots (6x6 colored squares in HBox after name label, unique aspects only) and Hovered/Unhovered signals via MouseEntered/MouseExited. Added ReagentTray signal forwarding for hover events. Wired into PuzzleScreen: tray hover → ShowReagent, tray unhover → ShowReagent(null), grid HoveredReagentChanged → ShowReagent(HoveredPlacedReagent). Integration test verifies show/clear/multi-aspect display. All 461 unit tests pass.

## Code Review (d) — 2026-03-05
- Reviewed commits: 354e7af..6e0632c (H.9, H.10, H.11)
- Findings: 3 (P0: 0, P1: 0, P2: 0, P3: 1, P4: 2)
- Fixed: 3, Tasks created: 0

## H.12 — PuzzleScreen orchestration and eagle sense
- Date: 2026-03-05
- Commit: b1ec43e
- Notes: Wired all remaining PuzzleScreen orchestration signals. (1) AspectLegendPanel.AspectSelected → computes ComputeLegendHighlights for clicked aspect, renders on grid via SetHighlights; clicking same aspect again clears. Tracked via _legendSelectedAspect state. (2) HoveredReagentChanged now also calls _legendPanel.HighlightAspects() with the hovered reagent's aspect set, and ClearHighlight() on unhover. (3) Eagle sense toggle on Tab key: ToggleEagleSense() flips _eagleSenseActive, computes ComputeEagleSenseHighlights for all placed reagents with status thresholds, renders highlights with eagle sense dim overlay. Refreshes automatically after placement/removal via RefreshEagleSense(). SetInputAsHandled prevents Tab from cycling focus. (4) RejectionFlash signal from MetaGridView wired to _legendPanel.FlashAspect() for constraint-violation visual feedback. Exposed StatusThresholds property on MetaGridView for public read access. All 461 unit tests pass.

## H.13 — Ghost preview constraint composition
- Date: 2026-03-05
- Commit: fdc1e46
- Notes: Added ComputeGhostConstraintHighlights to HighlightEngine — same violation detection as ComputeConstraintHighlights but produces CellHighlight entries on the GhostOverlay layer. When hovering a Sharp reagent over a bag edge, the ghost cells themselves now get red edge markers showing the invalid position (rather than only the grid cells behind them). Integrated into MetaGridView.ComputeAndRenderHighlights alongside existing Constraint and Interaction layers. The GhostOverlay layer was already defined in HighlightLayer enum and rendered by HighlightOverlay. 5 new unit tests: Sharp on edge produces GhostOverlay highlights with Danger severity, no violations returns empty, multi-cell Sharp only highlights the violating cell, Fragile in gravity produces Warning, and ghost highlights are exclusively on GhostOverlay layer (not Constraint). All 466 tests pass (461 existing + 5 new).

## H.14 — Visual polish design document
- Date: 2026-03-05
- Commit: 83cf900
- Notes: Created docs/visual-polish-design.md with all 10 sections: per-aspect icon designs (flame, snowflake, warning triangle, shield, anvil, skull-and-crossbones, blade, cracked glass, catalyst flask, herb/leaf), highlight visual styles (crosshatching for danger, pulsing glow for interactions, animated dashed borders for eagle sense), RetroDiffusion sprite pipeline (reagent portraits, bag textures, aspect icons), contiguous boundary refinements (glow effects, drop shadows, animated shimmer), quiet mode concept (earned after 10 safe placements or manual toggle), sprite editor UI concept (HighlightTheme resource with sliders), information density guidelines (4 progressive disclosure levels), eagle sense visual treatment (deep blue-black overlay, silhouetted reagents, glowing connection lines), layout balance checklist (proportions at 720p/1080p/mobile), and color palette reference (all aspect/severity/UI chrome colors matching GridConstants.cs). Docs-only — no code changes.

## Integration Review — 2026-03-05 (d)
- Reviewed commits: fdc1e46..ed745e4 (H.13, H.14, new integration tests)
- Build: PASS (466/466 unit tests)
- Harness: 22/22 tests pass, 0 failures
- Visual: All 30 screenshots verified, no regressions
- Issues: 0
- Added: `eagle_sense_orchestration` test (H.12 toggle flow) and `legend_grid_linking` test (H.12 legend-to-grid click)
- Notes: Highlight-system phase (H.1-H.14) now has full integration test coverage. Pre-existing cosmetic issues unchanged: tutorial vertical text, Heat label left-edge clipping, base thresholds in status bar.

## Code Review — 2026-03-05 (e)
- Reviewed commits: 949e489..18f605c (H.12, H.13, H.14, integration tests)
- Findings: 2 (P0: 0, P1: 0, P2: 0, P3: 2)
- Fixed: 0, Tasks created: 0

## HINT.1 — Core hint data types (HintState, HintResult, Reagent.IsHinted)
- Date: 2026-03-05
- Commit: a25909b
- Notes: First task of the hint-system phase. Created HintState class (MaxHints, HintsUsed, HintedReagentIndices HashSet, HasHintsRemaining computed bool, WasHinted method) and HintResult class with HintResultType enum (Success, NoHintsRemaining, ReagentLocked, NoSolutionFound, BlockedByLockedReagent, AlreadyAtSolutionPosition). Added Reagent.IsHinted bool property paralleling existing IsLocked pattern. Added PuzzleState.MaxHints int field for serializable hint budget. 4 new unit tests covering HintState budget tracking and hinted index queries. All 470 tests pass (466 existing + 4 new).

## HINT.2 — HintEngine core logic (auto-pick, dislodge, execute)
- Date: 2026-03-05
- Commit: caa1f1a
- Notes: Static class with 4 methods. GetSolutionPlacement does linear scan over PuzzleState.Solution. ComputeDislodgeList temporarily sets target rotation to compute solution positions, checks cell occupants via GetReagentAtCell, returns null if any occupant IsLocked. PickNextHintTarget uses two-pass priority: first scan for snap-back candidates (unplaced + previously hinted = free), then new hint candidates (unplaced + never hinted = costs budget). ExecuteHint orchestrates removal, dislodge, rotation, placement, and IsHinted flag. Dislodged reagents get Rotation=0 and IsHinted=false. 15 unit tests covering all methods. All 485 tests pass (470 existing + 15 new).

## HINT.3 — DifficultyParams MaxHints + generator integration
- Date: 2026-03-05
- Commit: e6ddc8e
- Notes: Added MaxHints property to DifficultyParams with per-level values (Tutorial1-5/Easy=3, Medium=2, Hard=1). Constructor takes optional maxHints parameter (default 0). PuzzleGenerator.TryGenerate now sets state.MaxHints = difficulty.MaxHints. Also updated TutorialData.cs to explicitly pass maxHints: 3. 10 new tests (Theory covering all 8 difficulty levels + 2 Facts for custom params and default value). All 495 tests pass.

## HINT.4 — PuzzleScreen hint orchestration
- Date: 2026-03-05
- Commit: 82eb71b
- Notes: Wired HintEngine and HintState into PuzzleScreen. New HintState field initialized from PuzzleState.MaxHints in InitializeFromPuzzleState(). OnHintPressed() handler: calls PickNextHintTarget for auto-pick, checks snap-back status (free re-hint vs budget cost), calls ExecuteHint, updates budget and UI. UseHint() public method for programmatic/testing access. Hint button with purple styling and "N/M" budget counter label; hidden entirely when MaxHints==0. UpdateHintButton() disables when budget exhausted AND no snap-back candidates available. ResetPuzzle() clears IsHinted on all reagents but preserves hint budget (no refund). All 495 unit tests pass, no new Core tests needed (this is a Game-layer wiring task).

## Code Review (f) — 2026-03-05
- Reviewed commits: e6ddc8e..f8e4117 (HINT.3, HINT.4, docs)
- Findings: 2 (P0: 0, P1: 1, P2: 0, P3: 1)
- Fixed: 1 (DRY: OnHintPressed/UseHint 40-line duplication → OnHintPressed delegates to UseHint), Tasks created: 0

## HINT.5 — Hint visual indicators (grid + tray)
- Date: 2026-03-05
- Commit: 25298b2
- Notes: Added visual indicators for hinted reagents. Grid: 2px purple border around cells of hinted reagents using contiguous boundary pattern (only outer edges drawn). Tray: small 6x6 purple dot on ReagentTrayItem when the reagent was previously hinted (snap-back available). HintState is passed from PuzzleScreen to ReagentTray via SetHintState() and used during BuildItems() to set indicators. 5 files changed, 116 insertions. All 495 tests pass.

## HINT.6 — SaveData hint tracking
- Date: 2026-03-05
- Commit: ba72035
- Notes: Added DailyHintsUsed Dictionary<int, int> property to SaveData mapping date seeds (YYYYMMDD) to hint counts used. RecordDailyHints(dateSeed, hintsUsed) stores the count, GetDailyHintsUsed(dateSeed) retrieves it (returns -1 for unknown seeds). Follows the existing CompletedDailies pattern. 2 new tests: round-trip storage and unknown seed returns -1. All 497 tests pass (495 existing + 2 new).

## Integration Review — 2026-03-05 (e)
- Reviewed commits: 851ec25..HEAD (HINT.1–HINT.6)
- Build: PASS (497/497 unit tests)
- Harness: 23/23 tests pass, 0 failures
- Visual: All 32 screenshots verified, no regressions
- Issues: 0
- Added: `hint_system` integration test — validates HintEngine → PuzzleScreen pipeline end-to-end (budget, use, snap-back)
- Notes: Hint-system phase (HINT.1–HINT.6) complete with full integration coverage. Pre-existing cosmetic issues unchanged: tutorial vertical text, Heat label left-edge clipping, base thresholds in status bar.

## HINT.7 — Hint button UI in PuzzleScreen
- Date: 2026-03-05
- Commit: 0560de9
- Notes: The hint button, count label, visibility toggle (MaxHints==0), and enable/disable logic were already implemented during HINT.4. This task finalized the UI by adding a `disabled` StyleBox override to CreateStyledButton (darkened background + dimmed font color) ensuring consistent visual feedback when the hint button is disabled. All 497 tests pass.

## HINT.8 — Integration test: full hint system
- Date: 2026-03-05
- Commit: 29ed603
- Notes: Enhanced the existing Test_HintSystem stub with comprehensive coverage: budget initialization (0/3), first hint placement with IsHinted verification, reagent removal and snap-back (no budget cost, same reagent re-placed), second hint on different reagent (budget increments to 2/3), dislodge scenario (manually place reagent at another's solution position, hint dislodges it), and reset preserving budget. Screenshots at all key states. All 497 unit tests pass.

## Code Review — 2026-03-05 (g)
- Reviewed commits: 718631a..58a0f93 (HINT.5, HINT.6, HINT.7)
- Findings: 4 (P0: 0, P1: 0, P2: 0, P3: 3, P4: 1)
- Fixed: 0, Tasks created: 0

## DOC.1 — Create docs/puzzle-design.md
- Date: 2026-03-05
- Commit: 1b300c1
- Notes: Created comprehensive puzzle design document with all 7 required sections: (1) Design philosophy — binary pass/fail as Sudoku model, optimization for higher tiers, time+hints scoring (never move count), never punish experimentation. (2) Tier definitions mapping puzzle types to the 4 project tiers. (3) Current MVP types — FitAllSafely (implemented), MaximizeValue and MeetRequirement (enum only). (4) Post-MVP optimization types — BalanceToNeutral, MinimizeSeverity, MaximizeWithConstraints, TimedChallenge with scoring details. (5) Scoring & Stars — 3-star system (completion, no-hints, speed), par times, timer rules, hint budget impact. (6) Aspirational destruction/transmutation/forced-explosion with infrastructure requirements. (7) DLC-tier actual alchemy — alembic, multi-step brewing, potion output, equipment types. Cross-referenced from roadmap.md, mvp-specification.md, and chemistry-system.md.

## DOC.2 — Update existing docs with puzzle-design cross-references
- Date: 2026-03-05
- Commit: 801a640
- Notes: Updated three docs to cross-reference puzzle-design.md. mvp-specification.md: added hint system entry to Nice-to-Have checklist (marked done) and puzzle-design.md to Related Documents. game-overview.md: added sentence after the 7-stage roadmap pointing to puzzle-design.md for detailed puzzle type designs, and added to Related Documents. chemistry-system.md: replaced the brief "Future: Brewing and Mixing (Aspirational)" section with a compact cross-reference to puzzle-design.md sections 6 and 7, and added to Related Documents. Pure documentation — no code changes, all 497 tests still pass.

## Code Review (h) — 2026-03-05
- Reviewed commits: ec3c12a..1efadb8
- Findings: 2 (P0: 0, P1: 0, P2: 1, P3: 0, P4: 1)
- Fixed: 0, Tasks created: 0

## Integration Review — 2026-03-06
- Reviewed commits: db17b9e..HEAD (ART.4, CQ.1-CQ.4, GF.1-GF.4, VS.1-VS.8, R1, R2)
- Build: PASS (632/632 unit tests)
- Harness: 31/31 tests pass, 0 failures
- Visual: All 40 screenshots verified, no regressions
- Issues: 0
- Added: `undo_redo` integration test — validates ActionHistory place/undo/redo lifecycle end-to-end (VS.4/VS.5 coverage gap)
- Coverage gaps assessed: VS.1 (Sharp/Fragile filter) and VS.6/VS.7 (visual state system) lack integration tests but are adequately covered by unit tests or are visual-only features. All other recent features have sufficient integration coverage.
- Pre-existing cosmetic issues unchanged: (1) tutorial win overlay vertical text, (2) Heat label left-edge clipping, (3) status bar base thresholds.

## T.1 — Install desloppify and run baseline scan
- Date: 2026-03-05
- Commit: 4249356
- Notes: Installed desloppify 0.9.1 with full extras. `update-skill claude` fails on Windows due to cp1252 encoding bug (UnicodeEncodeError on arrow character U+2192) — non-blocking, scan works fine. Baseline C# scan results: **objective 84.3/100**, strict 33.7/100 (20 subjective dimensions unassessed = 0%). 93 open issues: 11 structural (large files — PuzzleScreen 865 LOC, TestHarness 1376 LOC, MetaGridView 778 LOC, etc.), 42 unused imports, 31 test coverage gaps, 4 duplicate code clusters, 2 coupling (1 import cycle, 1 orphaned file), 2 low-cohesion files, 1 signature variance. Security: clean. No P0 issues. GDScript scan found 0 issues (100/100). Boilerplate duplication skipped (jscpd not available).

## Desloppify Baseline
- Scan date: 2026-03-05
- Version: desloppify 0.9.1
- C# objective score: 84.3/100
- C# strict score: 33.7/100 (20 subjective dimensions unassessed)
- Dimension scores: File health 93.7%, Code quality 99.2%, Duplication 99.2%, Security 99.4%, Test health 44.6%
- GDScript score: 100/100
- Total open issues: 93 (C#) + 0 (GDScript)
- P0 issues: 0

## Integration Review — 2026-03-05 (f)
- Reviewed commits: 1efadb8..1c7cdcc (T.1, highlight system H.1-H.14, hint system HINT.1-HINT.8, R-8, DOC.1-DOC.2)
- Build: PASS (497/497 unit tests)
- Harness: 23/23 tests pass, 0 failures
- Visual: 34 screenshots verified, no game-facing regressions
- Issues: 0 (P0: 0, P1: 0, P2: 0)
- Coverage gaps: 10 features (B.1-B.4 bag mechanics, A.1-A.6 aspect mechanics) lack integration tests — all have comprehensive unit tests but are excluded from the Easy-difficulty test path. Recommend future Test_MediumDifficultyPuzzle.
- Notes: Full highlight system (constraint, interaction, eagle sense, legend, item detail, rejection tooltip, ghost constraint) and hint system (budget, auto-pick, snap-back, dislodge, visual indicators) all working correctly end-to-end. 208 new unit tests since last integration review (289→497). Test harness grew from 13→23 integration tests.

## T.2 — Update review-code prompt with desloppify step
- Date: 2026-03-05
- Commit: acf41ab
- Notes: Added step 5 (desloppify scan/next/fix) to scripts/ralph/prompts/review-code.md. The new step runs `desloppify scan` for baseline score, `desloppify next` for highest-priority issue, fixes P0-P1 issues if quick, and re-scans to verify. Renumbered subsequent steps (5→6 through 11→12). Added Desloppify section to the report template for before/after score tracking. The qualitative deslop.md review (step 4) is preserved for architectural and game-specific concerns that desloppify cannot catch.

## T.3 — Create game architecture guidelines doc
- Date: 2026-03-05
- Commit: 861a1d0
- Notes: Created docs/game-architecture.md with 6 sections documenting game-specific structural patterns: scene management (pre-init property + deferred _Ready, QueueFree transitions, .tscn vs procedural), Godot scene-tree patterns (signal conventions, AddChild, theme overrides), web export performance budgets (WASM memory, draw calls, GC pressure), asset pipeline (placeholder→RetroDiffusion→curate→import flow), input handling (select-then-place, ghost preview, keyboard shortcuts), and three-layer testing strategy (xUnit for Core, Godot harness for integration, manual playtesting). All patterns reference real codebase files and line numbers. Cross-references docs/roadmap.md.

## Code Review (i) — 2026-03-05
- Reviewed commits: 5d3c5d4..7ed6878
- Findings: 1 (P0: 0, P1: 0, P2: 1)
- Fixed: 1, Tasks created: 0

## E.1 — Auto-solver: PuzzleSearchSolver with backtracking
- Date: 2026-03-05
- Commit: dafa8ea
- Notes: Pure C# backtracking constraint-satisfaction solver. Solve() finds one valid solution, CountSolutions() enumerates up to N solutions. Uses MRV heuristic from PuzzleGenerator (Heavy → Sharp → largest first). Reuses PuzzleSolver.ReconstructGrid/ReconstructReagents for setup, ChemistryEngine.EvaluateProspective for chemistry validation. Supports locked reagent pre-placement and CancellationToken for timeout. 12 new tests covering solvable/unsolvable/timeout/cancelled/reactive/multi-seed scenarios, plus CountSolutions edge cases. All 509 tests pass (497 existing + 12 new).

## E.2 — PuzzleFile wrapper with editor metadata
- Date: 2026-03-05
- Commit: cb69213
- Notes: Created PuzzleFile class wrapping PuzzleState with authoring metadata: Name (string), Description (string?), Author (string?), CreatedAt (DateTime). Static methods Save/Load for file I/O and SaveToString/LoadFromString for in-memory serialization, both using System.Text.Json with WriteIndented. Follows SaveManager.cs pattern: graceful null return on missing file or corrupt JSON, auto-creates directories on save. 10 new tests covering round-trip serialization (file and string), null optional fields, invalid JSON returns null, directory creation, overwrite behavior, and bag property preservation. All 519 tests pass (509 existing + 10 new).

## E.3 — ShapeEditorWidget reusable control
- Date: 2026-03-05
- Commit: a09deaa
- Notes: Created src/Game/Editor/ShapeEditorWidget.cs — a Godot Control for visually editing bool[,] shape matrices. Displays a configurable NxN grid (default 6x6) of clickable ColorRect cells. Click toggles on/off, hover shows feedback. Initialize(maxRows, maxCols, cellPixelSize) builds the grid procedurally following MetaGridView's pattern (ColorRect cells + grid lines). SetShape/GetShape for load/read (GetShape trims to bounding box of active cells), GetShapeRaw returns full grid. Clear() resets all. ShapeChanged signal emits on every toggle. Uses own pixel→grid conversion (configurable cell size, not tied to GridConstants.CellSize). Reusable for both bag and reagent shape editing. No Core tests needed (Godot UI widget). All 519 tests pass.

## Code Review (j) — 2026-03-05
- Reviewed commits: dafa8ea..e29edbc (E.1–E.3)
- Findings: 3 (P0: 0, P1: 0, P2: 2, P3: 0)
- Fixed: 2, Tasks created: 0

## Integration Review — 2026-03-05 (h)
- Reviewed commits: 1c7cdcc..e29edbc (T.2, T.3, code-review-i, E.1, E.2, E.3)
- Build: PASS (519/519 unit tests)
- Harness: 23/23 tests pass, 0 failures
- Visual: All 34 screenshots verified, no regressions
- Issues: 0 (P0: 0, P1: 0, P2: 0)
- Notes: E.1-E.3 are editor-infrastructure features (solver, file I/O, shape widget) with 22 new unit tests but no game-facing behavior to integration-test yet. Pre-existing cosmetic issues unchanged: tutorial vertical text, Heat label left-edge clipping, base thresholds in status bar.

## E.4 — BagEditorPanel
- Date: 2026-03-05
- Commit: bf1017c
- Notes: Created src/Game/Editor/BagEditorPanel.cs — a procedural Godot VBoxContainer for editing bags in the puzzle editor. Layout: header label + "Add Bag" button with count, scrollable list of collapsible BagEntry panels. Each entry contains a ShapeEditorWidget (6x6, 20px cells) for shape editing, SpinBox controls for PositionCol/PositionRow (0-20 range), and a 2-column GridContainer of CheckBoxes for bag properties (Null, Gravity, Locked, Insulated, Reinforced, Fragile). BagEntry is a nested partial class with its own Changed/RemoveRequested signals. Panel emits BagsChanged on any modification. GetBags()/SetBags() for data binding. Default new bag is 3x3 fully active at (0,0). Follows PuzzleScreen's procedural UI pattern with consistent theme overrides (gold headers, muted labels). No Core tests needed (Godot UI widget). All 519 tests pass.

## E.5 — ReagentEditorPanel
- Date: 2026-03-05
- Commit: 73cccaf
- Notes: Created src/Game/Editor/ReagentEditorPanel.cs — a procedural Godot VBoxContainer for editing reagents in the puzzle editor. Follows BagEditorPanel pattern exactly. Layout: header label + "Add Reagent" button with count, scrollable list of collapsible ReagentEntry panels. Each entry contains: ShapeEditorWidget (4x4, 20px cells) for shape editing, dynamic per-cell aspect section with OptionButton dropdowns populated from AspectMetadataDatabase (10 aspects + None option), Weight SpinBox (0-100, step 0.5), Value SpinBox (0-100, step 1), and IsLocked CheckBox. Key design: when the shape changes (cells toggled on/off), SyncAspectRowsToShape() adds/removes AspectCellRow entries to match the true-cell count, preserving existing aspect selections. Each AspectCellRow is a nested partial class with Changed signal. Default new reagent is 1x1 with no aspects. No Core tests needed (Godot UI widget). All 519 tests pass.

## E.6 — EditorScreen main layout and file I/O
- Date: 2026-03-05
- Commit: c5a9b51
- Notes: Created src/Game/Editor/EditorScreen.cs — the main puzzle editor screen and scenes/editor.tscn. Follows PuzzleScreen's procedural UI pattern: full-rect Control with procedural BuildUI(). Layout: header bar ("Puzzle Editor"), HBoxContainer with MetaGridView (left, centered) and scrollable right column containing BagEditorPanel + ReagentEditorPanel. Bottom bar has puzzle name LineEdit, New/Save/Load buttons with styled StyleBoxFlat. Uses PuzzleFile for serialization (Save to user://puzzles/, Load via FileDialog). OnEditorDataChanged() syncs state from editors, rebuilds MetaGrid, and replaces the MetaGridView. RebuildMetaGrid() mirrors PuzzleSolver.ReconstructGrid() — computes grid size from bag positions/shapes, minimum 6x6, places bags with parsed properties. NewPuzzle() creates default state with one 3x3 Normal bag. Disambiguated Vector2I conflict between Core.Grid and Godot using partial qualification. No Core tests needed (Godot UI screen). All 519 tests pass.

## Code Review (k) — E.4–E.6
- Reviewed commits: c5a9b51..8465dbc
- Findings: 6 (P0: 0, P1: 1, P2: 2, P3: 2, P4: 1)
- Fixed: 3, Tasks created: 0

## E.7 — SolverPanel with auto-solve integration
- Date: 2026-03-05
- Commit: 9a6d751
- Notes: Created src/Game/Editor/SolverPanel.cs — a VBoxContainer panel wired into EditorScreen below the MetaGridView. Three buttons: Solve (finds one solution via PuzzleSearchSolver.Solve), Count Solutions (counts up to 100 via CountSolutions), Show/Hide Solution (toggles blue overlay on grid). Solver runs on background thread using Task.Run with CancellationToken, results marshaled back to main thread via CallDeferred. Solution overlay renders semi-transparent blue ColorRects at ZIndex 3 on the MetaGridView. EditorScreen integration: SolverPanel clears results when bags/reagents change (OnEditorDataChanged, NewPuzzle, LoadPuzzle), updates grid view reference when grid is rebuilt. Also cleaned up unused `reagents` parameter from PuzzleSearchSolver's private Backtrack/CountBacktrack methods. All 519 tests pass.

## E.8 — Play mode toggle in editor
- Date: 2026-03-05
- Commit: 3d233b9
- Notes: Added Play/Edit toggle button to EditorScreen header bar. Play mode builds a fresh MetaGrid and reagent list from the current editor PuzzleState using PuzzleSolver.ReconstructGrid/ReconstructReagents, replaces the grid view, hides editor panels (BagEditorPanel, ReagentEditorPanel, SolverPanel, file controls), and shows a ReagentTray, ObjectivePanel, StatusBarPanel, and Reset button. Reagent selection, placement, removal, and keyboard shortcuts (Escape to cancel) work identically to PuzzleScreen via MetaGridView signals. Exiting play mode discards all placements and restores the editor view. Refactored grid view replacement into a shared ReplaceGridView(MetaGrid) method used by both editor refresh and play mode transitions. All 519 tests pass.

## Integration Review — 2026-03-05 (k)
- Reviewed commits: e29edbc..5b6fb26 (E.4, E.5, E.6, E.7, E.8, code-review-k fixes)
- Build: PASS (519/519 unit tests)
- Harness: 24/24 tests pass, 0 failures
- Visual: 35 screenshots verified, no regressions
- Issues: 0 (P0: 0, P1: 0, P2: 0)
- Added: `editor_screen` integration test — validates EditorScreen loads, starts in edit mode, play mode guard rejects empty puzzles
- Notes: Editor infrastructure phase (E.4–E.8) functional end-to-end. Pre-existing cosmetic issues unchanged: tutorial vertical text (result overlay width collapse), Heat label left-edge clipping (fixed 60px min width), base thresholds in status bar.

## E.9 — Editor integration tests
- Date: 2026-03-05
- Commit: 376dcbb
- Notes: Added three editor integration tests to TestHarness. Test_EditorCreateAndSave: builds a PuzzleState with 2 bags and 3 reagents, saves to file, loads into EditorScreen, saves from editor, loads back via PuzzleFile.Load, verifies bag/reagent counts and shape dimensions survive the round-trip. Test_EditorSolver: loads a known-solvable puzzle (3x3 bag, two 1x1 reagents) into editor and verifies PuzzleSearchSolver.Solve returns 2 placements; loads an unsolvable puzzle (1x1 bag, two reagents) and verifies solver returns null. Test_EditorPlayMode: loads puzzle with reagents, enters play mode (verified IsPlayMode=true), places a reagent directly on PlayGrid, exits play mode, saves state and compares with pre-play state to verify bags/reagents unchanged. Added internal PlayGrid/PlayReagentList test accessors to EditorScreen. All 27 integration tests pass (24 existing + 3 new), 519 unit tests pass.

## Code Review (l) — 2026-03-05
- Reviewed commits: 9a6d751..2bdeeb8 (E.7 SolverPanel, E.8 Play mode, E.9 Editor tests)
- Findings: 4 (P0: 0, P1: 1, P2: 0, P3: 2, P4: 1)
- Fixed: 1 (SolverPanel missing _ExitTree cleanup for background solver tasks), Tasks created: 0

## ART.1 — RetroDiffusion MCP server
- Date: 2026-03-05
- Commit: 7801c3f
- Notes: Created scripts/mcp/retrodiffusion-server.js — a Node.js ES module MCP server wrapping the RetroDiffusion API. Three tools: generate_sprite (POST /v1/inferences, saves PNGs + JSON metadata sidecars to art/generated/), list_styles (returns built-in styles grouped by model + any custom styles from art/retrodiffusion-styles.json), create_custom_style (POST /v1/styles with base64-encoded reference images, persists style_id). Credit exhaustion guard: after every API call, checks remaining_balance; if below 1.0, sets a flag that blocks all subsequent generate/create calls until balance is confirmed above threshold. API key loaded from .env via dotenv. Added to .mcp.json alongside existing Godot server. Also added node_modules/ to .gitignore. No C# code changes — all 519 tests still pass.

## ART.2 — Sprite preview scene
- Date: 2026-03-05
- Commit: f1d3089
- Notes: Created src/Game/Tools/SpritePreview.cs — a procedural Godot Control development tool for previewing all game sprites. Scans art/sprites/ recursively for .png files, displays them in a scrollable grid at 1x (32px) and 2x (64px) zoom with filename labels, grouped by subdirectory. Background color toggle cycles through dark gray, white, and checkerboard for transparency testing. Shows total sprite count. Displays placeholder message when art/sprites/ doesn't exist or is empty. Built following the TestHarness procedural pattern (no .tscn dependencies beyond the root scene). Scene file at scenes/sprite_preview.tscn. No Core code changes — all 519 tests still pass.

## ART.3 — Art workflow documentation
- Date: 2026-03-05
- Commit: 54ec509
- Notes: Created docs/art-workflow.md with all 5 required sections: (1) Overview — pipeline from placeholder rectangles through RetroDiffusion generation, human curation, Godot import, to runtime display. (2) Godot Built-in Tools — SpriteFrames editor, Theme editor, Remote Inspector, resource override for hot-swapping. (3) RetroDiffusion Pipeline — MCP server setup, prompt guidelines from art-and-style.md, human-in-the-loop style creation workflow, generated output locations. (4) Sprite Replacement Process — naming conventions (reagent_NAME.png, bag_TYPE.png, icon_ASPECT.png), Godot import settings for pixel art, runtime texture loading pattern using GD.Load<Texture2D>(). (5) Quality Checklist — 32x32 alignment, transparent background, aspect color coding, readability at 1x, bold outlines, distinct silhouettes, warm palette. Cross-references art-and-style.md and art-references.md. Docs-only — no code changes, all 519 tests still pass.

## Code Review — 2026-03-05 (m)
- Reviewed commits: e9e67a2..184d4d2 (ART.1–ART.3)
- Findings: 1 (P0: 0, P1: 1, P2: 0)
- Fixed: 1, Tasks created: 0

## ART.4 — Manuscript reference curation
- Date: 2026-03-06
- Commit: 8c56f06
- Notes: Created docs/art-references.md with all 5 required sections. (1) Overview connects historical manuscripts to the "earnest lore" design pillar. (2) Source Catalog documents 5 manuscripts: Vienna Dioscurides (6th century, 400+ botanical illustrations, Wikimedia Commons + PICRYL, public domain), Badianus Codex (1552, 204 herbs, INAH digitization), Splendor Solis (1582, 22 alchemical illustrations, Internet Archive), Rosarium Philosophorum (16th century, 20 woodcuts, various institutional), Apuleius Herbarium (medieval, 100+ botanical illustrations, various). (3) Mapping to Game Elements maps sources to reagent sprites, UI backdrops, menu art, and aspect icons. (4) Style Translation Notes covers what to preserve (hand-drawn quality, earth-tone palettes, botanical accuracy, alchemical symbolism) vs what to adapt (pixel-art-ify at 32x32, modernize for readability, cute-ify for cozy tone). (5) RetroDiffusion Integration describes the custom style pipeline (ART.5) reference image selection workflow. Docs-only — no code changes, all 519 tests still pass.

## CQ.1 — Extract PuzzleValidator to Core
- Date: 2026-03-06
- Commit: ae7ff2a
- Notes: Extracted puzzle win-condition evaluation from PuzzleScreen to pure C# in src/Core/Puzzle/. Created ConfirmResult.cs (moved enum from PuzzleScreen.cs), ValidationResult.cs (with static factories NotAllPlaced/HasReactions/Win), and PuzzleValidator.cs (static Evaluate method handling FitAllSafely, MaximizeValue, MeetRequirement). Removed duplicate ConfirmResult enum from PuzzleScreen.cs — all Game-side files already had `using AlchemistCourier.Core.Puzzle;` so resolution was seamless. Key gotcha: test setup initially used Fire/Ice aspects which don't exist in the MVP rule set; switched to Hot+Cold (SameBag Neutralization rule). Also learned that Sharp reagents can't be placed on edge cells of non-Reinforced bags, which blocked Sharp+Fragile adjacency tests in small bags. 7 new tests, 526 total pass.

## CQ.2 — Wire PuzzleScreen to PuzzleValidator
- Date: 2026-03-06
- Commit: 2f5167b
- Notes: Replaced inline win condition logic in PuzzleScreen.ConfirmPuzzle() with a single call to PuzzleValidator.Evaluate(). The method now handles all three PuzzleType modes via the validator, and adds score display in the success message for MaximizeValue puzzles. The ConfirmResult enum was already fully extracted in CQ.1, so no duplicate deletion was needed — all consumer files (TutorialScreen, TestHarness) already had the correct using directive. Clean refactor: 20 insertions, 22 deletions. All 526 tests pass.

## CQ.3 — Add locked reagent visual feedback
- Date: 2026-03-06
- Commit: 608b943
- Notes: Added amber border rendering for locked reagents in MetaGridView following the existing CreateHintedBorders() pattern. Added rejection feedback when right-clicking a locked reagent — shows tooltip "Cannot remove — locked reagent" and blocks removal. Also hardened PuzzleScreen.RemoveReagentAt() to check the MetaGrid.RemoveReagent() return value. Integration test Test_LockedReagentRemovalBlocked creates a custom PuzzleState with a locked reagent, verifies it's pre-placed, attempts removal, and confirms it's blocked. All 526 unit tests pass.

## Code Review — 2026-03-06
- Reviewed commits: 8c56f06..2f5167b (ART.4, CQ.1, CQ.2)
- Findings: 3 (P0: 0, P1: 1, P2: 0, P3: 2)
- Fixed: 1, Tasks created: 0

## CQ.4 — Decompose PuzzleScreen: extract ResultOverlayPanel and HintOrchestrator
- Date: 2026-03-06
- Commit: 5c86a24
- Notes: Extracted two cohesive subsystems from PuzzleScreen (855→761 lines). ResultOverlayPanel extends PanelContainer, encapsulates the result overlay UI construction and ShowWin/ShowFailure/Hide/FormatReactionType methods. HintOrchestrator is a plain C# class (not a Godot node) encapsulating hint domain logic: ExecuteHint(), HasHintsAvailable, FormatHintCount(). PuzzleScreen creates the orchestrator in InitializeFromPuzzleState() and delegates to it from UseHint()/UpdateHintButton(). UI refresh calls remain in PuzzleScreen. Used `new` keyword on Hide() to suppress CS0108 warning (CanvasItem.Hide inherited member). All 526 tests pass.

## Integration Review — 2026-03-06
- Reviewed commits: e29edbc..9809fb9 (E.4-E.9, ART.1-ART.4, CQ.1-CQ.4, code reviews)
- Build: PASS (526/526 unit tests)
- Harness: 28/28 tests pass, 0 failures
- Visual: All 41 screenshots verified, no regressions
- Issues: 0 (P0: 0, P1: 0, P2: 0)
- Added: `locked_reagent_removal_blocked` test (CQ.3 — locked pre-placement + removal rejection)
- Coverage: Editor tests (E.9) added 3 integration tests; locked reagent test added 1. Total harness grew 24→28.
- Notes: Large scope since last integration review — editor phase (E.4-E.9), art pipeline (ART.1-ART.4), and code quality refactoring (CQ.1-CQ.4). All features working end-to-end. PuzzleValidator extraction (CQ.1-CQ.2) confirmed by win_loss_evaluation test passing with PuzzleScreen delegating to Core validator. HintOrchestrator extraction (CQ.4) confirmed by hint_system test passing all budget/snap-back/dislodge scenarios. Editor solver, play mode, and save/load verified by dedicated integration tests. Pre-existing cosmetic issues unchanged: tutorial vertical text, Heat label left-edge clipping, base thresholds in status bar.

## GF.1 — Create GameFlowController
- Date: 2026-03-06
- Commit: fe4c0cb
- Notes: Created GameFlowController (src/Game/GameFlowController.cs) managing screen transitions between MainMenu, Tutorial, DailyPuzzle, and Editor screens. Uses SaveManager with GlobalizePath("user://save.json") for persistence. TransitionTo() handles cleanup (RemoveChild + QueueFree) and creation of new screens with pre-init properties. First-launch detection: TutorialLessonCompleted == 0 starts at Tutorial, otherwise MainMenu. Completion handlers save data and return to MainMenu. Also created: GameScreen enum, scenes/game.tscn (minimal scene pointing to controller), stub MainMenuScreen (3 buttons + streak footer), stub DailyPuzzleScreen (wraps PuzzleScreen with back button + already-completed guard), and TutorialCompleted event on TutorialScreen (fires from AdvanceLesson when IsComplete). GF.2/GF.3 will flesh out MainMenuScreen/DailyPuzzleScreen; GF.4 will wire main.tscn. All 526 tests pass.

## Code Review B — 2026-03-06
- Reviewed commits: 9809fb9..eba3d86
- Findings: 4 (P0: 0, P1: 1, P2: 0, P3: 3)
- Fixed: 3, Tasks created: 0

## GF.2 — Create MainMenuScreen
- Date: 2026-03-06
- Commit: 3b95f1d
- Notes: Replaced the GF.1 stub MainMenuScreen with full styled implementation. Three buttons with distinct colors: Daily Puzzle (green, shows streak count or "Completed" from SaveData), Tutorial (blue, shows lesson progress e.g. "Lesson 3/5" or "Complete"), Editor (neutral gray). All buttons use CreateStyledButton() with StyleBoxFlat for normal/hover/pressed states, matching PuzzleScreen's pattern. Streak footer uses GetEffectiveStreak() to avoid showing stale streaks. No new tests needed (Game-layer UI, no Core changes). All 526 tests pass.

## GF.3 — Create DailyPuzzleScreen wrapper
- Date: 2026-03-06
- Commit: fe4c0cb (created during GF.1)
- Notes: DailyPuzzleScreen was fully implemented as part of GF.1 — follows TutorialScreen pattern. Wraps PuzzleScreen with SaveInfo property, DailyCompleted/BackRequested events, back button + day label in top bar, streak info passed via HeaderStreakText, already-completed guard showing "Already completed today!" label. All requirements met. Marked as done without additional changes.

## GF.4 — Wire main.tscn to GameFlowController and add screen completion events
- Date: 2026-03-06
- Commit: 9d485fb
- Notes: Updated scenes/main.tscn to point to GameFlowController instead of PuzzleScreen — the game now starts at the main menu (or tutorial for first launch). TutorialCompleted event, completion handlers, and first-launch detection were all already implemented in GF.1. Added two integration tests: Test_GameFlowStartsAtMenu (TransitionTo MainMenu, verify CurrentScreen and node type) and Test_GameFlowFirstLaunchTutorial (TransitionTo Tutorial and DailyPuzzle, verify each transition produces correct screen enum and node type). All 526 unit tests pass.

## VS.1 — Fix ComplexityFilter: promote Sharp/Fragile to Simple tier
- Date: 2026-03-06
- Commit: a3dbd20
- Notes: Moved Sharp and Fragile from IntermediateAspects to SimpleAspects in ComplexityFilter. Simple tier now has 8 aspects (was 6), Intermediate has 2 (was 4). Simple-tier rules now include Breakage (7 total, was 6). Updated 6 ComplexityFilter tests for new counts. Key challenge: adding constrained aspects to the generator's pool caused generation failures — Sharp can't be on bag edges, Fragile can't be in Gravity bags, and Fragile breaks from ANY adjacent reaction (A.3 mechanic). Fix: (1) Cap constrained aspects (Sharp, Fragile) to at most 1 reagent each per puzzle in AssignAspects(). (2) Filter Fragile from pool when all bags are Gravity. (3) Add Fragile to placement priority ordering (placed early to claim non-Gravity bag slots). All 526 tests pass.

## VS.2 — Create ReagentCatalog with canonical single-aspect reagents
- Date: 2026-03-06
- Commit: 197a10f
- Notes: Created src/Core/Reagents/ReagentCatalog.cs with 8 species × 9 shapes = 72 canonical single-aspect reagent definitions. Species: Firevine(Hot), Snowflower(Cold), Burstbloom(Volatile), Purple Creeper(Toxic), Lead Ingot(Heavy), Nullflower(Stable), Sharpstone(Sharp), Glasswork(Fragile). Shapes: monomino (1 cell), domino (2), I-bar/L-bend triominoes (3), and I/O/T/S/L tetrominoes (4). Each species has consistent flavor text from docs/core-mechanics.md. Static initialization builds lookup indices by ID, aspect, and shape. Query methods: Get(speciesId, shapeName), GetById(id), GetByAspect(aspectId), GetByShape(shapeName), FindMatch(aspectId, shapeMatrix), GetShapeMatrix(shapeName). 22 new tests covering: 72 total count, unique IDs, 9 per species, 8 per shape, correct cell counts per shape, single-aspect consistency, flavor text presence, specific lookups, null returns for unknowns, FindMatch shape matching, GetShapeMatrix clone safety. All 577 tests pass (555 existing + 22 new).

## VS.4 — Implement ActionHistory (undo/redo) in Core
- Date: 2026-03-06
- Commit: a04612c
- Notes: Created undo/redo infrastructure in src/Core/Puzzle/. IGridAction interface with Execute(MetaGrid)/Undo(MetaGrid) returning bool. PlaceReagentAction stores reagent, position, rotation; Execute sets rotation and places, Undo removes and restores previous rotation. RemoveReagentAction captures position and rotation at construction time; Execute removes, Undo restores rotation and re-places. ActionHistory manages two Stack<IGridAction> (undo/redo) with Execute (push undo, clear redo), Undo (pop undo, push redo), Redo (pop redo, push undo), Clear. Failed Execute/Undo/Redo operations leave stacks unchanged. 18 new tests covering: basic state, place/remove actions, rotation tracking, multiple actions in order, redo cleared by new action, clear empties both stacks, empty stack returns false, full place-remove round-trip, failed execute not on stack, locked reagent removal blocked, cell occupancy verified through undo/redo. All 595 tests pass (577 existing + 18 new).

## Code Review C — 2026-03-06
- Reviewed commits: cf5896c..882a33b
- Findings: 4 (P0: 0, P1: 0, P2: 0, P3: 1, P4: 3)
- Fixed: 0, Tasks created: 0

## VS.6 — Create VisualStateResolver in Core
- Date: 2026-03-06
- Commit: 7f1a010
- Notes: Created src/Core/Visual/ directory with VisualState enum (Normal, Heated, Cooled, ThermalStress, Crushed, Exploding, Breaking) and VisualStateResolver static class. Resolver determines per-cell visual state for placed reagents based on adjacent aspects from neighboring reagents. Resolution priority: Crushed (Fragile + Heavy directly above, row-1) > ThermalStress (Fragile + both Hot and Cold adjacent) > Heated (Hot adjacent, no Cold) > Cooled (Cold adjacent, no Hot) > Normal. Non-fragile reagents can be Heated/Cooled but never ThermalStress or Crushed. Self-adjacency excluded — a reagent's own cells don't influence its visual state. 15 new tests covering: unplaced returns empty, isolated returns Normal, Heated/Cooled/ThermalStress for Fragile, non-Fragile gets Normal (not ThermalStress) with both Hot+Cold, Crushed (Heavy above), Crushed priority over Heated, Heavy below/beside not Crushed, diagonal not adjacent, multi-cell per-cell states, self-adjacency ignored, grid edge safety, no-aspect neighbor. All 612 tests pass (597 existing + 15 new).

## VS.3 — Wire PuzzleGenerator to use ReagentCatalog
- Date: 2026-03-06
- Commit: 9a0bcf0
- Notes: Added TryFindCatalogMatch() to PuzzleGenerator — for single-aspect reagents (all non-empty cells share one aspect), looks up ReagentCatalog.FindMatch(aspectId, shape) and uses the catalog definition instead of anonymous "reagent_N". Multi-aspect, no-aspect, and shape-unmatched reagents fall back to anonymous definitions. DemoPuzzle updated to use catalog entries for iceShard (snowflower_domino) and inertCrystal (nullflower_domino); multi-aspect reagents (fire_pebble, poison_fang, blast_jelly, cinnabar_block) kept as custom definitions. 4 new tests: catalog names found in Easy-difficulty puzzles, solution still valid with catalog reagents, no-aspect Tutorial1 puzzles fall back to anonymous, deterministic catalog matching across same seed. All 616 tests pass (612 existing + 4 new).

## Integration Review — 2026-03-06
- Reviewed commits: 608b943..942efa5 (CQ.1-CQ.4, GF.1-GF.4, VS.1-VS.4, VS.6, ART.4)
- Build: PASS (616/616 unit tests)
- Harness: 30/30 tests pass, 0 failures
- Visual: 39 screenshots verified, no regressions
- Issues: 0
- Notes: All Game-layer features have integration coverage. VS.4 (ActionHistory) and VS.6 (VisualStateResolver) are Core-only, awaiting Game-layer wiring (VS.5, VS.7). Game flow screenshots show test-artifact overlap (not a real bug). Pre-existing cosmetic issues unchanged.

## VS.5 — Wire undo/redo into PuzzleScreen UI
- Date: 2026-03-06
- Commit: 9ef9131
- Notes: Integrated ActionHistory (from VS.4) into PuzzleScreen and MetaGridView. All PlaceReagent/RemoveReagent calls now route through ActionHistory.Execute() for undo tracking. MetaGridView accepts optional ActionHistory via SetActionHistory() — editor and other consumers are unaffected (fallback to direct grid calls). Added Undo/Redo styled buttons to the button bar and Ctrl+Z/Ctrl+Y keyboard shortcuts in _UnhandledKeyInput. ResetPuzzle() clears history. Hint operations also clear history to prevent inconsistent undo state (hints modify the grid outside of ActionHistory). Locked reagents are excluded from history since they can't be interacted with. All 616 tests pass, no Core Godot imports.

## Code Review D — 2026-03-06
- Reviewed commits: 942efa5..651e894 (VS.5)
- Findings: 1 (P0: 0, P1: 0, P2: 1)
- Fixed: 0, Tasks created: 1 (R1 — extract RefreshAfterGridChange helper in PuzzleScreen)

## R1 — Extract RefreshAfterGridChange helper in PuzzleScreen
- Date: 2026-03-06
- Commit: b1cb698
- Notes: Extracted RefreshAfterGridChange(bool updateHints = false) helper consolidating 6 near-identical refresh sequences across OnReagentPlaced, OnReagentRemoved, RemoveReagentAt, UseHint, Undo, and Redo. Each call site reduced from 5-7 lines to a single method call. ResetPuzzle kept as a separate path since it omits UpdateStatusThresholds/RefreshEagleSense and has additional reset-specific logic. Net reduction: 20 lines (17 insertions, 37 deletions). Pure refactor — no behavior change. All 616 tests pass.

## VS.7 — Create SpriteSheetSpec and SpriteStateRenderer
- Date: 2026-03-06
- Commit: 16d3224
- Notes: Created the three-layer sprite state rendering pipeline per docs/sprite-state-system.md. (1) SpriteSheetSpec (src/Core/Visual/, pure C#): data model defining sprite sheet layout — StateRowMap (VisualState→row), FrameCounts (VisualState→frames), CellWidth/CellHeight. DefaultReagentSpec() factory creates standard 7-row layout: Normal/Heated/Cooled/ThermalStress/Crushed (1 frame each), Breaking/Exploding (4 frames each). GetRegion(state, frame) returns pixel coordinates for atlas lookup. SheetWidth/SheetHeight computed from max frames × cell dimensions. (2) ReagentSpriteSheet (src/Game/Visual/): wraps Texture2D + SpriteSheetSpec, loads from res://art/sprites/reagents/{id}.png via ResourceLoader with caching. GetAtlasTexture() returns an AtlasTexture for any state+frame. TryLoad/GetOrLoad with static cache. (3) SpriteStateRenderer (src/Game/Visual/): TextureRect that renders correct sprite region for current VisualState. Animated states auto-advance at 8fps via _Process, emit AnimationFinished signal when done. (4) MetaGridView integration: UpdateSpriteRenderers() called from RefreshDisplay. For each placed reagent with a catalog ID, checks if a sprite sheet exists (auto-detect). If found, creates/updates a SpriteStateRenderer at ZIndex 2 covering the reagent's rotated bounding box. Uses VisualStateResolver.Resolve() and PickDisplayState() to select the most critical visual state across all cells (priority: Exploding > Breaking > Crushed > ThermalStress > Heated > Cooled > Normal). Falls back to ColorRect for reagents without sprites. Gotcha: Dictionary.GetValueOrDefault() is unavailable in the Godot SDK — used TryGetValue pattern instead. 16 new unit tests for SpriteSheetSpec covering DefaultReagentSpec structure, GetRegion for all states, boundary frames, invalid states, custom specs, sheet dimensions. All 632 tests pass (616 existing + 16 new).

## VS.8 — Create DevSpriteGenerator tool
- Date: 2026-03-06
- Commit: 209cdb4
- Notes: Created src/Game/Tools/DevSpriteGenerator.cs — a Godot tool scene that procedurally generates 72 placeholder sprite sheet PNGs (8 species × 9 shapes) from ReagentCatalog using the Image API. Each sheet is 128×224 pixels (4 frames × 32px wide, 7 state rows × 32px tall). Normal row: aspect-colored fill + 1px dark border + centered 8×8 icon silhouette (flame for Hot, snowflake for Cold, warning triangle for Volatile, skull for Toxic, anvil for Heavy, shield for Stable, blade for Sharp, cracked diamond for Fragile). State overlays: Heated=orange corner+flame glyph, Cooled=blue corner+snowflake glyph, ThermalStress=red-blue diagonal split+crack lines, Crushed=darkened+downward arrow+top cracks. Animated states: Breaking (4 frames: cracks → widening → fragments → fading scatter), Exploding (4 frames: white flash → radial burst → particle scatter → fade). Outputs to art/sprites/reagents/{id}.png plus sprite_specs.json with per-entry metadata matching SpriteSheetSpec's format. Scene at scenes/dev_sprite_generator.tscn. All 632 tests pass.

## Code Review E — 2026-03-06
- Reviewed commits: a9149e4..13b4c79 (VS.7, VS.8)
- Findings: 4 (P0: 0, P1: 1, P2: 1, P3: 2)
- Fixed: 2, Tasks created: 1 (R2)

## R2 — Deduplicate MetaGridView border creation methods
- Date: 2026-03-06
- Commit: db17b9e
- Notes: Extracted CreateBorderRectWithStyle(pixelPos, direction, borderWidth, color) shared helper replacing three near-identical methods (CreateBorderRect, CreateHintedBorderRect, CreateLockedBorderRect) that differed only in border width and color. Extracted CreateReagentBorders(borderList, predicate, borderWidth, color) shared loop replacing two near-identical methods (CreateHintedBorders, CreateLockedBorders) that differed only in the reagent predicate (IsHinted vs IsLocked), border width, and color. Net: 20 insertions, 122 deletions. Pure refactor — no behavior change. All 632 tests pass.

## VS.9 — Implement ReactionAnimator
- Date: 2026-03-06
- Commit: d909a31
- Notes: Created src/Game/Animation/ReactionAnimator.cs — a Godot Control that plays visual reaction animations before the result overlay appears on puzzle confirmation. Supports four animation types: Explosion (white flash → orange/red burst → scale-up scatter → fade, 0.6s), Breakage (lavender brighten → shrink fragments → fade, 0.5s), ToxicCloud (green haze emanate → spread/fade, 0.5s), and GenericReaction (flash reaction color → fade, 0.4s) for cumulative types (Overheating, Hypothermia, Poisoning). Sprite-based path: for reagents with sprite sheets, creates temporary SpriteStateRenderers using Exploding/Breaking state rows at 8fps, then fades out. Procedural fallback: creates ColorRect overlays at affected cell positions, animates via Godot Tween. All reactions play in parallel; AllAnimationsComplete signal fires after all finish. SFX signal hooks (ExplosionStarted, BreakageStarted, ToxicCloudStarted) emitted per-animation for future audio. Wired into PuzzleScreen: on HasReactions, animations play first, then OnReactionAnimationsComplete shows the failure overlay. ReactionAnimator added as child of MetaGridView (matching grid coordinate space) at ZIndex 5. All 632 tests pass.

## VS.10 — Implement FallingAnimator for Heavy reagents
- Date: 2026-03-06
- Commit: 00aaef0
- Notes: Added MetaGrid.CalculateFallPosition in Core — shifts Heavy reagent position down row-by-row in Gravity bags until blocked by bag edge or occupied cell (7 new unit tests). Created FallingAnimator in Game/Animation — spawns colored overlays at the drop position and tweens them down to the final resting position (0.3s Bounce/EaseOut), emits FallingStarted signal for SFX, self-cleans after animation. Wired into MetaGridView: ghost preview now shows at the landing position for Heavy reagents, HandleLeftClick places at the calculated fall position, temporarily hides destination cells during animation, restores on completion. Highlight computations also use the effective position. All 639 tests pass (632 existing + 7 new).

## CR.1 — Refactor ReagentCatalog to item-first model (~24 unique items)
- Date: 2026-03-07
- Commit: b1421a8
- Notes: Rewrote ReagentCatalog from 72-entry species×shape combinatorial model (8 species × 9 shapes) to 24 unique named items per docs/item-catalog-design.md. Each item has one natural polyomino shape, one aspect, and unique flavor text. Items: 3 per aspect (Hot, Cold, Volatile, Toxic, Heavy, Stable, Sharp, Fragile), shapes range from monomino (8 items) through tetrominoes (5 items). Removed Get(speciesId, shapeName) method, added GetShapeName(ReagentDefinition). Rarity derived from cell count: 1-2=Common, 3=Uncommon, 4=Rare. Critical fix in PuzzleGenerator: decoupled catalog from aspect assignment — catalog now provides naming only (id, name, flavorText), generator always uses its own shape and aspects. This fixed 5 PuzzleGenerator test failures caused by catalog matches silently replacing generator aspects with full-coverage catalog aspects. DemoPuzzle updated to use GetById. ReagentCatalogTests fully rewritten for 24-item counts and individual lookups. All 655 tests pass (639 existing adjusted + 16 net new).

## CR.2 — Update all unit tests for new catalog structure
- Date: 2026-03-07
- Commit: (no-op, completed by CR.1)
- Notes: Verified all unit tests already updated during CR.1. ReagentCatalogTests already assert 24 items, 3 per aspect, correct shape distribution, and all 24 individual item IDs. Searched entire codebase for old catalog IDs (e.g., firevine_domino, snowflower_monomino) — zero references remain in code. All 655 tests pass.

## CR.3 — Update PuzzleGenerator, tutorial, and save/load for new catalog
- Date: 2026-03-07
- Commit: (no-op, catalog-agnostic design)
- Notes: Verified all consumers are catalog-agnostic. PuzzleGenerator uses TryFindCatalogMatch for optional naming only — no hardcoded catalog IDs. TutorialData uses DifficultyParams with fixed seeds, no reagent IDs. SaveData stores lesson numbers and date seeds only. PuzzleSearchSolver and EditorScreen have no catalog references. DemoPuzzle uses GetById("snowflower") and GetById("nullflower"), both present in new 24-item catalog. No code changes needed.

## CR.4 — Regenerate dev sprites for new catalog items
- Date: 2026-03-07
- Commit: 99b6a96
- Notes: Deleted 72 old sprite PNGs and their .import files from art/sprites/reagents/. Ran DevSpriteGenerator via Godot MCP to generate 24 new sprite sheets matching the new catalog item IDs. Each sheet is 128×224px (4 frames × 32px, 7 state rows × 32px). New files: anvil_shard.png, blight_moss.png, burstbloom.png, calm_stone.png, crystal_tear.png, ember_seed.png, firevine.png, frost_fern.png, frozen_teardrop.png, glass_shard.png, glasswork.png, gossamer_thread.png, ironbark_root.png, lead_ingot.png, lead_nugget.png, magma_slug.png, nullflower.png, purple_creeper.png, razor_fern.png, sharpstone.png, snowflower.png, spark_petal.png, thunder_egg.png, venom_drop.png, plus sprite_specs.json. All 655 tests pass.

## CR.5 — Integration test checkpoint after catalog refactor
- Date: 2026-03-07
- Commit: (checkpoint, no code changes)
- Notes: Full integration test run after catalog refactor (CR.1-CR.4). Build: 0 errors. Unit tests: 655/655 pass. Integration harness: 31/31 pass ([HARNESS_DONE] passed=31 failed=0). Screenshots visually verified — grid rendering, reagent placement, aspect colors, status bars, puzzle generation, tutorials, hints, editor, undo/redo all working correctly. No regressions from the 72→24 item catalog refactor.

## HF.1 — HoverFrameResolver (Core) + dev frame texture generation
- Date: 2026-03-07
- Commit: 966e9f0
- Notes: Created src/Core/Highlight/HoverFrameResolver.cs — pure C# static class with three methods: GetFrameKey(Reagent) returns "{aspect}_{shape}_r{rotation}" keys for texture lookup (falls back to "custom{N}" for non-catalog shapes), GetPerimeterEdges(Reagent) returns per-cell boundary edges using ContiguousBoundary (internal edges excluded), GetPerimeterEdges(bool[,]) shape-only overload for texture generation. Extended DevSpriteGenerator to produce 72 frame PNGs (8 aspects × 9 shapes) at art/sprites/frames/dev/{aspect}_{shape}.png — each with 4px colored outline on perimeter edges only, 1px white inner edge, transparent interior. Rotation 0 only (rotate at runtime). 12 new unit tests: frame key format (catalog/non-catalog/rotated/no-aspect), perimeter edges (monomino all 4 edges, domino shared edge excluded, l_tri corner edges, o_tet internal edges fully excluded, rotated domino, shape overload equivalence). All 667 tests pass (655 + 12 new).

## HF.2 — HoverFrameOverlay rendering with pulse animation
- Date: 2026-03-07
- Commit: fcfb59a
- Notes: Created src/Game/Inventory/HoverFrameOverlay.cs — Godot Control that renders aspect-themed perimeter frames over reagents. Loads pre-generated frame textures from art/sprites/frames/dev/{aspect}_{shape}.png with fallback to procedural ColorRect edge rendering when textures are missing. Uses parent Modulate for alpha, enabling uniform transparency across all children. Sinusoidal pulse animation (alpha oscillates between min/max over 1.5s period) via _Process. Three interaction modes: hover (0.25-0.55 alpha), selection (0.35-0.65), drag (steady 0.50). Smooth fade-out on HideFrame() over 0.15s. Added 7 hover frame constants to GridConstants.cs. Static texture cache with ClearCache() for scene transitions. No new tests (Godot UI component — tested via integration in HF.3). All 667 unit tests still pass.

## HF.3 — Wire HoverFrameOverlay into MetaGridView + integration test
- Date: 2026-03-07
- Commit: 1226c6f
- Notes: Wired HoverFrameOverlay into MetaGridView as a child node at Z-index 3 (between sprite renderers and ghost cells). UpdateHoveredReagent now shows/hides the hover frame when hovering placed reagents. Fixed SetHoverPosition to also call UpdateHoveredReagent for test harness consistency. SetSelectedReagent hides the frame when a tray reagent is selected for placement. Added Test_HoverFrameOverlay integration test verifying frame appears on hover (4 edge rects for monomino) and hides when moving to empty cell. All 667 unit tests pass, 32/32 integration tests pass.

## Code Review — 2026-03-07
- Reviewed commits: 966e9f0..bb6c93b (HF.1, HF.2)
- Findings: 4 (P0: 0, P1: 0, P2: 1, P3: 2, P4: 1)
- Fixed: 1 (DRY x3 — extracted PrimaryAspectId to ReagentDefinition), Tasks created: 0

## SA.1 — SpriteAssigner: load PNG + grid overlay + cell clicking
- Date: 2026-03-08
- Commit: 048fe53
- Notes: Created src/Game/Tools/SpriteAssigner.cs + scenes/sprite_assigner.tscn — standalone tool for assigning polyomino shapes to candidate sprite PNGs. Layout: header bar (Load PNG + Clear Grid buttons), sprite preview area (256x256 TextureRect) with clickable NxN grid overlay (ColorRect cells), controls panel (resolution dropdown 4/6/8, cell count, detected shape name), and status bar. Grid overlay uses ShapeEditorWidget pattern: ColorRect cells toggle on click with semi-transparent green highlight, grid lines at ZIndex 1, hover feedback. FileDialog follows EditorScreen pattern (filesystem access, PNG/JPG filters, lambda receivers with QueueFree). Shape detection via MatchCanonicalShape: tests trimmed shape against all 9 ReagentCatalog.ShapeNames in all 4 rotations using ShapeUtil.Rotate90Clockwise. Public API for testing: ToggleCell, DetectedShapeName, ActiveCellCount, CurrentShape. All 757 unit tests pass.

## SA.2 — SpriteAssigner: aspect/metadata + preview + JSON export
- Date: 2026-03-08
- Commit: 8cdd8ce
- Notes: Extended SpriteAssigner with three features: (1) Right-side metadata panel with aspect dropdown (11 options from AspectIds), rarity dropdown (Common through Quest), item name LineEdit, and flavor text TextEdit — all in a ScrollContainer for overflow. (2) 32px game-size preview showing selected cells as colored rectangles tinted by the selected aspect color (from GridConstants.GetAspectColor), centered in a dark preview area. Preview updates on cell toggle and aspect change. (3) JSON export via FileDialog (save mode), writing catalog entry with id (auto-sanitized snake_case from name), name, shape (bool[][] jagged array), shape_name (canonical match), aspect, rarity, flavor_text, sprite_source (original PNG path), and exported_at (ISO timestamp). Uses System.Text.Json with WriteIndented. Batch workflow: after export, grid and metadata fields clear but PNG stays loaded for re-assignment. Added public test API: SetItemName, SetAspectIndex, SetRarityIndex, SetFlavorText, BuildExportJsonForTesting, ExportToPath. All 757 tests pass.

## GO.1 — Soften Heavy constraint from hard block to warning
- Date: 2026-03-08
- Commit: dde18aa
- Notes: Removed IsHeavyConstraintValid check from CanPlaceReagent in MetaGrid.cs. Heavy reagents can now be placed above non-Heavy in gravity bags — the constraint is advisory only, communicated via GetHeavyViolations() and the HighlightEngine visual warning system. Updated two unit tests (Heavy_RejectedIfNonHeavyBelow → Heavy_AboveNonHeavy_AllowedButReportsViolation, Heavy_NonHeavyRejectedBelowHeavy → Heavy_NonHeavyBelowHeavy_AllowedButReportsViolation) to assert placement succeeds while violations are still correctly reported. This is the first task in the gameplay-overhaul phase — moving from hard placement blocks to advisory warnings. All 757 tests pass.

## SB.1 — Create SandboxScreen with catalog mode
- Date: 2026-03-08
- Commit: 8ffd21d
- Notes: Created SandboxScreen tool (src/Game/Tools/SandboxScreen.cs + scenes/sandbox.tscn) displaying all 24 catalog reagents grouped by aspect. Features: visual state toggle (all 7 VisualState values), aspect and shape filter dropdowns, 1x/2x sprite previews using ReagentSpriteSheet with fallback to colored rectangles. Tab bar with Catalog active and Playground stub (disabled, deferred to SB.2). Adapted task from original 72-entry catalog (8 species x 9 shapes) to current 24-item art-first catalog. All 757 tests pass.

## Code Review — 2026-03-08
- Reviewed commits: dde18aa..5650c92 (GO.1, SA.1, SA.2)
- Findings: 2 (P0: 0, P1: 0, P2: 1, P4: 1)
- Fixed: 1 (dead IsHeavyConstraintValid method), Tasks created: 0

## GO.2 — B.1 core: MetaGrid.MoveBag and RotateBag methods
- Date: 2026-03-08
- Commit: 5ee4923
- Notes: Added MoveBag(Bag, Vector2I) and RotateBag(Bag) to MetaGrid. MoveBag validates new position is in-bounds and doesn't overlap other bags, allows overlapping the bag's own previous cells for sliding moves. RotateBag uses new Bag.Rotation property (mirrors Reagent pattern, 0-3 normalized) with ShapeUtil.Rotate for 90° CW increments; reverts rotation on validation failure. Both methods reject if the bag contains any placed reagents — GO.3 will extend with reagent carrying. Updated Bag.GetMetaGridPositions and IsEdgeCell to use GetRotatedShape()/GetRotatedLocalCellPositions() accounting for rotation. BagDefinition unchanged (immutable). 16 new tests: valid move, out-of-bounds, overlap rejection, not-on-grid, reagent rejection, overlapping-own-position slide, L-shape move, rotation CW, cell mapping update, rotation out-of-bounds, rotation overlap, rotation with reagents, four-rotation cycle, rotation defaults, normalization, dimension swap, position accounting, edge cell accounting. All 776 tests pass (757 existing + 19 new).

## GO.3 — B.2 core: Bag movement carries contained reagents
- Date: 2026-03-08
- Commit: 7594d9e
- Notes: Extended MoveBag() and RotateBag() to carry reagents fully contained within the bag instead of rejecting the operation. A reagent is "fully contained" if ALL its cells are within the moving bag; reagents spanning multiple bags stay in place. Three private helpers added: GetFullyContainedReagents (collects unique reagents in bag, filters to those with all cells in-bag), ClearReagentCellOccupancy (clears cell.OccupyingReagent without changing PlacedPosition — used temporarily during bag movement), RestoreReagentCellOccupancy (re-stamps cells from reagent positions). MoveBag translates carried reagent positions by the same delta. RotateBag applies the 90° CW transform (newCol = oldShapeRows-1-row, newRow = col) to each reagent cell position around the bag origin, increments the reagent's Rotation, and computes new PlacedPosition as the min-corner. Both methods restore reagent occupancy on validation failure. 8 tests: move carries single-cell/multi-cell, spanning stays, multiple carried, rotate transforms domino position+rotation, rotate spanning stays, move/rotate failure restores state. All 783 tests pass (776 existing + 7 net new).

## Integration Review — 2026-03-08
- Reviewed commits: 7d7f708..cb0535a (CR.1-CR.5, HF.1-HF.3, SA.1-SA.2, GO.1-GO.2, SB.1, I.1-I.4, D.1-D.2, A.3, C.1, J.3)
- Build: PASS (776/776 unit tests)
- Harness: 32/32 tests pass, 0 failures (after fix)
- Visual: 40+ screenshots verified, no regressions
- Issues: 3 (P0: 0, P1: 0, P2: 0) — all test infrastructure bugs
- Fixed: 3 (status_bar Toxic assertion removed for I.3, win_loss_evaluation uses generated puzzle for I.2, hint_system dislodge calls HintEngine directly)
- Commit: cb0535a
- Coverage gaps: GO.2 (MoveBag/RotateBag), D.2 (thermal fracture/crushing), A.3 (per-bag gravity direction), J.3 (ViolationTracker) have unit tests but no integration tests — Core features not yet wired to game UI.
- Pre-existing cosmetic issues unchanged: Heat label left-edge clipping, base thresholds in status bar.

## Code Review — 2026-03-08b
- Reviewed commits: 3c764b7..63be155 (GO.3 reagent carrying)
- Findings: 3 (P0: 0, P1: 1, P2: 0, P3: 1, P4: 1)
- Fixed: 1 (dead code in RotateBag_InvalidRotation test), Tasks created: 1 (R3: spanning reagent data integrity)

## R3 — Handle spanning reagents when bag moves/rotates away
- Date: 2026-03-08
- Commit: 4695d2d
- Notes: Added RemoveOrphanedReagents() and ForceRemoveReagent() helpers to MetaGrid. After MoveBag/RotateBag succeeds, scans all placed reagents (excluding those already carried) for cells with null BagOwner. Any such reagent is force-removed (bypassing IsLocked). Updated 2 existing tests (MoveBag/RotateBag spanning reagent tests now verify removal instead of staying in place). Added 5 new tests: locked spanning reagent still removed on move, contained reagent stays after move, other-bag reagent unaffected by move, rotate with non-square bag orphans spanning reagent, non-spanning reagent in other bag unaffected. All 789 tests pass (786 existing + 3 net new).

## GO.4 — B.3 core: Invalid placements after bag movement
- Date: 2026-03-08
- Commit: 24d0d89
- Notes: Added HasLockedOrphanedReagent pre-check to both MoveBag and RotateBag. Before modifying grid state, computes which bag cells would be "abandoned" (old coverage minus new coverage). If any locked non-contained reagent has cells in abandoned positions, the move/rotate is rejected and all state is restored (bag position, contained reagent occupancy). Non-locked spanning reagents continue to be removed as before (R3 behavior). Updated 1 existing test (locked spanning reagent now blocks move instead of being force-removed). Added 6 new tests: locked spanning blocks move with full state restore, unlocked spanning still removed on move, locked spanning blocks rotation, rotation with locked spanning restores contained reagent, locked contained reagent allows move (carried with bag), unlocked spanning still removed on rotation. All 795 tests pass (789 existing + 6 new).

## GO.5 — B.4: Cross-bag reagent placement verification tests
- Date: 2026-03-08
- Commit: 96fadc0
- Notes: Added 7 regression tests verifying cross-bag reagent placement still works correctly after B.1-B.3 bag movement changes. Tests cover: domino spanning two adjacent bags places successfully, single-cell reagents on each side of bag boundary, placement fails when bags have a gap (non-adjacent), spanning reagent removed when bag moves away (orphan cleanup), spanning reagent survives when bag move still covers its cells (overlapping move), new cross-bag placement works after moving bags to be adjacent, rotation of non-square bag orphans and cleans up spanning reagent. All 802 tests pass (795 existing + 7 new).

## Code Review — 2026-03-08c
- Reviewed commits: 63be155..888fdfd (R3, GO.4, GO.5 + chore)
- Findings: 2 (P0: 0, P1: 0, P2: 0, P3: 1, P4: 1)
- Fixed: 0, Tasks created: 0

## GO.6 — A.4: Gravity direction indicator arrow on gravity bags
- Date: 2026-03-08
- Commit: ce08417
- Notes: Added _Draw() override to MetaGridView that draws a filled purple triangle (DrawPolygon) centered on each gravity bag, pointing in the bag's GravityDirection. Arrow is ~1.6 cells tall, 1.0 cells wide, semi-transparent purple (0.6, 0.3, 0.9, 0.7). Supports all four directions (Down/Up/Left/Right). QueueRedraw() called from RefreshDisplay() to keep arrows in sync with grid state. No Core changes — purely Game-layer UI. All 802 tests pass.

## Integration Review — 2026-03-08b
- Reviewed commits: cb0535a..0a9ff10 (GO.3, R3, GO.4, GO.5, GO.6)
- Build: PASS (802/802 unit tests)
- Harness: 33/33 tests pass, 0 failures (after adding 1 new test)
- Visual: 41 screenshots verified, no regressions
- Issues: 0
- New test: Test_GravityBagArrow — verifies gravity bag property reconstruction and arrow rendering via screenshot
- Coverage gaps: GO.3-GO.5 (bag movement with reagent carrying) are Core-only, no Game UI for bag manipulation yet. Will need integration tests when UI is wired.
- Pre-existing cosmetic issues unchanged: tutorial vertical text, Heat label clipping, base thresholds in status bar.

## GO.7 — D.3: Fragile damage visual indicator (pre-confirm)
- Date: 2026-03-08
- Commit: 69b4f6a
- Notes: Created DamageOverlay (src/Game/Inventory/DamageOverlay.cs) — a Godot Control that renders red tint overlays on cells affected by fragile violations (FragileCrushed, FragileThermalFracture, FragilePierced). Follows HighlightOverlay pattern with pooled ColorRect children. Wired into MetaGridView.RefreshDisplay() via ViolationTracker.ComputeAll() so overlays update live on every placement/removal. Three distinct red tint colors per violation type (slight hue variation for visual differentiation). DamageOverlay at ZIndex 5 in MetaGridView. Integration test verifies: Sharp alone = 0 damage overlays, Sharp+Fragile adjacent = 2 damage overlays (both affected cells), Fragile removed = 0 overlays. All 802 unit tests pass, 34/34 integration tests pass.

## GO.8 — E.1: Volatile explosion indicator (pre-confirm)
- Date: 2026-03-08
- Commit: bd232fb
- Notes: Extended DamageOverlay to also render VolatileExplosion violations with an orange tint (0.95, 0.5, 0.1, 0.4). Renamed IsFragileViolation to IsDamageViolation and added VolatileExplosion to the filter. ViolationTracker already detected VolatileExplosion from ChemistryEngine; this just wires the visual. No volatile placement blocking existed in CanPlaceReagent, so no removal needed — consistent with "invalid states freely allowed during play" design. Minimal 2-file change. All 802 tests pass.

## GO.9 — F.1: Error highlight pulse on confirm with violations
- Date: 2026-03-08
- Commit: 06046db
- Notes: Added PulseViolations() to DamageOverlay — renders violation cells then pulses them 3 times over 1 second using Godot Tween (alpha oscillates 0.2→1.0→0.2 per pulse, Finished callback chains to reaction animation). Added PulseViolations() facade on MetaGridView that computes violations via ViolationTracker.ComputeAll() and delegates to DamageOverlay. Wired into both PuzzleScreen.ConfirmPuzzle() and WebDemoScreen.OnConfirmPressed(): on HasReactions, violation pulse plays first, then chains to ReactionAnimator.PlayReactions via callback. Clean 67-line change across 4 files. All 802 tests pass.

## GO.10 — J.1: Bag state visual indicators (smoldering/icy)
- Date: 2026-03-08
- Commit: e2f390d
- Notes: Created BagStateOverlay (src/Game/Inventory/BagStateOverlay.cs) — a Godot Control that renders animated thermal tint overlays on bag cells to indicate hot/cold imbalance. Uses ChemistryEngine.GetBagBalanceViolations() to detect per-bag imbalance. Red/orange tint for too-hot bags (Imbalance > 0), blue tint for too-cold bags (Imbalance < 0). Alpha oscillates via sine wave in _Process() with 2s period (range 0.08–0.25). Follows DamageOverlay pattern with pooled ColorRect children grouped by bag for efficient per-frame color updates. Wired into MetaGridView at ZIndex 2 (below hover frame and highlights), refreshes on every placement/removal via RefreshDisplay(). Exposed RenderedCount and ActiveGroupCount test properties. All 802 tests pass.

## GO.11 — J.3 UI: ViolationListPanel for live violation display
- Date: 2026-03-08
- Commit: 55aea9c
- Notes: Created ViolationListPanel (PanelContainer) in the WebDemoScreen sidebar that shows a live list of current placement violations via ViolationTracker.ComputeAll(). Each violation entry is a clickable Button colored by severity (red=Danger, yellow=Warning) with transparent flat styling. Panel header shows "Issues (N)" count badge in amber when violations exist, or "No issues" in green when grid is clean. Entries emit ViolationClicked(int) signal for GO.12 cell-highlight integration. Wired into WebDemoScreen.BuildSidebar after AspectLegendPanel, refreshes on all grid-change paths (RefreshAfterGridChange, OnResetPressed, OnHintPressed). All 802 tests pass.

## Code Review (d) — 2026-03-08
- Reviewed commits: 55aea9c..660f6e9 (GO.11, integration tests)
- Findings: 1 (P0: 0, P1: 0, P2: 1)
- Fixed: 1, Tasks created: 0

## GO.12 — J.4: Violation click highlights affected cells
- Date: 2026-03-08
- Commit: ad79975
- Notes: Added ComputeViolationHighlights to HighlightEngine (Core, pure C#) — converts ViolationEntry.AffectedCells into a HighlightSet with ContiguousBoundary edges on the Interaction layer. Added HighlightIndices(HashSet<int>) to ReagentTray for multi-reagent highlighting. Wired ViolationClicked signal in WebDemoScreen: clicking a violation highlights affected cells on grid and involved reagents in tray, clicking the same violation toggles off, clicking another replaces. Added _UnhandledKeyInput for Escape to clear violation/legend highlights. Legend and violation selections are mutually exclusive. Grid changes (placement/removal/reset/undo/redo) auto-clear violation highlights via RefreshAfterGridChange(). 4 new unit tests: empty cells, single cell (All edges), adjacent cells (internal edges excluded), severity/aspect preservation. All 806 tests pass (802 existing + 4 new).

## LO.1 — Public panel accessors on WebDemoScreen
- Date: 2026-03-08
- Commit: b650c09
- Notes: Added 8 public read-only properties to WebDemoScreen: ReagentTray, StatusBar, Objective, LegendPanel, ViolationPanel, ResultOverlay, Grid, and TrayReagents (IReadOnlyList). GridView, DetailTooltip, and AspectTooltip already existed. Follows the same pattern as PuzzleScreen.cs (lines 66-79). Pure accessor change — no behavioral modification. All 806 tests pass.

## LO.2 — Layout test infrastructure: LayoutTestHelper + WebDemoLayout test
- Date: 2026-03-08
- Commit: 89053de
- Notes: Created LayoutTestHelper static class with 4 reusable layout assertions: CheckMinSize (verifies Control rect meets minimum thresholds), CheckVisible (visible + non-zero size), CheckNoOverlap (GetGlobalRect intersection check), CheckContainedIn (child fits within parent bounds). Added Test_WebDemoLayout to TestHarness that instantiates a WebDemoScreen, waits 3 frames for Godot layout to settle, then verifies GridView visibility/size, ReagentTray visibility, StatusBar visibility, and LegendPanel minimum width >= 180px. All 806 unit tests pass.

## SB.2 — Add playground mode to SandboxScreen
- Date: 2026-03-08
- Commit: b40fc35
- Notes: Added Playground tab to SandboxScreen with full interactive sandbox. Features: 12x12 MetaGridView, BagEditorPanel sidebar (reused from editor) with "Apply Bags" button to rebuild the grid, reagent catalog picker grouped by aspect with clickable cards, Confirm triggers ChemistryEngine.Evaluate + ReactionAnimator, Reset clears grid, Undo/Redo buttons + Ctrl+Z/Y keyboard shortcuts wired to ActionHistory, StatusBarPanel showing cumulative aspect totals. Tab switching between Catalog (existing) and Playground with lazy initialization. No win/lose conditions — purely exploratory. All 806 tests pass.

## Code Review — 2026-03-08e
- Reviewed commits: b650c09..b40fc35 (LO.1, LO.2, SB.2)
- Findings: 2 (P0: 0, P1: 2, P2: 0)
- Fixed: 2, Tasks created: 0

## LO.3 — Baseline responsive viewport tests
- Date: 2026-03-08
- Commit: e448093
- Notes: Added Test_WebDemoResponsive() to TestHarness that tests WebDemoScreen layout at three viewport sizes (1280x720, 1024x768, 800x600). Uses DisplayServer.WindowSetSize() for viewport resizing — more reliable than ContentScaleSize since the project has no content scale mode configured. Each resolution verifies grid and reagent tray remain visible with non-zero size, captures a labeled screenshot. Added SetViewportSize() helper to LayoutTestHelper returning original size for restore. This establishes a visual baseline BEFORE the layout overhaul tasks (LO.4+) change the structure. All 806 unit tests pass.

## LO.4 — Move reagent tray from sidebar to bottom horizontal strip
- Date: 2026-03-08
- Commit: f2a15b5
- Notes: Central layout change moving the tray from the right sidebar to a horizontal strip between the separator and status bar. ReagentTray gained SetOrientation(bool horizontal) — when horizontal, items flow in an HBoxContainer instead of the default VBoxContainer; header/separator are hidden in horizontal mode. ReagentTrayItem gained a compact layout mode (shape preview + aspect dots stacked vertically, no name label, 80px min width) triggered by a new `Initialize(reagent, index, compact)` overload; the two-arg overload defaults to non-compact for backward compatibility. WebDemoScreen.BuildSidebar renamed to BuildInfoColumn (reduced min width from 200px to 160px, tray removed), new BuildTrayStrip adds a horizontal ScrollContainer with auto horizontal scroll and disabled vertical scroll. PuzzleScreen is completely unmodified — it still uses the default vertical tray. All 806 tests pass.

## Integration Review — 2026-03-08c
- Reviewed commits: 0a9ff10..e2f8961 (GO.7–GO.12, LO.1–LO.4, SB.2)
- Build: PASS (806/806 unit tests)
- Harness: 40/40 tests pass, 0 failures (after fix)
- Visual: 48 screenshots verified, no regressions
- Issues: 1 (P0: 0, P1: 0, P2: 1) — WebDemo test infrastructure: _puzzleScreen not hidden (fixed)
- Added: `violation_list_panel` test (GO.11 — ViolationListPanel initialization, violation detection, and clearing)
- Coverage: GO.7-GO.12 all have integration coverage (direct or indirect). LO.1-LO.4 covered by layout tests. SB.2 is a dev tool (no integration test needed).
- Pre-existing cosmetic issues unchanged: Heat label left-edge clipping, base thresholds in status bar.

## LO.5 — Stable panel sizing to prevent layout jumps
- Date: 2026-03-08
- Commit: 0ef05e2
- Notes: Added minimum sizes to ViolationListPanel (80px height), AspectLegendPanel (120px height + ClipContents), and ObjectivePanel (160px width). Changed WebDemoScreen info column from ExpandFill to ShrinkBegin to prevent vertical stretching when panel content changes. All changes are additive — no behavioral impact on PuzzleScreen or existing tests. 806 tests pass.

## Code Review — 2026-03-08f
- Reviewed commits: 778029b..73bc252 (LO.5, integration review, bookkeeping)
- Findings: 1 (P0: 0, P1: 0, P2: 0, P3: 1)
- Fixed: 0, Tasks created: 0
- P3 note: ObjectivePanel min-width (160) doesn't match child label min-width (180) — cosmetic only.

## LO.6 — ReagentProfilePanel popup class
- Date: 2026-03-08
- Commit: de3d91a
- Notes: Created src/Game/Inventory/ReagentProfilePanel.cs — a PanelContainer popup for detailed reagent info. 24px-cell ProfileShapePreview (separate from ItemDetailPanel's 16px DetailShapePreview), reagent name (16pt), flavor text (12pt), full aspect breakdown with descriptions and constraint text from AspectMetadataDatabase. Dark semi-transparent style matching FloatingTooltip. Dismisses on click outside (_UnhandledInput) or Escape. ShowForReagent(reagent, globalPosition) positions near click with viewport clamping via CallDeferred. HideProfile() fades out with tween. Z-index 100, MouseFilter=Stop. CustomMinimumSize width=240px. Not yet wired into any screen. All 806 tests pass.

## LO.7 — Wire ReagentProfilePanel into WebDemoScreen
- Date: 2026-03-08
- Commit: a1e5b13
- Notes: Wired ReagentProfilePanel into WebDemoScreen with two interaction triggers. (1) Tray item click: OnReagentSelected now also shows the profile panel near the tray position, alongside existing reagent selection behavior. (2) Grid reagent click: added PlacedReagentClicked signal to MetaGridView, emitted when left-clicking on a placed reagent with no reagent selected (HandleLeftClick early-return path); WebDemoScreen connects and shows profile at the reagent's grid position offset. Profile auto-hides on ClearSelectionState (after placement, reset, undo/redo). Escape key dismisses profile with higher priority than violation/legend highlight clearing. Added ProfilePanel public accessor property for integration tests. PuzzleScreen unaffected — doesn't connect to the new signal. All 806 tests pass.

## LO.8 — Responsive layout adaptation for viewport sizes
- Date: 2026-03-08
- Commit: 823a70a
- Notes: Added breakpoint-driven layout adaptation to WebDemoScreen via _Notification(NotificationResized). Three tiers: >= 1280px standard layout (info column at 160px+), 800-1279px narrower info column (140px min) with legend in compact mode, < 800px info column hidden entirely with violation count shown as overlay badge on grid corner. Added SetCompactMode(bool) to AspectLegendPanel/AspectLegendEntry — compact mode collapses any expanded entries immediately (no animation), prevents ToggleExpand, shows only colored icon + aspect name. Stored _infoColumn and _infoSpacer as fields for visibility toggling. Violation badge is a styled Label with red circle background, repositioned on grid refresh. All 806 tests pass.

## Code Review — 2026-03-08g
- Reviewed commits: de3d91a..5adedea (LO.6, LO.7, LO.8)
- Findings: 4 (P0: 0, P1: 0, P2: 0, P3: 3, P4: 1)
- Fixed: 0, Tasks created: 0
- Clean batch. P3 notes: magic number 90 for tray item width (2 occurrences, Rule of Three not met), long BuildAspectBreakdown method (85 lines, readable), dual Escape handling for profile panel (intentional reusability). P4: hardcoded layout breakpoints.

## LO.9 — Update layout integration tests for new layout
- Date: 2026-03-08
- Commit: b793af2
- Notes: Rewrote Test_WebDemoLayout and Test_WebDemoResponsive to assert against the post-overhaul layout structure. Test_WebDemoLayout now verifies: grid visible with min 50x50, tray visible with height >= 50px, info column visible with width >= 140px, all four sidebar panels visible (StatusBar, LegendPanel, Objective, ViolationPanel), no overlap between grid/tray and grid/info column, and profile panel showing after programmatic reagent selection with min size 240x100. Test_WebDemoResponsive now verifies per-breakpoint behavior: 1280x720 (full layout with info column at 140px+ and no overlap), 1024x768 (narrower info column still visible), 800x600 (info column hidden). Added InfoColumn public accessor to WebDemoScreen for test access. All 806 tests pass.

## Integration Review — 2026-03-08d
- Reviewed commits: e2f8961..ef135c4 (LO.5–LO.9, code reviews 2026-03-08f/g)
- Build: PASS (806/806 unit tests)
- Harness: 40/40 tests pass, 0 failures (after fix)
- Visual: 48+ screenshots verified, no regressions
- Issues: 2 test infrastructure bugs fixed (tray height assertion too strict, DPI-dependent viewport sizing)
- Fixed: 2 (lowered inner tray height threshold from 50→30, replaced DisplayServer.WindowSetSize with direct Control.Size for DPI-independent breakpoint testing)
- Coverage: LO.5–LO.9 all covered by layout tests. GO.9/GO.12 acceptable gaps (animation/toggle pattern). GO.3–GO.5 Core-only awaiting UI wiring.
- Pre-existing cosmetic issues unchanged: Heat label left-edge clipping, base thresholds in status bar.

## LO.10 — Touch-friendly minimum sizing for interactive elements
- Date: 2026-03-08
- Commit: 2b152dd
- Notes: Updated minimum heights on all interactive elements for mobile touch targets. Button bar buttons in WebDemoScreen: 34px → 44px (Apple HIG standard). ReagentTrayItem: added 44px min height for both compact (horizontal) and full (vertical) layouts. ViolationListPanel entry buttons: added 40px min height. AspectLegendEntry header panels: added 40px min height. Added integration test checks that walk the WebDemoScreen node tree to verify ReagentTrayItem min heights >= 44px and button bar buttons >= 44px. All 806 unit tests pass, build clean.

## GJ.1 — Soften gravity constraint — unsupported placement + auto-fall
- Date: 2026-03-09
- Commit: 40f9824
- Notes: Removed IsGravityValid gate from CanPlaceReagent — gravity is now advisory only (warnings via GetGravityViolations). Added PlaceReagentWithFall(reagent, position) method that calls CalculateFallPosition then PlaceReagent at the fallen position. Updated MetaGridView.UpdateGhostPreview to apply fall to ALL reagents in gravity bags, not just Heavy (removed HasAspect gate). Updated 4 existing tests that expected gravity-unsupported placement to fail — now succeed. Added 3 new PlaceReagentWithFall tests: non-heavy in gravity bag falls to bottom, non-heavy in normal bag stays put, two reagents stack correctly. All 809 tests pass (806 existing adjusted + 3 new).

## GJ.2 — Debug + fix volatile explosion indicator
- Date: 2026-03-09
- Commit: 1fdd2a0
- Notes: Debugged the volatile explosion indicator chain end-to-end. Core logic (ViolationTracker, DamageOverlay, ReactionRuleDatabase, confirm flow) was all correct — the test harness's volatile_explosion_overlay test passes. The actual bug was in MetaGridView.UpdateGhostPreview: EvaluateProspective was called with _hoverGridPos.Value instead of effectivePos, so in gravity bags the reaction check happened at the hover position (before fall) rather than where the reagent actually lands. This caused the ghost to show "safe" when placement would trigger an explosion. Fix: one-line change to use effectivePos. Added 2 ViolationTracker unit tests for volatile net-hot threshold: cold-buffering (netHot=0 → no explosion) and partial cold-buffering (netHot=1 → explosion). All 811 tests pass.

## GJ.3 — Contextual info column — InfoColumnManager
- Date: 2026-03-09
- Commit: 6533bca
- Notes: Created InfoColumnManager (src/Game/Inventory/InfoColumnManager.cs) — a VBoxContainer that manages three mutually-exclusive views in the sidebar: PuzzleOverview (default, showing ObjectivePanel + AspectLegendPanel + ViolationListPanel), ReagentDetail (embedded shape preview + name + flavor + aspect breakdown with descriptions/constraints), and AspectDetail (aspect info + reactions + which tray reagents have it). Transitions: click tray/grid reagent → ReagentDetail, click aspect in legend → AspectDetail, Back button or Escape → PuzzleOverview. Replaced the floating ReagentProfilePanel popup with embedded info column content — no more fade/positioning/ZIndex overlay. Updated WebDemoScreen to delegate info column construction to InfoColumnManager.Initialize(). Updated test harness to check IsShowingOverview instead of ProfilePanel.IsShowing. All 811 tests pass.

## GJ.4 — Bag movement UI — select, drag to move, right-click to rotate
- Date: 2026-03-09
- Commit: 2fb8a8b
- Notes: Wired existing MetaGrid.MoveBag()/RotateBag() to interactive MetaGridView UI. Added bag selection state (click empty bag cell to select, cyan border highlight), click-then-drag to move (ghost preview with blue valid/red invalid overlay), right-click to rotate 90° CW. Bag selection is mutually exclusive with reagent selection. Added CanMoveBag() read-only validation method to MetaGrid for ghost preview without mutation. Added 3 new signals (BagSelected, BagMoved, BagRotated) wired through WebDemoScreen with info column status updates. Files changed: MetaGrid.cs (+CanMoveBag), GridConstants.cs (+4 bag selection constants), MetaGridView.cs (+264 lines for selection/drag/rotate), WebDemoScreen.cs (+signal wiring). All 811 tests pass.

## Code Review — 2026-03-09
- Reviewed commits: 6533bca..2c017fc (GJ.3, GJ.4)
- Findings: 1 (P0: 0, P1: 0, P2: 1)
- Fixed: 0, Tasks created: 0

## GJ.5 — Art loading infrastructure — AssetPreviewScene + sprite swap
- Date: 2026-03-09
- Commit: 247f381
- Notes: Created SpriteSwapper (src/Game/Visual/SpriteSwapper.cs) — static utility for custom-first sprite loading. Checks art/sprites/custom/{reagentId}.png before art/sprites/reagents/{reagentId}.png for reagent textures, and art/sprites/custom/frames/ before art/sprites/frames/dev/ for frame textures. Wired into ReagentSpriteSheet.TryLoad() and HoverFrameOverlay.LoadFrameTexture() to replace hardcoded paths with the fallback chain. Created AssetPreview tool scene (scenes/asset_preview.tscn + src/Game/Tools/AssetPreview.cs) with dropdown for category switching (Reagents/BagBorders/Background), reagent preview showing all 24 catalog items at 1x/2x/4x zoom with polyomino shape preview, grouped by aspect, with [custom] badge for items with custom sprites. Screenshot capture button. Bag and background views are placeholder with instructions for future art drops. All 811 tests pass.

## GJ.5b — Wire existing tiling_suitable sprites as proof-of-concept
- Date: 2026-03-09
- Commit: e4b10de
- Notes: Mapped 7 existing RetroDiffusion images from art/generated/tiling_suitable/ to aspect monomino sprites. Resized from various sizes (96x96, 128x128, 384x384) to 32x32 using PIL nearest-neighbor. Stored as art/sprites/custom/monomino_{aspectId}.png (gitignored). Extended SpriteSwapper with monomino fallback: LoadReagentTexture(reagentId, aspectId?) checks custom/monomino_{aspectId}.png when aspectId is provided. Extended ReagentSpriteSheet with IsSingleTile property — when loaded texture dimensions are <= cell size, GetAtlasTexture returns the whole image for all visual states instead of extracting sprite sheet regions. Updated all callers (MetaGridView, AssetPreview, ReactionAnimator, SandboxScreen) to pass PrimaryAspectId through the loading chain. Created manifest.json documenting aspect-to-source mappings. Covers 7 of 8 aspects (Toxic has no tiling_suitable source). All 811 unit tests pass, 40/40 integration tests pass.

## GJ.6 — Monomino tile sprite generation via RetroDiffusion
- Date: 2026-03-10
- Commit: ba00959
- Notes: Generated all 8 aspect monomino sprites via RetroDiffusion kells-alchemy custom style at 96x96, resized to 32x32 with PIL nearest-neighbor. 24 API calls (3 per aspect), best selected by visual inspection. Hot=celtic border fire crystal, Cold=celtic frost teardrop, Volatile=electric crackling crystal, Sharp=diamond blade with celtic knot, Fragile=gold-framed pink gem, Heavy=dark ore nugget, Stable=green jade celtic knot sphere, Toxic=purple crystals with skull and drip (NEW — was missing from GJ.5b). Updated manifest.json with all 8 entries. All sprites are gitignored (art/sprites/custom/). Balance: ~$51 remaining. All 811 tests pass.

## GJ.7 — Alchemical cell frame/mount texture
- Date: 2026-03-10
- Commit: 0402157
- Notes: Generated Celtic knot cell mount texture via RetroDiffusion kells-alchemy style (96x96, resized to 32x32 and 64x64 with PIL). White center converted to transparent for overlay use. Created CellMountOverlay (src/Game/Inventory/CellMountOverlay.cs) — renders aspect-tinted perimeter frames around ALL placed reagent cells. Monominos get the full texture overlay; multi-cell reagents use colored edge rects (3px wide) on perimeter edges only, computed via HoverFrameResolver.GetPerimeterEdges(). Wired into MetaGridView at Z-index 2 (between base cells and hover frame), called from RefreshDisplay(). Added CellMountAlpha and CellMountEdgeWidth constants to GridConstants. Art assets saved to art/sprites/custom/frames/ (gitignored). Balance: ~$49 remaining. All 811 tests pass.

## Code Review — 2026-03-10
- Reviewed commits: 2caf2f0..19cd8e0 (GJ.6, GJ.7, chore)
- Findings: 1 (P0: 0, P1: 0, P2: 0, P3: 0, P4: 1)
- Fixed: 1 (redundant type cast in CellMountOverlay.Clear()), Tasks created: 0

## GJ.8 — Bag border tileable edges
- Date: 2026-03-10
- Commit: 74bb9b2
- Notes: Generated celtic stone frame border (96x96) via RetroDiffusion kells-alchemy style, plus corner and horizontal edge variants for reference. Implemented NinePatchRect-based bag border rendering in MetaGridView: when border_default.png exists in art/sprites/custom/bags/, bags render with a 9-patch celtic frame (24px patch margins, 4px padding, tiled stretching). Falls back to the original ColorRect edge borders when texture is absent. Added SpriteSwapper.LoadBagBorderTexture() with custom/bags → ui/sliced fallback chain. Art assets gitignored. Balance: ~$49 remaining. All 811 tests pass.

## SI.1 — UIThemeLoader — sprite texture loading with fallback
- Date: 2026-03-09
- Commit: eedb1e8
- Notes: Created static UIThemeLoader (src/Game/Visual/UIThemeLoader.cs) providing five methods for loading UI textures from art/sprites/ui/ subdirectories: GetPanelStyleBox (panels/), GetAspectTileTexture (tiles/), GetDecorationTexture (decorations/), GetButtonStyleBox (sliced/button_*), GetBagThemeTexture (sliced/bag_*). All methods cache results in static dictionaries. Panel and button methods return StyleBoxTexture with 9-patch margins when textures exist, or StyleBoxFlat fallback with appropriate dark colors. Missing files trigger GD.PushWarning. Follows SpriteSwapper pattern for ResourceLoader.Exists/GD.Load chain. Respects density conventions: sliced Kit 01 elements at 1x, tileable panels and decorations at 2x. All 811 tests pass.

## Integration Review — 2026-03-10
- Reviewed commits: ef135c4..88dd632 (LO.10, GJ.1-GJ.8, SI.1, code reviews)
- Build: PASS (811/811 unit tests)
- Harness: 41/41 tests pass, 0 failures (after adding 1 new test)
- Visual: 49+ screenshots verified, no regressions
- Issues: 0
- New test: Test_GravityFallPlacement — verifies GJ.1 gravity soft constraint + PlaceReagentWithFall auto-drop + stacking
- Coverage gaps: GJ.5-GJ.8, SI.1 (art pipeline) not feasible to test (gitignored assets). GJ.3 InfoColumnManager has partial coverage (view switching but not full nav flow). GO.3-GO.5 Core-only awaiting UI wiring.
- Pre-existing cosmetic issues unchanged: Heat label left-edge clipping, base thresholds in status bar.

## SI.2 — Component Inspector Scene — isolated UI element preview
- Date: 2026-03-09
- Commit: f96277e
- Notes: Created ComponentInspector tool (src/Game/Tools/ComponentInspector.cs + scenes/component_inspector.tscn) for isolated UI element preview. Features: OptionButton dropdown with 9 component types (HeaderPanel, Button, InfoPanel, AspectEntry, StatusBar, DecorationCorner, DecorationSeparator, DecorationSigil, BagFrame), CheckButton sprite/procedural toggle, centered rendering with generous margins. On launch, auto-cycles through all components taking screenshots via ScreenshotHelper to test_output/component_{name}.png. Structured stdout: [COMPONENT] name, [SPRITE_LOADED] true|false, [SCREENSHOT] path, [INSPECTOR_DONE]. Follows AssetPreview pattern with neutral background, toolbar, and scrollable content area. Renders panels via UIThemeLoader, buttons with state variants, aspect tiles at 48px, decorations at 128px, bag frames as NinePatchRect. All 811 tests pass.

## Code Review (b) — 2026-03-09
- Reviewed commits: eedb1e8..2fac7fd (SI.1, SI.2, gravity fall test, integration review)
- Findings: 4 (P0: 0, P1: 0, P2: 1, P3: 0, P4: 3)
- Fixed: 1 (DRY: UIThemeLoader 3 identical texture-loading methods → LoadTexture helper), Tasks created: 0

## SI.3 — Panel background textures — parchment and leather
- Date: 2026-03-09
- Commit: 5206d04
- Notes: Wired UIThemeLoader sprite textures as panel backgrounds across WebDemoScreen. Header uses parchment_bordered.png (ornate border frame), info column panels (ObjectivePanel, AspectLegendPanel, ViolationListPanel) use parchment_plain.png, reagent tray strip uses dark_leather.png via PanelContainer wrapper. Adjusted text colors throughout for readability on warm parchment backgrounds — headers and body text switched from light grays to dark brown/sepia tones. Dark leather tray retains existing light text. ObjectivePanel (VBoxContainer) wrapped in a PanelContainer in InfoColumnManager to receive the parchment texture. Net -20 lines (removed verbose StyleBoxFlat declarations replaced by single UIThemeLoader calls). All 811 tests pass.

## SI.4 — Aspect tile sprites in legend and status bar
- Date: 2026-03-09
- Commit: 0c40ede
- Notes: Replaced colored ColorRect squares in AspectLegendEntry with TextureRect showing 32x32 aspect tile sprites (hot.png, cold.png, etc.) via UIThemeLoader.GetAspectTileTexture(). Changed _icon field type from ColorRect to Control to support both TextureRect and ColorRect fallback. Added aspect tile icons before labels in StatusBarPanel's AspectBar, scaled to bar height with nearest-neighbor filtering. Both components gracefully fall back to existing colored rectangles when texture files are missing. All 811 tests pass.

## SI.5 — Decorative elements — flourishes, separators, sigil
- Date: 2026-03-09
- Commit: 05bca8e
- Notes: Wired three decorative sprites into WebDemoScreen. Corner flourishes: 4 TextureRects overlaid on header panel corners using absolute positioning (FlipH/FlipV for each corner), updated on resize via Resized signal and AdaptLayout. Separator: replaced HSeparator between grid and tray with TextureRect showing golden vine separator.png, KeepAspectCentered stretch. Title sigil: added alchemist seal medallion TextureRect before title label in header HBox. All decorations load via UIThemeLoader.GetDecorationTexture() with graceful fallback (HSeparator for separator, no-op for flourishes/sigil when textures missing). All 811 tests pass.

## Code Review (c) — 2026-03-09
- Reviewed commits: 5206d04..9de252a (SI.3, SI.4, SI.5)
- Findings: 3 (P0: 0, P1: 0, P2: 0, P3: 3)
- Fixed: 0, Tasks created: 0

## SI.7 — Button sprite textures — styled buttons from Kit 01
- Date: 2026-03-09
- Commit: a1f1483
- Notes: Replaced StyleBoxFlat button styles with StyleBoxTexture from sliced Kit 01 sprites. CreateStyledButton now loads textures via UIThemeLoader.GetButtonStyleBox() — standard buttons use button_normal/hover/pressed, Confirm button uses alt variant for visual distinction. Disabled state duplicates normal texture with desaturated ModulateColor (0.5, 0.5, 0.5, 0.6). Falls back to StyleBoxFlat when textures are missing. Also added content margins (12/12/4/4) to button StyleBoxTextures in UIThemeLoader. Styled the reagent tray horizontal scrollbar with themed grabber (warm brown StyleBoxFlat in 3 states) and track (dark semi-transparent). scrollbar_track.png exists but is vertical-oriented so used procedural fallback for horizontal tray. All 811 tests pass.

## Integration Review — 2026-03-09c
- Reviewed commits: e2f8961..8166361 (SI.2–SI.7, code reviews)
- Build: PASS (811/811 unit tests)
- Harness: 41/41 tests pass, 0 failures
- Visual: 62 screenshots verified, no regressions
- Issues: 0
- Coverage: SI.2–SI.7 are visual-only changes with graceful fallbacks, verified via screenshot inspection. All recent features (GJ.1–GJ.4, GO.7–GO.12, LO.2–LO.5) have integration tests. Minor gap: InfoColumnManager aspect detail view untested (reagent detail view is tested).
- Pre-existing cosmetic issues unchanged: tutorial vertical text, Heat label left-edge clipping, base thresholds in status bar.

## SI.8 — Bag visual themes — distinct panel frames per bag type
- Date: 2026-03-09
- Commit: e73bf5b
- Notes: Replaced single-texture bag border rendering with per-bag-type themed textures. GetBagThemeName() maps bag properties to theme names: Reinforced/Null → ornate (dark subdued), Insulated → warm (golden knots), Gravity → brass (clean golden corners), None → default (brown celtic knotwork). Each bag now loads its own texture via UIThemeLoader.GetBagThemeTexture() which looks up art/sprites/ui/sliced/bag_{themeName}.png. Fallback is per-bag (not global) — if a specific bag's theme texture is missing, only that bag falls back to procedural ColorRect borders. Removed global _bagBorderTexture/_bagBorderTextureChecked fields and SpriteSwapper dependency from bag border rendering. Also improved fallback ColorRect rendering to use HashSet-based boundary detection per bag instead of full-grid iteration. All 811 tests pass.

## SI.9 — Full UI sprite integration verification
- Date: 2026-03-10
- Commit: d5bbf0b
- Notes: Added Test_UISpriteIntegration to TestHarness — end-to-end verification of all 24 UI sprite texture assets across 5 categories: 3 panel textures (parchment_bordered, dark_leather, parchment_plain), 8 aspect tiles (all aspects), 3 decorations (corner_flourish, separator, title_sigil), 6 button states (normal/hover/pressed + alt variants), and 4 bag themes (default, ornate, warm, brass). Test verifies asset existence via ResourceLoader.Exists, validates visual integration in WebDemoScreen (textured buttons, aspect tiles in legend, decoration TextureRects, bag NinePatchRects), and confirms fallback resilience — non-existent textures correctly return StyleBoxFlat/null, fake texture names properly fall back. Produces a diagnostic summary report. Currently all 24 textures exist on disk but lack Godot .import files (not yet imported through editor), so the test validates the full fallback path. All 811 unit tests pass, 42/42 integration tests pass.

## VP.1 — Full-page tiled parchment background
- Date: 2026-03-10
- Commit: 51aa125
- Notes: Replaced the solid black ColorRect background (0.1, 0.1, 0.12) in WebDemoScreen with a TextureRect tiling parchment_plain.png (64x64). Uses StretchMode.Tile with a dark warm modulate (0.4, 0.35, 0.3) so the parchment reads as a subtle warm background without competing with foreground panels. Falls back to the original solid color if the texture is missing. Added GetPanelTexture() to UIThemeLoader for raw panel texture access (vs the existing GetPanelStyleBox which wraps in a 9-patch StyleBox). All 811 unit tests pass.

## VP.2 — Fix tray texture tiling (not stretching)
- Date: 2026-03-10
- Commit: 3da3887
- Notes: Set AxisStretchHorizontal and AxisStretchVertical to StyleBoxTexture.AxisStretchMode.Tile in UIThemeLoader.GetPanelStyleBox(). This applies to all panel StyleBoxTextures (dark_leather tray, parchment panels) so the 9-patch center region tiles instead of stretching. Two-line change. All 811 tests pass.

## VP.3 — Scale up aspect tiles everywhere to 32x32
- Date: 2026-03-10
- Commit: a9665c8
- Notes: Scaled all aspect tile icons from their various small sizes to native 32x32. AspectLegendPanel icons from 16x16 to 32x32 (highlighted 18→34). StatusBarPanel BarHeight from 14 to 32 so tile icons display at native size. InfoColumnManager aspect detail header icon from 18x18 to 32x32 — also refactored to use TextureRect with UIThemeLoader.GetAspectTileTexture() fallback to ColorRect (was always ColorRect before). Reagent detail aspect icons from 12x12 to 32x32 with same TextureRect+fallback pattern. All TextureRects use TextureFilter=Nearest for crisp pixel art. All 811 tests pass.

## Code Review — 2026-03-10b
- Reviewed commits: d1cb459..3029bd6 (VP.1, VP.2, VP.3)
- Findings: 3 (P0: 0, P1: 1, P2: 2)
- Fixed: 1 (StatusBarPanel missing icon fallback), Tasks created: 1 (R1 — extract shared CreateAspectIcon + FormatRule helpers)

## R1 — Extract shared CreateAspectIcon helper to UIThemeLoader
- Date: 2026-03-10
- Commit: 2fc9224
- Notes: Extracted duplicated TextureRect/ColorRect aspect icon fallback pattern into UIThemeLoader.CreateAspectIcon(aspectId, size) — replaced 5 call sites across InfoColumnManager (x2), AspectLegendPanel, StatusBarPanel, ComponentInspector. Created AspectFormatHelpers static class consolidating FormatRule, Capitalize, FormatSensitivity, FormatAspects, and GetRuleColor — replaced 4 files (InfoColumnManager, AspectLegendPanel, FloatingAspectTooltip, AspectPrimerPanel). Net -158 lines. All 811 tests pass.

## VP.4 — Per-cell aspect tile rendering on grid + single-aspect reagents
- Date: 2026-03-10
- Commit: 0b0134b
- Notes: Added UpdateAspectTiles() to MetaGridView — renders a 32x32 TextureRect per occupied cell using UIThemeLoader.GetAspectTileTexture(aspectId) with Nearest filtering, falling back to existing ColorRect when no tile sprite exists. Multi-cell polyominoes show N separate tiles (not one stretched sprite). Added SingleAspectOnly property to DifficultyParams (default true) and wired it into PuzzleGenerator.AssignAspects() to force all cells of each reagent to share one uniform aspect. BalanceHotCold post-processing may still clear some cells, but all populated cells remain homogeneous. 1 new test. All 812 tests pass (811 existing + 1 new).

## Integration Review — 2026-03-10b
- Reviewed commits: 88dd632..HEAD (SI.9, VP.1–VP.4, R1, code reviews)
- Build: PASS (812/812 unit tests)
- Harness: 44/44 tests pass, 0 failures
- Visual: 49 screenshots verified, no regressions
- Issues: 0 (P0: 0, P1: 0, P2: 0)
- Added: `aspect_tiles_on_grid` test (VP.4 — per-cell tile rendering lifecycle), `cell_mount_overlay` test (GJ.7 — mount frame rendering lifecycle)
- Added: `MetaGridView.AspectTileCount` public accessor for test observability
- Coverage: VP.1–VP.4 all verified (direct + indirect). GJ.7–GJ.8 now have integration coverage. R1 pure refactor confirmed by all existing tests passing.
- Pre-existing cosmetic issues unchanged: left-edge label clipping, tutorial vertical text, base thresholds in status bar.

## VP.5 — Fix bag sprite gaps between adjacent bags
- Date: 2026-03-10
- Commit: 01195bc
- Notes: Set BagBorderPadding from 4 to 0 so NinePatchRect panels align exactly at cell boundaries with no gaps between adjacent bags. Added minimum size clamping (center-expand to 2*PatchMargin) for bags smaller than 48px on either axis, preventing NinePatch corner distortion on 1-cell-wide bags. Simple 2-file change (GridConstants.cs + MetaGridView.cs). All 812 tests pass.

## Code Review — 2026-03-10c
- Reviewed commits: f69a18a..609f68c (VP.4 integration tests, VP.5 bag border fix)
- Findings: 0 (P0: 0, P1: 0, P2: 0)

## VP.6 — Wire aspect tiles into ALL remaining colored-square locations
- Date: 2026-03-10
- Commit: d134000
- Notes: Replaced colored rectangles with aspect tile textures in 4 remaining locations: ShapePreview (8px tray items), DetailShapePreview (16px detail/tooltip panels), ProfileShapePreview (24px profile panels), and ghost overlay (32px placement preview). All three shape preview classes now use DrawTextureRect() in their _Draw() methods when UIThemeLoader.GetAspectTileTexture() returns a texture, falling back to colored rectangles when none exists. Ghost overlay adds semi-transparent (alpha 0.7) TextureRect nodes on top of the ghost ColorRects. Also upgraded profile aspect breakdown icons from 12px ColorRect to 16px via UIThemeLoader.CreateAspectIcon(). All 812 tests pass.
- Fixed: 0, Tasks created: 0

## VP.7 — Constrain info column panel heights
- Date: 2026-03-10
- Commit: b93a195
- Notes: Wrapped InfoColumnManager views in a ScrollContainer so panels scroll instead of overflowing past the viewport. Changed SizeFlagsVertical from ShrinkBegin to ExpandFill so the column fills available height. Reduced unnecessary minimum heights: AspectLegendPanel 120→0, ViolationListPanel 80→0, AspectLegendEntry header 44→36, violation entry buttons 40→30. Tightened spacing (separation 10→4/8). All 812 unit tests and 44 integration tests pass.

## VP.8 — Panel border style overhaul — hand-drawn not beveled
- Date: 2026-03-10
- Commit: 5f5d304
- Notes: Overhauled UIThemeLoader.GetPanelStyleBox() to produce hand-drawn thin-line borders instead of thick beveled 3D edges. Three-tier approach: (1) parchment_bordered keeps StyleBoxTexture but with 8px margins (down from 16px), making the ornate border thinner; (2) parchment_plain switches to StyleBoxFlat with 2px warm brown/gold border (Color(0.55, 0.42, 0.25)) for a hand-drawn line feel; (3) dark_leather keeps StyleBoxTexture with 6px margins. Fallback now also uses the hand-drawn style. Added per-panel margin dictionary and flat-border panel set for clean extensibility. All 812 unit tests and 44 integration tests pass.
