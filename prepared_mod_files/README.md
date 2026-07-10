# Prepared mod files (generated)

This folder is populated by [`tools/prepare_mod.ps1`](../tools/prepare_mod.ps1) after a successful build. It contains exactly the files that belong in the game directory:

`Mods/LimitByCraftingSkillMod/`

- `LimitByCraftingSkillMod.dll`
- `ModInfo.xml`
- `Config.xml`
- `ClassNameToCraftingSkillMap.xml`

Do **not** add `0Harmony.dll` here. The game provides Harmony via **`Mods\0_TFP_Harmony`**.

**Developers:** run `.\tools\prepare_mod.ps1` from the repo root (or use `.\tools\deploy.ps1`, which builds, prepares, and copies to your game install).

**Players:** copy every file from this folder into `Mods\LimitByCraftingSkillMod\` under your 7 Days to Die installation (replace existing files when updating the mod).

Generated files here are gitignored except this README.
