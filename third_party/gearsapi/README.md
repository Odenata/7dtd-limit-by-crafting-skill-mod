# Vendored GearsAPI (soft-dep for in-game mod settings)

**GearsAPI.dll**
- Version: 2.0.1.0 (from Gears v7.0.0-DP4 / 7DTD 3.0 line)
- Source release: https://github.com/s7092910/Gears/releases/tag/v7.0.0-DP4
- SHA256: `07a12c4ed96d19162100d025a18cfeaffd8568c88619078ec1f54b41342485f7`
- Author: Laydor (s7092910)
- Purpose: compile + **runtime soft dependency**. Ship `GearsAPI.dll` next to `LimitByCraftingSkillMod.dll` so the mod loads without Gears installed. Players need [Gears](https://www.nexusmods.com/7daystodie/mods/4017) + [Quartz](https://www.nexusmods.com/7daystodie/mods/2409) for the in-game settings UI.

**InControl.dll** (compile-only)
- Copied from the 7 Days to Die `Managed` folder for resolving GearsAPI metadata at compile time.
- SHA256: `dfeabc985f6ddf4d3eaf40f031f5baa81e0fd301a1f8d0714fb24da78be8f0b2`
- **Do not** ship InControl with LimitByCraftingSkillMod; the game already provides it.

## Updating

1. Download a Gears release zip matching the target game line.
2. Copy `GearsAPI.dll` into `lib/net472/`.
3. Refresh `InControl.dll` from the matching game `Managed` folder if the compiler requires it.
4. Update SHA256 and version notes in this README.

## License note

Gears does not publish a LICENSE file on GitHub as of 2026-07-13. Redistributing `GearsAPI.dll` inside consumer mods is the documented soft-dep pattern (Nexus / wiki). Keep attribution to Laydor and the GitHub release URL.
