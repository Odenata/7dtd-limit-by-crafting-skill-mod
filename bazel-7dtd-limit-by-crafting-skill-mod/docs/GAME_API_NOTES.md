# Game API notes for Limit by Crafting Skill Mod

Use the [Runtime API Workflow](https://github.com/Odenata/7dtd-mod-dev-tools/blob/main/docs/RUNTIME_API_WORKFLOW.md) in 7dtd-mod-dev-tools: start from checked-in docs in `docs/game-api/`, then query or refresh the runtime export as needed. When the mod compiles but fails in-game, follow [RUNTIME_API_MISMATCH_DEBUGGING.md](https://github.com/Odenata/7dtd-mod-dev-tools/blob/main/docs/RUNTIME_API_MISMATCH_DEBUGGING.md).

**Item loss protection:** The mod must never delete or lose items. Do not return false from a Prefix when the game has already removed the item from its source (e.g. blocking at `Equipment.SetSlotItem` or `Inventory.SetItem` after a drag). Restrict at an earlier point in the flow (e.g. before the move is committed).

## Investigation summary (from checked-in docs)

APIs were confirmed from `7dtd-mod-dev-tools/docs/game-api/assembly-csharp/by-type/`: **EntityAlive.Progression** (field) → **Progression.GetProgressionValue(string)** → **ProgressionValue.Level** for player skill level; **ItemClass.CraftingSkillGroup**; **Inventory.SetItem(int, ItemStack)** with **PUBLIC_SLOTS_PLAYMODE** for hotbar size; **Progression.ProgressionClasses**, **ProgressionClass.DisplayDataList**, **DisplayData.QualityStarts** for required-level mapping. Mod uses reflection (GameReflection) and HotbarRestrictionPatch/ArmorRestrictionPatch. Workstation open, vehicle drive, and popup API remain TBD until confirmed in-game or via runtime export query.

## Player crafting level

- **Goal:** Given an entity (EntityPlayerLocal / EntityAlive) and a crafting skill name (string, e.g. from `ItemClass.CraftingSkillGroup`), obtain the player's current level.
- **Confirmed (from docs/game-api):**
  - `EntityAlive` has field **`Progression Progression`** (not property; try GetField first in reflection).
  - `Progression` has **`GetProgressionValue(System.String _progressionName)`** returning `ProgressionValue`.
  - `ProgressionValue` has **`Level`** (int, property or field).
- **Mod:** `GameReflection.GetPlayerCraftingLevel` uses entity → Progression (field then property) → GetProgressionValue(skillName) → Level.

## Item required level

- **Goal:** Given `ItemClass` + `ItemValue` (with Quality), compute the minimum crafting level required to use that item.
- **Confirmed:** For **restriction** the mod uses **ClassNameToCraftingSkillMap.xml** (item name/key to game-reported name). `ItemValue.Quality` (UInt16). `ItemValue.HasQuality` (property).
- **Mapping (from docs):** `Progression` has **`ProgressionClasses`** (Dictionary<string, ProgressionClass>). `ProgressionClass` has **`DisplayDataList`** (List<ProgressionClass+DisplayData>). `DisplayData` has **`QualityStarts`** (int[]), **`Item`** / **`ItemName`**; **`GetQualityLevel(System.Int32 level)`** returns quality at a given skill level. For a given item quality Q, required level = QualityStarts[Q-1] when in range, else the minimum level L such that GetQualityLevel(L) >= Q.
- **Mod:** `GameReflection.GetRequiredLevelForItem` uses the local player's Progression → ProgressionClasses[progressionLookupName] → DisplayDataList; finds DisplayData matching the item (by Item reference or ItemName); then reads required level from QualityStarts or GetQualityLevel(level). Returns 0 if no matching DisplayData or no quality.

## Inventory and slots

- **Goal:** Identify hotbar slot indices and armor/equipment slot indices so we can block only those.
- **Confirmed (from docs):** `Inventory`: **`SetItem(System.Int32 _idx, ItemStack _itemStack)`**, **`GetSlots()`**, **`slots`** (ItemInventoryData[]). Properties: **`INVENTORY_SLOTS`**, **`PUBLIC_SLOTS`**, **`PUBLIC_SLOTS_PLAYMODE`** (hotbar/toolbelt size in play mode). **`entity`** (EntityAlive). **`itemArmor`** (ItemStack) — armor may be one slot or per-piece; equipment layout may vary. `Bag` is on EntityAlive as **`bag`**; used for storage. Hotbar = first N slots of player Inventory (N = PUBLIC_SLOTS_PLAYMODE, typically 10).
- **Choke point:** Patch **`Inventory.SetItem(int, ItemStack)`** when `idx < PUBLIC_SLOTS_PLAYMODE` (or use constant 10). Armor: if separate from main inventory, patch the setter used when equipping armor (TBD per game version; may be same Inventory or equipment-specific).

## Workstation / block open

- **Goal:** Intercept the moment the player tries to open the workstation UI (e.g. press E on placed Forge/Workbench). Block and show popup if restricted.
- **To confirm:** TileEntity or block interaction that opens the UI; method name and type to patch. May be in block activation or TileEntity open logic.

## Vehicle

- **Goal:** Restrict only "drive"; allow open inventory, refuel, pick up, passenger.
- **To confirm:** How "drive" is triggered vs "open inventory" / "refuel". Method or UI path to patch for drive only. Chassis ↔ vehicle name: e.g. entity or block name containing " Chassis" and mapping to vehicle display name (avoid hardcoding; use name heuristic). Note for future developers that the name heuristic is fragile as future vehicle additions may not use the same naming scheme.

## Popup / feedback

- **Goal:** Show red text: "You don't know how to use [item name]" and "[Crafting Skill Name] [player level]/[required level]".
- **To confirm:** In-game API for on-screen popup or notification (e.g. XUi, notification list, or similar). How to set text color to red.

## Server config

- **Goal:** In multiplayer, read config from server so restrictions are server-authoritative.
- **To confirm:** Where mod config is loaded (client vs server process). How to detect "we are the server" and read server's Config.xml. May require game API for mod config path or network sync.

## Patch targets (from investigation)

| Area         | Target type/method (candidate)                                      | Notes |
|-------------|----------------------------------------------------------------------|-------|
| Player level| `EntityAlive.Progression` → `GetProgressionValue(string)` → `ProgressionValue.Level`. | Progression is keyed by names like `craftingarmor` (not "Clothing" or "Armor"). GameReflection.ToProgressionLookupName maps item group to progression name; see 7dtd-mod-dev-tools docs/PROGRESSION_NAMES.md. |
| Hotbar      | **`XUiC_ItemStack.HandleStackSwap()`** (drag); **`XUiM_PlayerInventory.AddItemToToolbelt`** / **AddItemToPreferredToolbeltSlot** (incl. quick-move after backpack); **`ItemActionEntryEquip.OnActivated()`**; **`Inventory.SetItem`** (observe only) | **ToolbeltHandleStackSwapPatch**, **AddItemToToolbeltRestrictionPatch**, **ItemActionEntryEquipPatch**. No **HandleMoveToPreferredLocation** patch (see **`docs/QUICK_MOVE_AND_TOOLBELT_HOOKS.md`**). |
| Workstation tool slots | **`XUiC_ItemStack.HandleStackSwap`** on **`XUiC_WorkstationToolGrid`** stacks | **WorkstationToolHandleStackSwapPatch** |
| Vehicle mod slots | **`XUiC_ItemPartStack.HandleStackSwap`** under **`XUiC_VehiclePartStackGrid`** | **VehiclePartHandleStackSwapPatch** (Vehicles skill items only) |
| Vehicle deploy | **`ItemActionSpawnVehicle.ExecuteAction`** | **ItemActionSpawnVehicleRestrictionPatch** (client; server should run same mod for MP) |
| Armor       | **`XUiM_PlayerEquipment.EquipItem(ItemStack)`** (block here); **`XUiC_EquipmentStack.HandleStackSwap()`** (block drag-drop); **`Equipment.SetSlotItem`** (observe only) | **EquipItemRestrictionPatch** blocks at EquipItem. **EquipmentStackHandleStackSwapPatch** blocks at HandleStackSwap when the dragged stack (from XUi.dragAndDrop.CurrentStack) is restricted; item stays on cursor. We do not block at SetSlotItem to avoid item loss. |
| Workstation (other) | Material/input grids, TE open | Partial: tool grid only; see §4 in TODO.md. |
| Vehicle (other)     | Fuel, **VehicleInventory**, drive | Partial: part grid + spawn; server sync TBD. |
| Popup       | **`XUiC_CollectedItemList.AddItemStack`** / **`AddCraftingSkillNotification`** or message API | For red text: may need different API (e.g. GameManager, tooltip, or message window); to be confirmed in-game. |
| In-inventory red label | **`XUiC_ItemStackGrid.OnOpen`**, **`XUiC_EquipmentStackGrid.OnOpen`**, grid **`Update`**, **`Progression.addProgressionCurrency`** | Red label for restricted items in **all** UIs that display items: player backpack, toolbelt, equipment, container, vehicle, workstation, etc. Any controller assignable to `XUiC_ItemStackGrid` or `XUiC_EquipmentStackGrid` is treated the same (base-type detection via `IsAssignableFrom`). Known ItemStackGrid subclasses: Backpack, Toolbelt, PartList, VehicleContainer, WorkstationGrid, PowerSourceSlots, PowerRangedAmmoSlots. Apply on grid OnOpen (Postfix), set **RestrictionColorsDirty** when crafting skill levels up, refresh in grid Update when dirty (throttled). Get slot view via **ViewComponent** → **uiTransform** → **gameObject**; set **UILabel** `color`/`mColor` on that GameObject and children (or XUiV_Label for equipment slots). |

After confirming names and overloads in the runtime export (or in-game), update this table and the compat layer. Prefer a small compat helper and reflection for game-version resilience; see RUNTIME_API_MISMATCH_DEBUGGING.md.
