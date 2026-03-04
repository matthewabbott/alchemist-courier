# Alchemist Courier - Agent Guidelines

## Project Overview
Godot 4.6 C# inventory tetris game with alchemically reactive reagents.
See `docs/game-overview.md` for full context, `docs/mvp-specification.md` for current build target.

## Tech Stack
- **Engine:** Godot 4.6 (.NET 8 / C#)
- **Language:** C# with file-scoped namespaces
- **Target:** Web (HTML5) for MVP, Windows for full game
- **Art:** RetroDiffusion pixel art pipeline (placeholder colored rectangles for now)

## Project Structure
```
src/Core/     — Pure C#, NO Godot dependencies. Unit-testable standalone.
src/Game/     — Godot-specific game logic (bridges Core to nodes)
src/Game/Testing/ — Integration test harness (runs inside Godot)
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

## Integration Testing (Agent Loop)
The test harness runs inside Godot, takes screenshots, and dumps structured state to stdout.

```bash
# Run the test harness scene (via Godot MCP tool):
mcp__godot__run_project(projectPath="D:\a\programming\alchemist-courier", scene="scenes/test_harness.tscn")

# Read stdout for structured output:
mcp__godot__get_debug_output()

# Read screenshot PNGs (agent can view these as images):
Read tool on test_output/*.png files
```

### Structured output format
- `[HARNESS_START]` / `[HARNESS_DONE passed=N failed=N]` — harness lifecycle
- `[TEST] test_name` — test step beginning
- `[STATE] key=value key=value` — grid/reagent state snapshot
- `[ACTION] action_type param=value result=success|failed` — programmatic actions
- `[SCREENSHOT] /absolute/path/to/file.png` — screenshot saved
- `[PASS] test_name` / `[FAIL] test_name: reason` — test result

### Key files
- `src/Game/Testing/TestHarness.cs` — Test runner (add new tests here)
- `src/Game/Testing/TestActions.cs` — Reusable action helpers
- `src/Game/Testing/ScreenshotHelper.cs` — Viewport screenshot capture
- `scenes/test_harness.tscn` — Scene to run
- `test_output/` — Screenshot output directory (gitignored)

### Agent workflow for overnight runs
1. `dotnet build` — verify compilation
2. `dotnet test` — verify Core unit tests
3. Run test harness scene → parse stdout → read screenshots
4. If failures: fix code, rebuild, re-run harness
5. Repeat until all tests pass

## Key Design Docs
- `docs/architecture.md` — Data models, project structure, technical decisions
- `docs/core-mechanics.md` — Grid, bags, reagents, adjacency
- `docs/chemistry-system.md` — Aspects, schools, reaction rules
- `docs/mvp-specification.md` — What to build for the MVP
