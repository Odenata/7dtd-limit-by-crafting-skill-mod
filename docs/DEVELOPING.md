# Developer workflow

This document is for maintainers building, testing, deploying, or updating the map from source. Player installation and configuration instructions are in the top-level [`README.md`](../README.md).

## Source tree

Sources live at the repository root (`src/`, `tests/`). Do not commit or edit files under a nested `bazel-*` execroot mirror.

## Build

- **Bazel (hermetic):** `bazel build //src:LimitByCraftingSkillMod` — output DLL under `bazel-bin\src\LimitByCraftingSkillMod.dll`.
- **Bazel run (non-hermetic, uses host dotnet):** `bazel run //tools:build`.
- **Local IDE / PowerShell:** `.\tools\build.ps1` — requires `7dtd-mod-dev-tools` as a sibling repo (for example, `repos\7dtd-mod-dev-tools`). MSBuild outputs go to `src\bin\` (gitignored).

## Prepare mod files

`.\tools\prepare_mod.ps1` fills `prepared_mod_files\` with exactly what belongs in `Mods\LimitByCraftingSkillMod\`:

- `LimitByCraftingSkillMod.dll`
- `0Harmony.dll`
- `ModInfo.xml`
- `Config.xml`
- `ClassNameToCraftingSkillMap.xml`

Use `-DllPath` if you built with Bazel, pointing at `bazel-bin\src\LimitByCraftingSkillMod.dll`.

Equivalent Bazel entry point:

```bash
bazel run //tools:prepare_mod
```

## Package a release zip

`.\tools\package_release.ps1` creates a CurseForge/GitHub release archive under `dist\`:

```powershell
.\tools\package_release.ps1 -DllPath .\bazel-bin\src\LimitByCraftingSkillMod.dll
```

The zip contains one top-level `LimitByCraftingSkillMod\` folder with the deployable mod files. Publishing checklist and CurseForge listing notes: [`PUBLISHING.md`](PUBLISHING.md).

## Deploy to a local game install

`tools\deploy.ps1` always builds by default, runs `prepare_mod.ps1`, then copies everything from `prepared_mod_files\` into:

```text
<game>\Mods\LimitByCraftingSkillMod\
```

Game install directory resolution:

1. `SEVENDTD_GAME_PATH`
2. `7_DAYS_TO_DIE_GAME_PATH`
3. Steam default: `C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die`

PowerShell:

```powershell
.\tools\deploy.ps1
```

Git Bash with Bazel:

```bash
MSYS2_ARG_CONV_EXCL='*' bazel run //tools:deploy
```

## Test

Hermetic unit tests:

```bash
bazel test //tests:all
```

Windows Git Bash may require `BAZEL_SH` to point to Bash for the test runner.

Local .NET fallback:

```powershell
dotnet test tests\LimitByCraftingSkillMod.Tests.csproj
```

Optional game-data integration checks are documented in [`LOCAL_GAME_HARNESS.md`](LOCAL_GAME_HARNESS.md).

## ClassName map generator

The map generator is a hermetic Python tool used to compare game XML with `ClassNameToCraftingSkillMap.xml` and produce generated reference XML.

```bash
bazel run //tools:generate_classname_map_report -- --items <game>\Data\Config\items.xml --map src/ClassNameToCraftingSkillMap.xml --out-csv <report.csv> --out-generated-xml src/ClassNameToCraftingSkillMap_generated.xml
```

Details: [`../tools/README_MAP_GENERATOR.md`](../tools/README_MAP_GENERATOR.md).

## Required-level troubleshooting

To trace why an item resolves to required level `0` or a wrong tier after a game update, see [`DEBUG_INSTRUMENTATION.md`](DEBUG_INSTRUMENTATION.md).

## Design and maintenance docs

- [`DESIGN.md`](DESIGN.md): high-level behavior and policy.
- [`GATING_AND_RESTRICTIONS.md`](GATING_AND_RESTRICTIONS.md): required-level resolution and enforcement hooks.
- [`CLASS_NAME_MAP_HOWTO.md`](CLASS_NAME_MAP_HOWTO.md): map editing and override semantics.
- [`GAME_API_NOTES.md`](GAME_API_NOTES.md): game API assumptions and Harmony patch targets.
- [`PUBLISHING.md`](PUBLISHING.md): CurseForge/GitHub release packaging and upload checklist.
