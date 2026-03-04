# MVP Specification — Daily Web Puzzle

The Minimum Viable Product for Alchemist Courier is a **web-exportable daily puzzle game** built in Godot 4.6 with C# — a Wordle-like daily challenge where every player gets the same procedurally generated inventory puzzle, seeded from the current date.

## MVP Goals

1. **Validate the core fun**: Is inventory tetris with reactive aspects engaging?
2. **Public-facing product**: Hostable on itch.io or personal website, shareable
3. **Teach the rules**: Tutorial progression introduces mechanics incrementally
4. **Architectural foundation**: The systems built for MVP (grid, chemistry, procedural generation) are reused in the full game
5. **Prove the tech**: Godot 4.6 + C# + HTML5 export works reliably

## What the MVP Includes

### Core Systems (Must Have)
- [x] Meta-grid with bag placement
- [x] Bags with shapes (at least rectangular to start, ideally 2-3 shape types)
- [x] Reagents with shapes (1-4 cells, a range of shapes)
- [x] Reagent placement: pick up, rotate (90-degree increments), place in bag cells
- [x] Cell-contact adjacency detection
- [x] Physical/Chemistry school aspects at **Simple** complexity: Hot, Cold, Volatile, Stable, Heavy, Toxic (see `docs/chemistry-system.md` Complexity Levels)
- [x] Basic interaction rules: Volatile net-hot explosion, Hot+Cold neutralization, cumulative heat/cold/toxicity thresholds, Stable buffering, Heavy falling
- [x] Placement validation with warnings (codex-lite: show aspect icons, warn before dangerous placements)
- [x] Puzzle win/loss conditions
- [x] Procedural puzzle generation with deterministic seeding
- [x] Daily puzzle mode (date-seeded)
- [x] Tutorial progression (incrementally introduces mechanics)
- [x] Web export (Godot HTML5)
- [x] Basic save system (at minimum, track tutorial progress and daily puzzle completion)

### Nice to Have for MVP
- [ ] Simple complexity bag types: Null (implemented), Gravity, Locked
- [ ] Weight system (simple numeric cap)
- [ ] Score/value system for reagents (enables "maximize value" puzzles)
- [ ] Reagent consuming (right-click to consume for effect)
- [ ] Multiple save slots
- [ ] Share result (Wordle-style grid of emojis showing your solution)
- [ ] Aspect interaction primer / rules reference accessible from puzzle screen
- [ ] Non-rectangular bag shapes (L-shapes, irregular)

### Explicitly NOT in MVP
- Venture/encounter map
- Stamina system
- Permanent progression
- Base management
- Multiple alchemical schools (only Physical/Chemistry)
- Narrative/story
- Gamepad controls (mouse & keyboard only)
- Sound effects or music (silent, or minimal placeholder SFX)
- Character paper-doll / visible character

## Puzzle Design

### Puzzle Types

The MVP includes three puzzle types, used across the tutorial and daily challenges:

#### Type 1: Fit All Reagents Safely
- **Given**: A set of bags and a set of reagents
- **Goal**: Place ALL reagents into the bags without triggering any negative reactions
- **Win condition**: All reagents placed, no active negative reactions
- **Fail condition**: Impossible to place remaining reagents without triggering reactions (player can concede)

#### Type 2: Maximize Value Under Constraints
- **Given**: A set of bags and MORE reagents than can fit
- **Goal**: Place the highest total value of reagents without negative reactions
- **Scoring**: Sum of placed reagent values. Bonus for completing secondary objectives.
- **No fail condition**: Any valid (non-explosive) arrangement scores something

#### Type 3: Meet a Requirement
- **Given**: A set of bags and reagents, plus a specific requirement (e.g., "include at least 3 hot-aspected reagents")
- **Goal**: Meet the requirement without triggering negative reactions
- **Win condition**: Requirement met AND no active negative reactions
- **Complexity**: The requirement forces the player to include dangerous reagents, making the puzzle harder

### Puzzle Difficulty Scaling

Puzzle complexity has two dimensions: **scale** (how many bags/reagents to juggle) and **mechanical depth** (how many unique aspect behaviors and bag types are active). See `docs/chemistry-system.md` for full details.

All daily/standalone puzzles use **Simple** mechanical depth: 6 aspects (Hot, Cold, Volatile, Stable, Heavy, Toxic) and up to 4 bag types (Normal, Null, Gravity, Locked). Difficulty scaling is purely through **scale** -- more bags, more reagents, tighter fit. Even a "Hard" daily puzzle uses Simple mechanics, because the player needs to grok it from a cold start.

Difficulty is controlled by tuning these scale parameters:
- Number and size of bags
- Number and complexity of reagents
- Number and severity of aspects (within Simple mechanics)
- Whether bags have special properties (Gravity, Locked, Null)
- Tightness of fit (how much spare space exists)
- Whether the requirement forces dangerous combinations

