# Game version compatibility

Use this table to see which **7 Days to Die** builds work with which **mod** version. It is updated when maintainers verify a combination or when a game update breaks the mod.

| Mod version | 7 Days to Die (tested / expected) | Notes |
|-------------|-----------------------------------|--------|
| 0.1.0       | 2.6                               | Initial listing. |
| 0.1.0       | 3.0.0 (b252) experimental         | v3.0 compatibility: removed dead `GameManager.workstationOpened`; `BlockCollector.OnBlockActivated` prefix drops removed `int` param; `HandleStackSwap` retargeted to `XUiC_BasePartStack`; v3 drag API (`DragAndDropWindow.CurrentStack`); `ServerConfigSync` interruptible `MainMenuOpening`; workstation gating is **activation-prefix only** (UI close postfixes removed — they corrupted TE saves); `WorkstationActivationCommandsSafePatch` for forge radial-menu NRE. Init-verified patch load. Maintainer smoke PASS 2026-06-16: drag/shift-click gating, dew/apiary, forge/workbench/chem/cement popup-only, Q6 mod install. Classname map needs targeted ~30-row pass (`V3_MAP_DELTA_REPORT.md`). |

When you publish a release, add or adjust a row if the supported game range changed.
