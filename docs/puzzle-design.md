# Puzzle Design — Types, Scoring, and Progression

Comprehensive reference for puzzle types across all development tiers. This document covers the design philosophy, current implementations, post-MVP optimization types, scoring mechanics, and aspirational puzzle modes.

For the complexity model (Simple/Intermediate/Advanced), see `docs/chemistry-system.md` lines 16-59.
For the development roadmap and tier definitions, see `docs/roadmap.md`.
For the procedural generation algorithm, see `docs/mvp-specification.md` lines 92-122.

---

## 1. Design Philosophy

The puzzle is a **constraint satisfaction problem**: given a set of bags and reagents, find placements that satisfy spatial, chemical, and mechanical constraints. This creates the core tension — the player knows a solution exists (the generator guarantees it) but must discover how to navigate the constraint space.

### Binary Pass/Fail Is the Sudoku Model

FitAllSafely (the MVP puzzle type) uses binary pass/fail: either all reagents are placed safely or they aren't. This is the Sudoku model — there's a correct arrangement, and the satisfaction comes from finding it. No partial credit, no optimization, just "did you solve it?"

This model works well for **Simple** mechanical depth because the constraint set is small enough to hold in your head (6 aspects, 4 bag types). The player can reason deductively: "Volatile can't go next to Hot, and there's only one spot left that avoids adjacency, so it must go there."

### Optimization Adds Depth for Higher Tiers

Post-MVP puzzle types (BalanceToNeutral, MinimizeSeverity, MaximizeWithConstraints) introduce optimization. Instead of a binary answer, there's a spectrum of solutions from "passable" to "optimal." This works best with **Intermediate+** mechanical depth where the larger constraint space makes finding the global optimum non-trivial.

### Secondary Scoring: Time + Hints, NEVER Move Count

Scoring is secondary to completion. The 3-star system rewards efficiency:
- **Star 1**: Completion (solved the puzzle)
- **Star 2**: Efficiency (0 hints used)
- **Star 3**: Speed (under par time)

**Move count is explicitly excluded from scoring.** Placing and removing reagents is the core interaction — the player should freely experiment with arrangements without feeling penalized. Picking up a reagent to try it somewhere else is exploration, not failure.

### Never Punish Experimentation

The game follows the cozy design pillar from `docs/game-overview.md`: failure is a setback, not a punishment. Specific implications:

- **Reset is free and instant.** No "are you sure?" confirmation, no penalty.
- **Timer pauses** when the aspect primer/legend is open and during hint animations. Reading the rules shouldn't compete with speed.
- **Hints reduce Star 2 but don't prevent completion.** A player who uses all hints and solves the puzzle still gets Star 1.
- **Concede is always available.** The player can give up on a puzzle without shame. The UI should frame this neutrally ("Try again tomorrow" for daily puzzles).

---

## 2. Tier Definitions

Puzzle types are organized into the same four tiers used across the project (see `docs/roadmap.md`):

| Tier | Scope | Puzzle Types |
|------|-------|-------------|
| **1: MVP** | Currently shipping | FitAllSafely, MaximizeValue (enum only), MeetRequirement (enum only) |
| **2: Post-MVP** | Concrete designs, buildable next | BalanceToNeutral, MinimizeSeverity, MaximizeWithConstraints, TimedChallenge |
| **3: Aspirational** | Requires new infrastructure (same tier as item protrusion) | Destruction, Transmutation, Forced Explosion |
| **4: DLC / Expansion** | New game modes on top of puzzle engine | Alembic/Brewing, multi-step potion crafting |

---

## 3. Current Puzzle Types (MVP)

These types exist in the `PuzzleType` enum (`src/Core/Puzzle/PuzzleType.cs`). FitAllSafely is fully implemented with win/loss evaluation. The other two have enum variants but lack evaluation logic.

### FitAllSafely (Implemented)