## Procedural Puzzle Generation

### Algorithm: Disassemble-a-Solution

The generation approach guarantees solvability by working backwards from a valid solution:

```
1. GENERATE a valid solution
   a. Pick a set of bags based on difficulty parameters
   b. Pick a set of reagents based on difficulty parameters
   c. Place reagents into bags following all rules
   d. Verify no negative reactions in the arrangement
   e. If placement fails, adjust reagent/bag selection and retry

2. DISASSEMBLE the solution
   a. Record the valid arrangement as the "answer"
   b. Remove all (or some) reagents from the bags
   c. Optionally: shuffle bag positions on the meta-grid
   d. Optionally: lock some reagents in place (as hints or constraints)

3. PRESENT the puzzle
   a. Show the bags (in their positions on the meta-grid)
   b. Show the reagents to be placed (in a sidebar/tray)
   c. State the win condition (fit all / maximize / meet requirement)

4. SEED with today's date for daily mode
   a. Use a deterministic RNG seeded with YYYYMMDD
   b. All random choices (bag selection, reagent selection, placement order,
      bag positions) are driven by this seeded RNG
   c. Every player gets exactly the same puzzle
```

### Generation Parameters by Difficulty

| Difficulty | Bags | Reagents | Max Aspect Count | Bag Properties | Spare Space |
|-----------|------|----------|-----------------|----------------|-------------|
| Tutorial 1 | 1 small (3x3) | 2-3 (1-2 cells, no aspects) | 0 | None | Generous |
| Tutorial 2 | 1 small | 3-4 (1-3 cells, 1 aspect) | 1 | None | Moderate |
| Tutorial 3 | 2 small | 4-5 (1-3 cells, 1-2 aspects) | 2 | None | Moderate |
| Tutorial 4 | 2 bags (one special) | 4-6 (1-4 cells, 1-2 aspects) | 2 | 1 Null bag | Moderate |
| Easy Daily | 2-3 bags | 5-7 reagents | 2-3 | 0-1 special | Moderate |
| Medium Daily | 3-4 bags | 7-10 reagents | 3-4 | 1-2 special | Tight |
| Hard Daily | 4-5 bags | 10-14 reagents | 4-5 | 2-3 special | Very tight |

### Deterministic RNG

**Critical requirement**: All randomness must be deterministic given a seed. This means:
- Use a seeded pseudo-random number generator (not system random)
- Every random decision (bag shapes, reagent shapes, aspect assignment, placement order) must pull from the seeded sequence in a consistent order
- The same seed must always produce the same puzzle across all platforms and versions
- Seed format for daily: integer representation of YYYYMMDD (e.g., 20260303)

### Serializable Puzzle State

The puzzle state must be fully serializable for:
- Saving/loading progress on a puzzle
- Sharing puzzle results
- Potentially transmitting puzzles between client and server in the future

Minimum serializable state:
```
PuzzleState:
  seed: int
  difficulty: enum
  puzzleType: enum
  bags: [{shape, position_on_metagrid, special_property}]
  reagents: [{shape, aspects_per_cell, weight, value, is_locked}]
  placements: [{reagent_index, position_in_bag, rotation}]  // current player state
  solution: [{reagent_index, position_in_bag, rotation}]     // the valid answer
```

## Tutorial Progression

The tutorial is a sequence of puzzles that introduce one concept at a time:

### Lesson Sequence

| # | Concept Introduced | Puzzle Setup |
|---|-------------------|-------------|
| 1 | **Basic placement** | 1 bag, 2 single-cell reagents with no aspects. Just place them. |
| 2 | **Shapes & rotation** | 1 bag, 1 single-cell + 1 L-shaped reagent. Must rotate to fit. |
| 3 | **Multiple reagents, tight fit** | 1 bag, 3-4 reagents that exactly fill the bag. Spatial puzzle only. |
| 4 | **Aspects: Hot** | Introduces Hot aspect. "Hot reagents make you overheat if you carry too many." Cumulative threshold. |
| 5 | **Aspects: Volatile** | Introduces Volatile. "Volatile reagents are dangerous near hot ones." Adjacency reaction. |
| 6 | **Combining Hot + Volatile** | First real chemistry puzzle. Must place hot and volatile reagents without adjacency. |
| 7 | **Stable aspect** | Introduces Stable as a buffer. "Stable reagents reduce reactions nearby." |
| 8 | **Multiple bags** | Two bags on the meta-grid. Teaches bag arrangement and cross-bag adjacency. |
| 9 | **Spanning bags** | A reagent that must span two bags to fit. Teaches spanning mechanics. |
| 10 | **Null bag** | Introduces the Null bag property. "Aspects are suppressed in this bag." |
| 11+ | **Increasing complexity** | Layer additional Simple-tier aspects (Cold, Toxic, Heavy, Stable) one at a time. |

