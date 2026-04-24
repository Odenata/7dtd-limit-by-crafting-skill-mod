# In-game test checklist

After deploying the mod (`tools\deploy.ps1`), start 7 Days to Die and verify the following. Use a save or creative mode to get items above your current crafting level.

## Armor

- [x] Equip a piece of armor that is above your current Armor crafting level → **blocked** (item does not move to armor slot).
- [x] Equip the same armor when your Armor level is sufficient → **allowed**.
- [x] In inventory, restricted armor is visually indicated (e.g. label color).

## Hotbar (handheld weapons/tools)

- [ ] Add a weapon or tool that is above your current skill level to the hotbar (drag or right-click) → **blocked**.
- [ ] Add the same item when your skill level is sufficient → **allowed**.
- [x] Restricted handheld items in inventory are visually indicated.

### Electrician / Workstations / Harvesting (no item quality)

- [ ] **Electrician:** Add a mapped placeable (e.g. generator bank, wire relay) to hotbar when below Electrician level → **blocked** if required level &gt; 0 in progression.
- [ ] **Workstations:** Add **forge** or **workbench** item to hotbar when below Workstations level → **blocked** when progression matches.
- [ ] **Harvesting:** Stone pick / stone shovel / T0 tools in hotbar respect **HarvestingTools** level (synthetic tier 1 + map entry).
- [ ] With `DebugMode` true, if an item never restricts, check game log for `GetRequiredLevelForItem exit=0 reason=...` (e.g. `no_progression_match` ⇒ add map entry or fix progression match).

## Workstations

- [ ] Open a placed workstation (e.g. Forge, Workbench) that is above your Workstations level → **blocked**; red popup shows "You don't know how to use [item name]" and "[Workstations] X/Y".
- [ ] Open the same workstation when your level is sufficient → **allowed**.
- [ ] Picking up the workstation is still allowed when restricted.

## Vehicles

- [ ] Try to drive a vehicle you cannot craft (e.g. above Vehicles level) → **blocked**; red popup with skill level info.
- [ ] Open vehicle inventory, refuel, pick up vehicle, sit as passenger → **allowed** when restricted from driving.
- [ ] Drive when your Vehicles level is sufficient → **allowed**.

## Popup text

- [ ] Popup is **red**.
- [ ] Line 1: "You don't know how to use [item name]".
- [ ] Line 2: "[Crafting Skill Name] [player level]/[required level]".

## Food and Medical (opt-in; default off in `Config.xml`)

- [ ] Set **`<Food>true</Food>`** and/or **`<Medical>true</Medical>`**, restart, verify eat/drink/meds above your **craftingFood** / **craftingMedical** level are blocked with popup; at-level or below → allowed.
- [ ] With both **false** (default), consumables behave like **vanilla** (no gating from this mod).

## Config

- [ ] Disable a skill (e.g. Vehicles) in Config.xml → restart game, verify that skill is no longer restricted.
- [ ] Confirm **Food** / **Medical** default **false** unless you intend to test consumable gates.
- [ ] In multiplayer (if applicable), server config is used for restrictions, not client config.

## Debug

- [ ] Set `DebugMode` to true in Config.xml → check game log for mod messages when attempting restricted actions.

## Optional: progression.xml vs map (local dev)

With your game install’s `progression.xml`:

1. Set **`PROGRESSION_XML_PATH`** (or **`7DTD_PROGRESSION_XML`**) to `…\Data\Config\progression.xml`.
2. Optionally set **`CLASSNAME_MAP_XML`** to `src\ClassNameToCraftingSkillMap.xml`.
3. Run **`bazel test //tests:progression_assembly_integration`**.

The test asserts each mapped item resolves against unlock lists (same garage-door transforms as the mod). Skips when the env var is unset. Optional **`ASSEMBLY_CSHARP_DLL`** → `…\Managed\Assembly-CSharp.dll` runs a reflection smoke check on progression types.

For validating required levels against **live** game progression (player skill vs placeable), see **[LOCAL_GAME_HARNESS.md](LOCAL_GAME_HARNESS.md)** (in-game debug + optional separate harness repo).
