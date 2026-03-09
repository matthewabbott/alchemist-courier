# Chemistry System — Aspects, Schools, and Reactions

The chemistry system is the heart of Alchemist Courier. It transforms inventory tetris from a pure spatial puzzle into a richly interactive system where *what* you place matters as much as *where* you place it. Inspired by Breath of the Wild's procedural chemistry, the system uses general rules rather than hand-scripted interactions.

## Design Philosophy

### Flavor-First Design
All interaction rules should be designed based on "how would this work in 'real' alchemy or mysticism?" first, then balanced for gameplay second. The system draws from multiple real-world esoteric traditions to create interactions that feel *intuitive* — a player should be able to guess that fire and volatile substances don't mix, or that water quenches fire.

### Emergent Complexity
Simple, consistent rules combining to produce complex outcomes. The system should surprise even its designers with interesting edge cases, rather than having every interaction individually scripted.

### Procedural Chemistry (BotW-Inspired)
Like Breath of the Wild's chemistry engine, the aspect system defines general behavioral rules. Fire aspects generate heat. Volatile aspects react to heat. These are properties of the aspects themselves, not specific item-to-item interactions. A new reagent with fire and volatile aspects will behave predictably without needing new code.

## Complexity Model

See `docs/roadmap.md` for how these complexity tiers map to the development roadmap (MVP, Post-MVP, Aspirational).

Puzzle complexity has **two independent dimensions**:

### Dimension 1: Scale (size of the puzzle)

How many things the player must juggle spatially -- bags, reagents, cells. A large puzzle with only 6 aspects and 4 bag types can be extremely hard purely through spatial pressure (tight fit, many reagents, awkward shapes). Scale is controlled by DifficultyParams: bag count, bag sizes, reagent count, spare space percentage.

### Dimension 2: Mechanical Depth (number of unique interactions)

How many distinct aspect behaviors and bag types are active. More mechanics means more rules to internalize. Mechanical depth is controlled by the **complexity tier** (Simple, Intermediate, Advanced).

### How the Dimensions Interact

**One-off / standalone puzzles** (daily game, individual challenges) should trend toward **Simple mechanical depth at any scale**. A daily Hard puzzle is hard because it has 12 reagents in tight bags, not because it introduces Sharp+Fragile+Catalytic. The player needs to grok the puzzle from a cold start, so fewer mechanics means faster comprehension and more spatial focus.

**Sequential puzzle modes** (roguelite, tutorial, curated campaigns) can ramp up **mechanical depth gradually**. The player starts with 2 aspects and 1 bag type, and new mechanics are introduced one at a time over many puzzles. By puzzle 30, the player has internalized Intermediate mechanics through practice, so adding Catalytic feels natural rather than overwhelming.

This means the complexity tier isn't just about difficulty -- it's about **context**. A "Hard" daily puzzle and a "Hard" roguelite puzzle may have similar scale, but the daily uses Simple mechanics while the roguelite uses Intermediate or Advanced.

### Complexity Tiers

#### Simple (Tutorial, Daily Easy/Medium/Hard -- MVP Target)

- **Aspects (8)**: Hot, Cold, Volatile, Stable, Heavy, Toxic, Sharp, Fragile
- **Interactions**: Cumulative thresholds (Hot/Cold/Toxic), Hot+Cold neutralization, Volatile net-hot explosion, Stable buffering, Heavy falling, Sharp edge restriction, Sharp+Fragile breakage, Fragile vulnerability to adjacent reactions, Fragile thermal stress (Hot+Cold), Heavy crushing Fragile
- **Bag types**: Normal, Null, Gravity, Locked
- **Philosophy**: Few mechanics, complexity from puzzle size. The per-puzzle complexity budget (~5-6 elements) ensures no single puzzle uses all 8 aspects + 4 bag types simultaneously. All standalone/one-off puzzles should use this tier.

#### Intermediate (Roguelite Mid-Game, Curated Campaigns)

- **Adds aspects (2)**: Catalytic, Medicinal
- **Adds bag types**: Insulated, Reinforced, Fragile
- **Adds interactions**: Catalytic threshold lowering, Medicinal threshold relief
- **Philosophy**: Meaningful new constraints that interact with existing mechanics. Each new aspect has a primary mechanic that matters on its own (not just in combination with one specific partner). Only used in modes that build up to it sequentially.

#### Advanced (Roguelite Late-Game, Special Events)

- **Adds aspects (2+)**: Magnetic, Absorbent (and potentially Corrosive, Luminous)
- **Adds bag types**: Body (with protrusion)
- **Adds interactions**: Volatile chain reactions, Sharp piercing Null suppression, Magnetic forced clustering, Absorbent selective neutralization
- **Philosophy**: Full mechanical depth. Sequential modes build to this gradually over many puzzles. Players should feel like they've earned these interactions through mastery of simpler tiers.

