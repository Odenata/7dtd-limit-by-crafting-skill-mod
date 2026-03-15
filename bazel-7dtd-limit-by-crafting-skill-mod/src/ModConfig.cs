using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Loads and exposes mod configuration from Config.xml.
    /// In multiplayer, server config should override; read source is documented in DESIGN.md.
    /// </summary>
    public sealed class ModConfig
    {
        private static ModConfig _instance;
        private static readonly object Lock = new object();

        public static ModConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Lock)
                    {
                        if (_instance == null)
                            _instance = Load();
                    }
                }
                return _instance;
            }
        }

        public bool DebugMode { get; private set; }

        /// <summary>
        /// Per crafting skill: when true, restriction is enabled for that skill.
        /// Key is skill name (e.g. "Armor", "HarvestingTools"). Unknown skills default to true if not in config.
        /// </summary>
        public IReadOnlyDictionary<string, bool> CraftingSkillEnabled { get; private set; }

        private ModConfig()
        {
            CraftingSkillEnabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }

        private static ModConfig Load()
        {
            var config = new ModConfig();
            try
            {
                var modDir = GetModDirectory();
                if (string.IsNullOrEmpty(modDir)) return config;

                var configPath = Path.Combine(modDir, "Config.xml");
                if (!File.Exists(configPath)) return config;

                var doc = new XmlDocument();
                doc.Load(configPath);
                var root = doc.DocumentElement;
                if (root == null) return config;

                var debugNode = root.SelectSingleNode("DebugMode");
                if (debugNode != null && bool.TryParse(debugNode.InnerText?.Trim(), out var debug))
                    config.DebugMode = debug;

                var skills = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                var skillsNode = root.SelectSingleNode("CraftingSkills");
                if (skillsNode != null)
                {
                    foreach (XmlNode child in skillsNode.ChildNodes)
                    {
                        if (child.NodeType != XmlNodeType.Element) continue;
                        var name = child.Name;
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        var enabled = true;
                        if (!string.IsNullOrWhiteSpace(child.InnerText) && bool.TryParse(child.InnerText.Trim(), out var parsed))
                            enabled = parsed;
                        skills[name] = enabled;
                    }
                }
                config.CraftingSkillEnabled = skills;
            }
            catch (Exception)
            {
                // Leave defaults
            }
            return config;
        }

        private static string GetModDirectory()
        {
            try
            {
                var asm = typeof(ModConfig).Assembly;
                var loc = asm.Location;
                if (!string.IsNullOrEmpty(loc)) return Path.GetDirectoryName(loc);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Returns whether restriction is enabled for the given crafting skill name.
        /// If the skill is not in config, returns true (restrict by default).
        /// </summary>
        public bool IsRestrictionEnabledForSkill(string craftingSkillGroup)
        {
            if (string.IsNullOrWhiteSpace(craftingSkillGroup)) return false;
            if (CraftingSkillEnabled.TryGetValue(craftingSkillGroup, out var enabled)) return enabled;
            return true;
        }
    }
}
