# Gating and restrictions: mapping items to crafting levels and blocking use

This document explains **how** the mod decides that an item is gated by a crafting skill, and **where** in the game UI and world those gates are enforced. It is aimed at maintainers who need to adjust behavior after game updates or add new item types.

High-level design goals live in [`DESIGN.md`](DESIGN.md). Progression reflection details are in [`GAME_API_NOTES.md`](GAME_API_NOTES.md). Map XML attributes are covered in [`CLASS_NAME_MAP_HOWTO.md`](CLASS_NAME_MAP_HOWTO.md). Shift-click vs toolbelt behavior is in [`QUICK_MOVE_AND_TOOLBELT_HOOKS.md`](QUICK_MOVE_AND_TOOLBELT_HOOKS.md). Troubleshooting logs: [`DEBUG_INSTRUMENTATION.md`](DEBUG_INSTRUMENTATION.md).

---

## 1. Core pipeline (names you will see in code)

| Piece | Role |
|--------|------|
| [`ClassNameToCraftingSkillMapLoader`](../src/ClassNameToCraftingSkillMapLoader.cs) | Loads `ClassNameToCraftingSkillMap.xml` (disk + embedded merge). Supplies **skill group**, **progression name overrides**, **required level override/min** per item id. |
| [`GameReflection`](../src/GameReflection.cs) | Resolves **map key** from `ItemClass` / `ItemValue`, **crafting skill group**, **player skill level**, and **required level** from vanilla `Progression` / `DisplayData`. |
| [`RestrictionHelper`](../src/RestrictionHelper.cs) | Shared “is this stack restricted?” check used by patches and red-label UI. |
| [`LimitByCraftingSkillLogic`](../src/LimitByCraftingSkillLogic.cs) | Pure comparison: `requiredLevel > 0` and `playerLevel < requiredLevel` when the skill is enabled in config. |

Restriction applies only when **`GetRequiredLevelForItem` returns a level greater than zero** and config has that skill enabled. If the resolved required level is `0`, the item is treated as **not gated** for that check (see map HOWTO for floors and overrides).

---

## 2. How an item is tied to a crafting level

Several mechanisms stack. Not every item uses every mechanism.

### 2.1 Class name → skill group (`ClassNameToCraftingSkillMap.xml`)

Each `<Item>` row maps a **game item/block id** (class name) to a **logical skill group** (`craftingSkillGroup="Handguns"`, `HarvestingTools`, `Workstations`, …). That group drives which **progression tree** (`craftinghandguns`, …) is queried and which **Config.xml** toggle applies (`ToProgressionOrConfigName` maps names like `Clothing` → `Armor`).

Optional attributes on the same row:

| Attribute | Purpose |
|-----------|---------|
| `progressionMatchName` | When the progression row uses a different string than the item id, this is the name used for matching `DisplayData` / unlock rows. |
| `requiredLevelOverride` | If vanilla progression resolves to `0` or too low, force a **fixed** minimum crafting level for that id. |
| `requiredLevelMin` | **Floor** the resolved level to at least this value (useful when the first matched row is tier `0` in vanilla data). |

Loader and merge order: embedded resource first, then file on disk over the mod folder (see [`ModContentRoot`](../src/ModContentRoot.cs)).

### 2.2 Vanilla progression: “required level” for a skill + item

For a mapped skill group, [`GameReflection.GetRequiredLevelForItem`](../src/GameReflection.cs) walks the local player’s **Progression** into the matching **ProgressionClass** and its **`DisplayDataList`**. It must find a row (or unlock child) that matches the item, then read **how much skill level** that row demands for the item’s **quality context**.

Rough categories of data inside a `DisplayData` row:

1. **Positional `unlock_level` CSV (parsed into `QualityStarts`)**  
   Many `display_entry` rows (including **craftingSeeds** tier bands) use a comma-separated **`unlock_level`** in **progression.xml**; vanilla loads it into **`DisplayData.QualityStarts`**. Unit tests may use a fake **`unlock_level`** string field. **`GetRequiredLevelFromDisplayData`** reads those gates **before** `GetQualityLevel`, because composite rows pass a **small slot index** (1–5) that must not be interpreted as rolled item quality (`GetQualityLevel(L) >= 5` would otherwise return a far too low level).

2. **`QualityStarts` array index**  
   Used when the argument is a 1-based tier index and the array has a positive gate at that index (tests and some rows).

3. **Rolled item quality (`ItemValue.Quality`) via `GetQualityLevel`**  
   For weapons, tools, and armor, the mod finds the smallest crafting level `L` such that **`GetQualityLevel(L) >=`** stack quality (inverse lookup). When `Quality` is `0` / unrated, the lookup uses **tier `1`** as a minimum so progression still resolves.

