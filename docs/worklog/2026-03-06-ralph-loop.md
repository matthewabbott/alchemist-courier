# Ralph Loop — 2026-03-06

## ART.4: Manuscript reference curation
Scaffolded the art reference document cataloging five historical alchemy and herbal manuscripts that inspire the game's visual identity. The sources span from the 6th-century Vienna Dioscurides (the gold standard for botanical illustration) through the Aztec Badianus Codex and German alchemical Splendor Solis. Each source maps to specific game elements — botanical illustrations for reagent sprites, alchemical diagrams for UI backdrops, woodcuts for menu art. The style translation notes capture the core tension: preserving the hand-drawn quality and earth-tone palettes of medieval manuscripts while adapting them into readable 32x32 pixel art with a cozy tone. This document feeds directly into ART.5's custom RetroDiffusion style creation pipeline.
Commit: 8c56f06

## CQ.1: Extract PuzzleValidator to Core
Moved puzzle win-condition logic out of PuzzleScreen (Godot-dependent) into a pure C# PuzzleValidator in src/Core/Puzzle/. This is the first step of a two-part refactor (CQ.1 extracts, CQ.2 wires PuzzleScreen to use it). The ConfirmResult enum moved cleanly since all consumer files already had the Core.Puzzle using directive. ValidationResult wraps the enum with reaction details and score, using static factory methods for each outcome. PuzzleValidator.Evaluate dispatches on PuzzleType — FitAllSafely checks all-placed + no-reactions, MaximizeValue skips the unplaced check and sums placed reagent values, MeetRequirement delegates to FitAllSafely with a TODO for future requirements.
Commit: ae7ff2a