### Puzzle Complexity Budget

Independent of which complexity tier a mechanic belongs to, each individual puzzle has a **per-puzzle complexity budget** that caps how many distinct mechanics appear simultaneously. This prevents overwhelming the player even when many mechanics exist at the same tier.

The budget counts distinct interaction types: unique aspects + unique bag special properties active in the puzzle.

| Puzzle Context | Complexity Budget | Example |
|---------------|------------------|---------|
| **Simple (standalone/daily)** | ~5-6 distinct elements | Hot, Cold, Volatile, Stable + Normal, Gravity = 6 |
| **Intermediate (sequential)** | Higher, TBD during balancing | Could include 8-10 elements after gradual introduction |
| **Advanced (late roguelite)** | Full mechanical depth | All unlocked mechanics available |

**Rationale:** Even though the Simple tier defines 12 elements (8 aspects + 4 bag types), using all of them in a single puzzle would overwhelm a player encountering it cold. The budget ensures puzzles are graspable: the legend and tooltips stay manageable, and the player can internalize the active rules before solving.

The generator should select a subset of available mechanics per puzzle, keeping the total within budget. Scale (puzzle size, reagent count) provides difficulty independently of mechanical depth.

## Schools of Alchemy

Aspects are organized into **schools** -- thematic categories drawn from different alchemical and mystical traditions. Each school is a self-contained set of aspects with its own internal logic. Aspects from different schools can interact cross-school.

A reagent can have aspects from one or more schools. Simpler reagents tend to have aspects from only one school; complex/rare reagents may draw from multiple schools.

### Standard Aspect Count
- **Common reagents**: 1-2 aspects, usually from one school
- **Uncommon reagents**: 2-3 aspects, one or two schools
- **Rare reagents**: 3-4 aspects, potentially from multiple schools
- **Unique/Quest reagents**: 4+ aspects, multiple schools, complex interactions

### School 1: Physical / Chemistry (MVP -- Implement First)

The most intuitive school. These are properties that players can understand without any alchemical knowledge. This school should be fully implemented before any others.

Every aspect has a **primary mechanic** that makes it relevant on its own (non-parasitic), plus **secondary interactions** that create depth when combined with other aspects.

| Aspect | Complexity | Primary Mechanic (always relevant) | Secondary Interactions |
|--------|-----------|-----------------------------------|----------------------|
| **Hot** | Simple | Cumulative overheating threshold (whole inventory) | +Cold = neutralization; contributes to net-hot near Volatile |
| **Cold** | Simple | Cumulative hypothermia threshold (whole inventory) | +Hot = neutralization; reduces net-hot near Volatile (strategic tool) |
| **Volatile** | Simple | Explodes when adjacent net-hot > 0 (sum adjacent hot - cold) | Volatile+Volatile adjacent amplifies (Intermediate) |
| **Toxic** | Simple | Cumulative poisoning threshold; Toxic+Toxic same-bag = cloud | Toxic spread within bag (Intermediate) |
| **Heavy** | Simple | Falls as low as possible in ALL bags (not just Gravity). Descends until bottom-most cells hit the bag floor or another reagent. In Normal bags "bottom" is down; Gravity bags may redefine "bottom" direction | Crushes Fragile directly below (Intermediate); can't protrude (Advanced) |
| **Stable** | Simple | Raises thresholds of adjacent reactions (universal buffer) | Cancels Catalytic (Advanced) |
| **Sharp** | Simple | Can't place on edge cells of non-reinforced bags | +Fragile adjacent = breakage; pierces Null suppression (Advanced) |
| **Fragile** | Simple | Breaks from ANY adjacent reaction; can't be in gravity bags | +Sharp = breakage; Hot+Cold adjacent simultaneously = thermal stress fracture; crushed by Heavy directly above; higher value to compensate vulnerability |
| **Catalytic** | Intermediate | Lowers thresholds of adjacent reactions (inverse of Stable) | Universal amplifier -- makes mild interactions dangerous |
| **Medicinal** | Intermediate | Passively reduces cumulative thresholds when carried | Consume to clear status effects (roguelite) |
| **Magnetic** | Advanced | Must be placed adjacent to another Magnetic item (forced clustering) | Creates adjacency pressure with everything near the cluster |
| **Absorbent** | Advanced | Neutralizes one adjacent aspect per cell (player chooses) | Strategic tool: sacrifice space to protect neighbors |

