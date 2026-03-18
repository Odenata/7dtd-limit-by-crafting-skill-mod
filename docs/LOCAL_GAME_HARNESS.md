# Local game harness (optional)

The mod’s unit tests use **mocks** (`ItemClass`, fake `DisplayDataList`). They do **not** load `Assembly-CSharp.dll` or a real player `Progression` graph.

To validate **exact** required levels against the running game (e.g. *“Electrician 10, can I place `ironGarageDoor_PoweredWhite`?”*), use one of these approaches:

## In-game (minimal)

1. Deploy the mod, set **`DebugMode`** in `Config.xml`.
2. Use a character with a known Electrician level (creative / XP tweak).
3. Try the action (hotbar / place). Watch the log for  
   `GetRequiredLevelForItem exit=0 reason=...` and  
   `IsItemRestricted` / `requiredLevel_zero_not_restricted` in trace output.
4. Compare to the progression UI (crafting menu) for the same item.

## Separate local-only harness repo (future)

A **dedicated repository** (not this mod) can:

- Reference **`…/7DaysToDie_Data/Managed/Assembly-CSharp.dll`** (and related managed assemblies as needed).
- Host a minimal executable or test project that:
  - Loads the game’s item database and progression types.
  - Constructs or attaches to a **minimal player / Progression** context (exact API depends on game version; may require running under the game process or a stripped host).
  - Calls the same reflection paths as [`GameReflection`](../src/GameReflection.cs) (`DisplayDataList`, `QualityStarts`, `GetUnlockItem`, …) against **live** data.

**Why separate:** keeps game binaries and fragile bootstrap **out of the mod repo** and avoids licensing/CI issues. Document findings (e.g. “powered garage hits parent row with `QualityStarts[0]==0`”) back here or in [`CLASS_NAME_MAP_HOWTO.md`](CLASS_NAME_MAP_HOWTO.md).

## Auditing “required level is 0”

If an item is mapped but **never restricts**, common causes:

1. **`no_progression_match`** — fix `progressionMatchName` / map group.
2. **Match succeeds but `QualityStarts[0] == 0`** — vanilla treats synthetic tier 1 as level 0; the mod only restricts when **`requiredLevel > 0`** ([`LimitByCraftingSkillLogic`](../src/LimitByCraftingSkillLogic.cs)). Use **`requiredLevelMin`** / **`requiredLevelOverride`** if you want a floor anyway.

Powered iron garage rows: the mod now takes the **maximum** required level across matching `craftingelectrician` `DisplayData` rows when the map key matches **`ironGarageDoor_*Powered*`** (see `GameReflection`).