- **Given**: A set of bags and a set of reagents.
- **Goal**: Place ALL reagents into the bags without triggering any negative reactions.
- **Win condition**: All reagents placed, `ChemistryEngine.Evaluate()` returns zero negative `ReactionResult` entries.
- **Fail condition**: None enforced — the player can always reset and try again, or concede.
- **Scoring**: Binary pass/fail. Secondary: time and hints used (for 3-star system when implemented).
- **Mechanical depth**: Simple (6 aspects, 4 bag types) for all standalone/daily puzzles.
- **This is the Sudoku model.** A correct solution exists; the player must find it.

### MaximizeValue (Enum Exists, Not Evaluated)

- **Given**: A set of bags and MORE reagents than can fit.
- **Goal**: Place the highest total value of reagents without negative reactions.
- **Scoring**: Sum of placed reagent values. Any valid (reaction-free) arrangement scores something.
- **No fail condition**: The player always achieves some score.
- **Implementation notes**: Requires `Reagent.Value` to be meaningful (currently set by generator but not displayed or evaluated). Needs a score display in the UI and a "best score" tracking system in SaveData.

### MeetRequirement (Enum Exists, Not Evaluated)

- **Given**: A set of bags and reagents, plus a specific requirement (e.g., "include at least 3 hot-aspected reagents").
- **Goal**: Meet the requirement without triggering negative reactions.
- **Win condition**: Requirement met AND zero negative reactions.
- **Complexity**: The requirement forces the player to include dangerous reagents, creating interesting constraint interactions. "You need 3 Hot reagents, but you also have Volatile — can you arrange them safely?"
- **Implementation notes**: Needs a `PuzzleRequirement` data model (aspect type, minimum count, etc.) and a requirement evaluator. The generator must produce puzzles where the requirement is satisfiable.

---

## 4. Post-MVP Optimization Puzzle Types

These types go beyond binary pass/fail, introducing a scoring spectrum. They are designed for the puzzle progression mode (Stage 2 in `docs/roadmap.md`) where players tackle curated sequences rather than one-off daily puzzles.

All optimization types provide a **par value** — the generator-known optimal or target score. The 3-star system applies relative to par.

### BalanceToNeutral

- **Concept**: Target net-zero for a specific aspect pair (e.g., Hot vs Cold).
- **Scoring**: `abs(total_hot_magnitude - total_cold_magnitude)`. Lower is better. Par = 0 (perfect balance achievable).
- **Strategy**: The player must place both Hot and Cold reagents such that their cumulative magnitudes cancel out, while also respecting spatial constraints (Volatile adjacency, bag properties, etc.).
- **Why it's interesting**: Forces the player to think about magnitude math, not just binary presence/absence. A 3-magnitude Hot reagent needs exactly 3 magnitudes of Cold to balance.

### MinimizeSeverity

- **Concept**: All reagents MUST be placed. Some reactions are unavoidable. Minimize total severity.
- **Scoring**: Sum of reaction severities (Explosion = 10, Breakage = 5, etc.). Lower is better. Par = minimum achievable severity (generator computes this from the known solution).
- **Strategy**: The player triages — which reactions are cheapest to trigger? Can Stable buffering eliminate one reaction to focus severity elsewhere?
- **Why it's interesting**: Inverts the normal "avoid all reactions" goal. The player must reason about which reactions are least harmful and plan around them.

### MaximizeWithConstraints

- **Concept**: Oversized reagent set, limited bag space. Maximize total value of placed reagents.
- **Scoring**: Sum of placed reagent values. Higher is better. Par = generator's optimal value.
- **Strategy**: Spatial efficiency meets value optimization. A high-value reagent with an awkward shape might waste space that could hold two smaller reagents with higher combined value.
- **Why it's interesting**: Closest to the "real" inventory management problem from the roguelite vision. Every placement is a trade-off between value and spatial cost.

### TimedChallenge

