#!/usr/bin/env python3
"""
Build ClassNameToCraftingSkillMap reports from items.xml, progression.xml, and the hand map.

Run with Bazel:
  bazel run //tools:generate_classname_map_report -- --items ... --map ... --progression ... \\
    --out-generated-xml ... --out-progression-mismatch-csv ...
"""
from __future__ import annotations

import argparse
import csv
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

# Game-reported craftingSkillGroup -> ordered progression.xml crafting_skill @name keys (mod search order).
CRAFTING_GROUP_TO_PROGRESSION_KEYS: dict[str, list[str]] = {
    "Blades": ["craftingBlades"],
    "Bows": ["craftingBows"],
    "Clubs": ["craftingClubs"],
    "Clothing": ["craftingArmor"],
    "Electrician": ["craftingElectrician", "craftingWorkstations"],
    "Explosives": ["craftingExplosives"],
    "Food": ["craftingFood"],
    "Handguns": ["craftingHandguns"],
    "HarvestingTools": ["craftingHarvestingTools"],
    "Knuckles": ["craftingKnuckles"],
    "MachineGuns": ["craftingMachineGuns"],
    "Medical": ["craftingMedical"],
    "RepairTools": ["craftingRepairTools"],
    "Rifles": ["craftingRifles"],
    "Robotics": ["craftingRobotics"],
    "SalvageTools": ["craftingSalvageTools"],
    "Seeds": ["craftingSeeds"],
    "Shotguns": ["craftingShotguns"],
    "Sledgehammers": ["craftingSledgehammers"],
    "Spears": ["craftingSpears"],
    "Tools": ["craftingHarvestingTools"],
    "Traps": ["craftingTraps"],
    "Vehicles": ["craftingVehicles"],
    "Workstations": ["craftingWorkstations"],
    "Ammo": [
        "craftingHandguns",
        "craftingShotguns",
        "craftingRifles",
        "craftingMachineGuns",
        "craftingBows",
        "craftingExplosives",
    ],
    "Weapons": [
        "craftingHandguns",
        "craftingShotguns",
        "craftingRifles",
        "craftingMachineGuns",
        "craftingBows",
        "craftingExplosives",
    ],
}

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


def parse_map_classname_to_group(map_path: Path) -> dict[str, str]:
    tree = ET.parse(map_path)
    root = tree.getroot()
    out: dict[str, str] = {}
    for el in root.iter():
        if el.tag != "Item":
            continue
        cn = (el.get("className") or "").strip()
        cg = (el.get("craftingSkillGroup") or "").strip()
        if cn and cg:
            out[cn] = cg
    return out


def parse_map_progression_overrides(map_path: Path) -> dict[str, str]:
    """className -> progressionMatchName from hand map."""
    tree = ET.parse(map_path)
    out: dict[str, str] = {}
    for el in tree.getroot().iter():
        if el.tag != "Item":
            continue
        cn = (el.get("className") or "").strip()
        pm = (el.get("progressionMatchName") or "").strip()
        if cn and pm:
            out[cn] = pm
    return out


def _add_unlock_tokens(acc: set[str], raw: str | None) -> None:
    if not raw:
        return
    for part in raw.split(","):
        s = part.strip()
        if s:
            acc.add(s)


def parse_progression_unlock_index(progression_path: Path) -> dict[str, set[str]]:
    """
    crafting_skill @name -> set of item strings from display_entry/@item and unlock_entry/@item.
    """
    tree = ET.parse(progression_path)
    root = tree.getroot()
    index: dict[str, set[str]] = {}
    for el in root.iter():
        if el.tag.lower() != "crafting_skill":
            continue
        name = (el.get("name") or "").strip()
        if not name:
            continue
        acc = index.setdefault(name, set())
        for sub in el.iter():
            st = sub.tag.lower()
            if st == "display_entry":
                _add_unlock_tokens(acc, sub.get("item"))
            elif st == "unlock_entry":
                _add_unlock_tokens(acc, sub.get("item"))
    return index


def parse_progression_crafting_names(progression_path: Path) -> list[str]:
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


def union_unlock_strings_for_group(
    crafting_skill_group: str, index: dict[str, set[str]]
) -> tuple[set[str], list[str]]:
    """(lowercase item names, progression keys searched)."""
    keys = CRAFTING_GROUP_TO_PROGRESSION_KEYS.get(crafting_skill_group, [])
    if not keys and crafting_skill_group:
        # Fallback: crafting + normalized
        guess = "crafting" + crafting_skill_group.replace(" ", "")
        if guess in index:
            keys = [guess]
    seen_lower: set[str] = set()
    for key in keys:
        for s in index.get(key, ()):
            seen_lower.add(s.lower())
    return seen_lower, keys


def _canonical_from_lower(index: dict[str, set[str]], keys: list[str], lower: str) -> str:
    for key in keys:
        for s in index.get(key, ()):
            if s.lower() == lower:
                return s
    for _k, items in index.items():
        for s in items:
            if s.lower() == lower:
                return s
    return lower