4. **Composite rows with several unlock children**  
   One `DisplayData` row can list **multiple different items** (e.g. **craftingFood** bands) via **`UnlockDataList`** / `GetUnlockData(i)`. The **unlock_level** column is **1-based** into **`QualityStarts`**. The resolver uses **child count** to decide whether to treat the argument as “composite unlock column” vs “stack quality tier” (see `ResolveDisplayDataQualityOrUnlockColumn` in `GameReflection.cs`).  
   Vanilla **`progression.xml`** often uses **one** `<unlock_entry item="a,b" unlock_tier="N"/>` (comma-separated **item** list). The game may still represent that as a **single** `UnlockData` with a **comma-separated `ItemName`**, or expand it into **several** children that **share** the same **`UnlockTier`**. The mod **matches** map keys against **each comma token** in `ItemName`, and for **multi-child** rows **`ResolveTierForUnlockChild`** uses the **matched** child’s stored **`UnlockTier`** (plus the usual **0-based → 1-based** column mapping) whenever that value is readable — **not** list index **`u + 1`**, which would mis-gate when the in-memory list order and column order disagree. If **`UnlockTier`** is missing, the mod falls back to **1-based** list position. **`UnlockTier`** may be boxed as **`byte`/`short`/`uint`**; the reader coerces any non-negative integral. Single-child rows use **`ReadUnlockTierForQualityStarts`** as for **plantedAloe1**.

5. **Synthetic tier for “no quality” skills**  
   Placeables and some skills (`Electrician`, `Workstations`, `HarvestingTools`, `Explosives`, `Seeds`, `Food`, `Medical` in `UsesSyntheticQualityTierForRequiredLevel`) often have stacks with **no meaningful `ItemValue.Quality`**. The mod still runs the progression lookup using **minimum tier `1`** so forges, mines, etc. get a non-zero gate when the row matches.

### 2.3 Map key resolution (`GetItemClassNameForMap`)

The map is keyed by **item/block id strings**. For **block** placeables (mines, doors, workstations), **`ItemClass.IsBlock()`** is true: the mod may use **`GetItemOrBlockId` + `ItemClass.list`**, **`ToBlockValue`**, or **`BlockValue.type` + `Block.list`** so XML **Extends** variants (same parent `Name`, different concrete block) still resolve to the correct id.

For **handheld** items (`IsBlock` false), those block-oriented paths are **skipped** so a stray `ToBlockValue()` (often “air” / terrain) does not override **`ItemClass.Name`**.

### 2.4 Electrician vs Workstations

Some electric placeables match **`craftingelectrician`** at a low tier while the real gate sits under **`craftingworkstations`**. After resolving Electrician, the code may take the **max** with a Workstations pass for that item (see `GetRequiredLevelForItemWithProgression`).

### 2.5 HarvestingTools: stone tier (`T0` axe / shovel) and `progressionMatchName`

Vanilla **`craftingHarvestingTools`** has a **stone** `display_entry` with **`unlock_level="1,2,4,6,8,10"`** (quality bands 1–6). The next tier (**iron**) uses **`11,13,16,…`**. In **`ClassNameToCraftingSkillMap.xml`**, stone axe / shovel rows often set **`progressionMatchName`** to an **iron** tool id so progression **matching** still works (the stone row’s **`unlock_entry`** lists **`meleeToolRepairT0StoneAxe`** and **`meleeToolShovelT0StoneShovel`**, not **`meleeToolAxeT0StoneAxe`**).

**Problem A — iron bands on stone quality:** [`BuildProgressionMatchCandidates`](../src/GameReflection.cs) prepends **`progressionMatchName`**, so the first successful resolution can apply the **iron** row’s bands to **every** stone quality (e.g. quality 2 would require skill **13** instead of **2**). **`GetRequiredLevelForItemWithProgression`** therefore re-resolves the **stone** row using **anchor ids only** (no map override list): **`meleeToolShovelT0StoneShovel`** for the shovel, and **`meleeToolRepairT0StoneAxe`** for any id whose name contains **`stoneaxe`** (stone axe and repair stone axe share that progression row). When that anchor read returns **`stoneLv >= effectiveQuality`**, it replaces the tree result; when the stone row is **degenerate** (flat bands in tests), **`stoneLv < effectiveQuality`** and the **iron-based** level is kept.

