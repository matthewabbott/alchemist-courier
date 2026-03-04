# Integration Testing & Godot Upgrade — 2026-03-03

## Summary

Set up an agent-driven integration testing harness that runs inside Godot, captures screenshots, and dumps structured results to a log file. Also upgraded the project from Godot 4.3 / .NET 6 to Godot 4.6.1 / .NET 8.

## Why the Upgrade

We needed better testing infrastructure for overnight agentic programming runs. Researching options revealed:

- **GodotTestDriver** (Chickensoft) requires Godot 4.4+ / .NET 8 bindings
- **Godot `--headless`** disables rendering entirely — no screenshots possible
- **C# can't export to web** — browser-based testing tools were out
- **.NET 6 is EOL** (since Nov 2024) — .NET 8 is the current LTS

The custom test harness approach works on any Godot version, but the upgrade was worth doing now to unblock future use of GodotTestDriver and to get off the dead .NET 6 target.

## What Was Built

### Integration Test Harness (new)

| File | Description |
|------|-------------|
| `src/Game/Testing/TestHarness.cs` | Test runner: 7-step sequence, structured stdout + file-based log, auto-quit |
| `src/Game/Testing/TestActions.cs` | Reusable helpers: SelectAndPlace, SelectRotateAndPlace, RemoveAt, GetStateString |
| `src/Game/Testing/ScreenshotHelper.cs` | Viewport → PNG capture utility |
| `scenes/test_harness.tscn` | Standalone scene for running the harness |

### PuzzleScreen API Additions (modified)

Added public state inspection and programmatic control so the test harness (and future tutorials/AI) can drive the UI without mouse input:

- **Properties:** `Grid`, `TrayReagents`, `SelectedReagent`, `GridView`
- **Methods:** `SelectReagent()`, `PlaceSelectedAt()`, `RotateSelected()`, `CancelSelection()`, `RemoveReagentAt()`

### Godot 4.3 → 4.6.1 Upgrade

| File | Change |
|------|--------|
| `Alchemist-Courier.csproj` | `Godot.NET.Sdk/4.3.0` → `4.6.1`, `net6.0` → `net8.0`, removed android/ios TFM conditionals |
| `tests/.../AlchemistCourier.Core.Tests.csproj` | `net6.0` → `net8.0` |
| `project.godot` | features `"4.3"` → `"4.6"` |
| `*.uid` (24 files) | New sidecar files from Godot 4.4+ UID system |
| `scenes/main.tscn`, `icon.svg.import` | Editor migration (uid:// references) |

## Agent Testing Loop

The full loop an agent follows for overnight runs:

1. `dotnet build` — verify compilation
2. `dotnet test` — verify 47 Core unit tests pass
3. `mcp__godot__run_project(scene="scenes/test_harness.tscn")` — run harness
4. `Read test_output/test_results.log` — parse structured `[PASS]`/`[FAIL]` results
5. `Read test_output/*.png` — visually inspect screenshots
6. If failures: fix code → rebuild → re-run

## Commits

| Hash | Message |
|------|---------|
| `fc91325` | Upgrade Godot 4.3 to 4.6.1, .NET 6 to .NET 8 |
| `d049f73` | Add integration test harness with screenshot capture |
| `33351af` | Add integration testing docs, gitignore test_output |

## Next Steps

- Explore GodotTestDriver now that we're on 4.6.1 for richer Playwright-style interaction
- Add more test cases to the harness as new features are built
- Consider GdUnit4 v6.1.x for in-editor test execution
- Merge to master once stable
