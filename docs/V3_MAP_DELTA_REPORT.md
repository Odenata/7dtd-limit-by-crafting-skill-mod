# V3.0 ClassName Map Delta Report

**Task:** V3-TASK-004  
**Game build:** V 3.0.0 (b252) experimental  
**Baseline map:** `src/ClassNameToCraftingSkillMap.xml` (319 rows, unchanged by this task)  
**Date:** 2026-06-15

## Executive summary

**No bulk map rewrite is required for v3.0.** The hand-maintained map remains largely valid: **296 / 319** entries resolve to an existing `items.xml` id and/or `blocks.xml` block id. Mod-attachment quality tiers (the highest-risk v3.0 surface) are **correctly unmapped** — **0** of **107** `item_modifier` ids appear in the map.

A **targeted maintenance pass** (~30 rows) is recommended: fix **4 id renames/removals**, prune **~19 removed placeable color variants**, optionally **add 3–6** new progression-backed craftables, and document **107 mod ids** for generator denylist hardening.

---

## Methodology

| Source | Path / command |
|--------|----------------|
| Required docs | `7dtd-mod-dev-tools/agent-notes/topics/game-v3-api-drift-2026-06-15.md`, `GAME_V3_PREP.md`; mod `CLASS_NAME_MAP_HOWTO.md`, `CRAFTING_SKILL_MAPPINGS.md`, `GATING_AND_RESTRICTIONS.md` |
| Game config | `Data/Config/items.xml`, `blocks.xml`, `item_modifiers.xml`, `progression.xml`, `recipes.xml` |
| Map generator | `python tools/generate_classname_map_report.py --items … --progression … --map src/ClassNameToCraftingSkillMap.xml` (Bazel target `//tools:generate_classname_map_report` failed with invalid package `/tools` in this shell; Python direct run succeeded) |
| Supplemental scripts | Inline Python cross-checks (items + blocks + modifiers + progression unlock index) |

### Critical v3.0 config note

**`items.xml` contains zero `CraftingSkillGroup` properties** in b252. The map generator’s “not in items.xml” summary is **misleading** if `blocks.xml` is not consulted — **146** handheld rows are fine; **123** map keys are block-only placeables (expected). Always cross-check **`blocks.xml`** and **`item_modifiers.xml`** for v3.0 deltas.

---

## Summary counts

| Category | Count | Notes |
|----------|------:|-------|
| Map entries (baseline) | 319 | Unchanged |
| Entries valid in v3 (item and/or block id exists) | 296 | 173 handheld + 123 block placeables |
| **Stale ids** (absent from items + blocks) | **23** | See rename/remove table |
| **New gear ids** (progression unlock, craftable, not in map) | **6** | Optional adds |
| **Mod attachment ids** (`item_modifiers.xml`) | **107** | **27** with Q1–Q6 quality tiers |
| Mod attachments **in map** (policy violation) | **0** | Policy preserved |
| Mod attachments with `CraftingSkillGroup` in XML | **0** | No accidental fallback gating vector |
| **CraftingSkillGroup changes** on mapped gear | **0** | Property removed from items.xml entirely |
| **Id renames** affecting mapped rows | **4** | See table |
| **Removed placeable variants** in map | **19** | Pink/Purple/Black garage doors, vault `_player`, industrialLight07 |
| **combineStation** (new v3 workstation) | 1 | Block exists; **not** in progression unlocks |
| Progression mismatch rows (generator) | 148 | Mostly expected: exact unlock uses icons/aliases; garage transforms already in map |

---

## Actionable delta table

Columns: `item_id | category | current_map_status | v3_observation | recommended_action | rationale`

### A — Renames / stale map keys (fix required)

