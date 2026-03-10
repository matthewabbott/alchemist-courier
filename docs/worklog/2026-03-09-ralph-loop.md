# Ralph Loop — 2026-03-09

## GJ.1: Soften gravity constraint — unsupported placement + auto-fall
Gravity was previously a hard placement gate — CanPlaceReagent rejected unsupported positions in gravity bags. This made the UX rigid (you had to place bottom-up). Now gravity is advisory: placement succeeds anywhere, GetGravityViolations reports warnings, and PlaceReagentWithFall auto-drops reagents to their resting position. Also removed the Heavy-only gate in MetaGridView so ALL reagents in gravity bags show fall previews and auto-fall, not just Heavy ones. This aligns with the game jam goal of making placement feel more natural and forgiving.
Commit: 40f9824

## GJ.2: Debug + fix volatile explosion indicator
Traced the full volatile explosion indicator chain: ViolationTracker → ChemistryEngine → DamageOverlay → confirm flow. Everything in the core and rendering was correct — the test harness proved the overlay works after placement. The real bug was a one-liner introduced by GJ.1's gravity changes: MetaGridView.UpdateGhostPreview called EvaluateProspective with the raw hover position instead of the gravity-adjusted effectivePos, so the ghost preview showed "safe" even when the reagent would fall next to a hot reagent and explode. Classic case of updating the placement path but missing the prospective check path. Added two net-hot threshold tests as well.
Commit: 1fdd2a0

## GJ.3: Contextual info column — InfoColumnManager
The fixed info column (always showing Objective + Legend + Violations) has been replaced with a contextual system. InfoColumnManager is a VBoxContainer that manages three mutually-exclusive views. The default PuzzleOverview shows the same three panels as before. Clicking a reagent in the tray or on the grid switches to ReagentDetail, showing the reagent's shape, name, flavor text, and aspect breakdown — all the info that was previously in a floating popup. Clicking an aspect in the legend switches to AspectDetail, showing aspect description, reactions, and which tray reagents carry it. Back button or Escape returns to the overview. This eliminates the floating ReagentProfilePanel popup (with its ZIndex=100, fade animations, and viewport clamping) in favor of stable embedded content in the sidebar. Cleaner UX, especially at narrow viewport widths.
Commit: 6533bca

## GJ.4: Bag movement UI — select, drag to move, right-click to rotate
Wired the existing MetaGrid.MoveBag()/RotateBag() Core methods to interactive UI in MetaGridView. Click an empty cell within a bag to select it (cyan border highlight, mutually exclusive with reagent selection). While selected, click the bag again to start dragging — mouse motion shows a ghost preview (blue=valid, red=invalid placement). Click to confirm the move, or right-click to cancel the drag. Right-click without dragging rotates the bag 90° CW. Escape or clicking outside deselects. Added CanMoveBag() to MetaGrid as a read-only validation method for the ghost preview (avoids grid mutation during drag). Three new signals (BagSelected, BagMoved, BagRotated) wired through WebDemoScreen with status bar messages.
Commit: 2fb8a8b

## Code Review
Reviewed GJ.3 (InfoColumnManager) and GJ.4 (bag movement UI) commits. Code quality is clean — good Core/Game separation, consistent UI patterns, proper mutual exclusivity between bag and reagent selection. One P2 finding: CanMoveBag doesn't check HasLockedOrphanedReagent like MoveBag does, so ghost preview could show valid for a rejected move — but locked spanning reagents are not reachable in current game flow, so no fix needed yet. No P0/P1 issues. 811 tests pass, build clean.

## GJ.5: Art loading infrastructure — AssetPreviewScene + sprite swap
Created the SpriteSwapper fallback chain (custom/ → reagents/) and AssetPreview tool scene for browsing all 24 catalog sprites at multiple zoom levels. This is the foundation for dropping in real art — any PNG in art/sprites/custom/{reagentId}.png automatically replaces the dev-generated sprite sheet.
Commit: 247f381

