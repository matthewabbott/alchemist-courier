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

## Code Review (d)
Reviewed R-6 and R-7 changes. Both are clean refactoring — no P0/P1/P2 issues.
R-7's HasAspect method is well-placed, general-purpose, and properly tested.
R-6 string literal replacement is thorough across Core/ and Game/ (one P3 note:
TestHarness still uses raw strings in GetAspectTotal calls, acceptable for test code).
Verified no remaining duplicated aspect-check patterns — ChemistryEngine's
ReagentHasUnsuppressedAspect is intentionally distinct (grid/Null-bag-aware).

## A.1: Volatile net-hot calculation
The most meaningful chemistry rework yet — transformed Volatile from a simple pair-trigger
("Hot adjacent? Explode.") into a nuanced net-hot calculation where each Volatile cell sums
its neighbors' Hot magnitudes minus Cold magnitudes. This makes Cold strategically interesting:
it's no longer just a cumulative liability, but a tool players can deliberately place as a heat
shield between Hot and Volatile reagents. The implementation lives in a new
EvaluateVolatileNetHotRule method dispatched when an Adjacent rule has a single trigger aspect.
All 21 existing explosion-related tests pass unchanged because the basic behavior (Hot next to
Volatile = explosion) is preserved — the new dimension is that Cold can now prevent it.
Commit: ebe0fe1

## A.2: Sharp edge placement constraint
Added Sharp's primary non-parasitic mechanic — Sharp reagents can't sit on edge cells of
non-Reinforced bags. An "edge cell" is any bag cell with a cardinal neighbor outside the bag
shape. This makes Sharp interesting on its own: in a 3x3 bag only the center cell is interior,
meaning Sharp reagents need large bags or special shapes to have placement options. Reinforced
bags waive this constraint, creating a natural synergy. The PuzzleGenerator now prioritizes
Sharp reagents in placement order (after Heavy, before size-sorting) since interior cells are
scarce. Non-rectangular bag shapes interact nicely — an L-shaped bag has zero interior cells,
while a plus-shaped bag has exactly one.
Commit: c00e323

## A.3: Implement Fragile universal vulnerability
Fragile's core design philosophy is "universally vulnerable" — it breaks from ANY adjacent
reaction, not just the Sharp+Fragile pair. This makes Fragile interesting on its own: placing
a Fragile reagent near any potential reaction site is risky, even if there's no Sharp in the
puzzle. Implemented as a second pass in ChemistryEngine.Evaluate: after all standard reaction
rules fire, we collect every affected cell and check if any Fragile reagent is adjacent. The
dedup logic skips Fragile reagents already broken by the Sharp+Fragile pair rule to avoid
double-counting. Also added a Gravity bag placement constraint — Fragile things can't survive
the falling/stacking environment. Both mechanics reinforce Fragile's identity as a high-risk,
high-reward aspect (once value scoring arrives).
Commit: c7d689b

## A.4: Add Catalytic aspect to ChemistryEngine
Catalytic is the strategic mirror of Stable — where Stable raises effective thresholds to
suppress reactions, Catalytic lowers them to make reactions easier to trigger. The core formula
is `effectiveThreshold = max(0, threshold + stableBuffer - catalyticReduction)`, applied
consistently across all three sensitivity tiers. Refactored the existing
`GetAdjacentStableMagnitude` into a generic `GetAdjacentAspectMagnitude` parameterized by
aspect ID, eliminating the need for a duplicate method. An interesting edge case: Catalytic
can't reduce thresholds below 0, so pair reactions (Sharp+Fragile, threshold=0) fire regardless
of nearby Catalytic — but Catalytic CAN counter Stable suppression of those same pairs.
Commit: a69a5fa

## A.5: Add Medicinal passive threshold reduction
Medicinal completes the trio of threshold modifiers alongside Stable and Catalytic, but with
a fundamentally different design: it's passive and global rather than adjacency-based. Carrying
Medicinal reagents anywhere in your inventory raises all cumulative thresholds (Overheating,
Hypothermia, Poisoning) by the total Medicinal magnitude. This means Medicinal doesn't need
careful spatial positioning — its strategic value comes from carrying it at all, trading bag
space for safety margin. Added a GetTotalAspectMagnitude helper that sums across the entire
inventory (respecting Null bag suppression). Importantly, Medicinal only affects WholeInventory
rules — it does NOT suppress Adjacent or SameBag reactions like explosions or neutralizations.
Commit: 5734bb9

