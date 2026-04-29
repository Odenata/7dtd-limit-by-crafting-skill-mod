# Publishing

This document is for maintainers publishing release files to CurseForge or GitHub Releases.

The normal CurseForge workflow is manual: generate a zip from this repo, upload it through the CurseForge project dashboard, and paste the listing/changelog text. Keep generated zips out of git.

## Source files

- CurseForge description source: [`CURSEFORGE_DESCRIPTION.md`](CURSEFORGE_DESCRIPTION.md)
- Project icon source: [`../icon.png`](../icon.png)
- Player README: [`../README.md`](../README.md)
- Current release version: [`../VERSION`](../VERSION)

Keep these version fields aligned for each release:

- [`../VERSION`](../VERSION)
- [`../MODULE.bazel`](../MODULE.bazel) `module(version = "...")`
- [`../src/ModInfo.xml`](../src/ModInfo.xml) `<Version value="..." />`

## Release checklist

1. Decide the target 7 Days to Die version(s) supported by the release.
2. Update version fields if this is a new release.
3. Update player-facing notes in `README.md` and `CURSEFORGE_DESCRIPTION.md` if behavior changed.
4. Run verification:

   ```bash
   bazel test //tests:all
   ```

5. Build the DLL:

   ```bash
   bazel build //src:LimitByCraftingSkillMod
   ```

6. Create the release zip:

   ```powershell
   .\tools\package_release.ps1 -DllPath .\bazel-bin\src\LimitByCraftingSkillMod.dll
   ```

7. Inspect the zip before upload. It should contain one top-level folder:

   ```text
   LimitByCraftingSkillMod/
     0Harmony.dll
     ClassNameToCraftingSkillMap.xml
     Config.xml
     LimitByCraftingSkillMod.dll
     ModInfo.xml
   ```

8. Upload `dist\LimitByCraftingSkillMod-<version>.zip` to CurseForge.
9. Choose the appropriate CurseForge release type:
   - `Release` for the default recommended download.
   - `Beta` for a public test build.
   - `Alpha` only for experimental builds.
10. Select the supported 7 Days to Die game version(s).
11. Add a markdown changelog for the file.
12. Paste or update the CurseForge project description from `CURSEFORGE_DESCRIPTION.md`.
13. Verify the uploaded file after approval by downloading it and checking the zip layout.
14. Create a matching Git tag and GitHub Release if you want GitHub to retain old downloadable versions.

## CurseForge project page

CurseForge does not automatically sync the project description from GitHub in the normal dashboard flow. Treat `CURSEFORGE_DESCRIPTION.md` as the source to copy from.

Recommended project metadata:

- Name: `Limit by Crafting Skill`
- Summary: `Restricts high-tier item use by each player's crafting skill level.`
- Logo image: `icon.png`
- Source / issue link: `https://github.com/Odenata/7dtd-limit-by-crafting-skill-mod`

## Archive policy

Do not commit generated release archives. Use:

- `dist/` for local release zips.
- CurseForge files for public CurseForge downloads.
- GitHub Releases for public archived versions.

Old CurseForge files can stay available unless a release is broken or unsafe. Prefer archiving over deleting if you need to hide an old file.

## Future automation

CurseForge has an upload API, including markdown changelogs and release metadata. Do not add API automation until the manual process and project metadata are stable.
