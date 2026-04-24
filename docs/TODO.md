# Follow-ups (low priority)

- **Inventory UI:** Stale red labels after slot swap — **`RestrictionLabelColor.ApplyRestrictionColorsToGrid`** now resets empty and non-restricted slots to default (white) each refresh, not only tinting restricted stacks. Re-test in-game; if a theme uses non-white default names, we may need to read vanilla color from the control instead of `Color.white`.
