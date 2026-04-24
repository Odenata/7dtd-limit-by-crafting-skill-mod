# Limit by Crafting Skill Mod

Restricts equipping and using items to the player's crafting skill level for that item type. See [LootProgressionByCraftingSkillModIdea.md](LootProgressionByCraftingSkillModIdea.md) and [docs/DESIGN.md](docs/DESIGN.md) for design. **Open work:** [docs/TODO.md](docs/TODO.md). **Map maintenance:** [tools/README_MAP_GENERATOR.md](tools/README_MAP_GENERATOR.md).

## Build

Sources live at the **repository root** (`src/`, `tests/`). Do not commit or edit files under a nested `bazel-*` execroot mirror.

- **Bazel (hermetic):** `bazel build //src:LimitByCraftingSkillMod` — output DLL under `bazel-bin\src\LimitByCraftingSkillMod.dll` (not `src\bin`).
- **Bazel run (non-hermetic, uses host dotnet):** `bazel run //tools:build`
- **Local (IDE):** `.\tools\build.ps1` — requires `7dtd-mod-dev-tools` as a sibling repo (e.g. `repos\7dtd-mod-dev-tools`). MSBuild outputs go to `src\bin\` (gitignored).

## Prepare mod files (for players or manual copy)

`.\tools\prepare_mod.ps1` fills **`prepared_mod_files\`** with exactly what belongs in `Mods\LimitByCraftingSkillMod\` (DLL, `0Harmony.dll`, `ModInfo.xml`, `Config.xml`, `ClassNameToCraftingSkillMap.xml`). Use **`-DllPath`** if you built with Bazel (point at `bazel-bin\src\LimitByCraftingSkillMod.dll`). See [`prepared_mod_files/README.md`](prepared_mod_files/README.md).

- **Bazel:** `bazel run //tools:prepare_mod` (same script via `prepare_run.ps1`).

## ClassName map generator (hermetic Python)

- **Run:** `bazel run //tools:generate_classname_map_report -- --items <game>\Data\Config\items.xml --map src/ClassNameToCraftingSkillMap.xml --out-csv … --out-generated-xml src/ClassNameToCraftingSkillMap_generated.xml`
- **Details:** [tools/README_MAP_GENERATOR.md](tools/README_MAP_GENERATOR.md)

## Test

- **Bazel:** `bazel test //tests:all` (on Windows may require `BAZEL_SH` set to bash for test runner; see 7dtd-mod-dev-tools docs).
- **dotnet:** From repo root with 7dtd-mod-dev-tools as sibling: `dotnet test tests\LimitByCraftingSkillMod.Tests.csproj`.
- **In-game:** After deploy, use [docs/IN_GAME_TEST_CHECKLIST.md](docs/IN_GAME_TEST_CHECKLIST.md). Optional: [docs/LOCAL_GAME_HARNESS.md](docs/LOCAL_GAME_HARNESS.md) (live progression / future standalone harness).

## Debugging required-level issues

To trace why an item shows required level **0** or wrong tier after a game update, see **[docs/DEBUG_INSTRUMENTATION.md](docs/DEBUG_INSTRUMENTATION.md)** (NDJSON log helpers and where to hook `GetRequiredLevelForItem`).

## Deploy

Deploy **always builds** (Release by default), runs **`prepare_mod.ps1`**, then copies everything from **`prepared_mod_files\`** into `<game>\Mods\LimitByCraftingSkillMod\`. This avoids stale DLLs and missing `ClassNameToCraftingSkillMap.xml`.

- **Game install directory:** `SEVENDTD_GAME_PATH` or `7_DAYS_TO_DIE_GAME_PATH`, else the default Steam path.
- **Local:** `.\tools\deploy.ps1` — optional `-SkipBuild` if you already built; optional `-DllPath` for a Bazel-built DLL.
- **Bazel:** `MSYS2_ARG_CONV_EXCL='*' bazel run tools:deploy` from Git Bash (MSYS strips `//` in `//tools:deploy` unless you use this env or run from **cmd/PowerShell** as `bazel run //tools:deploy`).
