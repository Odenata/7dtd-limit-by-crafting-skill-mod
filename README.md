# Limit by Crafting Skill Mod

Restricts equipping and using items to the player's crafting skill level for that item type. See [LootProgressionByCraftingSkillModIdea.md](LootProgressionByCraftingSkillModIdea.md) and [docs/DESIGN.md](docs/DESIGN.md) for design. **Open work:** [docs/TODO.md](docs/TODO.md).

## Build

- **Bazel (hermetic):** `bazel build //src:LimitByCraftingSkillMod`
- **Bazel run (non-hermetic, uses host dotnet):** `bazel run //tools:build`
- **Local (IDE/deploy):** `.\tools\build.ps1` — requires `7dtd-mod-dev-tools` as a sibling repo (e.g. `repos\7dtd-mod-dev-tools`).

## Test

- **Bazel:** `bazel test //tests:all` (on Windows may require `BAZEL_SH` set to bash for test runner; see 7dtd-mod-dev-tools docs).
- **dotnet:** From repo root with 7dtd-mod-dev-tools as sibling: `dotnet test tests\LimitByCraftingSkillMod.Tests.csproj`.
- **In-game:** After deploy, use [docs/IN_GAME_TEST_CHECKLIST.md](docs/IN_GAME_TEST_CHECKLIST.md).

## Debugging required-level issues

To trace why an item shows required level **0** or wrong tier after a game update, see **[docs/DEBUG_INSTRUMENTATION.md](docs/DEBUG_INSTRUMENTATION.md)** (NDJSON log helpers and where to hook `GetRequiredLevelForItem`).

## Deploy

- **Bazel run:** `bazel run //tools:deploy` (builds if DLL missing, then copies to game `Mods` folder).
- **Local:** `.\tools\deploy.ps1` — optional `$env:7_DAYS_TO_DIE_GAME_PATH` for game install dir.
