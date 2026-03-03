# Alchemist Courier - Agent Guidelines

## Project Overview
Godot 4.3 C# inventory tetris game with alchemically reactive reagents.
See `docs/game-overview.md` for full context, `docs/mvp-specification.md` for current build target.

## Tech Stack
- **Engine:** Godot 4.3 (.NET / C#)
- **Language:** C# with file-scoped namespaces
- **Target:** Web (HTML5) for MVP, Windows for full game
- **Art:** RetroDiffusion pixel art pipeline (placeholder colored rectangles for now)

## Project Structure
```
src/Core/     — Pure C#, NO Godot dependencies. Unit-testable standalone.
src/Game/     — Godot-specific game logic (bridges Core to nodes)
src/UI/       — UI components
scenes/       — Godot .tscn scene files
resources/    — Godot .tres resource files
docs/         — Design documentation
docs/worklog/ — Session worklogs (see docs/agent-workflow.md)
tests/        — xUnit test projects
```

## Critical Conventions
1. **`src/Core/` must have ZERO Godot dependencies.** No `using Godot;`. Use `AlchemistCourier.Core.Grid.Vector2I` instead of `Godot.Vector2I`.
2. **Shape matrices are `bool[rows, cols]`** (row-major). Positions are `Vector2I(col, row)`.
3. **Per-cell aspects** are indexed by flattened row-major order through true cells in the shape.
4. **Namespace pattern:** `AlchemistCourier.Core.{System}` for core, `AlchemistCourier.Game.{System}` for game logic.
5. **`.csproj` and `.sln` are committed.** Godot initially generates them, but we track them in git because we've added custom settings (`<Nullable>enable</Nullable>`, `<Compile Remove="tests/**" />`).

## Build & Test
```bash
# Build everything (main project + tests):
dotnet build

# Run Core unit tests:
dotnet test

# Or target specific project:
dotnet build Alchemist-Courier.csproj
dotnet test tests/AlchemistCourier.Core.Tests/
```

No bootstrap step needed — `.csproj` and `.sln` are in the repo. Fresh clone -> `dotnet restore` -> `dotnet build` works.

## Before Committing
- Run `dotnet test` — all tests must pass
- Verify no Godot imports in `src/Core/` files
- Group commits logically by system (see branch history for examples)

## Session Workflow
1. Read this file and relevant `docs/` files
2. Check recent worklogs in `docs/worklog/` to understand current state
3. Do work on a feature branch (not master)
4. Create a worklog entry at session end — see `docs/agent-workflow.md` for template

## Key Design Docs
- `docs/architecture.md` — Data models, project structure, technical decisions
- `docs/core-mechanics.md` — Grid, bags, reagents, adjacency
- `docs/chemistry-system.md` — Aspects, schools, reaction rules
- `docs/mvp-specification.md` — What to build for the MVP
