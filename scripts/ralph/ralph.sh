#!/bin/bash
# Ralph loop for Alchemist Courier
# Runs Claude Code in a loop, one PRD task per iteration.
#
# Usage:
#   ./scripts/ralph/ralph.sh              # Default 20 iterations
#   ./scripts/ralph/ralph.sh 50           # 50 iterations max
#   ./scripts/ralph/ralph.sh 10 --dry     # Dry run (echo prompt, don't run)
#
# Prerequisites:
#   - claude CLI available on PATH
#   - Git Bash on Windows (or any bash shell)
#   - Run from the project root directory

set -e

MAX_ITER=${1:-20}
DRY_RUN=${2:-""}
ITER=0
PROJECT_DIR="$(cd "$(dirname "$0")/../.." && pwd)"
PROMPT_FILE="$PROJECT_DIR/scripts/ralph/PROMPT.md"
PRD_FILE="$PROJECT_DIR/prd.json"

cd "$PROJECT_DIR"

echo "========================================"
echo " Ralph Loop — Alchemist Courier"
echo " Max iterations: $MAX_ITER"
echo " Project: $PROJECT_DIR"
echo " Prompt: $PROMPT_FILE"
echo "========================================"

while [ $ITER -lt $MAX_ITER ]; do
    ITER=$((ITER + 1))
    echo ""
    echo "[RALPH] ──────────────────────────────"
    echo "[RALPH] Iteration $ITER / $MAX_ITER"
    echo "[RALPH] Time: $(date '+%Y-%m-%d %H:%M:%S')"
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

    if [ "$DRY_RUN" = "--dry" ]; then
        echo "[RALPH] DRY RUN — would run: claude -p \"\$(cat $PROMPT_FILE)\" --output-format text"
        echo "[RALPH] Prompt preview (first 10 lines):"
        head -10 "$PROMPT_FILE"
        echo "[RALPH] ...skipping execution."
    else
        # Run Claude Code with the prompt
        echo "[RALPH] Starting Claude Code..."
        claude -p "$(cat "$PROMPT_FILE")" --output-format text || {
            echo "[RALPH] Claude Code exited with error (code $?). Continuing..."
        }
    fi

    echo "[RALPH] Iteration $ITER complete."

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
