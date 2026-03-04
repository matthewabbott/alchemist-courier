# Technical Architecture

This document provides architectural guidance for implementing Alchemist Courier in Godot 4.6 with C#. It covers the data model, key systems, project structure, and technical constraints. Future agents should reference this when making implementation decisions.

## Tech Stack

| Component | Choice | Rationale |
|-----------|--------|-----------|
| **Engine** | Godot 4.6 | Open source, strong 2D support, HTML5 export, active community |
| **Language** | C# (.NET) | Strong typing suits complex game systems (chemistry engine, inventory logic, reaction evaluation). The project uses Godot's .NET integration. |
| **Art Pipeline** | RetroDiffusion → Godot import | AI-generated pixel art, imported as sprites/textures |
| **Target Platforms** | Web (HTML5) primary for MVP, Windows PC for full game | Godot supports both from a single project |
| **AI Tooling** | Claude Code | AI-assisted development for code, docs, and iteration |

## Project Structure (Recommended)

```
alchemist-courier/
├── project.godot
├── docs/                          # Design documentation (this folder)
│   ├── game-overview.md
│   ├── core-mechanics.md
│   ├── chemistry-system.md
│   ├── mvp-specification.md
│   ├── venture-and-progression.md
│   ├── art-and-style.md
│   └── architecture.md
├── src/                           # All C# source code
│   ├── Core/                      # Core data models and logic (no Godot dependencies)
│   │   ├── Grid/                  # Grid, cells, positions
│   │   ├── Bags/                  # Bag definitions, properties
│   │   ├── Reagents/              # Reagent definitions, shapes, aspects
│   │   ├── Chemistry/             # Aspect system, reaction rules, evaluation
│   │   ├── Puzzle/                # Puzzle generation, validation, solving
│   │   └── Save/                  # Serialization, save/load
│   ├── Game/                      # Godot-specific game logic
│   │   ├── Inventory/             # Inventory UI, drag-and-drop, placement
│   │   ├── Puzzle/                # Puzzle mode game flow
│   │   ├── Venture/               # Venture system (post-MVP)
│   │   ├── Map/                   # Encounter map (post-MVP)
│   │   └── Base/                  # Base management (aspirational)
│   └── UI/                        # UI components
│       ├── Screens/               # Main menu, puzzle screen, results
│       ├── Components/            # Reusable UI widgets
│       └── Theme/                 # UI theming and styles
├── scenes/                        # Godot scene files (.tscn)
│   ├── main.tscn
│   ├── puzzle/
│   ├── ui/
│   └── venture/                   # Post-MVP
├── resources/                     # Godot resource files (.tres)
│   ├── reagents/                  # Reagent definitions
│   ├── bags/                      # Bag definitions
│   ├── reactions/                 # Reaction rule definitions
│   ├── puzzles/                   # Hand-crafted puzzle data (if any)
│   └── themes/                    # UI theme resources
├── art/                           # Art assets
│   ├── sprites/
│   │   ├── reagents/
│   │   ├── bags/
│   │   ├── character/
│   │   ├── ui/
│   │   └── icons/
│   ├── backgrounds/
│   └── placeholder/               # Colored rectangles for MVP
└── audio/                         # Audio assets
    ├── sfx/
    └── music/
```

### Key Principle: Core Logic Independence

The `src/Core/` folder should contain **pure C# logic with no Godot dependencies**. This means:
- Grid math, aspect evaluation, reaction rules, puzzle generation — all testable without running Godot
- The `src/Game/` and `src/UI/` folders bridge Core logic to Godot nodes and scenes
- This separation makes the chemistry engine unit-testable and potentially portable

## Data Model

### Grid Cell (Atomic Unit)

The grid cell is the smallest unit of the spatial system. Everything is built from cells.

