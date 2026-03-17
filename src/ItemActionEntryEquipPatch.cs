using System;
using System.Reflection;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Blocks the "Equip" action (e.g. equip key W or context menu Equip) when the item is restricted.
    /// OnActivated is called when the player triggers equip; we block before any inventory change.
    /// Applied manually in ModApi from the game assembly (ItemActionEntryEquip).
    /// </summary>
    internal static class ItemActionEntryEquipPatch
    {
        public static bool Prefix(object __instance)
        {
            try
            {
                ItemStack stack = GetItemStackFromActionEntry(__instance);
                if (stack == null || stack.IsEmpty()) return true;
                if (!RestrictionHelper.IsItemRestricted(stack)) return true;

                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] ItemActionEntryEquip.OnActivated BLOCKED: restricted item (equip action)");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog($"[LimitByCraftingSkill] ItemActionEntryEquip patch error: {ex.Message}");
                return true;
            }
        }

        private static ItemStack GetItemStackFromActionEntry(object actionEntry)
        {
            if (actionEntry == null) return null;
            object parentItem = GetPropertyOrField(actionEntry, "ParentItem") ?? GetPropertyOrField(actionEntry, "parentItem");
            object current = parentItem;
            for (int i = 0; i < 20 && current != null; i++)
            {
                object xui = GetPropertyOrField(current, "xui") ?? GetPropertyOrField(current, "Xui");
                if (xui != null)
                {
                    object itemStackCtrl = GetPropertyOrField(xui, "itemStack") ?? GetPropertyOrField(xui, "ItemStack");
                    if (itemStackCtrl != null)
                    {
                        object stack = GetPropertyOrField(itemStackCtrl, "ItemStack") ?? GetPropertyOrField(itemStackCtrl, "itemStack");
                        if (stack is ItemStack isVal) return isVal;
                    }
                }
                current = GetPropertyOrField(current, "Parent") ?? GetPropertyOrField(current, "parent");
            }
            return null;
        }

        private static object GetPropertyOrField(object obj, string name)
        {
            if (obj == null) return null;
            var type = obj.GetType();
            var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null) return prop.GetValue(obj, null);
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(obj);
        }
    }
}
