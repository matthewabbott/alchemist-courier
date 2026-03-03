# Agent Workflow Guide

Conventions for AI agents (Claude Code sessions) working on Alchemist Courier.

## Session Start

1. Read `CLAUDE.md` (auto-loaded by Claude Code)
2. Read the most recent entry in `docs/worklog/` to understand current state
3. Check `git status`, `git log --oneline -10`, and current branch
4. Review relevant `docs/` files for the planned work

## Session End

1. Ensure all changes are committed on a feature branch (not master)
2. Create a worklog entry (see below)
3. Update `CLAUDE.md` if any conventions changed
4. Note any known issues or incomplete work in the worklog

## Worklog Conventions

### When to Create

- After any session that produces commits (code, docs, or config changes)
- NOT needed for pure Q&A / planning sessions with no artifacts

### File Naming

```
docs/worklog/YYYY-MM-DD-brief-title.md
```

If multiple sessions on the same day: `YYYY-MM-DD-brief-title-2.md`

### Template

```markdown
# Session: [Descriptive Title]
**Date:** YYYY-MM-DD
**Branch:** `branch-name`

## Summary
One paragraph: what was accomplished and why.

## Changes
Bulleted list of new/modified files with one-line descriptions.

## Design Decisions
Key choices made during the session that future agents should know.
Include the reasoning, not just the decision.

## Commits
| Hash | Message |
|------|---------|
| `abc1234` | Commit message |

## Known Issues
Anything incomplete, fragile, or surprising.

## Next Steps
Bulleted checklist of logical follow-up work.
```

### Guidelines

- Keep it brief — this is a journal, not a novel
- Focus on **what** and **why**, not blow-by-blow of the session
- Include commit hashes so future agents can `git show` for details
- Design decisions are the most valuable part — explain trade-offs
- Next steps should be actionable and specific

## Branching

- Feature branches: `feature/descriptive-name`
- Bug fixes: `fix/descriptive-name`
- Docs/config only: `docs/descriptive-name`
- Branch from `master` unless building on an unmerged feature branch

## Commit Messages

- Imperative mood, present tense ("Add grid system" not "Added grid system")
- First line: concise summary (under 72 chars)
- Body: explain **why**, not just **what**
- Reference design docs when relevant
