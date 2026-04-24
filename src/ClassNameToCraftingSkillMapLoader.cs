using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Loads ClassNameToCraftingSkillMap.xml from the mod directory (and embedded fallback in the DLL).
    /// </summary>
    internal static class ClassNameToCraftingSkillMapLoader
    {
        private static IReadOnlyDictionary<string, string> _map;
        private static IReadOnlyDictionary<string, string> _progressionMatchByClassName;
        private static IReadOnlyDictionary<string, int> _requiredLevelOverrideByClassName;
        private static IReadOnlyDictionary<string, int> _requiredLevelMinByClassName;
        private static readonly object _lock = new object();

        public static IReadOnlyDictionary<string, string> GetMap()
        {
            EnsureLoaded();
            return _map;
        }

        public static string GetProgressionMatchNameOrClassName(string className)
        {
            if (string.IsNullOrEmpty(className)) return className;
            return TryGetProgressionMatchOverride(className, out var pm) ? pm : className;
        }

        public static bool TryGetProgressionMatchOverride(string className, out string progressionName)
        {
            progressionName = null;
            if (string.IsNullOrEmpty(className)) return false;
            EnsureLoaded();
            if (_progressionMatchByClassName != null &&
                _progressionMatchByClassName.TryGetValue(className, out var pm) &&
                !string.IsNullOrWhiteSpace(pm))
            {
                progressionName = pm.Trim();
                return true;
            }
            return false;
        }

        public static bool TryGetRequiredLevelOverride(string className, out int level)
        {
            level = 0;
            if (string.IsNullOrEmpty(className)) return false;
            EnsureLoaded();
            return _requiredLevelOverrideByClassName != null &&
                   _requiredLevelOverrideByClassName.TryGetValue(className, out level) && level > 0;
        }

        public static bool TryGetRequiredLevelMin(string className, out int minLevel)
        {
            minLevel = 0;
            if (string.IsNullOrEmpty(className)) return false;
            EnsureLoaded();
            return _requiredLevelMinByClassName != null &&
                   _requiredLevelMinByClassName.TryGetValue(className, out minLevel) && minLevel > 0;
        }

        internal static void InvalidateLoadedData()
        {
            lock (_lock)
            {
                _map = null;
                _progressionMatchByClassName = null;
                _requiredLevelOverrideByClassName = null;
                _requiredLevelMinByClassName = null;
            }
        }

        private static void EnsureLoaded()
        {
            if (_map != null) return;
            lock (_lock)
            {
                if (_map != null) return;
                LoadMapCore();
            }
        }

        private static void LoadMapCore()
        {
            var empty = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var emptyProg = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var emptyOv = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var prog = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var overrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var levelMins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            try
            {
                TryMergeEmbeddedMap(dict, prog, overrides, levelMins);

                var modDir = GetModDirectory();
                if (!string.IsNullOrEmpty(modDir))
                {
                    var path = Path.Combine(modDir, "ClassNameToCraftingSkillMap.xml");
                    if (File.Exists(path))
                        MergeMapFromXmlFile(path, dict, prog, overrides, levelMins);
                }

                _map = dict.Count > 0 ? dict : empty;
                _progressionMatchByClassName = prog.Count > 0 ? prog : emptyProg;
                _requiredLevelOverrideByClassName = overrides.Count > 0 ? overrides : emptyOv;
                _requiredLevelMinByClassName = levelMins.Count > 0 ? levelMins : emptyOv;
            }
            catch
            {
                _map = empty;
                _progressionMatchByClassName = emptyProg;
                _requiredLevelOverrideByClassName = emptyOv;
                _requiredLevelMinByClassName = emptyOv;
            }
        }

        private static void TryMergeEmbeddedMap(
            Dictionary<string, string> dict,
            Dictionary<string, string> prog,
            Dictionary<string, int> overrides,
            Dictionary<string, int> levelMins)
        {
            try
            {
                var asm = typeof(ClassNameToCraftingSkillMapLoader).Assembly;
                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (!name.EndsWith("ClassNameToCraftingSkillMap.xml", StringComparison.OrdinalIgnoreCase))
                        continue;
                    using (var stream = asm.GetManifestResourceStream(name))
                    {
                        if (stream == null) continue;
                        var doc = new XmlDocument();
                        doc.Load(stream);
                        MergeMapFromDocument(doc, dict, prog, overrides, levelMins);
                        return;
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private static void MergeMapFromXmlFile(
            string path,
            Dictionary<string, string> dict,
            Dictionary<string, string> prog,
            Dictionary<string, int> overrides,
            Dictionary<string, int> levelMins)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(path);
                MergeMapFromDocument(doc, dict, prog, overrides, levelMins);
            }
            catch
            {
                // ignored
            }
        }

        private static void MergeMapFromDocument(
            XmlDocument doc,
            Dictionary<string, string> dict,
            Dictionary<string, string> prog,
            Dictionary<string, int> overrides,
            Dictionary<string, int> levelMins)
        {
            var root = doc.DocumentElement;
            if (root == null) return;

            foreach (XmlNode node in root.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element || node.Name != "Item") continue;
                var className = node.Attributes?.GetNamedItem("className")?.Value?.Trim();
                var craftingSkillGroup = node.Attributes?.GetNamedItem("craftingSkillGroup")?.Value?.Trim();
                var progressionMatch = node.Attributes?.GetNamedItem("progressionMatchName")?.Value?.Trim();
                var levelOv = node.Attributes?.GetNamedItem("requiredLevelOverride")?.Value?.Trim();
                var levelMin = node.Attributes?.GetNamedItem("requiredLevelMin")?.Value?.Trim();
                if (string.IsNullOrEmpty(className)) continue;
                dict[className] = craftingSkillGroup ?? "";
                if (!string.IsNullOrEmpty(progressionMatch))
                    prog[className] = progressionMatch;
                if (!string.IsNullOrEmpty(levelOv) && int.TryParse(levelOv, out var lv) && lv > 0)
                    overrides[className] = lv;
                if (!string.IsNullOrEmpty(levelMin) && int.TryParse(levelMin, out var mn) && mn > 0)
                    levelMins[className] = mn;
            }
        }

        private static string GetModDirectory()
        {
            return ModContentRoot.ResolveModDirectory();
        }
    }
}