## A.6: Add ComplexityLevel enum and wire to PuzzleGenerator
Implemented the two-dimensional complexity model from docs/chemistry-system.md. The key insight:
puzzle difficulty has two independent axes — scale (how many bags/reagents) and mechanical depth
(how many unique aspect interactions). Daily/standalone puzzles should always use Simple mechanical
depth, varying only scale. ComplexityLevel enum (Simple/Intermediate/Advanced) controls which
aspects, bag types, and reaction rules are available. ComplexityFilter centralizes the filtering
logic — Simple gets 6 aspects and 4 bag types, Intermediate adds Sharp/Fragile/Catalytic/Medicinal
plus Reinforced/Insulated/Fragile bags, Advanced adds Magnetic/Absorbent and Levitating bags.
PuzzleGenerator now draws from ComplexityFilter instead of a hardcoded MvpAspects array, meaning
future roguelite mode just needs to set ComplexityLevel.Intermediate on DifficultyParams. An
interesting consequence: Simple tier excludes the Breakage rule (Sharp+Fragile are Intermediate),
so Simple puzzles have 6 reaction rules instead of 7.
Commit: 3bcb216

## Code Review (e)
Reviewed A.6 (ComplexityLevel + ComplexityFilter). Found and fixed two P1 issues:
circular namespace dependency from ReactionRuleDatabase.GetRulesForComplexity() (Chemistry
importing Puzzle, creating a cycle) — removed the dead pass-through method; and magic string
literals "magnetic"/"absorbent" in ComplexityFilter.AdvancedAspects — added AspectIds.Magnetic
and AspectIds.Absorbent constants. Net result: -25 lines, cleaner dependency graph, all 289
tests pass.
Commit: 1b0c26c

## 3.5: Add aspect interaction primer
Added a toggleable AspectPrimerPanel overlay that lists all reaction rules from the
ReactionRuleDatabase. Each rule displays as a color-coded one-liner showing trigger aspects,
sensitivity tier (adjacent/same bag/cumulative), optional threshold, and result name — making
the chemistry system discoverable without memorization. Implemented as a PanelContainer in the
right column, toggled via a "Reactions" button alongside Reset/Confirm. The panel is the final
UI-chemistry piece needed before the layout polish pass (5.1). Integration test verifies
visibility toggle and correct rule count (7 MVP rules).
Commit: 29a0bc4

## 4.2: Replace DemoPuzzle with PuzzleGenerator
The bridge between the Core puzzle infrastructure and the UI is finally in place.
PuzzleScreen now accepts a PuzzleState via a property set before _Ready, using the
existing PuzzleSolver reconstruction methods to hydrate runtime objects from the
serialized format. The interesting part was handling locked reagents: they need to be
pre-placed on the grid during initialization, but ResetPuzzle was using a while-loop
that assumed all reagents could be removed — locked reagents caused an infinite loop
since MetaGrid.RemoveReagent silently rejects them. Switched to reverse iteration
with a skip check. The integration test creates a second PuzzleScreen mid-harness
(QueueFree the old one, create new with PuzzleData), applies the generator's known
solution, and confirms it wins — proving the full pipeline works end-to-end.
Commit: a28eed1

## Architecture Review (d)
Full architecture review at the phase boundary (all pre-game-flow phases complete). Verdict:
GREEN — proceed to game-flow. Key findings: StatusBarPanel doesn't apply Null-bag suppression
(functional bug, fix before/during 4.1), MEMORY.md has stale metadata (Godot 4.3/net6.0/.csproj
gitignored — all wrong), Grid→Chemistry dependency in MetaGrid for placement constraints
(workable for MVP, refactor before Intermediate tier), and several pieces of dead code from
iterative development. No priority-0 tasks created — all issues are low/medium severity.
Report: docs/reviews/architecture-review-2026-03-04d.md

## 4.1: Add win/loss evaluation to PuzzleScreen
First game-flow phase task. Wired the Confirm button to ChemistryEngine.Evaluate — when all
reagents are placed, the engine runs all MVP reaction rules against the grid. Zero reactions
means the player wins (green "SUCCESS" overlay); any reactions means loss (red "FAILED" overlay
listing each reaction type and affected cells). The result overlay is a styled PanelContainer
with semi-transparent dark background and colored header, built procedurally in BuildUI().

