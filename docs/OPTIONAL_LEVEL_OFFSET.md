# Optional: Level restriction offset (document only)

**Status:** Not implemented. Tentative design for a possible config option.

## Idea

By default, the mod restricts an item when the player's crafting level is *below* the item's required level. An optional **offset** could allow use of items one (or N) tiers above the player's current level.

Examples:

- Offset = 0 (default): Player must have level ≥ required to use the item.
- Offset = 1: Player can use items up to one tier above what they can craft (e.g. can use quality 3 when they can craft quality 2).

This could give back some of the excitement of finding higher quality loot without letting players get too far ahead too quickly.

## Implementation sketch

- Add a config value (e.g. `LevelOffset`, integer ≥ 0).
- In restriction logic, treat effective required level as `max(0, requiredLevel - offset)` when deciding if the item is restricted.

## Rationale for not implementing

- Simply out of scope for initial implementation. No technical limitations.
