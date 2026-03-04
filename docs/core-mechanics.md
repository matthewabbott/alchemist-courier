# Core Mechanics — Grid, Bags, and Reagents

This document specifies the core spatial puzzle system that forms the foundation of all gameplay in Alchemist Courier. Every other system (chemistry, ventures, progression) builds on top of these primitives.

## The Meta-Grid

The player's entire inventory is represented as a single large rectangular grid called the **meta-grid**. This grid conceptually represents the character's body/person — the space where bags can be worn or carried.

### Properties
- **Shape**: Rectangle (for simplicity). Loosely flavored as the character's body.
- **Cell size**: 32x32 pixels per cell
- **Dimensions**: TBD during prototyping, but should be large enough to accommodate multiple bags with some arrangement choices, while small enough that bag placement feels constrained and strategic
- **Contents**: The meta-grid does NOT directly hold reagents. It holds **bags**. Bags hold reagents.

### Design Intent
The meta-grid creates a *two-layer spatial puzzle*:
1. **Layer 1**: Arrange bags on the meta-grid (macro puzzle)
2. **Layer 2**: Arrange reagents within bags (micro puzzle)

The player can choose to place bags adjacent to each other (enabling reagents to span bags, maximizing space) or leave gaps between bags (preventing cross-bag adjacency reactions, maximizing safety).

## Bags

Bags are the fundamental inventory containers. **There is no "base" inventory** — ALL inventory space comes from bags. The player starts with one small bag and acquires more over time.

### Bag Properties

| Property | Description |
|----------|-------------|
| **Shape** | The cells the bag occupies on the meta-grid. Can be rectangular, L-shaped, split (e.g., "boot compartments" = two separate 2x1 areas), or other shapes. |
| **Size** | Number of cells the bag provides for reagent storage. A 3x3 bag has 9 cells. |
| **Special Properties** | Optional modifiers that affect reagents stored in the bag (see below). |

### Bag Special Properties

Bag types are introduced at different complexity levels (see `docs/chemistry-system.md` for the complexity level definitions). Each bag type is designed to create spatial constraints that interact with the aspect system.

#### Simple Complexity (MVP)

**Normal** (default):
- No special properties
- Aspects function normally, no placement restrictions beyond spatial fit

**Null**:
- Suppresses ALL aspects of reagents stored in this bag's cells
- A volatile reagent in a Null bag won't react to adjacent heat
- Spanning: only the cells physically inside the Null bag are suppressed (per-cell resolution)
- Sharp pierces Null suppression at Advanced complexity
- Selective null (suppress only specific aspects) is aspirational

**Gravity**:
- Reagents "fall" to the bottom of the bag based on bag orientation
- Placement constraint: items must be supported (resting on the bottom row or on top of existing items)
- Heavy items always fall TRUE down (world gravity), ignoring bag orientation
- Puzzle effect: limits where items can go, bottom fills first, creates stacking challenges
- Fragile items can't be placed in Gravity bags (Intermediate -- they'd break from falling)

**Locked**:
- In puzzle mode: bag contains pre-placed reagents that can't be moved or removed
- Creates fixed constraints the player must work around
- Implementation: `isLocked: true` flag on placements
- The locked reagents' aspects are active and participate in reactions -- they're constraints, not inert decorations

#### Intermediate Complexity

