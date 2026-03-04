# Session: Design Audit & Ralph Loop Prep
**Date:** 2026-03-04
**Branch:** `dev`

## Summary

Major design iteration session. Started with a brainstorm comparing the implemented mechanics against the designer's vision, identified parasitic aspects and underspecified mechanics, then rewrote the design docs and PRD to fix everything before the next Ralph loop run.

The core insight: the original aspect design had Sharp and Fragile as "parasitic" (only meaningful when paired with each other). We de-parasitized them by giving each aspect a standalone primary mechanic, then organized all aspects and bag types into three complexity tiers (Simple, Intermediate, Advanced). The designer also clarified that complexity has **two independent dimensions** — Scale (puzzle size) and Mechanical Depth (number of unique interactions) — and that all standalone/daily puzzles should use Simple mechanics regardless of difficulty.

A pre-Ralph audit then uncovered 3 bugs in existing code (Null suppression unimplemented, wrong MvpAspects, inconsistent Stable buffering) plus doc drift (stale Godot versions, undefined "bag orientation", Intermediate aspects in Simple-only tutorial). All fixed in docs/PRD.

Also hardened the Ralph loop script with rate-limit detection and graceful abort.

## Changes

**Design docs (major rewrites):**
- `docs/chemistry-system.md` — Added two-dimensional complexity model, rewrote Physical school aspect table with 12 aspects organized by tier, added de-parasitization notes, Volatile net-hot spec, aspirational aspects section
- `docs/core-mechanics.md` — Expanded bag type specs by complexity tier (Simple: Normal/Null/Gravity/Locked, Intermediate: Reinforced/Insulated/Fragile, Advanced: Body), added protrusion system aspirational section, Body bag safety constraint
- `docs/mvp-specification.md` — Referenced Simple complexity as MVP target, fixed tutorial lesson 11+ to use Simple-tier aspects only
- `docs/architecture.md` — Added complexity tier annotations to SchoolType
- `docs/game-overview.md`, `docs/architecture.md`, `docs/mvp-specification.md` — Fixed stale Godot 4.3 references to 4.6

**PRD updates:**
- `prd.json` — Added 10 new tasks (B.1-B.4 bag mechanics, A.1-A.6 aspect rework), then added 3 priority-0 bugfix tasks (R-3 Null suppression, R-4 MvpAspects, R-5 Stable normalization), rewrote 8 task descriptions for precision, fixed 6 dependency chains

**Ralph loop hardening:**
- `scripts/ralph/ralph.ps1` — Rate-limit detection (regex on output for 429/overloaded/throttle), consecutive error tracking (abort after 3), graceful termination with context logging

## Design Decisions

**Two-dimensional complexity model**: Scale (spatial pressure from puzzle size) and Mechanical Depth (number of distinct interactions) are independent. One-off puzzles like dailies should always be Simple in mechanics — difficulty comes from scale. Sequential modes (roguelite, tutorial campaigns) can ramp mechanical depth incrementally. This prevents "cold start" confusion where a player encounters 10+ mechanics in a standalone puzzle.

**De-parasitization philosophy**: Every aspect must have a primary mechanic that matters even in isolation. Sharp's primary mechanic is the edge placement constraint (can't go on bag edges), not just "breaks Fragile." Fragile's primary is universal vulnerability (breaks from ANY adjacent reaction), not just "broken by Sharp." This makes each aspect independently interesting for puzzle design.

**"Locked" is a placement flag, not a bag type**: The original design conflated locked bags with locked reagents. Clarified that any bag can contain locked (pre-placed, immovable) reagents — it's a per-placement property, not a bag property. `BagPropertyType.Locked` in the enum is a misnomer.

**Volatile net-hot system**: Replaces simple Hot+Volatile pair detection. Each Volatile cell computes `net_hot = sum(adjacent Hot) - sum(adjacent Cold)`. This makes Cold strategically useful as a "heat shield" near Volatile reagents, rather than just a cumulative threshold mechanic.

## Commits
| Hash | Message |
|------|---------|
| `e50e987` | Update design docs with complexity levels, de-parasitized aspects, and new PRD tasks |
| `7610a5d` | Refine complexity model: two dimensions (scale vs mechanics), Body bag flavor |
| `22127e5` | Pre-Ralph audit: fix doc inconsistencies, add bugfix tasks, rewrite PRD |
| `77971e2` | Add rate-limit detection and graceful abort to Ralph loop |

## Known Issues

- Ralph's implement prompt writes to `progress.md` but not `docs/worklog/`. Adding worklog support next.
- `BagPropertyType.Locked` enum value is a misnomer (should be on reagents, not bags). Not blocking — B.2 task description explains this.
- Existing `ReactionRuleDatabase` Explosion rule is a pair-trigger (`[hot, volatile]`) that will need to become a single-trigger (`[volatile]`) with net-hot evaluation. A.1 task describes this but it's a non-trivial refactor of how `EvaluateAdjacentRule` works.

## Next Steps

- Add worklog writing to Ralph's implement prompt (daily rolling worklog per loop run)
- Run Ralph loop: `.\scripts\ralph\ralph.ps1 -MaxIter 20`
- Ralph will pick up R-3, R-4, R-5 first (priority 0), then B.1-B.4 and A.1-A.6
- Review `dev` branch after loop completes, merge to `master` when satisfied
