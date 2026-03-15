# Game API notes for Limit by Crafting Skill Mod

Use the [Runtime API Workflow](https://github.com/Odenata/7dtd-mod-dev-tools/blob/main/docs/RUNTIME_API_WORKFLOW.md) in 7dtd-mod-dev-tools: start from checked-in docs in `docs/game-api/`, then query or refresh the runtime export as needed. When the mod compiles but fails in-game, follow [RUNTIME_API_MISMATCH_DEBUGGING.md](https://github.com/Odenata/7dtd-mod-dev-tools/blob/main/docs/RUNTIME_API_MISMATCH_DEBUGGING.md).

## Player crafting level

- **Goal:** Given an entity (EntityPlayerLocal / EntityAlive) and a crafting skill name (string, e.g. from `ItemClass.CraftingSkillGroup`), obtain the player's current level.
- **Types:** `ProgressionValue` has `Level` (int). `ProgressionClass` has `Name`, `GetRequirementsForLevel`, `DisplayDataList` (quality tiers).
- **To confirm:** How to get the player's ProgressionValue for a skill name. Likely: a manager (e.g. ProgressionManager) or a method on the entity that returns ProgressionValue by name. Query runtime for `ProgressionValue`, `ProgressionClass`, and entity/manager methods that take a skill name and return level or ProgressionValue.

## Item required level

- **Goal:** Given `ItemClass` + `ItemValue` (with Quality), compute the minimum crafting level required to use that item.
- **Known:** `ItemClass.CraftingSkillGroup` (string). `ItemValue.Quality` (UInt16). `ItemValue.HasQuality` (property).
- **To confirm:** Mapping from (CraftingSkillGroup, Quality) to required level. Likely via progression/recipe data: `ProgressionClass.DisplayDataList` with quality tiers, or `LevelRequirement` / `GetRequirementsForLevel(level)`. Query runtime for how the game decides "player can craft quality X at level Y".

## Inventory and slots

- **Goal:** Identify hotbar slot indices and armor/equipment slot indices so we can block only those.
- **Known (mocks):** `Inventory.GetSlots()`, `SetItem(idx, stack)`, `SetSlots(...)`. `Bag.GetSlots()`, `SetSlot(idx, stack, callChanged)`.
- **To confirm:** Which indices are hotbar vs bag vs armor. Whether armor is on the same inventory or a separate structure (e.g. equipment slots on EntityPlayer). Single best choke point to intercept: e.g. one method that runs whenever an item is set into a hotbar or armor slot. Query: `Inventory`, `Bag`, slot layout constants or documentation.

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

## Patch targets (to be filled after investigation)

| Area       | Target type/method (candidate)     | Notes                    |
|-----------|------------------------------------|--------------------------|
| Armor     | TBD (e.g. SetSlot when slot is armor) | Single choke point       |
| Hotbar    | TBD (e.g. Inventory.SetItem when idx in hotbar range) | All paths to hotbar      |
| Workstation | TBD (open UI)                    | Block open only          |
| Vehicle   | TBD (drive action)                | Allow inventory/refuel   |
| Popup     | TBD (notification/popup API)     | Red text, two lines      |

After confirming names and overloads in the runtime export, update this table and the compat layer (GameReflection or equivalent) in the mod. Prefer a small compat helper and reflection for game-version resilience; see RUNTIME_API_MISMATCH_DEBUGGING.md.
