# Project overview: Limit by Crafting Skill Mod

**Repo:** `7dtd-limit-by-crafting-skill-mod`
**Mod name:** LimitByCraftingSkillMod

## Goal

Slow progression and make crafting skills more meaningful by restricting equipping and use of items to the player's crafting level for that item type. A player cannot equip or use any item whose required level (by quality/tier and crafting skill) exceeds their current level in the relevant skill.

## Approach

- **Restriction points:** Armor slots, hotbar (handheld), workstation UI open, vehicle drive. Traps/robotics follow handheld rules. **Food** and **Medical** (eat/drink/meds via `ItemActionEat`) are **implemented** and **opt-in** — `Config.xml` defaults them to **false**; see the **Food and Medicine** section in [DESIGN.md](DESIGN.md).
- **Logic:** Pure function `LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, configEnabled)`. Game-facing layer uses reflection/compat to get player level and item required level, then calls this logic.
- **Config:** One toggle per crafting skill. The MVP reads `Config.xml` from the local mod folder; server-authoritative config sync is not implemented.
- **Visualization:** Inventory label color for restricted items; red popup when blocking in-world use.

## Repo layout

- **src/:** Mod DLL (ModApi, ModConfig, LimitByCraftingSkillLogic, patches, GameReflection, visualization).
- **tests/:** Hermetic unit tests for core logic, config, reflection/progression resolution, and map-generator helpers. Optional integration tests can consume local game XML / assemblies when environment variables are set.
- **tools/:** build, prepare, deploy, and map-generation helpers.
- **docs/:** Design, game API notes, map maintenance, developer workflow, and optional docs.

## References

- Implementation design: [docs/DESIGN.md](DESIGN.md)
- Game API and patch targets: [docs/GAME_API_NOTES.md](GAME_API_NOTES.md)
- Developer workflow: [docs/DEVELOPING.md](DEVELOPING.md)
- Sister repos: 7dtd-mod-dev-tools, 7dtd-dev-inspector-mod, 7dtd-auto-read-mod (Bazel, Harmony, mocks). These may not all be public yet. Please reach out to the author if you need these, to nudge them to publish.
