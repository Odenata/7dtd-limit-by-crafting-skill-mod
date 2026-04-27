# Optional: Prevent acquisition (document only)

**Status:** Not implemented. Tentative design for a possible future feature.

## Idea

Beyond restricting *use* of over-level items, prevent players from *acquiring* them from loot, traders, or rewards. That would require hooking into:

- **Loot generation:** When a level-based craftable item is generated for a loot container, either automatically scrap it (preserving crafting components) or remove it from the loot result.
- **Trade system:** Block purchase (or replace with a downgrade) when the item would be above the player's crafting level.
- **Reward system:** Similarly filter or downgrade quest/reward items.

## Rationale for not implementing

- Use restriction may be sufficient for the design goal.
- Implementation scope is large (multiple systems, edge cases, compatibility with other mods).

This document preserves the idea for later consideration.
