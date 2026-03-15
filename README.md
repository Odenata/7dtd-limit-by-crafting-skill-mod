# Limit by Crafting Skill Mod

Restricts equipping and using items to the player's crafting skill level for that item type. See [LootProgressionByCraftingSkillModIdea.md](LootProgressionByCraftingSkillModIdea.md) and [docs/DESIGN.md](docs/DESIGN.md) for design.

## Build

- **Bazel (hermetic):** `bazel build //src:LimitByCraftingSkillMod`
- **Local (IDE/deploy):** `tools\build.ps1` or call dev-tools build from 7dtd-mod-dev-tools

## Test

- **Bazel:** `bazel test //tests:all` (on Windows may require `BAZEL_SH` set to bash for test runner; see 7dtd-mod-dev-tools docs).
- **dotnet:** From repo root with 7dtd-mod-dev-tools as sibling: `dotnet test tests\LimitByCraftingSkillMod.Tests.csproj`.
- **In-game:** After deploy, use [docs/IN_GAME_TEST_CHECKLIST.md](docs/IN_GAME_TEST_CHECKLIST.md).
