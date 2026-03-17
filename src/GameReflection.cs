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
        /// Gets the crafting skill group name for the item class. Uses only ClassNameToCraftingSkillMap.xml:
        /// looks up the item's map key (item name or type name) in the map; if found, returns the mapped craftingSkillGroup.
        /// If the file is missing or the key has no mapping, returns null (do not restrict the item).
        /// </summary>
        internal static string GetCraftingSkillGroup(ItemClass itemClass)
        {
            if (itemClass == null) return null;
            var mapKey = GetItemClassNameForMap(itemClass);
            if (string.IsNullOrWhiteSpace(mapKey)) return null;
            var map = ClassNameToCraftingSkillMapLoader.GetMap();
            if (!map.TryGetValue(mapKey, out var mapped) || string.IsNullOrWhiteSpace(mapped)) return null;
            var trimmed = mapped.Trim();
            return trimmed;
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
        /// Gets the minimum crafting level required to use this item at its current quality.
        /// Uses Progression.ProgressionClasses[lookupName].DisplayDataList to find the item's DisplayData,
        /// then QualityStarts or GetQualityLevel(level) to compute required level. Returns 0 if no progression data or no quality.
        /// </summary>
        internal static int GetRequiredLevelForItem(ItemClass itemClass, ItemValue itemValue)
        {
            if (itemClass == null || itemValue == null) return 0;
            var skillGroup = GetCraftingSkillGroup(itemClass);
            if (string.IsNullOrWhiteSpace(skillGroup)) return 0;
            if (!HasQuality(itemValue)) return 0;
            var quality = GetQuality(itemValue);
            if (quality <= 0) return 0;

            var entity = GetLocalPlayer();
            if (entity == null) return 0;
            var lookupName = ToProgressionLookupName(skillGroup);
            if (string.IsNullOrWhiteSpace(lookupName)) return 0;

            try
            {
                object progression = GetProgression(entity);
                if (progression == null) return 0;

                var progType = progression.GetType();
                var pcField = progType.GetField("ProgressionClasses", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pcField == null) return 0;
                var progClasses = pcField.GetValue(progression);
                if (progClasses == null) return 0;

                var dictType = progClasses.GetType();
                var indexer = dictType.GetMethod("get_Item", new[] { typeof(string) })
                    ?? dictType.GetMethod("get_Item", new[] { typeof(object) });
                if (indexer == null) return 0;

                object progressionClass = null;
                try
                {
                    progressionClass = indexer.Invoke(progClasses, new object[] { lookupName });
                }
                catch { }
                if (progressionClass == null) return 0;

                var displayDataListField = progressionClass.GetType().GetField("DisplayDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (displayDataListField == null) return 0;
                var displayDataList = displayDataListField.GetValue(progressionClass) as IList;
                if (displayDataList == null || displayDataList.Count == 0) return 0;

                var itemNameForMatch = GetItemClassNameForMap(itemClass);

                for (int i = 0; i < displayDataList.Count; i++)
                {
                    object displayData = displayDataList[i];
                    if (displayData == null) continue;
                    if (!DisplayDataMatchesItem(displayData, itemClass, itemNameForMatch)) continue;

                    return GetRequiredLevelFromDisplayData(displayData, quality);
                }
            }
            catch { }
            return 0;
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
    }
}
