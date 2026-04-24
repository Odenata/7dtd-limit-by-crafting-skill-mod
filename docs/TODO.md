# Follow-ups (low priority)

- **Inventory UI:** When the player **swaps** items in a restricted inventory slot, the **red restriction tint/label** can stay on the slot and apply to the **new** item incorrectly. Re-validate or clear slot decoration when slot contents change (likely near `InventoryGridRestrictionColorPatches` / label refresh on `SetItem` or equivalent).
