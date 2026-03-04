# Ralph loop for Alchemist Courier (PowerShell)
# Runs Claude Code in a loop with role rotation: implement, code review,
# integration testing, and architectural review.
#
# Usage:
#   .\scripts\ralph\ralph.ps1              # Default 20 iterations
#   .\scripts\ralph\ralph.ps1 -MaxIter 50  # 50 iterations max
#   .\scripts\ralph\ralph.ps1 -DryRun      # Dry run (echo prompt, don't run)
#
# Prerequisites:
#   - claude CLI available on PATH (native install v2+)
#   - Python 3 on PATH (for phase detection)

param(
    [int]$MaxIter = 20,
    [switch]$DryRun
)

# Use Continue for native commands (git writes info to stderr which would
# otherwise be treated as terminating errors). We check $LASTEXITCODE manually.
$ErrorActionPreference = "Continue"
$R = "[RALPH]"

# Prevent "nested session" error if launched from a terminal that inherited
# Claude Desktop's environment variables
if ($env:CLAUDECODE) { Remove-Item Env:\CLAUDECODE -ErrorAction SilentlyContinue }

# -- Preflight checks --

# 1. Claude CLI must be available
$claudeCmd = Get-Command claude -ErrorAction SilentlyContinue
if (-not $claudeCmd) {
    Write-Host "$R FATAL: 'claude' CLI not found on PATH." -ForegroundColor Red
    Write-Host "$R Install: irm https://claude.ai/install.ps1 | iex"
    exit 1
}

# 2. Set CLAUDE_CODE_GIT_BASH_PATH if not already set
if (-not $env:CLAUDE_CODE_GIT_BASH_PATH) {
    $candidates = @(
        'D:\Program Files\Git\bin\bash.exe',
        'C:\Program Files\Git\bin\bash.exe',
        'C:\Program Files (x86)\Git\bin\bash.exe'
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            $env:CLAUDE_CODE_GIT_BASH_PATH = $candidate
            break
        }
    }
}

if (-not $env:CLAUDE_CODE_GIT_BASH_PATH) {
    Write-Host "$R FATAL: Could not find git-bash. Set CLAUDE_CODE_GIT_BASH_PATH manually." -ForegroundColor Red
    Write-Host "$R Example: `$env:CLAUDE_CODE_GIT_BASH_PATH = 'D:\Program Files\Git\bin\bash.exe'"
    exit 1
}
Write-Host "$R CLAUDE_CODE_GIT_BASH_PATH=$env:CLAUDE_CODE_GIT_BASH_PATH"

# 3. Smoke test claude CLI
try {
    $claudeVersion = & claude --version 2>&1 | Out-String
    if ($claudeVersion -match "Claude Code") {
        Write-Host "$R Claude CLI: $($claudeVersion.Trim())"
    } else {
        Write-Host "$R Claude CLI: version check returned unexpected output" -ForegroundColor Yellow
        Write-Host "$R   Output: $($claudeVersion.Trim())"
    }
} catch {
    Write-Host "$R WARNING: claude --version failed: $_" -ForegroundColor Yellow
}

# 4. Python must be available (for phase detection)
$pythonCmd = Get-Command python -ErrorAction SilentlyContinue
if (-not $pythonCmd) {
    Write-Host "$R WARNING: 'python' not found. Phase detection will be disabled." -ForegroundColor Yellow
}

# -- Path setup --
$ProjectDir  = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$PromptsDir  = Join-Path $ProjectDir "scripts\ralph\prompts"
$PrdFile     = Join-Path $ProjectDir "prd.json"
$PhaseScript = Join-Path $ProjectDir "scripts\ralph\check-phase.py"
$LogDir      = Join-Path $ProjectDir "scripts\ralph\logs"

# Ensure log directory exists
if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir -Force | Out-Null }

Set-Location $ProjectDir

# -- Ensure dev branch exists and is checked out --
$devCheck = & git rev-parse --verify dev 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "$R Creating dev branch from master..."
    & git checkout -b dev master 2>&1 | Out-Null
} else {
    Write-Host "$R Checking out dev branch..."
    & git checkout dev 2>&1 | Out-Null
}

# -- State variables --
$Iter = 0
$ImplCount = 0
$LastCodeReview = 0
$LastIntegrationTest = 0

Write-Host "========================================"
Write-Host " Ralph Loop -- Alchemist Courier"
Write-Host " Max iterations: $MaxIter"
Write-Host " Project: $ProjectDir"
Write-Host " Logs: $LogDir"
Write-Host " Roles: implement, review-code, review-integration, review-architecture"
Write-Host "========================================"

