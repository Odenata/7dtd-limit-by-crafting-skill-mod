# Required-level debugging instrumentation

## Built-in: game log (`DebugMode`)

When **`Config.xml`** has `<DebugMode>true</DebugMode>`, **`GetRequiredLevelForItem`** writes to the **game output log** lines like:

`[LimitByCraftingSkill] GetRequiredLevelForItem exit=0 reason=no_progression_match mapKey=forge skillGroup=Workstations hasQuality=False rawQuality=0`

**Reason values:** `no_map` (unmapped item; first occurrence per `mapKey` only), `no_quality` (mapped but not Electrician/Workstations/HarvestingTools and no quality), `no_player`, `no_lookup`, `no_progression`, `no_progression_class`, `no_display_data`, `no_progression_match`, `exception`.

---

Use the NDJSON file approach below when you need **structured** logs or every return value (not only zeros).

## Log file

- **Path:** `%AppData%\7DaysToDie\logs\debug-limit-mod-required-level.log` (pick any fixed name; avoid clashing with other mods’ logs).
- **Format:** NDJSON (one JSON object per line). Append only; delete or rename the file before each test run for a clean capture.
- **Requires:** `using System.IO;` and `using System.Text;` in `GameReflection.cs`.

## What to log (three layers)

| Layer | `hypothesisId` / purpose | When |
|-------|-------------------------|------|
| **A. Return value** | `return_value` | Every exit from `GetRequiredLevelForItem`: the **actual int** returned (`returnedLevel`). |
| **B. DisplayData names** | `display_data_names` | Once per `lookupName` when the function would return **0** (no row matched). Shows top-level `DisplayData.ItemName` values (often `""` for armor). |
| **C. Deep probe** | `required_level_probe` | Once per call (optional): cheap diagnostics—display row count, whether exact `ItemName` match exists on class, alternate level reads. |

Interpretation:

- If **A** shows `returnedLevel: 0` for an item that should gate crafting, check **B**: empty `displayDataItemNames` ⇒ matching must use **unlocks** (`UnlockDataList`, `GetUnlockData`, `GetUnlockItem`, `RecipeList`)—see `DESIGN.md`.
- **C** is legacy-oriented; in 2.5+ `Progression.ProgressionClasses` may be unavailable, so probes that scan all classes often yield `-1` for E/F.

## Hook points in `GetRequiredLevelForItem`

After `displayDataList` is resolved and non-empty:

1. **Optional:** call `LogRequiredLevelProbe(...)` once per invocation (see appendix).
2. **On success** (direct `DisplayData` match): before `return GetRequiredLevelFromDisplayData(...)`, call `AppendReturnValueLog(lookupName, itemNameForMatch, quality, level)`.
3. **On success** (unlock path): same before each `return GetRequiredLevelFromDisplayData(...)`.
4. **On failure** (fall through to `return 0`): call `AppendReturnValueLog(..., 0)` then `LogDisplayDataItemNamesOnce(lookupName, itemNameForMatch, displayDataList)`.

Wrap calls in `try { ... } catch { }` so logging never breaks gameplay.

## Grep examples (after a session)

```text
grep return_value debug-limit-mod-required-level.log
grep craftingarmor debug-limit-mod-required-level.log
grep display_data_names debug-limit-mod-required-level.log
```

## Appendix A — Minimal helpers (copy-paste)

Use a **constant log file name** you recognize; change `DebugLogFileName` if multiple debug sessions run.