- **Concept**: FitAllSafely with a visible countdown timer.
- **Scoring**: Binary completion + time remaining. Star thresholds based on speed.
- **Par times**: Easy = 90s, Medium = 150s, Hard = 240s.
- **No fail on timeout**: Timer reaching zero doesn't end the puzzle — the player can keep trying but won't earn the speed star.
- **Timer behavior**: Starts on first reagent selection. Pauses when the aspect legend/primer is open. Stops on confirm.
- **Why it's interesting**: Adds time pressure without changing the spatial puzzle. Tests pattern recognition speed and decisiveness.

---

## 5. Scoring & Stars

### 3-Star System

Every puzzle (across all types) uses a consistent 3-star rating:

| Star | Condition | What It Measures |
|------|-----------|-----------------|
| Star 1 | Puzzle completed | Can you solve it at all? |
| Star 2 | 0 hints used | Can you solve it independently? |
| Star 3 | Under par time | Can you solve it quickly? |

Stars are cumulative — earning Star 3 requires also meeting Star 1 and Star 2 conditions. A player who uses hints but finishes fast gets only Star 1.

### Par Times

| Difficulty | Par Time |
|-----------|----------|
| Tutorial | No timer |
| Easy | 90 seconds |
| Medium | 150 seconds |
| Hard | 240 seconds |

### Timer Rules

- **Starts**: On first reagent selection (not on puzzle load — the player can study the board freely).
- **Pauses**: When the aspect legend panel is expanded, during hint animations, when the puzzle is backgrounded (tab switch in web).
- **Stops**: On successful confirm (win) or concede.
- **Display**: Visible countdown (or count-up with par marker) in the header bar. Non-intrusive — a small label, not a flashing alarm.

### Hint Budget Impact

- Using any hints disqualifies Star 2 for that puzzle attempt.
- Hint usage is tracked per daily puzzle in `SaveData.DailyHintsUsed`.
- Snap-back (re-hinting a previously hinted reagent) is free — it doesn't consume additional budget.
- The hint budget varies by difficulty: Tutorial = 3, Easy = 3, Medium = 2, Hard = 1.

### Optimization Scoring (Post-MVP)

For optimization puzzle types (BalanceToNeutral, MinimizeSeverity, MaximizeWithConstraints), the star system extends:

| Star | Condition |
|------|-----------|
| Star 1 | Valid solution submitted (meets minimum threshold) |
| Star 2 | Score within 90% of par (near-optimal) |
| Star 3 | Score equals par (optimal) AND under par time |

The par value is stored in PuzzleState alongside the solution. The generator computes it during puzzle creation.

---

## 6. Aspirational — Destruction & Transmutation

These puzzle types require reactions to happen **during gameplay**, not just at confirm time. This is a fundamental infrastructure change — the current `ChemistryEngine.Evaluate()` runs once on confirm. These types need real-time reaction execution, reagent removal/creation, and grid state recalculation after each reaction.

**Tier marker:** Same tier as item protrusion (`docs/roadmap.md` Tier 3). Requires new infrastructure that doesn't exist yet.

### Destruction Puzzles

Reactions remove reagent cells or entire reagents from the grid. The player deliberately triggers explosions to make room for remaining placements.

- **Flow**: Place reagent -> reaction triggers immediately -> affected cells/reagents are destroyed -> grid updates -> player continues placing.
- **Strategy**: Sacrifice cheap reagents to clear space for valuable ones. Use explosions to break apart large reagents blocking critical positions.
- **Infrastructure needed**: Real-time reaction execution on placement (not just on confirm), reagent destruction animation, grid cell damage/removal, chain reaction resolution (explosion destroys Fragile, which triggers breakage, which...).

### Transmutation Puzzles

Two reagents combine into a third with a new shape and new aspects. Requires a recipe database mapping input pairs to output reagents.

