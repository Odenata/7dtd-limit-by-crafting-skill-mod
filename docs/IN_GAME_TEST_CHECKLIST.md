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

## Config

- [ ] Disable a skill (e.g. Vehicles) in Config.xml → restart game, verify that skill is no longer restricted.
- [ ] In multiplayer (if applicable), server config is used for restrictions, not client config.

## Debug

- [ ] Set `DebugMode` to true in Config.xml → check game log for mod messages when attempting restricted actions.
