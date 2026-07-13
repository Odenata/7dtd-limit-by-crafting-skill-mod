# Changelog

All notable **LimitByCraftingSkillMod** releases. Draft notes live in [`docs/releases/unreleased.md`](docs/releases/unreleased.md). Game support matrix: [`docs/COMPATIBILITY.md`](docs/COMPATIBILITY.md).

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.1.1] - 2026-07-13

### Changed

- Faster inventory restriction red labels: less work per grid refresh and no continuous open-container rescans (smoother FPS with large loot UIs).

### Fixed

- Red restriction labels no longer disappear after moving items between open inventory/container slots.
- Red labels re-apply when moving items from a container into the player backpack.

### Notes

- Target game: 3.0.0 (b259) stable.

## [1.1.0] - 2026-07-09

### Changed

- No longer ships `0Harmony.dll`. Requires the game's official `Mods\0_TFP_Harmony`.
- Install folder is DLL + ModInfo + Config + ClassName map only.

### Notes

- Target game: 3.0.0 (b259) stable.

## [1.0.1] - 2026-07-09

### Notes

- Verified against official stable (Steam); rebuild against b259. No bag-specific change. EAC off. Server and clients need the same mod version. Target game: 3.0.0 (b259) stable.

## [1.0.0] - 2026-06-16

### Changed

- **Breaking release for v3.0 only** — patch targets, drag API, and workstation gating reworked; do not install on 2.6.
- Activation-prefix workstation gating; v3 drag API; interruptible `MainMenuOpening`; forge radial-menu safety finalizers.

### Notes

- Target game: 3.0.0 (b252) experimental. Maintainer smoke PASS 2026-06-16.

## [0.1.0] - 2026-03-01

### Added

- Initial listing for Alpha 21 / game 2.6.

### Notes

- **Not compatible with 3.0.**
