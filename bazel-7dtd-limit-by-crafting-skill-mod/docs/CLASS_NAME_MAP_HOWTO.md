# Class name to crafting skill map — how to maintain

The mod restricts items by **crafting skill**. It decides which skill an item uses by looking up the item's **map key** in **ClassNameToCraftingSkillMap.xml**. The key is the **item name** (from the game's item definition, e.g. `gunBowT0PrimitiveBow`), as shown by the 7dtd-dev-inspector-mod; the mod uses `ItemClass.Name` with `GetType().Name` as fallback. If the file is missing or the key has no entry, **the item is not restricted**.

This doc explains how to edit the map when the game is updated or when you add mods.

## Where the map lives

- **In the repo:** `src/ClassNameToCraftingSkillMap.xml`
- **At runtime:** The file must be in the **mod folder** next to the mod DLL and `Config.xml` (same folder the game loads the mod from). The build/deploy process copies it there.

## When to edit the map

- **Game update:** New or renamed item classes may appear; add or update entries for them.
- **New mods:** Mod-added items have class names that won't be in the map; add entries by hand.
- **Wrong mapping:** If an item is restricted by the wrong skill (or not restricted when it should be), edit the map and fix the `craftingSkillGroup` for that `className`.

## Editing the map

1. Open `ClassNameToCraftingSkillMap.xml` in the mod folder (after deploy) or in `src/` in the repo (before building).
2. Add, remove, or change `<Item className="..." craftingSkillGroup="..." />` entries as needed.
3. Save the file. If you edited in `src/`, rebuild and deploy the mod so the updated XML is in the game's mod folder.

## How to find the map key (className)

- **7dtd-dev-inspector-mod:** Hover over an item in-game; the inspector shows the item identifier used for lookup (the item name from the game's XML, same as `ItemClass.Name`). Use that value as `className` in the map.

## Valid `craftingSkillGroup` values

The value must be a **game-reported name** that the mod already understands for level lookup and config. Use the names from the "Game-reported name" column in [CRAFTING_SKILL_MAPPINGS.md](CRAFTING_SKILL_MAPPINGS.md), for example:

- **Bows**, **Rifles**, **Shotguns**, **Handguns**, **MachineGuns**
- **Clubs**, **Spears**, **Sledgehammers**, **Blades**, **Knuckles**
- **Medical**, **Explosives**, **Robotics**, **Traps**, **Seeds**
- **HarvestingTools**, **RepairTools**, **SalvageTools**
- **Clothing** (for armor)
- **Electrician**

Do not use progression lookup names (e.g. `craftingbows`); use the game-reported name (e.g. `Bows`).

## Adding a single new item

1. Open `src/ClassNameToCraftingSkillMap.xml` (or the map in the mod folder).
2. Copy an existing `<Item className="..." craftingSkillGroup="..." />` line.
3. Change `className` to the new item's class name and `craftingSkillGroup` to the correct skill (see list above).
4. Save and rebuild/deploy if you edited in the repo.

## XML format

```xml
<ClassNameToCraftingSkillMap>
  <Item className="gunBowT0PrimitiveBow" craftingSkillGroup="Bows" />
  <Item className="medicalFirstAidBandage" craftingSkillGroup="Medical" />
</ClassNameToCraftingSkillMap>
```

- **className:** Item name from the game's item definition (as shown by 7dtd-dev-inspector-mod; the mod looks up using `ItemClass.Name` then falls back to `GetType().Name`).
- **craftingSkillGroup:** Game-reported name (e.g. Bows, Medical, Clothing). Must be one of the names the mod maps to a progression and config key; see CRAFTING_SKILL_MAPPINGS.md.

For optional future ideas (e.g. heuristic-based generation), see [CLASS_NAME_MAP_FUTURE_IDEAS.md](CLASS_NAME_MAP_FUTURE_IDEAS.md).
