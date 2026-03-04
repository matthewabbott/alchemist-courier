# Ralph Loop — 2026-03-04

## R-3: Implement Null bag aspect suppression in ChemistryEngine
The Null bag mechanic was defined in the BagPropertyType enum but never enforced—reagents
in Null bags had their aspects counted fully, defeating the purpose. Added per-cell
suppression that checks each cell's bag property before reading aspects, integrated across
all three ChemistryEngine sensitivity tiers (Adjacent, SameBag, WholeInventory) and the
Stable buffering helper. Spanning reagents get partial suppression: only cells physically
inside the Null bag lose their aspects, while cells in normal bags remain active. This is
architecturally simple (one new predicate + one helper method) but mechanically important
for tutorial lesson 10 and the strategic depth Null bags provide.
Commit: 18b9332

## R-4: Fix PuzzleGenerator.MvpAspects to match Simple complexity tier
The MvpAspects array included Sharp and Fragile (Intermediate-tier aspects) but was missing
Heavy (Simple-tier). This meant generated puzzles could include Sharp+Fragile breakage
interactions that belong to a higher complexity tier, while missing Heavy which is key to
the Gravity bag mechanic in B.3. One-line fix, no test changes needed—the puzzle tests are
deterministic but aspect-agnostic, and the Sharp+Fragile reaction rule tests exercise the
database directly.
Commit: f159dd3

## R-5: Normalize Stable buffering pattern across all ChemistryEngine tiers
ChemistryEngine had two different Stable buffering patterns: Adjacent and SameBag
distinct-pair tiers used a binary skip (`stableBuffer > rule.Threshold`), while
WholeInventory and SameBag same-aspect used the correct additive pattern
(`effectiveThreshold = rule.Threshold + stableBuffer`). Normalized all tiers to
compute `effectiveThreshold` consistently. For current threshold=0 pair rules the
behavior is identical, but the additive pattern is correct for future non-zero
thresholds and makes the code intent clearer across all three evaluation paths.
Added 6 boundary tests verifying exact-magnitude behavior at threshold edges.
Commit: 3e35cf1

## Code Review (b)
Reviewed R-3 through R-5 (Null bag suppression, MvpAspects fix, Stable normalization).
Found P1 bug: R-5's "normalization" of pair-path Stable buffering introduced a degenerate
comparison (`effectiveThreshold > rule.Threshold` is always `stableBuffer > 0`). Reverted
pair paths to the original clear form `stableBuffer > rule.Threshold`. Also removed dead
`ReagentHasAspect` method left behind by R-3. All 182 tests pass.
Commit: 7bfa0b4

## B.1: Implement Gravity bag type
First bag-mechanics phase task. Added gravity placement validation to MetaGrid—in Gravity
bags, reagents must be supported from below (resting on the bag bottom or stacked on other
reagents). The interesting design choice was handling multi-cell reagents that self-support:
a vertical domino can support its own top cell via its bottom cell. Used lazy HashSet
allocation to avoid overhead when no Gravity bags are present. Also wired bag properties
through the full serialization pipeline (PuzzleGenerator → PuzzleBag.Properties → PuzzleSolver)
so Gravity constraints are enforced consistently during generation and validation. The generator
assigns ~30% of bags as Gravity when AllowSpecialBags is true.
Commit: 9ee9109

## B.2: Implement locked reagent placements
Locked reagents are pre-placed constraints the player must work around — the first mechanic
that makes puzzles feel hand-crafted even though they're generated. Key design decision: locked
is a per-reagent flag, not a bag property, since any bag can contain locked reagents. The
implementation touches four layers: Reagent (IsLocked property), MetaGrid (removal prevention),
PuzzleGenerator (randomly locks 1-2 solution reagents when AllowSpecialBags=true), and PuzzleSolver
(validates locked positions match the solution exactly). The generator deliberately never locks all
reagents and requires at least 3 reagents before locking any, ensuring the player always has
meaningful choices. PuzzleReagent.IsLocked was already in PuzzleState.cs from task 2.2 — nice
foresight that made serialization zero-effort.
Commit: a0039c7

## 05:45 — B.3: Heavy aspect placement constraint in Gravity bags
Gravity bags already enforced "no floating" from B.1, but Heavy reagents needed an additional
constraint: they must always sink to the bottom. The implementation adds a column-wise check in
MetaGrid — when placing a Heavy reagent, it scans below for non-Heavy occupants (rejects if found),
and when placing a non-Heavy reagent, it scans above for Heavy occupants (rejects if found). The
PuzzleGenerator now sorts Heavy reagents first in the placement order so they naturally land at the
bottom of Gravity bags before non-Heavy reagents stack on top. Clean separation: the constraint lives
entirely in MetaGrid validation, while the generator just ensures placement order.
Commit: 9affe1d

## Code Review (c)
Reviewed B.2 (locked reagents) and B.3 (Heavy placement constraint). Code is clean — no P0/P1/P2
issues found. Three P3 observations: duplicated HasHeavyAspect helper between MetaGrid and
PuzzleGenerator (only 2 occurrences, Rule of Three says don't abstract yet), magic string "heavy"
(consistent with existing codebase patterns), and MetaGrid gaining aspect-specific knowledge
(acceptable for MVP, flag if more aspect-placement rules emerge). Positive notes: good constraint
separation, bidirectional checking, thorough tests, Heavy-first sort strategy.

## B.4: Non-rectangular bag shapes
Until now every generated bag was a plain rectangle — functional but visually monotonous and
strategically flat. Added a pool of 8 non-rectangular templates (L, T, hourglass, S-step, plus,
U, notched rectangle, chunky L) that the generator draws from at higher difficulties (~40% per
bag when AllowSpecialBags is true). The elegant part: zero changes to MetaGrid's placement
validation, because it already operates on per-cell bag ownership rather than bounding boxes.
Non-rectangular bags create more interesting packing puzzles and interact naturally with Gravity
constraints from B.1 — an L-shaped Gravity bag forces very specific stacking patterns.
Commit: 7487949

## R-6: Extract aspect ID string literals to AspectIds constants class
Addressed architecture review finding M4: aspect IDs like "hot", "cold", "volatile" were scattered
as magic strings across 7 source files. Created a single AspectIds constants class with 10 const
fields, then mechanically replaced every occurrence. A small change with outsized discoverability
benefit — any future aspect ID rename is now a single-point change, and typos are caught at compile
time instead of silently mismatching at runtime.
Commit: de4a45b

## R-7: Deduplicate ReagentHasHeavyAspect into Reagent.HasAspect
MetaGrid and PuzzleGenerator both had identical private static methods checking if a reagent has
the Heavy aspect. Extracted a generalized `HasAspect(string aspectId)` instance method on Reagent
itself — any aspect can now be queried without knowing the internal AspectsPerCell structure. This
also sets up cleanly for future aspect-based queries (e.g., A.2 Sharp edge constraints will need
`reagent.HasAspect(AspectIds.Sharp)`).
Commit: 8e82a2e
