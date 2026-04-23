using System;
using System.Reflection;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Shared helper for determining if an item is restricted by crafting level.
    /// Used by patches and by the in-inventory red-label visualization.
    /// </summary>
    internal static class RestrictionHelper
    {
        /// <summary>
        /// Returns true if the item should be shown as restricted (player level below required for this item).
        /// Uses the same logic as EquipItemRestrictionPatch.
        /// </summary>
        public static bool IsItemRestricted(ItemStack stack)
        {
            if (stack == null || stack.IsEmpty()) return false;
            var itemValue = GetItemValue(stack);
            if (itemValue == null) return false;
            var itemClass = itemValue.ItemClass;
            if (itemClass == null) return false;

            var mapKey = GameReflection.GetItemClassNameForMap(itemClass, itemValue);
            var trace = AgentDebugSessionLog.IsTraceMapKey(mapKey);
            // #region agent log
            void LogAgentTrace(string sk, int req, int pl, bool rest, string detail)
            {
                if (!trace) return;
                try
                {
                    var map = ClassNameToCraftingSkillMapLoader.GetMap();
                    var inMap = !string.IsNullOrEmpty(mapKey) && map.ContainsKey(mapKey);
                    AgentDebugSessionLog.Write("H1-H5", "RestrictionHelper:IsItemRestricted", "trace_item",
                        mapKey, inMap, sk ?? "", req, pl, rest, detail);
                }
                catch { }
            }
            // #endregion

            var skillGroup = GameReflection.GetCraftingSkillGroup(itemClass, itemValue);
            if (string.IsNullOrWhiteSpace(skillGroup))
            {
                LogAgentTrace(null, 0, 0, false, "no_skill_group");
                return false;
            }
            if (ModConfig.Instance == null)
            {
                LogAgentTrace(skillGroup, 0, 0, false, "no_mod_config");
                return false;
            }
            if (!IsRestrictionEnabledForSkillGroup(skillGroup))
            {
                LogAgentTrace(skillGroup, 0, 0, false, "skill_disabled_in_config");
                // #region agent log
                if (string.Equals(skillGroup, "HarvestingTools", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var map = ClassNameToCraftingSkillMapLoader.GetMap();
                        var inMap = !string.IsNullOrEmpty(mapKey) && map.ContainsKey(mapKey);
                        AgentDebugSessionLog.WriteHarvestingEval(mapKey, inMap, "skill_disabled_in_config", 0, 0, false,
                            "enable HarvestingTools in ModConfig");
                    }
                    catch { }
                }
                // #endregion
                return false;
            }

            var player = GameReflection.GetLocalPlayer();
            if (player == null)
            {
                LogAgentTrace(skillGroup, 0, 0, false, "no_local_player");
                return false;
            }

            var requiredLevel = GameReflection.GetRequiredLevelForItem(itemClass, itemValue);
            var playerLevel = GameReflection.GetPlayerCraftingLevel(player, skillGroup);
            var restricted = LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true);
            // #region agent log
            if (string.Equals(skillGroup, "HarvestingTools", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var map2 = ClassNameToCraftingSkillMapLoader.GetMap();
                    var inMap2 = !string.IsNullOrEmpty(mapKey) && map2.ContainsKey(mapKey);
                    var det = requiredLevel <= 0 ? "requiredLevel_zero_no_gate" : (restricted ? "restricted" : "player_meets_required");
                    AgentDebugSessionLog.WriteHarvestingEval(mapKey, inMap2, "eval", requiredLevel, playerLevel, restricted, det);
                }
                catch { }
            }
            // #endregion
            if (ModConfig.Instance != null && ModConfig.Instance.DebugMode && restricted)
            {
                var lookupName = GameReflection.ToProgressionLookupName(skillGroup);
                ModApi.DebugLog($"[LimitByCraftingSkill] IsItemRestricted: skillGroup=\"{skillGroup}\" lookup=\"{lookupName}\" playerLevel={playerLevel} required={requiredLevel}");
            }
            LogAgentTrace(skillGroup, requiredLevel, playerLevel, restricted,
                requiredLevel <= 0 ? "requiredLevel_zero_not_restricted" : (restricted ? "restricted" : "player_meets_required"));
            return restricted;
        }

        /// <summary>
        /// True if restriction is enabled in config for this skill group.
        /// </summary>
        private static bool IsRestrictionEnabledForSkillGroup(string skillGroup)
        {
            if (string.IsNullOrWhiteSpace(skillGroup) || ModConfig.Instance == null) return false;
            var configName = GameReflection.ToProgressionOrConfigName(skillGroup);
            return ModConfig.Instance.IsRestrictionEnabledForSkill(configName);
        }

        /// <summary>
        /// Gets ItemValue from an ItemStack (property or field).
        /// </summary>
        internal static ItemValue GetItemValue(ItemStack stack)
        {
            if (stack == null) return null;
            try
            {
                var t = stack.GetType();
                var prop = t.GetProperty("itemValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null) return prop.GetValue(stack, null) as ItemValue;
                var field = t.GetField("itemValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return field?.GetValue(stack) as ItemValue;
            }
            catch
            {
                return null;
            }
        }
    }
}