- **Flow**: Place two reagents such that their reactive cells are adjacent -> transmutation triggers -> both inputs are removed -> output reagent appears in their place (or in the tray).
- **Strategy**: Plan transmutation chains — reagent A + reagent B = reagent C, which then combines with reagent D. Spatial planning must account for both the inputs' positions and the output's shape.
- **Infrastructure needed**: Recipe database (input aspects/shapes -> output reagent definition), transmutation animation, output reagent shape/position calculation, recipe discovery system (codex integration).

### Forced Explosion (Win Condition IS the Reaction)

Inversion of FitAllSafely — the win condition is triggering a specific reaction.

- **Example**: "Trigger an explosion in bag 2" or "Create a toxic cloud using exactly these reagents."
- **Strategy**: Instead of avoiding reactions, the player must engineer them precisely. Stable and Catalytic become key tools for controlling exactly which reactions fire.
- **Infrastructure needed**: Goal-specific reaction targeting (which bag, which reaction type), "desired reaction" distinction from "undesired reaction" in ChemistryEngine, win condition evaluator that checks for specific reactions rather than absence of all reactions.

---

## 7. DLC / Expansion — Actual Alchemy

These features would be new game modes layered on the existing puzzle engine. They go beyond "place reagents in bags" into multi-step processing. The design pillar applies: "Errands for an Alchemist, not Actual Alchemist Simulator" — keep the cozy tone, don't simulate real chemistry.

**Tier marker:** Tier 4 (DLC/Expansion) in `docs/roadmap.md`. Requires new game modes on top of the puzzle engine.

### Alembic Container

A special bag type that processes reagents over time (turns). It has input slots and an output slot.

- **Visual**: A stylized alchemical vessel with labeled compartments (intake, reaction chamber, output spout).
- **Mechanic**: Load ingredients into input slots. Each "turn" (or processing step) applies heat/cooling/distillation. After N steps, the output slot produces a product.
- **Puzzle angle**: Which reagents go in which slots? Ordering matters — loading in the wrong sequence causes unwanted reactions inside the alembic.

### Multi-Step Brewing

A series of placement puzzles forming a recipe:

1. **Preparation**: Place raw reagents into a mortar & pestle. Spatial constraints apply (Sharp reagents cut Fragile ones during grinding).
2. **Heating**: Transfer ground mixture to an alembic. Heat management — too much Hot causes boil-over, too little doesn't activate.
3. **Cooling**: Move heated mixture to a cooling rack. Cold aspect timing matters.
4. **Extraction**: Final placement into output vials. Product quality depends on how well each step was executed.

Each step is a self-contained puzzle using the existing grid/bag/chemistry systems with step-specific constraints and objectives.

### Potion Output

Successfully brewed potions become consumable items for venture mode:
- **Healing potions**: Restore stamina (remove injury locks).
- **Resistance potions**: Temporarily raise specific thresholds (fire resistance, poison immunity).
- **Enhancement potions**: Buff the player's abilities (extra bag capacity, reduced stamina cost).
- **Transmutation potions**: Change a reagent's aspects (powerful, rare, quest-reward tier).

### Equipment Types

Multiple workstation variants, each with different spatial layouts and constraints:
- **Mortar & Pestle**: Small, single-bag. For grinding/mixing. Sharp reagents have special interactions.
- **Alembic**: Multi-compartment. For distillation. Heat and cold management critical.
- **Distillation Apparatus**: Large, complex layout. For advanced recipes. Multiple temperature zones.
- **Crucible**: High-heat. For transmutation. Only fire-resistant reagents survive.

---

## Related Documents

- `docs/mvp-specification.md` — MVP scope, generation algorithm, tutorial sequence
- `docs/chemistry-system.md` — Aspect tiers, reaction rules, sensitivity system
- `docs/roadmap.md` — Full feature roadmap with tier classifications
- `docs/game-overview.md` — Design pillars and game vision
- `docs/core-mechanics.md` — Grid, bags, reagents, adjacency rules
- `docs/venture-and-progression.md` — Venture loop, stamina, quests (context for potion output)
