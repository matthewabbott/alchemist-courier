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

# Prevent "nested session" error if launched from a terminal that inherited
# Claude Desktop's environment variables
unset CLAUDECODE 2>/dev/null || true

# ── Preflight checks ──
# Fail loudly if prerequisites are missing

# 1. Claude CLI must be available
if ! command -v claude >/dev/null 2>&1; then
    echo "[RALPH] FATAL: 'claude' CLI not found on PATH."
    echo "[RALPH] Install: irm https://claude.ai/install.ps1 | iex"
    exit 1
fi

# 2. Claude Code on Windows needs to know where git-bash lives
if [ -z "$CLAUDE_CODE_GIT_BASH_PATH" ]; then
    for candidate in \
        "/c/Program Files/Git/bin/bash.exe" \
        "/d/Program Files/Git/bin/bash.exe" \
        "$(cygpath -u "$(cygpath -w /usr/bin/bash | sed 's|/usr/bin/bash.exe|/bin/bash.exe|')" 2>/dev/null)"
    do
        if [ -f "$candidate" ]; then
            export CLAUDE_CODE_GIT_BASH_PATH="$(cygpath -w "$candidate")"
            break
        fi
    done
fi

if [ -z "$CLAUDE_CODE_GIT_BASH_PATH" ]; then
    echo "[RALPH] FATAL: Could not find git-bash. Set CLAUDE_CODE_GIT_BASH_PATH manually."
    echo "[RALPH] Example: export CLAUDE_CODE_GIT_BASH_PATH='D:\\Program Files\\Git\\bin\\bash.exe'"
    exit 1
fi
echo "[RALPH] CLAUDE_CODE_GIT_BASH_PATH=$CLAUDE_CODE_GIT_BASH_PATH"

# 3. Use winpty on Git Bash/mintty (needed for proper I/O on Windows)
WINPTY=""
if command -v winpty >/dev/null 2>&1; then
    WINPTY="winpty"
    echo "[RALPH] Using winpty for mintty compatibility"
fi

# 4. Verify claude can actually start (smoke test without winpty — --version doesn't need tty)
CLAUDE_SMOKE=$(claude --version 2>&1) || true
if echo "$CLAUDE_SMOKE" | grep -q "Claude Code"; then
    echo "[RALPH] Claude CLI: $CLAUDE_SMOKE"
else
    # Fallback: try without capturing (winpty can't be captured in subshell)
    echo "[RALPH] Claude CLI: version check skipped (mintty/winpty mode)"
fi

# 4. Python must be available (for phase detection)
if ! command -v python >/dev/null 2>&1; then
    echo "[RALPH] WARNING: 'python' not found. Phase detection will be disabled."
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
LOG_DIR="$PROJECT_DIR/scripts/ralph/logs"

# Ensure log directory exists
mkdir -p "$LOG_DIR"

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
echo " Logs: $LOG_DIR"
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
    ITER_LOG="$LOG_DIR/iter-$(printf '%03d' $ITER)-$ROLE-$(date '+%Y%m%d-%H%M%S').log"
    echo "[RALPH] Role: $ROLE"
    echo "[RALPH] Prompt: $PROMPT_FILE"
    echo "[RALPH] Log: $ITER_LOG"

    if [ ! -f "$PROMPT_FILE" ]; then
        echo "[RALPH] FATAL: Prompt file not found: $PROMPT_FILE"
        exit 1
    fi

    if [ "$DRY_RUN" = "--dry" ]; then
        echo "[RALPH] DRY RUN — would run: claude -p \"\$(cat $PROMPT_FILE)\" --output-format text"
        echo "[RALPH] Prompt preview (first 5 lines):"
        head -5 "$PROMPT_FILE"
        echo "[RALPH] ...skipping execution."
    else
        # Run Claude Code with the selected prompt, logging output
        echo "[RALPH] Starting Claude Code ($ROLE)..."
        CLAUDE_EXIT=0
        if [ -n "$WINPTY" ]; then
            # winpty can't be piped through tee (strips the tty).
            # Redirect to log file; user can tail -f from another terminal.
            echo "[RALPH] Tail the log: tail -f \"$ITER_LOG\""
            $WINPTY claude -p "$(cat "$PROMPT_FILE")" --output-format text >"$ITER_LOG" 2>&1 || CLAUDE_EXIT=$?
            echo "[RALPH] ── Claude output (last 20 lines) ──"
            tail -20 "$ITER_LOG"
            echo "[RALPH] ── end ──"
        else
            echo "[RALPH] Tail the log: tail -f $ITER_LOG"
            claude -p "$(cat "$PROMPT_FILE")" --output-format text 2>&1 | tee "$ITER_LOG" || CLAUDE_EXIT=$?
        fi

        LOG_LINES=$(wc -l < "$ITER_LOG" 2>/dev/null | tr -d '[:space:]')
        echo "[RALPH] Log saved: $ITER_LOG ($LOG_LINES lines)"

        # Fail loudly if claude produced no output (likely auth/config issue)
        if [ "${LOG_LINES:-0}" -le 1 ]; then
            echo "[RALPH] ERROR: Claude produced no output. Possible causes:"
            echo "[RALPH]   - Not authenticated (run 'claude' interactively first)"
            echo "[RALPH]   - Git-bash not found (check CLAUDE_CODE_GIT_BASH_PATH)"
            echo "[RALPH]   - API key / billing issue"
            echo "[RALPH] Log contents:"
            cat "$ITER_LOG" 2>/dev/null || echo "(empty)"
            echo "[RALPH] Aborting loop."
            exit 1
        fi

        if [ $CLAUDE_EXIT -ne 0 ]; then
            echo "[RALPH] Claude Code exited with error (code $CLAUDE_EXIT). Continuing..."
        fi
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
echo " Logs: $LOG_DIR"
echo "========================================"
