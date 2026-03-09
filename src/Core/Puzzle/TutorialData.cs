using System.Collections.Generic;

namespace AlchemistCourier.Core.Puzzle;

/// <summary>
/// Static provider for the tutorial lesson sequence.
/// Each lesson introduces one concept, progressing from pure spatial placement
/// to chemistry interactions.
/// </summary>
public static class TutorialData
{
    /// <summary>
    /// Returns the first 5 tutorial lessons from the MVP specification.
    /// </summary>
    public static List<TutorialLesson> GetLessons() => new()
    {
        // Lesson 1: Basic placement — 1 bag, 2 single-cell reagents, no aspects.
        new TutorialLesson
        {
            LessonNumber = 1,
            ConceptName = "Basic Placement",
            HintText = "Place all reagents into the bag. Click a reagent, then click a cell in the bag.",
            FixedSeed = 1001,
            DifficultyParams = new DifficultyParams(
                bagCount: 1,
                bagMinSize: 6,
                bagMaxSize: 9,
                reagentCount: 2,
                maxAspectCount: 0,
                spareSpacePercent: 0.5f,
                allowSpecialBags: false,
                maxHints: 3),
            RequiredPuzzleType = PuzzleType.FitAllSafely,
        },

        // Lesson 2: Shapes & rotation — 1 bag, 3 reagents including multi-cell shapes.
        // Player must rotate to fit.
        new TutorialLesson
        {
            LessonNumber = 2,
            ConceptName = "Shapes & Rotation",
            HintText = "Some reagents have unusual shapes. Press R to rotate a selected reagent before placing it.",
            FixedSeed = 2001,
            DifficultyParams = new DifficultyParams(
                bagCount: 1,
                bagMinSize: 6,
                bagMaxSize: 9,
                reagentCount: 3,
                maxAspectCount: 0,
                spareSpacePercent: 0.3f,
                allowSpecialBags: false,
                maxHints: 3),
            RequiredPuzzleType = PuzzleType.FitAllSafely,
        },

        // Lesson 3: Multiple reagents, tight fit — 1 bag, 4 reagents that nearly fill the bag.
        // Pure spatial puzzle.
        new TutorialLesson
        {
            LessonNumber = 3,
            ConceptName = "Tight Fit",
            HintText = "Space is limited! Fit all reagents into the bag. Try different rotations and positions.",
            FixedSeed = 3001,
            DifficultyParams = new DifficultyParams(
                bagCount: 1,
                bagMinSize: 9,
                bagMaxSize: 12,
                reagentCount: 4,
                maxAspectCount: 0,
                spareSpacePercent: 0.1f,
                allowSpecialBags: false,
                maxHints: 3),
            RequiredPuzzleType = PuzzleType.FitAllSafely,
        },

        // Lesson 4: Aspects: Hot — introduces the Hot aspect and cumulative overheating.
        new TutorialLesson
        {
            LessonNumber = 4,
            ConceptName = "Aspects: Hot",
            HintText = "Reagents can have aspects. Hot reagents contribute to overheating — carry too many and you'll get burned!",
            FixedSeed = 4001,
            DifficultyParams = new DifficultyParams(
                bagCount: 1,
                bagMinSize: 6,
                bagMaxSize: 9,
                reagentCount: 3,
                maxAspectCount: 1,
                spareSpacePercent: 0.3f,
                allowSpecialBags: false,
                maxHints: 3),
            RequiredPuzzleType = PuzzleType.FitAllSafely,
        },

        // Lesson 5: Aspects: Volatile — introduces Volatile and adjacency reactions.
        new TutorialLesson
        {
            LessonNumber = 5,
            ConceptName = "Aspects: Volatile",
            HintText = "Volatile reagents are dangerous near hot ones. Watch the placement warnings — red means danger!",
            FixedSeed = 5001,
            DifficultyParams = new DifficultyParams(
                bagCount: 2,
                bagMinSize: 6,
                bagMaxSize: 9,
                reagentCount: 4,
                maxAspectCount: 1,
                spareSpacePercent: 0.3f,
                allowSpecialBags: false,
                maxHints: 3),
            RequiredPuzzleType = PuzzleType.FitAllSafely,
        },
    };
}
