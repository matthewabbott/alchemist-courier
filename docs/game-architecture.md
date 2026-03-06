# Game Architecture Guidelines

Structural patterns and conventions for Alchemist Courier as it grows beyond MVP. Reference this when adding new screens, systems, or game modes.

See `docs/roadmap.md` for the full feature roadmap across all tiers.

---

## 1. Scene Management

### State Machine for Game Flow

The game flow is a flat state machine of screens. Each screen is a Godot `Control` node that owns its full UI hierarchy:

```
Menu → Tutorial → Daily Puzzle → (future: Editor, Venture)
```

Current screens:
- `PuzzleScreen` (`src/Game/Inventory/PuzzleScreen.cs`) — the core gameplay screen
- `TutorialScreen` (`src/Game/TutorialScreen.cs`) — wraps PuzzleScreen with lesson progression
- `EditorScreen` (`src/Game/Editor/EditorScreen.cs`) — puzzle editor (in progress)

### Screen Lifecycle Pattern

Screens use a **pre-initialization property + deferred _Ready()** pattern:

```csharp
// 1. Set data BEFORE adding to scene tree
var screen = new PuzzleScreen { PuzzleData = state };

// 2. AddChild triggers _Ready() which reads the property
parentNode.AddChild(screen);
```

Inside `_Ready()`:
1. Set layout anchors (`LayoutPreset.FullRect`)
2. Read external data (property) or fall back to defaults
3. Call `BuildUI()` to construct the node hierarchy
4. Call `WireSignals()` to connect child signals
5. Run initial state updates

### Screen Transitions

Replace screens by removing the old instance and adding a new one:

```csharp
// Remove old screen
_puzzleScreen.PuzzleConfirmed -= OnPuzzleConfirmed;
_mainVBox.RemoveChild(_puzzleScreen);
_puzzleScreen.QueueFree();  // Defers cleanup to frame end
_puzzleScreen = null;

// Create and add new screen
_puzzleScreen = new PuzzleScreen { PuzzleData = newState };
_mainVBox.AddChild(_puzzleScreen);
```

Use `QueueFree()` (not `Free()`) to avoid dangling references during the current frame.

### When to Use .tscn vs Procedural C#

- **.tscn scene files:** Minimal root wrappers only. `scenes/main.tscn` and `scenes/test_harness.tscn` are ~13 lines each — a `Control` node with a script reference. No complex node trees in .tscn.
- **Procedural C#:** All complex UI is built in `BuildUI()` methods via `AddChild()`. This keeps the layout logic co-located with the behavior code and avoids split-brain between .tscn and .cs files.

---

## 2. Godot Scene-Tree Patterns

### Signal Conventions

Declare signals using the `[Signal]` attribute with `EventHandler` suffix delegates:

```csharp
[Signal]
public delegate void ReagentPlacedEventHandler();

[Signal]
public delegate void ReagentSelectedEventHandler(int reagentIndex);

[Signal]
public delegate void RejectionFlashEventHandler(string aspectId);
```

Connect signals in `WireSignals()` using C# `+=` syntax:

```csharp
private void WireSignals()
{
    _reagentTray.ReagentSelected += OnReagentSelected;
    _gridView.ReagentPlaced += OnReagentPlaced;
    _legendPanel.AspectSelected += OnLegendAspectSelected;
}
```

Emit signals using `EmitSignal(SignalName.X, args)`:

```csharp
EmitSignal(SignalName.ReagentPlaced);
EmitSignal(SignalName.RejectionFlash, aspectId);
```

### Node Naming

Nodes are referenced via typed fields (`_gridView`, `_reagentTray`), not by name lookups. Avoid `GetNode<T>("path")` — the parent that creates a child keeps a direct reference.

### AddChild Patterns

Build hierarchies using Godot's layout containers:

```csharp
var hbox = new HBoxContainer();
var leftPanel = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
var rightPanel = new VBoxContainer { CustomMinimumSize = new Godot.Vector2(220, 0) };

hbox.AddChild(leftPanel);
hbox.AddChild(rightPanel);
AddChild(hbox);
```

Layout containers used: `VBoxContainer`, `HBoxContainer`, `MarginContainer`, `CenterContainer`, `ScrollContainer`, `PanelContainer`. Use `SizeFlags` and `CustomMinimumSize` for sizing.

### Theme Overrides for Styling

Apply visual styling inline via theme overrides:

```csharp
var label = new Label
{
    Text = "Title",
    HorizontalAlignment = HorizontalAlignment.Center,
};
label.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.6f));
label.AddThemeFontSizeOverride("font_size", 18);
```

---

## 3. Performance Budgets for Web Export