while ($Iter -lt $MaxIter) {
    $Iter++
    Write-Host ""
    Write-Host "$R ----------------------------------"
    Write-Host "$R Iteration $Iter / $MaxIter"
    Write-Host ("$R Time: " + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
    Write-Host "$R Impl count: $ImplCount"
    Write-Host "$R ----------------------------------"

    # Check if all tasks are done
    $prdContent = Get-Content $PrdFile -Raw -ErrorAction SilentlyContinue
    if (-not $prdContent -or $prdContent -notmatch '"pending"') {
        Write-Host "$R All tasks complete! No pending tasks in prd.json." -ForegroundColor Green
        break
    }

    # Count remaining tasks
    $Pending  = ([regex]::Matches($prdContent, '"pending"')).Count
    $Done     = ([regex]::Matches($prdContent, '"done"')).Count
    $Blocked  = ([regex]::Matches($prdContent, '"blocked"')).Count
    Write-Host "$R Tasks -- pending: $Pending, done: $Done, blocked: $Blocked"

    # -- Role selection --
    $Role = "implement"

    # Check for phase boundary first
    if ($pythonCmd) {
        try {
            $phaseResult = & python $PhaseScript $PrdFile 2>&1 | Out-String
            $phaseLines = $phaseResult.Trim() -split "`n"
            $phaseComplete = $phaseLines[0].Trim()

            if ($phaseComplete -eq "yes") {
                $newPhase = if ($phaseLines.Count -gt 1) { $phaseLines[-1].Trim() } else { "unknown" }
                $Role = "review-architecture"
                Write-Host "$R Phase completed: $newPhase -- triggering architectural review" -ForegroundColor Cyan
            }
        } catch {
            Write-Host "$R Phase detection failed: $_" -ForegroundColor Yellow
        }
    }

    # Only apply counter-based roles if phase detection didn't trigger
    if ($Role -eq "implement") {
        if (($ImplCount - $LastIntegrationTest) -ge 5) {
            $Role = "review-integration"
            $LastIntegrationTest = $ImplCount
            Write-Host "$R 5 implementations since last integration test -- triggering" -ForegroundColor Cyan
        } elseif (($ImplCount - $LastCodeReview) -ge 3) {
            $Role = "review-code"
            $LastCodeReview = $ImplCount
            Write-Host "$R 3 implementations since last code review -- triggering" -ForegroundColor Cyan
        }
    }

    $PromptFile = Join-Path $PromptsDir "$Role.md"
    $timestamp  = Get-Date -Format 'yyyyMMdd-HHmmss'
    $iterPadded = $Iter.ToString("D3")
    $IterLog    = Join-Path $LogDir "iter-${iterPadded}-${Role}-${timestamp}.log"

    Write-Host "$R Role: $Role"
    Write-Host "$R Prompt: $PromptFile"
    Write-Host "$R Log: $IterLog"

    if (-not (Test-Path $PromptFile)) {
        Write-Host "$R FATAL: Prompt file not found: $PromptFile" -ForegroundColor Red
        exit 1
    }

    if ($DryRun) {
        Write-Host "$R DRY RUN -- would run: claude -p <prompt> --output-format text" -ForegroundColor Yellow
        Write-Host "$R Prompt preview (first 5 lines):"
        Get-Content $PromptFile -TotalCount 5 | ForEach-Object { Write-Host "  $_" }
        Write-Host "$R ...skipping execution."
    } else {
        # Run Claude Code with the selected prompt, logging output
        Write-Host "$R Starting Claude Code ($Role)..."
        Write-Host "$R Monitor log: Get-Content -Wait '$IterLog'"

        $promptContent = Get-Content $PromptFile -Raw
        $claudeExit = 0

        # Create log file immediately so Get-Content -Wait works from the start
        "# Ralph iteration $Iter - $Role - started $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Out-File -FilePath $IterLog
        Write-Host "$R Claude is working (this can take several minutes with no output)..."

        # Tool permissions: file tools are project-scoped by default.
        # Bash is restricted to specific command patterns.
        $toolsArg = 'Edit,Write,Read,Glob,Grep,WebSearch,WebFetch,"Bash(git *)","Bash(dotnet *)","Bash(python *)","Bash(mkdir *)","Bash(rm *)","Bash(cp *)","Bash(mv *)","Bash(ls *)","Bash(cat *)","Bash(head *)","Bash(tail *)",mcp__godot__*'

        try {
            # PowerShell provides a real console -- no winpty needed.
            # Pipe through Tee-Object to both display and log.
            & claude -p $promptContent --output-format text --allowedTools $toolsArg 2>&1 | Tee-Object -FilePath $IterLog
            $claudeExit = $LASTEXITCODE
        } catch {
            Write-Host "$R Claude Code threw an exception: $_" -ForegroundColor Red
            $claudeExit = 1
        }

        # Check log output
        $logLines = 0
        if (Test-Path $IterLog) {
            $logLines = (Get-Content $IterLog | Measure-Object -Line).Lines
        }
        Write-Host "$R Log saved: $IterLog ($logLines lines)"

        # Fail loudly if claude produced no output
        if ($logLines -le 1) {
            Write-Host "$R ERROR: Claude produced no output. Possible causes:" -ForegroundColor Red
            Write-Host "$R   - Not authenticated (run 'claude' interactively first)"
            Write-Host "$R   - API key / billing issue"
            Write-Host "$R   - Git-bash not found (check CLAUDE_CODE_GIT_BASH_PATH)"
            Write-Host "$R Log contents:"
            if (Test-Path $IterLog) {
                Get-Content $IterLog | Write-Host
            } else {
                Write-Host "(empty)"
            }
            Write-Host "$R Aborting loop." -ForegroundColor Red
            exit 1
        }

        if ($claudeExit -ne 0) {
            Write-Host "$R Claude Code exited with error (code $claudeExit). Continuing..." -ForegroundColor Yellow
        }
    }

    # Increment impl counter for implementation runs (including dry run, for accurate simulation)
    if ($Role -eq "implement") {
        $ImplCount++
    }

    Write-Host "$R Iteration $Iter complete ($Role)."

    # Brief pause between iterations
    if ($Iter -lt $MaxIter) {
        Write-Host "$R Sleeping 5 seconds before next iteration..."
        Start-Sleep -Seconds 5
    }
}

Write-Host ""
Write-Host "========================================"
Write-Host " Ralph Loop finished after $Iter iterations."
$prdContent = Get-Content $PrdFile -Raw -ErrorAction SilentlyContinue
$Done    = ([regex]::Matches($prdContent, '"done"')).Count
$Pending = ([regex]::Matches($prdContent, '"pending"')).Count
$Blocked = ([regex]::Matches($prdContent, '"blocked"')).Count
Write-Host " Final: done=$Done, pending=$Pending, blocked=$Blocked"
Write-Host " Logs: $LogDir"
Write-Host "========================================"
