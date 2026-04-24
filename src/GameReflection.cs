using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
                var fromGm = playerProp?.GetValue(gm, null) as EntityAlive;
                if (fromGm != null) return fromGm;

                object world = null;
                var worldField = gmT.GetField("m_World", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (worldField != null)
                    world = worldField.GetValue(gm);
                if (world == null)
                {
                    var worldProp = gmT.GetProperty("World", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    world = worldProp?.GetValue(gm, null);
                }
                if (world != null)
                {
                    var wT = world.GetType();
                    var lpeField = wT.GetField("m_LocalPlayerEntity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var lpe = lpeField?.GetValue(world);
                    if (lpe is EntityAlive eaWorld) return eaWorld;
                    var lpListField = wT.GetField("m_LocalPlayerEntities", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (lpListField?.GetValue(world) is IList lpList && lpList.Count > 0 && lpList[0] is EntityAlive eaList)
                        return eaList;
                }

                return null;
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

        /// <summary>
        /// Reads ProgressionClass from a ProgressionValue instance (same fields as <see cref="GetProgressionClassFromProgressionValue"/>).
        /// </summary>
        private static object ExtractProgressionClassFromProgressionValueInstance(object pv)
        {
            if (pv == null) return null;
            try
            {
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
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Finds crafting skill ProgressionClass when <see cref="GetProgressionValue"/> rejects our lookup string casing
        /// (vanilla uses <c>craftingExplosives</c>; we often pass <c>craftingexplosives</c>).
        /// </summary>
        private static object FindProgressionClassViaQuickList(object progression, string craftingSkillGroup)
        {
            if (progression == null || string.IsNullOrWhiteSpace(craftingSkillGroup)) return null;
            var canonical = ToProgressionLookupName(craftingSkillGroup);
            if (string.IsNullOrWhiteSpace(canonical)) return null;
            try
            {
                var quickListField = progression.GetType().GetField("ProgressionValueQuickList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var list = quickListField?.GetValue(progression) as IList;
                if (list == null) return null;
                for (var i = 0; i < list.Count; i++)
                {
                    var pv = list[i];
                    if (pv == null) continue;
                    var n = GetProgressionValueName(pv);
                    if (string.IsNullOrEmpty(n)) continue;
                    if (!string.Equals(n, canonical, StringComparison.OrdinalIgnoreCase)) continue;
                    var pc = ExtractProgressionClassFromProgressionValueInstance(pv);
                    if (pc != null) return pc;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Direct lookup on <c>Progression.ProgressionClasses</c> (dictionary of name → ProgressionClass).
        /// More reliable than <see cref="GetProgressionValue"/> when string keys differ by casing.
        /// </summary>
        private static object TryGetProgressionClassFromClassesDictionary(object progression, string craftingSkillGroup)
        {
            if (progression == null || string.IsNullOrWhiteSpace(craftingSkillGroup)) return null;
            var canonical = ToProgressionLookupName(craftingSkillGroup);
            if (string.IsNullOrWhiteSpace(canonical)) return null;
            try
            {
                var dictField = progression.GetType().GetField("ProgressionClasses", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var dict = dictField?.GetValue(progression) as IDictionary;
                if (dict == null) return null;
                foreach (DictionaryEntry e in dict)
                {
                    if (e.Key is string ks && string.Equals(ks, canonical, StringComparison.OrdinalIgnoreCase))
                        return e.Value;
                }
            }
            catch { }
            return null;
        }

        private static IList GetDisplayDataListFromProgressionClass(object progressionClass)
        {
            if (progressionClass == null) return null;
            try
            {
                var t = progressionClass.GetType();
                var f = t.GetField("DisplayDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var list = f?.GetValue(progressionClass) as IList;
                if (list != null) return list;
                var p = t.GetProperty("DisplayDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return p?.GetValue(progressionClass, null) as IList;
            }
            catch { return null; }
        }

        private static IList GetUnlockDataListFromDisplayData(object displayData)
        {
            if (displayData == null) return null;
            try
            {
                var t = displayData.GetType();
                var f = t.GetField("UnlockDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var list = f?.GetValue(displayData) as IList;
                if (list != null) return list;
                var p = t.GetProperty("UnlockDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return p?.GetValue(displayData, null) as IList;
            }
            catch { return null; }
        }

        /// <summary>
        /// Resolves the game's ProgressionClass for a crafting skill group (map/config name: Explosives, Workstations, …).
        /// </summary>
        private static object GetProgressionClassForCraftingSkill(object progression, string craftingSkillGroup)
        {
            if (progression == null || string.IsNullOrWhiteSpace(craftingSkillGroup)) return null;
            var lookup = ToProgressionLookupName(craftingSkillGroup);
            if (string.IsNullOrWhiteSpace(lookup)) return null;

            var pc = TryGetProgressionClassFromClassesDictionary(progression, craftingSkillGroup);
            if (pc != null) return pc;

            pc = GetProgressionClassFromProgressionValue(progression, lookup);
            if (pc != null) return pc;

            // Vanilla progression names often camel-case the segment after "crafting" (craftingExplosives).
            if (lookup.Length > "crafting".Length && lookup.StartsWith("crafting", StringComparison.OrdinalIgnoreCase))
            {
                var tail = lookup.Substring("crafting".Length);
                if (tail.Length > 0)
                {
                    var camel = "crafting" + char.ToUpperInvariant(tail[0]) + tail.Substring(1);
                    pc = GetProgressionClassFromProgressionValue(progression, camel);
                    if (pc != null) return pc;
                }
            }

            return FindProgressionClassViaQuickList(progression, craftingSkillGroup);
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
            return GetItemClassNameForMap(itemClass, null);
        }

        /// <param name="itemValue">When set, uses id / BlockValue.type indices before <c>Block</c> / <c>GetBlock()</c> so Extends-style mines (hubcap vs candy tin) map correctly.</param>
        internal static string GetItemClassNameForMap(ItemClass itemClass, ItemValue itemValue)
        {
            if (itemClass == null) return null;
            var t = itemClass.GetType();
            // Extends (XML): prototypes may share ItemClass.Name / Block reference with the parent; inventory ItemValue
            // still carries the concrete item/block id, and BlockValue.type indexes Block.list[] for the variant.
            if (itemValue != null)
            {
                var fromId = TryGetMapKeyFromItemOrBlockListIndex(itemValue);
                if (!string.IsNullOrWhiteSpace(fromId))
                    return fromId.Trim();

                try
                {
                    var toBv = itemValue.GetType().GetMethod("ToBlockValue", Type.EmptyTypes);
                    if (toBv != null)
                    {
                        var bv = toBv.Invoke(itemValue, null);
                        if (bv != null)
                        {
                            var fromTypeIdx = TryGetBlockNameFromBlockValueTypeIndex(bv);
                            if (!string.IsNullOrWhiteSpace(fromTypeIdx))
                                return fromTypeIdx.Trim();

                            var blockProp = bv.GetType().GetProperty("Block", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            var blk = blockProp?.GetValue(bv, null);
                            var blockName = GetBlockNameForMap(blk);
                            if (!string.IsNullOrWhiteSpace(blockName)) return blockName.Trim();
                        }
                    }
                }
                catch { }
            }

            try
            {
                var isBlockMethod = t.GetMethod("IsBlock", Type.EmptyTypes);
                if (isBlockMethod != null && isBlockMethod.Invoke(itemClass, null) is bool isBlock && isBlock)
                {
                    var getBlockMethod = t.GetMethod("GetBlock", Type.EmptyTypes);
                    if (getBlockMethod != null)
                    {
                        var blk = getBlockMethod.Invoke(itemClass, null);
                        var blockName = GetBlockNameForMap(blk);
                        if (!string.IsNullOrWhiteSpace(blockName)) return blockName.Trim();
                    }
                }
            }
            catch { }

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
            try
            {
                var getItemName = t.GetMethod("GetItemName", Type.EmptyTypes);
                if (getItemName != null)
                {
                    var s = getItemName.Invoke(itemClass, null) as string;
                    if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
                }
            }
            catch { }

            return itemClass.GetType().Name;
        }

        /// <summary>
        /// Gets the crafting skill group for restrictions. First ClassNameToCraftingSkillMap.xml (case-insensitive key).
        /// If unmapped, uses the game's <c>ItemClass.CraftingSkillGroup</c> when it is Electrician, Workstations, HarvestingTools, or Tools (maps to HarvestingTools).
        /// </summary>
        internal static string GetCraftingSkillGroup(ItemClass itemClass)
        {
            return GetCraftingSkillGroup(itemClass, null);
        }

        internal static string GetCraftingSkillGroup(ItemClass itemClass, ItemValue itemValue)
        {
            if (itemClass == null) return null;
            var mapKey = GetItemClassNameForMap(itemClass, itemValue);
            if (!string.IsNullOrWhiteSpace(mapKey))
            {
                var map = ClassNameToCraftingSkillMapLoader.GetMap();
                if (map.TryGetValue(mapKey, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
                    return mapped.Trim();
            }

            var inferred = InferCraftingSkillGroupFromMapKey(mapKey);
            if (!string.IsNullOrWhiteSpace(inferred))
                return inferred;
            inferred = TryInferCraftingSkillGroupFromItemTags(itemClass);
            if (!string.IsNullOrWhiteSpace(inferred))
                return inferred;

            return TrySkillGroupFromItemClassCraftingSkillGroup(itemClass);
        }

        /// <summary>When the map has no row, vanilla item ids still follow predictable prefixes (items.xml).</summary>
        private static string InferCraftingSkillGroupFromMapKey(string mapKey)
        {
            if (string.IsNullOrEmpty(mapKey)) return null;
            if (mapKey.StartsWith("planted", StringComparison.OrdinalIgnoreCase))
                return "Seeds";
            if (mapKey.StartsWith("thrown", StringComparison.OrdinalIgnoreCase))
                return "Explosives";
            if (mapKey.StartsWith("mine", StringComparison.OrdinalIgnoreCase))
                return "Explosives";
            if (mapKey.StartsWith("ammoRocket", StringComparison.OrdinalIgnoreCase))
                return "Explosives";
            if (mapKey.StartsWith("resourceRocket", StringComparison.OrdinalIgnoreCase))
                return "Explosives";
            if (mapKey.StartsWith("gunExplosives", StringComparison.OrdinalIgnoreCase))
                return "Explosives";
            return null;
        }

        private static string TryInferCraftingSkillGroupFromItemTags(ItemClass itemClass)
        {
            if (itemClass == null) return null;
            try
            {
                var t = itemClass.GetType();
                object ft = null;
                var itemTagsProp = t.GetProperty("ItemTags", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (itemTagsProp != null)
                    ft = itemTagsProp.GetValue(itemClass, null);
                if (ft == null)
                {
                    var itemTagsField = t.GetField("ItemTags", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    ft = itemTagsField?.GetValue(itemClass);
                }
                if (ft == null) return null;
                var s = ft.ToString();
                if (string.IsNullOrEmpty(s)) return null;
                if (s.IndexOf("explosivesSkill", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Explosives";
                if (s.IndexOf("perkDemolitionsExpert", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Explosives";
                if (s.IndexOf("plantingSkill", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    s.IndexOf("perkLivingOffTheLand", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Seeds";
            }
            catch
            {
                // ignored
            }
            return null;
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
            if (string.Equals(s, "Explosives", StringComparison.OrdinalIgnoreCase))
                return "Explosives";
            if (string.Equals(s, "Seeds", StringComparison.OrdinalIgnoreCase))
                return "Seeds";
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
                   || string.Equals(skillGroup, "HarvestingTools", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(skillGroup, "Explosives", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(skillGroup, "Seeds", StringComparison.OrdinalIgnoreCase);
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
        /// Gets the minimum crafting level required to use this item at its current quality (or tier 1 for Electrician / Workstations / HarvestingTools / Explosives when the item has no quality).
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

        /// <summary>
        /// Required level for a placed workstation block (Workstations skill, synthetic quality tier 1).
        /// Uses block name for ClassNameToCraftingSkillMap lookup; returns 0 if not in map or not Workstations.
        /// </summary>
        internal static int GetRequiredLevelForWorkstationBlock(object blockValue)
        {
            if (blockValue == null) return 0;
            var block = GetBlockFromBlockValue(blockValue);
            if (block == null) return 0;
            var map = ClassNameToCraftingSkillMapLoader.GetMap();
            var mapKey = GetBlockNameForMap(block);
            if (string.IsNullOrWhiteSpace(mapKey) || !map.TryGetValue(mapKey, out var skillGroup) || !string.Equals(skillGroup, "Workstations", StringComparison.OrdinalIgnoreCase))
            {
                mapKey = GetWorkstationBlockMapKeyFromTypeName(block.GetType().Name, map);
                if (string.IsNullOrWhiteSpace(mapKey) || !map.TryGetValue(mapKey, out skillGroup) || !string.Equals(skillGroup, "Workstations", StringComparison.OrdinalIgnoreCase))
                    return 0;
            }
            var entity = GetLocalPlayer();
            if (entity == null) return 0;
            var progression = GetProgression(entity);
            if (progression == null) return 0;
            var lookupName = ToProgressionLookupName(skillGroup);
            if (string.IsNullOrWhiteSpace(lookupName)) return 0;
            var resolved = -1;
            foreach (var progressionMapKey in EnumerateWorkstationProgressionMapKeys(mapKey))
            {
                resolved = TryResolveRequiredLevelByMapKeyOnly(progression, skillGroup, progressionMapKey, effectiveQuality: 1);
                if (resolved >= 0)
                    break;
            }
            if (resolved >= 0 && ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelOverride(mapKey, out var ovLevel))
                resolved = Math.Max(resolved, ovLevel);
            if (resolved >= 0 && ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelMin(mapKey, out var minLv))
                resolved = Math.Max(resolved, minLv);
            if (resolved < 0 && ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelOverride(mapKey, out var fallbackOv))
                resolved = fallbackOv;
            if (resolved < 0 && ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelMin(mapKey, out var fallbackMin))
                resolved = fallbackMin;
            return resolved >= 0 ? resolved : 0;
        }

        internal static object GetBlockFromBlockValue(object blockValue)
        {
            if (blockValue == null) return null;
            try
            {
                var prop = blockValue.GetType().GetProperty("Block", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null) return prop.GetValue(blockValue, null);
                var field = blockValue.GetType().GetField("Block", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return field?.GetValue(blockValue);
            }
            catch { return null; }
        }

        /// <summary>
        /// Gets the BlockValue from a TileEntity (e.g. TileEntityWorkstation). Uses BlockValue property or blockValue field.
        /// </summary>
        internal static object GetBlockValueFromTileEntity(object tileEntity)
        {
            if (tileEntity == null) return null;
            try
            {
                var t = tileEntity.GetType();
                var prop = t.GetProperty("BlockValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null) return prop.GetValue(tileEntity, null);
                var field = t.GetField("blockValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return field?.GetValue(tileEntity);
            }
            catch { return null; }
        }

        /// <summary>
        /// Tries to get a Workstations map key from a block type name when Block.Name is not in map (e.g. BlockForge → forge, BlockChemistryStation → chemistryStation).
        /// </summary>
        private static string GetWorkstationBlockMapKeyFromTypeName(string typeName, System.Collections.Generic.IReadOnlyDictionary<string, string> map)
        {
            if (string.IsNullOrEmpty(typeName) || map == null) return null;
            if (!typeName.StartsWith("Block", StringComparison.Ordinal) || typeName.Length <= 5) return null;
            var suffix = typeName.Substring(5);
            if (suffix.Length == 0) return null;
            var candidate = char.ToLowerInvariant(suffix[0]) + suffix.Substring(1);
            if (map.TryGetValue(candidate, out var group) && string.Equals(group, "Workstations", StringComparison.OrdinalIgnoreCase))
                return candidate;
            return null;
        }

        /// <summary>
        /// Progression DisplayData ItemName often differs from block Name / map key (e.g. toolForge vs forge). Try each until one matches.
        /// </summary>
        private static System.Collections.Generic.IEnumerable<string> EnumerateWorkstationProgressionMapKeys(string mapKey)
        {
            if (string.IsNullOrWhiteSpace(mapKey))
                yield break;

            var list = new System.Collections.Generic.List<string>();
            void Add(string k)
            {
                if (string.IsNullOrWhiteSpace(k)) return;
                var t = k.Trim();
                foreach (var e in list)
                    if (string.Equals(e, t, StringComparison.OrdinalIgnoreCase))
                        return;
                list.Add(t);
            }

            if (ClassNameToCraftingSkillMapLoader.TryGetProgressionMatchOverride(mapKey, out var pm) && !string.IsNullOrWhiteSpace(pm))
                Add(pm.Trim());
            Add(mapKey);

            switch (mapKey.Trim().ToLowerInvariant())
            {
                case "forge":
                    Add("toolForge");
                    Add("terrForge");
                    Add("cntForge");
                    break;
                case "workbench":
                    Add("toolWorkbenchPlaceable");
                    Add("cntWorkbench");
                    break;
                case "cementmixer":
                    Add("cementMixerPlaceable");
                    Add("cntCementMixer");
                    break;
                case "chemistrystation":
                    Add("chemistryStationPlaceable");
                    Add("cntChemistryStation");
                    break;
                case "cntapiary":
                    Add("apiary");
                    Add("cntApiary");
                    break;
                case "cntdewcollector":
                    Add("dewCollector");
                    Add("cntDewCollector");
                    break;
            }

            foreach (var k in list)
                yield return k;
        }

        /// <summary>
        /// <see cref="ItemValue.GetItemOrBlockId"/> + <see cref="ItemClass.list"/> definition name. Extends-style items
        /// still get distinct list slots; this avoids merged <see cref="ItemClass.Name"/> / block prototypes.
        /// </summary>
        private static string TryGetMapKeyFromItemOrBlockListIndex(ItemValue itemValue)
        {
            if (itemValue == null) return null;
            try
            {
                var id = TryReadItemOrBlockId(itemValue);
                if (id == null || id.Value < 0) return null;
                var idx = id.Value;

                var icList = TryGetStaticArrayField(typeof(ItemClass), "list") as ItemClass[];
                if (icList != null && idx < icList.Length && icList[idx] != null)
                {
                    var n = GetDirectItemClassDefinitionName(icList[idx]);
                    if (!string.IsNullOrWhiteSpace(n)) return n;
                }
            }
            catch
            {
                // ignored
            }
            return null;
        }

        private static Array TryGetStaticArrayField(Type declaredType, string fieldName)
        {
            try
            {
                var f = declaredType.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null)
                {
                    var v = f.GetValue(null);
                    if (v is Array a) return a;
                }
            }
            catch
            {
                // ignored
            }
            return null;
        }

        private static int? TryReadItemOrBlockId(ItemValue itemValue)
        {
            if (itemValue == null) return null;
            try
            {
                var ivt = itemValue.GetType();
                var m = ivt.GetMethod("GetItemOrBlockId", Type.EmptyTypes) ?? ivt.GetMethod("GetItemId", Type.EmptyTypes);
                if (m == null) return null;
                var o = m.Invoke(itemValue, null);
                switch (o)
                {
                    case int i: return i;
                    case uint ui: return (int)ui;
                    case short s: return s;
                    case ushort us: return us;
                }
            }
            catch
            {
                // ignored
            }
            return null;
        }

        /// <summary>Name / pName / GetItemName only — no recursion into block/id paths.</summary>
        private static string GetDirectItemClassDefinitionName(ItemClass itemClass)
        {
            if (itemClass == null) return null;
            try
            {
                var t = itemClass.GetType();
                var nameProp = t.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (nameProp != null)
                {
                    var v = nameProp.GetValue(itemClass, null);
                    if (v is string s && !string.IsNullOrWhiteSpace(s)) return s.Trim();
                }
                var nameField = t.GetField("pName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (nameField != null)
                {
                    var v = nameField.GetValue(itemClass);
                    if (v is string s2 && !string.IsNullOrWhiteSpace(s2)) return s2.Trim();
                }
                var getItemName = t.GetMethod("GetItemName", Type.EmptyTypes);
                if (getItemName != null)
                {
                    var s3 = getItemName.Invoke(itemClass, null) as string;
                    if (!string.IsNullOrWhiteSpace(s3)) return s3.Trim();
                }
            }
            catch
            {
                // ignored
            }
            return null;
        }

        /// <summary>
        /// <see cref="BlockValue.type"/> indexes <see cref="Block.list"/>; <see cref="BlockValue.Block"/> may still point at an Extends parent block.
        /// </summary>
        private static string TryGetBlockNameFromBlockValueTypeIndex(object blockValue)
        {
            if (blockValue == null) return null;
            try
            {
                var bt = blockValue.GetType();
                object tv = null;
                var tf = bt.GetField("type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (tf != null) tv = tf.GetValue(blockValue);
                if (tv == null)
                {
                    var tp = bt.GetProperty("type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    tv = tp?.GetValue(blockValue, null);
                }
                if (tv == null) return null;
                var typeIdx = tv is int i ? i : tv is uint u ? (int)u : -1;
                if (typeIdx < 0) return null;

                var blockType = typeof(ItemClass).Assembly.GetType("Block");
                if (blockType == null) return null;
                var arr = TryGetStaticArrayField(blockType, "list");
                if (arr == null || typeIdx >= arr.Length) return null;
                var blk = arr.GetValue(typeIdx);
                return GetBlockNameForMap(blk);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Block name used for ClassNameToCraftingSkillMap lookup (Name property/field, then type name).
        /// </summary>
        internal static string GetBlockNameForMap(object block)
        {
            if (block == null) return null;
            try
            {
                var t = block.GetType();
                var nameProp = t.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (nameProp != null)
                {
                    var v = nameProp.GetValue(block, null);
                    if (v is string s && !string.IsNullOrWhiteSpace(s)) return s.Trim();
                }
                var nameField = t.GetField("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (nameField != null)
                {
                    var v = nameField.GetValue(block);
                    if (v is string s2 && !string.IsNullOrWhiteSpace(s2)) return s2.Trim();
                }
                return block.GetType().Name;
            }
            catch
            {
                return block.GetType().Name;
            }
        }

        /// <summary>
        /// Map key for a vehicle entity (EntityDriveable/EntityVehicle) for ClassNameToCraftingSkillMap.
        /// Uses entity type name heuristic so new vehicles work if they follow naming (e.g. EntityXyz → vehicleXyzPlaceable).
        /// Returns null if not a Vehicles skill vehicle.
        /// </summary>
        internal static string GetVehicleEntityMapKey(object vehicleEntity)
        {
            if (vehicleEntity == null) return null;
            var typeName = vehicleEntity.GetType().Name;
            if (string.IsNullOrEmpty(typeName)) return null;
            var map = ClassNameToCraftingSkillMapLoader.GetMap();
            if (typeName.StartsWith("Entity", StringComparison.OrdinalIgnoreCase) && typeName.Length > 6)
            {
                var suffix = typeName.Substring(6);
                var candidate = "vehicle" + suffix + "Placeable";
                if (map.ContainsKey(candidate)) return candidate;
                if (suffix.StartsWith("V", StringComparison.Ordinal) && suffix.Length > 1)
                {
                    candidate = "vehicle" + suffix.Substring(1) + "Placeable";
                    if (map.ContainsKey(candidate)) return candidate;
                }
                if (string.Equals(suffix, "VJeep", StringComparison.OrdinalIgnoreCase) && map.ContainsKey("vehicleTruck4x4Placeable"))
                    return "vehicleTruck4x4Placeable";
            }
            return null;
        }

        /// <summary>
        /// Required level to drive this vehicle entity (Vehicles skill, synthetic tier 1). Returns 0 if not in map or not Vehicles.
        /// </summary>
        internal static int GetRequiredLevelForVehicleEntity(object vehicleEntity)
        {
            var mapKey = GetVehicleEntityMapKey(vehicleEntity);
            if (string.IsNullOrWhiteSpace(mapKey)) return 0;
            var map = ClassNameToCraftingSkillMapLoader.GetMap();
            if (!map.TryGetValue(mapKey, out var skillGroup) || !string.Equals(skillGroup, "Vehicles", StringComparison.OrdinalIgnoreCase))
                return 0;
            var entity = GetLocalPlayer();
            if (entity == null) return 0;
            var progression = GetProgression(entity);
            if (progression == null) return 0;
            var lookupName = ToProgressionLookupName(skillGroup);
            if (string.IsNullOrWhiteSpace(lookupName)) return 0;
            var resolved = TryResolveRequiredLevelByMapKeyOnly(progression, skillGroup, mapKey, effectiveQuality: 1);
            if (resolved >= 0 && ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelOverride(mapKey, out var ovLevel))
                resolved = Math.Max(resolved, ovLevel);
            if (resolved >= 0 && ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelMin(mapKey, out var minLv))
                resolved = Math.Max(resolved, minLv);
            if (resolved < 0 && ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelOverride(mapKey, out var fallbackOv))
                resolved = fallbackOv;
            if (resolved < 0 && ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelMin(mapKey, out var fallbackMin))
                resolved = fallbackMin;
            return resolved >= 0 ? resolved : 0;
        }

        /// <summary>
        /// Display name for a vehicle entity (e.g. "Bicycle", "Minibike") for popup message.
        /// </summary>
        internal static string GetVehicleEntityDisplayName(object vehicleEntity)
        {
            if (vehicleEntity == null) return "vehicle";
            var typeName = vehicleEntity.GetType().Name;
            if (typeName.StartsWith("Entity", StringComparison.OrdinalIgnoreCase) && typeName.Length > 6)
            {
                var suffix = typeName.Substring(6);
                if (suffix.StartsWith("V", StringComparison.Ordinal) && suffix.Length > 1)
                    suffix = suffix.Substring(1);
                if (string.Equals(suffix, "Jeep", StringComparison.OrdinalIgnoreCase))
                    return "4x4 Truck";
                return suffix;
            }
            return typeName;
        }

        /// <summary>
        /// Resolves required level from progression by map key name only (no ItemClass). Used for blocks/placeables.
        /// </summary>
        private static int TryResolveRequiredLevelByMapKeyOnly(object progression, string craftingSkillGroup, string mapKeyName, int effectiveQuality)
        {
            if (progression == null || string.IsNullOrWhiteSpace(craftingSkillGroup)) return -1;
            var progressionClass = GetProgressionClassForCraftingSkill(progression, craftingSkillGroup);
            if (progressionClass == null) return -1;
            var displayDataList = GetDisplayDataListFromProgressionClass(progressionClass);
            if (displayDataList == null || displayDataList.Count == 0) return -1;
            var candidates = BuildProgressionMatchCandidates(mapKeyName);
            foreach (var itemNameForMatch in candidates)
            {
                for (int i = 0; i < displayDataList.Count; i++)
                {
                    object displayData = displayDataList[i];
                    if (displayData == null) continue;
                    if (!DisplayDataMatchesMapKey(displayData, itemNameForMatch)) continue;
                    var lvl = GetRequiredLevelFromDisplayData(displayData, effectiveQuality);
                    if (lvl >= 0) return lvl;
                }
                for (int i = 0; i < displayDataList.Count; i++)
                {
                    object displayData = displayDataList[i];
                    if (displayData == null) continue;
                    var unlockList = GetUnlockDataListFromDisplayData(displayData);
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
                                if (!UnlockEntryMatchesMapKey(ud, itemNameForMatch)) continue;
                                var lvl = GetRequiredLevelFromDisplayData(displayData, effectiveQuality);
                                if (lvl >= 0) return lvl;
                            }
                        }
                        continue;
                    }
                    for (var u = 0; u < count; u++)
                    {
                        var ud = unlockList[u];
                        if (!UnlockEntryMatchesMapKey(ud, itemNameForMatch)) continue;
                        var lvl = GetRequiredLevelFromDisplayData(displayData, effectiveQuality);
                        if (lvl >= 0) return lvl;
                    }
                }
            }
            return -1;
        }

        private static bool DisplayDataMatchesMapKey(object displayData, string mapKeyName)
        {
            if (displayData == null || string.IsNullOrEmpty(mapKeyName)) return false;
            var itemNameField = displayData.GetType().GetField("ItemName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (itemNameField != null)
            {
                var name = itemNameField.GetValue(displayData) as string;
                if (string.Equals(name, mapKeyName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static bool UnlockEntryMatchesMapKey(object unlockData, string mapKeyName)
        {
            if (unlockData == null || string.IsNullOrEmpty(mapKeyName)) return false;
            var t = unlockData.GetType();
            var itemNameField = t.GetField("ItemName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (itemNameField != null)
            {
                var name = itemNameField.GetValue(unlockData) as string;
                if (string.Equals(name, mapKeyName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            var itemField = t.GetField("item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var item = itemField?.GetValue(unlockData);
            if (item != null)
            {
                var icName = GetItemClassNameForMap(item as ItemClass);
                if (string.Equals(icName, mapKeyName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <param name="missingProgressionReason">If non-null, progression is null and this is the log reason (no_player / no_progression).</param>
        private static int GetRequiredLevelForItemWithProgression(ItemClass itemClass, ItemValue itemValue, object progression, string missingProgressionReason)
        {
            if (itemClass == null || itemValue == null) return 0;
            var mapKey = GetItemClassNameForMap(itemClass, itemValue);
            var skillGroup = GetCraftingSkillGroup(itemClass, itemValue);
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
                var mapKeyName = GetItemClassNameForMap(itemClass, itemValue);
                var resolvedLevel = TryResolveRequiredLevelInTree(progression, skillGroup, itemClass, mapKeyName, effectiveQuality);
                // Electrician placeables (e.g. powered garage) may match craftingelectrician at tier 0 while the real
                // unlock tier lives under craftingworkstations — always take the higher of the two trees.
                if (string.Equals(skillGroup, "Electrician", StringComparison.OrdinalIgnoreCase))
                {
                    var wsLevel = TryResolveRequiredLevelInTree(progression, "Workstations", itemClass, mapKeyName, effectiveQuality);
                    if (wsLevel > resolvedLevel)
                    {
                        resolvedLevel = wsLevel;
                        if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                            ModApi.DebugLog($"Electrician item used Workstations progression for mapKey={mapKeyName} requiredLevel={resolvedLevel}");
                    }
                }
                // Override applies when progression gives no positive gate (including matched row at tier 0).
                if (resolvedLevel <= 0 && ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelOverride(mapKeyName, out var ovLevel))
                    resolvedLevel = ovLevel;
                // XML requiredLevelMin: floor when progression is missing, wrong (under-tier), or zero.
                if (ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelMin(mapKeyName, out var minLv) && minLv > 0)
                    resolvedLevel = System.Math.Max(resolvedLevel, minLv);
                if (resolvedLevel >= 0)
                    return resolvedLevel;

                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                {
                    var pc = GetProgressionClassForCraftingSkill(progression, skillGroup);
                    IList ddl = pc != null ? GetDisplayDataListFromProgressionClass(pc) : null;
                    var samples = ddl != null ? CollectSampleProgressionItemNames(ddl, 24) : new List<string>();
                    var candidates = BuildProgressionMatchCandidates(mapKeyName);
                    ModApi.DebugLog("no_progression_match mapKey=" + mapKeyName + " lookup=" + ToProgressionLookupName(skillGroup)
                        + " candidates=" + string.Join("|", candidates) + " samples=" + string.Join("|", samples));
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

        private static int TryResolveRequiredLevelInTree(object progression, string craftingSkillGroup, ItemClass itemClass, string mapKeyName, int effectiveQuality)
        {
            if (progression == null || string.IsNullOrWhiteSpace(craftingSkillGroup)) return -1;
            var progressionClass = GetProgressionClassForCraftingSkill(progression, craftingSkillGroup);
            if (progressionClass == null) return -1;
            var displayDataList = GetDisplayDataListFromProgressionClass(progressionClass);
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
                var tier = TryMatchUnlockTier1Based(displayData, itemClass, itemNameForMatch);
                if (tier >= 1)
                {
                    var lvl = GetRequiredLevelFromDisplayData(displayData, tier);
                    if (lvl > best) best = lvl;
                    continue;
                }
                if (!DisplayDataMatchesItem(displayData, itemClass, itemNameForMatch)) continue;
                var lvl2 = GetRequiredLevelFromDisplayData(displayData, effectiveQuality);
                if (lvl2 > best) best = lvl2;
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

        /// <summary>
        /// Loose name pair match for progression DisplayData / unlock strings vs ItemClass map keys.
        /// Rejects misleading prefix overlaps where a shorter id is a substring at the start of a longer sibling id
        /// (e.g. progression row "thrownGrenade" must not match item <c>thrownGrenadeContact</c>).
        /// Separator boundaries (_ - .) and digits after the prefix still allow variant matches (garage doors, T1).
        /// </summary>
        private static bool LooseProgressionNamePairMatch(string rowOrRecipeName, string mapKeyName)
        {
            if (string.IsNullOrEmpty(rowOrRecipeName) || string.IsNullOrEmpty(mapKeyName)) return false;
            if (string.Equals(rowOrRecipeName, mapKeyName, StringComparison.OrdinalIgnoreCase)) return true;

            const int minLen = 5;
            if (rowOrRecipeName.Length < minLen || mapKeyName.Length < minLen) return false;

            if (rowOrRecipeName.IndexOf(mapKeyName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (!rowOrRecipeName.StartsWith(mapKeyName, StringComparison.OrdinalIgnoreCase)) return true;
                return !IsMisleadingCamelCasePrefixExtension(mapKeyName, rowOrRecipeName);
            }

            if (mapKeyName.IndexOf(rowOrRecipeName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (!mapKeyName.StartsWith(rowOrRecipeName, StringComparison.OrdinalIgnoreCase)) return true;
                return !IsMisleadingCamelCasePrefixExtension(rowOrRecipeName, mapKeyName);
            }

            return false;
        }

        /// <summary>
        /// True when <paramref name="longerId"/> starts with <paramref name="shorterId"/> but continues as a separate
        /// item name segment (camelCase sibling), not a delimiter or tier suffix.
        /// </summary>
        private static bool IsMisleadingCamelCasePrefixExtension(string shorterId, string longerId)
        {
            if (longerId.Length <= shorterId.Length) return false;
            if (!longerId.StartsWith(shorterId, StringComparison.OrdinalIgnoreCase)) return false;
            var c = longerId[shorterId.Length];
            if (c == '_' || c == '-' || c == '.' || char.IsDigit(c)) return false;
            if (char.IsUpper(c) && shorterId.Length >= 6) return true;
            return false;
        }

        /// <summary>
        /// Composite crafting_skill display_entry rows (e.g. explosives T2/T3) store multiple unlock_entry children.
        /// Tier index (1-based) must match the child's slot so QualityStarts/unlock_level maps to the correct gate.
        /// </summary>
        private static int ReadUnlockTierForQualityStarts(object unlockData, int unlockIndex0Based)
        {
            if (unlockData != null)
            {
                try
                {
                    var tf = unlockData.GetType().GetField("UnlockTier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (tf != null && tf.GetValue(unlockData) is int ut && ut >= 1)
                        return ut;
                }
                catch { }
            }
            return unlockIndex0Based + 1;
        }

        private static int CountUnlockChildren(object displayData)
        {
            if (displayData == null) return 0;
            var unlockList = GetUnlockDataListFromDisplayData(displayData);
            if (unlockList != null && unlockList.Count > 0)
                return unlockList.Count;
            try
            {
                var mGetUd = displayData.GetType().GetMethod("GetUnlockData", new[] { typeof(int) });
                if (mGetUd == null) return 0;
                var n = 0;
                for (var u = 0; u < 256; u++)
                {
                    object ud = null;
                    try { ud = mGetUd.Invoke(displayData, new object[] { u }); } catch { break; }
                    if (ud == null) break;
                    n = u + 1;
                }
                return n;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Several unlock_entry children can share the same UnlockTier while <c>unlock_level</c> on the row is positional.
        /// </summary>
        private static int ResolveTierForUnlockChild(int siblingCount, object unlockData, int unlockIndex0Based)
        {
            if (siblingCount > 1)
                return unlockIndex0Based + 1;
            return ReadUnlockTierForQualityStarts(unlockData, unlockIndex0Based);
        }

        private static int TryMatchUnlockTier1Based(object displayData, ItemClass itemClass, string itemNameForMatch)
        {
            if (displayData == null || string.IsNullOrEmpty(itemNameForMatch)) return -1;
            var siblingCount = CountUnlockChildren(displayData);
            var unlockList = GetUnlockDataListFromDisplayData(displayData);
            var count = unlockList?.Count ?? 0;
            if (count > 0)
            {
                for (var u = 0; u < count; u++)
                {
                    var ud = unlockList[u];
                    if (!UnlockEntryMatchesCraftItem(displayData, u, ud, itemClass, itemNameForMatch)) continue;
                    return ResolveTierForUnlockChild(siblingCount, ud, u);
                }
                return -1;
            }
            var mGetUd = displayData.GetType().GetMethod("GetUnlockData", new[] { typeof(int) });
            if (mGetUd != null)
            {
                for (var u = 0; u < 256; u++)
                {
                    object ud = null;
                    try { ud = mGetUd.Invoke(displayData, new object[] { u }); } catch { break; }
                    if (ud == null) break;
                    if (!UnlockEntryMatchesCraftItem(displayData, u, ud, itemClass, itemNameForMatch)) continue;
                    return ResolveTierForUnlockChild(siblingCount, ud, u);
                }
            }
            return -1;
        }

        private static int TryResolveRequiredLevelWithProgressionName(ItemClass itemClass, IList displayDataList, int effectiveQuality, string itemNameForMatch)
        {
            for (int i = 0; i < displayDataList.Count; i++)
            {
                object displayData = displayDataList[i];
                if (displayData == null) continue;
                var tier = TryMatchUnlockTier1Based(displayData, itemClass, itemNameForMatch);
                if (tier >= 1)
                    return GetRequiredLevelFromDisplayData(displayData, tier);
            }
            for (int i = 0; i < displayDataList.Count; i++)
            {
                object displayData = displayDataList[i];
                if (displayData == null) continue;
                if (!DisplayDataMatchesItem(displayData, itemClass, itemNameForMatch)) continue;
                return GetRequiredLevelFromDisplayData(displayData, effectiveQuality);
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

            var unlockList = GetUnlockDataListFromDisplayData(displayData);
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
                        if (LooseProgressionNamePairMatch(name, itemNameForMatch)) return true;
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
                        if (r.Length >= 5 && itemNameForMatch.Length >= 5 && LooseProgressionNamePairMatch(r, itemNameForMatch))
                            return true;
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
                    if (name.Length >= minLen && itemNameForMatch.Length >= minLen && LooseProgressionNamePairMatch(name, itemNameForMatch))
                        return true;
                }
            }
            return false;
        }

        /// <summary>Parses comma-separated unlock_level from DisplayData when QualityStarts is missing or too short.</summary>
        private static int TryGetRequiredLevelFromUnlockLevelField(object displayData, int tier1Based)
        {
            if (displayData == null || tier1Based < 1) return -1;
            var t = displayData.GetType();
            foreach (var fieldName in new[] { "unlockLevel", "UnlockLevel", "unlock_level" })
            {
                FieldInfo f;
                try
                {
                    f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }
                catch
                {
                    continue;
                }
                if (f == null) continue;
                object raw;
                try
                {
                    raw = f.GetValue(displayData);
                }
                catch
                {
                    continue;
                }
                if (raw is int[] ia && tier1Based <= ia.Length)
                {
                    var lv = ia[tier1Based - 1];
                    if (lv >= 0) return lv;
                }
                if (raw is string s && !string.IsNullOrWhiteSpace(s))
                {
                    var parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tier1Based <= parts.Length &&
                        int.TryParse(parts[tier1Based - 1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
                        parsed >= 0)
                        return parsed;
                }
            }
            return -1;
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
                    // Do not return 0: Explosives/Seeds rows often use zeros here while gates live in unlock_level CSV.
                    if (level > 0) return level;
                }
            }

            var csvLevel = TryGetRequiredLevelFromUnlockLevelField(displayData, quality);
            if (csvLevel >= 0) return csvLevel;

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
                return TryResolveRequiredLevelInTree(progression, "Electrician", itemClass, mapKeyName, effectiveQuality);
            }

            public static int TryResolveCraftingExplosivesRequiredLevel(object progression, ItemClass itemClass, string mapKeyName, int effectiveQuality)
            {
                return TryResolveRequiredLevelInTree(progression, "Explosives", itemClass, mapKeyName, effectiveQuality);
            }

            public static int GetRequiredLevelFromDisplayDataForTests(object displayData, int qualityOrTier)
            {
                return GetRequiredLevelFromDisplayData(displayData, qualityOrTier);
            }
        }
    }
}
