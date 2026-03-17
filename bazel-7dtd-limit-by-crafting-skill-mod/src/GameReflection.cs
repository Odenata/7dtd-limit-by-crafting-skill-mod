using System;
using System.Collections;
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
            if (string.Equals(gameCraftingSkillGroup, "Tools/Traps", System.StringComparison.OrdinalIgnoreCase))
                return null; // Handled in GetPlayerCraftingLevel: use min of Tools and Traps
            if (string.Equals(gameCraftingSkillGroup, "Weapons", System.StringComparison.OrdinalIgnoreCase))
                return null; // Handled in GetPlayerCraftingLevel: use max of weapon progressions
            if (string.Equals(gameCraftingSkillGroup, "Ammo/Weapons", System.StringComparison.OrdinalIgnoreCase))
                return null; // Handled in GetPlayerCraftingLevel: use max of Ammo and Weapons
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
            if (craftingSkillGroup.IndexOf('/') >= 0)
            {
                var parts = craftingSkillGroup.Split('/');
                var levels = new System.Collections.Generic.List<int>();
                foreach (var part in parts)
                {
                    var trimmed = part?.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                    var level = GetPlayerCraftingLevel(entity, trimmed);
                    levels.Add(level);
                }
                if (levels.Count == 0) return 0;
                // Tools/Traps: require both skills (use min). Ammo/Weapons and others: use max.
                if (string.Equals(craftingSkillGroup, "Tools/Traps", System.StringComparison.OrdinalIgnoreCase))
                {
                    int minLevel = levels[0];
                    for (int i = 1; i < levels.Count; i++)
                        if (levels[i] < minLevel) minLevel = levels[i];
                    return minLevel;
                }
                int maxLevel = 0;
                foreach (var level in levels)
                    if (level > maxLevel) maxLevel = level;
                return maxLevel;
            }
            if (string.Equals(craftingSkillGroup, "Weapons", System.StringComparison.OrdinalIgnoreCase))
                return GetPlayerCraftingLevelWeapons(entity);
            try
            {
                object progression = GetProgression(entity);
                if (progression == null) return 0;
                var progType = progression.GetType();
                var getValueMethod = progType.GetMethod("GetProgressionValue", new[] { typeof(string) });
                if (getValueMethod == null) return GetLevelFromQuickList(progression, craftingSkillGroup);

                var lookupName = ToProgressionLookupName(craftingSkillGroup);
                if (string.IsNullOrEmpty(lookupName)) return 0;
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

        /// <summary>Returns max level across weapon crafting progressions (bows, handguns, shotguns, rifles, machineguns). Used for "Weapons" and "Ammo/Weapons".</summary>
        private static int GetPlayerCraftingLevelWeapons(EntityAlive entity)
        {
            if (entity == null) return 0;
            try
            {
                object progression = GetProgression(entity);
                if (progression == null) return 0;
                var progType = progression.GetType();
                var getValueMethod = progType.GetMethod("GetProgressionValue", new[] { typeof(string) });
                if (getValueMethod == null) return 0;
                int max = 0;
                foreach (var name in new[] { "craftingbows", "craftinghandguns", "craftingshotguns", "craftingrifles", "craftingmachineguns" })
                {
                    var pv = getValueMethod.Invoke(progression, new object[] { name });
                    if (pv == null) continue;
                    var level = GetLevelFromProgressionValue(pv);
                    if (level > max) max = level;
                }
                return max;
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
        /// True if the name maps to a single progression key (not a composite like "Ammo/Weapons" or "Tools/Traps").
        /// Used to prefer specific names (e.g. "Bows", "Spears") over parent composites.
        /// </summary>
        private static bool MapsToSingleProgression(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var lookup = ToProgressionLookupName(name);
            return !string.IsNullOrEmpty(lookup);
        }

        /// <summary>
        /// Gets the crafting skill group name for the item class. Collects candidates from PropCraftingSkillGroup,
        /// CraftingSkillGroup (each type in hierarchy), Group, and Groups[]; prefers the first that maps to a
        /// single progression (e.g. "Bows") over a composite (e.g. "Ammo/Weapons").
        /// </summary>
        private static readonly System.Collections.Generic.HashSet<string> _loggedCraftingSkillTypes = new System.Collections.Generic.HashSet<string>();
        private static readonly object _loggedCraftingSkillTypesLock = new object();

        internal static string GetCraftingSkillGroup(ItemClass itemClass)
        {
            if (itemClass == null) return null;
            var candidates = new System.Collections.Generic.List<string>();
            var csgFromHierarchy = new System.Collections.Generic.List<string>();
            var type = itemClass.GetType();
            const BindingFlags declared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            const BindingFlags any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var propVal = GetPropertyOrFieldValue(itemClass, type, "PropCraftingSkillGroup", any);
            AddIfNonEmpty(candidates, propVal);
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                string csgVal = null;
                var f = t.GetField("CraftingSkillGroup", declared);
                if (f != null) csgVal = f.GetValue(itemClass) as string;
                if (string.IsNullOrWhiteSpace(csgVal))
                {
                    var p = t.GetProperty("CraftingSkillGroup", declared);
                    if (p != null) csgVal = p.GetValue(itemClass, null) as string;
                }
                AddIfNonEmpty(candidates, csgVal);
                if (!string.IsNullOrWhiteSpace(csgVal)) csgFromHierarchy.Add(csgVal.Trim());
            }
            var groupVal = GetPropertyOrFieldValue(itemClass, type, "Group", any);
            AddIfNonEmpty(candidates, groupVal);
            var groupsArr = type.GetField("Groups", any)?.GetValue(itemClass) as string[]
                ?? type.GetProperty("Groups", any)?.GetValue(itemClass, null) as string[];
            if (groupsArr != null)
            {
                foreach (var g in groupsArr)
                    AddIfNonEmpty(candidates, g);
            }

            if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
            {
                var typeName = type.Name;
                lock (_loggedCraftingSkillTypesLock)
                {
                    if (_loggedCraftingSkillTypes.Add(typeName))
                    {
                        var groupsStr = groupsArr != null ? string.Join(",", groupsArr) : "";
                        ModApi.DebugLog($"CraftingSkillGroup sources: Type={typeName} PropCraftingSkillGroup={propVal ?? "(null)"} CraftingSkillGroup=[{string.Join(", ", csgFromHierarchy)}] Group={groupVal ?? "(null)"} Groups=[{groupsStr}]");
                    }
                }
            }

            foreach (var c in candidates)
                if (MapsToSingleProgression(c)) return c;
            return candidates.Count > 0 ? candidates[0] : null;
        }

        private static void AddIfNonEmpty(System.Collections.Generic.List<string> list, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) list.Add(value.Trim());
        }

        private static string GetPropertyOrFieldValue(object obj, Type type, string name, BindingFlags flags)
        {
            if (obj == null || type == null || string.IsNullOrEmpty(name)) return null;
            var p = type.GetProperty(name, flags);
            if (p != null)
            {
                var v = p.GetValue(obj, null);
                return v as string;
            }
            var f = type.GetField(name, flags);
            if (f != null)
            {
                var v = f.GetValue(obj);
                return v as string;
            }
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
        /// Gets the minimum crafting level required to use this item.
        /// Uses progression data when available; otherwise uses quality as a stand-in (required level = quality).
        /// Returns 0 if the item has no level requirement (e.g. no quality, or skill group empty).
        /// </summary>
        internal static int GetRequiredLevelForItem(ItemClass itemClass, ItemValue itemValue)
        {
            if (itemClass == null || itemValue == null) return 0;
            var skillGroup = GetCraftingSkillGroup(itemClass);
            if (string.IsNullOrWhiteSpace(skillGroup)) return 0;
            if (!HasQuality(itemValue)) return 0;
            var quality = GetQuality(itemValue);
            if (quality <= 0) return 0;
            return quality;
        }
    }
}
