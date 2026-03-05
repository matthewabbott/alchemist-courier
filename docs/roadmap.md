# Feature Roadmap

Central reference for every planned feature in Alchemist Courier, from the current MVP through long-term aspirational ideas. Each feature is classified into one of four tiers and links to its detailed spec.

**How to use this document:** Read it top-to-bottom for the full picture, or jump to a tier for scoping. Cross-references point to the detailed design doc and section. If a cross-reference says "(pending)" the doc doesn't exist yet but is tracked as a PRD task.

## Tier Definitions

| Tier | What It Means | Examples |
|------|--------------|---------|
| **1: MVP** | Currently building. Daily web puzzle. | Core grid, chemistry, puzzle gen, tutorial, daily mode |
| **2: Post-MVP** | Concrete designs, buildable as next steps. Stages 2-6 of the roadmap. | Puzzle progression, venture loop, quests, base, regions |
| **3: Aspirational** | Interesting but requires new mechanical infrastructure. | Item protrusion, destruction/transmutation, advanced alchemy schools |
| **4: DLC / Expansion** | "Errands for an Alchemist, not Actual Alchemist Simulator." | Brewing/alembic, full character expression, music |

See `docs/game-overview.md` for the design pillars that guide all tiers: the puzzle IS the game, flavor-first design, depth through emergence, cozy progression, approachable surface with rich interior.

---

## Tier 1: MVP (Daily Web Puzzle)

**Status:** ~95% complete. See `docs/mvp-specification.md` for full spec.

### Implemented

- **Core inventory tetris** — meta-grid, bags (Normal, Null, Gravity, Locked), reagents with shapes/rotation, placement validation. See `docs/core-mechanics.md`.
- **Chemistry system** — 6 Simple aspects (Hot, Cold, Volatile, Stable, Heavy, Toxic), 7 reaction rules, 3 sensitivity tiers, 5 modifiers (Null suppression, Stable buffer, Catalytic debuff, Medicinal relief, Insulated threshold). See `docs/chemistry-system.md` lines 40-45.
- **Procedural puzzle generation** — seeded disassemble-a-solution algorithm, deterministic PRNG. See `docs/mvp-specification.md` lines 92-122.
- **Tutorial system** — 5 progressive lessons introducing mechanics one at a time.
- **Daily puzzle** — date-seeded (YYYYMMDD), streak tracking, difficulty rotation.
- **Win/loss evaluation** — confirm flow, zero-reaction check for FitAllSafely.
- **Save/load** — JSON serialization, localStorage for web, tutorial progress + daily completions + streaks.

### In Progress

- **Hint system** — auto-pick mode, soft-lock (movable, unlike IsLocked), snap-back for previously-hinted reagents, dislodge occupied cells, configurable budget per difficulty. See `docs/puzzle-design.md` (pending DOC.1).
- **Highlight system** — hover detection, selection feedback, contiguous boundary rendering, aspect icons on cells.
- **Visual polish** — contiguous bag/reagent borders, aspect iconography, placement animations. See `docs/visual-polish-design.md` (pending H.14).

### MVP Puzzle Types

- **FitAllSafely** (implemented) — place all reagents with zero reactions. Binary pass/fail.
- **MaximizeValue** (enum exists, evaluation not implemented) — place subset maximizing total reagent value.
- **MeetRequirement** (enum exists, not implemented) — meet a specific constraint (e.g., "include 3+ hot reagent cells").

See `docs/mvp-specification.md` lines 56-76.

### Scoring (MVP)

- **Time** + **hints used**. Never move count — placing/removing is the core interaction; don't punish experimentation.
- Binary pass/fail for FitAllSafely.

See `docs/puzzle-design.md` (pending DOC.1).

---

## Tier 2: Post-MVP (Stages 2-6)

Each stage builds on the previous. The 7-stage roadmap originates in `docs/game-overview.md` lines 74-84.

### Stage 2: Puzzle Progression Mode

Curated and procedurally generated puzzle sequences, replacing the one-off daily format with a campaign-like structure.

- **3-star system** per puzzle: Star 1 = completion, Star 2 = no hints used, Star 3 = under par time.
- **Par times** by difficulty: Easy = 90s, Medium = 150s, Hard = 240s.
- **Select-then-hint mode** — player picks which reagent to hint (more strategic than auto-pick). Deferred from MVP.
- Stars persist in save data, visible on puzzle history.

See `docs/puzzle-design.md` (pending DOC.1).

### Stage 2b: Optimization Puzzle Types

New PuzzleType enum variants that go beyond binary pass/fail:

- **BalanceToNeutral** — target net-zero for a specific aspect pair (e.g., Hot vs Cold). Score = absolute distance from zero. Par value provided.
- **MinimizeSeverity** — all reagents MUST be placed, some reactions are unavoidable. Score = total reaction severity. Par = minimum achievable severity.
- **MaximizeWithConstraints** — oversized reagent set, limited bag space. Score = total value of placed reagents.
- **TimedChallenge** — FitAllSafely with a visible timer. Star thresholds for speed. No fail on timeout, just lower rating.

