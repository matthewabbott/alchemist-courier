# Art References — Historical Manuscript Sources

## 1. Overview

Alchemist Courier draws its visual identity from real historical alchemy and herbal manuscripts. This is not just aesthetic window-dressing — the game's "earnest lore" design pillar (see `docs/game-overview.md`) means that the alchemical subject matter is treated with genuine reverence, even as the surface tone stays cozy and approachable.

Historical manuscripts provide the bridge between authenticity and style. Their hand-drawn botanical illustrations, alchemical diagrams, and symbolic woodcuts ground the game's pixel art in a real visual tradition. When a reagent sprite looks like something a medieval herbalist might have drawn (just pixel-ified and cute-ified), it reinforces the feeling that this is a world where alchemy *works* — not a world where alchemy is a thin coat of paint over generic fantasy.

The sources cataloged here serve as reference material for the RetroDiffusion custom style pipeline (see ART.1, ART.5) and as a living reference for human creative direction.

## 2. Source Catalog

### Vienna Dioscurides (Codex Vindobonensis)
- **Date:** circa 512 CE (6th century)
- **Origin:** Byzantine Constantinople; now held at the Austrian National Library, Vienna
- **Illustration count:** 400+ full-color botanical illustrations
- **Access:**
  - Wikimedia Commons — [Category:Codex Vindobonensis](https://commons.wikimedia.org/wiki/Category:Vienna_Dioscurides) (162+ files)
  - PICRYL — [Vienna Dioscurides collection](https://picryl.com/collections/vienna-dioscurides)
- **License:** Public domain
- **Notes:** The earliest and most lavish surviving illustrated herbal. Botanical illustrations are remarkably naturalistic for their era — plants shown with roots, flowers, and fruit in careful detail. Gold-standard reference for reagent sprites.

### Badianus Codex (Libellus de Medicinalibus Indorum Herbis)
- **Date:** 1552
- **Origin:** Aztec; created at the Colegio de Santa Cruz de Tlatelolco, Mexico. Now held by INAH (Instituto Nacional de Antropologia e Historia), Mexico City
- **Illustration count:** 204 herb/plant illustrations
- **Access:**
  - INAH digitization (2021) — [Libellus de Medicinalibus Indorum Herbis](https://mediateca.inah.gob.mx/repositorio/islandora/object/codice:851)
- **License:** Public domain (16th century work)
- **Notes:** The first medical text from the Americas. Unique cross-cultural perspective — Aztec herbal knowledge recorded in Latin with Nahuatl plant names. Illustrations have a distinctive flat, decorative style with vivid earth-tone coloring. Excellent source for non-European reagent inspiration.

### Splendor Solis
- **Date:** 1582
- **Origin:** German alchemical treatise; finest copy held at the British Library (Harley MS 3469)
- **Illustration count:** 22 full-page alchemical illustrations
- **Access:**
  - Internet Archive — [Splendor Solis](https://archive.org/details/splendor-solis)
  - British Library digitized manuscripts
- **License:** Public domain
- **Notes:** Widely considered the most beautiful alchemical manuscript ever produced. Rich allegorical scenes depict the stages of the Great Work (opus magnum) with vivid colors and elaborate symbolism. The flask-within-a-scene compositions are ideal reference for UI backdrops and codex page art.

### Rosarium Philosophorum
- **Date:** 16th century (earliest printed edition 1550)
- **Origin:** European alchemical tradition; multiple institutional holdings
- **Illustration count:** 20 alchemical woodcuts
- **Access:**
  - Various institutional digitizations (Bayerische Staatsbibliothek, Wellcome Collection)
  - Wikimedia Commons — [Category:Rosarium philosophorum](https://commons.wikimedia.org/wiki/Category:Rosarium_philosophorum)
- **License:** Public domain
- **Notes:** Symbolic woodcut series depicting the alchemical marriage (coniunctio) and transformation stages. The stark black-and-white woodcut style translates exceptionally well to pixel art — clean lines, strong silhouettes, symbolic clarity. Good reference for menu art, chapter headers, and achievement icons.

### Apuleius Herbarium (Herbarium Apuleii Platonici)
- **Date:** Medieval (surviving copies from 6th–12th centuries; text attributed to 5th century)
- **Origin:** Late Roman/early medieval Europe; multiple surviving manuscripts across European libraries
- **Illustration count:** Varies by copy; typically 100+ botanical illustrations
- **Access:**
  - Various institutional digitizations (Bodleian Library, Leiden University Library, Florence Laurentian Library)
  - Wikimedia Commons — search "Herbarium Apuleii"
- **License:** Public domain
- **Notes:** One of the most copied herbals of the medieval period. Illustrations range from naturalistic (earlier copies) to highly stylized (later copies where artists copied copies). The stylized late-medieval versions — with their flattened perspective and bold outlines — are particularly useful as pixel art references.

## 3. Mapping to Game Elements

| Source | Inspires | Game Elements |
|--------|----------|---------------|
| **Vienna Dioscurides** | Reagent sprites | Botanical reagents (herbs, roots, flowers, fungi). The naturalistic detail provides anatomical accuracy even at 32x32. |
| **Badianus Codex** | Reagent sprites | Exotic/non-European reagents. The flat decorative style suits pixel art directly. Earth-tone palette reference. |
| **Splendor Solis** | UI backdrops, codex art | Flask compositions for puzzle screen backgrounds. Allegorical scenes for codex/lore pages. Color palette reference for UI theming. |
| **Rosarium Philosophorum** | Menu art, chapter headers | Woodcut-style title screens, achievement icons, loading screens. The symbolic visual language (king/queen, sun/moon, flasks) maps to in-game alchemy concepts. |
| **Apuleius Herbarium** | Reagent sprites, aspect icons | Stylized late-medieval copies provide simplified plant forms ideal for pixel art translation. Root/flower/leaf patterns for aspect iconography. |

### Cross-cutting themes
- **Alchemical symbols** (from Splendor Solis, Rosarium) → aspect icons, school emblems, UI decorative elements
- **Botanical accuracy** (from Vienna Dioscurides, Badianus) → reagent sprites that feel "real" even in pixel form
- **Medieval page layout** (from all sources) → codex/journal UI design, bordered panels, illuminated initial letters

## 4. Style Translation Notes

### What to preserve
- **Hand-drawn quality:** Slight irregularities, organic line weight variation. Pixel art should feel crafted, not machine-generated.
- **Earth-tone palettes:** Ochres, umber, verdigris, madder red, indigo. Historical manuscripts used natural pigments — the resulting palette is warm, muted, and cohesive.
- **Botanical accuracy:** Real plant morphology (leaf arrangements, flower structures, root systems) should be recognizable even at small scale. Players who know botany should see it reflected.
- **Alchemical symbolism:** Sun/moon duality, elemental triangles, the ouroboros, transformation stages. These symbols carry meaning in the game's lore system.
- **Page composition:** Bordered illustrations, text-and-image integration, marginal annotations. These inform codex and journal UI design.

### What to adapt
- **Pixel-art-ify at 32x32:** Reduce to essential silhouette and 2-3 key details. A medieval herb illustration with 50 leaves becomes a pixel sprite with a recognizable leaf cluster.
- **Modernize for readability:** Historical manuscripts were meant for slow study at arm's length. Game sprites must read instantly at screen distance. Increase contrast, simplify overlapping elements, strengthen silhouettes.
- **Cute-ify for cozy tone:** The game's surface tone is cozy and approachable. Reagent sprites should feel friendly — rounder forms, slightly oversized features (flowers, eyes on creatures), warm highlight colors. Think: a medieval herbalist's drawing reimagined by someone who also loves Stardew Valley.
- **Consistent scale and framing:** Historical sources vary wildly in scale and composition. Game sprites need uniform 32x32 framing with consistent padding, grounding, and facing direction.
- **Readable at small size:** Unlike manuscript pages meant for close study, game sprites appear alongside dozens of others in an inventory grid. Prioritize distinctiveness over detail.

## 5. RetroDiffusion Integration

These manuscript sources feed into the RetroDiffusion custom style pipeline:

1. **Custom style creation (ART.5):** Human selects 5-9 reference images from the sources cataloged above. These are passed as `reference_images` to the RetroDiffusion `create_custom_style` tool (set up in ART.1). The selected images should span the range of desired qualities — botanical accuracy from Vienna Dioscurides, flat decorative style from Badianus, earth-tone palette from multiple sources.

2. **Style iteration:** The custom style is tested against 3-5 reagent prompts (e.g., "mushroom," "crystal," "flame herb") and refined by human review. The goal is a style that produces sprites feeling both historically grounded and game-ready.

3. **Style recording:** Approved style parameters and the `style_id` are recorded in `art/retrodiffusion-styles.json`. Reference images used are documented alongside the style for reproducibility.

4. **Ongoing generation:** Once the custom style is approved, all future reagent sprite generation uses it as the base style, ensuring visual consistency across the full reagent catalog.

### Reference image selection guidance
When selecting reference images for style creation, aim for:
- **Variety of plant types** — flowers, roots, fungi, leaves, fruits — so the style generalizes
- **Consistent quality** — avoid mixing high-detail naturalistic images with crude late copies
- **Representative palette** — images that collectively demonstrate the desired earth-tone range
- **Clear subjects** — isolated plant illustrations work better than complex multi-figure scenes

Human creative direction drives all aesthetic decisions. The agent's role is to facilitate generation, organize outputs, and document results.
