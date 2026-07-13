using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Loads and exposes mod configuration. Multiplayer clients can receive a server-provided snapshot
    /// that takes precedence over the local Config.xml until the connection ends.
    /// When Gears World settings are loaded, those values are preferred and mirrored back to Config.xml.
    /// </summary>
    public sealed class ModConfig
    {
        /// <summary>Skill names in Config.xml / ModSettings.xml order.</summary>
        public static readonly string[] CraftingSkillNames =
        {
            "Armor",
            "Bows",
            "Handguns",
            "Shotguns",
            "Rifles",
            "MachineGuns",
            "Explosives",
            "Blades",
            "Clubs",
            "Spears",
            "Sledgehammers",
            "Knuckles",
            "HarvestingTools",
            "RepairTools",
            "SalvageTools",
            "Seeds",
            "Traps",
            "Robotics",
            "Workstations",
            "Vehicles",
            "Electrician",
            "Medical",
            "Food",
        };

        private static ModConfig _instance;
        private static ModConfigSnapshot _serverSnapshot;
        private static readonly object Lock = new object();

        private Dictionary<string, bool> _craftingSkillEnabled;

        public static ModConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Lock)
                    {
                        if (_instance == null)
                            _instance = LoadEffective();
                    }
                }
                return _instance;
            }
        }

        /// <summary>Called when <see cref="ModContentRoot"/> learns the game mod path so Config.xml is re-read from disk.</summary>
        internal static void InvalidateReloadableInstance()
        {
            lock (Lock)
            {
                _instance = null;
            }
            RestrictionScan.ClearCaches();
        }

        public bool DebugMode { get; set; }
        public bool IsServerProvided { get; private set; }
        public string Source { get; private set; }
        public string Hash { get; private set; }

        /// <summary>
        /// Per crafting skill: when true, restriction is enabled for that skill.
        /// Key is skill name (e.g. "Armor", "HarvestingTools"). Unknown skills default to true if not in config.
        /// </summary>
        public IReadOnlyDictionary<string, bool> CraftingSkillEnabled => _craftingSkillEnabled;

        private ModConfig()
        {
            _craftingSkillEnabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            Source = "defaults";
            Hash = "";
        }

        private ModConfig(ModConfigSnapshot snapshot, bool isServerProvided)
        {
            if (snapshot == null) snapshot = ModConfigSnapshot.Empty("defaults");
            DebugMode = snapshot.DebugMode;
            _craftingSkillEnabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (snapshot.CraftingSkillEnabled != null)
            {
                foreach (var pair in snapshot.CraftingSkillEnabled)
                    _craftingSkillEnabled[pair.Key] = pair.Value;
            }
            IsServerProvided = isServerProvided;
            Source = snapshot.Source;
            Hash = snapshot.Hash;
        }

        private static ModConfig LoadEffective()
        {
            lock (Lock)
            {
                if (_serverSnapshot != null)
                    return new ModConfig(_serverSnapshot, true);
            }

            return new ModConfig(LoadLocalSnapshot(), false);
        }

        internal static ModConfigSnapshot LoadLocalSnapshot()
        {
            try
            {
                var modDir = GetModDirectory();
                if (string.IsNullOrEmpty(modDir)) return ModConfigSnapshot.Empty("missing mod directory");

                var configPath = Path.Combine(modDir, "Config.xml");
                return ModConfigSnapshot.FromFile(configPath);
            }
            catch (Exception)
            {
                // Leave defaults
                return ModConfigSnapshot.Empty("config load error");
            }
        }

        private static string GetModDirectory()
        {
            return ModContentRoot.ResolveModDirectory();
        }

        internal static bool TryApplyServerXml(string xml, string source, out string error)
        {
            error = null;
            try
            {
                var snapshot = ModConfigSnapshot.FromXml(xml, string.IsNullOrWhiteSpace(source) ? "server" : source);
                lock (Lock)
                {
                    _serverSnapshot = snapshot;
                    _instance = new ModConfig(snapshot, true);
                }
                RestrictionScan.ClearCaches();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        internal static void ClearServerSnapshot()
        {
            lock (Lock)
            {
                _serverSnapshot = null;
                _instance = null;
            }
        }

        internal static string GetLocalConfigXmlForSync()
        {
            return LoadLocalSnapshot().ToXmlString();
        }

        /// <summary>
        /// World-synced crafting-skill toggles (Gears World settings when present).
        /// Mutates the live instance even when a server snapshot was applied.
        /// </summary>
        public void ApplyWorldSettings(IDictionary<string, bool> skills)
        {
            if (skills == null)
                return;

            var changed = false;
            foreach (var pair in skills)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;
                if (_craftingSkillEnabled.TryGetValue(pair.Key, out var existing) && existing == pair.Value)
                    continue;
                _craftingSkillEnabled[pair.Key] = pair.Value;
                changed = true;
            }

            if (changed)
                RestrictionScan.ClearCaches();
        }

        /// <summary>Per-client Global knobs (debug logging).</summary>
        public void ApplyGlobalSettings(bool debugMode)
        {
            DebugMode = debugMode;
        }

        /// <summary>
        /// Persist current Instance to Config.xml (best-effort mirror for non-Gears / ServerConfigSync).
        /// </summary>
        public void Save()
        {
            try
            {
                var modDir = GetModDirectory();
                if (string.IsNullOrEmpty(modDir))
                    return;

                var configPath = Path.Combine(modDir, "Config.xml");
                Directory.CreateDirectory(modDir);

                var sb = new StringBuilder();
                sb.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
                sb.AppendLine("<LimitByCraftingSkillModConfig>");
                sb.AppendLine("  <!-- One toggle per crafting skill. When true, items requiring that skill are restricted by player level. -->");
                sb.AppendLine("  <!-- Multiplayer: when connected to a server running this mod, clients use the server's Config.xml. -->");
                sb.AppendLine("  <CraftingSkills>");
                foreach (var name in CraftingSkillNames)
                {
                    bool enabled;
                    if (!_craftingSkillEnabled.TryGetValue(name, out enabled))
                    {
                        // Match shipped Config.xml defaults when a key was never set.
                        enabled = !(string.Equals(name, "Medical", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(name, "Food", StringComparison.OrdinalIgnoreCase));
                    }
                    sb.AppendLine($"    <{name}>{(enabled ? "true" : "false")}</{name}>");
                }
                sb.AppendLine("  </CraftingSkills>");
                sb.AppendLine("  <!-- If true, writes extra info to the game log (for troubleshooting). Values: true, false -->");
                sb.AppendLine($"  <DebugMode>{(DebugMode ? "true" : "false")}</DebugMode>");
                sb.AppendLine("</LimitByCraftingSkillModConfig>");

                File.WriteAllText(configPath, sb.ToString());
                SafeLog($"LimitByCraftingSkillMod config saved");
            }
            catch (Exception ex)
            {
                SafeLog($"LimitByCraftingSkillMod config save failed: {ex.Message}");
            }
        }

        /// <summary>Parse Gears Switch / string values into bool (true/false, enabled/disabled, etc.).</summary>
        public static bool ParseBoolSetting(string value, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            var v = value.Trim();
            if (bool.TryParse(v, out var parsed))
                return parsed;

            switch (v.ToLowerInvariant())
            {
                case "1":
                case "yes":
                case "on":
                case "enable":
                case "enabled":
                    return true;
                case "0":
                case "no":
                case "off":
                case "disable":
                case "disabled":
                    return false;
                default:
                    return defaultValue;
            }
        }

        /// <summary>
        /// Returns whether restriction is enabled for the given crafting skill name.
        /// If the skill is not in config, returns true (restrict by default).
        /// When Gears World is loaded, re-reads World CurrentValues first.
        /// </summary>
        public bool IsRestrictionEnabledForSkill(string craftingSkillGroup)
        {
            GearsModApi.SyncWorldSettingsIfLoaded();
            return GetSkillRestrictionEnabled(craftingSkillGroup);
        }

        /// <summary>Lookup without Gears World sync (used while applying Gears values).</summary>
        internal bool GetSkillRestrictionEnabled(string craftingSkillGroup)
        {
            if (string.IsNullOrWhiteSpace(craftingSkillGroup)) return false;
            if (_craftingSkillEnabled.TryGetValue(craftingSkillGroup, out var enabled)) return enabled;
            return true;
        }

        private static void SafeLog(string message)
        {
            try
            {
                UnityEngine.Debug.Log($"[LimitByCraftingSkillMod] {message}");
            }
            catch
            {
                // Unity not available (test environment)
            }
        }
    }
}