See `docs/puzzle-design.md` (pending DOC.1).

### Stage 2c: Chemistry Complexity -- Intermediate Tier

Unlocked progressively through puzzle campaigns, never in standalone daily puzzles.

- **New aspects (4):** Sharp, Fragile, Catalytic, Medicinal.
- **New bag types:** Insulated, Reinforced, Fragile.
- **New interactions:** Sharp edge constraint, Sharp+Fragile breakage, Catalytic threshold lowering, Fragile universal vulnerability, Medicinal threshold relief.

See `docs/chemistry-system.md` lines 47-52 (tier overview) and lines 87-93 (aspect details).
See `docs/core-mechanics.md` lines 64-82 (bag type specs).

### Stage 3: Venture Loop

Slay the Spire-style encounter map wrapping the puzzle core. The primary gameplay mode of the full game.

- **Venture flow:** Prepare -> traverse branching map -> gather reagents -> manage inventory -> push deeper or turn back -> return home -> receive rewards.
- **Stamina system** — numeric bar, movement costs (1 per node), locked stamina from status effects (overheating, hypothermia, poison, injury), recovery through consumption/encounters.
- **Encounter map** — branching paths, 8-15 nodes deep, node types: Reagent Gathering, Shop, Rest, Hazard, Event, Quest Node, Forced.
- **Failure system** — run-ending (out of stamina = lose gathered reagents) and within-venture setbacks (explosions, breakage, contamination). Permanent progression never lost.

See `docs/venture-and-progression.md` -- this doc is the full spec for Stages 3-6.

### Stage 4: Quest System

Objectives that drive ventures and create interesting constraints.

- **Quest types:** aspect gathering, specific reagent retrieval, value threshold, restriction challenge, volatile cargo, multi-aspect balance.
- **Quest-specific bags** — containment vessels, specimen cases (returned after venture).
- **Quest sources** — quest board, NPC requests, procedural generation.

See `docs/venture-and-progression.md` lines 103-133.

### Stage 5: Base Management

Home base using the same grid mechanics as the puzzle.

- **Home grid** — larger grid for storing reagents/bags between ventures. Upgradeable with furniture.
- **Base upgrades** — storage furniture (cabinets, drawers), workstations (alchemy bench, codex stand), amenities (bed, garden, kitchen).
- **Permanent progression** — currency, bag collection, reagent storage, stamina upgrades, codex knowledge, alchemical effects (cold-blooded, fireproof, immunity, contamination, insight), unlocked regions.
- **The base loop:** return from venture -> sell/store -> check quest board -> buy/upgrade -> prepare -> depart.

See `docs/venture-and-progression.md` lines 162-219.

### Stage 5b: Art -- Chibi on Map

- Small chibi character visible on the encounter map during ventures.
- Simple idle and movement animations between nodes.
- No bag visualization yet.

See `docs/art-and-style.md` lines 73-76.

### Stage 6: Regions & Story

Multiple wilderness biomes with distinct reagents, hazards, and narrative.

| Region | Theme | Unique Hazards |
|--------|-------|---------------|
| Verdant Woods | Forest (starting) | Mild (thorns, insects) |
| Ember Peaks | Volcanic | Environmental heat |
| Frost Hollows | Frozen | Environmental cold |
| Miasma Marsh | Toxic swamp | Environmental toxicity |
| Crystal Caverns | Underground | Fragile terrain (bag damage) |
| The Void | Extreme endgame | All hazards, aspects behave strangely |

Regions unlock progressively through quests and gear. See `docs/venture-and-progression.md` lines 221-234.

### Post-MVP Bag Types

- **Expanding** (rare) — can be stitched/expanded during special events, gaining cells. Roguelite progression mechanic. See `docs/core-mechanics.md` lines 92-96.

---

## Tier 3: Aspirational (Requires New Infrastructure)

Features that are interesting extensions but need mechanical systems that don't exist yet. Same tier as item protrusion from bags.

### Chemistry Complexity -- Advanced Tier

The deepest mechanical layer, reached only after mastering Intermediate.

- **New aspects:** Magnetic (forced clustering), Absorbent (selective neutralization).
- **Body bag type** — represents hands/back, allows unlimited protrusion, safety constraints (holdability).
- **Aspirational aspects** (flavor-first, mechanics TBD): Luminous, Corrosive, Resonant, Adhesive, Ethereal.

See `docs/chemistry-system.md` lines 54-59 (tier overview), lines 91-93 (Magnetic/Absorbent), lines 139-147 (aspirational aspects).
See `docs/core-mechanics.md` lines 84-90 (Body bag).

### Additional Alchemy Schools

New aspect families drawn from different esoteric traditions. Each school is self-contained with cross-school interactions.

- **Wuxing / Five Elements** (Post-MVP) — Wood, Fire, Earth, Metal, Water in generating and overcoming cycles. Adjacent generating pairs strengthen; overcoming pairs suppress/destroy.
- **Western Alchemy / The Great Work** (Post-MVP) — Nigredo -> Albedo -> Citrinitas -> Rubedo stages. Sequence-dependent reactions; out-of-order placement causes adverse effects.
- **Astrology** (TBD) — planetary/zodiacal affinities, possibly time-based effects.
- **Other systems** (sketch only) — catalytic chemistry, herbal/botanical, spiritual/ethereal.

