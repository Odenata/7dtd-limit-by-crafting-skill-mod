# TODO / follow-up work

Tracked improvements and known gaps for **LimitByCraftingSkillMod** and related tooling. Check items off as you complete them.

## 1. Validate map names per game version

**Problem:** `ClassNameToCraftingSkillMap.xml` entries need verification against each build (e.g. `meleeToolAxeT2SteelAxe` vs `…SteelFireaxe`).

**Approach:**

- Maintain a checklist or generated table: **className → works in V x.y (bNN)?** / needs `progressionMatchName`? / needs `requiredLevelOverride`?
- Test with a **new, unleveled character** so low skills make restrictions obvious (equip blocked, red labels where applicable).

---

## 2. Non-inventory interactions: vehicles & workstations — partial

**Implemented (prototype):** **`WorkstationToolHandleStackSwapPatch`**, **`VehiclePartHandleStackSwapPatch`**, **`ItemActionSpawnVehicleRestrictionPatch`**; **`WorkstationOpenRestrictionPatch`** (block opening placed workstation UI via **BlockWorkstation.OnBlockActivated**); **`VehicleDriveRestrictionPatch`** (block drive via **EntityDriveable.EnterVehicle**). In-world blocked actions show a **red popup** via **RestrictionFeedback.ShowRestrictionPopup** (GameManager.ShowTooltip with "ui_denied"). See **`docs/GAME_API_NOTES.md`**.
**Workstations (final for Chemistry Station):** blocked at **`GUIWindowManager.Open("workstation_chemistryStation", ...)`**.

**Still open:** **`VehicleInventory`** / fuel / refuel actions; server-authoritative validation for multiplayer; workstation **input/material** grids beyond tool slots.

---

## 3. Optional: non-quality items & inventory UI clarity

**Context:** Non-stacking items **without** a quality attribute often show **no restricted label** in inventory (vehicles, workstations are examples).

**Options:**

- After **§4** is solid, **drop inventory restriction** for categories that are fully gated elsewhere (if inventory restriction adds no value).
- Or add a **non-quality visual** (tooltip / icon) where patches already run.

---

## 4. Repository hygiene: `.gitignore`

**Problem:** Build outputs and local logs are easy to commit by mistake (`src/bin/`, `src/obj/`, `tests/bin/`, `tests/obj/`, `debug-*.log`, Bazel outputs, copied game DLLs in output folders).

**TODO:**

- Add root **`.gitignore`** (typical .NET: `bin/`, `obj/`, `*.user`, `*.suo`, local log files).
- If files are already tracked, run `git rm -r --cached` on those paths once (coordinate with team).
- Align any **`bazel-*`** mirror or symlinked tree with the same ignore rules if applicable.
