using System;
using System.Reflection;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Blocks right-click (and similar) "move to preferred location" when the item would go to the toolbelt
    /// and the item is restricted. HandleMoveToPreferredLocation is called on the source slot (e.g. backpack);
    /// we block before the move so the item stays in place.
    /// Applied manually in ModApi from the game assembly (XUiC_ItemStack).
    /// </summary>
    internal static class ToolbeltHandleMoveToPreferredLocationPatch
    {
        public static bool Prefix(object __instance)
        {
            try
            {
                ItemStack stack = GetItemStack(__instance);
                if (stack == null || stack.IsEmpty()) return true;
                if (!RestrictionHelper.IsItemRestricted(stack)) return true;
                if (!IsSourceBackpack(__instance)) return true;

                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] HandleMoveToPreferredLocation BLOCKED: restricted item move to toolbelt");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog($"[LimitByCraftingSkill] HandleMoveToPreferredLocation patch error: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// True when this slot's grid is the player backpack (so "preferred location" is typically toolbelt).
        /// </summary>
        private static bool IsSourceBackpack(object itemStackController)
        {
            if (itemStackController == null) return false;
            object parent = GetPropertyOrField(itemStackController, "Parent") ?? GetPropertyOrField(itemStackController, "parent");
            while (parent != null)
            {
                object stackLocation = GetPropertyOrField(parent, "StackLocation");
                if (stackLocation != null && IsStackLocationBackpack(stackLocation))
                    return true;
                var parentType = parent.GetType();
                string name = parentType.FullName ?? parentType.Name;
                if (name.IndexOf("XUiC_Backpack", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                parent = GetPropertyOrField(parent, "Parent") ?? GetPropertyOrField(parent, "parent");
            }
            return false;
        }

        private static bool IsStackLocationBackpack(object stackLocationValue)
        {
            if (stackLocationValue == null) return false;
            var type = stackLocationValue.GetType();
            var asm = type.Assembly;
            var stackLocationType = asm.GetType("XUiC_ItemStack+StackLocationTypes");
            if (stackLocationType == null) return false;
            var backpackField = stackLocationType.GetField("Backpack", BindingFlags.Public | BindingFlags.Static);
            if (backpackField == null) return false;
            var backpackEnum = backpackField.GetValue(null);
            return backpackEnum != null && stackLocationValue.Equals(backpackEnum);
        }

        private static ItemStack GetItemStack(object controller)
        {
            if (controller == null) return null;
            object stack = GetPropertyOrField(controller, "ItemStack") ?? GetPropertyOrField(controller, "itemStack");
            return stack as ItemStack;
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
