#!/bin/bash
# Ralph loop for Alchemist Courier
# Runs Claude Code in a loop with role rotation: implement, code review,
# integration testing, and architectural review.
#
# Usage:
#   ./scripts/ralph/ralph.sh              # Default 20 iterations
#   ./scripts/ralph/ralph.sh 50           # 50 iterations max
#   ./scripts/ralph/ralph.sh 10 --dry     # Dry run (echo prompt, don't run)
#
# Prerequisites:
#   - claude CLI available on PATH (native install v2+)
#   - Git Bash on Windows (or any bash shell)
#   - Python 3 on PATH (for phase detection)

set -e

# Claude Code on Windows needs to know where git-bash lives
if [ -z "$CLAUDE_CODE_GIT_BASH_PATH" ]; then
    for candidate in \
        "/c/Program Files/Git/bin/bash.exe" \
        "/d/Program Files/Git/bin/bash.exe" \
        "$(cygpath -u "$(cygpath -w /usr/bin/bash | sed 's|/usr/bin/bash.exe|/bin/bash.exe|')" 2>/dev/null)"
    do
        if [ -f "$candidate" ]; then
            export CLAUDE_CODE_GIT_BASH_PATH="$(cygpath -w "$candidate")"
            echo "[RALPH] Set CLAUDE_CODE_GIT_BASH_PATH=$CLAUDE_CODE_GIT_BASH_PATH"
            break
        fi
    done
fi

MAX_ITER=${1:-20}
DRY_RUN=${2:-""}
ITER=0
IMPL_COUNT=0
LAST_CODE_REVIEW=0
LAST_INTEGRATION_TEST=0
PROJECT_DIR="$(cd "$(dirname "$0")/../.." && pwd)"
PROMPTS_DIR="$PROJECT_DIR/scripts/ralph/prompts"
PRD_FILE="$PROJECT_DIR/prd.json"
PHASE_SCRIPT="$PROJECT_DIR/scripts/ralph/check-phase.py"

cd "$PROJECT_DIR"

# Ensure dev branch exists and is checked out
if ! git rev-parse --verify dev >/dev/null 2>&1; then
    echo "[RALPH] Creating dev branch from master..."
    git checkout -b dev master
else
    echo "[RALPH] Checking out dev branch..."
    git checkout dev 2>/dev/null || true
fi

echo "========================================"
echo " Ralph Loop — Alchemist Courier"
echo " Max iterations: $MAX_ITER"
echo " Project: $PROJECT_DIR"
echo " Roles: implement, review-code, review-integration, review-architecture"
echo "========================================"

while [ $ITER -lt $MAX_ITER ]; do
    ITER=$((ITER + 1))
    echo ""
    echo "[RALPH] ──────────────────────────────"
    echo "[RALPH] Iteration $ITER / $MAX_ITER"
    echo "[RALPH] Time: $(date '+%Y-%m-%d %H:%M:%S')"
    echo "[RALPH] Impl count: $IMPL_COUNT"
    echo "[RALPH] ──────────────────────────────"

    # Check if all tasks are done
    if ! grep -q '"pending"' "$PRD_FILE" 2>/dev/null; then
        echo "[RALPH] All tasks complete! No pending tasks in prd.json."
        break
    fi

    # Count remaining tasks
    PENDING=$(grep -c '"pending"' "$PRD_FILE" 2>/dev/null | tr -d '[:space:]')
    DONE=$(grep -c '"done"' "$PRD_FILE" 2>/dev/null | tr -d '[:space:]')
    BLOCKED=$(grep -c '"blocked"' "$PRD_FILE" 2>/dev/null | tr -d '[:space:]')
    : "${PENDING:=0}" "${DONE:=0}" "${BLOCKED:=0}"
    echo "[RALPH] Tasks — pending: $PENDING, done: $DONE, blocked: $BLOCKED"

    # ── Role selection ──
    # Check for phase boundary first
    PHASE_RESULT=$(python "$PHASE_SCRIPT" "$PRD_FILE" 2>/dev/null || echo "no")
    PHASE_COMPLETE=$(echo "$PHASE_RESULT" | head -1)

    if [ "$PHASE_COMPLETE" = "yes" ]; then
        NEW_PHASE=$(echo "$PHASE_RESULT" | tail -1)
        ROLE="review-architecture"
        echo "[RALPH] Phase completed: $NEW_PHASE — triggering architectural review"
    elif [ $((IMPL_COUNT - LAST_INTEGRATION_TEST)) -ge 5 ]; then
        ROLE="review-integration"
        LAST_INTEGRATION_TEST=$IMPL_COUNT
        echo "[RALPH] 5 implementations since last integration test — triggering"
    elif [ $((IMPL_COUNT - LAST_CODE_REVIEW)) -ge 3 ]; then
        ROLE="review-code"
        LAST_CODE_REVIEW=$IMPL_COUNT
        echo "[RALPH] 3 implementations since last code review — triggering"
    else
        ROLE="implement"
    fi

    PROMPT_FILE="$PROMPTS_DIR/$ROLE.md"
    echo "[RALPH] Role: $ROLE"
    echo "[RALPH] Prompt: $PROMPT_FILE"

    if [ ! -f "$PROMPT_FILE" ]; then
        echo "[RALPH] ERROR: Prompt file not found: $PROMPT_FILE"
        exit 1
    fi

    if [ "$DRY_RUN" = "--dry" ]; then
        echo "[RALPH] DRY RUN — would run: claude -p \"\$(cat $PROMPT_FILE)\" --output-format text"
        echo "[RALPH] Prompt preview (first 5 lines):"
        head -5 "$PROMPT_FILE"
        echo "[RALPH] ...skipping execution."
    else
        # Run Claude Code with the selected prompt
        echo "[RALPH] Starting Claude Code ($ROLE)..."
        claude -p "$(cat "$PROMPT_FILE")" --output-format text || {
            echo "[RALPH] Claude Code exited with error (code $?). Continuing..."
        }
    fi

    # Increment impl counter for implementation runs (including dry run, for accurate simulation)
    if [ "$ROLE" = "implement" ]; then
        IMPL_COUNT=$((IMPL_COUNT + 1))
    fi

    echo "[RALPH] Iteration $ITER complete ($ROLE)."

    # Brief pause between iterations
    if [ $ITER -lt $MAX_ITER ]; then
        echo "[RALPH] Sleeping 5 seconds before next iteration..."
        sleep 5
    fi
done

echo ""
echo "========================================"
echo " Ralph Loop finished after $ITER iterations."
DONE=$(grep -c '"done"' "$PRD_FILE" 2>/dev/null | tr -d '[:space:]')
PENDING=$(grep -c '"pending"' "$PRD_FILE" 2>/dev/null | tr -d '[:space:]')
BLOCKED=$(grep -c '"blocked"' "$PRD_FILE" 2>/dev/null | tr -d '[:space:]')
: "${DONE:=0}" "${PENDING:=0}" "${BLOCKED:=0}"
echo " Final: done=$DONE, pending=$PENDING, blocked=$BLOCKED"
echo "========================================"
