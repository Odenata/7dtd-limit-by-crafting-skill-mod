# Design: Limit by Crafting Skill Mod

## Core rule

The player may not equip or use an item if the item's **required level** (for its CraftingSkillGroup and quality/tier) is greater than the player's current level in that crafting skill.

## Item "required level"

- **Source:** Each item has `ItemClass.CraftingSkillGroup` (string) and optionally a quality tier. Quality is exposed as `ItemValue.Quality` (e.g. 1–5). Items without quality are treated as requiring a single tier (e.g. required level 0 or 1 — no restriction, or a minimal level to be validated against the game's progression data).
- **Derivation:** Required level comes from `ProgressionValue` → `ProgressionClass` → `DisplayDataList`. For each matched `DisplayData` row, the mod reads gates in order: positional **`unlock_level`** CSV (and similar), then **`QualityStarts`**, then inverse **`GetQualityLevel`** for rolled item quality—see [`GATING_AND_RESTRICTIONS.md`](GATING_AND_RESTRICTIONS.md). Matching the item to a row: direct `DisplayData.item` / `ItemName` when set; otherwise scan `UnlockDataList` (or `GetUnlockData(i)` when the list is empty), matching via unlock `item`/`ItemName`, `DisplayData.GetUnlockItem(i)`, or `UnlockData.RecipeList` strings. Armor and some other skills use empty top-level `ItemName` and populate unlocks instead.
- **No quality tier:** For **Electrician**, **Workstations**, **HarvestingTools**, **Explosives**, **Seeds**, and (when **Food** / **Medical** restrictions are enabled) most consumables, inventory items often have no meaningful `ItemValue` quality. Those skills use **synthetic tier 1** for progression lookup so placeables, tools, mines, seeds, and consumable rows still resolve a band from vanilla **DisplayData** when progression matches. Rolled quality still applies where vanilla uses it (e.g. weapons, armor).
- **Debug:** With `Config.xml` → `DebugMode` true, `GetRequiredLevelForItem` logs `exit=0` with **reason** (`no_map`, `no_quality`, `no_progression_match`, etc.). `no_map` is logged at most once per item name. For temporary deeper instrumentation, see **`docs/DEBUG_INSTRUMENTATION.md`**.

## Restriction points

### Armor

Prevent placing items into player armor/equipment slots. Block at **XUiM_PlayerEquipment.EquipItem** (click-to-equip) and at **XUiC_EquipmentStack.HandleStackSwap** (drag-drop onto equipment slot). When blocking drag-drop the item remains on the cursor (we block before the move). Optionally show feedback.

### Handheld (weapons and tools)

**Preferred approach:** Prevent adding the item to the hotbar. All code paths that put an item into a hotbar slot must be blocked. Block at **XUiC_ItemStack.HandleStackSwap** when the drop target is a toolbelt slot; block at **XUiM_PlayerInventory.AddItemToToolbelt** / **AddItemToPreferredToolbeltSlot** (covers quick-move / shift-click fallback after backpack merge—see **`docs/QUICK_MOVE_AND_TOOLBELT_HOOKS.md`**). Do **not** prefix **HandleMoveToPreferredLocation**: vanilla tries **AddItemToBackpack** before **AddItemToToolbelt**, and skipping the whole method breaks backpack stacking.

**Rejected alternatives (documented only):**

- **Prevent setting active slot:** Would make controls less predictable; discarded.
- **Allow equipping but prevent abilities:** Would require checking on every use (expensive) and could confuse the player when nothing happens; documented as considered.

### Workstations

When the player attempts to open the placed workstation UI (e.g. default keybind 'E'), check their crafting level for the Workstations skill against the placed block's required level. If below, block opening the interface and show feedback. Picking up and other interactions may remain allowed.

### Upgrade / modifier items (intentionally permissive)

**Policy:** We do **not** treat typical **upgrade** or **install-only** items as gated end products. Those are things the player inserts into another block or item to improve it, rather than “uses” as a standalone equipped tool or placeable. Examples: **workstation** upgrade parts (Crucible, Bellows, Anvil, …), **vehicle** modifier parts (extra seat, armor plating, …), **weapon** attachments (scopes, magazines, …), and **battery** items slotted into battery banks.

**How that works in practice:**

- They are **not listed** in [`ClassNameToCraftingSkillMap.xml`](../src/ClassNameToCraftingSkillMap.xml), so [`RestrictionHelper`](../src/RestrictionHelper.cs) does not mark them restricted for skill-level purposes.
- We **do not** add dedicated “block every insert into part / mod / battery / workstation-upgrade slot” rules for those categories. Some existing hooks (e.g. workstation **tool** grids, vehicle **part** grids) still call `RestrictionHelper` for **dragged** stacks; keeping upgrade items **unmapped** avoids accidental blocks there. Maintainers should **not** map those ids to a crafting group unless there is a deliberate product decision to gate that install path.

See [`GATING_AND_RESTRICTIONS.md`](GATING_AND_RESTRICTIONS.md) §3.9 for hook-level nuance (e.g. workstation tool grid, vehicle part slots, and the Vehicles skill).

**Chemistry Station:** blocked by intercepting its specific UI window name (`GUIWindowManager.Open("workstation_chemistryStation", ...)`) when the player is below the required Workstations level.

### Vehicles

Only restrict the **drive** action. Allow: open vehicle inventory, refuel, pick up, passenger seats. Map vehicle entity/block to required level via the **Vehicles** crafting skill. Vehicle types are mapped to crafting components by the chassis heuristic: e.g. look up component by vehicle name + `" Chassis"` (e.g. "Bicycle Chassis" for "Bicycle"). Avoid hardcoding vehicle names; use a data-driven or name-based lookup so new/renamed vehicles work.

### Traps and robotics

Treated as handheld: if the item cannot be placed in the hotbar, it cannot be held and thus cannot be placed in the world. No separate restriction path.

## Visualization

### Inventory

Indicate restricted items by coloring the in-inventory item **label red** wherever the player sees items: player inventory (backpack, toolbelt), character equipment slots, container/loot inventories, vehicle inventory, workstation inventory, and any other UI that displays item stacks or equipment. The same rule applies for all restricted item types (armor, tools, workstations, vehicles); the only difference is which grid type the game uses (item-stack grid vs equipment-stack grid). Grids are detected by **base type** (`XUiC_ItemStackGrid` / `XUiC_EquipmentStackGrid`) so all subclasses (e.g. Backpack, Toolbelt, PartList, VehicleContainer, WorkstationGrid) are covered. Labels are provided for stackable items and items with quality tiers, so not all items have a label to turn red.

**Triggers:** (1) **OnOpen** Postfix on `XUiC_ItemStackGrid` and `XUiC_EquipmentStackGrid` calls `ApplyRestrictionColorsToGrid` once. (2) When the player levels up a crafting skill, **Progression.addProgressionCurrency** Postfix sets a dirty flag. (3) Each grid’s **Update** Postfix, when the grid is open, runs `ApplyRestrictionColorsToGrid` only if the dirty flag is set or a per-grid throttle has elapsed (e.g. every 0.2s per open grid), then clears the dirty flag. Immediate apply on open and when dirty keeps labels correct after open/level-up; throttling the steady-state refresh limits compute. The throttle interval can be tuned if the game overwrites label color more frequently.

**Label application:** Restriction check is shared (`RestrictionHelper.IsItemRestricted`). For item grids we use `SetLabelColorOnEntry(go)` on the slot controller’s view GameObject (ViewComponent → uiTransform → gameObject) and its children so the visible item name label is colored; for equipment we use the grid’s `items` array or player equipment fallback and the same label-setting approach. Label color is set via reflection on UILabel `color`/`mColor`. Grey overlay or "X" on the sprite are documented as future work if needed.

### In-world (workstation, vehicle, etc.)

When a blocked use occurs, show an **orange** popup using the game's existing popup/notification system with:

1. "You don't know how to use [item name]"
2. "[Crafting Skill Name] [player level]/[required level]"

The implementation uses `GameManager.ShowTooltip` via reflection with `"ui_denied"` where available, plus tooltip tint patches for the mod's denial text; see [`GAME_API_NOTES.md`](GAME_API_NOTES.md) and [`RestrictionFeedback`](../src/RestrictionFeedback.cs).

## Config

- **Per–crafting-skill toggles:** One toggle per crafting skill (e.g. Armor, HarvestingTools, Workstations, Vehicles). When enabled, restriction applies for that skill; when disabled, items for that skill are not restricted.
- **Server vs client:** Single-player uses local `Config.xml`. In multiplayer, the server sends its `Config.xml` to connected clients via the game's config-file net package path, and clients apply that snapshot ahead of local config for restriction checks.

### Food and Medicine (optional toggles) — **implemented**

- **Release default:** **Food** and **Medical** are **false** in `Config.xml` so eat/drink/meds match vanilla use unless the player opts in. Set to **true** to enforce skill gates for mapped consumables in [`ClassNameToCraftingSkillMap.xml`](../src/ClassNameToCraftingSkillMap.xml).

- **Categorization:** Drinks, meals, and similar are mapped to **`Food`**; bandages, kits, drugs, etc. to **`Medical`**. The map drives **`GetCraftingSkillGroup`** and the **`Config.xml`** flag via `ToProgressionOrConfigName` (no rename).

- **Enforcement surface:** When enabled, the mod patches **`ItemActionEat.ExecuteAction`** (check on **mouse release** so hold-to-eat does not spam) and **`ItemActionEat.ExecuteInstantAction`** (context / UI instant use). The same path covers most food, drinks, and medical consumables. Shared check: **`RestrictionHelper.IsItemRestricted`**; popup: **`RestrictionFeedback`**.

- **Required level (progression):** Not stack **quality** — vanilla uses **`craftingFood`** / **`craftingMedical`** with **`display_entry` / `unlock_entry`** and comma-separated `unlock_level` band lists. Important details: XML often has **`item="a,b"`** in one `unlock_entry`; the mod must match **comma tokens** in `UnlockData.ItemName` and resolve the **`UnlockTier` → `QualityStarts` / `unlock_level` column** per **matched** child, not by list index alone. Reflection may box **`UnlockTier`** as `byte`/`short`/etc. Full maintenance notes: **[`GATING_AND_RESTRICTIONS.md`](GATING_AND_RESTRICTIONS.md)** §2.2.4 and §3.6.

## Roadmap / known gaps

Shift-click containers, vehicle/workstation non-inventory paths, name extraction, `.gitignore` hygiene, and related gaps; API/hook table in **`docs/GAME_API_NOTES.md`**.

## Optional (document only, no implementation)

- **Prevent acquisition:** See `docs/OPTIONAL_PREVENT_ACQUISITION.md`.
- **Level restriction offset:** See `docs/OPTIONAL_LEVEL_OFFSET.md`.
