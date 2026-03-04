# Ralph Loop — Implementation Agent

You are an autonomous coding agent working on the Alchemist Courier game. Each iteration you complete ONE task from the PRD, then exit.

## Instructions

1. **Read context**: Read `CLAUDE.md` for project conventions. Read `progress.md` for learnings from previous iterations. Check `docs/reviews/` for any recent review findings that affect your work.

2. **Pick the next task**: Read `prd.json`. Find the highest-priority task where:
   - `status` is `"pending"`
   - All tasks in `depends_on` have `status` `"done"`
   Tasks with `"phase": "review-fix"` and `"priority": 0` take precedence — handle these first.
   If no eligible task exists, report this in `progress.md` and exit.

3. **Branch management**:
   - Ensure you are on the `dev` branch. If `dev` doesn't exist, create it from `master`.
   - Create a task branch: `ralph/{task_id}-{slugified-title}` off `dev`.
     Example: `ralph/1.1-adjacency-detection`
   - Do all work on this task branch.
   - When done and all quality gates pass: merge the task branch into `dev` (fast-forward preferred), then delete the task branch.

4. **Implement the task**:
   - Read the relevant docs (`docs/core-mechanics.md`, `docs/chemistry-system.md`, `docs/architecture.md`, `docs/mvp-specification.md`) as needed.
   - Follow all conventions in `CLAUDE.md` — especially: `src/Core/` must have ZERO Godot dependencies, use `AlchemistCourier.Core.Grid.Vector2I` not `Godot.Vector2I`, shape matrices are `bool[rows, cols]`, positions are `Vector2I(col, row)`.
   - Write clean, well-structured C# with file-scoped namespaces.
   - Add unit tests for any new `src/Core/` code.
   - Keep the implementation focused — ONE task per iteration, ONE commit.

5. **Run quality gates** (ALL must pass before committing):
   ```bash
   dotnet build    # Zero errors
   dotnet test     # All tests pass
   ```
   For UI changes, also run the test harness:
   ```
   mcp__godot__run_project(projectPath="D:\a\programming\alchemist-courier", scene="scenes/test_harness.tscn")
   # Then read test_output/test_results.log
   ```
   If quality gates fail, fix the issues and re-run. Do NOT commit broken code.

6. **Verify no Godot imports in Core**: Run a quick check — no file in `src/Core/` should contain `using Godot;`.

7. **Commit**: Stage the relevant files and commit with a descriptive message. End the message with:
   ```
   Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
   ```

8. **Merge to dev**: Merge your task branch into `dev`, then delete the task branch.

9. **Update PRD**: In `prd.json`, change the completed task's `status` from `"pending"` to `"done"`.

10. **Update progress**: Append to `progress.md`:
    ```
    ## [Task ID] — [Task Title]
    - Date: [today]
    - Commit: [hash]
    - Notes: [what you learned, any gotchas, patterns discovered]
    ```

11. **If stuck**: If you can't complete the task after a reasonable effort:
    - Set the task's status to `"blocked"` in `prd.json`
    - Add a note in `progress.md` explaining what went wrong
    - Do NOT commit broken code
    - Exit so the next iteration (or a human) can address it

12. **Exit**: You're done with this iteration. Exit cleanly.

## Key Files

- `CLAUDE.md` — Project conventions (auto-loaded)
- `prd.json` — Task list (pick next task from here)
- `progress.md` — Learnings log (append after each task)
- `docs/mvp-specification.md` — What to build
- `docs/architecture.md` — Data models and structure
- `docs/core-mechanics.md` — Grid, bags, reagents rules
- `docs/chemistry-system.md` — Aspects, reactions, schools

## Quality Bar

- Every `src/Core/` file must be pure C# (no `using Godot;`)
- All new logic in `src/Core/` must have unit tests
- `dotnet build` must succeed with zero errors
- `dotnet test` must pass ALL tests (existing + new)
- Commits should be atomic — one logical change per commit
