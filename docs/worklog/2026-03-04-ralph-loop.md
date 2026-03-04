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
