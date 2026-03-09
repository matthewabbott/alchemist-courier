# Venture System & Progression — Post-MVP Design

This document covers the systems that wrap around the core inventory puzzle to create the full roguelite experience. These are **post-MVP** — they build on the foundation established by the daily puzzle MVP. The venture loop is the primary gameplay mode of the full game. This document details Stages 3-6 of the feature roadmap. See `docs/roadmap.md` for the full tier overview and cross-references to all planned features.

## Venture Loop Overview

A **venture** is a single excursion into the wilderness to gather reagents. It follows a Slay-the-Spire-like structure:

```
1. PREPARE  → Choose bags, review quest, check stamina
2. VENTURE  → Traverse a branching encounter map
3. GATHER   → At each encounter, decide what to pick up
4. MANAGE   → Arrange reagents in bags, deal with reactions
5. DECIDE   → Push deeper (spend stamina) or turn back
6. RETURN   → Navigate home, collecting along the way
7. REWARD   → Receive quest rewards, sell reagents, progress
```

The tension comes from the push-your-luck dynamic: every step deeper means more valuable reagents but less stamina margin for the trip home. Meanwhile, your inventory becomes increasingly volatile as you add more reactive substances.

## Stamina System

Stamina is the player's resource for moving through the encounter map. It creates the fundamental tension of the venture: go further for better rewards vs. save enough to get home.

### Basic Rules
- The player has a **stamina bar** (numeric, with a visual bar representation)
- Traversing from one encounter node to the next costs **1 stamina** (base cost)
- The player must maintain enough stamina to **return home** from their current position
- If stamina reaches 0, the venture fails (see Failure section)

### Stamina Modifiers

The state of the player's inventory directly affects their stamina:

| Effect | Stamina Impact | Visual |
|--------|---------------|--------|
| **Overheating** (cumulative hot > threshold) | Locks a chunk of stamina (inaccessible but recoverable) | Cross-hatched bar segment with flame icon |
| **Hypothermia** (cumulative cold > threshold) | Locks a chunk of stamina | Cross-hatched bar segment with snowflake icon |
| **Poisoned** (cumulative toxic > threshold) | Ongoing stamina drain (lose X per step) | Green-tinted bar segment draining |
| **Injury** (from explosions or hazards) | Locks a chunk of stamina | Cross-hatched bar segment with red cross icon |
| **Encumbered** (weight over soft cap?) | Each step costs more stamina (1.5x or 2x) | Heavy footstep icon on stamina bar |

**Key design**: Locked stamina is NOT permanently lost during the venture. It can be recovered by:
- Consuming a Medicinal reagent (heals injuries)
- Consuming a Cold reagent (reduces overheating) / Hot reagent (reduces hypothermia)
- Discarding the offending reagents (reducing cumulative aspect totals below threshold)
- Visiting a healing encounter node

This creates interesting decisions: do you consume a valuable reagent to unlock stamina, or tough it out and try to make it home?

### Distance Home

The "distance home" is the number of stamina points needed to return to the start of the map. This is displayed to the player at all times.

- **Calculation**: Minimum number of steps back along the path the player has traversed (or the shortest available return path if the map allows shortcuts)
- **Warning system**: The UI prominently warns when the player's available stamina (total minus locked) is close to the distance home
  - Green: Comfortable margin (stamina > distance home + 3)
  - Yellow: Getting tight (stamina = distance home + 1-2)
  - Red: Danger zone (stamina = distance home exactly, or less)

## Encounter Map

The encounter map is a branching path that the player navigates during a venture, similar to Slay the Spire's map.

### Map Structure
- **Branching paths**: Multiple routes from start to the deepest point
- **Nodes**: Each node is an encounter of a specific type
- **Depth**: Maps have a depth of roughly 8-15 nodes from start to end (scaling with region/difficulty)
- **The player can turn back at any node** — they don't have to reach the end

### Node Visibility
- The immediate next set of nodes is always visible (the player can see what types are available)
- Deeper nodes may be partially obscured (show type but not details) or fully hidden (depending on region/difficulty)

### Encounter Types

| Type | Description | Frequency |
|------|-------------|-----------|
| **Reagent Gathering** | The core encounter. Player is offered reagents to take. | Most common |
| **Shop / Trade** | Exchange reagents, currency, or stamina for other resources. | Uncommon |
| **Rest / Healing** | Recover stamina, heal injuries, mend bags. Like Slay the Spire's campfires. | Uncommon |
| **Hazard** | Environmental danger (extreme heat, cold, toxins). May force aspect effects or bag damage. | Moderate |
| **Event** | Random narrative event with choices. May offer rewards, impose costs, or modify the venture. | Rare |
| **Quest Node** | Related to the active quest. May contain the target reagent or a quest-specific challenge. | Fixed (1-2 per map) |
| **Forced** | Player MUST take a reagent or suffer a penalty. Creates interesting pressure. | Rare |