```csharp
private const string DebugLogFileName = "debug-limit-mod-required-level.log";

private static string GetRequiredLevelDebugLogPath()
{
    var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "7DaysToDie", "logs");
    try { if (!Directory.Exists(dir)) Directory.CreateDirectory(dir); } catch { }
    return Path.Combine(dir, DebugLogFileName);
}

private static string EscapeJson(string s)
{
    return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
}

private static void AppendReturnValueLog(string lookupName, string itemName, int quality, int returnedLevel)
{
    var path = GetRequiredLevelDebugLogPath();
    var ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    var sb = new StringBuilder();
    sb.Append("{\"hypothesisId\":\"return_value\",\"message\":\"GetRequiredLevelForItem_return\",");
    sb.Append("\"data\":{\"lookupName\":\"").Append(EscapeJson(lookupName ?? "")).Append("\",\"itemName\":\"").Append(EscapeJson(itemName ?? "")).Append("\",\"quality\":").Append(quality).Append(",\"returnedLevel\":").Append(returnedLevel).Append("},");
    sb.Append("\"timestamp\":").Append(ts).Append("}\n");
    File.AppendAllText(path, sb.ToString());
}

private static readonly System.Collections.Generic.HashSet<string> _loggedDisplayDataNamesForLookup =
    new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

private static void LogDisplayDataItemNamesOnce(string lookupName, string itemNameForMatch, IList displayDataList)
{
    if (string.IsNullOrEmpty(lookupName) || displayDataList == null) return;
    lock (_loggedDisplayDataNamesForLookup)
    {
        if (_loggedDisplayDataNamesForLookup.Contains(lookupName)) return;
        _loggedDisplayDataNamesForLookup.Add(lookupName);
    }
    var names = new System.Collections.Generic.List<string>();
    const int max = 20;
    for (var i = 0; i < displayDataList.Count && i < max; i++)
    {
        var dd = displayDataList[i];
        if (dd == null) continue;
        var nameField = dd.GetType().GetField("ItemName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        names.Add(nameField?.GetValue(dd) as string ?? "");
    }
    var path = GetRequiredLevelDebugLogPath();
    var ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    var sb = new StringBuilder();
    sb.Append("{\"hypothesisId\":\"display_data_names\",\"message\":\"DisplayData_ItemNames_for_no_match\",");
    sb.Append("\"data\":{\"lookupName\":\"").Append(EscapeJson(lookupName)).Append("\",\"itemNameForMatch\":\"").Append(EscapeJson(itemNameForMatch ?? "")).Append("\",\"displayDataItemNames\":[");
    for (var i = 0; i < names.Count; i++)
    {
        if (i > 0) sb.Append(",");
        sb.Append("\"").Append(EscapeJson(names[i])).Append("\"");
    }
    sb.Append("]},\"timestamp\":").Append(ts).Append("}\n");
    File.AppendAllText(path, sb.ToString());
}
```

## Appendix B — Optional compact probe (per call)

Logs one line per `GetRequiredLevelForItem` call with **progression-class-local** facts (no `ProgressionClasses` dictionary required):

```csharp
private static void LogRequiredLevelProbe(string lookupName, object progressionClass, int displayDataCount, string itemNameForMatch, int quality)
{
    try
    {
        var anyExactItemName = false;
        var unlockTotal = 0;
        if (progressionClass != null)
        {
            var listField = progressionClass.GetType().GetField("DisplayDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var list = listField?.GetValue(progressionClass) as IList;
            if (list != null)
                for (var i = 0; i < list.Count; i++)
                {
                    var dd = list[i];
                    if (dd == null) continue;
                    var nameField = dd.GetType().GetField("ItemName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var n = nameField?.GetValue(dd) as string;
                    if (!string.IsNullOrEmpty(n) && string.Equals(n, itemNameForMatch, StringComparison.OrdinalIgnoreCase))
                        anyExactItemName = true;
                    var ulf = dd.GetType().GetField("UnlockDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var ul = ulf?.GetValue(dd) as IList;
                    if (ul != null) unlockTotal += ul.Count;
                }
        }
        var ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        var sb = new StringBuilder();
        sb.Append("{\"hypothesisId\":\"required_level_probe\",\"message\":\"required_level\",");
        sb.Append("\"data\":{\"lookupName\":\"").Append(EscapeJson(lookupName ?? "")).Append("\",\"itemName\":\"").Append(EscapeJson(itemNameForMatch ?? "")).Append("\",\"quality\":").Append(quality);
        sb.Append(",\"displayDataCount\":").Append(displayDataCount).Append(",\"anyExactDisplayDataItemName\":").Append(anyExactItemName ? "true" : "false");
        sb.Append(",\"unlockDataListEntryCountTotal\":").Append(unlockTotal).Append("},\"timestamp\":").Append(ts).Append("}\n");
        File.AppendAllText(GetRequiredLevelDebugLogPath(), sb.ToString());
    }
    catch { }
}
```

## Historical note (March 2026 session)

Earlier iterations used a fixed filename `debug-4a55c6.log` and a **`sessionId` field** for an external NDJSON ingest. That ingest is optional; the snippets above omit `sessionId` so logs stay self-contained on disk.

## Related docs

- **`DESIGN.md`** — how required level is derived (DisplayData + unlocks).
- **`docs/GAME_API_NOTES.md`** — progression types and names.