```csharp
public class GridCell
{
    public Vector2I Position { get; set; }       // (col, row) on the meta-grid
    public CellOwner Owner { get; set; }         // Which bag (or meta-grid) this cell belongs to
    public Reagent? OccupyingReagent { get; set; } // The reagent in this cell, if any
    public bool IsDamaged { get; set; }          // Damaged cells can't hold reagents
    public bool IsLocked { get; set; }           // Locked cells can't have reagents moved
}
```

### Meta-Grid

```csharp
public class MetaGrid
{
    public int Width { get; }
    public int Height { get; }
    public GridCell[,] Cells { get; }            // The full grid
    public List<Bag> PlacedBags { get; }         // Bags on the meta-grid

    // Key methods:
    public bool CanPlaceBag(Bag bag, Vector2I position);
    public void PlaceBag(Bag bag, Vector2I position);
    public void RemoveBag(Bag bag);
    public List<GridCell> GetBagCells(Bag bag);   // All cells belonging to a bag
    public List<GridCell> GetAdjacentCells(GridCell cell); // Cardinal neighbors
}
```

### Bag

```csharp
public class BagDefinition  // Data definition (from resource files)
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool[,] Shape { get; set; }           // Which cells exist (true = cell, false = void)
    public BagProperty[] Properties { get; set; } // Special properties (Null, Locked, Gravity, etc.)
}

public class Bag  // Runtime instance
{
    public BagDefinition Definition { get; }
    public Vector2I PositionOnMetaGrid { get; set; }  // Where this bag sits
    public List<GridCell> Cells { get; }               // The actual cells (positioned)

    public bool HasProperty(BagPropertyType type);
}

public enum BagPropertyType
{
    None,
    Null,       // Suppresses aspects
    Locked,     // Prevents reagent movement after placement
    Gravity,    // Reagents fall to bottom
    Reinforced, // Resistant to damage
    Fragile,    // Easily damaged
    Insulated,  // Raises reaction thresholds
    Levitating  // Can be placed anywhere on meta-grid
}
```

### Reagent

```csharp
public class ReagentDefinition  // Data definition (from resource files)
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool[,] Shape { get; set; }             // Which cells the reagent occupies
    public AspectSet[] AspectsPerCell { get; set; } // Aspects for each cell (indexed matching shape)
    public float Weight { get; set; }
    public int Value { get; set; }
    public Rarity Rarity { get; set; }
    public bool IsConsumable { get; set; }
    public ConsumptionEffect? ConsumptionEffect { get; set; }
}

public class Reagent  // Runtime instance
{
    public ReagentDefinition Definition { get; }
    public int Rotation { get; set; }              // 0, 90, 180, 270 degrees
    public Vector2I? PlacedPosition { get; set; }  // Position on meta-grid (null = not placed)
    public Bag? PlacedInBag { get; set; }          // Primary bag (or null)
    public bool IsDiscovered { get; set; }         // Has the player handled this reagent before?

    // Key methods:
    public bool[,] GetRotatedShape();
    public AspectSet GetAspectsAtCell(Vector2I localCell);  // Aspects at a specific cell, accounting for rotation
    public List<Vector2I> GetOccupiedCells();               // All cells this reagent occupies (in meta-grid coords)
}

public enum Rarity { Common, Uncommon, Rare, VeryRare, Quest }
```

### Aspect System

```csharp
public class Aspect
{
    public string Id { get; set; }           // "hot", "cold", "volatile", etc.
    public string Name { get; set; }         // Display name
    public SchoolType School { get; set; }   // Which school this aspect belongs to
    public int Magnitude { get; set; }       // How "much" of this aspect (default 1)
}

public class AspectSet
{
    public List<Aspect> Aspects { get; }     // All aspects on a cell/reagent

    public bool Has(string aspectId);
    public int GetMagnitude(string aspectId);
    public int TotalMagnitude();             // Sum of all magnitudes
}

public enum SchoolType
{
    Physical,       // Simple: Hot, Cold, Volatile, Stable, Heavy, Toxic | Intermediate: Sharp, Fragile, Medicinal, Catalytic
    Wuxing,         // Wood, Fire, Earth, Metal, Water
    WesternAlchemy, // Nigredo, Albedo, Citrinitas, Rubedo
    Astrology,      // TBD
}
```