| item_id | category | current_map_status | v3_observation | recommended_action | rationale |
|---------|----------|-------------------|----------------|-------------------|-----------|
| foodShephardsPie | Food | mapped (typo) | Renamed to `foodShepardsPie` in items.xml + craftingFood unlock | **rename** → `foodShepardsPie` | Progression row uses corrected spelling; stale key never matches in-game stacks |
| meleeToolAxeT2SteelFireaxe | HarvestingTools | mapped + progressionMatchName | Id **removed**; progression unlock is `meleeToolAxeT2SteelAxe` | **remove** duplicate row | `meleeToolAxeT2SteelAxe` already mapped; Fireaxe id is dead |
| meleeToolAxeT2SteelAxe | HarvestingTools | mapped; progressionMatchName=`meleeToolAxeT2SteelFireaxe` | Progression lists `meleeToolAxeT2SteelAxe` directly | **override** — drop `progressionMatchName` | v3 progression exact-match; override pointed at removed id |
| meleeToolAxeT0StoneAxe | HarvestingTools | mapped; progressionMatchName iron anchor | Id **removed**; stone tier is `meleeToolRepairT0StoneAxe` only | **remove** or retarget key to repair stone axe | v3 consolidated stone axe into repair tool id |
| meleeToolPickT0StonePickaxe | HarvestingTools | mapped | Id **removed**; no T0 pick in items or progression | **remove** | No v3 item to gate |

### B — Removed placeable / block variants (prune stale rows)

| item_id | category | current_map_status | v3_observation | recommended_action | rationale |
|---------|----------|-------------------|----------------|-------------------|-----------|
| industrialLight07_player | Electrician | mapped + override 25 | **Absent** from items/blocks (only `industrialLight01_player`, `industrialLight02_player`) | **remove** | Dead map key |
| vaultDoor01_player | Electrician | mapped | Replaced by `vaultDoor01` block (no `_player` suffix) | **rename** → `vaultDoor01` or **remove** if unpowered vault not gated | Id no longer exists |
| vaultDoor02_player | Electrician | mapped | Same pattern as vaultDoor01 | **rename** → `vaultDoor02` or **remove** | Id no longer exists |
| vaultHatch01_player | Electrician | mapped | Check `vaultHatch01` block family | **rename** or **remove** after live id check | `_player` suffix gone in blocks.xml |
| steelGarageDoor3x3_PoweredPink | Electrician | mapped + override 55 | **Absent** from blocks.xml | **remove** | Color variant culled in v3 |
| steelGarageDoor3x3_PoweredPurple | Electrician | mapped + override 55 | **Absent** | **remove** | Color variant culled |
| steelGarageDoor4x3_PoweredPink | Electrician | mapped + override 55 | **Absent** | **remove** | Color variant culled |
| steelGarageDoor4x3_PoweredPurple | Electrician | mapped + override 55 | **Absent** | **remove** | Color variant culled |
| steelGarageDoor5x3_PoweredPink | Electrician | mapped + override 55 | **Absent** | **remove** | Color variant culled |
| steelGarageDoor5x3_PoweredPurple | Electrician | mapped + override 55 | **Absent** | **remove** | Color variant culled |
| woodenGarageDoor3x3_PoweredBlack | Electrician | mapped + progressionMatchName | **Absent** | **remove** | Color variant culled |
| woodenGarageDoor3x3_PoweredPink | Electrician | mapped + progressionMatchName | **Absent** | **remove** | Color variant culled |
| woodenGarageDoor3x3_PoweredPurple | Electrician | mapped + progressionMatchName | **Absent** | **remove** | Color variant culled |
| woodenGarageDoor4x3_PoweredBlack | Electrician | mapped + override 55 | **Absent** | **remove** | Color variant culled |
| woodenGarageDoor4x3_PoweredPink | Electrician | mapped + override 55 | **Absent** | **remove** | Color variant culled |
| woodenGarageDoor4x3_PoweredPurple | Electrician | mapped + override 55 | **Absent** | **remove** | Color variant culled |
| woodenGarageDoor5x3_PoweredBlack | Electrician | mapped + override 55 | **Absent** | **remove** | Color variant culled |
| woodenGarageDoor5x3_PoweredPink | Electrician | mapped + override 55 | **Absent** | **remove** | Color variant culled |
| woodenGarageDoor5x3_PoweredPurple | Electrician | mapped + override 55 | **Absent** | **remove** | Color variant culled |

### C — Optional new progression-backed craftables (add if gating desired)

