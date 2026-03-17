# Design: Limit by Crafting Skill Mod

## Core rule

The player may not equip or use an item if the item's **required level** (for its CraftingSkillGroup and quality/tier) is greater than the player's current level in that crafting skill.

## Item "required level"

- **Source:** Each item has `ItemClass.CraftingSkillGroup` (string) and optionally a quality tier. Quality is exposed as `ItemValue.Quality` (e.g. 1–5). Items without quality are treated as requiring a single tier (e.g. required level 0 or 1 — no restriction, or a minimal level to be validated against the game's progression data).
- **Derivation:** Required level is derived from the game's progression/recipe data. The game uses `ProgressionClass` and related types (e.g. `DisplayData` with quality tiers, `LevelRequirements`, `GetRequirementsForLevel`). This mapping will be validated against the runtime API (ProgressionValue, ProgressionClass, QualityInfo, etc.). See `docs/GAME_API_NOTES.md` for patch targets and types. Progression lookup names for crafting skills (e.g. craftingarmor) are in GAME_API_NOTES and in 7dtd-mod-dev-tools (e.g. PROGRESSION_NAMES.md).

## Restriction points

### Armor

Prevent placing items into player armor/equipment slots. Block at **XUiM_PlayerEquipment.EquipItem** (click-to-equip) and at **XUiC_EquipmentStack.HandleStackSwap** (drag-drop onto equipment slot). When blocking drag-drop the item remains on the cursor (we block before the move). Optionally show feedback.

### Handheld (weapons and tools)

**Preferred approach:** Prevent adding the item to the hotbar. All code paths that put an item into a hotbar slot must be blocked (e.g. intercept `Inventory.SetItem` / `SetSlot` when the destination index is a hotbar slot). If the item cannot be in the hotbar, the player cannot equip it to their hand. Block at **XUiC_ItemStack.HandleStackSwap** when the drop target is a toolbelt slot (same pattern as equipment drag-drop); the item remains on the cursor when blocked. Also block at **XUiC_ItemStack.HandleMoveToPreferredLocation** when the source is the player backpack (move to toolbelt) and the item is restricted. Block at **XUiM_PlayerInventory.AddItemToToolbelt** and **AddItemToPreferredToolbeltSlot** when the player uses the equip key (e.g. W) to add an item to the hotbar.

**Rejected alternatives (documented only):**

- **Prevent setting active slot:** Would make controls less predictable; discarded.
- **Allow equipping but prevent abilities:** Would require checking on every use (expensive) and could confuse the player when nothing happens; documented as considered.

### Workstations

When the player attempts to open the placed workstation UI (e.g. default keybind 'E'), check their crafting level for the Workstations skill against the placed block's required level. If below, block opening the interface and show feedback. Picking up and other interactions remain allowed.

Workstation modifiers (e.g. Bellows, Crucible) that are not placed in the world are not restricted in the initial implementation (future work).

### Vehicles

Only restrict the **drive** action. Allow: open vehicle inventory, refuel, pick up, passenger seats. Map vehicle entity/block to required level via the **Vehicles** crafting skill. Vehicle types are mapped to crafting components by the chassis heuristic: e.g. look up component by vehicle name + `" Chassis"` (e.g. "Bicycle Chassis" for "Bicycle"). Avoid hardcoding vehicle names; use a data-driven or name-based lookup so new/renamed vehicles work.

### Traps and robotics

Treated as handheld: if the item cannot be placed in the hotbar, it cannot be held and thus cannot be placed in the world. No separate restriction path.

## Visualization

### Inventory

Indicate restricted items by coloring the in-inventory item **label red** wherever the player sees items: player inventory (backpack, toolbelt), character equipment slots, container/loot inventories, vehicle inventory, workstation inventory, and any other UI that displays item stacks or equipment. The same rule applies for all restricted item types (armor, tools, workstations, vehicles); the only difference is which grid type the game uses (item-stack grid vs equipment-stack grid). Grids are detected by **base type** (`XUiC_ItemStackGrid` / `XUiC_EquipmentStackGrid`) so all subclasses (e.g. Backpack, Toolbelt, PartList, VehicleContainer, WorkstationGrid) are covered.

**Triggers:** (1) **OnOpen** Postfix on `XUiC_ItemStackGrid` and `XUiC_EquipmentStackGrid` calls `ApplyRestrictionColorsToGrid` once. (2) When the player levels up a crafting skill, **Progression.addProgressionCurrency** Postfix sets a dirty flag. (3) Each grid’s **Update** Postfix, when the grid is open, runs `ApplyRestrictionColorsToGrid` only if the dirty flag is set or a per-grid throttle has elapsed (e.g. every 0.2s per open grid), then clears the dirty flag. Immediate apply on open and when dirty keeps labels correct after open/level-up; throttling the steady-state refresh limits compute. The throttle interval can be tuned if the game overwrites label color more frequently.

**Label application:** Restriction check is shared (`RestrictionHelper.IsItemRestricted`). For item grids we use `SetLabelColorOnEntry(go)` on the slot controller’s view GameObject (ViewComponent → uiTransform → gameObject) and its children so the visible item name label is colored; for equipment we use the grid’s `items` array or player equipment fallback and the same label-setting approach. Label color is set via reflection on UILabel `color`/`mColor`. Grey overlay or "X" on the sprite are documented as future work if needed.

### In-world (workstation, vehicle, etc.)

When a blocked use occurs, show a **red** popup using the game's existing popup/notification system with:

1. "You don't know how to use [item name]"
2. "[Crafting Skill Name] [player level]/[required level]"

The exact API for this popup is to be identified in the game API investigation.

## Config

- **Per–crafting-skill toggles:** One toggle per crafting skill (e.g. Armor, HarvestingTools, Workstations, Vehicles). When enabled, restriction applies for that skill; when disabled, items for that skill are not restricted.
- **Server vs client:** In multiplayer, restrictions must respect the **server's** config, not the client's. Where config is read (server vs client process) and how server authority is enforced is documented here and implemented when the game API for that is clear. Initial implementation can be client-only with a note to add server path later.

## Optional (document only, no implementation)

- **Prevent acquisition:** See `docs/OPTIONAL_PREVENT_ACQUISITION.md`.
- **Level restriction offset:** See `docs/OPTIONAL_LEVEL_OFFSET.md`.