### Reaction Rules

```csharp
public class ReactionRule
{
    public string Id { get; set; }
    public string Name { get; set; }               // "Explosion", "Neutralization", etc.

    // Trigger conditions
    public AspectRequirement[] Requirements { get; set; } // What aspects must be present
    public SensitivityTier Sensitivity { get; set; }      // How close they must be
    public Granularity Granularity { get; set; }          // Per-cell or per-item counting
    public int Threshold { get; set; }                    // How much is needed

    // Results
    public ReactionResult Result { get; set; }            // What happens when triggered
}

public class AspectRequirement
{
    public string AspectId { get; set; }      // Which aspect
    public int MinCount { get; set; }         // Minimum count (cells or items depending on granularity)
}

public enum SensitivityTier
{
    AdjacentCells,   // Must be cardinally touching
    SameBag,         // Must be in the same bag
    WholeInventory   // Can be anywhere
}

public enum Granularity
{
    PerCell,   // Count individual cells with the aspect
    PerItem    // Count distinct reagent items with the aspect
}

public class ReactionResult
{
    public ReactionResultType Type { get; set; }
    public float Magnitude { get; set; }       // Severity
    // Additional parameters per type
}

public enum ReactionResultType
{
    Explosion,          // Destroy reagents + damage cells + injury
    Neutralization,     // Cancel opposing aspects
    Breakage,           // Destroy fragile reagent
    StatusEffect,       // Apply cumulative effect (heat, cold, toxicity)
    AspectAmplification,// Strengthen an aspect (wuxing generating)
    AspectSuppression,  // Weaken an aspect (wuxing overcoming)
    Transmutation       // Change aspect to another (western alchemy stage progression)
}
```

## Chemistry Engine

The chemistry engine evaluates the current state of the inventory and determines all active reactions. It should be a **standalone system** that can be called with a grid state and returns a list of active reactions.

### Evaluation Flow

```
Input: MetaGrid with all bags and placed reagents
Output: List<ActiveReaction> (all currently triggered reactions)

1. For each ReactionRule in the rule database:
   a. Determine the scope based on SensitivityTier:
      - AdjacentCells: check all pairs of adjacent occupied cells
      - SameBag: check all reagents within each bag
      - WholeInventory: check all reagents across all bags
   b. Within the scope, count aspects based on Granularity:
      - PerCell: count cells with the required aspects
      - PerItem: count reagents with the required aspects
   c. Apply modifiers:
      - Catalytic aspects lower the threshold
      - Stable aspects raise the threshold
      - Null bag cells suppress aspects entirely
      - Insulated bags raise thresholds
   d. If modified count >= modified threshold, the reaction is active
   e. Record the reaction with its affected cells/reagents

2. Return all active reactions
```

### Prospective Evaluation

When the player is hovering a reagent over a position (before confirming), the engine runs a **prospective evaluation**:
1. Temporarily place the reagent at the hovered position
2. Run full evaluation
3. Compare with current state (before the hover)
4. Highlight NEW reactions that would be triggered by this placement
5. Remove the temporary placement

This must run in < 16ms to maintain 60fps responsiveness.

### Data-Driven Rules

All reaction rules should be defined in **data files** (Godot Resources or JSON), not hard-coded. This allows:
- Easy balancing (tweak thresholds without recompiling)
- Adding new reactions without code changes
- Different rule sets for different regions or game modes
- Modding support (aspirational)

```
resources/reactions/
  explosion.tres       # Hot + Volatile → Explosion
  neutralization.tres  # Hot + Cold → Neutralization
  toxic_cloud.tres     # Toxic + Toxic → Intensified toxicity
  breakage.tres        # Sharp + Fragile → Breakage
  overheating.tres     # Cumulative hot > threshold → Overheating
  hypothermia.tres     # Cumulative cold > threshold → Hypothermia
  poisoned.tres        # Cumulative toxic > threshold → Poisoned
```

