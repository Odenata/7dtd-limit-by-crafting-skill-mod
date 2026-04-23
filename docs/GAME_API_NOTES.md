# Game API notes for Limit by Crafting Skill Mod

Use the [Runtime API Workflow](https://github.com/Odenata/7dtd-mod-dev-tools/blob/main/docs/RUNTIME_API_WORKFLOW.md) in 7dtd-mod-dev-tools: start from checked-in docs in `docs/game-api/`, then query or refresh the runtime export as needed. When the mod compiles but fails in-game, follow [RUNTIME_API_MISMATCH_DEBUGGING.md](https://github.com/Odenata/7dtd-mod-dev-tools/blob/main/docs/RUNTIME_API_MISMATCH_DEBUGGING.md).

**Item loss protection:** The mod must never delete or lose items. Do not return false from a Prefix when the game has already removed the item from its source (e.g. blocking at `Equipment.SetSlotItem` or `Inventory.SetItem` after a drag). Restrict at an earlier point in the flow (e.g. before the move is committed).

## Investigation summary (from checked-in docs)

APIs were confirmed from `7dtd-mod-dev-tools/docs/game-api/assembly-csharp/by-type/`: **EntityAlive.Progression** (field) → **Progression.GetProgressionValue(string)** → **ProgressionValue.Level** for player skill level; **ItemClass.CraftingSkillGroup**; **Inventory.SetItem(int, ItemStack)** with **PUBLIC_SLOTS_PLAYMODE** for hotbar size; **Progression.ProgressionClasses**, **ProgressionClass.DisplayDataList**, **DisplayData.QualityStarts** for required-level mapping. Mod uses reflection (GameReflection) and HotbarRestrictionPatch/ArmorRestrictionPatch. Workstation open (**BlockWorkstation.OnBlockActivated**), vehicle drive (**EntityDriveable.EnterVehicle**), and popup (**GameManager.ShowTooltip** with "ui_denied") are implemented; see sections below.

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

- **Goal:** Prevent using the workstation UI when under level; show popup; **do not** use Harmony Prefix+skip on block/UI methods (causes client UI lock until rejoin).
- **Confirmed:** The client reliably runs **`XUiC_WorkstationWindowGroup.OnOpen()`** when the crafting workstation UI opens; **`GameManager.workstationOpened`** is **not** always invoked on that path. **Mod:** **Postfix on `OnOpen`** — after vanilla OnOpen, if restricted: **`GUIWindowManager.CloseIfOpen(WorkstationWindow)`** (from **`BlockWorkstation.WorkstationData`** when known), **`XUi.GetWindowsByType` for `XUiC_WorkstationWindowGroup`**, close controller, then **`RestrictionFeedback.ShowRestrictionPopup`**. **`WorkstationOpenRestrictionPatch`** remains unregistered for reference.
- Required level for block: **`GameReflection.GetRequiredLevelForWorkstationBlock(BlockValue)`** using block name → ClassNameToCraftingSkillMap (Workstations) → progression with synthetic quality 1.

## Vehicle

- **Goal:** Restrict only "drive"; allow open inventory, refuel, pick up, passenger.
- **Confirmed:** **`EntityDriveable.EnterVehicle(EntityAlive _entity)`** is called when the player chooses "drive" (attach as driver). Other actions (open inventory, refuel) use different code paths. Patch EnterVehicle with a Prefix: when Vehicles restriction applies and vehicle required level &gt; player level, show popup and return false. Vehicle → required level: **`GameReflection.GetRequiredLevelForVehicleEntity(object)`** / **GetVehicleEntityMapKey(object)** using entity type name heuristic (e.g. EntityBicycle → vehicleBicyclePlaceable) and ClassNameToCraftingSkillMap (Vehicles); progression with synthetic tier 1.
- **Mod:** **VehicleDriveRestrictionPatch** (client-side).

## Popup / feedback

- **Goal:** Show red text: "You don't know how to use [item name]" and "[Crafting Skill Name] [player level]/[required level]".
- **Confirmed (runtime export):** **`GameManager.ShowTooltip`** static overloads include:
  - `(EntityPlayerLocal, string, bool, bool, float)`
  - `(EntityPlayerLocal, string, string, string, ToolTipEvent, bool, bool, float)` — use **`_alertSound = "ui_denied"`** for the red/denied style (same as vanilla e.g. ttWorkstationNotEmpty, ttRepairBeforePickup).
  - `(EntityPlayerLocal, string, string[], string, ToolTipEvent, bool, bool, float)`
  - **`ShowTooltipMP(EntityPlayer, string, string)`** for multiplayer-oriented paths if needed.
  There is **no** `(EntityPlayerLocal, string, string, string)`-only overload in current exports; the handler and trailing parameters are required.
- Two lines can be passed as a single string with `\n`. **RestrictionFeedback.ShowRestrictionPopup** wraps the body in **NGUI color tags** (`[ff3030]` … `[-]`) so the tooltip body renders red; still passes **`ui_denied`** when available. Invokes ShowTooltip via reflection with fallbacks (9-arg → string[] 9-arg → 5-arg → ShowTooltipMP).

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
| Workstation open UI | **`XUiC_WorkstationWindowGroup.OnOpen` Postfix** (close + popup if restricted) | **WorkstationWindowOnOpenRestrictionPatch**. `workstationBlock` / `workstationData.TileEntity` for level; `WorkstationData.WorkstationWindow` for **`CloseIfOpen`**. |
| Workstation (other) | Material/input grids, TE open | Partial: tool grid only; see §4 in TODO.md. |
| Vehicle drive       | **`EntityDriveable.EnterVehicle(EntityAlive)`** | **VehicleDriveRestrictionPatch** (client). Entity type → GetRequiredLevelForVehicleEntity. |
| Vehicle (other)     | Fuel, **VehicleInventory**, part grid, spawn | Part grid + spawn patched; drive patched; server sync TBD. |
| Popup       | **`GameManager.ShowTooltip(EntityPlayerLocal, string, string, string, ToolTipEvent, bool, bool, float)`** with **`"ui_denied"`**; fallbacks above | **RestrictionFeedback.ShowRestrictionPopup** (reflection). Red style. |
| In-inventory red label | **`XUiC_ItemStackGrid.OnOpen`**, **`XUiC_EquipmentStackGrid.OnOpen`**, grid **`Update`**, **`Progression.addProgressionCurrency`** | Red label for restricted items in **all** UIs that display items: player backpack, toolbelt, equipment, container, vehicle, workstation, etc. Any controller assignable to `XUiC_ItemStackGrid` or `XUiC_EquipmentStackGrid` is treated the same (base-type detection via `IsAssignableFrom`). Known ItemStackGrid subclasses: Backpack, Toolbelt, PartList, VehicleContainer, WorkstationGrid, PowerSourceSlots, PowerRangedAmmoSlots. Apply on grid OnOpen (Postfix), set **RestrictionColorsDirty** when crafting skill levels up, refresh in grid Update when dirty (throttled). Get slot view via **ViewComponent** → **uiTransform** → **gameObject**; set **UILabel** `color`/`mColor` on that GameObject and children (or XUiV_Label for equipment slots). |

After confirming names and overloads in the runtime export (or in-game), update this table and the compat layer. Prefer a small compat helper and reflection for game-version resilience; see RUNTIME_API_MISMATCH_DEBUGGING.md.
