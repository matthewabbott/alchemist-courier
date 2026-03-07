# Item Catalog Design

This document describes the item-first reagent catalog: design philosophy, shape coverage targets, and a starter set of unique items. This is a **living document** — items will evolve through human-in-the-loop RetroDiffusion art generation.

See also: `docs/art-and-style.md` (art direction), `docs/core-mechanics.md` (reagent system), `docs/chemistry-system.md` (aspect rules).

---

## Design Philosophy

### Item = One Shape

Each reagent in the catalog is a **unique item with ONE natural polyomino shape.** The old model (8 species x 9 shapes = 72 entries) is replaced by ~20-30 distinct items, each with a shape that naturally fits its visual design.

**Why this is better:**
- **Art quality** — Freeform prompts produce gorgeous results; shape-forced prompts degrade quality
- **Player comprehension** — Each item looks genuinely different, rather than "the same flower but bent differently"
- **Game authenticity** — A real alchemist's inventory has diverse items, not one herb in 9 sizes
- **Shape communication** — Hover frames show exact cell occupancy; the art only needs to loosely suggest the shape

### Art-First Design Process

Items are not designed in a spreadsheet and then illustrated. The process is:

1. **Generate** — Create beautiful freeform art with loose prompts using the golden recipe (see `docs/art-and-style.md`)
2. **Curate** — Select the most aesthetically pleasing results
3. **Assess** — Determine which polyomino shape the art naturally suggests
4. **Assign** — Give the item its shape, name, and aspect based on what it looks like
5. **Iterate** — If a needed shape has no good art, try gentle prompts with shape suggestions (e.g., "branching fronds in a line" for I-tri). Never demand exact compliance.

If a prompt consistently produces beautiful art that doesn't match any needed shape, keep the art and assign it to whatever shape it naturally suggests. Then find a different approach for the shape you still need.

---

## Shape Coverage Targets

Each of the 8 Simple-tier aspects needs items covering a variety of shapes. The goal is full coverage of all 9 standard polyomino shapes across the catalog, with each aspect having 2-3 items.

### Standard Polyomino Shapes

| Shape | Size | Rarity | Notes |
|-------|------|--------|-------|
| monomino | 1 cell | Common | Every aspect should have at least one |
| domino | 2 cells | Common | Natural "pair" shape, very placeable |
| i_tri | 3 cells | Uncommon | Straight line of 3 |
| l_tri | 3 cells | Uncommon | L-bend, 3 cells |
| i_tet | 4 cells | Rare | Straight line of 4, awkward to place |
| o_tet | 4 cells | Rare | 2x2 square, compact |
| t_tet | 4 cells | Rare | T-shape |
| s_tet | 4 cells | Rare | Zigzag (S or Z) |
| l_tet | 4 cells | Rare | L-shape, 4 cells |

**Target distribution:** Every aspect gets 1 monomino + 1 domino + 1 larger piece. The larger pieces are distributed so all 9 shapes appear in the catalog.

---

## Starter Catalog

**These items are provisional.** Names, shapes, and even which items exist will evolve as art is generated. This table provides a starting point for the code refactor.

### Hot Aspect

| Item | Shape | Visual Concept | Flavor |
|------|-------|---------------|--------|
| Ember Seed | monomino | Glowing coal/seed with inner fire | A smoldering seed that radiates gentle warmth. Handle with tongs. |
| Firevine | domino | Curling vine with flame-like leaves | A climbing vine that perpetually smolders, leaving scorch marks wherever it grows. |
| Magma Slug | l_tri | Molten creature curled in an L | A slow-moving alchemical slug whose trail glows like cooling lava. |

### Cold Aspect

| Item | Shape | Visual Concept | Flavor |
|------|-------|---------------|--------|
| Frozen Teardrop | monomino | Single crystalline ice drop | A tear shed by a frost spirit, permanently frozen mid-fall. |
| Snowflower | domino | Crystalline frost bloom with ice-blue petals | A blue snowflake-shaped bloom that never thaws, even in summer. |
| Frost Fern | i_tri | Branching ice fronds in a line | Delicate frozen fronds that grow along windowpanes in alchemist's workshops. |

### Volatile Aspect

