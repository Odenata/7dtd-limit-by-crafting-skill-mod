# Quick-move, shift-click, and toolbelt hooks

Vanilla `XUiC_ItemStack.HandleMoveToPreferredLocation` (see decompiled `Assembly-CSharp`) does:

1. **`AddItemToBackpack(ItemStack)`** — merge / move within backpack first.
2. **Only if that returns false:** **`AddItemToToolbelt(ItemStack)`**.

Therefore:

- Blocking **only** `AddItemToToolbelt` / `AddItemToPreferredToolbeltSlot` allows backpack merge and shift-click into storage while still preventing restricted items from landing on the hotbar when the backpack path fails.
- Prefixing **`HandleMoveToPreferredLocation`** and returning `false` for backpack sources **skips step 1 entirely**, which breaks shift-click stacking and similar flows. This mod **does not** patch `HandleMoveToPreferredLocation`; it relies on **`AddItemToToolbeltRestrictionPatch`** plus toolbelt **`HandleStackSwap`** for hotbar placement.
