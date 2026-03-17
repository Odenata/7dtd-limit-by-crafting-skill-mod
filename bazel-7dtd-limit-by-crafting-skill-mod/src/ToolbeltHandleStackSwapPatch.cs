using System;
using System.Reflection;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Blocks drag-drop of restricted items onto toolbelt (hotbar) slots. HandleStackSwap is invoked when the user
    /// drops an item onto a toolbelt slot; we block before the move so the item stays on the cursor.
    /// Applied manually in ModApi from the game assembly (XUiC_ItemStack).
    /// </summary>
    internal static class ToolbeltHandleStackSwapPatch
    {
        public static bool Prefix(object __instance)
        {
            try
            {
                if (!IsToolbeltSlot(__instance)) return true;

                ItemStack dragStack = GetDragStack(__instance);
                if (dragStack == null || dragStack.IsEmpty()) return true;
                if (!RestrictionHelper.IsItemRestricted(dragStack)) return true;

                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] HandleStackSwap BLOCKED: restricted item onto toolbelt");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog($"[LimitByCraftingSkill] Toolbelt HandleStackSwap patch error: {ex.Message}");
                return true;
            }
        }

        private static bool IsToolbeltSlot(object itemStackController)
        {
            if (itemStackController == null) return false;
            object parent = GetPropertyOrField(itemStackController, "Parent") ?? GetPropertyOrField(itemStackController, "parent");
            while (parent != null)
            {
                var parentType = parent.GetType();
                string name = parentType.FullName ?? parentType.Name;
                if (name.IndexOf("XUiC_Toolbelt", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                var asm = parentType.Assembly;
                var toolbeltType = asm.GetType("XUiC_Toolbelt");
                if (toolbeltType != null && toolbeltType.IsAssignableFrom(parentType))
                    return true; // parent is XUiC_Toolbelt or subclass
                parent = GetPropertyOrField(parent, "Parent") ?? GetPropertyOrField(parent, "parent");
            }
            return false;
        }

        private static ItemStack GetDragStack(object controller)
        {
            if (controller == null) return null;
            try
            {
                object xui = GetPropertyOrField(controller, "xui");
                if (xui == null)
                {
                    var parent = GetPropertyOrField(controller, "Parent") ?? GetPropertyOrField(controller, "parent");
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
