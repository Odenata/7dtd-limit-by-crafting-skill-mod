using System;
using System.IO;
using System.Text;

namespace LimitByCraftingSkillMod
{
    /// <summary>NDJSON debug log for in-world restriction session 1c192f.</summary>
    internal static class InWorldRestrictionDebugLog
    {
        // Debug instrumentation kept for temporary investigations.
        // Disabled in the final build to avoid log spam.
        private static bool EnableInWorldRestrictionDebugLog => ModConfig.Instance != null && ModConfig.Instance.DebugMode;

        private const string LogFileName = "debug-1c192f.log";
        private const string SessionId = "1c192f";

        private static string[] LogPaths()
        {
            var list = new System.Collections.Generic.List<string>();
            try
            {
                var dir = Path.GetDirectoryName(typeof(InWorldRestrictionDebugLog).Assembly.Location);
                if (!string.IsNullOrEmpty(dir))
                    list.Add(Path.Combine(dir, LogFileName));
            }
            catch { }
            try
            {
                list.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "repos",
                    "7dtd-limit-by-crafting-skill-mod", LogFileName));
            }
            catch { }
            return list.ToArray();
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static string JsonStr(string s)
        {
            if (s == null) return "null";
            return "\"" + Escape(s) + "\"";
        }

        /// <summary>Append one NDJSON line to session log. dataJson must be a valid JSON object string e.g. "{\"k\":\"v\"}".</summary>
        internal static void Write(string hypothesisId, string location, string message, string dataJson, string runId = null)
        {
            if (!EnableInWorldRestrictionDebugLog)
                return;
            try
            {
                if (string.IsNullOrEmpty(dataJson)) dataJson = "{}";
                var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var line = "{\"sessionId\":\"" + SessionId + "\",\"hypothesisId\":" + JsonStr(hypothesisId)
                    + ",\"timestamp\":" + ts + ",\"location\":" + JsonStr(location)
                    + ",\"message\":" + JsonStr(message) + ",\"data\":" + dataJson
                    + (string.IsNullOrEmpty(runId) ? "" : ",\"runId\":" + JsonStr(runId)) + "}\n";
                foreach (var path in LogPaths())
                {
                    try { File.AppendAllText(path, line); } catch { }
                }
            }
            catch { }
        }
    }
}
