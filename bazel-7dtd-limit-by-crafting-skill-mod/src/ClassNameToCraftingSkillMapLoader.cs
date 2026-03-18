using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Loads ClassNameToCraftingSkillMap.xml from the mod directory. Maps ItemClass type names to game-reported
    /// crafting skill names (e.g. "Bows", "Medical"). If the file is missing, returns an empty map so no items are restricted by map lookup.
    /// </summary>
    internal static class ClassNameToCraftingSkillMapLoader
    {
        private static IReadOnlyDictionary<string, string> _map;
        /// <summary>Optional: item name as it appears on Electrician (etc.) progression rows when it differs from ItemClass.Name.</summary>
        private static IReadOnlyDictionary<string, string> _progressionMatchByClassName;
        private static IReadOnlyDictionary<string, int> _requiredLevelOverrideByClassName;
        /// <summary>Minimum required level after progression resolve: effective = max(vanillaResolved, min).</summary>
        private static IReadOnlyDictionary<string, int> _requiredLevelMinByClassName;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the class name to crafting skill group map. Loaded once from ClassNameToCraftingSkillMap.xml in the mod directory.
        /// Returns empty dictionary if file is missing or invalid.
        /// </summary>
        public static IReadOnlyDictionary<string, string> GetMap()
        {
            EnsureLoaded();
            return _map;
        }

        /// <summary>
        /// When set in XML as progressionMatchName, use this string to find the item in progression DisplayData (craft unlock list).
        /// </summary>
        public static string GetProgressionMatchNameOrClassName(string className)
        {
            if (string.IsNullOrEmpty(className)) return className;
            return TryGetProgressionMatchOverride(className, out var pm) ? pm : className;
        }

        /// <summary>True when XML defines progressionMatchName for this className.</summary>
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

        /// <summary>When set, use this level if progression resolves to ≤0 (no match or tier 0). For floors when level is already &gt;0, use requiredLevelMin.</summary>
        public static bool TryGetRequiredLevelOverride(string className, out int level)
        {
            level = 0;
            if (string.IsNullOrEmpty(className)) return false;
            EnsureLoaded();
            return _requiredLevelOverrideByClassName != null &&
                   _requiredLevelOverrideByClassName.TryGetValue(className, out level) && level > 0;
        }

        /// <summary>When set, required level is at least this value after a successful progression resolve.</summary>
        public static bool TryGetRequiredLevelMin(string className, out int minLevel)
        {
            minLevel = 0;
            if (string.IsNullOrEmpty(className)) return false;
            EnsureLoaded();
            return _requiredLevelMinByClassName != null &&
                   _requiredLevelMinByClassName.TryGetValue(className, out minLevel) && minLevel > 0;
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
            var modDir = GetModDirectory();
            var empty = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var emptyProg = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var emptyOv = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(modDir))
            {
                _map = empty;
                _progressionMatchByClassName = emptyProg;
                _requiredLevelOverrideByClassName = emptyOv;
                _requiredLevelMinByClassName = emptyOv;
                return;
            }

            var path = Path.Combine(modDir, "ClassNameToCraftingSkillMap.xml");
            if (!File.Exists(path))
            {
                _map = empty;
                _progressionMatchByClassName = emptyProg;
                _requiredLevelOverrideByClassName = emptyOv;
                _requiredLevelMinByClassName = emptyOv;
                return;
            }

            try
            {
                var doc = new XmlDocument();
                doc.Load(path);
                var root = doc.DocumentElement;
                if (root == null)
                {
                    _map = empty;
                    _progressionMatchByClassName = emptyProg;
                    _requiredLevelOverrideByClassName = emptyOv;
                    _requiredLevelMinByClassName = emptyOv;
                    return;
                }

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var prog = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var overrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var levelMins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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
                _map = dict;
                _progressionMatchByClassName = prog;
                _requiredLevelOverrideByClassName = overrides;
                _requiredLevelMinByClassName = levelMins;
            }
            catch
            {
                _map = empty;
                _progressionMatchByClassName = emptyProg;
                _requiredLevelOverrideByClassName = emptyOv;
                _requiredLevelMinByClassName = emptyOv;
            }
        }

        private static string GetModDirectory()
        {
            try
            {
                var asm = typeof(ClassNameToCraftingSkillMapLoader).Assembly;
                var loc = asm.Location;
                if (!string.IsNullOrEmpty(loc)) return Path.GetDirectoryName(loc);
            }
            catch { }
            return null;
        }
    }
}