### Aspect Interaction Primer

Accessible from the puzzle screen (button or sidebar), the primer is a bite-sized reference showing:
- List of all aspects in the current puzzle's scope
- Simple icons and one-line descriptions
- Which aspects react with which (with danger icons)
- Approachable for novices — no jargon, clear visual language

## Daily Puzzle Mode

### Flow
1. Player opens the game (web page)
2. If tutorial not completed, start/resume tutorial
3. If tutorial completed, show today's daily puzzle
4. Player completes (or concedes) the puzzle
5. Show results: success/failure, score (if applicable), time taken
6. Optionally: share results (Wordle-style emoji grid)

### Daily Puzzle Difficulty
- Rotates through Easy / Medium / Hard on a 3-day cycle (or random seeded selection)
- Puzzle type also varies (fit all / maximize / meet requirement)
- Determined by the daily seed, so it's consistent for all players

### Streak Tracking
- Track consecutive days of puzzle completion
- Display streak count on main screen
- Simple but effective retention mechanic

## UI Layout (MVP)

### Puzzle Screen
```
+------------------------------------------------------+
|  [Alchemist Courier]           [Day 42] [Streak: 7]  |
+------------------------------------------------------+
|                        |                              |
|    META-GRID           |    REAGENT TRAY              |
|    (bags arranged)     |    (reagents to place)        |
|                        |                              |
|    +-------+-------+  |    [reagent 1] [reagent 2]   |
|    | Bag 1 | Bag 2 |  |    [reagent 3] [reagent 4]   |
|    |       |       |  |    [reagent 5]                |
|    +-------+-------+  |                              |
|                        |    OBJECTIVE:                 |
|                        |    "Place all reagents        |
|                        |     without explosions"       |
|                        |                              |
+------------------------------------------------------+
|  STATUS BAR                                           |
|  Heat: [===---] 3/6   Toxicity: [=-----] 1/6         |
|  [Confirm] [Reset] [Primer ?]                         |
+------------------------------------------------------+
```

### Key UI Elements
- **Meta-grid**: Central play area showing bags and placed reagents
- **Reagent tray**: Sidebar showing reagents available to place
- **Objective**: Clear statement of win condition
- **Status bar**: Cumulative aspect totals (heat, cold, toxicity) with thresholds shown
- **Confirm button**: Finalizes placement (triggers win/loss check)
- **Reset button**: Returns all reagents to the tray
- **Primer button**: Opens the aspect interaction reference

### Placement Warnings (Codex-Lite)
- When the player hovers a reagent over a position, the UI shows:
  - Green highlight: safe placement
  - Yellow highlight: creates a reaction that's within threshold (warning but OK)
  - Red highlight: would trigger a dangerous reaction (explosion, breakage, etc.)
  - Aspect icons on reagent cells visible at all times (once discovered in tutorial)
  - Status bar updates in real-time to show projected cumulative effects

## Technical Requirements for MVP

### Web Export
- Must compile and run as Godot HTML5 export
- Test on Chrome, Firefox, Safari, Edge
- Responsive layout (works on various screen sizes — see `docs/art-and-style.md` for resolution strategy)
- No native OS dependencies (pure web)

### Performance
- Puzzle generation should complete in < 1 second
- Adjacency/reaction evaluation should be instant (< 16ms for 60fps responsiveness)
- No loading screens needed (all assets are lightweight pixel art or placeholder rectangles)

### Save System (MVP)
- Save to browser localStorage (for web builds)
- Persist: tutorial progress, daily puzzle completion history, streak count
- Multiple save slots: at least 3, or unlimited if the UI can handle it cleanly
- Save format: JSON (serializable puzzle state)

## Placeholder Art Strategy

For MVP, use **colored rectangles** as placeholder art:
- Bag cells: light gray rectangles with border
- Reagent cells: colored by primary aspect (red=hot, blue=cold, orange=volatile, green=toxic, etc.)
- Aspect icons: simple geometric symbols (circle=hot, triangle=volatile, skull=toxic, etc.)
- Meta-grid: darker background grid
- Character: not shown in MVP (abstract status panel only)

Replace with RetroDiffusion pixel art iteratively as the system stabilizes.

## Success Criteria

The MVP is "done" when:
1. A player can complete the tutorial and understand the core mechanics
2. A daily puzzle generates correctly from a date seed
3. The daily puzzle is solvable (generation guarantees a solution exists)
4. Placement warnings correctly identify dangerous placements
5. The game runs in a web browser without errors
6. Puzzle state saves/loads correctly across sessions
7. The game is hostable on itch.io or a static web host

## Related Documents

- `docs/core-mechanics.md` — Full specification of grid, bags, reagents
- `docs/chemistry-system.md` — Full specification of aspects and interactions
- `docs/architecture.md` — Technical architecture and data models
- `docs/art-and-style.md` — Art direction and placeholder strategy
