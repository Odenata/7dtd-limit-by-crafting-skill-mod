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

            var skillGroup = GameReflection.GetCraftingSkillGroup(itemClass);
            if (string.IsNullOrWhiteSpace(skillGroup)) return false;
            if (ModConfig.Instance == null) return false;
            if (!IsRestrictionEnabledForSkillGroup(skillGroup)) return false;

            var player = GameReflection.GetLocalPlayer();
            if (player == null) return false;

            var requiredLevel = GameReflection.GetRequiredLevelForItem(itemClass, itemValue);
            var playerLevel = GameReflection.GetPlayerCraftingLevel(player, skillGroup);
            var restricted = LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true);
            if (ModConfig.Instance != null && ModConfig.Instance.DebugMode && restricted)
            {
                var lookupName = GameReflection.ToProgressionLookupName(skillGroup);
                ModApi.DebugLog($"[LimitByCraftingSkill] IsItemRestricted: skillGroup=\"{skillGroup}\" lookup=\"{lookupName}\" playerLevel={playerLevel} required={requiredLevel}");
            }
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
