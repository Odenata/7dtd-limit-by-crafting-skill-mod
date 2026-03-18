# ClassName map generator / report

[`generate_classname_map_report.py`](generate_classname_map_report.py) reads vanilla **`items.xml`** and compares every item that declares **`CraftingSkillGroup`** against [`src/ClassNameToCraftingSkillMap.xml`](../src/ClassNameToCraftingSkillMap.xml).

**Outputs**

1. **CSV** — one row per item with a crafting group in `items.xml`: suggested group, and whether that `className` already appears in the mod map.
2. **Summary text** — mod map entries that do **not** appear in `items.xml` (possible typos or renames per game version).

**Optional:** pass **`progression.xml`** to emit a list of progression names containing `crafting` (for aligning `progressionMatchName` / tier unlocks).

**Example**

```powershell
python tools/generate_classname_map_report.py `
  --items "D:\Steam\steamapps\common\7 Days To Die\Data\Config\items.xml" `
  --map src/ClassNameToCraftingSkillMap.xml `
  --out-csv out/map_items.csv `
  --progression "D:\Steam\steamapps\common\7 Days To Die\Data\Config\progression.xml" `
  --out-progression-csv out/progression_names.csv
```

Run after each game update to refresh diffs; merge CSV hints into the XML manually or with a follow-up script.
