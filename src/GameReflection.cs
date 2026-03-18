using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Reflection-based access to game types for player crafting level and item required level.
    /// Hides member names so the mod can tolerate game version drift; see docs/GAME_API_NOTES.md and RUNTIME_API_MISMATCH_DEBUGGING in dev-tools.
    /// </summary>
    internal static class GameReflection
    {
        /// <summary>
        /// Gets the local player entity. Uses reflection; tries GameManager.Instance as static field then property,
        /// then myEntityPlayerLocal as field then property (game API may expose either).
        /// </summary>
        internal static EntityAlive GetLocalPlayer()
        {
            try
            {
                var gmType = typeof(GameManager);
                object gm = null;
                var instField = gmType.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (instField != null)
                    gm = instField.GetValue(null);
                if (gm == null)
                {
                    var instProp = gmType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (instProp != null)
                        gm = instProp.GetValue(null, null);
                }
                if (gm == null) return null;
                var gmT = gm.GetType();
                var playerField = gmT.GetField("myEntityPlayerLocal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? gmT.GetField("MyEntityPlayerLocal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (playerField != null)
                {
                    var p = playerField.GetValue(gm);
                    if (p is EntityAlive ea) return ea;
                }
                var playerProp = gmT.GetProperty("myEntityPlayerLocal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? gmT.GetProperty("MyEntityPlayerLocal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return playerProp?.GetValue(gm, null) as EntityAlive;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Maps the game's item/group name to the config key only (e.g. Config.xml uses "Armor").
        /// Use this for IsRestrictionEnabledForSkill.
        /// </summary>
        internal static string ToProgressionOrConfigName(string gameCraftingSkillGroup)
        {
            if (string.IsNullOrWhiteSpace(gameCraftingSkillGroup)) return gameCraftingSkillGroup;
            if (string.Equals(gameCraftingSkillGroup, "Clothing", System.StringComparison.OrdinalIgnoreCase))
                return "Armor";
            if (string.Equals(gameCraftingSkillGroup, "Tools", System.StringComparison.OrdinalIgnoreCase))
                return "HarvestingTools";
            if (string.Equals(gameCraftingSkillGroup, "Ammo", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(gameCraftingSkillGroup, "Weapons", System.StringComparison.OrdinalIgnoreCase))
                return "Weapons";
            return gameCraftingSkillGroup;
        }

        /// <summary>
        /// Returns the name used in Progression.GetProgressionValue / ProgressionValueQuickList for level lookup.
        /// ItemClass may report "Clothing" but the game stores the level under "craftingarmor". See 7dtd-mod-dev-tools docs/PROGRESSION_NAMES.md.
        /// </summary>
        internal static string ToProgressionLookupName(string gameCraftingSkillGroup)
        {
            if (string.IsNullOrWhiteSpace(gameCraftingSkillGroup)) return gameCraftingSkillGroup;
            if (string.Equals(gameCraftingSkillGroup, "Clothing", System.StringComparison.OrdinalIgnoreCase))
                return "craftingarmor";
            if (string.Equals(gameCraftingSkillGroup, "HarvestingTools", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(gameCraftingSkillGroup, "Harvesting Tool", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(gameCraftingSkillGroup, "Harvesting Tools", System.StringComparison.OrdinalIgnoreCase))
                return "craftingharvestingtools";
            if (string.Equals(gameCraftingSkillGroup, "Bows", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(gameCraftingSkillGroup, "Bow", System.StringComparison.OrdinalIgnoreCase))
                return "craftingbows";
            if (string.Equals(gameCraftingSkillGroup, "RepairTools", System.StringComparison.OrdinalIgnoreCase))
                return "craftingrepairtools";
            if (string.Equals(gameCraftingSkillGroup, "SalvageTools", System.StringComparison.OrdinalIgnoreCase))
                return "craftingsalvagetools";
            if (string.Equals(gameCraftingSkillGroup, "Clubs", System.StringComparison.OrdinalIgnoreCase))
                return "craftingclubs";
            if (string.Equals(gameCraftingSkillGroup, "Sledgehammers", System.StringComparison.OrdinalIgnoreCase))
                return "craftingsledgehammers";
            if (string.Equals(gameCraftingSkillGroup, "Spears", System.StringComparison.OrdinalIgnoreCase))
                return "craftingspears";
            if (string.Equals(gameCraftingSkillGroup, "Handguns", System.StringComparison.OrdinalIgnoreCase))
                return "craftinghandguns";
            if (string.Equals(gameCraftingSkillGroup, "Shotguns", System.StringComparison.OrdinalIgnoreCase))
                return "craftingshotguns";
            if (string.Equals(gameCraftingSkillGroup, "Rifles", System.StringComparison.OrdinalIgnoreCase))
                return "craftingrifles";
            if (string.Equals(gameCraftingSkillGroup, "MachineGuns", System.StringComparison.OrdinalIgnoreCase))
                return "craftingmachineguns";
            if (string.Equals(gameCraftingSkillGroup, "Explosives", System.StringComparison.OrdinalIgnoreCase))
                return "craftingexplosives";
            if (string.Equals(gameCraftingSkillGroup, "Robotics", System.StringComparison.OrdinalIgnoreCase))
                return "craftingrobotics";
            if (string.Equals(gameCraftingSkillGroup, "Medical", System.StringComparison.OrdinalIgnoreCase))
                return "craftingmedical";
            if (string.Equals(gameCraftingSkillGroup, "Food", System.StringComparison.OrdinalIgnoreCase))
                return "craftingfood";
            if (string.Equals(gameCraftingSkillGroup, "Seeds", System.StringComparison.OrdinalIgnoreCase))
                return "craftingseeds";
            if (string.Equals(gameCraftingSkillGroup, "Traps", System.StringComparison.OrdinalIgnoreCase))
                return "craftingtraps";
            if (string.Equals(gameCraftingSkillGroup, "Tools", System.StringComparison.OrdinalIgnoreCase))
                return "craftingharvestingtools";
            if (string.Equals(gameCraftingSkillGroup, "Workstations", System.StringComparison.OrdinalIgnoreCase))
                return "craftingworkstations";
            if (string.Equals(gameCraftingSkillGroup, "Vehicles", System.StringComparison.OrdinalIgnoreCase))
                return "craftingvehicles";
            if (string.Equals(gameCraftingSkillGroup, "Blades", System.StringComparison.OrdinalIgnoreCase))
                return "craftingblades";
            if (string.Equals(gameCraftingSkillGroup, "Knuckles", System.StringComparison.OrdinalIgnoreCase))
                return "craftingknuckles";
            if (string.Equals(gameCraftingSkillGroup, "Electrician", System.StringComparison.OrdinalIgnoreCase))
                return "craftingelectrician";
            // Fallback: game often uses "crafting" + lowercase group name (no spaces)
            var normalized = gameCraftingSkillGroup.Trim().ToLowerInvariant().Replace(" ", "");
            if (normalized.Length > 0)
                return "crafting" + normalized;
            return gameCraftingSkillGroup;
        }

        /// <summary>
        /// Gets the player's current level for the given crafting skill group name.
        /// Tries GetProgressionValue with progression lookup name (e.g. "craftingarmor"), then raw name, then config name.
        /// Reads level from Level property or level field. If both fail, scans ProgressionValueQuickList by name.
        /// Returns 0 if the entity or skill cannot be resolved (e.g. in mocks).
        /// </summary>
        internal static int GetPlayerCraftingLevel(EntityAlive entity, string craftingSkillGroup)
        {
            if (entity == null || string.IsNullOrWhiteSpace(craftingSkillGroup)) return 0;
            try
            {
                object progression = GetProgression(entity);
                if (progression == null) return 0;
                var progType = progression.GetType();
                var getValueMethod = progType.GetMethod("GetProgressionValue", new[] { typeof(string) });
                if (getValueMethod == null) return GetLevelFromQuickList(progression, craftingSkillGroup);

                var lookupName = ToProgressionLookupName(craftingSkillGroup);
                if (string.IsNullOrWhiteSpace(lookupName)) return 0;
                var namesToTry = new[] { lookupName, lookupName.ToLowerInvariant(), craftingSkillGroup, ToProgressionOrConfigName(craftingSkillGroup) };
                foreach (var name in namesToTry)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var pv = getValueMethod.Invoke(progression, new object[] { name });
                    if (pv == null) continue;
                    var level = GetLevelFromProgressionValue(pv);
                    if (level >= 0) return level;
                }
                return GetLevelFromQuickList(progression, craftingSkillGroup);
            }
            catch
            {
                return 0;
            }
        }

        private static object GetProgression(EntityAlive entity)
        {
            if (entity == null) return null;
            var entityType = entity.GetType();
            var progressionField = entityType.GetField("Progression", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (progressionField != null)
            {
                var v = progressionField.GetValue(entity);
                if (v != null) return v;
            }
            var progressionProp = entityType.GetProperty("Progression", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return progressionProp?.GetValue(entity, null);
        }

        /// <summary>Gets the ProgressionClass for a skill by getting the ProgressionValue then its ProgressionClass (or cachedProgressionClass). Used when Progression.ProgressionClasses is not present at runtime.</summary>
        private static object GetProgressionClassFromProgressionValue(object progression, string lookupName)
        {
            if (progression == null || string.IsNullOrEmpty(lookupName)) return null;
            try
            {
                var progType = progression.GetType();
                var getPvMethod = progType.GetMethod("GetProgressionValue", new[] { typeof(string) });
                if (getPvMethod == null) return null;
                var pv = getPvMethod.Invoke(progression, new object[] { lookupName });
                if (pv == null) return null;
                var pvType = pv.GetType();
                var pcProp = pvType.GetProperty("ProgressionClass", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pcProp != null)
                {
                    var pc = pcProp.GetValue(pv, null);
                    if (pc != null) return pc;
                }
                var pcField = pvType.GetField("cachedProgressionClass", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pcField != null)
                {
                    var pc = pcField.GetValue(pv);
                    if (pc != null) return pc;
                }
                return null;
            }
            catch { return null; }
        }

        private static int GetLevelFromProgressionValue(object pv)
        {
            if (pv == null) return -1;
            var pvType = pv.GetType();
            var levelProp = pvType.GetProperty("Level", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (levelProp != null)
            {
                var v = levelProp.GetValue(pv, null);
                if (v is int i) return i;
            }
            var levelField = pvType.GetField("level", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (levelField != null)
            {
                var v = levelField.GetValue(pv);
                if (v is int j) return j;
            }
            return -1;
        }

        private static int GetLevelFromQuickList(object progression, string craftingSkillGroup)
        {
            if (progression == null || string.IsNullOrWhiteSpace(craftingSkillGroup)) return 0;
            try
            {
                var lookupName = ToProgressionLookupName(craftingSkillGroup);
                var configName = ToProgressionOrConfigName(craftingSkillGroup);
                var quickListField = progression.GetType().GetField("ProgressionValueQuickList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var list = quickListField?.GetValue(progression) as IList;
                if (list == null) return 0;
                var namesToMatch = new[] { lookupName, craftingSkillGroup, configName };
                foreach (var names in namesToMatch)
                {
                    if (string.IsNullOrWhiteSpace(names)) continue;
                    for (var i = 0; i < list.Count; i++)
                    {
                        var pv = list[i];
                        if (pv == null) continue;
                        var pvName = GetProgressionValueName(pv);
                        if (!string.Equals(pvName, names, StringComparison.OrdinalIgnoreCase)) continue;
                        var level = GetLevelFromProgressionValue(pv);
                        if (level >= 0) return level;
                    }
                }
            }
            catch { }
            return 0;
        }

        private static string GetProgressionValueName(object pv)
        {
            if (pv == null) return null;
            var t = pv.GetType();
            var prop = t.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
            {
                var v = prop.GetValue(pv, null);
                if (v is string s) return s;
            }
            var field = t.GetField("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(pv) as string;
        }

        /// <summary>True if the progression value is a crafting skill (e.g. craftingarmor). Used to set RestrictionColorsDirty on level-up.</summary>
        internal static bool IsCraftingProgressionValue(object pv)
        {
            var name = GetProgressionValueName(pv);
            return !string.IsNullOrEmpty(name) && name.IndexOf("crafting", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Returns the key used for ClassNameToCraftingSkillMap lookup: item name (from XML) then fallback to C# type name.
        /// Uses reflection for Name/pName to tolerate API drift. Aligns with what the dev-inspector shows for item identity.
        /// </summary>
        internal static string GetItemClassNameForMap(ItemClass itemClass)
        {
            if (itemClass == null) return null;
            var t = itemClass.GetType();
            var nameProp = t.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (nameProp != null)
            {
                try
                {
                    var v = nameProp.GetValue(itemClass, null);
                    if (v is string s && !string.IsNullOrWhiteSpace(s)) return s.Trim();
                }
                catch { }
            }
            var nameField = t.GetField("pName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (nameField != null)
            {
                try
                {
                    var v = nameField.GetValue(itemClass);
                    if (v is string s && !string.IsNullOrWhiteSpace(s)) return s.Trim();
                }
                catch { }
            }
            return itemClass.GetType().Name;
        }

        /// <summary>
        /// Gets the crafting skill group for restrictions. First ClassNameToCraftingSkillMap.xml (case-insensitive key).
        /// If unmapped, uses the game's <c>ItemClass.CraftingSkillGroup</c> when it is Electrician, Workstations, HarvestingTools, or Tools (maps to HarvestingTools).
        /// </summary>
        internal static string GetCraftingSkillGroup(ItemClass itemClass)
        {
            if (itemClass == null) return null;
            var mapKey = GetItemClassNameForMap(itemClass);
            if (!string.IsNullOrWhiteSpace(mapKey))
            {
                var map = ClassNameToCraftingSkillMapLoader.GetMap();
                if (map.TryGetValue(mapKey, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
                    return mapped.Trim();
            }
            return TrySkillGroupFromItemClassCraftingSkillGroup(itemClass);
        }

        /// <summary>
        /// Reads the game's CraftingSkillGroup field/property; returns a normalized group only for electrician/harvesting/workstation coverage.
        /// </summary>
        private static string TrySkillGroupFromItemClassCraftingSkillGroup(ItemClass itemClass)
        {
            var raw = ReadItemClassCraftingSkillGroupRaw(itemClass);
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Trim();
            if (string.Equals(s, "Tools", StringComparison.OrdinalIgnoreCase))
                return "HarvestingTools";
            if (string.Equals(s, "HarvestingTools", StringComparison.OrdinalIgnoreCase))
                return "HarvestingTools";
            if (string.Equals(s, "Electrician", StringComparison.OrdinalIgnoreCase))
                return "Electrician";
            if (string.Equals(s, "Workstations", StringComparison.OrdinalIgnoreCase))
                return "Workstations";
            return null;
        }

        private static string ReadItemClassCraftingSkillGroupRaw(ItemClass itemClass)
        {
            if (itemClass == null) return null;
            try
            {
                var t = itemClass.GetType();
                var f = t.GetField("CraftingSkillGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null)
                {
                    var v = f.GetValue(itemClass) as string;
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
                var p = t.GetProperty("CraftingSkillGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null)
                {
                    try
                    {
                        var v = p.GetValue(itemClass, null) as string;
                        if (!string.IsNullOrWhiteSpace(v)) return v;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Gets the quality value for the item (0 if no quality). Uses reflection for game compatibility.
        /// </summary>
        internal static int GetQuality(ItemValue itemValue)
        {
            if (itemValue == null) return 0;
            var t = itemValue.GetType();
            var f = t.GetField("Quality", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                var v = f.GetValue(itemValue);
                if (v is ushort u) return u;
                if (v is int i) return i;
            }
            var p = t.GetProperty("Quality", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null)
            {
                var v = p.GetValue(itemValue, null);
                if (v is ushort u) return u;
                if (v is int i) return i;
            }
            return 0;
        }

        /// <summary>
        /// Returns whether the item has a quality attribute (affects whether we apply level restriction).
        /// </summary>
        internal static bool HasQuality(ItemValue itemValue)
        {
            if (itemValue == null) return false;
            var t = itemValue.GetType();
            var p = t.GetProperty("HasQuality", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null)
            {
                var v = p.GetValue(itemValue, null);
                return v is bool b && b;
            }
            return GetQuality(itemValue) > 0;
        }

        /// <summary>
        /// Skills whose handheld/placeable items often have no ItemValue quality; use progression tier 1 for required level.
        /// </summary>
        internal static bool UsesSyntheticQualityTierForRequiredLevel(string skillGroup)
        {
            if (string.IsNullOrWhiteSpace(skillGroup)) return false;
            return string.Equals(skillGroup, "Electrician", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(skillGroup, "Workstations", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(skillGroup, "HarvestingTools", StringComparison.OrdinalIgnoreCase);
        }

        private static readonly HashSet<string> _debugLoggedNoMapKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static void LogGetRequiredLevelExit0(string mapKey, string skillGroup, bool hasQuality, int rawQuality, string reason)
        {
            try
            {
                if (ModConfig.Instance == null || !ModConfig.Instance.DebugMode) return;
                if (reason == "no_map")
                {
                    var k = mapKey ?? "";
                    lock (_debugLoggedNoMapKeys)
                    {
                        if (!_debugLoggedNoMapKeys.Add(k)) return;
                    }
                }
                ModApi.DebugLog("[LimitByCraftingSkill] GetRequiredLevelForItem exit=0 reason=" + reason
                    + " mapKey=" + (mapKey ?? "")
                    + " skillGroup=" + (skillGroup ?? "")
                    + " hasQuality=" + hasQuality + " rawQuality=" + rawQuality);
            }
            catch { }
        }

        /// <summary>
        /// Gets the minimum crafting level required to use this item at its current quality (or tier 1 for Electrician / Workstations / HarvestingTools when the item has no quality).
        /// </summary>
        internal static int GetRequiredLevelForItem(ItemClass itemClass, ItemValue itemValue)
        {
            var entity = GetLocalPlayer();
            if (entity == null)
                return GetRequiredLevelForItemWithProgression(itemClass, itemValue, null, missingProgressionReason: "no_player");
            var progression = GetProgression(entity);
            if (progression == null)
                return GetRequiredLevelForItemWithProgression(itemClass, itemValue, null, missingProgressionReason: "no_progression");
            return GetRequiredLevelForItemWithProgression(itemClass, itemValue, progression, missingProgressionReason: null);
        }

        /// <summary>
        /// Unit tests: supply a fake progression with GetProgressionValue(string) returning an object that has ProgressionClass.DisplayDataList.
        /// </summary>
        internal static int GetRequiredLevelForItemForUnitTest(ItemClass itemClass, ItemValue itemValue, object progression)
        {
            return GetRequiredLevelForItemWithProgression(itemClass, itemValue, progression,
                progression == null ? "no_progression" : null);
        }

        /// <param name="missingProgressionReason">If non-null, progression is null and this is the log reason (no_player / no_progression).</param>
        private static int GetRequiredLevelForItemWithProgression(ItemClass itemClass, ItemValue itemValue, object progression, string missingProgressionReason)
        {
            if (itemClass == null || itemValue == null) return 0;
            var mapKey = GetItemClassNameForMap(itemClass);
            var skillGroup = GetCraftingSkillGroup(itemClass);
            if (string.IsNullOrWhiteSpace(skillGroup))
            {
                LogGetRequiredLevelExit0(mapKey, null, HasQuality(itemValue), GetQuality(itemValue), "no_map");
                return 0;
            }

            var hasQ = HasQuality(itemValue);
            var rawQ = GetQuality(itemValue);
            int effectiveQuality;
            if (hasQ && rawQ > 0)
                effectiveQuality = rawQ;
            else if (UsesSyntheticQualityTierForRequiredLevel(skillGroup))
                effectiveQuality = 1;
            else
            {
                LogGetRequiredLevelExit0(mapKey, skillGroup, hasQ, rawQ, "no_quality");
                return 0;
            }

            if (progression == null)
            {
                if (missingProgressionReason != null)
                    LogGetRequiredLevelExit0(mapKey, skillGroup, hasQ, rawQ, missingProgressionReason);
                return 0;
            }
            var lookupName = ToProgressionLookupName(skillGroup);
            if (string.IsNullOrWhiteSpace(lookupName))
            {
                LogGetRequiredLevelExit0(mapKey, skillGroup, hasQ, rawQ, "no_lookup");
                return 0;
            }

            try
            {
                var mapKeyName = GetItemClassNameForMap(itemClass);
                var resolvedLevel = TryResolveRequiredLevelInTree(progression, lookupName, itemClass, mapKeyName, effectiveQuality);
                // Electrician placeables (e.g. powered garage) may match craftingelectrician at tier 0 while the real
                // unlock tier lives under craftingworkstations — always take the higher of the two trees.
                if (string.Equals(skillGroup, "Electrician", StringComparison.OrdinalIgnoreCase))
                {
                    var wsLevel = TryResolveRequiredLevelInTree(progression, "craftingworkstations", itemClass, mapKeyName, effectiveQuality);
                    if (wsLevel > resolvedLevel)
                    {
                        resolvedLevel = wsLevel;
                        if (AgentDebugSessionLog.IsTraceMapKey(mapKeyName))
                            AgentDebugSessionLog.WriteWorkstationFallback(mapKeyName, resolvedLevel);
                    }
                }
                // Override applies when progression gives no positive gate (including matched row at tier 0).
                if (resolvedLevel <= 0 && ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelOverride(mapKeyName, out var ovLevel))
                    resolvedLevel = ovLevel;
                if (resolvedLevel >= 0)
                {
                    if (ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelMin(mapKeyName, out var minLv))
                        resolvedLevel = System.Math.Max(resolvedLevel, minLv);
                    return resolvedLevel;
                }

                if (AgentDebugSessionLog.IsTraceMapKey(mapKeyName))
                {
                    var pc = GetProgressionClassFromProgressionValue(progression, lookupName);
                    IList ddl = null;
                    if (pc != null)
                    {
                        var f = pc.GetType().GetField("DisplayDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        ddl = f?.GetValue(pc) as IList;
                    }
                    var samples = ddl != null ? CollectSampleProgressionItemNames(ddl, 24) : new List<string>();
                    var candidates = BuildProgressionMatchCandidates(mapKeyName);
                    var hasXml = ClassNameToCraftingSkillMapLoader.TryGetProgressionMatchOverride(mapKeyName, out _);
                    AgentDebugSessionLog.WriteProgressionProbe(mapKeyName, lookupName + "|tried_ws", string.Join("|", candidates), hasXml, string.Join("|", samples));
                }

                LogGetRequiredLevelExit0(mapKey, skillGroup, hasQ, rawQ, "no_progression_match");
                return 0;
            }
            catch
            {
                LogGetRequiredLevelExit0(mapKey, skillGroup, hasQ, rawQ, "exception");
                return 0;
            }
        }

        private static int TryResolveRequiredLevelInTree(object progression, string progressionLookupName, ItemClass itemClass, string mapKeyName, int effectiveQuality)
        {
            if (progression == null || string.IsNullOrEmpty(progressionLookupName)) return -1;
            var progressionClass = GetProgressionClassFromProgressionValue(progression, progressionLookupName);
            if (progressionClass == null) return -1;
            var displayDataListField = progressionClass.GetType().GetField("DisplayDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var displayDataList = displayDataListField?.GetValue(progressionClass) as IList;
            if (displayDataList == null || displayDataList.Count == 0) return -1;
            var candidates = BuildProgressionMatchCandidates(mapKeyName);
            var poweredIronGarage = IsPoweredIronGarageMapKey(mapKeyName);
            var best = -1;
            foreach (var itemNameForMatch in candidates)
            {
                var resolved = poweredIronGarage
                    ? TryResolveRequiredLevelWithProgressionNameMax(itemClass, displayDataList, effectiveQuality, itemNameForMatch)
                    : TryResolveRequiredLevelWithProgressionName(itemClass, displayDataList, effectiveQuality, itemNameForMatch);
                if (!poweredIronGarage && resolved >= 0)
                    return resolved;
                if (resolved > best)
                    best = resolved;
            }
            if (best >= 0)
                return best;
            var garageFb = TryResolvePoweredGarageViaUnlockScan(displayDataList, effectiveQuality, mapKeyName);
            return garageFb >= 0 ? garageFb : -1;
        }

        /// <summary>
        /// Powered iron garage placeables often hit a parent DisplayData row with QualityStarts[0]==0 before a specific row
        /// with a higher tier; take the maximum required level across all matching rows.
        /// </summary>
        private static bool IsPoweredIronGarageMapKey(string mapKeyName)
        {
            if (string.IsNullOrEmpty(mapKeyName)) return false;
            if (mapKeyName.IndexOf("garagedoor", StringComparison.OrdinalIgnoreCase) < 0) return false;
            if (mapKeyName.IndexOf("powered", StringComparison.OrdinalIgnoreCase) < 0) return false;
            return mapKeyName.StartsWith("ironGarageDoor_", StringComparison.OrdinalIgnoreCase)
                   && !mapKeyName.StartsWith("ironGarageDoor01_", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Like <see cref="TryResolveRequiredLevelWithProgressionName"/> but returns the maximum level among all matching DisplayData rows.</summary>
        private static int TryResolveRequiredLevelWithProgressionNameMax(ItemClass itemClass, IList displayDataList, int effectiveQuality, string itemNameForMatch)
        {
            var best = -1;
            for (int i = 0; i < displayDataList.Count; i++)
            {
                object displayData = displayDataList[i];
                if (displayData == null) continue;
                if (!DisplayDataMatchesItem(displayData, itemClass, itemNameForMatch)) continue;
                var lvl = GetRequiredLevelFromDisplayData(displayData, effectiveQuality);
                if (lvl > best) best = lvl;
            }
            for (int i = 0; i < displayDataList.Count; i++)
            {
                object displayData = displayDataList[i];
                if (displayData == null) continue;
                var unlockListField = displayData.GetType().GetField("UnlockDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var unlockList = unlockListField?.GetValue(displayData) as IList;
                var count = unlockList?.Count ?? 0;
                if (count == 0)
                {
                    var mGetUd = displayData.GetType().GetMethod("GetUnlockData", new[] { typeof(int) });
                    if (mGetUd != null)
                    {
                        for (var u = 0; u < 256; u++)
                        {
                            object ud = null;
                            try { ud = mGetUd.Invoke(displayData, new object[] { u }); } catch { break; }
                            if (ud == null) break;
                            if (!UnlockEntryMatchesCraftItem(displayData, u, ud, itemClass, itemNameForMatch)) continue;
                            var lvl = GetRequiredLevelFromDisplayData(displayData, effectiveQuality);
                            if (lvl > best) best = lvl;
                        }
                    }
                    continue;
                }
                for (var u = 0; u < count; u++)
                {
                    var ud = unlockList[u];
                    if (!UnlockEntryMatchesCraftItem(displayData, u, ud, itemClass, itemNameForMatch)) continue;
                    var lvl = GetRequiredLevelFromDisplayData(displayData, effectiveQuality);
                    if (lvl > best) best = lvl;
                }
            }
            return best;
        }

        private static List<string> BuildProgressionMatchCandidates(string mapKeyName)
        {
            var list = new List<string>();
            void Add(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return;
                foreach (var e in list)
                    if (string.Equals(e, s, StringComparison.OrdinalIgnoreCase)) return;
                list.Add(s);
            }
            if (string.IsNullOrEmpty(mapKeyName)) return list;
            if (ClassNameToCraftingSkillMapLoader.TryGetProgressionMatchOverride(mapKeyName, out var ex)) Add(ex);
            Add(mapKeyName);
            if (mapKeyName.StartsWith("ironGarageDoor_", StringComparison.OrdinalIgnoreCase) &&
                !mapKeyName.StartsWith("ironGarageDoor01_", StringComparison.OrdinalIgnoreCase))
                Add("ironGarageDoor01_" + mapKeyName.Substring("ironGarageDoor_".Length));
            if (mapKeyName.StartsWith("woodenGarageDoor3x3_", StringComparison.OrdinalIgnoreCase) &&
                mapKeyName.IndexOf("woodenGarageDoor01_3x3_", StringComparison.OrdinalIgnoreCase) < 0)
                Add("woodenGarageDoor01_3x3_" + mapKeyName.Substring("woodenGarageDoor3x3_".Length));
            return list;
        }

        private static int TryResolveRequiredLevelWithProgressionName(ItemClass itemClass, IList displayDataList, int effectiveQuality, string itemNameForMatch)
        {
            for (int i = 0; i < displayDataList.Count; i++)
            {
                object displayData = displayDataList[i];
                if (displayData == null) continue;
                if (!DisplayDataMatchesItem(displayData, itemClass, itemNameForMatch)) continue;
                return GetRequiredLevelFromDisplayData(displayData, effectiveQuality);
            }
            for (int i = 0; i < displayDataList.Count; i++)
            {
                object displayData = displayDataList[i];
                if (displayData == null) continue;
                var unlockListField = displayData.GetType().GetField("UnlockDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var unlockList = unlockListField?.GetValue(displayData) as IList;
                var count = unlockList?.Count ?? 0;
                if (count == 0)
                {
                    var mGetUd = displayData.GetType().GetMethod("GetUnlockData", new[] { typeof(int) });
                    if (mGetUd != null)
                    {
                        for (var u = 0; u < 256; u++)
                        {
                            object ud = null;
                            try { ud = mGetUd.Invoke(displayData, new object[] { u }); } catch { break; }
                            if (ud == null) break;
                            if (!UnlockEntryMatchesCraftItem(displayData, u, ud, itemClass, itemNameForMatch)) continue;
                            return GetRequiredLevelFromDisplayData(displayData, effectiveQuality);
                        }
                    }
                    continue;
                }
                for (var u = 0; u < count; u++)
                {
                    var ud = unlockList[u];
                    if (!UnlockEntryMatchesCraftItem(displayData, u, ud, itemClass, itemNameForMatch)) continue;
                    return GetRequiredLevelFromDisplayData(displayData, effectiveQuality);
                }
            }
            return -1;
        }

        /// <summary>
        /// Powered garage placeables often fail name-only progression match; scan unlock rows for any ItemClass
        /// containing "garagedoor" and the same color suffix (e.g. ..._PoweredWhite → tail "White").
        /// </summary>
        private static int TryResolvePoweredGarageViaUnlockScan(IList displayDataList, int effectiveQuality, string mapKeyName)
        {
            if (string.IsNullOrEmpty(mapKeyName)) return -1;
            if (mapKeyName.IndexOf("garagedoor", StringComparison.OrdinalIgnoreCase) < 0) return -1;
            if (mapKeyName.IndexOf("powered", StringComparison.OrdinalIgnoreCase) < 0) return -1;
            var li = mapKeyName.LastIndexOf('_');
            if (li < 0 || li >= mapKeyName.Length - 1) return -1;
            var tail = mapKeyName.Substring(li + 1);
            if (tail.Length < 2) return -1;

            string canonical = null;
            if (mapKeyName.StartsWith("ironGarageDoor_", StringComparison.OrdinalIgnoreCase) &&
                !mapKeyName.StartsWith("ironGarageDoor01_", StringComparison.OrdinalIgnoreCase))
                canonical = "ironGarageDoor01_" + mapKeyName.Substring("ironGarageDoor_".Length);

            var matches = new System.Collections.Generic.List<object>();
            for (int i = 0; i < displayDataList.Count; i++)
            {
                var displayData = displayDataList[i];
                if (displayData != null && PoweredGarageUnlockListContainsTail(displayData, tail))
                    matches.Add(displayData);
            }
            if (matches.Count == 0) return -1;

            if (!string.IsNullOrEmpty(canonical))
            {
                foreach (var displayData in matches)
                {
                    var name = GetDisplayDataItemName(displayData);
                    if (string.Equals(name, canonical, StringComparison.OrdinalIgnoreCase))
                        return GetRequiredLevelFromDisplayData(displayData, effectiveQuality);
                }
            }

            var bestSpecific = -1;
            foreach (var displayData in matches)
            {
                var name = GetDisplayDataItemName(displayData);
                if (string.IsNullOrEmpty(name)) continue;
                if (name.IndexOf("ironGarageDoor01", StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (name.IndexOf(tail, StringComparison.OrdinalIgnoreCase) < 0) continue;
                var lvl = GetRequiredLevelFromDisplayData(displayData, effectiveQuality);
                if (lvl > bestSpecific) bestSpecific = lvl;
            }
            if (bestSpecific >= 0) return bestSpecific;

            var bestAny = -1;
            foreach (var displayData in matches)
            {
                var lvl = GetRequiredLevelFromDisplayData(displayData, effectiveQuality);
                if (lvl > bestAny) bestAny = lvl;
            }
            return bestAny;
        }

        private static string GetDisplayDataItemName(object displayData)
        {
            if (displayData == null) return null;
            try
            {
                var f = displayData.GetType().GetField("ItemName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return f?.GetValue(displayData) as string;
            }
            catch { return null; }
        }

        private static bool PoweredGarageUnlockListContainsTail(object displayData, string tail)
        {
            var ddType = displayData.GetType();
            var mGetItem = ddType.GetMethod("GetUnlockItem", new[] { typeof(int) });
            if (mGetItem == null) return false;

            bool IndexMatches(int u)
            {
                try
                {
                    var ic = mGetItem.Invoke(displayData, new object[] { u }) as ItemClass;
                    if (ic == null) return false;
                    var n = GetItemClassNameForMap(ic);
                    if (string.IsNullOrEmpty(n)) return false;
                    if (n.IndexOf("garagedoor", StringComparison.OrdinalIgnoreCase) < 0) return false;
                    return n.IndexOf(tail, StringComparison.OrdinalIgnoreCase) >= 0;
                }
                catch
                {
                    return false;
                }
            }

            var unlockListField = ddType.GetField("UnlockDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var unlockList = unlockListField?.GetValue(displayData) as IList;
            var count = unlockList?.Count ?? 0;
            if (count > 0)
            {
                for (var u = 0; u < count; u++)
                    if (IndexMatches(u)) return true;
                return false;
            }
            for (var u = 0; u < 512; u++)
            {
                ItemClass ic = null;
                try { ic = mGetItem.Invoke(displayData, new object[] { u }) as ItemClass; } catch { break; }
                if (ic == null) break;
                if (IndexMatches(u)) return true;
            }
            return false;
        }

        private static List<string> CollectSampleProgressionItemNames(IList displayDataList, int max)
        {
            var r = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < displayDataList.Count && r.Count < max; i++)
            {
                var dd = displayDataList[i];
                if (dd == null) continue;
                var f = dd.GetType().GetField("ItemName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var n = f?.GetValue(dd) as string;
                if (string.IsNullOrEmpty(n) || !seen.Add(n)) continue;
                r.Add(n);
            }
            return r;
        }

        private static bool DisplayDataMatchesItem(object displayData, ItemClass itemClass, string itemNameForMatch)
        {
            if (displayData == null) return false;
            var ddType = displayData.GetType();
            var itemField = ddType.GetField("item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (itemField != null)
            {
                var item = itemField.GetValue(displayData);
                if (item == itemClass) return true;
            }
            var itemProp = ddType.GetProperty("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (itemProp != null)
            {
                try
                {
                    var item = itemProp.GetValue(displayData, null);
                    if (item == itemClass) return true;
                }
                catch { }
            }
            var itemNameField = ddType.GetField("ItemName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (itemNameField != null && !string.IsNullOrEmpty(itemNameForMatch))
            {
                var name = itemNameField.GetValue(displayData) as string;
                if (string.Equals(name, itemNameForMatch, StringComparison.OrdinalIgnoreCase)) return true;
            }
            if (!string.IsNullOrEmpty(itemNameForMatch))
            {
                object ddItem = null;
                if (itemField != null) ddItem = itemField.GetValue(displayData);
                if (ddItem == null && itemProp != null) try { ddItem = itemProp.GetValue(displayData, null); } catch { }
                if (ddItem is ItemClass ddItemClass)
                {
                    var ddMapKey = GetItemClassNameForMap(ddItemClass);
                    if (string.Equals(ddMapKey, itemNameForMatch, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            if (itemNameField != null && !string.IsNullOrEmpty(itemNameForMatch))
            {
                var name = itemNameField.GetValue(displayData) as string;
                if (!string.IsNullOrEmpty(name))
                {
                    const int minContainsLength = 5;
                    if (name.Length >= minContainsLength && itemNameForMatch.Length >= minContainsLength)
                    {
                        if (name.IndexOf(itemNameForMatch, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                        if (itemNameForMatch.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    }
                }
            }
            return false;
        }

        private static bool UnlockEntryMatchesCraftItem(object displayData, int unlockIndex, object unlockData, ItemClass itemClass, string itemNameForMatch)
        {
            if (unlockData != null && UnlockDataMatchesItem(unlockData, itemClass, itemNameForMatch)) return true;
            if (displayData != null && itemClass != null)
            {
                var m = displayData.GetType().GetMethod("GetUnlockItem", new[] { typeof(int) });
                if (m != null)
                {
                    try
                    {
                        var ic = m.Invoke(displayData, new object[] { unlockIndex }) as ItemClass;
                        if (ic != null)
                        {
                            if (ReferenceEquals(ic, itemClass)) return true;
                            if (!string.IsNullOrEmpty(itemNameForMatch) &&
                                string.Equals(GetItemClassNameForMap(ic), itemNameForMatch, StringComparison.OrdinalIgnoreCase)) return true;
                        }
                    }
                    catch { }
                }
            }
            if (unlockData != null && !string.IsNullOrEmpty(itemNameForMatch))
            {
                var recipeField = unlockData.GetType().GetField("RecipeList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var arr = recipeField?.GetValue(unlockData) as string[];
                if (arr != null)
                {
                    foreach (var r in arr)
                    {
                        if (string.IsNullOrEmpty(r)) continue;
                        if (string.Equals(r, itemNameForMatch, StringComparison.OrdinalIgnoreCase)) return true;
                        if (r.Length >= 4 && itemNameForMatch.Length >= 4)
                        {
                            if (r.IndexOf(itemNameForMatch, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                            if (itemNameForMatch.IndexOf(r, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                        }
                    }
                }
            }
            return false;
        }

        private static bool UnlockDataMatchesItem(object unlockData, ItemClass itemClass, string itemNameForMatch)
        {
            if (unlockData == null) return false;
            var t = unlockData.GetType();
            var itemField = t.GetField("item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (itemField != null && itemField.GetValue(unlockData) == itemClass) return true;
            var itemProp = t.GetProperty("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (itemProp != null)
            {
                try { if (itemProp.GetValue(unlockData, null) == itemClass) return true; } catch { }
            }
            var itemNameField = t.GetField("ItemName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (itemNameField != null && !string.IsNullOrEmpty(itemNameForMatch))
            {
                var name = itemNameField.GetValue(unlockData) as string;
                if (string.Equals(name, itemNameForMatch, StringComparison.OrdinalIgnoreCase)) return true;
            }
            if (!string.IsNullOrEmpty(itemNameForMatch))
            {
                object uItem = null;
                if (itemField != null) uItem = itemField.GetValue(unlockData);
                if (uItem == null && itemProp != null) try { uItem = itemProp.GetValue(unlockData, null); } catch { }
                if (uItem is ItemClass uIc)
                {
                    if (string.Equals(GetItemClassNameForMap(uIc), itemNameForMatch, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            if (itemNameField != null && !string.IsNullOrEmpty(itemNameForMatch))
            {
                var name = itemNameField.GetValue(unlockData) as string;
                if (!string.IsNullOrEmpty(name))
                {
                    const int minLen = 5;
                    if (name.Length >= minLen && itemNameForMatch.Length >= minLen)
                    {
                        if (name.IndexOf(itemNameForMatch, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                        if (itemNameForMatch.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    }
                }
            }
            return false;
        }

        private static int GetRequiredLevelFromDisplayData(object displayData, int quality)
        {
            if (displayData == null || quality <= 0) return 0;
            var ddType = displayData.GetType();

            var qualityStartsField = ddType.GetField("QualityStarts", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (qualityStartsField != null)
            {
                var qualityStarts = qualityStartsField.GetValue(displayData) as int[];
                if (qualityStarts != null && quality >= 1 && quality <= qualityStarts.Length)
                {
                    int level = qualityStarts[quality - 1];
                    if (level >= 0) return level;
                }
            }

            var getQualityLevelMethod = ddType.GetMethod("GetQualityLevel", new[] { typeof(int) });
            if (getQualityLevelMethod != null)
            {
                for (int level = 1; level <= 100; level++)
                {
                    try
                    {
                        var q = getQualityLevelMethod.Invoke(displayData, new object[] { level });
                        if (q is int qual && qual >= quality) return level;
                    }
                    catch { break; }
                }
            }
            return 0;
        }

        /// <summary>Public entry points for tests (Bazel test assembly name may not receive InternalsVisibleTo).</summary>
        public static class TestHooks
        {
            public static int TryResolveCraftingElectricianRequiredLevel(object progression, ItemClass itemClass, string mapKeyName, int effectiveQuality)
            {
                return TryResolveRequiredLevelInTree(progression, "craftingelectrician", itemClass, mapKeyName, effectiveQuality);
            }
        }
    }
}