## Puzzle Generation System

### Requirements
- **Deterministic**: Same seed → same puzzle, always, on all platforms
- **Guaranteed solvable**: Every generated puzzle has at least one valid solution
- **Parameterized difficulty**: Configurable knobs for complexity

### Seeded RNG

Use a deterministic PRNG (pseudo-random number generator):

```csharp
public class SeededRandom
{
    private Random _rng;

    public SeededRandom(int seed)
    {
        _rng = new Random(seed);
    }

    public int Next(int min, int max) => _rng.Next(min, max);
    public float NextFloat() => (float)_rng.NextDouble();
    public T Pick<T>(List<T> list) => list[Next(0, list.Count)];
    public void Shuffle<T>(List<T> list) { /* Fisher-Yates using _rng */ }
}

// Daily seed: int.Parse(DateTime.Now.ToString("yyyyMMdd"))
// e.g., March 3, 2026 → 20260303
```

**Critical**: All random decisions in puzzle generation must go through this seeded RNG. No calls to `GD.Randi()`, `new Random()`, or any other unseeded source during generation.

### Generation Algorithm

See `docs/mvp-specification.md` for the full disassemble-a-solution algorithm. Key implementation notes:

```csharp
public class PuzzleGenerator
{
    public PuzzleState Generate(int seed, DifficultyParams difficulty)
    {
        var rng = new SeededRandom(seed);

        // 1. Select bags
        var bags = SelectBags(rng, difficulty);

        // 2. Place bags on meta-grid
        var bagLayout = ArrangeBags(rng, bags, difficulty.MetaGridSize);

        // 3. Select reagents
        var reagents = SelectReagents(rng, difficulty, bags);

        // 4. Place reagents validly (the "solution")
        var solution = PlaceReagentsValidly(rng, reagents, bagLayout);
        // If placement fails, retry with different selection

        // 5. Disassemble: remove reagents, optionally lock some
        var puzzle = Disassemble(rng, solution, difficulty);

        return puzzle;
    }
}
```

### Solver / Validator

A solver is needed to verify that generated puzzles are solvable and (optionally) to rate difficulty:

```csharp
public class PuzzleSolver
{
    // Verifies at least one valid solution exists
    public bool IsSolvable(PuzzleState puzzle);

    // Finds all valid solutions (for difficulty rating)
    public List<PuzzleSolution> FindAllSolutions(PuzzleState puzzle, int maxSolutions = 100);

    // Rates difficulty based on solution count, constraint tightness, etc.
    public DifficultyRating RateDifficulty(PuzzleState puzzle);
}
```

## Save System

### Web (MVP): localStorage

```csharp
// Use Godot's JavaScript bridge for web builds
public class WebSaveManager
{
    public void Save(string slotId, string jsonData)
    {
        // Call JavaScript: localStorage.setItem(key, value)
    }

    public string Load(string slotId)
    {
        // Call JavaScript: localStorage.getItem(key)
    }
}
```

### Desktop: File-based

```csharp
public class DesktopSaveManager
{
    private string SavePath => OS.GetUserDataDir() + "/saves/";

    public void Save(string slotId, string jsonData)
    {
        // Write to SavePath + slotId + ".json"
    }

    public string Load(string slotId)
    {
        // Read from SavePath + slotId + ".json"
    }
}
```

### Save Data Schema

```json
{
  "version": "1.0",
  "slotId": "slot_1",
  "tutorialProgress": {
    "completedLessons": [1, 2, 3, 4, 5],
    "currentLesson": 6
  },
  "dailyPuzzle": {
    "completedDates": ["20260301", "20260302", "20260303"],
    "currentStreak": 3,
    "bestStreak": 7
  },
  "codex": {
    "discoveredReagents": ["cinnabar", "giggle_mushroom", "fire_crystal"],
    "discoveredAspects": ["hot", "cold", "volatile", "stable"],
    "discoveredReactions": ["explosion", "neutralization"]
  },
  "settings": {
    "sfxVolume": 0.8,
    "musicVolume": 0.5,
    "controls": {}
  }
}
```