#### De-Parasitization Notes

A "parasitic" mechanic (in the Magic: The Gathering sense) only interacts with one specific other mechanic. Parasitism makes puzzles feel scripted rather than emergent. Each aspect above is designed to have standalone relevance:

- **Sharp** is NOT just "breaks Fragile." Its primary mechanic (edge placement restriction) matters even without Fragile in the puzzle. Every non-reinforced bag creates spatial constraints for Sharp reagents.
- **Fragile** is NOT just "broken by Sharp." Its primary mechanic (breaks from any adjacent reaction) makes it universally vulnerable. Hot+Volatile explosion nearby? Fragile breaks. It also can't go in gravity bags, adding a placement constraint.
- **Heavy** is NOT just "contributes weight." It actively affects placement by falling, which interacts with gravity bags and spatial planning.
- **Catalytic** and **Stable** are mirrors of each other, and both interact with ALL reaction thresholds, not specific aspects.

#### Volatile: Net-Hot Specification

Volatile uses a **net-hot** calculation rather than simple Hot adjacency. This makes Cold strategically useful as a heat shield, not just a cumulative threshold liability.

```
For each Volatile cell:
  net_hot = sum(magnitude of Hot cells in cardinal neighbors)
          - sum(magnitude of Cold cells in cardinal neighbors)
  if net_hot > effective_threshold:
    trigger Explosion
  where effective_threshold = base (0) + stable_buffer - catalytic_reduction
```

Cold cells adjacent to Volatile cells reduce the effective heat, potentially preventing explosions. This creates a meaningful spatial interplay: players can deliberately place Cold reagents as buffers between Hot and Volatile, giving Cold a dual role (cumulative liability + local tactical tool).

#### Example Physical/Chemistry Interactions

| Trigger | Result | Sensitivity |
|---------|--------|-------------|
| Volatile cell with net-hot > 0 | **Explosion**: Destroys reagents, damages adjacent bag cells, may injure player | Adjacent cells, per-cell |
| Hot + Cold (same bag) | **Neutralization**: Both aspects are weakened or cancelled | Same bag, per-item |
| Toxic + Toxic (same bag, 2+ items) | **Toxic cloud**: Toxicity intensifies, may spread to adjacent bags | Same bag, cumulative |
| Sharp + Fragile (adjacent cells) | **Breakage**: Fragile reagent cracks or shatters | Adjacent cells, per-cell |
| Any reaction adjacent to Fragile | **Breakage**: Fragile reagent breaks from nearby reaction | Adjacent cells, per-cell |
| Heavy directly above Fragile (any bag) | **Crushing**: Heavy cell above Fragile cell blocks placement. In Gravity bags "above" = toward bag top; in Normal bags "above" = one row up | Placement constraint, per-cell |
| Fragile + (Hot AND Cold adjacent) | **Thermal Stress Fracture**: Fragile cell adjacent to both Hot and Cold simultaneously breaks from thermal contradiction. Inverse of Volatile (which needs net-hot). Fragile can be near Hot OR Cold safely, but not both | Adjacent cells, per-cell |
| Sharp on edge of non-reinforced bag | **Placement blocked**: Sharp reagents can't be on bag edge cells | Placement constraint |
| Catalytic + any reactive pair | **Lowered threshold**: The reaction's threshold is reduced | Adjacent cells, per-cell |
| Stable + any reactive pair | **Raised threshold**: The reaction's threshold is increased | Adjacent cells, per-cell |
| Medicinal (passive) | **Threshold relief**: Reduces cumulative thresholds while carried | Whole inventory, passive |
| Medicinal (consumed) | **Healing**: Clears status effects (roguelite only) | N/A (consumption) |
| Magnetic (placement) | **Must cluster**: Must be adjacent to another Magnetic item | Placement constraint |
| Absorbent (passive) | **Neutralizes**: Absorbs one adjacent aspect per cell | Adjacent cells, per-cell |
| Hot cumulative > threshold | **Overheating**: Stamina locked | Whole inventory, cumulative |
| Cold cumulative > threshold | **Hypothermia**: Stamina locked | Whole inventory, cumulative |
| Toxic cumulative > threshold | **Poisoned**: Ongoing stamina damage | Whole inventory, cumulative |

### Aspirational Aspects (Flavor-First Archive)

These aspects have strong alchemical flavor but need further mechanical definition. They may belong in the Physical school or in future schools. Archived here for future consideration.