**Problem B — shared `UnlockTier` on multiple children:** The stone `display_entry` is commonly represented as **two** `UnlockData` children that **share the same stored `UnlockTier`** (one XML **`unlock_tier`** for both tools). For generic **multi-child** composites (food, explosives), the column into **`unlock_level`** / **`QualityStarts`** is the **matched unlock tier only**. For **all children sharing the same `UnlockTier`**, the column must still follow **stack quality** (1-based index into the CSV). [`AllUnlockChildrenShareSameStoredUnlockTierZeroBased`](../src/GameReflection.cs) detects that pattern; [`ResolveDisplayDataQualityOrUnlockColumn`](../src/GameReflection.cs) then uses **`max(unlockTier1Based, itemQuality)`** so qualities 3–6 read **4, 6, 8, 10** instead of always using column **1**.

---

## 3. Where restrictions are enforced (player actions)

The mod does **not** try to intercept every possible code path once; it targets the **minimal set of hooks** that cover gameplay. After a game update, **renamed types/methods** or **new UI entry points** are the usual breakage points.

### 3.1 Inventory: hotbar (toolbelt)

| Player action | What we hook | Implementation |
|----------------|--------------|----------------|
| **Drag-and-drop** onto a hotbar slot | `XUiC_ItemStack.HandleStackSwap` | [`ToolbeltHandleStackSwapPatch`](../src/ToolbeltHandleStackSwapPatch.cs) (applied from [`ModApi`](../src/ModApi.cs) against the **game** assembly). |
| **Equip key / quick move to toolbelt** | `XUiM_PlayerInventory.AddItemToToolbelt`, `AddItemToPreferredToolbeltSlot` | [`AddItemToToolbeltRestrictionPatch`](../src/AddItemToToolbeltRestrictionPatch.cs). |
| **Setting hotbar slot programmatically** | `Inventory.SetItem` | [`HotbarRestrictionPatch`](../src/HotbarRestrictionPatch.cs) (`[HarmonyPatch]` on mod assembly). |

Shift-click flows: vanilla tries **backpack** before **toolbelt**; the mod intentionally **does not** patch `HandleMoveToPreferredLocation` so backpack merges keep working—see [`QUICK_MOVE_AND_TOOLBELT_HOOKS.md`](QUICK_MOVE_AND_TOOLBELT_HOOKS.md).

### 3.2 Inventory: armor / clothing / equipment slots

| Player action | What we hook | Implementation |
|----------------|--------------|----------------|
| **Click-to-equip** (equipment UI) | `XUiM_PlayerEquipment.EquipItem(ItemStack)` | [`EquipItemRestrictionPatch`](../src/EquipItemRestrictionPatch.cs) (game assembly). |
| **Drag-and-drop** onto an equipment slot | `XUiC_EquipmentStack.HandleStackSwap` | [`EquipmentStackHandleStackSwapPatch`](../src/EquipmentStackHandleStackSwapPatch.cs) (game assembly). |
| **Some programmatic equip paths** | `Equipment.SetSlotItem` | [`EquipmentRestrictionPatch`](../src/EquipmentRestrictionPatch.cs) (`[HarmonyPatch]`). |

### 3.3 Toolbelt / equipment “equip” from item list UI

| Player action | What we hook | Implementation |
|----------------|--------------|----------------|
| **Activate equip entry** in item lists | `ItemActionEntry.OnActivated` (name may vary; see `ModApi`) | [`ItemActionEntryEquipPatch`](../src/ItemActionEntryEquipPatch.cs) (game assembly). |

### 3.4 Workstations (open UI, chemistry, tool grids)

| Player action | What we hook | Implementation |
|----------------|--------------|----------------|
| **Open crafting / workstation window** | `XUiC_WorkstationWindowGroup` / related `OnOpen` postfixes, `CraftingWindowGroup.OnOpen` filtered | [`WorkstationRestrictionUi`](../src/WorkstationRestrictionUi.cs) via `ModApi.ApplyWorkstationWindowOnOpenRestrictionPatchFromGameAssembly`. |
| **Bind tile entity** | `WorkstationWindowGroup.SetTileEntity` postfix | Same class—keeps context when the window opens. |
| **Central “workstation opened”** | `GameManager.workstationOpened` postfix | `WorkstationRestrictionUi` from `ModApi`. |
| **Chemistry station window by name** | `GUIWindowManager.Open` / `OpenIfNotOpen` postfixes | [`GUIWindowManagerOpenNameLogPatch`](../src/GUIWindowManagerOpenNameLogPatch.cs) (closes + popup if below level). |
| **Drag tool into workstation tool slot** | `HandleStackSwap` on workstation tool grid | [`WorkstationToolHandleStackSwapPatch`](../src/WorkstationVehicleStackSwapPatches.cs). |

