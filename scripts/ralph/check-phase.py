#!/usr/bin/env python3
"""Check if a phase just completed in prd.json.

Compares current phase completion state against a marker file.
Prints 'yes' if a new phase just completed, 'no' otherwise.
Also prints the name of the newly completed phase if applicable.

Usage: python check-phase.py [path/to/prd.json]
"""

import json
import os
import sys

def get_phase_status(prd_path):
    """Return dict of {phase: {total, done}} from prd.json."""
    with open(prd_path, "r") as f:
        data = json.load(f)

    phases = {}
    for task in data.get("tasks", []):
        phase = task.get("phase", "unknown")
        if phase not in phases:
            phases[phase] = {"total": 0, "done": 0}
        phases[phase]["total"] += 1
        if task.get("status") == "done":
            phases[phase]["done"] += 1

    return phases

def get_completed_phases(phases):
    """Return sorted list of phase names where all tasks are done."""
    return sorted(
        p for p, v in phases.items()
        if v["total"] > 0 and v["total"] == v["done"]
        and p != "review-fix"  # Exclude review-fix phase from completion tracking
    )

def main():
    project_dir = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    prd_path = sys.argv[1] if len(sys.argv) > 1 else os.path.join(project_dir, "prd.json")
    marker_path = os.path.join(project_dir, ".ralph-phase-marker")

    if not os.path.exists(prd_path):
        print("no")
        sys.exit(0)

    phases = get_phase_status(prd_path)
    completed = get_completed_phases(phases)
    completed_str = " ".join(completed)

    # Read previous state
    previous_str = ""
    if os.path.exists(marker_path):
        with open(marker_path, "r") as f:
            previous_str = f.read().strip()

    # Write current state
    with open(marker_path, "w") as f:
        f.write(completed_str)

    # Check if a new phase just completed
    previous_set = set(previous_str.split()) if previous_str else set()
    current_set = set(completed) if completed else set()
    new_phases = current_set - previous_set

    if new_phases:
        print("yes")
        print(" ".join(sorted(new_phases)))
    else:
        print("no")

if __name__ == "__main__":
    main()
