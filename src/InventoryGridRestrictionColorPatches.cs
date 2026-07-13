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
    /// When a grid slot changes (drag/drop, pickup, etc.), mark restriction colors dirty so open grids re-apply.
    /// Vanilla label refresh after moves otherwise clears our red tint until the next OnOpen.
    /// </summary>
    internal static class ItemStackGridSlotChangedRestrictionColorPatch
    {
        public static void Postfix(object __instance)
        {
            RestrictionLabelColor.MarkColorsDirty();
        }
    }

    internal static class EquipmentStackGridSlotChangedRestrictionColorPatch
    {
        public static void Postfix(object __instance)
        {
            RestrictionLabelColor.MarkColorsDirty();
        }
    }

    /// <summary>
    /// After vanilla ForceSetItemStack redraws the slot label, re-apply restriction tint immediately.
    /// Covers backpack sync paths (UpdateBackend / RefreshBackpackSlots) that skip HandleSlotChangedEvent.
    /// </summary>
    internal static class ItemStackForceSetRestrictionColorPatch
    {
        public static void Postfix(object __instance)
        {
            RestrictionLabelColor.MarkColorsDirty();
            RestrictionLabelColor.ApplyRestrictionColorToItemStackController(__instance);
        }
    }

    /// <summary>Grid-level backend/stack sync — marks dirty so any slot vanilla redraws get recolored on Update.</summary>
    internal static class ItemStackGridBackendRestrictionColorPatch
    {
        public static void Postfix(object __instance)
        {
            RestrictionLabelColor.MarkColorsDirty();
        }
    }

    /// <summary>
    /// Postfix for XUiController.Update. Only runs logic when instance is ItemStackGrid or EquipmentStackGrid (or subclass),
    /// since Update is declared on the base type and Harmony requires patching the declarer.
    /// Uses base-type detection so Backpack, Toolbelt, PartList, VehicleContainer, WorkstationGrid, etc. are included.
    /// Re-applies on dirty (slot change / level-up) or briefly after OnOpen; no forever 5 Hz throttle.
    /// </summary>
    internal static class GridUpdateRestrictionColorPatch
    {
        private static readonly Dictionary<object, float> _lastRunByGrid = new Dictionary<object, float>();
        private const float JustOpenedReapplyWindowSeconds = 0.15f;
        private const float ReapplyMinIntervalSeconds = 0.05f;
        private const int MaxTrackedGridControllers = 128;

        private static Assembly _cachedAssembly;
        private static Type _cachedItemStackGridType;
        private static Type _cachedEquipmentStackGridType;

        private static void EnsureGridTypesCached(Assembly asm)
        {
            if (asm == null) return;
            if (ReferenceEquals(_cachedAssembly, asm)) return;
            _cachedAssembly = asm;
            _cachedItemStackGridType = asm.GetType("XUiC_ItemStackGrid");
            _cachedEquipmentStackGridType = asm.GetType("XUiC_EquipmentStackGrid");
        }

        public static void Postfix(object __instance)
        {
            if (__instance == null) return;
            var controllerType = __instance.GetType();
            var asm = controllerType.Assembly;
            EnsureGridTypesCached(asm);
            var itemStackGridType = _cachedItemStackGridType;
            var equipmentStackGridType = _cachedEquipmentStackGridType;
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
            bool inDirtyWindow = (now - RestrictionLabelColor.LastColorsDirtyTime) <= RestrictionLabelColor.DirtyReapplyWindowSeconds;
            bool runBecauseDirty = RestrictionLabelColor.RestrictionColorsDirty || inDirtyWindow;
            float lastOpenTime = isItemStackGrid
                ? RestrictionLabelColor.LastItemStackGridOpenTime
                : RestrictionLabelColor.LastEquipmentGridOpenTime;
            bool inJustOpenedWindow = (now - lastOpenTime) <= JustOpenedReapplyWindowSeconds;

            bool shouldRun = false;
            if (runBecauseDirty || inJustOpenedWindow)
            {
                lock (_lastRunByGrid)
                {
                    if (!_lastRunByGrid.TryGetValue(__instance, out float lastRun)
                        || (now - lastRun >= ReapplyMinIntervalSeconds))
                        shouldRun = true;
                }
            }

            if (!shouldRun) return;

            RestrictionLabelColor.ApplyRestrictionColorsToGrid(__instance);
            if (!inDirtyWindow)
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
