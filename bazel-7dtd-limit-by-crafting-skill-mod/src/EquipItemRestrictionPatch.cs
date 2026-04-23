using System;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Restricts equipping armor/equipment by crafting level at EquipItem, before the item is moved.
    /// When restricted we skip the original and return the stack unchanged so the item is never lost.
    /// Applied manually in ModApi from the game assembly so we patch the correct type (see ApplyEquipItemPatchFromGameAssembly).
    /// </summary>
    internal static class EquipItemRestrictionPatch
    {
        static bool Prefix(ItemStack _stack, ref ItemStack __result)
        {
            try
            {
                if (_stack == null || _stack.IsEmpty()) return true;
                var itemValue = RestrictionHelper.GetItemValue(_stack);
                if (itemValue == null) return true;
                var itemClass = itemValue.ItemClass;
                if (itemClass == null) return true;

                var skillGroup = GameReflection.GetCraftingSkillGroup(itemClass, itemValue);
                if (string.IsNullOrWhiteSpace(skillGroup)) return true;
                var configName = GameReflection.ToProgressionOrConfigName(skillGroup);
                if (!ModConfig.Instance.IsRestrictionEnabledForSkill(configName)) return true;

                var player = GameReflection.GetLocalPlayer();
                if (player == null) return true;

                var requiredLevel = GameReflection.GetRequiredLevelForItem(itemClass, itemValue);
                var playerLevel = GameReflection.GetPlayerCraftingLevel(player, skillGroup);
                if (!LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true)) return true;

                RestrictionFeedback.ShowRestrictionPopupForBlockedItemStack(_stack);
                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog($"EquipItem BLOCKED: {configName} player={playerLevel} required={requiredLevel}");
                __result = _stack;
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog($"EquipItem patch error: {ex.Message}");
                return true;
            }
        }

    }
}
