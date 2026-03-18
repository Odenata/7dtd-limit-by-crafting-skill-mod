# TODO / follow-up work

Tracked improvements and known gaps for **LimitByCraftingSkillMod** and related tooling. Check items off as you complete them.

---

## 1. Shift-click / container moves — addressed

**Fix:** Removed **`HandleMoveToPreferredLocation`** prefix; **`AddItemToToolbelt`** / **`AddItemToPreferredToolbeltSlot`** handle the toolbelt path after vanilla **`AddItemToBackpack`**. See **`docs/QUICK_MOVE_AND_TOOLBELT_HOOKS.md`**.

**Follow-up:** If new false positives appear, audit additional UI entry points.

---

## 2. Extract item names & progression names from game data — addressed (baseline)

**Tool:** **`bazel run //tools:generate_classname_map_report`** with install **`items.xml`** (optional **`progression.xml`**). Hermetic Python via **rules_python**; see **`tools/README_MAP_GENERATOR.md`**. **`src/ClassNameToCraftingSkillMap_generated.xml`** is the sorted/merged view (mod map + optional `items.xml` `CraftingSkillGroup` rows). CI: **`.github/workflows/map-generator.yml`**.

**Dev-tools:** Broader pipeline still TBD — [`TODO_ITEM_PROGRESSION_NAME_PIPELINE.md`](../../7dtd-mod-dev-tools/docs/TODO_ITEM_PROGRESSION_NAME_PIPELINE.md).

---

## Electrician `requiredLevelOverride` / progression (TODO)

**Problem:** Required levels from `craftingelectrician` + `craftingworkstations` `DisplayData` often yield **0** or wrong tiers for placeables (e.g. powered garage doors), so restrictions do not apply reliably.

**Done for reference:** `ClassNameToCraftingSkillMap_generated.xml` sets **`requiredLevelOverride="30"`** on powered garage placeables: **`ironGarageDoor_Powered*`**, **`steelGarageDoor*Powered*`**, **`woodenGarageDoor*Powered*`** (hand-maintained in generated file until the generator or runtime logic is fixed).

**TODO:** Audit the full **Electrician** map set: for each item, record vanilla progression tier (or confirm override/min), remove redundant overrides when progression resolve is trustworthy, and optionally teach **`generate_classname_map_report.py`** to emit overrides only where validated.

---

## 3. Validate map names per game version

**Problem:** `ClassNameToCraftingSkillMap.xml` entries need verification against each build (e.g. `meleeToolAxeT2SteelAxe` vs `…SteelFireaxe`).

**Approach:**

- Maintain a checklist or generated table: **className → works in V x.y (bNN)?** / needs `progressionMatchName`? / needs `requiredLevelOverride`?  
- Test with a **new, unleveled character** so low skills make restrictions obvious (equip blocked, red labels where applicable).

---

## 4. Non-inventory interactions: vehicles & workstations — partial

**Implemented (prototype):** **`WorkstationToolHandleStackSwapPatch`**, **`VehiclePartHandleStackSwapPatch`**, **`ItemActionSpawnVehicleRestrictionPatch`**. See **`docs/GAME_API_NOTES.md`**.

**Still open:** **`VehicleInventory`** / fuel / refuel actions; server-authoritative validation for multiplayer; workstation **input/material** grids beyond tool slots.

---

## 5. Optional: non-quality items & inventory UI clarity

**Context:** Non-stacking items **without** a quality attribute often show **no restricted label** in inventory (vehicles, workstations are examples).

**Options:**

- After **§4** is solid, **drop inventory restriction** for categories that are fully gated elsewhere (if inventory restriction adds no value).  
- Or add a **non-quality visual** (tooltip / icon) where patches already run.

---

## 6. Repository hygiene: `.gitignore`

**Problem:** Build outputs and local logs are easy to commit by mistake (`src/bin/`, `src/obj/`, `tests/bin/`, `tests/obj/`, `debug-*.log`, Bazel outputs, copied game DLLs in output folders).

**TODO:**

- Add root **`.gitignore`** (typical .NET: `bin/`, `obj/`, `*.user`, `*.suo`, local log files).  
- If files are already tracked, run `git rm -r --cached` on those paths once (coordinate with team).  
- Align any **`bazel-*`** mirror or symlinked tree with the same ignore rules if applicable.

---

## 7. Food & Medicine — explicitly out of scope (documented)

**Status:** **Not implemented** and **low priority**.

**Reason:** Those categories are usually consumed **from any inventory slot**, not only toolbelt/equip. Blocking would need **new hooks** (use/eat actions from bag slots), which is a larger design change.

**Action:** Keep this doc + `DESIGN.md` / `CLASS_NAME_MAP_HOWTO.md` aligned so contributors don’t assume Food/Medicine are covered.

---

*Last updated: session documenting post–steel axe map fix.*
