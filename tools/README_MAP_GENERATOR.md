# ClassName map generator / report

Run with **Bazel** (hermetic Python via [rules_python](https://github.com/bazelbuild/rules_python)):

```text
bazel run //tools:generate_classname_map_report -- --items <path/to/items.xml> --map src/ClassNameToCraftingSkillMap.xml ...
```

Use `--` before script flags. On Windows, use absolute paths for `--items` / `--map` if the binary’s working directory is not the repo root.

## What it does

1. **CSV** (`--out-csv`) — one row per item that has **`CraftingSkillGroup`** in `items.xml`, plus whether that `className` is in the mod map. *Vanilla 7DTD `items.xml` usually has no `CraftingSkillGroup` rows*, so this CSV may be empty.
2. **Summary** — mod map `className` values not present as item names in `items.xml` (possible renames).
3. **`ClassNameToCraftingSkillMap_generated.xml`** (`--out-generated-xml`) — **does not overwrite** the hand-edited map. Emits the mod map (plus `items.xml` overrides unless `--generated-xml-from-items-only`), sorted by **group** then **name similarity**. **`progressionMatchName`** is filled from:
   - the hand map when present, else
   - **`progression.xml` analysis** when `--progression` is passed (see below).
   **`requiredLevelOverride` is never auto-generated.**

### `progression.xml` (unlock index)

Pass **`--progression`** to the game’s **`Data/Config/progression.xml`**. The tool indexes every **`crafting_skill`** tree’s **`display_entry/@item`** and **`unlock_entry/@item`** (comma-separated lists).

- If **`className`** appears exactly in the relevant skill tree(s), no extra attribute.
- Otherwise it suggests **`progressionMatchName`** when:
  - a **transform** matches an unlock string (e.g. `ironGarageDoor_*` → `ironGarageDoor01_*`, `woodenGarageDoor3x3_*` → `woodenGarageDoor01_3x3_*`), matching [GameReflection.cs](../src/GameReflection.cs) parity, or
  - **powered iron garage** placeables: `ironGarageDoor_PoweredColor` → `ironGarageDoor01_PoweredColor` (often **not** listed per-color in XML; verify in-game).

**`--out-progression-mismatch-csv`** — one row per map entry whose `className` is **not** an exact unlock in the indexed trees, with `status` and any suggested name (review before trusting).

**`--out-progression-csv`** — still lists progression node names containing `crafting` (legacy helper).

## Examples

**PowerShell** (full refresh including progression hints):

```powershell
$game = "${env:ProgramFiles(x86)}\Steam\steamapps\common\7 Days To Die\Data\Config"
$env:BAZEL_SH = "C:\Program Files\Git\bin\bash.exe"
bazel run //tools:generate_classname_map_report -- `
  --items "$game\items.xml" `
  --map (Resolve-Path "src\ClassNameToCraftingSkillMap.xml") `
  --progression "$game\progression.xml" `
  --out-csv "$env:TEMP\map_report_items.csv" `
  --out-progression-mismatch-csv "$env:TEMP\map_progression_mismatch.csv" `
  --out-generated-xml (Resolve-Path "src\ClassNameToCraftingSkillMap_generated.xml")
```

## Tests

- **Hermetic:** `bazel test //tests/map_generator:generate_classname_map_report_test`
- **Optional (env-gated):** `bazel test //tests:progression_assembly_integration`
  - Set **`PROGRESSION_XML_PATH`** or **`7DTD_PROGRESSION_XML`** to game `progression.xml` and optionally **`CLASSNAME_MAP_XML`** to `src/ClassNameToCraftingSkillMap.xml` to assert every mapped item resolves against unlock lists (plus the same garage transforms as above).
  - Set **`ASSEMBLY_CSHARP_DLL`** to **`…/7 Days To Die/7DaysToDie_Data/Managed/Assembly-CSharp.dll`** for a **reflection smoke** check that `ProgressionClass` / `DisplayDataList` still exist after game updates.

Decompiled loaders in **7dtd-mod-dev-tools** (`document-game-api/decompiled-local`) document the XML shape (`display_entry` / `unlock_entry`); authoritative data is always **`progression.xml`** from the install.

After updates, merge into [`src/ClassNameToCraftingSkillMap.xml`](../src/ClassNameToCraftingSkillMap.xml) by hand.