### WASM Memory Limits

The web export targets WASM via Godot's HTML5 export. Key constraints:
- **Memory ceiling:** ~2GB addressable (browser-dependent). Keep total heap usage well below 512MB.
- **Startup time:** Minimize assembly size. The `src/Core/` layer compiles to a single assembly (`Alchemist-Courier.dll`). Avoid large third-party dependencies.
- **No native plugins:** Everything must be managed C# or Godot built-in.

### Draw Call Budgets

Pixel art is cheap per-sprite but many small nodes add up:
- Each `ColorRect` is a draw call. The grid uses `ColorRect[width, height]` arrays — an 8x8 grid = 64 draw calls for cells alone, plus grid lines and overlays.
- **Current approach:** Acceptable for MVP grid sizes (8x8 to 12x12). If grid sizes grow significantly, consider batched `_Draw()` rendering with `DrawRect()` calls in a single `Control._Draw()` override.
- Ghost preview cells and highlight overlays add transient draw calls. These are cleared when not active.

### GC Pressure

Avoid per-frame allocations in `_Process()` and `_Input()`:

**Good — timer-based cleanup using cached fields:**
```csharp
// Fields allocated once
private float _tooltipTimer;
private float _flashTimer;

public override void _Process(double delta)
{
    if (_tooltipTimer > 0)
    {
        _tooltipTimer -= (float)delta;
        if (_tooltipTimer <= 0) HideRejectionTooltip();
    }
}
```

**Good — reuse node arrays:**
```csharp
// Allocated once in Initialize(), reused via Color property changes
private ColorRect[,] _cellRects;

// RefreshDisplay() updates colors, never reallocates
_cellRects[col, row].Color = newColor;
```

**Avoid:**
- `new List<T>()` or LINQ allocations inside `_Process()` / `_Input()`
- String concatenation in hot paths (use `GD.Print()` only for debug/test output)
- Creating new `ColorRect` nodes per frame — pool and reuse instead

### Struct vs Class

`AlchemistCourier.Core.Grid.Vector2I` is a struct (value type). Grid positions are passed by value with zero heap allocation. Keep data types that are used in tight loops as structs where appropriate.

---

## 4. Asset Pipeline

### Current State: Placeholder Art

All visuals are currently colored rectangles rendered via `ColorRect.Color`. No texture files are loaded at runtime. This is intentional for MVP — art comes later via the RetroDiffusion pipeline.

### Color Mapping

Aspect colors are defined in `src/Game/Inventory/GridConstants.cs`:

```csharp
public static Color GetAspectColor(string aspectId) => aspectId switch
{
    "hot" => new Color(1.0f, 0.4f, 0.2f),
    "cold" => new Color(0.3f, 0.6f, 1.0f),
    // ...
};
```

Cell colors, placement validity colors, and highlight severity alphas are also centralized in `GridConstants`.

### Placeholder to Real Art Replacement

When sprites are ready, the replacement workflow is:

1. **Generate** sprites via RetroDiffusion MCP server → `art/generated/`
2. **Curate** — human selects approved sprites → `art/sprites/{category}/`
3. **Import** — Godot auto-imports PNGs with `.import` sidecar files. Set import settings: nearest-neighbor filtering, 2D pixel preset.
4. **Load** — Replace `ColorRect` rendering with `Sprite2D` or `TextureRect` nodes, loading textures via `GD.Load<Texture2D>("res://art/sprites/...")`.

Naming conventions in `art/sprites/`:
- `reagent_{name}.png` — reagent portraits
- `bag_{type}.png` — bag textures
- `icon_{aspect}.png` — aspect icons (32x32)

### Godot Import Settings for Pixel Art

All sprite imports should use:
- Filter: **Nearest** (no interpolation)
- Compression: **Lossless**
- Mipmaps: **Off**

---

## 5. Input Handling

### Drag-and-Drop Architecture

The game uses a **select-then-place** pattern (not true drag-and-drop):

1. **Select** — Click a `ReagentTrayItem` to select a reagent
2. **Preview** — Mouse movement over the grid shows ghost cells at the hover position
3. **Place** — Left-click on the grid places the reagent (if valid)
4. **Remove** — Right-click on a placed reagent returns it to the tray

### Input Event Flow

`MetaGridView` overrides `_Input()` to dispatch mouse and keyboard events:

```csharp
public override void _Input(InputEvent @event)
{
    if (@event is InputEventMouseMotion motion)
        HandleMouseMotion(motion);
    else if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed)
        HandleMouseClick(mouseBtn);
    else if (@event is InputEventKey key && key.Pressed && !key.Echo)
        HandleKeyPress(key);
}
```

