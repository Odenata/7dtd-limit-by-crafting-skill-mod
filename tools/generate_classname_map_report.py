#!/usr/bin/env python3
"""
Build candidate ClassNameToCraftingSkillMap rows from vanilla items.xml and cross-check
against the mod's ClassNameToCraftingSkillMap.xml.

Usage (from repo root or anywhere):
  python tools/generate_classname_map_report.py ^
    --items "C:/Program Files (x86)/Steam/steamapps/common/7 Days To Die/Data/Config/items.xml" ^
    --map ../src/ClassNameToCraftingSkillMap.xml ^
    --out-csv map_report.csv

Optional --progression path adds a second CSV of crafting perk names from progression.xml
for manual alignment with progressionMatchName overrides.

Requires Python 3.8+.
"""
from __future__ import annotations

import argparse
import csv
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def parse_items_crafting_groups(items_path: Path) -> dict[str, str]:
    """item name -> CraftingSkillGroup value (last wins if duplicate)."""
    tree = ET.parse(items_path)
    root = tree.getroot()
    out: dict[str, str] = {}
    for el in root.iter():
        if el.tag.lower() != "item":
            continue
        name = el.get("name")
        if not name:
            continue
        for child in el:
            if child.tag.lower() != "property":
                continue
            if (child.get("name") or "").strip() == "CraftingSkillGroup":
                val = (child.get("value") or "").strip()
                if val:
                    out[name] = val
    return out


def parse_map_classnames(map_path: Path) -> set[str]:
    tree = ET.parse(map_path)
    root = tree.getroot()
    names: set[str] = set()
    for el in root.iter():
        if el.tag != "Item":
            continue
        cn = el.get("className")
        if cn:
            names.add(cn.strip())
    return names


def parse_progression_crafting_names(progression_path: Path) -> list[str]:
    """Collect progression node names that look like crafting perks."""
    tree = ET.parse(progression_path)
    root = tree.getroot()
    names: list[str] = []
    for el in root.iter():
        n = el.get("name")
        if not n:
            continue
        if "crafting" in n.lower():
            names.append(n)
    return sorted(set(names))


def main() -> int:
    ap = argparse.ArgumentParser(description="Map report from items.xml + mod map.")
    ap.add_argument("--items", type=Path, required=True, help="Vanilla items.xml")
    ap.add_argument("--map", type=Path, required=True, help="ClassNameToCraftingSkillMap.xml")
    ap.add_argument("--out-csv", type=Path, default=Path("map_report_items.csv"))
    ap.add_argument("--progression", type=Path, help="Optional progression.xml")
    ap.add_argument("--out-progression-csv", type=Path, default=Path("map_report_progression_names.csv"))
    args = ap.parse_args()

    if not args.items.is_file():
        print(f"Missing items.xml: {args.items}", file=sys.stderr)
        return 1
    if not args.map.is_file():
        print(f"Missing map: {args.map}", file=sys.stderr)
        return 1

    items_map = parse_items_crafting_groups(args.items)
    mapped = parse_map_classnames(args.map)

    rows = []
    for name, group in sorted(items_map.items()):
        rows.append(
            {
                "className": name,
                "craftingSkillGroup_from_items_xml": group,
                "in_mod_map": "yes" if name in mapped else "no",
            }
        )

    args.out_csv.parent.mkdir(parents=True, exist_ok=True)
    with args.out_csv.open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=["className", "craftingSkillGroup_from_items_xml", "in_mod_map"])
        w.writeheader()
        w.writerows(rows)

    not_in_items = sorted(mapped - set(items_map.keys()))
    summary = args.out_csv.with_name(args.out_csv.stem + "_summary.txt")
    with summary.open("w", encoding="utf-8") as f:
        f.write(f"items.xml entries with CraftingSkillGroup: {len(items_map)}\n")
        f.write(f"mod map entries: {len(mapped)}\n")
        f.write(f"mod map classNames not found in items.xml (typo or renamed): {len(not_in_items)}\n")
        for n in not_in_items[:500]:
            f.write(f"  {n}\n")
        if len(not_in_items) > 500:
            f.write(f"  ... and {len(not_in_items) - 500} more\n")

    print(f"Wrote {args.out_csv} ({len(rows)} rows)")
    print(f"Wrote {summary}")

    if args.progression and args.progression.is_file():
        prog = parse_progression_crafting_names(args.progression)
        with args.out_progression_csv.open("w", newline="", encoding="utf-8") as f:
            w = csv.writer(f)
            w.writerow(["progression_name_crafting_related"])
            for n in prog:
                w.writerow([n])
        print(f"Wrote {args.out_progression_csv} ({len(prog)} names)")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
