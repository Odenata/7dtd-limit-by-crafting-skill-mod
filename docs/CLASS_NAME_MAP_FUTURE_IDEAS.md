# Class name map — optional future ideas

This doc captures ideas we may revisit later. **No implementation is planned right now.**

## Heuristic-based generation

We could reintroduce a script or tool that generates a **best-guess** ClassNameToCraftingSkillMap from:

- **Prefix heuristics** on class names (e.g. `gunBow*` → Bows, `meleeWpnSpear*` → Spears), using patterns derived from known samples (see e.g. `tmp-classname-samples.md`). Output would be a generated file that maintainers review and then use or merge into the canonical map.
- **Exact overrides** for class names that don’t match any prefix (e.g. `generatorBank` → Electrician).

The map would remain the single source of truth at runtime; the generator would only be a convenience for producing a starting point or refresh after game updates.

## Scraping the game API or items.xml

We could try to **discover** item class names from:

- **7dtd-mod-dev-tools game API docs:** e.g. parse the ItemClass hierarchy or by-type markdown to list types that extend ItemClass. The static API only exposes a small set of base types (ItemClassArmor, ItemClassBlock, …), not the concrete runtime names (gunBowT0PrimitiveBow, etc.), so this would only partly fill the map.
- **Game items.xml:** If we have a path to the game’s item config (e.g. `Data/Config/items.xml`), we could scrape `<item name="...">` (or equivalent) to get concrete class names and then run heuristics on them.

Combining heuristics with one or both of these sources could reduce the need to maintain the map entirely by hand when the game or mods add new items.
