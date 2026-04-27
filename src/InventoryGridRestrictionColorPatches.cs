using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// When inventory/equipment grids open or when restriction colors are dirty (e.g. after level-up),
    /// applies red label color to restricted items.
    /// Applied manually in ModApi from the game assembly so we don't require grid types in the mock.
    /// </summary>
    internal static class ItemStackGridOnOpenPatch
    {
        public static void Postfix(object __instance)
        {
            RestrictionLabelColor.MarkItemStackGridJustOpened();
            RestrictionLabelColor.ApplyRestrictionColorsToGrid(__instance);
        }
    }

    internal static class EquipmentStackGridOnOpenPatch
    {
        public static void Postfix(object __instance)
        {
            RestrictionLabelColor.MarkEquipmentGridJustOpened();
            RestrictionLabelColor.ApplyRestrictionColorsToGrid(__instance);
        }
    }

    internal static class ItemStackGridUpdatePatch
    {
        public static void Postfix(object __instance)
        {
            if (__instance == null) return;
            if (!GetIsOpen(__instance)) return;
            if (!RestrictionLabelColor.RestrictionColorsDirty) return;
            RestrictionLabelColor.ApplyRestrictionColorsToGrid(__instance);
            RestrictionLabelColor.RestrictionColorsDirty = false;
        }

        internal static bool GetIsOpen(object controller)
        {
            try
            {
                var t = controller.GetType();
                var prop = t.GetProperty("IsOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null) return true.Equals(prop.GetValue(controller, null));
                var field = t.GetField("IsOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return field != null && true.Equals(field.GetValue(controller));
            }
            catch { return false; }
        }
    }

    internal static class EquipmentStackGridUpdatePatch
    {
        public static void Postfix(object __instance)
        {
            if (__instance == null) return;
            if (!ItemStackGridUpdatePatch.GetIsOpen(__instance)) return;
            if (!RestrictionLabelColor.RestrictionColorsDirty) return;
            RestrictionLabelColor.ApplyRestrictionColorsToGrid(__instance);
            RestrictionLabelColor.RestrictionColorsDirty = false;
        }
    }

    /// <summary>
    /// Postfix for XUiController.Update. Only runs logic when instance is ItemStackGrid or EquipmentStackGrid (or subclass),
    /// since Update is declared on the base type and Harmony requires patching the declarer.
    /// Uses base-type detection so Backpack, Toolbelt, PartList, VehicleContainer, WorkstationGrid, etc. are included.
    /// Throttles apply per grid instance (e.g. every 0.2s) when open; always runs when dirty (level-up) or on open.
    /// </summary>
    internal static class GridUpdateRestrictionColorPatch
    {
        private static readonly Dictionary<object, float> _lastRunByGrid = new Dictionary<object, float>();
        private const float ThrottleSeconds = 0.2f;
        private const int MaxTrackedGridControllers = 128;

        public static void Postfix(object __instance)
        {
            if (__instance == null) return;
            var controllerType = __instance.GetType();
            var asm = controllerType.Assembly;
            var itemStackGridType = asm.GetType("XUiC_ItemStackGrid");
            var equipmentStackGridType = asm.GetType("XUiC_EquipmentStackGrid");
            string name = controllerType.FullName ?? controllerType.Name;
            bool isItemStackGrid = itemStackGridType != null && itemStackGridType.IsAssignableFrom(controllerType)
                || (itemStackGridType == null && name.IndexOf("ItemStackGrid", StringComparison.OrdinalIgnoreCase) >= 0);
            bool isEquipmentStackGrid = equipmentStackGridType != null && equipmentStackGridType.IsAssignableFrom(controllerType)
                || (equipmentStackGridType == null && name.IndexOf("EquipmentStackGrid", StringComparison.OrdinalIgnoreCase) >= 0);

            if (!isItemStackGrid && !isEquipmentStackGrid) return;

            bool isOpen = ItemStackGridUpdatePatch.GetIsOpen(__instance);
            if (!isOpen)
            {
                lock (_lastRunByGrid)
                {
                    _lastRunByGrid.Remove(__instance);
                }
                return;
            }

            float now = UnityEngine.Time.time;
            bool runBecauseDirty = RestrictionLabelColor.RestrictionColorsDirty;
            bool runBecauseThrottle = false;
            lock (_lastRunByGrid)
            {
                if (!_lastRunByGrid.TryGetValue(__instance, out float lastRun) || (now - lastRun >= ThrottleSeconds))
                    runBecauseThrottle = true;
            }

            if (!runBecauseDirty && !runBecauseThrottle) return;

            RestrictionLabelColor.ApplyRestrictionColorsToGrid(__instance);
            RestrictionLabelColor.RestrictionColorsDirty = false;
            lock (_lastRunByGrid)
            {
                // XUi controllers normally transition through IsOpen=false, where we remove them. If a game update destroys
                // controllers without closing, cap the cache so a long session cannot retain an unbounded number of grids.
                if (_lastRunByGrid.Count > MaxTrackedGridControllers)
                    _lastRunByGrid.Clear();
                _lastRunByGrid[__instance] = now;
            }
        }
    }
}