- **Luminous**: Glowing reagents that illuminate or amplify adjacent properties. Potentially better suited for roguelite (reveal hidden aspects on adjacent reagents) than pure puzzle mode. Fits the "mystical reagent" fantasy.
- **Corrosive**: Acid that eats through containers. Adjacent bag cells become unusable? Eats through Null suppression? Strong flavor but needs careful balancing to avoid being frustrating.
- **Resonant**: Amplifies the magnitude of same-aspect neighbors. Interesting multiplicative mechanic but may be too niche to justify a dedicated aspect.
- **Adhesive**: Once placed adjacent to another reagent, the pair can't be separated (must be moved together). A constraint mechanic that creates commitment in placement order.
- **Ethereal**: Non-physical, passes through other reagents. Doesn't interact with Physical school at all. May belong in a Spirit school or serve as a "wildcard" that ignores spatial rules.

### School 2: Wuxing / Five Elements (Post-MVP)

Based on the Chinese five-element system (wuxing) and feng shui. Five elements arranged in two cycles:

**The Five Elements**: Wood, Fire, Earth, Metal, Water

**Generating Cycle** (each element nourishes the next):
```
Wood → Fire → Earth → Metal → Water → Wood
(Wood feeds Fire, Fire creates Earth/ash, Earth bears Metal,
 Metal collects Water/condensation, Water nourishes Wood)
```

**Overcoming Cycle** (each element controls another):
```
Wood → Earth → Water → Fire → Metal → Wood
(Wood parts Earth/roots, Earth absorbs Water, Water extinguishes Fire,
 Fire melts Metal, Metal chops Wood)
```

#### Gameplay Implications
- **Generating pairs** adjacent to each other: The nourished element is strengthened (amplified aspects, increased value)
- **Overcoming pairs** adjacent to each other: The overcome element is weakened or suppressed (reduced aspects, potential destruction)
- This creates a "what feeds what" puzzle layer on top of spatial arrangement
- Players must learn the cycles to predict which reagent combinations are beneficial vs. destructive

### School 3: Western Alchemy / The Great Work (Post-MVP)

Based on the four stages of the alchemical Magnum Opus:

| Stage | Color | Description | Gameplay Meaning |
|-------|-------|-------------|-----------------|
| **Nigredo** | Black | Decomposition, putrefaction | Decay, breaking down, entropy. Reagents with nigredo may degrade over time or weaken neighbors. |
| **Albedo** | White | Purification, washing | Cleansing, neutralizing. Albedo reagents may purify toxic or corrupt neighbors. |
| **Citrinitas** | Yellow | Awakening, solar energy | Activation, enhancement. Citrinitas may amplify the effects of adjacent reagents. |
| **Rubedo** | Red | The final stage, the philosopher's stone | Perfection, transmutation. Rubedo reagents are the most powerful and most volatile. |

#### Gameplay Implications
- The four stages form a **progression**: nigredo → albedo → citrinitas → rubedo
- Adjacent reagents in the correct sequence may "advance" each other (a nigredo reagent next to the right conditions becomes albedo)
- Skipping stages or placing them out of order causes adverse reactions
- Rubedo reagents are extraordinarily valuable but highly reactive with everything
- This school rewards knowledge of alchemical tradition and careful sequencing

### School 4: Astrology (Post-MVP, TBD)

Loosely based on astrological affinities. Details to be designed, but could include:
- Planetary associations (Mercury, Venus, Mars, Jupiter, Saturn, Sun, Moon)
- Zodiacal affinities
- Sympathies and antipathies based on traditional astrological rules
- Time-based effects (certain aspects stronger/weaker depending on in-game time or day)

### School 5+: Other Systems (Aspirational)

- **Catalytic chemistry**: Simplified real-world chemical rules (acids, bases, catalysts, solvents)
- **Herbal/botanical**: Plant-based properties (thorny, rooted, photosynthetic, parasitic)
- **Spiritual/ethereal**: Ghost-like properties (phasing, weight-of-soul, haunted)

## Sensitivity System (Detail)

Every reaction rule specifies a **sensitivity tier** and a **granularity**. This determines how the game evaluates whether the reaction's conditions are met.

### Sensitivity Tiers

```
Most Sensitive                              Least Sensitive
     |                                            |
     v                                            v
[Adjacent Cells] -----> [Same Bag] -----> [Whole Inventory]
```

1. **Adjacent Cells**: The reacting aspects must be on cells that are cardinally touching. This is the tightest constraint — the player can avoid the reaction by separating reagents by even one cell.

2. **Same Bag**: The reacting aspects must be in the same bag, but can be anywhere within it. Position within the bag doesn't matter, only co-presence. This represents effects like fumes spreading in an enclosed space.

3. **Whole Inventory**: The aspects can be anywhere across all bags. This represents effects that accumulate on the player's person regardless of containment. Primarily used for cumulative threshold effects (total heat, total toxicity).

