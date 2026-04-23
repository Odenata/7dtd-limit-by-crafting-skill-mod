using System;
using System.Reflection;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Blocks placing restricted tools on workstation tool grids (forge hammer slot, etc.).
    /// </summary>
    internal static class WorkstationToolHandleStackSwapPatch
    {
        public static bool Prefix(object __instance)
        {
            try
            {
                if (!IsUnderWorkstationToolGrid(__instance)) return true;
                var drag = UiDragDropReflection.GetDragStackFromXUiChild(__instance);
                if (drag == null || drag.IsEmpty()) return true;
                if (!RestrictionHelper.IsItemRestricted(drag)) return true;
                RestrictionFeedback.ShowRestrictionPopupForBlockedItemStack(drag);
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] Workstation tool slot HandleStackSwap BLOCKED");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] Workstation tool patch error: " + ex.Message);
                return true;
            }
        }

        private static bool IsUnderWorkstationToolGrid(object controller)
        {
            object parent = UiDragDropReflection.GetPropertyOrField(controller, "Parent") ?? UiDragDropReflection.GetPropertyOrField(controller, "parent");
            while (parent != null)
            {
                var n = parent.GetType().FullName ?? parent.GetType().Name;
                if (n.IndexOf("XUiC_WorkstationToolGrid", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                parent = UiDragDropReflection.GetPropertyOrField(parent, "Parent") ?? UiDragDropReflection.GetPropertyOrField(parent, "parent");
            }
            return false;
        }
    }

    /// <summary>
    /// Blocks installing restricted vehicle mods (Vehicles skill group) via part slots.
    /// </summary>
    internal static class VehiclePartHandleStackSwapPatch
    {
        public static bool Prefix(object __instance)
        {
            try
            {
                if (!IsUnderVehiclePartGrid(__instance)) return true;
                if (ModConfig.Instance == null || !ModConfig.Instance.IsRestrictionEnabledForSkill("Vehicles"))
                    return true;
                var drag = UiDragDropReflection.GetDragStackFromXUiChild(__instance);
                if (drag == null || drag.IsEmpty()) return true;
                var iv = RestrictionHelper.GetItemValue(drag);
                var ic = iv?.ItemClass;
                if (ic == null) return true;
                var sg = GameReflection.GetCraftingSkillGroup(ic);
                if (!string.Equals(sg, "Vehicles", StringComparison.OrdinalIgnoreCase)) return true;
                if (!RestrictionHelper.IsItemRestricted(drag)) return true;
                RestrictionFeedback.ShowRestrictionPopupForBlockedItemStack(drag);
                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] Vehicle part slot HandleStackSwap BLOCKED");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] Vehicle part patch error: " + ex.Message);
                return true;
            }
        }

        private static bool IsUnderVehiclePartGrid(object controller)
        {
            object parent = UiDragDropReflection.GetPropertyOrField(controller, "Parent") ?? UiDragDropReflection.GetPropertyOrField(controller, "parent");
            while (parent != null)
            {
                var n = parent.GetType().FullName ?? parent.GetType().Name;
                if (n.IndexOf("XUiC_VehiclePartStackGrid", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                parent = UiDragDropReflection.GetPropertyOrField(parent, "Parent") ?? UiDragDropReflection.GetPropertyOrField(parent, "parent");
            }
            return false;
        }
    }

    internal static class UiDragDropReflection
    {
        internal static ItemStack GetDragStackFromXUiChild(object controller)
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

        internal static object GetPropertyOrField(object obj, string name)
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
