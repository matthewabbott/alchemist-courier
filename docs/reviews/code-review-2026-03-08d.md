# Code Review — 2026-03-08d
**Commits reviewed:** 55aea9c..660f6e9 (GO.11 ViolationListPanel, integration tests for volatile/bag state/bag movement)

## Findings

### DRY — Duplicated refresh sequence in WebDemoScreen
- **`src/Game/WebDemoScreen.cs:484-489` (OnResetPressed)** — Duplicated the exact 5-line refresh sequence already encapsulated in `RefreshAfterGridChange()`.
  Before:
  ```csharp
  _gridView.RefreshDisplay();
  _reagentTray.RefreshDisplay();
  UpdateUndoRedoButtons();
  UpdateStatusThresholds();
  _violationPanel.Refresh();
  ```
  After: `RefreshAfterGridChange();`

- **`src/Game/WebDemoScreen.cs:505-510` (OnHintPressed)** — Same duplication.
  Before: same 5 lines inline.
  After: `RefreshAfterGridChange();`

**Why**: The `_violationPanel.Refresh()` call was added to GO.11's integration, but was added inline to OnResetPressed and OnHintPressed instead of recognizing that `RefreshAfterGridChange()` already existed as the single refresh point. With 3 occurrences (RefreshAfterGridChange + two copies), this hit the Rule of Three. P3 quick win → bumped to P2.

### No other issues
- **ViolationListPanel.cs** — Clean 135-line implementation. Good separation: signal-based click forwarding, severity color mapping, flat button styling. No P0-P2 issues.
- **TestHarness.cs** — 247 lines of new integration tests follow the established pattern (setup → action → verify → restore). "Restore DemoPuzzle" block repeated in each test is fine (DAMP > DRY for tests).
- **Core purity** — ViolationListPanel correctly lives in `src/Game/Inventory/`, no Core contamination.

## Actions Taken
- Fixed DRY violation: replaced 2 inline refresh sequences with `RefreshAfterGridChange()` calls (commit 6a8053d)
- No prd.json tasks created (only 1 finding, now fixed)

## Desloppify
- Before: N/A (no desloppify binary available)
- After: N/A
- Top issue: N/A

## Status
ISSUES_FIXED