### Granularity

Within each sensitivity tier:

- **Per-Cell**: Count individual cells with the relevant aspect. A 4-cell reagent with fire on every cell counts as 4 fire cells. Thresholds are set against cell counts.
  - Example: "Explosion requires 3+ adjacent volatile cells near 1+ hot cell"

- **Per-Item**: Count distinct reagents (items) with the relevant aspect. A 4-cell reagent with fire on every cell counts as 1 fire item. Thresholds are set against item counts.
  - Example: "Overcoming reaction requires 2 adjacent wuxing-fire items"

Per-item is inherently "less sensitive" — it treats a large multi-fire-cell reagent the same as a small single-fire-cell one.

### Threshold Values

Each reaction rule has a **threshold** — the minimum amount of the relevant aspect needed for the reaction to trigger. Thresholds are tunable per-rule and can be modified by:
- **Catalytic** aspects (lower the threshold)
- **Stable** aspects (raise the threshold)
- **Bag properties** (Null bags suppress aspects entirely, Insulated bags raise thresholds)
- **Environmental effects** (region-specific modifiers in ventures)

### Example: Full Reaction Rule

```
Reaction: Explosion
  Trigger: Hot + Volatile
  Sensitivity: Adjacent Cells
  Granularity: Per-Cell
  Threshold: 1 hot cell adjacent to 1 volatile cell
  Modified by: Catalytic (-1 threshold, so 0 = auto-trigger near catalyst)
               Stable (+1 threshold, so needs 2 of one to trigger)
               Null bag (suppresses the aspect entirely)
  Result: Both reagents destroyed, adjacent bag cells damaged,
          player takes injury (stamina locked)
```

## Within-Venture Consequences

Reactions don't just prevent placement — they have in-game consequences during ventures:

### Explosion
- Destroys the reacting reagents
- Damages adjacent bag cells (cells become unusable until mended)
- May injure the player (locking some stamina behind injury)
- Does NOT end the venture — the player continues with reduced resources

### Injury
- Locks a portion of the player's stamina (shown as cross-hatched bar with a red cross icon)
- Locked stamina is NOT permanently lost — it can be recovered by consuming a Medicinal reagent or visiting a healing encounter
- Multiple injuries stack

### Bag Damage
- Individual cells within a bag are destroyed / made unusable
- Damaged cells cannot hold reagents
- Can be mended by: special encounters, consuming a mending reagent, or specific bag properties (self-repairing)
- Severe damage may destroy an entire bag

### Contamination (Cumulative)
- Toxic, hot, cold, and other cumulative effects build up across the inventory
- When a threshold is crossed, the player suffers the corresponding status effect
- Status effects lock stamina or impose other penalties
- Can be reduced by discarding the offending reagents, consuming counteracting reagents, or visiting appropriate encounters

## Codex Integration

The Alchemical Codex tracks what the player has learned about reagents and their interactions. Critically, codex knowledge is **seamlessly integrated into the gameplay UI** — the player should never need to open the codex to benefit from it.

### Discovery
- When the player first encounters a reagent, its basic properties are unknown
- As the player handles the reagent (picks it up, places it, witnesses its reactions), its aspects are revealed
- Discovered aspects appear as **visual labels/icons on the reagent** in all future encounters

### Predictive Warnings
- Once a reaction has been discovered (witnessed at least once), the UI **warns the player before confirming a placement** that would trigger it
- This is automatic — no need to consult the codex
- Warnings appear as visual indicators on the prospective placement (e.g., flashing cells, danger icons, red highlight on the affected area)
- The character portrait/sprite may react to dangerous prospective placements (sweating, panicking)

### Codex as Reference
- The codex CAN be opened as a reference journal
- Lists all discovered reagents, their aspects, and witnessed reactions
- Primarily informational — all practical benefits are already in the UI
- May include lore text, alchemical illustrations, and flavor descriptions

## Future: Destruction, Transmutation, and Brewing

Aspirational puzzle types (destruction, transmutation, forced explosion) and DLC-tier features (alembic brewing, multi-step potion crafting) are documented in detail in `docs/puzzle-design.md` sections 6 and 7. These require infrastructure beyond the current evaluate-on-confirm model and are not in scope for MVP.

## Related Documents

- `docs/core-mechanics.md` — The grid, bags, and reagents that aspects are applied to
- `docs/mvp-specification.md` — Which aspects and interactions appear in the MVP
- `docs/puzzle-design.md` — Future puzzle types, scoring, destruction/transmutation, and brewing
- `docs/architecture.md` — Data structures for the chemistry engine
