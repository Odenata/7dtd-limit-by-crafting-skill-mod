# Game version compatibility

Use this table to see which **7 Days to Die** builds work with which **mod** version. It is updated when maintainers verify a combination or when a game update breaks the mod.

| Mod version | 7 Days to Die (tested / expected) | Notes |
|-------------|-----------------------------------|--------|
| 0.1.0       | 2.6                               | Initial listing for Alpha 21 / game 2.6. **Not compatible with 3.0.** |
| 1.0.0       | 3.0.0 (b252) experimental         | **Breaking release for v3.0 only** — targeted experimental b252; patch targets, drag API, and workstation gating reworked; do not install on 2.6. Activation-prefix workstation gating; v3 drag API; interruptible `MainMenuOpening`; forge radial-menu safety finalizers. Maintainer smoke PASS 2026-06-16. Classname map needs targeted ~30-row pass (`V3_MAP_DELTA_REPORT.md`). |
| 1.0.1       | 3.0.0 (b259) stable               | Verified against official stable (Steam); rebuild against b259. No bag-specific change. Install requires EAC off. Server and clients need the same mod version. |
| 1.1.0       | 3.0.0 (b259) stable               | No longer ships `0Harmony.dll`. Requires the game's official `Mods\0_TFP_Harmony`. Install folder is DLL + ModInfo + Config + ClassName map only. |

When you publish a release, add or adjust a row if the supported game range changed.
