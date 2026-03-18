using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace LimitByCraftingSkillMod
{
    /// <summary>NDJSON session log for debug mode (session 4a55c6).</summary>
    internal static class AgentDebugSessionLog
    {
        private const string SessionId = "4a55c6";

        private static int _harvestingEvalLogCount;

        /// <summary>Runtime evidence for HarvestingTools restriction (capped per session).</summary>
        internal static void WriteHarvestingEval(string mapKey, bool? inXmlMap, string phase, int requiredLevel, int playerLevel, bool restricted, string detail)
        {
            if (_harvestingEvalLogCount >= 50) return;
            _harvestingEvalLogCount++;
            try
            {
                var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var sb = new StringBuilder(384);
                sb.Append("{\"sessionId\":\"").Append(SessionId).Append("\",\"hypothesisId\":\"H-Harvest\",\"timestamp\":").Append(ts)
                    .Append(",\"location\":\"RestrictionHelper:IsItemRestricted\",\"message\":\"harvesting_eval\",\"data\":{")
                    .Append("\"phase\":").Append(JsonStr(phase))
                    .Append(",\"mapKey\":").Append(JsonStr(mapKey))
                    .Append(",\"inXmlMap\":").Append(inXmlMap.HasValue ? (inXmlMap.Value ? "true" : "false") : "null")
                    .Append(",\"requiredLevel\":").Append(requiredLevel)
                    .Append(",\"playerLevel\":").Append(playerLevel)
                    .Append(",\"restricted\":").Append(restricted ? "true" : "false")
                    .Append(",\"detail\":").Append(JsonStr(detail))
                    .Append("}}\n");
                var line = sb.ToString();
                foreach (var path in LogPaths())
                {
                    try { File.AppendAllText(path, line); } catch { }
                }
            }
            catch { }
        }

        internal static bool IsTraceMapKey(string mapKey)
        {
            if (string.IsNullOrEmpty(mapKey)) return false;
            var x = mapKey.ToLowerInvariant();
            if (x.IndexOf("meleetoolpick", StringComparison.Ordinal) >= 0) return true;
            if (x.IndexOf("meleetoolaxet2steelaxe", StringComparison.Ordinal) >= 0) return true;
            if (x.IndexOf("meleetoolaxet2steelfireaxe", StringComparison.Ordinal) >= 0) return true;
            if (x.IndexOf("garagedoor", StringComparison.Ordinal) >= 0 && x.IndexOf("powered", StringComparison.Ordinal) >= 0)
                return true;
            return false;
        }

        // #region agent log
        internal static void Write(string hypothesisId, string location, string message,
            string mapKey, bool? inXmlMap, string skillGroup, int requiredLevel, int playerLevel, bool restricted, string detail = null)
        {
            try
            {
                var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var sb = new StringBuilder(512);
                sb.Append("{\"sessionId\":\"").Append(SessionId).Append("\",\"hypothesisId\":\"").Append(Escape(hypothesisId))
                    .Append("\",\"timestamp\":").Append(ts).Append(",\"location\":\"").Append(Escape(location))
                    .Append("\",\"message\":\"").Append(Escape(message)).Append("\",\"data\":{")
                    .Append("\"mapKey\":").Append(JsonStr(mapKey))
                    .Append(",\"inXmlMap\":").Append(inXmlMap.HasValue ? (inXmlMap.Value ? "true" : "false") : "null")
                    .Append(",\"skillGroup\":").Append(JsonStr(skillGroup))
                    .Append(",\"requiredLevel\":").Append(requiredLevel)
                    .Append(",\"playerLevel\":").Append(playerLevel)
                    .Append(",\"restricted\":").Append(restricted ? "true" : "false")
                    .Append(",\"detail\":").Append(JsonStr(detail))
                    .Append("}}\n");
                var line = sb.ToString();
                foreach (var path in LogPaths())
                {
                    try
                    {
                        File.AppendAllText(path, line);
                    }
                    catch { /* try next */ }
                }
            }
            catch { }
        }
        internal static void WriteWorkstationFallback(string mapKey, int requiredLevel)
        {
            try
            {
                var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var line = "{\"sessionId\":\"" + SessionId + "\",\"hypothesisId\":\"fallback\",\"timestamp\":" + ts
                    + ",\"location\":\"GameReflection:GetRequiredLevelForItem\",\"message\":\"electrician_used_workstations_progression\",\"data\":{"
                    + "\"mapKey\":" + JsonStr(mapKey) + ",\"requiredLevel\":" + requiredLevel + "}}\n";
                foreach (var path in LogPaths())
                {
                    try { File.AppendAllText(path, line); } catch { }
                }
            }
            catch { }
        }

        internal static void WriteProgressionProbe(string mapKey, string lookupName, string candidatesJoined, bool xmlProgressionOverride, string sampleItemNames)
        {
            try
            {
                var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var line = "{\"sessionId\":\"" + SessionId + "\",\"hypothesisId\":\"probe\",\"timestamp\":" + ts
                    + ",\"location\":\"GameReflection:GetRequiredLevelForItem\",\"message\":\"no_progression_match_probe\",\"data\":{"
                    + "\"mapKey\":" + JsonStr(mapKey) + ",\"lookup\":" + JsonStr(lookupName)
                    + ",\"xmlProgressionOverride\":" + (xmlProgressionOverride ? "true" : "false")
                    + ",\"candidates\":" + JsonStr(candidatesJoined) + ",\"sampleItemNames\":" + JsonStr(sampleItemNames) + "}}\n";
                foreach (var path in LogPaths())
                {
                    try { File.AppendAllText(path, line); } catch { }
                }
            }
            catch { }
        }

        // #endregion

        private static string[] LogPaths()
        {
            var list = new System.Collections.Generic.List<string>();
            try
            {
                var dir = Path.GetDirectoryName(typeof(AgentDebugSessionLog).Assembly.Location);
                if (!string.IsNullOrEmpty(dir))
                    list.Add(Path.Combine(dir, "debug-4a55c6.log"));
            }
            catch { }
            try
            {
                list.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "repos",
                    "7dtd-limit-by-crafting-skill-mod", "debug-4a55c6.log"));
            }
            catch { }
            return list.ToArray();
        }

        private static string JsonStr(string s)
        {
            if (s == null) return "null";
            return "\"" + Escape(s) + "\"";
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }
}