def suggest_progression_match_name(
    class_name: str,
    crafting_skill_group: str,
    index: dict[str, set[str]],
) -> tuple[str | None, str, list[str]]:
    """
    Returns (progressionMatchName or None, status, candidate_list_for_csv).
    status: in_progression | transform_xml | heuristic_garage01 | ambiguous | not_mapped_group
    """
    if not class_name or not crafting_skill_group:
        return None, "not_mapped_group", []

    keys = CRAFTING_GROUP_TO_PROGRESSION_KEYS.get(crafting_skill_group)
    if not keys:
        return None, "not_mapped_group", []

    union_lower, _ = union_unlock_strings_for_group(crafting_skill_group, index)
    cn_low = class_name.lower()

    if cn_low in union_lower:
        return None, "in_progression", []

    candidates: list[str] = []

    def try_transform(t: str) -> str | None:
        tl = t.lower()
        if tl in union_lower:
            return _canonical_from_lower(index, keys, tl)
        return None

    # Parity with GameReflection.BuildProgressionMatchCandidates
    if class_name.startswith("ironGarageDoor_") and not class_name.startswith("ironGarageDoor01_"):
        rest = class_name[len("ironGarageDoor_") :]
        t = "ironGarageDoor01_" + rest
        hit = try_transform(t)
        if hit:
            return hit, "transform_xml", []
        candidates.append(t)

    if class_name.startswith("woodenGarageDoor3x3_") and "woodenGarageDoor01_3x3_" not in class_name:
        rest = class_name[len("woodenGarageDoor3x3_") :]
        t = "woodenGarageDoor01_3x3_" + rest
        hit = try_transform(t)
        if hit:
            return hit, "transform_xml", []
        candidates.append(t)

    # Powered iron garage placeables: progression lists helpers, not per-color items; suggest 01 name (in-game parity).
    if re.match(r"^ironGarageDoor_Powered[A-Za-z]", class_name) and "BlockVariantHelper" not in class_name:
        t = "ironGarageDoor01_" + class_name[len("ironGarageDoor_") :]
        hit = try_transform(t)
        if hit:
            return hit, "transform_xml", []
        return t, "heuristic_garage01", [t]

    # Suffix + garagedoor + powered: unique unlock in searched trees
    if "garagedoor" in cn_low and "powered" in cn_low:
        li = class_name.rfind("_")
        if li > 0 and li < len(class_name) - 1:
            tail = class_name[li + 1 :].lower()
            tail_hits: list[str] = []
            for key in keys:
                for u in index.get(key, ()):
                    ul = u.lower()
                    if "garagedoor" in ul and "powered" in ul and ul.endswith("_" + tail):
                        tail_hits.append(u)
            uniq = list(dict.fromkeys(tail_hits))
            if len(uniq) == 1:
                u0 = uniq[0]
                if u0.lower() != cn_low:
                    return u0, "suffix_unique_xml", []

    if candidates:
        return None, "ambiguous", candidates
    return None, "no_match_in_xml", []


def compute_progression_matches_for_map(
    class_to_group: dict[str, str],
    index: dict[str, set[str]],
) -> tuple[dict[str, str], list[dict[str, str]]]:
    """suggested progressionMatchName; mismatch CSV rows (non-exact unlock names)."""
    suggested: dict[str, str] = {}
    mismatches: list[dict[str, str]] = []
    for cn, group in sorted(class_to_group.items()):
        union_lower, _keys = union_unlock_strings_for_group(group, index)
        if cn.lower() in union_lower:
            continue
        pm, status, cands = suggest_progression_match_name(cn, group, index)
        if pm and status in ("transform_xml", "suffix_unique_xml", "heuristic_garage01"):
            suggested[cn] = pm
        row = {
            "className": cn,
            "craftingSkillGroup": group,
            "status": status,
            "suggested_progressionMatchName": pm or "",
            "candidates": "|".join(cands),
        }
        mismatches.append(row)
    return suggested, mismatches


def classname_sort_key(class_name: str) -> tuple:
    s = class_name
    s = re.sub(r"([a-z])([A-Z])", r"\1 \2", s)
    s = re.sub(r"([A-Za-z])(\d)", r"\1 \2", s)
    s = re.sub(r"(\d)([A-Za-z])", r"\1 \2", s)
    tokens = s.split()
    return (tuple(t.lower() for t in tokens), class_name)


def xml_escape_attr(value: str) -> str:
    return (
        value.replace("&", "&amp;")
        .replace('"', "&quot;")
        .replace("<", "&lt;")
        .replace(">", "&gt;")
    )