See `docs/chemistry-system.md` lines 149-205.

### Item Protrusion

Reagent cells can extend beyond bag boundaries, losing their aspects (treated as Null-suppressed).

- Must be at least 50% inside the bag.
- Body bags allow unlimited protrusion.
- Heavy items cannot protrude.
- Sealed bags disallow protrusion.
- Creates trade-offs: fit large reagents in small bags at the cost of aspect utilization.

See `docs/core-mechanics.md` lines 281-297.

### Destruction & Transmutation Puzzles

New puzzle types where reactions happen DURING gameplay, not just at confirm.

- **Destruction** — reactions that remove reagent cells or entire reagents. Player deliberately triggers explosions to make room.
- **Transmutation** — two reagents combine into a third with new shape and aspects. Requires a recipe database.
- **Forced Explosion** — win condition IS triggering a specific reaction. Inversion of FitAllSafely.
- **Requires:** reaction execution during gameplay, reagent removal/creation animation, grid state recalculation post-reaction.

See `docs/puzzle-design.md` (pending DOC.1).

### Levitating Bag

Doesn't need meta-grid adjacency to the character -- floats and can be placed anywhere. Removes the spatial constraint of contiguous bag arrangement.

See `docs/core-mechanics.md` lines 98-100.

### Art -- Paper-Doll with Status

Side-view paper-doll showing the character with equipped bags visible on their body.

- Status effects visible: sweating (heat), shivering (cold), green tint (poison), bandages (injury).
- **Prospective reactions:** character reacts to unconfirmed placements (starts sweating if arrangement would overheat, panics before explosion, relaxes when safe).
- Inspired by Risk of Rain 2's visual equipment display.

See `docs/art-and-style.md` lines 78-91.

### Consumable & Transformative Potions

- **Consumable potions** — crafted/purchased, brought on ventures for temporary effects (fire resistance, stamina restoration, threshold raising).
- **Transformative potions** — permanent character modifications gained through quests/mastery (asbestos hands, null touch, extra pockets).

See `docs/venture-and-progression.md` lines 190-198.

---

## Tier 4: DLC / Expansion

"This game is Errands for an Alchemist." These features would essentially be new game modes layered on the existing puzzle engine. Documented for long-term vision.

### Actual Alchemy / Brewing

- **Alembic container** — special bag type that processes reagents over time (turns). Input slots + output slot. Recipe-based.
- **Multi-step brewing** — load ingredients -> heat -> cool -> extract product. Each step is a placement puzzle with different constraints.
- **Potion output** — consumable items for venture mode (healing, buffs, environmental manipulation).
- **Equipment types** — mortar & pestle, alembic, distillation apparatus.
- Think Stardew Valley's homestead but for an alchemist's workshop.

See `docs/chemistry-system.md` lines 309-315.
See `docs/puzzle-design.md` (pending DOC.1).

### Art -- Full Expression (Dream Tier)

- Character sprite shows ALL equipped bags, carried reagents visible as tiny sprites.
- Walking animation changes with encumbrance level.
- Environmental reactions (squinting in bright areas, huddling in cold regions).
- NPC-quality expressiveness for cutscenes and events.

See `docs/art-and-style.md` lines 93-97.

### Selective Null Suppression

Bags that suppress only specific aspects (not all). More strategic than current all-or-nothing Null bags.

See `docs/core-mechanics.md` line 49.

### Sound & Music

- **Direction:** Stardew Valley warmth meets Book of Hours mystical ambiance.
- **Themes:** puzzle-solving, venture map, encounters, home base, per-region variations.
- Placeholder/royalty-free tracks during development.

See `docs/art-and-style.md` lines 202-206.

---

## Cross-Reference Index

| Document | What It Covers | Tiers |
|----------|---------------|-------|
| `docs/mvp-specification.md` | MVP scope, puzzle types, generation, tutorial, UI layout | 1 |
| `docs/puzzle-design.md` (pending DOC.1) | All puzzle types, scoring, hints, destruction, optimization | 1-4 |
| `docs/visual-polish-design.md` (pending H.14) | Contiguous borders, aspect icons, animations, visual feedback | 1 |
| `docs/venture-and-progression.md` | Venture loop, stamina, encounters, quests, base, regions, save | 2 |
| `docs/chemistry-system.md` | Complexity tiers, all 5 schools, reaction rules, brewing | 1-4 |
| `docs/core-mechanics.md` | Grid, bags, reagents, adjacency, spanning, protrusion | 1-3 |
| `docs/art-and-style.md` | Visual style, character tiers, RetroDiffusion pipeline, audio | 1-4 |
| `docs/architecture.md` | Technical architecture, data models, layer separation | 1 |
| `docs/architecture-diagram.md` | Mermaid + ASCII system and data flow diagrams | 1 |
