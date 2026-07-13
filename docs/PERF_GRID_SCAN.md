# Grid restriction scan performance

FPS drops when opening containers with this mod come from **red-label restriction coloring** (`ApplyRestrictionColorsToGrid` → per-slot restriction checks), not workstation block activation.

## Hermetic benchmark

```bash
# Git Bash on Windows: MSYS_NO_PATHCONV=1 avoids //tests being rewritten
MSYS_NO_PATHCONV=1 bazel test //tests:restriction_scan_benchmark --test_output=all
```

Tagged `manual` — not part of `//tests:all` / CI.

The harness warmups once, then times 200× `RestrictionScan.Evaluate` for stack counts **8 / 45 / 90 / 180** using real `ClassNameToCraftingSkillMap.xml` keys and a non-trivial fake progression graph. Console lines look like:

`RestrictionScan baseline: N=180 evaluated=180 restricted=180 ms/scan=… µs/item=…`

- **baseline** — per-scan required-level cache only (no cross-frame memo)
- **memo** — cross-frame required-level memo enabled (models post-optimization live path)

## Baseline timings (2026-07-13, before Phase 3 live opts)

Machine-local Stopwatch numbers; compare relative deltas on the same machine.

| N stacks | baseline ms/scan | baseline µs/item | memo ms/scan | memo µs/item |
|---------:|-----------------:|-----------------:|-------------:|-------------:|
| 8 | 0.050 | 6.24 | 0.025 | 3.06 |
| 45 | 0.889 | 19.75 | 0.042 | 0.93 |
| 90 | 2.638 | 29.31 | 0.095 | 1.06 |
| 180 | 11.607 | 64.48 | 0.171 | 0.95 |

## After Phase 3 (cross-frame memo path)

Same harness, memo column is what the live grid path uses (`useRequiredLevelMemo: true`). Relative win vs baseline at N=180: ~**68×** fewer ms/scan hermetically. Live FPS win additionally comes from dropping blind 0.2s open-grid rescans.

| N stacks | memo ms/scan | memo µs/item |
|---------:|-------------:|-------------:|
| 8 | 0.028 | 3.48 |
| 45 | 0.041 | 0.91 |
| 90 | 0.101 | 1.12 |
| 180 | 0.177 | 0.99 |

Deployed to the game Mods folder after these changes; confirm in-session: large loot + backpack open stays smooth, red labels still update on open and after crafting skill gain.

## In-game cost (live smoke)

1. Deploy with **DebugMode off** in `Config.xml` (fair timing; debug logs inflate cost).
2. Open a large loot container with backpack visible.
3. Confirm red labels still apply on open and after a crafting skill gain.
4. Optional: temporarily set DebugMode on and watch for `ApplyRestrictionColorsToGrid` / `IsItemRestricted` spam — after Phase 3, open/dirty should dominate, not a steady 5 Hz rescan while idle.

## Related hot path

- `RestrictionLabelColor.ApplyRestrictionColorsToGrid`
- `RestrictionScan` / `RestrictionHelper.IsItemRestricted`
- `GridUpdateRestrictionColorPatch` (dirty + short just-opened window; no blind 0.2s forever-throttle)
- `HandleSlotChangedEvent` marks dirty so moves between open grids recolor without a continuous rescan