| Item | Shape | Visual Concept | Flavor |
|------|-------|---------------|--------|
| Spark Petal | monomino | Unstable glowing bud | A single petal that sparks when disturbed. Keep away from heat sources. |
| Burstbloom | l_tri | Heat-sensitive bloom with swollen pods | A bloom from the foothills that detonates when warmed. Prized for controlled demolition. |
| Thunder Egg | o_tet | Explosive geode, compact and round | A mineral egg that cracks with tremendous force when its internal pressure is released. |

### Toxic Aspect

| Item | Shape | Visual Concept | Flavor |
|------|-------|---------------|--------|
| Venom Drop | monomino | Toxic droplet, vivid green | Concentrated essence from a venomous bloom. One drop is enough. |
| Purple Creeper | domino | Trailing poison vine | A venomous trailing plant with poisonous sap that stains everything purple. |
| Blight Moss | t_tet | Spreading toxic fungus in a T-shape | A creeping moss that poisons the soil wherever it takes root. Spreads aggressively. |

### Heavy Aspect

| Item | Shape | Visual Concept | Flavor |
|------|-------|---------------|--------|
| Lead Nugget | monomino | Dense metal piece | A small lump of alchemical lead, deceptively heavy for its size. |
| Lead Ingot | domino | Standard metal bar | Dense alchemical metal, prized for its weight in counterbalancing reactions. |
| Anvil Shard | l_tet | Heavy angular fragment | A broken piece of an alchemist's anvil. Still useful as ballast. |

### Stable Aspect

| Item | Shape | Visual Concept | Flavor |
|------|-------|---------------|--------|
| Calm Stone | monomino | Soothing pebble with soft glow | A river stone imbued with stabilizing essence. Soothes nearby reactions. |
| Nullflower | domino | Dampening bloom, serene gray-blue | A calm blossom that absorbs reactive energy from its surroundings. |
| Ironbark Root | i_tri | Steady wooden root, straight and solid | A section of ironbark tree root. Its steadiness dampens chemical volatility. |

### Sharp Aspect

| Item | Shape | Visual Concept | Flavor |
|------|-------|---------------|--------|
| Glass Shard | monomino | Cutting fragment with keen edges | A razor-sharp sliver of alchemical glass. Cuts through almost anything. |
| Sharpstone | domino | Faceted crystal with metallic glints | Jagged crystalline mineral with cutting edges that never dull. |
| Razor Fern | s_tet | Zigzag blade fern | A fern whose fronds are edged like saw blades. Even the wind avoids it. |

### Fragile Aspect

| Item | Shape | Visual Concept | Flavor |
|------|-------|---------------|--------|
| Crystal Tear | monomino | Delicate ice-like drop | A hollow crystal that shatters at the slightest pressure. Beautiful but doomed. |
| Glasswork | domino | Blown glass vessel, pale lavender | Delicate blown glass containing trapped starlight. Handle with extreme care. |
| Gossamer Thread | i_tet | Fragile thread line, ethereal | Spun from spider silk and moonlight. Stronger than it looks, but not by much. |

### Coverage Summary

| Shape | Items | Aspects |
|-------|-------|---------|
| monomino | 8 | All (one each) |
| domino | 8 | All (one each) |
| i_tri | 3 | Cold, Stable, (free slot) |
| l_tri | 2 | Hot, Volatile |
| i_tet | 1 | Fragile |
| o_tet | 1 | Volatile |
| t_tet | 1 | Toxic |
| s_tet | 1 | Sharp |
| l_tet | 1 | Heavy |
| **Total** | **24** | All 9 shapes covered |

---

## Evolution Strategy

As RetroDiffusion art is generated:

1. **Keep what works** — If a generated snowflower naturally looks like a triomino instead of a domino, change its shape assignment
2. **Invent new items** — If we need more l_tri coverage and a hot prompt produces something L-shaped but doesn't look like a slug, give it a new name
3. **Retire items** — If an item consistently fails to generate well, replace it
4. **Add variety** — The 24-item starter set can grow. Some aspects might end up with 4-5 items if good art emerges
5. **Multi-aspect items** (future) — Post-MVP, items with 2+ aspects per cell become possible. These are entirely new catalog entries.

The code should make it easy to add, remove, and reassign items. The ReagentCatalog should be data-driven enough that changing the item list doesn't require refactoring the puzzle generator or test infrastructure.
