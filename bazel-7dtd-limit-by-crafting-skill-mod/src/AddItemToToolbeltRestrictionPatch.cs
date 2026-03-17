using System;
using System.Reflection;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Blocks the equip key (e.g. W) from adding restricted items to the toolbelt.
    /// The game calls XUiM_PlayerInventory.AddItemToToolbelt or AddItemToPreferredToolbeltSlot
    /// when the player selects an item and presses the equip key; we block before the add.
    /// Applied manually in ModApi from the game assembly.
    /// </summary>
    internal static class AddItemToToolbeltRestrictionPatch
    {
        /// <summary>Prefix for AddItemToToolbelt(ItemStack). Returns false to block and set __result to false.</summary>
        public static bool Prefix(object __instance, object _itemStack, ref bool __result)
        {
            return CheckAndBlock(__instance, _itemStack, ref __result);
        }

        /// <summary>Prefix for AddItemToPreferredToolbeltSlot(ItemStack, int). Returns false to block and set __result to false.</summary>
        public static bool PrefixPreferredSlot(object __instance, object _itemStack, int _slot, ref bool __result)
        {
            return CheckAndBlock(__instance, _itemStack, ref __result);
        }

        private static bool CheckAndBlock(object __instance, object itemStackObj, ref bool __result)
        {
            try
            {
                var stack = itemStackObj as ItemStack;
                if (stack == null || stack.IsEmpty()) return true;
                if (!RestrictionHelper.IsItemRestricted(stack)) return true;

                __result = false;
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] AddItemToToolbelt BLOCKED: restricted item (equip key)");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog($"[LimitByCraftingSkill] AddItemToToolbelt patch error: {ex.Message}");
                return true;
            }
        }
    }
}