| item_id | category | current_map_status | v3_observation | recommended_action | rationale |
|---------|----------|-------------------|----------------|-------------------|-----------|
| foodShepardsPie | Food | unmapped (typo row stale) | craftingFood unlock tier 70 composite row | **add** `Food` | Replaces renamed typo entry |
| thrownAmmoPipeBomb | Explosives | unmapped | New T1 explosives unlock (level 2) | **add** `Explosives` + `requiredLevelMin=2` | Matches progression band; pipe bomb craftable |
| ammoRocketFrag | Explosives | unmapped | craftingExplosives unlock (rocket launcher tier) | **add** `Explosives` | Rocket ammo gated by explosives skill |
| ammoRocketHE | Explosives | unmapped | Same tree as Frag | **add** `Explosives` | Same |
| ammoGasCan | Vehicles | unmapped | craftingVehicles unlock with minibike/motorcycle parts | **exclude** | Consumable fuel; gating hotbar gas cans is harsh — prefer unmapped unless live test says otherwise |
| ammoGasCanBundle | Vehicles | unmapped | craftingVehicles unlock tier 45 | **exclude** | Bundle craft ingredient; policy-aligned with vehicle **parts** excluded |
| combineStation | Workstations | unmapped | New block in `blocks.xml`; recipe has **no** craftingWorkstations progression row | **no-change** (or add with explicit override after live test) | Opens from day 1; no vanilla skill gate — only map if mod policy wants workstation open restriction |

### D — Mod attachments / install items (must stay excluded per §3.9)

Representative rows; **full set = 107** `item_modifier` names. **None** are in the current map. **27** have `ShowQuality=true` and Q1–Q6 `tier="1,2,3,4,5,6"` passive effects.

| item_id | category | current_map_status | v3_observation | recommended_action | rationale |
|---------|----------|-------------------|----------------|-------------------|-----------|
| modMeleeGraveDigger | mod_attachment (melee) | unmapped | Q1–Q6 dirt damage 5%→30%; `item_modifiers.xml` | **exclude** | Policy §3.9; quality tier must not gate installs |
| modMeleeWoodSplitter | mod_attachment (melee) | unmapped | Q1–Q6 wood damage tiers | **exclude** | Same |
| modGunMagazineExtender | mod_attachment (gun) | unmapped | Q1–Q6 magazine bonus tiers | **exclude** | Same |
| modArmorInsulatedLinerT1 | mod_attachment (armor) | unmapped | Q1–Q6 insulation tiers | **exclude** | Same |
| modFuelTankSmall / modFuelTankLarge | mod_attachment (vehicle) | unmapped | Q1–Q6 fuel capacity tiers | **exclude** | Vehicle mod install policy |
| toolAnvil / toolBellows / toolForgeCrucible | workstation_upgrade | unmapped | In progression craftingWorkstations unlocks | **exclude** | Workstation upgrade installs |
| carBattery | battery | unmapped | Vehicle progression ingredient | **exclude** | Battery slot policy |
| vehicle*Bicycle*Chassis / *Handlebars / *Accessories | vehicle_part | unmapped | craftingVehicles unlock children | **exclude** | Parts are not placeable vehicle items |
| *Schematic (e.g. modGunScopeSmallSchematic) | schematic | unmapped | Readable/schematic items | **no-change** | Auto-read domain; not hotbar gear gates |

**Generator hardening:** If `generate_classname_map_report` is extended for v3, parse **`item_modifiers.xml`** and flag any suggested add as **`exclude`** automatically.

### E — Existing rows: progression / override review (no band changes detected)

Spot-checked v3 `unlock_level` CSVs for mapped weapon/tool/armor/workstation rows — **bands unchanged** vs documented map comments (e.g. craftingHarvestingTools stone `1,2,4,6,8,10`, explosives T1 `2,5,10`, workstations workbench tier 10). Existing `requiredLevelOverride` / `requiredLevelMin` floors remain appropriate.

| item_id | category | current_map_status | v3_observation | recommended_action | rationale |
|---------|----------|-------------------|----------------|-------------------|-----------|
| meleeToolRepairT0StoneAxe | HarvestingTools | mapped + iron progressionMatchName | Exact unlock in stone display_entry | **no-change** | Stone anchor logic in `GameReflection` still valid |
| meleeToolShovelT0StoneShovel | HarvestingTools | mapped + iron progressionMatchName | Exact unlock in stone row | **no-change** | Same |
| cntDewCollector / cntApiary | Workstations | mapped + overrides 16 / 30 | Progression bands 16 / 30 unchanged | **no-change** | Overrides match progression |
| vehicle*Placeable | Vehicles | mapped | All five placeables still in items.xml + craftingVehicles | **no-change** | Ids stable |
| gunExplosivesT3RocketLauncher | Explosives | mapped (no min) | Rocket launcher still quality-gated 50–100 in comments | **no-change** | Intentionally omitted from strict min floors |
| ironGarageDoor_Powered* / woodenGarageDoor3x3_Powered* (surviving colors) | Electrician | mapped + overrides / progressionMatchName | Powered doors still in blocks.xml; progression uses `ironGarageDoor01_*` helpers | **no-change** | Existing transforms still needed |

