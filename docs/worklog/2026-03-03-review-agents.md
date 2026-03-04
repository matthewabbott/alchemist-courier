# 2026-03-03 — Review Agents + Dev Branch Strategy

## What

Added three review agent roles to the Ralph loop and a `dev` branch strategy for safer autonomous development.

**Review agents:**
- **Code review (deslop)** — runs every 3 implementations, reviews recent changes against `docs/agents/deslop.md` coding principles. Fixes P0/P1 directly, creates tasks for larger issues.
- **Integration testing** — runs every 5 implementations, executes the Godot test harness, parses structured output and screenshots, fixes test failures or escalates bugs.
- **Architectural review** — runs at phase boundaries, checks alignment with game vision, design docs, and conventions. Fixes stale docs directly.

**Dev branch strategy:** Ralph loop works on `dev` (auto-created from `master`). Per-task branches `ralph/{id}-{slug}` merge back to `dev`. Human reviews and merges `dev` → `master`.

**Tooling changes:**
- Prompts reorganized into `scripts/ralph/prompts/` (implement, review-code, review-integration, review-architecture)
- Role selection logic in `ralph.sh` with implementation counter and phase detection
- `check-phase.py` helper compares phase completion state against a marker file
- Review output goes to `docs/reviews/`, summaries to `progress.md`
- Review-generated fix tasks use `R` prefix IDs with priority 0

## Why

The original Ralph loop was implement-only — no quality checks between tasks. An autonomous agent can drift from the game vision, accumulate code smells, or silently break integration. Three docs already referenced "Godot 4.3" after the 4.6 upgrade — exactly the kind of drift these agents catch.

The `dev` branch adds a human checkpoint. Autonomous work stays on `dev`; nothing reaches `master` without review.

## Also this session

- Upgraded Claude CLI from ancient npm 0.2.9 to native install 2.1.66 (auto-updating)
- Confirmed Windows x86_64 compatibility for Ralph loop

## Commits

- `a242534` — Add review agent roles and dev branch strategy to Ralph loop
- `34740fd` — Merge feature/review-agents to master
