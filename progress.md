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