### Reagent Gathering Encounters

The most common encounter type. How reagents are offered varies:

- **Pick X of Y**: Encounter shows 3-5 reagents, player picks 1-2 to add to their bag. Classic draft/reward screen.
- **Take it or leave it**: Encounter offers one specific reagent. Player decides: bag it or skip it.
- **Forced acquisition**: Rare — the reagent is forced into the player's bag (or they take a penalty like injury or stamina loss). Creates "oh no, how do I deal with this?" moments.
- **Mix**: Different encounters within the same venture use different offer styles.

### Encounter Rewards (Beyond Reagents)
- **Bag drops**: Occasionally find new bags (immediately usable if meta-grid has space)
- **Equipment**: Special tools or items that provide venture-long bonuses
- **Currency**: Used at shops or for permanent progression
- **Stamina restoration**: Some encounters restore a small amount of stamina
- **Bag mending**: Repair damaged bag cells

## Quest System

Quests provide the objective for each venture. They give the player a reason to push deeper and take risks.

### Quest Structure
- Each venture has **one primary quest** (selected before departing)
- Quests define a **requirement** that the player must satisfy when returning home
- Successfully completing a quest grants **bonus rewards** on top of the venture's haul

### Quest Types

| Type | Example | Difficulty |
|------|---------|-----------|
| **Aspect gathering** | "Bring home 5+ fire-aspected reagent cells" | Easy-Medium |
| **Specific reagent retrieval** | "Retrieve the Philosopher's Ember from depth 10+" | Medium-Hard |
| **Value threshold** | "Return with reagents worth 50+ total value" | Easy-Medium |
| **Restriction challenge** | "Complete a venture using only 2 bags" | Hard |
| **Volatile cargo** | "Transport a highly reactive quest reagent home safely" | Hard |
| **Multi-aspect balance** | "Bring home hot AND cold reagents without either threshold triggering" | Hard |

### Quest Sources
- Quest board at home base
- NPC requests (aspirational)
- Procedurally generated from region parameters
- Story-driven quests (aspirational)

### Quest-Specific Bags
Some quests may lend or require specific bags:
- A "containment vessel" bag with Null property for transporting volatile quest reagents
- A "specimen case" bag with Lock property to preserve delicate quest items
- Quest bags are returned after the venture (not permanent additions)

## Failure System

### Types of Failure

#### Venture Failure (Run-Ending)
- **Out of stamina**: Player cannot reach home. The venture ends.
- **Default penalty**: Lose ALL reagents gathered during this venture (both found and brought from home)
- **Sometimes also lose bags**: In more dangerous regions or catastrophic failures
- **Permanent progression is never lost**: Currency spent, upgrades bought, codex entries learned — all kept

#### Within-Venture Setbacks (Non-Ending)
- **Explosions**: Destroy involved reagents + damage adjacent bag cells + possible injury
- **Breakage**: Fragile reagent shatters, reagent is lost
- **Contamination**: Cumulative thresholds exceeded, status effects applied
- **Bag damage**: Individual cells become unusable

**Critical design point**: Within-venture setbacks do NOT end the venture. The player continues with reduced resources. This makes the game forgiving moment-to-moment while still punishing strategically — you can recover from a single explosion, but cascading failures will leave you too depleted to complete the quest.

### Regional Failure Variation
Different regions impose different failure penalties:
- **Safe regions**: Lose gathered reagents only (not brought items)
- **Standard regions**: Lose all reagents (gathered + brought)
- **Dangerous regions**: Lose all reagents AND some/all bags
- **Extreme regions**: Lose everything AND take a long-term penalty (contamination, injury that persists across ventures)

This creates a natural risk/reward gradient as the player unlocks more dangerous areas.

## Permanent Progression

Progression persists across ventures. The player always has something to show for their time.

### Progression Types

| Type | Description | Persistence |
|------|-------------|-------------|
| **Currency** | Earned from selling reagents and completing quests. Used to buy bags, upgrades, and consumables. | Permanent |
| **Bag collection** | Bags acquired through ventures, quests, or purchase. Kept between ventures. | Permanent (unless lost in dangerous region) |
| **Reagent storage** | Some reagents can be saved at home base for use in future ventures. | Permanent |
| **Stamina upgrades** | Increase base stamina pool through progression milestones. | Permanent |
| **Codex knowledge** | Discovered reagent properties and reactions. Improves UI warnings. | Permanent |
| **Alchemical effects** | Long-term bonuses or maluses from repeated alchemical exposure. | Permanent |
| **Unlocked regions** | New areas to venture into, with different reagents and hazards. | Permanent |

