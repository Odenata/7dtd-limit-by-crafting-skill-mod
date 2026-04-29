# Limit by Crafting Skill

Limit by Crafting Skill slows progression in **7 Days to Die** by restricting use of items based on the various crafting skills. This keeps early high-tier loot, quest rewards, and gifts from other players from bypassing crafting progression.

## What it changes

- Armor and clothing can be blocked until the player's **Armor** skill is high enough.
- Weapons, tools, traps, robotics, and placeable items can be blocked from the hotbar until the relevant crafting skill is high enough.
- Workstation blocks can be blocked until the player's **Workstations** skill is high enough.
- Vehicles can still be opened, refueled, picked up, and used as a passenger, but driving can be blocked until the player's **Vehicles** skill is high enough.
- Food and Medical restrictions are available, but are turned off by default.
- Upgrade-style items are intentionally not gated by default. Examples include workstation upgrades such as Crucible or Bellows, vehicle modifiers such as extra seats, weapon attachments such as scopes, and batteries installed into battery banks.

## Configuration

The mod includes a `Config.xml` file. Each entry under `<CraftingSkills>` turns one restriction category on or off.

Example:

```xml
<Vehicles>true</Vehicles>
```

Set a category to `false` to disable that category's restrictions. `Food` and `Medical` are shipped as `false`, so eating, drinking, and medicine behave like vanilla unless you opt in. Feel free to customize your experience by limiting only certain categories to achieve the balance that matches your desired playstyle.

Users can also edit `ClassNameToCraftingSkillMap.xml` to adjust item-to-skill mappings or add explicit required-level overrides, useful if you want to loosen or tighten restrictions for specific items.

## Multiplayer

This mod can be run on servers! The server's config will be used, so individual players do not need to make adjustments.

Install the same mod version on the dedicated server and every client.

Because this is a Harmony / DLL mod, **EAC must be disabled** for modded servers. Unfortunately that means crossplay is not supported.

## Game version compatibility

Archives are named by **mod version**. Not every game update requires a new mod build. For which **7 Days to Die** versions match each mod version, see the compatibility table in the repository:

https://github.com/Odenata/7dtd-limit-by-crafting-skill-mod/blob/main/docs/COMPATIBILITY.md

## Source and contributions

Source, development notes, and issue tracking are available on GitHub:

https://github.com/Odenata/7dtd-limit-by-crafting-skill-mod
