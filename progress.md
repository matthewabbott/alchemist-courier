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
