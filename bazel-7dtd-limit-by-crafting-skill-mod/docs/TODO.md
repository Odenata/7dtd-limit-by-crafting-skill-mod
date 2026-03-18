# TODO / follow-up work

Tracked improvements and known gaps for **LimitByCraftingSkillMod** and related tooling. Check items off as you complete them.

---

## 1. Shift-click / container moves (high priority)

**Problem:** Restriction logic is blocking **shift-click** (and possibly other flows) when moving **correctly restricted** items into **container inventories** (loot, storage, etc.).  

**Goal:** Restrict only moves into **toolbelt / hotbar** and **armor (equipment) slots**. Moving restricted items into chests, vehicles, workstations, player backpack-as-container, etc. should behave like vanilla.

**Notes:** Audit Harmony patches on stack swap / move handlers; distinguish target grid type (hotbar vs container vs equipment).

---

## 2. Extract item names & progression names from game data

**Problem:** `ItemClass.Name` and names on progression unlock rows often differ; we can’t rely on checked-in game API docs alone.

**Ideas:**

- Parse vanilla **`items.xml`** / **`progression.xml`** (and modded copies) from the game install to build:  
  - all item class names  
  - progression `DisplayData` / unlock identifiers as used in XML  
- Or runtime **instrumentation**: scan **creative menu** item lists and log `ItemClass.Name` + `CraftingSkillGroup`.

**Dev-tools:** When a pipeline exists, add a section to **`7dtd-mod-dev-tools`** API / workflow docs (e.g. under `docs/RUNTIME_API_WORKFLOW.md` or a new doc linked from `docs/game-api/README.md`). Placeholder: [`TODO_ITEM_PROGRESSION_NAME_PIPELINE.md`](../../7dtd-mod-dev-tools/docs/TODO_ITEM_PROGRESSION_NAME_PIPELINE.md) (sibling repo under `repos/`).

---

## 3. Validate map names per game version

**Problem:** `ClassNameToCraftingSkillMap.xml` entries need verification against each build (e.g. `meleeToolAxeT2SteelAxe` vs `…SteelFireaxe`).

**Approach:**

- Maintain a checklist or generated table: **className → works in V x.y (bNN)?** / needs `progressionMatchName`? / needs `requiredLevelOverride`?  
- Test with a **new, unleveled character** so low skills make restrictions obvious (equip blocked, red labels where applicable).

---

## 4. Non-inventory interactions: vehicles & workstations

**Problem:** Restrictions today focus on **inventory / equip / toolbelt**. Players may still **use** gated items via:

- **Vehicle** interactions (install / fuel / drive-related items if applicable)  
- **Workstation** UI (crafting, upgrading, placing from workstation context)

**Goal:** Define desired behavior and patch the relevant code paths so skill gates match inventory rules where intended.

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
