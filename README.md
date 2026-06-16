# Limit by Crafting Skill Mod

*Limit by Crafting Skill* restricts the use of craftable items based on each player's crafting skill levels. If your skill level is below an item's required crafting level, you will be blocked from using it.

Consider this mod if you find vanilla progression to be fast, especially when driven by high-tier loot or rewards too early. It also prevents some over-helping from friends who might have supplied such loot rather than letting the player progress on their own.

## Game version compatibility

Release downloads are named by **mod version**; the same zip may work across multiple game patches. See **[docs/COMPATIBILITY.md](docs/COMPATIBILITY.md)** for which **7 Days to Die** versions go with which mod release.

## What It Restricts

- Armor and clothing cannot be equipped until your Armor skill is high enough.
- Weapons, tools, traps, robotics, and placeable items cannot be put on the hotbar when restricted.
- Workstation blocks cannot be opened until your Workstations skill is high enough.
- Vehicles can still be opened, refueled, picked up, and used as a passenger, but cannot be driven until your Vehicles skill is high enough.
- Food and medical restrictions are available, but off by default.
- Upgrade-style items are intentionally not gated by default. Examples include workstation upgrades such as Crucible or Bellows, vehicle modifiers such as extra seats, weapon attachments such as scopes, and batteries installed into battery banks.

## Installation

Install the mod into your 7 Days to Die game folder:

```text
<7 Days To Die>\Mods\LimitByCraftingSkillMod\
```

That folder must contain these files:

- `LimitByCraftingSkillMod.dll`
- `0Harmony.dll`
- `ModInfo.xml`
- `Config.xml`
- `ClassNameToCraftingSkillMap.xml`

If you received a prepared mod folder or release zip, copy all of those files into `Mods\LimitByCraftingSkillMod\`. Create the `Mods` folder if your game install does not already have one.

Restart 7 Days to Die after installing or replacing mod files.

## Configuration

Edit `Config.xml` in the installed mod folder, then restart the game.

Each entry under `<CraftingSkills>` turns one restriction category on or off:

```xml
<Vehicles>true</Vehicles>
```

Set a category to `false` to disable that category's restrictions. `Food` and `Medical` are shipped as `false`, so eating, drinking, and medicine behave like vanilla unless you opt in. Feel free to customize your experience by limiting only certain categories to achieve the balance that matches your desired playstyle.

Set `<DebugMode>true</DebugMode>` only while troubleshooting. It writes extra mod messages to the game log.

## Multiplayer

Install the same mod version on the dedicated server and every client. Because this is a Harmony / DLL mod, EAC must be disabled for the server.

When a client joins a server running this mod, the server sends its `Config.xml` to that client and the client uses those crafting-skill toggles for restriction checks. The local client `Config.xml` still applies in single-player or on servers that do not send this sync package. Keep `ClassNameToCraftingSkillMap.xml` aligned manually across server and clients.

## Updating

Replace every file in:

```text
<7 Days To Die>\Mods\LimitByCraftingSkillMod\
```

If you customized `Config.xml` or `ClassNameToCraftingSkillMap.xml`, back those files up first and re-apply your changes after updating.

## Uninstalling

Delete this folder:

```text
<7 Days To Die>\Mods\LimitByCraftingSkillMod\
```

Then restart the game.

## Troubleshooting

- If a restriction does not appear to apply, confirm the mod folder contains all five required files above.
- If you changed `Config.xml`, restart the game before testing.
- If a server is involved, confirm the client log reports that server config sync was applied. Also confirm server and clients have the same mod version and `ClassNameToCraftingSkillMap.xml`.
- For maintainer-level troubleshooting, see `docs/DEBUG_INSTRUMENTATION.md`.

## For Developers

Build, test, deployment, map maintenance, and design notes live under `docs/`. Start with `docs/DEVELOPING.md`.