### F — Schematics / recipes (informational)

- Mod schematics remain `*Schematic` suffix items in `items.xml` only (no separate craftable mod item row).
- `combineStation` recipe is early-game (scrap iron + wood); no `craftingWorkstations` unlock passive — **not** a progression-gated workstation like chemistry station.

---

## Policy verification: mod quality tiers

v3.0 adds progressive quality to mod attachments via `item_modifiers.xml` (`ShowQuality`, per-tier `passive_effect` values). Because these ids are **omitted** from `ClassNameToCraftingSkillMap.xml`:

- `GetCraftingSkillGroup` returns null → **no restriction** on inventory drag/install paths (§3.9).
- No `CraftingSkillGroup` XML fallback exists on modifiers.

**Risk if map were bulk-regenerated from items.xml alone:** generator would **not** auto-add modifiers today (they are not in items.xml). Future generator versions must explicitly denylist `item_modifiers.xml` names.

---

## What remains uncertain (live confirmation)

1. **Vault door unpowered ids** — whether `vaultDoor01` / `vaultDoor02` placeables should inherit powered-door gates or stay ungated (map had separate `_player` rows without override).
2. **combineStation** — whether workstation-open restriction should apply before a progression row exists (currently no hook in progression.xml).
3. **Magnitude / boosted-stat weapons** — same class names as mapped gear; gating uses `ItemValue.Quality` — live Q6 boosted tools should be smoke-tested (V3-TASK-002).
4. **`ProgressionClass.Enabled` sandbox fields** — new in v3 API; disabled progression rows could yield `requiredLevel=0` for mapped items (audit `GameReflection` with sandbox presets).
5. **Generator Bazel target** — `bazel run //tools:generate_classname_map_report` failed in bash with `invalid package name '/tools'`; environment fix needed for hermetic regen.

---

## Artifacts

| Artifact | Path |
|----------|------|
| **This report** | `docs/V3_MAP_DELTA_REPORT.md` |
| Generator CSV (empty CraftingSkillGroup rows) | `docs/V3_MAP_DELTA_CANDIDATES.csv` |
| Generator stale-key summary (items-only, misleading) | `docs/V3_MAP_DELTA_CANDIDATES_summary.txt` |
| Progression mismatch export | `docs/V3_MAP_PROGRESSION_MISMATCH.csv` |
| Refreshed generated reference map | `src/ClassNameToCraftingSkillMap_generated.xml` |

`ClassNameToCraftingSkillMap.xml` was **not** modified.

---

## Recommended next agent

| Role | Task |
|------|------|
| **Task Agent** | Apply targeted map edits from sections A + B (+ optional C) in a single PR; fix `meleeToolAxeT2SteelAxe` progressionMatchName; run `bazel test //tests/map_generator:...` and progression integration if env allows |
| **Exploratory Agent** | **V3-TASK-002** — live verify Q6 mod attachment installable + Q6 weapon gated on hotbar |
| **Task Agent** | **V3-TASK-001** — Harmony patch repair (`GameManager.workstationOpened` removed) before map changes matter in-game |

---

## Handoff (V3-TASK-004)

1. **Goal:** Assess whether `ClassNameToCraftingSkillMap.xml` needs a bulk v3.0 update → **No bulk update; targeted ~30-row maintenance.**
2. **Inputs:** Docs listed above; v3 `items.xml`, `blocks.xml`, `item_modifiers.xml`, `progression.xml`; `python tools/generate_classname_map_report.py …` (Bazel attempt failed).
3. **Known:** 6 optional new gear ids; 107 mod ids to exclude (0 in map); 0 CraftingSkillGroup XML changes; 4 renames + 19 removed variants; 296/319 entries still valid.
4. **Uncertain:** Vault unpowered naming, combineStation gating policy, magnitude quality live behavior, sandbox `Enabled` fields.
5. **Artifacts:** `docs/V3_MAP_DELTA_REPORT.md` + auxiliary CSVs under `docs/`.
6. **Next:** Task Agent for map PR; Exploratory V3-TASK-002 for live mod-tier verification.