### Alchemical Effects (Long-Term)

Repeated exposure to certain aspects has cumulative long-term effects on the character:

| Effect | Trigger | Consequence |
|--------|---------|-------------|
| **Cold-blooded** | Repeated cold exposure | Reduced cold sensitivity (higher threshold before hypothermia) |
| **Fireproof** | Repeated heat exposure | Reduced heat sensitivity |
| **Immunity to 1000 Poisons** | Repeated toxic exposure | Reduced toxicity sensitivity |
| **Elemental Contamination** | Excessive exposure to one element | Permanent aspect "aura" that affects nearby reagents — could be helpful or harmful |
| **Alchemical Insight** | Completing advanced codex entries | Passive bonuses (better reagent identification, earlier warnings, etc.) |

### Consumable Potions (Aspirational)
- Crafted or purchased at home base
- Brought on ventures for temporary effects (X encounters duration)
- Examples: Fire resistance potion (raises heat threshold for 5 encounters), Stamina tonic (restores 3 stamina), Stabilizer (raises all reaction thresholds nearby)

### Transformative Potions (Aspirational)
- Permanent character modifications
- Gained through quest rewards or alchemical mastery
- Examples: Asbestos hands (can handle hot reagents with less risk), Null touch (slightly suppresses aspects of reagents you place), Extra pockets (adds meta-grid cells)

## Base Management (Aspirational)

The player's home base uses a **grid-based storage system** similar to the inventory:

### Home Grid
- Larger grid than the venture meta-grid
- Used for storing reagents and bags between ventures
- Upgradeable: buy more cabinets, shelves, drawers (each adds grid space)
- Organizing your home collection is a satisfying puzzle in itself

### Base Upgrades
- **Storage furniture**: Cabinets, drawers, chests — each with their own grid shape and special properties
- **Workstations**: Alchemy bench (aspirational brewing), reagent rack (display/organize), codex stand
- **Amenities**: Comfortable bed (bonus stamina for next venture), garden (grow reagents), kitchen (prepare consumable potions)

### The Base Loop
```
Return from venture → Sell/store reagents → Check quest board →
Buy/upgrade bags and base → Prepare consumables → Depart on next venture
```

## Regions (Aspirational)

Different wilderness regions offer different reagents, hazards, and challenges:

| Region | Theme | Unique Reagents | Hazards | Unlock |
|--------|-------|----------------|---------|--------|
| **Verdant Woods** | Forest, basic | Common herbs, mushrooms, saps | Mild (thorns, insects) | Starting region |
| **Ember Peaks** | Volcanic | Fire crystals, magma slime, ash bloom | Heat (cumulative hot from environment) | Quest unlock |
| **Frost Hollows** | Frozen | Ice cores, cryo-moss, diamond dust | Cold (cumulative cold from environment) | Quest unlock |
| **Miasma Marsh** | Toxic swamp | Poison bulbs, bile slugs, peat essence | Toxic (cumulative toxic from environment) | Gear unlock (gas mask) |
| **Crystal Caverns** | Underground | Gemstones, mineral deposits, cave fungi | Fragile terrain (bag damage from falls) | Quest unlock |
| **The Void** | Extreme/endgame | Null essence, paradox stone, void salt | All hazards, aspects behave strangely | Mastery unlock |

Regions can be accessible before being "officially" unlocked, but environmental hazards make them extremely difficult without the appropriate gear/progression. This rewards persistent/skilled players who want to venture into danger early.

## Save System (Full Game)

### Auto-Save
- Auto-save after each encounter during a venture (Slay the Spire style)
- If the player quits mid-venture, they resume at the last encounter
- Restarting an encounter on resume allows some flexibility (but not full save-scumming across the whole venture)

### Save Slots
- Multiple save slots: at least 3, or unlimited if the UI can be clean
- Each slot is an independent "account" with its own progression
- Only one active save per slot (no branching saves)

### Save Data Includes
- Character progression (stamina upgrades, alchemical effects, codex)
- Bag collection
- Home base state (storage, upgrades)
- Current venture state (if mid-venture)
- Quest log
- Daily puzzle history and streak

## Related Documents

- `docs/core-mechanics.md` — Grid, bags, reagents that the venture wraps around
- `docs/chemistry-system.md` — The aspect interactions that make inventory management risky
- `docs/mvp-specification.md` — The daily puzzle that precedes venture implementation
- `docs/art-and-style.md` — Visual representation of ventures, character, map
- `docs/architecture.md` — Technical structure for save system, map generation, etc.
