# Design: Limit by Crafting Skill Mod

## Core rule

The player may not equip or use an item if the item's **required level** (for its CraftingSkillGroup and quality/tier) is greater than the player's current level in that crafting skill.

## Item "required level"

- **Source:** Each item has `ItemClass.CraftingSkillGroup` (string) and optionally a quality tier. Quality is exposed as `ItemValue.Quality` (e.g. 1–5). Items without quality are treated as requiring a single tier (e.g. required level 0 or 1 — no restriction, or a minimal level to be validated against the game's progression data).
- **Derivation:** Required level is derived from the game's progression/recipe data. The game uses `ProgressionClass` and related types (e.g. `DisplayData` with quality tiers, `LevelRequirements`, `GetRequirementsForLevel`). This mapping will be validated against the runtime API (ProgressionValue, ProgressionClass, QualityInfo, etc.). See `docs/GAME_API_NOTES.md` for patch targets and types.

## Restriction points

### Armor

Prevent placing items into player armor/equipment slots. Use a single choke point: e.g. inventory/bag `SetSlot` or equivalent when the destination is an armor slot. Block the operation and optionally show feedback.

### Handheld (weapons and tools)

**Preferred approach:** Prevent adding the item to the hotbar. All code paths that put an item into a hotbar slot must be blocked (e.g. intercept `Inventory.SetItem` / `SetSlot` when the destination index is a hotbar slot). If the item cannot be in the hotbar, the player cannot equip it to their hand.

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

Indicate restricted items in inventory (e.g. recolor the item label). Grey overlay or "X" on the sprite are documented as future work if the primary option is insufficient.

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
