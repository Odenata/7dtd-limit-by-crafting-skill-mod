using System;
using System.Collections.Generic;
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
            var player = GameReflection.GetLocalPlayer();
            if (player == null)
                return false;
            return IsItemRestricted(stack, player, GameReflection.GetProgression(player));
        }

        /// <summary>
        /// Restriction check with pre-resolved player and progression (one resolve per grid apply).
        /// Optional caches avoid repeated progression / required-level lookups within one apply.
        /// </summary>
        public static bool IsItemRestricted(
            ItemStack stack,
            EntityAlive player,
            object progression,
            IDictionary<string, int> skillLevelCache = null,
            IDictionary<string, int> requiredLevelCache = null,
            bool useRequiredLevelMemo = false)
        {
            if (stack == null || stack.IsEmpty()) return false;
            var itemValue = GetItemValue(stack);
            if (itemValue == null) return false;
            var itemClass = itemValue.ItemClass;
            if (itemClass == null) return false;
            if (player == null)
                return false;

            Func<string, int> getLevel = skillGroup => GameReflection.GetPlayerCraftingLevel(player, skillGroup);
            var restricted = RestrictionScan.EvaluateOne(
                itemClass,
                itemValue,
                progression,
                getLevel,
                skillLevelCache,
                useRequiredLevelMemo,
                requiredLevelCache);

            if (restricted && ModConfig.Instance != null && ModConfig.Instance.DebugMode)
            {
                var skillGroup = GameReflection.GetCraftingSkillGroup(itemClass, itemValue);
                var lookupName = GameReflection.ToProgressionLookupName(skillGroup);
                int playerLevel = 0;
                if (skillLevelCache != null && skillLevelCache.TryGetValue(skillGroup, out var cached))
                    playerLevel = cached;
                else
                    playerLevel = GameReflection.GetPlayerCraftingLevel(player, skillGroup);
                var requiredLevel = GameReflection.GetRequiredLevelForItemForUnitTest(itemClass, itemValue, progression);
                ModApi.DebugLog($"[LimitByCraftingSkill] IsItemRestricted: skillGroup=\"{skillGroup}\" lookup=\"{lookupName}\" playerLevel={playerLevel} required={requiredLevel}");
            }
            return restricted;
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