def write_generated_map_xml(
    class_to_group: dict[str, str],
    out_path: Path,
    *,
    source_label: str = "items.xml",
    progression_match_by_class: dict[str, str] | None = None,
    progression_basename: str | None = None,
) -> None:
    by_group: dict[str, list[str]] = {}
    for class_name, group in class_to_group.items():
        by_group.setdefault(group, []).append(class_name)

    pm = progression_match_by_class or {}
    items_basename = Path(source_label).name
    lines: list[str] = [
        '<?xml version="1.0" encoding="utf-8"?>',
        "<!-- Generated by tools/generate_classname_map_report.py. "
        "Merge into ClassNameToCraftingSkillMap.xml; verify progressionMatchName in-game. "
        "requiredLevelOverride is never auto-generated. -->",
        f"<!-- items.xml input: {xml_escape_attr(items_basename)} -->",
    ]
    if progression_basename:
        lines.append(f"<!-- progression.xml: {xml_escape_attr(progression_basename)} -->")
    lines.append("<ClassNameToCraftingSkillMap>")

    for group in sorted(by_group.keys(), key=str.lower):
        names = by_group[group]
        names.sort(key=classname_sort_key)
        lines.append(f"  <!-- {xml_escape_attr(group)} -->")
        for cn in names:
            g = xml_escape_attr(group)
            c = xml_escape_attr(cn)
            pms = pm.get(cn)
            if pms:
                p = xml_escape_attr(pms)
                lines.append(
                    f'  <Item className="{c}" craftingSkillGroup="{g}" progressionMatchName="{p}"/>'
                )
            else:
                lines.append(f'  <Item className="{c}" craftingSkillGroup="{g}"/>')
        lines.append("")

    while lines and lines[-1] == "":
        lines.pop()
    lines.append("</ClassNameToCraftingSkillMap>")

    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    ap = argparse.ArgumentParser(description="Map report from items.xml + mod map + progression.xml")
    ap.add_argument("--items", type=Path, required=True, help="Vanilla items.xml")
    ap.add_argument("--map", type=Path, required=True, help="ClassNameToCraftingSkillMap.xml")
    ap.add_argument("--out-csv", type=Path, default=Path("map_report_items.csv"))
    ap.add_argument("--out-generated-xml", type=Path, default=None)
    ap.add_argument(
        "--generated-xml-from-items-only",
        action="store_true",
        help="Generated XML: only items.xml CraftingSkillGroup rows",
    )
    ap.add_argument("--progression", type=Path, help="progression.xml (unlock index + optional CSV)")
    ap.add_argument("--out-progression-csv", type=Path, default=Path("map_report_progression_names.csv"))
    ap.add_argument(
        "--out-progression-mismatch-csv",
        type=Path,
        default=None,
        help="Rows where className is not an exact progression unlock (review)",
    )
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

    progression_basename = None
    hand_pm = parse_map_progression_overrides(args.map)
    pm_by_class: dict[str, str] = dict(hand_pm)

    if args.progression and args.progression.is_file():
        progression_basename = args.progression.name
        idx = parse_progression_unlock_index(args.progression)
        if args.generated_xml_from_items_only:
            gen_map_for_prog = dict(items_map)
        else:
            gen_map_for_prog = {**parse_map_classname_to_group(args.map), **items_map}
        suggested, mismatches = compute_progression_matches_for_map(gen_map_for_prog, idx)
        for cn, pm in suggested.items():
            if cn not in pm_by_class:
                pm_by_class[cn] = pm

        if args.out_progression_mismatch_csv:
            args.out_progression_mismatch_csv.parent.mkdir(parents=True, exist_ok=True)
            with args.out_progression_mismatch_csv.open("w", newline="", encoding="utf-8") as f:
                w = csv.DictWriter(
                    f,
                    fieldnames=[
                        "className",
                        "craftingSkillGroup",
                        "status",
                        "suggested_progressionMatchName",
                        "candidates",
                    ],
                )
                w.writeheader()
                w.writerows(mismatches)
            print(f"Wrote {args.out_progression_mismatch_csv} ({len(mismatches)} rows)")

        prog = parse_progression_crafting_names(args.progression)
        with args.out_progression_csv.open("w", newline="", encoding="utf-8") as f:
            w = csv.writer(f)
            w.writerow(["progression_name_crafting_related"])
            for n in prog:
                w.writerow([n])
        print(f"Wrote {args.out_progression_csv} ({len(prog)} names)")
    elif args.out_progression_mismatch_csv:
        print("Note: --out-progression-mismatch-csv ignored without --progression", file=sys.stderr)

    if args.out_generated_xml is not None:
        if args.generated_xml_from_items_only:
            gen_map = dict(items_map)
        else:
            gen_map = {**parse_map_classname_to_group(args.map), **items_map}
        pm_for_xml = {k: v for k, v in pm_by_class.items() if k in gen_map}
        write_generated_map_xml(
            gen_map,
            args.out_generated_xml,
            source_label=str(args.items.resolve()),
            progression_match_by_class=pm_for_xml if pm_for_xml else None,
            progression_basename=progression_basename,
        )
        n_pm = len(pm_for_xml)
        print(f"Wrote {args.out_generated_xml} ({len(gen_map)} entries, {n_pm} with progressionMatchName)")
        if not items_map and not args.generated_xml_from_items_only:
            print(
                "Note: items.xml had no CraftingSkillGroup properties; "
                "generated XML is mostly the mod map re-ordered.",
                file=sys.stderr,
            )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
