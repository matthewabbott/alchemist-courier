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
- 256x256 (max allowed for kells-alchemy)
- 128x128 (proven good — reference baseline)
- 96x96 (minimum allowed for kells-alchemy)

> **Resolution constraint discovered:** The kells-alchemy custom style enforces a minimum width of 96px and maximum of 256px. 64x64 and 32x32 are NOT possible with this style. Inventory sprites must be generated at 96+ and downscaled.

### What We're Measuring
- Does art quality degrade at 96x96?
- Does 256x256 add useful detail or introduce artifacts?
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

## Resolution Findings

> **Key constraint:** kells-alchemy style enforces min 96px, max 256px. 64x64 and 32x32 are not possible.

| Resolution | Quality | Notes |
|-----------|---------|-------|
| 256x256 | ★★★★★ | Most detail. BUT: tends to generate text labels ("FIRE VINE REAGENT"), alchemical annotations (Arabic-like script, mystical symbols), or ornamental backgrounds. Treats the canvas as a manuscript page. Great for codex art but needs cleanup for game sprites. |
| 128x128 | ★★★★★ | **Sweet spot.** Consistently beautiful, clean compositions, reliable background removal. Proven golden recipe size. Best balance of detail and clean output. |
| 96x96 | ★★★★ | Still good! Slightly less detail but readable. Occasionally generates ornamental backgrounds (dark blue illuminated borders) that resist background removal. 1 in 3 had this issue. |

**Recommendation:** Generate at **128x128** for inventory sprites, **256x256** for codex art (with manual cleanup). 96x96 is usable but 128 is more reliable.

---

## Prompt Strategy Findings

| Strategy | Quality | Shape Match | Notes |
|----------|---------|-------------|-------|
| A (loose) | ★★★★★ | N/A (no shape target) | Best pure art quality. Items are gorgeous but take whatever shape the AI wants. Monominos and items without shape needs should use this. |
| B (guided) | ★★★★★ | ★★★★ for dominoes/L-shapes, ★★★ for I-shapes | Shape hints woven naturally into visual descriptions work very well for "pair" and "branching" shapes. Struggles with "straight line" — AI interprets as "fan/spread." Art quality is NOT degraded by guidance. |
| C (extensive) | ★★★★ | ★★★★★ for bold shapes (T, cross) | Full context works best for distinctive shapes (T-tetromino Blight Moss was near-perfect). More ornamental/Celtic results. But extra context can dilute shape hints for subtle shapes — Frost Fern I-tri was less successful than B. |

### Detailed Shape Observations

**Shapes that work well with guided prompts (B):**
- **Domino** — "two [things] connected by a [connector]" → consistently produces pairs. Both snowflower and firevine nailed it.
- **L-triomino** — "branching with a bud splitting off to one side" → produces clear L-ish compositions. Frost fern and magma slug both worked.
- **T-tetromino** — "three spreading horizontally with one drooping below" → produced a near-perfect T (1 of 3 variants). Best result in Experiment B.

**Shapes that need the extensive prompt (C):**
- **T-tetromino** — Blight Moss with "cross or T-shape" in extensive prompt produced 2/3 near-perfect T-shapes. Best T-shape results overall.
- Any shape where you can describe the overall silhouette rather than counting elements.

**Shapes that are hard regardless:**
- **I-triomino** — "three in a straight line" or "vertical line" tends to produce fans/tridents (B) or single compound leaves (C). The AI doesn't like making things in a strict line. May need to approach differently: "a tall narrow stalk" rather than "three things in a line."

### Per-Item Highlights

| Item | Aspect | Prompt Style | Result |
|------|--------|-------------|--------|
| Snowflower | Cold | A (loose) | Gorgeous crystalline frost flowers. Natural monomino/domino shapes. |
| Firevine | Hot | A (loose) | Stunning curling vines with flame leaves. Vertical compositions. |
| Ember Seed | Hot | A (loose) | Perfect monomino — glowing cracked coal. High contrast, reads well at any size. |
| Sharpstone | Sharp | A (loose) | Angular purple crystals with gold veins. Naturally elongated (domino). |
| Crystal Tear | Fragile | A (loose) | Translucent iridescent teardrops. Delicate and ethereal. Perfect monomino. |
| Thunder Egg | Volatile | A (loose) | Crackling geode with lightning energy. Round/compact — natural o-tet. |
| Snowflower pair | Cold | B (domino) | Two frost blooms on a connecting stem. Clear domino shape. |
| Frost Fern | Cold | B (L-tri) | Branching frond with side bud. Readable L-shape. |
| Frost Fern | Cold | B (I-tri) | Fan/trident instead of line. Shape guidance partially failed. |
| Frost Fern | Cold | B (T-tet) | 1/3 variants had clear T-shape. Moderate success. |
| Firevine pair | Hot | B (domino) | Two flame buds on curling vine. Excellent domino. |
| Magma Slug | Hot | B (L-tri) | Curled molten creature. More C-shape than L but gorgeous art. |
| Frost Fern | Cold | C (I-tri) | Ornamental fern leaves. Shape guidance diluted by extra context. |
| Blight Moss | Toxic | C (T-tet) | **Best T-shape result!** 2/3 variants were near-perfect T silhouettes. |

---

## Best Approach

### Recommended Workflow

**Use different prompt strategies for different needs:**

1. **Monominos and shape-agnostic items → Strategy A (loose)**
   - Pure golden recipe produces the best art quality
   - Let the AI create freely, assign shape after
   - Best for: Ember Seed, Crystal Tear, Frozen Teardrop, Venom Drop, etc.

2. **Dominoes and L-shapes → Strategy B (guided)**
   - Natural-language shape hints work well without degrading quality
   - "Two [things] connected" for dominoes, "branching to one side" for L-shapes
   - Best for: Firevine (domino), Frost Fern (L-tri), Snowflower (domino)

3. **T-shapes and distinctive silhouettes → Strategy C (extensive)**
   - Full context helps when you need a specific bold silhouette
   - Works best when the shape can be described as an overall form ("cross", "T-shape")
   - Best for: Blight Moss (T-tet), items with very specific shape needs

4. **I-shapes (straight lines) → Needs more experimentation**
   - Neither B nor C reliably produced straight-line arrangements
   - Try: "a tall narrow vertical stalk" rather than "three things in a line"
   - May need to accept that some shapes require more generation attempts and curation

### Resolution Strategy

- **128x128** for all inventory sprite generation (sweet spot)
- **256x256** for codex art / detailed illustrations (expect text/annotation artifacts)
- Downscale from 128→32 for in-game cells, 128→16 for half-cell items
- 96x96 as fallback if 128 feels too detailed for some items

### Generation Budget

At ~$0.66 per batch of 3 variants (based on observed balance changes), generating the full 24-item catalog at 128x128 with 3 variants each would cost approximately:
- 24 batches × $0.66 = **~$16** for a first pass
- With re-rolls and experiments, budget **~$25-30** for the full catalog
- Current remaining balance: ~$32
