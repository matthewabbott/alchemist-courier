"""
Extract specific named sprites from UI kit spritesheets.

Uses known coordinates from the auto-slicer analysis to extract
elements with meaningful filenames for game integration.

Also extracts the full bottom-right panel from each bag-type kit.

Usage:
    python scripts/extract_named_sprites.py
"""

import json
from pathlib import Path
from PIL import Image


def extract_region(img, x, y, w, h):
    """Extract a region from an image."""
    return img.crop((x, y, x + w, y + h))


def find_panel_region(img, min_size=60):
    """Find the large panel in the far bottom-right corner of a spritesheet.

    The 3x3 panel grid is consistently in the last ~96px of each kit.
    We scan only x>155, y>155 to avoid stray buttons/icons.
    """
    width, height = img.size
    # Look only in the far bottom-right corner (where the panel grid lives)
    start_x = int(width * 0.6)  # ~154 for 256px
    start_y = int(height * 0.6)

    pixels = img.load()
    min_px, min_py = width, height
    max_px, max_py = 0, 0

    for y in range(start_y, height):
        for x in range(start_x, width):
            a = pixels[x, y][3] if len(pixels[x, y]) == 4 else 255
            if a > 10:
                min_px = min(min_px, x)
                min_py = min(min_py, y)
                max_px = max(max_px, x)
                max_py = max(max_py, y)

    if max_px <= min_px or max_py <= min_py:
        return None, None

    w = max_px - min_px + 1
    h = max_py - min_py + 1

    if w < min_size or h < min_size:
        return None, None

    return (min_px, min_py, w, h), img.crop((min_px, min_py, max_px + 1, max_py + 1))


def main():
    project_root = Path(__file__).parent.parent
    panels_dir = project_root / "art" / "sprites" / "ui" / "panels"
    output_dir = project_root / "art" / "sprites" / "ui" / "sliced"
    output_dir.mkdir(parents=True, exist_ok=True)

    catalog = {}

    # ── Kit 01: Primary UI chrome ──────────────────────────────────
    kit01 = Image.open(panels_dir / "ui_kit_01.png").convert("RGBA")
    print("=== Kit 01: Primary UI Chrome ===")

    kit01_extracts = {
        # Header bar (celtic knotwork)
        "header_bar": (9, 11, 142, 26),
        # Scrollbar track (tall vertical)
        "scrollbar_track": (163, 57, 18, 93),
        # Medium square buttons (two variants — normal and highlighted)
        "icon_button_md_normal": (113, 106, 30, 28),
        "icon_button_md_highlight": (113, 74, 30, 28),
        # Small square buttons — row 1 (16x16, 4 variants)
        "icon_button_sm_01": (12, 56, 16, 16),
        "icon_button_sm_02": (36, 56, 16, 16),
        "icon_button_sm_03": (60, 56, 16, 16),
        "icon_button_sm_04": (84, 56, 16, 16),
        # Small square buttons — row 2 (may be pressed/hover states)
        "icon_button_sm_05": (12, 80, 16, 16),
        "icon_button_sm_06": (36, 80, 16, 16),
        "icon_button_sm_07": (60, 80, 16, 16),
        "icon_button_sm_08": (84, 80, 16, 16),
        # Round buttons (18x18, first 4)
        "round_button_01": (11, 111, 18, 18),
        "round_button_02": (35, 111, 18, 18),
        "round_button_03": (59, 111, 18, 18),
        "round_button_04": (83, 111, 18, 18),
        # Pill buttons — top row (likely normal/hover/pressed)
        "button_normal": (16, 168, 32, 16),
        "button_hover": (64, 168, 32, 16),
        "button_pressed": (112, 168, 32, 16),
        # Pill buttons — bottom row (alternative set)
        "button_alt_normal": (16, 192, 32, 16),
        "button_alt_hover": (64, 192, 32, 16),
        "button_alt_pressed": (112, 192, 32, 16),
        # Small top-right buttons (tabs or toggles)
        "tab_button_01": (165, 15, 23, 18),
        "tab_button_02": (192, 15, 24, 18),
        "tab_button_03": (220, 15, 23, 18),
        # Decorative small elements
        "ornament_01": (53, 225, 21, 22),
        "ornament_02": (85, 225, 21, 22),
    }

    # Extract the full panel as one image (bottom-right 3x3 grid)
    # The 9 pieces span from ~(164,164) to ~(244,244)
    panel_region, panel_img = find_panel_region(kit01)
    if panel_img:
        panel_img.save(output_dir / "panel_kit01.png")
        catalog["panel_kit01"] = {
            "file": "panel_kit01.png",
            "source": "ui_kit_01.png",
            "region": {"x": panel_region[0], "y": panel_region[1],
                       "w": panel_region[2], "h": panel_region[3]},
            "role": "9-patch panel frame (primary UI style)"
        }
        print(f"  panel_kit01: {panel_region[2]}x{panel_region[3]} at ({panel_region[0]},{panel_region[1]})")

    for name, (x, y, w, h) in kit01_extracts.items():
        sprite = extract_region(kit01, x, y, w, h)
        filename = f"{name}.png"
        sprite.save(output_dir / filename)
        catalog[name] = {
            "file": filename,
            "source": "ui_kit_01.png",
            "region": {"x": x, "y": y, "w": w, "h": h},
            "role": name.replace("_", " "),
        }
        print(f"  {name}: {w}x{h}")

    # ── Bag-type panels from other kits ────────────────────────────
    bag_kits = {
        2: "bag_default",
        3: "bag_gravity",
        6: "bag_ornate",
        7: "bag_warm",
        9: "bag_brass",
    }

    print("\n=== Bag-Type Panels ===")
    for kit_num, bag_name in bag_kits.items():
        kit_path = panels_dir / f"ui_kit_{kit_num:02d}.png"
        if not kit_path.exists():
            print(f"  Kit {kit_num:02d}: NOT FOUND")
            continue

        kit_img = Image.open(kit_path).convert("RGBA")
        region, panel = find_panel_region(kit_img, min_size=50)

        if panel:
            filename = f"{bag_name}.png"
            panel.save(output_dir / filename)
            catalog[bag_name] = {
                "file": filename,
                "source": f"ui_kit_{kit_num:02d}.png",
                "region": {"x": region[0], "y": region[1],
                           "w": region[2], "h": region[3]},
                "role": f"Bag visual theme ({bag_name.replace('bag_', '')})"
            }
            print(f"  {bag_name}: {region[2]}x{region[3]} from kit {kit_num:02d}")
        else:
            print(f"  Kit {kit_num:02d}: No large panel found in bottom-right")

    # ── Write catalog ──────────────────────────────────────────────
    catalog_path = output_dir / "catalog.json"
    with open(catalog_path, "w") as f:
        json.dump(catalog, f, indent=2)

    print(f"\nCatalog: {catalog_path}")
    print(f"Total named sprites: {len(catalog)}")


if __name__ == "__main__":
    main()