`ReagentTrayItem` uses `_GuiInput()` for click handling and `MouseEntered`/`MouseExited` signals for hover:

```csharp
public override void _GuiInput(InputEvent @event)
{
    if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
    {
        EmitSignal(SignalName.Clicked, _index);
        AcceptEvent();
    }
}
```

### Keyboard Shortcuts

- **R** — Rotate selected reagent (`MetaGridView.HandleKeyPress`)
- **Esc** — Cancel selection (`MetaGridView.HandleKeyPress`)
- **Tab** — Toggle Eagle Sense (`PuzzleScreen._UnhandledKeyInput`)

### Ghost Preview System

When a reagent is selected and the mouse hovers over the grid:
1. `HandleMouseMotion()` computes `_hoverGridPos` from local mouse coordinates
2. `UpdateGhostPreview()` creates/updates `_ghostCells` (pooled `ColorRect` nodes)
3. Ghost colors indicate validity: green (safe), yellow (warning), red (invalid)
4. `ComputeAndRenderHighlights()` adds constraint and interaction highlights via `HighlightEngine`

### Touch Input (Web Export)

Touch events are not yet implemented. Godot maps single-touch to mouse events automatically, so basic tap-to-select and tap-to-place works. Multi-touch gestures (pinch-to-zoom, two-finger rotate) would need explicit `InputEventScreenTouch`/`InputEventScreenDrag` handling.

---

## 6. Testing Strategy

### Three-Layer Testing

| Layer | Framework | Scope | Location | Runner |
|-------|-----------|-------|----------|--------|
| **Unit** | xUnit | `src/Core/` logic | `tests/AlchemistCourier.Core.Tests/` | `dotnet test` |
| **Integration** | Custom harness | Godot game behavior | `src/Game/Testing/` | Godot scene runner |
| **Manual** | Human playtesting | UX, feel, visual correctness | N/A | Run `scenes/main.tscn` |

### Unit Tests (Core Layer)

Pure C# tests with zero Godot dependencies. Run via `dotnet test` without Godot installed.

```csharp
[Fact]
public void SameDateProducesSamePuzzle()
{
    var date = new DateTime(2026, 3, 4);
    var puzzle1 = DailyPuzzleManager.GenerateDailyPuzzle(date);
    var puzzle2 = DailyPuzzleManager.GenerateDailyPuzzle(date);
    Assert.Equal(puzzle1.Seed, puzzle2.Seed);
}
```

Coverage: grid operations, chemistry evaluation, puzzle generation/validation, save/load serialization, highlight computation, hint logic. Currently 497+ tests.

### Integration Tests (Godot Harness)

The test harness (`src/Game/Testing/TestHarness.cs`) runs inside Godot as a scene, exercises the full game stack (Core + Game + UI), and produces structured output.

**Running:**
```bash
# Via Godot MCP tool:
mcp__godot__run_project(projectPath, scene="scenes/test_harness.tscn")
# Then read output:
mcp__godot__get_debug_output()
```

**Test structure:**
```csharp
private async Task Test_PlaceReagent()
{
    Log("[TEST] place_fire_pebble");
    bool placed = TestActions.SelectAndPlace(_puzzleScreen, 0, 2, 2);
    await WaitFrames(2);
    Screenshot("after_place_fire_pebble.png");

    if (placed && _puzzleScreen.PlacedReagentCount == 1)
        Pass("place_fire_pebble");
    else
        Fail("place_fire_pebble", $"placed={placed}");
}
```

**Output format:**
```
[HARNESS_START]
[TEST] place_fire_pebble
[ACTION] place col=2 row=2 result=success
[SCREENSHOT] D:\...\test_output\after_place_fire_pebble.png
[PASS] place_fire_pebble
[HARNESS_DONE passed=23 failed=0]
```

Currently 23 integration tests covering placement, removal, rotation, chemistry warnings, tutorial flow, daily puzzle, hint system, highlight rendering, and eagle sense.

**Helper classes:**
- `TestActions.cs` — Reusable actions (select, place, rotate, confirm, reset)
- `ScreenshotHelper.cs` — Captures viewport to PNG in `test_output/`

### When to Use Which Layer

- **New `src/Core/` logic:** Always add xUnit tests. This is the primary quality gate.
- **UI behavior changes:** Add integration test steps to `TestHarness.cs`. Screenshots verify visual correctness.
- **Layout/spacing tweaks:** Manual playtesting. Integration tests verify elements exist but can't judge aesthetic quality.
- **Cross-system interactions** (e.g., chemistry affects UI highlights): Integration test with state assertions + screenshot verification.
