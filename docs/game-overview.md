# Alchemist Courier — Game Overview

## What Is This Game?

**Alchemist Courier** is a roguelite/progressive puzzle game about inventory tetris with alchemically reactive reagents. The player is an alchemist's apprentice who ventures into a fantasy wilderness to gather reagents, managing a grid-based inventory where items have shapes, weights, and alchemical aspects that interact with each other — sometimes helpfully, sometimes explosively.

The core fantasy: you're a courier/gatherer trying to fit as much valuable (and volatile) cargo into your bags as possible without things reacting catastrophically, while managing your stamina to get home safely.

## Working Title

**Alchemist Courier** (formerly "Mass Flux" in early drafts)

## Genre

Singleplayer roguelite puzzle game. Combines:
- **Inventory tetris** (Resident Evil 4, Backpack Hero)
- **Roguelite venture structure** (Slay the Spire encounter maps)
- **Progressive permanent upgrades** (Pokemon Mystery Dungeon, incremental games)
- **Spatial puzzle solving** (Tetris, packing puzzles)

## Tone

**Cozy core, earnest lore.** The surface is cute and approachable — chibi pixel art, friendly UI, lighthearted consequences. But the codex entries, world lore, and alchemy system have genuine depth and atmosphere. Think: Stardew Valley's warmth with Book of Hours' mystical reverence for its subject matter.

Failure is not punishing in a "you're bad" way — it's more like "oops, that was ambitious, let's try a different approach." The game respects the player's time while still making mastery feel earned.

## Inspirations

| Game | What We Take From It |
|------|---------------------|
| **Backpack Hero / Backpack Battler** | Grid inventory as the core gameplay mechanic. Bags with special properties. Items interacting based on arrangement. |
| **Resident Evil 4** | Grid-based inventory where items have shapes and must be physically arranged to fit. |
| **Slay the Spire** | Branching encounter map structure. Venture/run-based progression. Encounter variety. Auto-save after encounters. |
| **Recettear** | Dispatching into dungeons to gather resources. Side-view character aesthetic. Lighthearted tone with mechanical depth. |
| **Pokemon Mystery Dungeon** | Accumulating permanent character progression across runs. Home base customization. "Hunger"/stamina limiting dungeon time. Cozy + earnest tone. |
| **Breath of the Wild** | Procedural chemistry system — elements interact according to general rules rather than hand-scripted events. |
| **Book of Hours** | Aspect/quality system with genuine alchemical and mystical depth. |
| **Peak** | Stamina penalties from environmental effects (cold locking out stamina chunks). |
| **Core Keeper / Stardew Valley** | Cozy base-building, pixel art aesthetic, satisfying collection loops. |

## Tech Stack

| Component | Choice | Notes |
|-----------|--------|-------|
| **Engine** | Godot 4.6 | Open source, good 2D support, HTML5 export |
| **Language** | C# (.NET) | Strong typing suits complex game systems (chemistry engine, inventory logic) |
| **Art** | RetroDiffusion | AI pixel art generation. Placeholder-first workflow: colored rectangles initially, replaced iteratively |
| **Programming** | Claude Code | AI-assisted development |
| **Platform (MVP)** | Web (HTML5) | Godot web export, hostable on itch.io or personal site |
| **Platform (Full)** | Windows PC | Primary desktop target |

## Monetization

Free web game / portfolio piece. No monetization pressure. The daily puzzle serves as both the MVP and a public-facing hook. The full game (with ventures, progression, base management) may be released as pay-what-you-want on itch.io if it matures enough.

## Target Audience

- Puzzle game players who enjoy spatial reasoning challenges
- Fans of roguelite deckbuilders / inventory management games
- Incremental game players who want short, delineated "runs" with permanent progression
- Players who enjoy cozy games with hidden mechanical depth

## MVP Definition

**The MVP is a web-exportable daily puzzle game** (think Wordle, Enclosure):
- Procedurally generated inventory puzzles using a daily seed (everyone gets the same puzzle)
- Tutorial progression that teaches the rules incrementally
- Multiple puzzle types: fit all reagents, maximize value, meet requirements
- Hostable on itch.io or personal website
- The daily puzzle IS the first public-facing product

See `docs/mvp-specification.md` for full details.

## Post-MVP Roadmap

Each stage builds on the previous:

1. **Daily Web Puzzle** (MVP) — Core inventory tetris + chemistry system + procedural generation
2. **Puzzle Progression Mode** — Curated/generated puzzle sequences as tutorial and challenge content
3. **Venture Loop** — Slay the Spire encounter map, stamina system, gather-and-return gameplay
4. **Quest System** — Specific objectives driving ventures (retrieve X fire reagents, etc.)
5. **Base Management** — Home base with grid-based storage, upgrades, permanent progression
6. **Regions & Story** — Multiple biomes, narrative, NPC interactions
7. **Advanced Alchemy** — Multiple schools of alchemy, complex interactions, brewing/mixing

See `docs/roadmap.md` for the complete feature roadmap with tier classifications and detailed cross-references. For detailed puzzle type designs across all tiers (optimization types, destruction/transmutation, brewing), see `docs/puzzle-design.md`.

## Key Design Pillars

1. **The puzzle IS the game.** Every other system exists to create interesting puzzle scenarios. If the inventory tetris isn't fun, nothing else matters.
2. **Flavor-first design.** Alchemical interactions should feel like they make sense in-fiction before they make sense as game balance. "How would this work in 'real' alchemy?" comes before spreadsheet optimization.
3. **Depth through emergence.** Simple rules combining to create complex situations, rather than complex rules. The chemistry system should produce surprising interactions from straightforward components.
4. **Cozy progression.** Failure is a setback, not a punishment. The player always has something to show for their time. Permanent progression makes every venture feel worthwhile.
5. **Approachable surface, rich interior.** The game should be immediately playable by anyone who's played Tetris, but reward deep engagement with its alchemical systems.

## Related Documents

- `docs/core-mechanics.md` — Grid system, bags, reagents, adjacency rules
- `docs/chemistry-system.md` — Aspect schools, interaction rules, the chemistry engine
- `docs/mvp-specification.md` — Daily puzzle MVP specification
- `docs/roadmap.md` — Complete feature roadmap (MVP through DLC) with tier classifications
- `docs/venture-and-progression.md` — Post-MVP venture loop, stamina, quests, base
- `docs/art-and-style.md` — Visual direction, RetroDiffusion, UI, audio
- `docs/puzzle-design.md` — Puzzle types, scoring, and progression across all tiers
- `docs/architecture.md` — Technical architecture, data models, C# patterns
