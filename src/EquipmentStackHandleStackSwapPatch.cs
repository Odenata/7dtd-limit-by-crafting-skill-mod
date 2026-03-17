using System;
using System.Reflection;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Blocks drag-drop of restricted items onto equipment slots. HandleStackSwap is invoked when the user
    /// drops an item onto an equipment slot; we block before the move so the item stays on the cursor.
    /// Applied manually in ModApi from the game assembly (XUiC_EquipmentStack).
    /// </summary>
    internal static class EquipmentStackHandleStackSwapPatch
    {
        public static bool Prefix(object __instance)
        {
            try
            {
                ItemStack dragStack = GetDragStack(__instance);
                if (dragStack == null || dragStack.IsEmpty()) return true;
                if (!RestrictionHelper.IsItemRestricted(dragStack)) return true;

                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] HandleStackSwap BLOCKED: restricted item drag onto equipment slot");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog($"[LimitByCraftingSkill] HandleStackSwap patch error: {ex.Message}");
                return true;
            }
        }

        private static ItemStack GetDragStack(object equipmentStackController)
        {
            if (equipmentStackController == null) return null;
            try
            {
                object xui = GetPropertyOrField(equipmentStackController, "xui");
                if (xui == null)
                {
                    var parent = GetPropertyOrField(equipmentStackController, "Parent") ?? GetPropertyOrField(equipmentStackController, "parent");
                    while (parent != null)
                    {
                        xui = GetPropertyOrField(parent, "xui");
                        if (xui != null) break;
                        parent = GetPropertyOrField(parent, "Parent") ?? GetPropertyOrField(parent, "parent");
                    }
                }
                if (xui == null) return null;
                object dragAndDrop = GetPropertyOrField(xui, "dragAndDrop") ?? GetPropertyOrField(xui, "DragAndDrop");
                if (dragAndDrop == null) return null;
                object stack = GetPropertyOrField(dragAndDrop, "CurrentStack") ?? GetPropertyOrField(dragAndDrop, "itemStack");
                return stack as ItemStack;
            }
            catch
            {
                return null;
            }
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
