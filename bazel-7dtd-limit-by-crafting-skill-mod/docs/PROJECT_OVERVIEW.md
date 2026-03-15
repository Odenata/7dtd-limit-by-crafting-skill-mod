# Project overview: Limit by Crafting Skill Mod

**Repo:** `7dtd-limit-by-crafting-skill-mod`  
**Mod name:** LimitByCraftingSkillMod

## Goal

Slow progression and make crafting skills more meaningful by restricting equipping and use of items to the player's crafting level for that item type. A player cannot equip or use any item whose required level (by quality/tier and crafting skill) exceeds their current level in the relevant skill.

## Approach

- **Restriction points:** Armor slots, hotbar (handheld), workstation UI open, vehicle drive. Traps/robotics follow handheld rules.
- **Logic:** Pure function `LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, configEnabled)`. Game-facing layer uses reflection/compat to get player level and item required level, then calls this logic.
- **Config:** One toggle per crafting skill; server config overrides client in multiplayer.
- **Visualization:** Inventory label color for restricted items; red popup when blocking in-world use.

## Repo layout

- **src/:** Mod DLL (ModApi, ModConfig, LimitByCraftingSkillLogic, patches, GameReflection, visualization).
- **tests/:** Hermetic unit tests (logic, config).
- **tools/:** build.ps1, deploy.ps1 (call 7dtd-mod-dev-tools build-deploy).
- **docs/:** DESIGN.md, GAME_API_NOTES.md, optional docs, in-game checklist.

## References

- High-level idea: [LootProgressionByCraftingSkillModIdea.md](../LootProgressionByCraftingSkillModIdea.md)
- Implementation design: [docs/DESIGN.md](DESIGN.md)
- Game API and patch targets: [docs/GAME_API_NOTES.md](GAME_API_NOTES.md)
- Sister repos: 7dtd-mod-dev-tools, 7dtd-auto-read-mod (Bazel, Harmony, mocks).
