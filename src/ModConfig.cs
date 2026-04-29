using System;
using System.Collections.Generic;
using System.IO;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Loads and exposes mod configuration. Multiplayer clients can receive a server-provided snapshot
    /// that takes precedence over the local Config.xml until the connection ends.
    /// </summary>
    public sealed class ModConfig
    {
        private static ModConfig _instance;
        private static ModConfigSnapshot _serverSnapshot;
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
        }

        public bool DebugMode { get; private set; }
        public bool IsServerProvided { get; private set; }
        public string Source { get; private set; }
        public string Hash { get; private set; }

        /// <summary>
        /// Per crafting skill: when true, restriction is enabled for that skill.
        /// Key is skill name (e.g. "Armor", "HarvestingTools"). Unknown skills default to true if not in config.
        /// </summary>
        public IReadOnlyDictionary<string, bool> CraftingSkillEnabled { get; private set; }

        private ModConfig()
        {
            CraftingSkillEnabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            Source = "defaults";
            Hash = "";
        }

        private ModConfig(ModConfigSnapshot snapshot, bool isServerProvided)
        {
            if (snapshot == null) snapshot = ModConfigSnapshot.Empty("defaults");
            DebugMode = snapshot.DebugMode;
            CraftingSkillEnabled = snapshot.CraftingSkillEnabled ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
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