## GJ.5b: Wire existing tiling_suitable sprites as proof-of-concept
First end-to-end test of the "drop PNG → shows in game" workflow. Mapped 7 RetroDiffusion images from the tiling_suitable batch to aspect-based monomino sprites (e.g., the firevine leaf/celtic image → monomino_hot.png). Extended SpriteSwapper with a monomino fallback path: when loading a sprite for a reagent, it now also checks monomino_{aspectId}.png, so every monomino reagent of a given aspect benefits from a single shared sprite. Extended ReagentSpriteSheet with IsSingleTile detection — a 32x32 single-tile image is used as-is for all visual states rather than attempting sprite sheet region extraction. Interesting design trade-off: the monomino sprites are shared across all reagents of an aspect (e.g., both the monomino Hot item and any future Hot reagent without a custom sprite), which is a natural fallback but means the art isn't item-specific.
Commit: e4b10de

## SI.1: UIThemeLoader — sprite texture loading with fallback
Built the centralized UI texture loader that the sprite integration phase depends on. UIThemeLoader is a static class with five methods covering the full art/sprites/ui/ directory structure: panels (parchment/leather backgrounds as 9-patch StyleBoxTexture), aspect tiles (32x32 icons), decorations (flourishes/separator/sigil), buttons (sliced Kit 01 elements as 9-patch StyleBoxTexture), and bag themes (border textures for NinePatchRect). Every method gracefully degrades — StyleBoxFlat fallback for panels/buttons, null for textures — so the game always renders even without art assets. Follows the SpriteSwapper pattern with static dictionary caching and GD.PushWarning on missing files.
Commit: eedb1e8

## Code Review (b)
Reviewed SI.1 (UIThemeLoader) and SI.2 (ComponentInspector) commits. Found one DRY violation in UIThemeLoader — three identical texture-loading methods — and extracted a `LoadTexture` helper. ComponentInspector is 676 lines but acceptable for a developer tool; hardcoded colors and a redundant cast noted as P4 only. No P0/P1 issues. 811 tests pass.

## SI.2: Component Inspector Scene — isolated UI element preview
Created a dedicated tool scene for verifying sprite integration one component at a time. The inspector renders each UI element type (panels, buttons, aspect tiles, status bars, decorations, bag frames) in isolation against a neutral background, with a dropdown to select component type and a toggle to flip between sprite textures and procedural fallback. On launch it auto-cycles through all 9 component types, printing structured [COMPONENT]/[SPRITE_LOADED]/[SCREENSHOT] output and saving PNGs. This gives the integration pipeline a repeatable visual test — run the scene, check the screenshots, compare sprite vs procedural side-by-side.
Commit: f96277e

## SI.3: Panel background textures — parchment and leather
First visible sprite integration into the actual game UI. Replaced inline StyleBoxFlat declarations with UIThemeLoader.GetPanelStyleBox() calls — header panel gets ornate parchment_bordered, info column panels get plain parchment, reagent tray gets dark_leather. The interesting trade-off was text contrast: the existing light-on-dark palette (white/gold text on near-black backgrounds) is unreadable against warm tan parchment, so all affected text shifted to dark brown/sepia tones. ObjectivePanel needed a PanelContainer wrapper in InfoColumnManager since it's a VBoxContainer with no native panel stylebox. Net effect: -20 lines (verbose StyleBoxFlat blocks replaced by one-liners).
Commit: 5206d04

## SI.4: Aspect tile sprites in legend and status bar
Wired the 8 generated aspect tile sprites (32x32 pixel art PNGs) into the two UI components that show aspect icons. AspectLegendEntry switches from a plain colored square to a TextureRect with nearest-neighbor filtering, maintaining the same 16x16 display size. StatusBarPanel adds a tile icon before each aspect label. Both paths fall back to the existing procedural ColorRect when textures are missing, so the game still runs without art assets. Changed _icon field from ColorRect to Control to accommodate the polymorphic icon type.
Commit: 0c40ede

## SI.5: Decorative elements — flourishes, separators, sigil
Added three decorative sprite elements to WebDemoScreen. The corner flourishes were the most interesting: Godot's PanelContainer manages child sizing, so the flourishes can't be children of the header panel itself. Instead they're overlaid as direct children of the WebDemoScreen Control, positioned absolutely relative to the header panel's global rect. The position recalculates on both the header's Resized signal and the screen's AdaptLayout, covering both initial layout and window resize. The vine separator replaces the plain HSeparator with a KeepAspectCentered TextureRect — it naturally centers at the right height without stretching. Title sigil sits inline in the header HBox before the game title.
Commit: 05bca8e
