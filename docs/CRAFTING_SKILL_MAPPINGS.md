# Crafting skill name mappings (implemented)

This document shows how **game-reported item group names** (from `ItemClass.CraftingSkillGroup` or similar) map to **config keys** and **progression lookup names** used by the mod. Use it to spot mismatches when the game uses a name we don’t yet map.

### How we get the crafting skill for an item (restriction)

The mod uses **only** **ClassNameToCraftingSkillMap.xml** to decide which crafting skill an item uses for restriction. It looks up the item's map key (ItemClass.Name / item name from XML, with GetType().Name fallback) in that file. If the file is missing or the key has no mapping, **the item is not restricted**. There is no game API or heuristic at runtime.

To update or extend the map (e.g. after a game update or for mod-added items), see **[CLASS_NAME_MAP_HOWTO.md](CLASS_NAME_MAP_HOWTO.md)**.

---

## 1. Config name (Config.xml)

**Config key** = value used in `Config.xml` under `<CraftingSkills>` and in `IsRestrictionEnabledForSkill()`.

| Game-reported name (item group) | Config key |
|---------------------------------|------------|
| Clothing | **Armor** |
| Tools | **HarvestingTools** |
| Ammo | **Weapons** |
| Weapons | **Weapons** |
| *any other* | *unchanged* (e.g. HarvestingTools → HarvestingTools, Traps → Traps) |

---

## 2. Progression lookup name (level lookup)

**Progression name** = key passed to `Progression.GetProgressionValue()` to read the player’s level.  
If the mod can’t map a name, it falls back to `"crafting" + lowercase(group).Replace(" ", "")` (e.g. `"SomeGroup"` → `"craftingsomegroup"`).

### Single-group (one progression key)

| Game-reported name | Progression lookup |
|---------------------|--------------------|
| Clothing | craftingarmor |
| HarvestingTools, Harvesting Tool(s) | craftingharvestingtools |
| Bows, Bow | craftingbows |
| RepairTools | craftingrepairtools |
| SalvageTools | craftingsalvagetools |
| Clubs | craftingclubs |
| Sledgehammers | craftingsledgehammers |
| Spears | craftingspears |
| Handguns | craftinghandguns |
| Shotguns | craftingshotguns |
| Rifles | craftingrifles |
| MachineGuns | craftingmachineguns |
| Explosives | craftingexplosives |
| Robotics | craftingrobotics |
| Medical | craftingmedical |
| Food | craftingfood |
| Seeds | craftingseeds |
| Traps | craftingtraps |
| Tools | craftingharvestingtools |
| Workstations | craftingworkstations |
| Vehicles | craftingvehicles |
| Blades | craftingblades |
| Knuckles | craftingknuckles |
| Electrician | craftingelectrician |

---

## 3. Summary by “item type” (what you might see in-game)

| Item type (typical) | Game group we’ve seen | Config | Level used |
|----------------------|------------------------|--------|------------|
| Armor / clothing | Clothing | Armor | craftingarmor |
| Bows, guns, ammo | Ammo/Weapons | Weapons | max(ammo, weapons); weapons = max(bows, handguns, shotguns, rifles, machineguns) |
| Harvesting tools (e.g. shovel, axe) | Tools/Traps or Tools | HarvestingTools / Traps | Tools/Traps: min(tools, traps); Tools: craftingharvestingtools |
| Traps | Tools/Traps or Traps | Traps | Tools/Traps: min(tools, traps); Traps: craftingtraps |
| Bows only (if game ever sent “Bows”) | Bows | Bows | craftingbows |

---

## 4. Where this is implemented

- **Config name:** `GameReflection.ToProgressionOrConfigName()`
- **Progression lookup:** `GameReflection.ToProgressionLookupName()`
- **Player level:** `GameReflection.GetPlayerCraftingLevel(entity, craftingSkillGroup)` (single group only)
- **Restriction enabled:** `RestrictionHelper.IsRestrictionEnabledForSkillGroup()` (single config key)

If you see a **game-reported name** in logs (e.g. from the `IsItemRestricted: skillGroup="..."` debug line) that isn’t in the tables above, add a mapping for it in `GameReflection.cs` and optionally in `7dtd-mod-dev-tools` `docs/PROGRESSION_NAMES.md`.