### Multiple Save Slots
- At least 3 save slots, or unlimited with a clean UI
- Each slot is independent (separate tutorial progress, codex, etc.)
- For the full game (post-MVP), each slot also includes venture progress, base state, bag collection, etc.

## Web Export Considerations

### HTML5 Constraints
- No file system access → use localStorage for saves
- No native threads → ensure chemistry evaluation is fast (single-threaded)
- Audio may require user interaction before playing (browser policy)
- Font rendering may differ → test pixel fonts across browsers
- WebGL support required → should be fine for 2D pixel art

### Testing
- Test on: Chrome, Firefox, Safari, Edge
- Test at various browser window sizes
- Test localStorage persistence across sessions
- Test deterministic puzzle generation (same seed = same puzzle across browsers)

### Hosting
- Godot HTML5 export produces: index.html + .js + .wasm + .pck files
- Host on itch.io (free, handles all the hosting) or static web server
- itch.io provides embed and fullscreen support out of the box

## Performance Targets

| Operation | Target | Notes |
|-----------|--------|-------|
| Chemistry evaluation (full grid) | < 16ms | Must not drop frames when hovering |
| Puzzle generation | < 1 second | Can show a brief loading indicator |
| Reagent placement (UI) | < 8ms | Snappy drag-and-drop feel |
| Save/Load | < 100ms | Imperceptible |
| Initial page load (web) | < 3 seconds | Small asset size (pixel art + placeholder) |

## Godot Scene Structure (Recommended)

```
Main (Node)
├── GameManager (Node) — C# singleton, manages game state
├── PuzzleScreen (Control)
│   ├── MetaGridView (Control) — Renders the meta-grid + bags + reagents
│   │   ├── BagView (Control) — One per bag, renders cells
│   │   └── ReagentView (Control) — One per placed reagent
│   ├── ReagentTray (Control) — Sidebar with available reagents
│   │   └── ReagentTrayItem (Control) — One per unplaced reagent
│   ├── StatusPanel (Control) — Stamina, aspect totals, warnings
│   ├── ObjectivePanel (Control) — Current puzzle objective
│   └── ActionButtons (Control) — Confirm, Reset, Primer
├── TutorialOverlay (Control) — Tutorial hints and progression
├── DailyPuzzleManager (Node) — Handles daily seed, streak tracking
└── SaveManager (Node) — Save/load orchestration
```

## Key Architectural Decisions

### 1. Separate Core Logic from Godot
The chemistry engine, grid math, and puzzle generation should have **zero Godot dependencies**. This enables:
- Unit testing with standard C# test frameworks
- Potential future porting to other engines/platforms
- Cleaner separation of concerns

### 2. Data-Driven Design
Reagents, bags, reaction rules, and puzzle parameters should be defined in **data files** (Godot Resources preferred for editor integration, JSON as fallback for portability). Avoid hard-coding game content.

### 3. Prospective Evaluation as First-Class Feature
The "hover preview" (showing what WOULD happen if the player places a reagent) is a core UX feature, not an afterthought. The chemistry engine must support efficient prospective evaluation.

### 4. Deterministic Everything
All randomness goes through seeded RNG. This ensures:
- Daily puzzles are identical for all players
- Saves can store just the seed + player actions (compact)
- Bugs are reproducible (replay with same seed)

### 5. Web-First for MVP
Design and test with web export in mind from day one. Don't use features that break on HTML5. Test in browsers early and often.

## Related Documents

- `docs/game-overview.md` — Project overview and tech stack rationale
- `docs/core-mechanics.md` — Game design that the architecture must implement
- `docs/chemistry-system.md` — The chemistry engine specification
- `docs/mvp-specification.md` — What to build first
- `docs/art-and-style.md` — Asset organization and art pipeline