#### 3.4.1 Dew Collector and Apiary (`BlockCollector`, `dewcollector` XUi)

Vanilla **`cntDewCollector`** and **`cntApiary`** use **`Class=Collector`** and the same XUi **`window_group`** name **`dewcollector`** (`XUiC_DewCollectorWindowGroup`), **not** `WorkstationWindowGroup` / `CraftingWindowGroup`. UI-only hooks can miss the real open path or mis-resolve the block id when the window name is shared.

| Player action | What we hook | Implementation |
|----------------|--------------|----------------|
| **Activate / open** (primary gate) | **`BlockCollector.OnBlockActivated`** (instance methods on **`BlockCollector`** only: `WorldBase, …` and `String, WorldBase, …` overloads) | **`ModApi.ApplyCollectorOpenRestrictionPatchFromGameAssembly`** — Harmony **prefix** on the **game** assembly, delegating to [`WorkstationOpenRestrictionPatch.Prefix` / `PrefixWithCommand`](../src/WorkstationOpenRestrictionPatch.cs) so **`GetRequiredLevelForWorkstationBlock`** and **Workstations** config apply **before** the collector UI opens (e.g. Apiary **`requiredLevelOverride`** 30 vs Dew Collector 16). |
| **UI bind / reopen** (supplement) | **`XUiC_DewCollectorWindowGroup.SetTileEntity(TileEntityCollector)`**, **`OnOpen()`** postfixes | Same [`WorkstationRestrictionUi`](../src/WorkstationRestrictionUi.cs) helpers as other workstations—tile entity’s **`blockValue`** drives the correct map key (see [`GameReflection.GetBlockValueFromTileEntity`](../src/GameReflection.cs)). |

**Why not prefix every `BlockWorkstation.OnBlockActivated`?** Broad prefix-on-activate on **all** workstation blocks correlated with **client UI lock** in testing, so those prefixes stay **off**; see [`ApplyWorkstationOpenPatchFromGameAssembly`](../src/ModApi.cs). **`BlockCollector`** is a **narrow** exception: same restriction logic, scoped only to Dew Collector / Apiary activation.

### 3.5 Vehicles

| Player action | What we hook | Implementation |
|----------------|--------------|----------------|
| **Enter driver seat** | `EntityVehicle.EnterVehicle` prefix | [`VehicleDriveRestrictionPatch`](../src/VehicleDriveRestrictionPatch.cs). |
| **Place / spawn vehicle item** | `ItemActionSpawnVehicle.Execute` (or equivalent) | [`ItemActionSpawnVehicleRestrictionPatch`](../src/ItemActionSpawnVehicleRestrictionPatch.cs). |
| **Drag mod into vehicle part slot** | `ItemPartStack.HandleStackSwap` | [`VehiclePartHandleStackSwapPatch`](../src/WorkstationVehicleStackSwapPatches.cs). |

Inventory / hotbar restrictions still apply to vehicle **items** in bags; driving is a separate gate.

### 3.6 Food and Medical (consumables, optional in `Config.xml`)

| Player action | What we hook | Implementation |
|----------------|--------------|----------------|
| **Eat / drink / use med** (held, primary) | `ItemActionEat.ExecuteAction` | [`ItemActionExecuteRestrictionPatch`](../src/ItemActionExecuteRestrictionPatch.cs) — validate on **mouse release** (same idea as throw). |
| **Instant use** (inventory / UI) | `ItemActionEat.ExecuteInstantAction` | Same class — **prefix**; blocks when `RestrictionHelper.IsItemRestricted` and shows popup. |

**Off by default:** `Config.xml` → **`<Food>false</Food>`** and **`<Medical>false</Medical>`** in shipped defaults. When enabled, items in [`ClassNameToCraftingSkillMap.xml`](../src/ClassNameToCraftingSkillMap.xml) for those skills use **required level** from **`craftingFood`** / **`craftingMedical`** (see **§2.2.4** for composite `unlock_entry` / comma `item=` / per-child `UnlockTier`). In-inventory **red label** still uses `RestrictionHelper` when the toggles are on.

### 3.7 Throw, place block, rockets (held item actions)

| Player action | What we hook | Implementation |
|----------------|--------------|----------------|
| **Throw away / thrown weapon / place as block / projectile** | `ItemAction*.Execute` on concrete game types | [`ItemActionExecuteRestrictionPatch`](../src/ItemActionExecuteRestrictionPatch.cs) (several prefixes; see `ModApi.ApplyItemActionExecuteRestrictionPatchesFromGameAssembly`). |

### 3.8 Visual feedback (not a hard block)

