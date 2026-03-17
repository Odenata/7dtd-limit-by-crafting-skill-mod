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
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the class name to crafting skill group map. Loaded once from ClassNameToCraftingSkillMap.xml in the mod directory.
        /// Returns empty dictionary if file is missing or invalid.
        /// </summary>
        public static IReadOnlyDictionary<string, string> GetMap()
        {
            if (_map != null) return _map;
            lock (_lock)
            {
                if (_map != null) return _map;
                _map = LoadMapCore();
            }
            return _map;
        }

        private static IReadOnlyDictionary<string, string> LoadMapCore()
        {
            var modDir = GetModDirectory();
            if (string.IsNullOrEmpty(modDir)) return new Dictionary<string, string>(0);

            var path = Path.Combine(modDir, "ClassNameToCraftingSkillMap.xml");
            if (!File.Exists(path)) return new Dictionary<string, string>(0);

            try
            {
                var doc = new XmlDocument();
                doc.Load(path);
                var root = doc.DocumentElement;
                if (root == null) return new Dictionary<string, string>(0);

                var dict = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (XmlNode node in root.ChildNodes)
                {
                    if (node.NodeType != XmlNodeType.Element || node.Name != "Item") continue;
                    var className = node.Attributes?.GetNamedItem("className")?.Value?.Trim();
                    var craftingSkillGroup = node.Attributes?.GetNamedItem("craftingSkillGroup")?.Value?.Trim();
                    if (string.IsNullOrEmpty(className)) continue;
                    dict[className] = craftingSkillGroup ?? "";
                }
                return dict;
            }
            catch
            {
                return new Dictionary<string, string>(0);
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
