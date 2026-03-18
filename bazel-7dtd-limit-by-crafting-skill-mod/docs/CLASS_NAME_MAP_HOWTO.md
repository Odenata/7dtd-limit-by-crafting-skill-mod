# Class name to crafting skill map — how to maintain

The mod restricts items by **crafting skill**. It resolves the skill in this order:

1. **ClassNameToCraftingSkillMap.xml** — lookup by item name (`ItemClass.Name`, case-insensitive). Map entry wins if present.
2. **Game `ItemClass.CraftingSkillGroup`** — only when **unmapped**, and only if the value is **Electrician**, **Workstations**, **HarvestingTools**, or **Tools** (treated as HarvestingTools). This covers wire tools, picks, shovels, etc. that the game tags but are not listed in the XML.

If neither applies, **the item is not restricted** by this mod for handheld/hotbar logic.

### `progressionMatchName` (optional)

`ItemClass.Name` and the name on the **Electrician / Workstations** progression unlock row sometimes differ (e.g. placeable `ironGarageDoor_PoweredWhite` vs progression row `ironGarageDoor01_PoweredWhite`). If `GetRequiredLevelForItem` logs `no_progression_match` for a mapped item, add:

```xml
<Item className="ironGarageDoor_PoweredWhite" craftingSkillGroup="Electrician"
      progressionMatchName="ironGarageDoor01_PoweredWhite"/>
```

Progression lookup uses `progressionMatchName` when set; the map key stays `className`.

**Name vs level:** `progressionMatchName` only helps **find** the right progression row. If the mod still does not restrict the item, check whether the resolved **required level is 0**: restriction applies only when **`requiredLevel > 0`**. The first matching row may use `QualityStarts[0] == 0` (vanilla “free” tier) while a later row has a higher tier—for **powered iron garage** placeables (`ironGarageDoor_*Powered*`), the mod uses the **maximum** level across matching rows in the Electrician tree. If you still see no gate, use **`requiredLevelMin`** or **`requiredLevelOverride`** below.

### Electrician items crafted via workbench (e.g. powered garage doors)

Some items are tagged **Electrician** but their **unlock tier lives under `craftingworkstations`** in vanilla. The mod tries **Electrician** first, then **`craftingworkstations`**, while still comparing the player’s **Electrician** level to that required tier.

### `requiredLevelOverride` (optional)

Use when progression resolves to **no positive requirement** (no match, **or** match at tier **0**). The mod applies the override whenever the computed required level is **≤ 0**. For “raise a level that already resolved” (e.g. vanilla says 10 but you want 25), use **`requiredLevelMin`** instead.

Set a fixed gate when needed:

```xml
<Item className="ironGarageDoor_PoweredWhite" craftingSkillGroup="Electrician" requiredLevelOverride="40"/>
```

### `requiredLevelMin` (optional)

After progression resolves a required level, the mod uses **max(vanillaResolved, min)** when set:

```xml
<Item className="meleeToolAxeT2SteelFireaxe" craftingSkillGroup="HarvestingTools" requiredLevelMin="25"/>
```

Use for verification or stricter floors. Unlike `requiredLevelOverride`, this does **not** replace a successful progression match—it only raises the bar.

This doc explains how to edit the map when the game is updated or when you add mods.

**Class name vs display:** Use **`ItemClass.Name`** (e.g. from DevInspector), not the localized item display name. The same axe may be `meleeToolAxeT2SteelAxe` in one game version and `meleeToolAxeT2SteelFireaxe` in another—add both `className` entries if needed.

## Where the map lives

- **In the repo:** `src/ClassNameToCraftingSkillMap.xml`
- **Generated reference (sorted by group / name similarity):** `src/ClassNameToCraftingSkillMap_generated.xml` — refresh with `bazel run //tools:generate_classname_map_report` **including `--progression …/progression.xml`** for suggested `progressionMatchName` rows (see `tools/README_MAP_GENERATOR.md`). Carries hand-map overrides when present; **never** auto-fills `requiredLevelOverride`.
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