**Reinforced**:
- Sharp items CAN be placed on edge cells (overrides Sharp's edge restriction)
- +1 to all reaction thresholds for reactions involving reagents within this bag
- Immune to explosion damage (roguelite: bag cells can't be destroyed)
- Flavor: a sturdy, padded container for dangerous materials

**Insulated**:
- +1 or +2 to all reaction thresholds for reactions involving reagents within this bag
- Functions like a localized Stable effect for the whole bag
- Stacks with Stable aspect (Stable reagent in Insulated bag = very high effective threshold)
- Does NOT suppress aspects (unlike Null) -- reactions can still happen, just harder to trigger

**Fragile (bag)**:
- Can't hold Sharp items (placement blocked)
- Any explosion in or adjacent to the bag destroys the entire bag and all contents
- Forces careful aspect management -- high risk, potentially high reward (could be larger or have bonus properties)
- Flavor: a delicate glass container

#### Advanced Complexity (Aspirational)

**Body**:
- Represents the character literally holding or wearing the item (e.g., "hands" as a 1x1, "back" as a 2x3)
- Allows unlimited protrusion (see Protrusion System below)
- **Safety constraint**: any cell touching the character (i.e., inside the Body bag) must be safe for a human to touch. A torch's flame cells (Hot, Volatile) can't go in the hand, but its wooden handle (no dangerous aspects) can. This makes per-cell aspect design matter -- a reagent's orientation determines whether it's holdable.
- Brief description only -- full spec deferred to post-MVP

#### Aspirational (Not Yet Tiered)

**Expanding** (rare):
- Can be "stitched" or expanded during special events, gaining additional cells
- Roguelite progression mechanic

**Levitating** (special):
- Doesn't need to be adjacent to the character on the meta-grid -- it "floats" and can be placed anywhere
- Removes the meta-grid adjacency constraint for this one bag

### Bag Placement Rules
- Bags are placed on the meta-grid and must fit within its bounds
- Bags cannot overlap each other on the meta-grid
- Bags placed adjacent to each other create **contiguous inventory space** — reagents can span across them
- Bags can be intentionally separated (leaving empty meta-grid cells between them) to prevent cross-bag adjacency
- The meta-grid space is limited, so the player cannot carry unlimited bags
- Flavor-wise, bags are worn on the character's person (back, belt, hands, boots, etc.), with some exceptions (levitating bag)

### Bag Shapes
Bags themselves are spatial puzzles to arrange. Examples:
- **Standard pouch**: 3x3 rectangle
- **Satchel**: 4x3 rectangle
- **Belt pouch**: 1x4 horizontal strip
- **Boot compartments**: Two separate 2x1 areas (L-shaped or split on the meta-grid)
- **Bandolier**: Diagonal or irregular arrangement of cells
- **Alchemist's case**: 2x5 narrow rectangle with a Null property

Part of the fun is frankensteining an eclectic mix of differently-shaped bags into an arrangement that meets the player's needs.

## Reagents

Reagents are the items the player gathers and must arrange in their bags. They are the primary "pieces" of the inventory puzzle.

### Reagent Properties

| Property | Description |
|----------|-------------|
| **Shape** | The cells the reagent occupies when placed. Like Tetris pieces, but with more variety. |
| **Aspects** | Alchemical properties organized by "schools" (see `docs/chemistry-system.md`). Each cell of a reagent can have different aspects. |
| **Weight** | A numeric value contributing to total carry weight (separate from grid space). |
| **Value** | How much the reagent is worth when brought home (for scoring, quests, currency). |
| **Rarity** | Common, uncommon, rare, etc. Correlates with shape complexity and aspect count. |

### Reagent Shapes

Reagent shapes follow a **rarity-complexity correlation**:

| Rarity | Typical Size | Shape Complexity |
|--------|-------------|-----------------|
| **Common** | 1-2 cells | Single cell or domino. Easy to place. |
| **Uncommon** | 2-3 cells | Dominoes, L-shapes, small triominoes. Manageable. |
| **Rare** | 3-4 cells | Tetrominoes (T, L, S, Z, O, I shapes). Require planning. |
| **Very Rare** | 4-6+ cells | Pentominoes or larger, unusual shapes. Awkward and demanding. |
| **Quest/Unique** | Variable | May have very specific, restrictive shapes that define the puzzle. |

- Shapes are defined per-reagent in data (not limited to standard polyomino sets)
- Reagents can be **rotated** in 90-degree increments
- Reagents generally **cannot be flipped/mirrored** (unless a specific reagent property allows it)

### Per-Cell Aspects

A key design feature: **individual cells within a single reagent can have different aspects.** This means:
- A 3-cell reagent might have its left cell be "hot", middle cell be "neutral", and right cell be "volatile"
- Placement orientation matters — rotating the reagent changes which aspects are adjacent to which neighbors
- This creates deep placement puzzles even with a single reagent

### Reagent Design Philosophy

Reagents are designed **top-down from flavor**:
1. Start with evocative/signature items (fantastical, cute, loosely lore-grounded)
2. Extrapolate what regions/setting they imply
3. Extrapolate what other items would exist in that setting
4. Cross-reference with games and alchemical traditions for inspiration

Examples of the aesthetic: giggle mushroom, star jelly, flame sprite essence, cloud cotton, crystal snail shell — but also grounded items like cinnabar, quicksilver, moonstone. The vibe is "fantastical cute with alchemical authenticity underneath."

## Adjacency System

Adjacency determines which reagents can react with each other. This is the bridge between spatial placement and the chemistry system.

### Cell-Contact Based Adjacency

Two reagents are considered **adjacent** if ANY of their occupied cells touch in **cardinal directions** (up, down, left, right). Diagonal cells are NOT adjacent.

```
Example: Reagent A (2 cells) and Reagent B (3 cells)

  [A][A][ ]
  [ ][B][B]
  [ ][B][ ]

A and B are adjacent because A's right cell and B's top cell share a cardinal edge.
```

### Sensitivity Tiers

The chemistry system uses **sensitivity tiers** to determine how close reagents need to be for a reaction to trigger. From most to least sensitive:

| Tier | Description | Example Use |
|------|-------------|-------------|
| **Adjacent cells** | Reagent cells must be cardinally touching | Volatile + Fire → Explosion (requires direct contact) |
| **Same bag** | Reagents must be in the same bag (any position) | Toxic fumes spread within a contained space |
| **Whole inventory** | Reagents can be anywhere in any bag | Cumulative heat across all bags causes overheating |

### Granularity: Per-Cell vs Per-Item

Within each sensitivity tier, reactions can be measured at two granularities:

- **Per-cell**: Count individual cells with the relevant aspect. "3 adjacent fire cells" means three individual cells with the fire aspect touching.
- **Per-item**: Count items (reagents) with the relevant aspect. "2 adjacent fire items" means two separate reagents that each have at least one fire-aspected cell.

Per-item is the "less sensitive" version — a large reagent with many fire cells still counts as only 1 fire item.

### Interaction Resolution

When the player places or moves a reagent, the game evaluates all adjacency relationships and triggers any applicable reactions. See `docs/chemistry-system.md` for the full interaction rules.

## Spanning Bags

A reagent can occupy cells in two (or more) adjacent bags simultaneously if the bags are placed next to each other on the meta-grid. This is called **spanning**.

### Per-Cell Bag Effect Resolution

When a reagent spans multiple bags, **bag effects resolve per-cell**:
- Each cell of the reagent is affected only by the bag it physically occupies
- A reagent with one cell in a Null bag and one cell in a normal bag has its Null-bag cell's aspects suppressed, while the other cell's aspects remain active
- This creates interesting partial-suppression situations

```
Example: A 2-cell reagent [Hot][Volatile] spanning two bags:

  [Null Bag]  [Normal Bag]
  [ ][Hot*]   [Volatile][ ]
  [ ][ ]      [ ][ ]

The Hot cell is in the Null bag → its "hot" aspect is suppressed
The Volatile cell is in the Normal bag → its "volatile" aspect is active
The reagent is only partially dangerous!
```

## Weight System

Weight is a **flat numeric carrying capacity** separate from the grid system:
- The player has a maximum carry weight
- Each reagent has a weight value
- Total weight of all carried reagents must not exceed capacity
- Weight does NOT consume grid space — a heavy 1-cell reagent takes 1 cell but might weigh a lot
- Bags may or may not have their own weight (TBD during balancing)

Weight interacts with the stamina system in ventures (see `docs/venture-and-progression.md`).

## Consuming Reagents

The player can **consume** a reagent to activate its effects (healing, stamina recovery, aspect neutralization, etc.) and remove it from their inventory.

### How Consuming Works
- **UI**: Right-click / context menu on a placed reagent
- **Effect**: The reagent is removed from the bag and its consumable effect is applied
- **Availability**: Primarily during ventures (context-dependent). Some reagents may also be consumable only during specific encounters (Slay the Spire style — e.g., using a "strike" reagent to break a fall)
- **Trade-off**: Consuming a valuable reagent means you can't bring it home for quest/scoring purposes

### Aspirational: Combining/Brewing
In future updates, a "mixing and brewing" system could allow combining reagents for stronger effects (inspired by Resident Evil 4's herb combining). This is NOT in scope for MVP.

## Controls

### Mouse & Keyboard (Primary)
- **Click** to pick up / place a reagent
- **Scroll wheel** to rotate a reagent while held
- **Right-click** for context menu (consume, inspect, discard)
- **Click + drag** as alternative placement method
- **Arrow keys / WASD + R** for keyboard rotation

### Gamepad
- **D-pad** to move selection / navigate
- **A/X/Start** to place / confirm
- **Shoulder buttons** to rotate

All controls are fully customizable.

## Key Invariants (Rules That Must Always Hold)

1. All inventory space comes from bags. No "bare" meta-grid storage.
2. Reagents must fit entirely within bag cells (can span bags, but every cell must be in a bag).
3. Bags must fit within the meta-grid bounds and cannot overlap.
4. Adjacency is always cell-contact, cardinal-direction based.
5. Bag effects always resolve per-cell for spanning reagents.
6. Consuming a reagent always removes it from the bag.

## Protrusion System (Aspirational)

Some bags have a "top" or "opening" that allows items to stick out beyond the bag's cell boundaries. This is a post-MVP mechanic noted here for future reference.

### Basic Rules
- Cells of a reagent that extend outside the bag don't count for aspect purposes (treated as if Null-suppressed)
- Items must be at least 50% inside the bag (more cells inside than outside)
- Body bags: unlimited protrusion (hands can hold items that stick out in any direction)
- Sealed/closed bags: no protrusion allowed
- Heavy items can't protrude (too dense to balance)

### Design Intent
Protrusion adds a spatial trick: you can fit a large reagent into a small bag by letting it stick out, but the protruding cells lose their aspects. This creates trade-offs between space efficiency and aspect utilization.

Full specification deferred to post-MVP implementation.

## Related Documents

- `docs/chemistry-system.md` — How aspects interact (the rules that make adjacency matter)
- `docs/mvp-specification.md` — Which subset of these mechanics appears in the MVP
- `docs/architecture.md` — Data structures for implementing grids, bags, reagents