| Surface | Behavior | Implementation |
|---------|----------|----------------|
| **Red item name** in grids | Tint label when `RestrictionHelper.IsItemRestricted` | [`InventoryGridRestrictionColorPatches`](../src/InventoryGridRestrictionColorPatches.cs) + `ModApi.ApplyInventoryGridPatchesFromGameAssembly`. |
| **Tooltips** | Tint / text hints | [`PopupToolTipDisplayTooltipTextPatch`](../src/PopupToolTipDisplayTooltipTextPatch.cs), [`GameManagerShowTooltipTintPatch`](../src/GameManagerShowTooltipTintPatch.cs), etc. |
| **Popups when blocked** | “You don’t know how to use …” style | [`RestrictionFeedback`](../src/RestrictionFeedback.cs). |

### 3.9 Upgrade / modifier installs (policy: do not gate by default)

**Design intent** (see [`DESIGN.md`](DESIGN.md) § Workstations → *Upgrade / modifier items*): items that only exist as **installs** into another entity—workstation upgrades (Crucible, Bellows, …), vehicle mods (extra seat, …), weapon attachments (scopes, …), **battery** cells slotted into devices—should remain **usable for installation** without a crafting-skill check. The mod achieves that by **omitting** those class names from `ClassNameToCraftingSkillMap.xml`, so `GetRequiredLevelForItem` / `RestrictionHelper` never mark them restricted.

**Hook caveat (maintainers):** a few drag targets still consult `RestrictionHelper` on the **dragged** stack:

| Surface | File | Behavior |
|---------|------|----------|
| Workstation **tool** grid (forge hammer slot, etc.) | [`WorkstationToolHandleStackSwapPatch`](../src/WorkstationVehicleStackSwapPatches.cs) | Blocks drop if `RestrictionHelper.IsItemRestricted(drag)` — **any** mapped+restricted item. Do **not** map workstation upgrade tools here unless you intend to block them. |
| Vehicle **part** grid | [`VehiclePartHandleStackSwapPatch`](../src/WorkstationVehicleStackSwapPatches.cs) | Only when **Vehicles** restriction is enabled **and** `GetCraftingSkillGroup` is **Vehicles** **and** the stack is restricted; then the drop is blocked. Other skill groups are not checked on this path. |

There is **no** dedicated patch set for “weapon mod slot” or “battery slot” inventory grids; those stacks follow normal inventory rules plus whatever the item is **mapped** as. **Policy:** keep upgrade/mod/battery **item ids** out of the map unless you explicitly want them gated.

### 3.10 Other patches

- [`ProgressionLevelUpPatch`](../src/ProgressionLevelUpPatch.cs): marks restriction UI dirty after crafting skill changes.  
- [`PatchAll`](../src/ModApi.cs) also picks up `[HarmonyPatch]` types in the mod assembly (e.g. `HotbarRestrictionPatch`, `EquipmentRestrictionPatch`) in addition to the **explicit** `Apply*` game-assembly patches.

---

## 4. After a game update

1. **Symptom: wrong skill or always unrestricted**  
   Check **map key** (`GetItemClassNameForMap`) and **map row** for that id; then **`GetCraftingSkillGroup`** and **`GetRequiredLevelForItem`** with `DebugMode` logging.

2. **Symptom: wrong tier vs quality gate**  
   Inspect the relevant **`DisplayData`** row: does it use **`GetQualityLevel`**, **`QualityStarts`**, unlock children, or CSV `unlock_level`? Align `GameReflection.GetRequiredLevelFromDisplayData` and `TryResolveRequiredLevelWithProgressionName` with vanilla data.

3. **Symptom: item moves but should not (or opposite)**  
   Find which **hook** in section 3 should cover that UI path; compare decompiled **`Assembly-CSharp`** signatures to the `ModApi` reflection `GetMethod` calls.

---

## 5. Related files (quick index)

| Area | Files |
|------|--------|
| Map XML | [`../src/ClassNameToCraftingSkillMap.xml`](../src/ClassNameToCraftingSkillMap.xml), loader above |
| Config | [`../src/Config.xml`](../src/Config.xml), [`ModConfig.cs`](../src/ModConfig.cs) |
| Progression math | [`GameReflection.cs`](../src/GameReflection.cs) (large; search `GetRequiredLevelForItem`, `TryResolveRequiredLevel`, `GetRequiredLevelFromDisplayData`) |
| Harmony wiring | [`ModApi.cs`](../src/ModApi.cs) |
| Tests for progression shape | [`../tests/RequiredLevelFromProgressionTests.cs`](../tests/RequiredLevelFromProgressionTests.cs), [`../tests/GameReflectionTests.cs`](../tests/GameReflectionTests.cs) |
