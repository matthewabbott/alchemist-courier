"""
Slice UI kit spritesheets into individual elements.

Uses connected-component analysis on the alpha channel to find
distinct non-transparent regions, then extracts each as a PNG.

Usage:
    python scripts/slice_ui_kits.py [--kit N] [--min-size 10]

Options:
    --kit N       Process only kit N (1-9). Default: process all.
    --min-size N  Minimum bounding-box dimension to keep (filters dust). Default: 10.
    --output DIR  Output directory. Default: art/sprites/ui/sliced/
"""

import argparse
import json
import os
from pathlib import Path
from collections import deque

from PIL import Image


def find_connected_components(img, alpha_threshold=10, min_size=10):
    """Find bounding boxes of connected non-transparent regions."""
    width, height = img.size
    pixels = img.load()
    visited = [[False] * width for _ in range(height)]
    components = []

    for y in range(height):
        for x in range(width):
            if visited[y][x]:
                continue
            a = pixels[x, y][3] if len(pixels[x, y]) == 4 else 255
            if a < alpha_threshold:
                visited[y][x] = True
                continue

            # BFS flood fill
            queue = deque([(x, y)])
            visited[y][x] = True
            min_x, min_y = x, y
            max_x, max_y = x, y

            while queue:
                cx, cy = queue.popleft()
                min_x = min(min_x, cx)
                min_y = min(min_y, cy)
                max_x = max(max_x, cx)
                max_y = max(max_y, cy)

                for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                    nx, ny = cx + dx, cy + dy
                    if 0 <= nx < width and 0 <= ny < height and not visited[ny][nx]:
                        na = pixels[nx, ny][3] if len(pixels[nx, ny]) == 4 else 255
                        if na >= alpha_threshold:
                            visited[ny][nx] = True
                            queue.append((nx, ny))

            w = max_x - min_x + 1
            h = max_y - min_y + 1
            if w >= min_size and h >= min_size:
                components.append({
                    "x": min_x,
                    "y": min_y,
                    "w": w,
                    "h": h,
                })

    # Sort top-to-bottom, left-to-right
    components.sort(key=lambda c: (c["y"], c["x"]))
    return components


def merge_nearby_components(components, gap=2):
    """Merge components that are within `gap` pixels of each other.

    Some elements have thin gaps (1-2px) between sub-parts that split
    into separate components. This merges them back together.
    """
    if not components:
        return components

    merged = True
    while merged:
        merged = False
        new_components = []
        used = [False] * len(components)

        for i in range(len(components)):
            if used[i]:
                continue
            a = components[i].copy()
            for j in range(i + 1, len(components)):
                if used[j]:
                    continue
                b = components[j]
                # Check if bounding boxes overlap or are within gap
                if (a["x"] - gap <= b["x"] + b["w"] and
                    b["x"] - gap <= a["x"] + a["w"] and
                    a["y"] - gap <= b["y"] + b["h"] and
                    b["y"] - gap <= a["y"] + a["h"]):
                    # Merge
                    new_x = min(a["x"], b["x"])
                    new_y = min(a["y"], b["y"])
                    a = {
                        "x": new_x,
                        "y": new_y,
                        "w": max(a["x"] + a["w"], b["x"] + b["w"]) - new_x,
                        "h": max(a["y"] + a["h"], b["y"] + b["h"]) - new_y,
                    }
                    used[j] = True
                    merged = True
            new_components.append(a)
            used[i] = True

        components = sorted(new_components, key=lambda c: (c["y"], c["x"]))

    return components


def slice_kit(kit_path, output_dir, kit_num, min_size=10, merge_gap=2):
    """Slice a single UI kit spritesheet."""
    img = Image.open(kit_path).convert("RGBA")
    print(f"\nKit {kit_num:02d}: {img.size[0]}x{img.size[1]}")

    components = find_connected_components(img, min_size=min_size)
    print(f"  Found {len(components)} raw components (>= {min_size}px)")

    components = merge_nearby_components(components, gap=merge_gap)
    print(f"  After merging (gap={merge_gap}): {len(components)} elements")

    elements = []
    for i, comp in enumerate(components):
        x, y, w, h = comp["x"], comp["y"], comp["w"], comp["h"]
        cropped = img.crop((x, y, x + w, y + h))

        name = f"kit{kit_num:02d}_{i:02d}_{w}x{h}"
        filename = f"{name}.png"
        out_path = output_dir / filename
        cropped.save(out_path)

        elements.append({
            "name": name,
            "file": filename,
            "source": f"ui_kit_{kit_num:02d}.png",
            "region": {"x": x, "y": y, "w": w, "h": h},
            "size": {"w": w, "h": h},
        })
        print(f"  [{i:02d}] {x},{y} {w}x{h} -> {filename}")

    return elements


def main():
    parser = argparse.ArgumentParser(description="Slice UI kit spritesheets")
    parser.add_argument("--kit", type=int, help="Process only this kit number (1-9)")
    parser.add_argument("--min-size", type=int, default=10, help="Min dimension to keep")
    parser.add_argument("--merge-gap", type=int, default=2, help="Max gap for merging components")
    parser.add_argument("--output", type=str, default=None, help="Output directory")
    args = parser.parse_args()

    project_root = Path(__file__).parent.parent
    panels_dir = project_root / "art" / "sprites" / "ui" / "panels"
    output_dir = Path(args.output) if args.output else project_root / "art" / "sprites" / "ui" / "sliced"
    output_dir.mkdir(parents=True, exist_ok=True)

    all_elements = {}

    if args.kit:
        kits = [args.kit]
    else:
        kits = list(range(1, 10))

    for kit_num in kits:
        kit_path = panels_dir / f"ui_kit_{kit_num:02d}.png"
        if not kit_path.exists():
            print(f"Kit {kit_num:02d}: NOT FOUND at {kit_path}")
            continue

        elements = slice_kit(kit_path, output_dir, kit_num,
                           min_size=args.min_size, merge_gap=args.merge_gap)
        all_elements[f"kit_{kit_num:02d}"] = elements

    # Write catalog
    catalog_path = output_dir / "catalog.json"
    with open(catalog_path, "w") as f:
        json.dump(all_elements, f, indent=2)
    print(f"\nCatalog written to {catalog_path}")
    print(f"Total elements: {sum(len(v) for v in all_elements.values())}")


if __name__ == "__main__":
    main()