The integration test was the tricky part — needed to find two complete 6-reagent arrangements
for the DemoPuzzle that (a) fit all reagents within bag bounds, (b) respect all placement
constraints (Sharp needs Reinforced bag, Gravity, etc.), (c) LOSS path triggers exactly one
Explosion via Hot adjacent to Volatile, and (d) WIN path avoids ALL reactions including
cumulative thresholds. The winning arrangement carefully positions Fire Pebble (Hot=2) at
(4,2) for loss (adjacent to Blast Jelly's Volatile cells) vs (4,3) for win (safe distance).
Manual chemistry verification confirmed cumulative totals stay under effective thresholds
(Hot 3 < 5, Cold 2 < 4, Toxic 3 < 4 with Medicinal buffering).

Learned: PlacedPosition on Reagent is the anchor (top-left of bounding box), which for rotated
shapes may be a `false` cell in the shape matrix. Using GetReagentAtCell on the anchor can
return null, causing infinite loops in cleanup code. Use ResetPuzzle() instead of manual removal.
Commit: ded67ea

## Code Review (f)
Reviewed last 3 commits (3.5 aspect primer, 4.1 win/loss evaluation, 4.2 PuzzleGenerator wiring).
Found and fixed one P1 DRY violation: selection-clearing logic (set null, reset index, clear grid/tray)
was copy-pasted in 5 locations across PuzzleScreen. Extracted `ClearSelectionState()` private method,
net -15 lines. Remaining findings (P3-P4: hardcoded UI values, repeated GetMvpRules calls, test cleanup
patterns) are acceptable for current stage. Code quality is solid — the game-flow additions follow
existing patterns cleanly.
Commit: 494309a

## 4.3: Add tutorial lesson data model and first 5 lessons
Created the data layer for the tutorial system. TutorialLesson is a simple data model
capturing everything needed to generate and present a tutorial puzzle: lesson number,
concept name, player-facing hint text, a fixed seed for deterministic generation,
difficulty parameters, and puzzle type. TutorialData.GetLessons() returns the first 5
lessons from the MVP spec, following a pedagogical progression: three pure spatial lessons
(Basic Placement, Shapes & Rotation, Tight Fit) that teach grid mechanics without chemistry,
then two aspect-introduction lessons (Hot and Volatile) that layer in chemistry one concept
at a time. Lesson 4 intentionally steps back to fewer reagents (3 vs 4) so the player can
focus on the new concept without spatial overwhelm. All seeds validated as producing solvable
puzzles via PuzzleGenerator + PuzzleSolver.IsSolvable.
Commit: 9e6a035

## 4.4: Add tutorial flow screen
Created TutorialScreen as a wrapper around PuzzleScreen that manages the 5-lesson tutorial
sequence. Each lesson generates its puzzle from TutorialLesson params, displays lesson name
and hint text in a styled panel at the top, and reveals a "Next Lesson" button on win. The
key design decision was adding a PuzzleConfirmed event to PuzzleScreen rather than polling
or modifying PuzzleScreen's internals—TutorialScreen subscribes to the event and reacts to
wins. AdvanceLesson() tears down the current PuzzleScreen and builds a fresh one from the
next lesson's data. Integration test applies lesson 1's solution programmatically, confirms
win, advances, and verifies lesson 2 loads with different hint text and a new PuzzleScreen.
Commit: 9a993b6

## Code Review (g)
Reviewed tasks 4.3–4.5 (TutorialLesson/TutorialData, TutorialScreen, DailyPuzzleManager, PuzzleConfirmed event).
All code is clean — no P0/P1/P2 issues. Core/Game boundary respected, event wiring is correct, tests are thorough (14 new unit + 1 integration). Report: docs/reviews/code-review-2026-03-04g.md

## 4.5: Add daily puzzle mode
Built the DailyPuzzleManager — a lightweight static class that converts dates into deterministic
puzzles. The seed is simply YYYYMMDD as an integer, difficulty rotates on a 3-day cycle using
day-of-year % 3 (Easy → Medium → Hard), and puzzle type varies by seed % 3 across the three types.
The critical design constraint: all daily puzzles use Simple mechanical depth regardless of difficulty
level, per the two-dimensional complexity model. A Hard daily is hard because it has 4 bags and 12
reagents with tight fit, not because it introduces Sharp+Fragile. GenerateToday() provides the
runtime convenience method. The whole thing is 77 lines of pure C# with no Godot dependencies.
Commit: e029128

## 4.6: Add save system
The final game-flow phase task: persistent save data. SaveData is intentionally minimal per the
architecture review's guidance — just tutorial progress (highest lesson completed), daily completion
history (YYYYMMDD date list), and streak tracking. The streak logic handles edge cases: month
boundaries work via DateTime arithmetic, duplicate completions are no-ops, and GetEffectiveStreak()
distinguishes a "live" streak (played today or yesterday) from a stale one. SaveManager provides
both file-based persistence (desktop) and static Serialize/Deserialize methods for future web
localStorage swap — no Godot dependency needed since System.Text.Json handles serialization and
System.IO handles file ops. The graceful degradation pattern (corrupt JSON → fresh SaveData) means
the game never crashes on a bad save file.
Commit: c4d78c9

## 5.1: UI layout polish to match MVP mockup
The final MVP task — polishing PuzzleScreen's procedural UI to match the ASCII mockup in the
specification. Replaced the plain centered title with a styled header bar: "Alchemist Courier" on the
left, with optional day count and streak labels on the right for daily puzzle mode. Added
HeaderDayText/HeaderStreakText properties that callers set before _Ready(). Styled the three action
buttons with distinct background colors (Confirm=green, Reset=warm gray, Reactions=indigo) and
hover/pressed states, making the primary action visually prominent. Moved the result overlay from the
VBox flow (which pushed content down when shown) to a centered absolute overlay. Tightened margins
and spacing for a more compact layout. All 16 integration tests pass — the layout change is purely
cosmetic with no behavioral impact.
Commit: 1de6cae
