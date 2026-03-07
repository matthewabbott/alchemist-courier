# Art Generation Experiment Plan

This document captures the experiment design for finding prompt strategies that produce inventory sprites which are readable at small sizes, aesthetically pleasing, and loosely mappable to polyomino shapes.

See also: `docs/art-and-style.md` (golden recipe), `docs/item-catalog-design.md` (target items).

---

## Goal

Find a RetroDiffusion prompt strategy that produces sprites which are:
1. **Visually comprehensible at 16x16px** (half a grid cell)
2. **Aesthetically pleasing** in the kells-alchemy style
3. **Loosely mappable** to polyomino shapes (the art suggests the shape, hover frames confirm it)

## Common Settings

```
Style:     kells-alchemy (user_style_f1887992a9e54751ba878b8c5a0ef3b8)
Model:     rd_fast
Remove BG: true
Variants:  3 per prompt
```

## Experiment A — Loose Prompts (No Shape Direction)

Pure golden recipe. No shape hints at all. Baseline for quality at each resolution.

### Prompts

**Cold (Snowflower):**
```
A snowflower alchemical reagent, a crystalline frost flower with ice-blue
petals and silver stems, delicate frozen botanical specimen, cool blues
and whites with hints of pale violet, single item centered
```

**Hot (Firevine):**
```
A firevine alchemical reagent, a curling smoldering vine with glowing
embers and small flame-like leaves, warm reds and oranges with gold
highlights, botanical illustration style, single item centered
```

### Sizes
- 128x128 (proven good — reference baseline)
- 64x64 (middle ground — new test)
- 32x32 (cell-native — can the style survive?)

### What We're Measuring
- Does art quality degrade at 64x64 or 32x32?
- When downscaled to 16x16, can you still tell what it is?
- What natural shapes do the generations suggest?

---

## Experiment B — Guided Prompts (Gentle Shape Hints)

Golden recipe plus natural-language shape suggestions. Never explicit grid/polyomino language.

### Prompt Template
```
[Golden prompt base], [shape suggestion woven in naturally]
```

### Shape Suggestions (Cold aspect)

**Domino:**
```
A snowflower alchemical reagent, two crystalline frost blooms connected
by a delicate ice-covered stem, cool blues and whites with hints of pale
violet, botanical pair specimen, single item centered
```

**L-triomino:**
```
A frost fern alchemical reagent, a branching ice frond with a smaller
bud splitting off to one side, cool blues and silver, frozen botanical
specimen, single item centered
```

**I-triomino:**
```
A frost fern alchemical reagent, three delicate ice fronds growing in
a straight line from a single root, cool blues and whites, frozen
botanical specimen, single item centered
```

**T-tetromino:**
```
A frost fern alchemical reagent, three ice fronds spreading horizontally
from a central stem with one frond drooping below, crystalline blues
and silver, frozen botanical specimen, single item centered
```

### Shape Suggestions (Hot aspect)

**Domino:**
```
A firevine alchemical reagent, two smoldering flame buds connected by
a curling ember vine, warm reds and oranges with gold highlights,
botanical illustration style, single item centered
```

**L-triomino:**
```
A magma slug alchemical reagent, a small molten creature curled with
its tail bending to one side, warm oranges and deep reds with glowing
highlights, single item centered
```

### What We're Measuring
- Does shape guidance help or hurt art quality?
- Do the generated shapes actually match the suggested polyomino?
- Is the shape readable at 16x16?

---

## Experiment C — Extensive Prompts (Full Context)

Explain the game concept, why shapes matter, what we're going for. Let the AI interpret.

### Prompt Template
```
An alchemical reagent for an inventory puzzle game inspired by the Book
of Kells. This item should be visually distinctive at very small sizes
with bold outlines and a clear silhouette. It represents a [item name]
— [description]. The overall shape of the item should loosely suggest
[shape in natural language] so that players can intuit its grid footprint
at a glance, but artistic beauty is more important than geometric
precision. [Color palette], botanical illustration style, single item
centered.
```

### Example (Cold I-triomino)
```
An alchemical reagent for an inventory puzzle game inspired by the Book
of Kells. This item should be visually distinctive at very small sizes
with bold outlines and a clear silhouette. It represents a Frost Fern
— delicate frozen fronds that grow along windowpanes in alchemist
workshops. The overall shape of the item should loosely suggest three
elements arranged in a vertical line so that players can intuit its
grid footprint at a glance, but artistic beauty is more important than
geometric precision. Cool blues, whites, and silver with hints of pale
violet, botanical illustration style, single item centered.
```

### What We're Measuring
- Does full context improve or confuse the AI?
- Is the extra prompt length worth it?
- Does explaining "why" lead to better shape conformance?

---

## Resolution Findings (to be filled in)

| Resolution | Quality | Readability at 16px | Notes |
|-----------|---------|-------------------|-------|
| 128x128 | | | |
| 64x64 | | | |
| 32x32 | | | |

## Prompt Strategy Findings (to be filled in)

| Strategy | Quality | Shape Match | 16px Readability | Notes |
|----------|---------|-------------|-----------------|-------|
| A (loose) | | | | |
| B (guided) | | | | |
| C (extensive) | | | | |

## Best Approach (to be filled in)

_After experiments: document which strategy + resolution combo produces the best results._
