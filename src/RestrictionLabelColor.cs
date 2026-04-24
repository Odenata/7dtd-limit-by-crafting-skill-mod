using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Applies red label color to restricted items in any inventory UI (player inventory, containers, vehicle, workstation, equipment, etc.).
    /// Uses the same restriction check (RestrictionHelper.IsItemRestricted) for all item types. Triggered on grid OnOpen and when crafting skill level changes (dirty flag + grid Update).
    /// </summary>
    internal static class RestrictionLabelColor
    {
        /// <summary>Set by progression level-up patch; cleared when a grid refreshes its labels.</summary>
        public static bool RestrictionColorsDirty { get; set; }

        /// <summary>Set by OnOpen patches so Update can run apply again in the "just opened" window (e.g. 0.15s) without throttle.</summary>
        public static float LastItemStackGridOpenTime { get; set; }
        public static float LastEquipmentGridOpenTime { get; set; }

        /// <summary>Call from ItemStackGrid OnOpen postfix so Update runs apply again shortly without waiting for throttle.</summary>
        public static void MarkItemStackGridJustOpened() { LastItemStackGridOpenTime = UnityEngine.Time.time; }

        /// <summary>Call from EquipmentStackGrid OnOpen postfix so Update runs apply again shortly without waiting for throttle.</summary>
        public static void MarkEquipmentGridJustOpened() { LastEquipmentGridOpenTime = UnityEngine.Time.time; }

        private static readonly UnityEngine.Color RedColor = new UnityEngine.Color(1f, 0f, 0f, 1f);

        /// <summary>Restore item name labels when a slot is empty or no longer restricted (swap/move leaves stale red).</summary>
        private static readonly UnityEngine.Color InventoryLabelDefaultColor = UnityEngine.Color.white;

        /// <summary>
        /// Applies restriction coloring to a grid: for each slot with an item, if restricted set label red.
        /// Detects grid by base type (XUiC_ItemStackGrid / XUiC_EquipmentStackGrid) so all subclasses (Backpack, Toolbelt, PartList, VehicleContainer, WorkstationGrid, etc.) are covered.
        /// </summary>
        public static void ApplyRestrictionColorsToGrid(object grid)
        {
            if (grid == null) return;
            try
            {
                var gridType = grid.GetType();
                string typeName = gridType.FullName ?? gridType.Name;

                var asm = gridType.Assembly;
                var itemStackGridType = asm.GetType("XUiC_ItemStackGrid");
                var equipmentStackGridType = asm.GetType("XUiC_EquipmentStackGrid");
                bool isItemStackGrid = itemStackGridType != null && itemStackGridType.IsAssignableFrom(gridType)
                    || (itemStackGridType == null && typeName.IndexOf("ItemStackGrid", StringComparison.OrdinalIgnoreCase) >= 0);
                bool isEquipmentStackGrid = equipmentStackGridType != null && equipmentStackGridType.IsAssignableFrom(gridType)
                    || (equipmentStackGridType == null && typeName.IndexOf("EquipmentStackGrid", StringComparison.OrdinalIgnoreCase) >= 0);

                // ItemStackGrid and subclasses: backpack, toolbelt, container, vehicle, workstation, part list, etc.
                // Always use SetLabelColorOnEntry(go) so the slot's GameObject tree (item name label) gets colored; TrySetColorOnLabelView often hits "timer" only, not the name.
                if (isItemStackGrid)
                {
                    var controllers = GetItemStackControllers(grid, gridType);
                    if (controllers != null)
                    {
                        foreach (var ctrl in controllers)
                        {
                            if (ctrl == null) continue;
                            var go = GetViewGameObject(ctrl);
                            if (go == null) continue;
                            var stack = GetItemStackFromController(ctrl);
                            if (stack == null || stack.IsEmpty())
                            {
                                SetLabelColorOnEntry(go, InventoryLabelDefaultColor);
                                continue;
                            }
                            if (RestrictionHelper.IsItemRestricted(stack))
                                SetLabelColorOnEntry(go, RedColor);
                            else
                                SetLabelColorOnEntry(go, InventoryLabelDefaultColor);
                        }
                    }
                    return;
                }

                // EquipmentStackGrid: character equipment slots
                if (isEquipmentStackGrid)
                {
                    var slotControllers = GetEquipmentSlotControllers(grid, gridType);
                    if (slotControllers != null)
                    {
                        // Prefer grid's own items array (same order as slot enum); fallback to player.equipment.GetSlotItem(index)
                        var gridItems = GetPropertyOrField(grid, "items");
                        ItemValue[] gridItemsArray = gridItems as ItemValue[];

                        var player = GameReflection.GetLocalPlayer();
                        object equipment = null;
                        MethodInfo getSlotItemMethod = null;
                        int equipmentSlotCount = -1;
                        if (player != null)
                        {
                            equipment = GetPropertyOrField(player, "equipment") ?? GetPropertyOrField(player, "Equipment");
                            if (equipment != null)
                            {
                                var eqType = equipment.GetType();
                                getSlotItemMethod = eqType.GetMethod("GetSlotItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);
                                var getSlotCountMethod = eqType.GetMethod("GetSlotCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                if (getSlotCountMethod != null)
                                {
                                    var countObj = getSlotCountMethod.Invoke(equipment, null);
                                    if (countObj is int c) equipmentSlotCount = c;
                                }
                            }
                        }

                        foreach (var ctrl in slotControllers)
                        {
                            if (ctrl == null) continue;
                            var stack = GetItemStackFromEquipmentController(ctrl);
                            if (stack == null || stack.IsEmpty())
                            {
                                var slotEnumVal = GetPropertyOrField(ctrl, "EquipSlot") ?? GetPropertyOrField(ctrl, "equipSlot");
                                if (slotEnumVal != null && slotEnumVal.GetType().IsEnum)
                                {
                                    int slotIndex = (int)slotEnumVal;
                                    if (slotIndex >= 0)
                                    {
                                        if (gridItemsArray != null && slotIndex < gridItemsArray.Length)
                                        {
                                            var iv = gridItemsArray[slotIndex];
                                            if (iv != null && !iv.IsEmpty())
                                                stack = new ItemStack(iv, 1);
                                        }
                                        if ((stack == null || stack.IsEmpty()) && equipment != null && getSlotItemMethod != null
                                            && (equipmentSlotCount < 0 || slotIndex < equipmentSlotCount))
                                        {
                                            try
                                            {
                                                var slotItem = getSlotItemMethod.Invoke(equipment, new object[] { slotIndex });
                                                if (slotItem is ItemValue iv && iv != null && !iv.IsEmpty())
                                                    stack = new ItemStack(iv, 1);
                                            }
                                            catch { /* index may be out of range for this game version */ }
                                        }
                                    }
                                }
                            }
                            if (stack == null || stack.IsEmpty())
                            {
                                TrySetColorOnLabelView(ctrl, InventoryLabelDefaultColor);
                                var goEmpty = GetViewGameObject(ctrl);
                                if (goEmpty != null)
                                    SetLabelColorOnEntry(goEmpty, InventoryLabelDefaultColor);
                                continue;
                            }
                            if (RestrictionHelper.IsItemRestricted(stack))
                            {
                                if (TrySetColorOnLabelView(ctrl, RedColor))
                                    continue;
                                var go = GetViewGameObject(ctrl);
                                if (go != null)
                                    SetLabelColorOnEntry(go, RedColor);
                            }
                            else
                            {
                                TrySetColorOnLabelView(ctrl, InventoryLabelDefaultColor);
                                var go = GetViewGameObject(ctrl);
                                if (go != null)
                                    SetLabelColorOnEntry(go, InventoryLabelDefaultColor);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog($"[RestrictionLabelColor] ApplyRestrictionColorsToGrid error: {ex.Message}");
            }
        }

        /// <summary>Gets the view's GameObject from an XUiController (ViewComponent.uiTransform.gameObject).</summary>
        internal static object GetViewGameObject(object controller)
        {
            if (controller == null) return null;
            try
            {
                var view = GetPropertyOrField(controller, "ViewComponent") ?? GetPropertyOrField(controller, "viewComponent");
                if (view == null) return null;
                var transform = GetPropertyOrField(view, "uiTransform") ?? GetPropertyOrField(view, "UiTransform");
                if (transform == null) return null;
                var gameObject = GetPropertyOrField(transform, "gameObject");
                return gameObject;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Try to set color on XUiV_Label (e.g. stackValue on equipment slots, or any Label/View on item stack). Label view has Color property and may have a nested UILabel.</summary>
        private static bool TrySetColorOnLabelView(object controller, UnityEngine.Color color)
        {
            if (controller == null) return false;
            try
            {
                var namesToTry = new[] { "stackValue", "StackValue", "itemName", "ItemName", "timer", "Timer" };
                foreach (var name in namesToTry)
                {
                    var labelView = GetPropertyOrField(controller, name);
                    if (labelView == null) continue;
                    if (TrySetColorOnLabelViewInstance(labelView, color)) return true;
                }
                // Scan controller for any view-like member with Color (e.g. XUiV_Label on XUiC_ItemStack)
                var ctrlType = controller.GetType();
                foreach (var member in ctrlType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    object viewObj = null;
                    if (member is FieldInfo fi && !fi.IsStatic) viewObj = fi.GetValue(controller);
                    else if (member is PropertyInfo pi && pi.CanRead)
                    {
                        var getter = pi.GetGetMethod(true);
                        if (getter != null && !getter.IsStatic) viewObj = pi.GetValue(controller, null);
                    }
                    if (viewObj == null) continue;
                    var t = viewObj.GetType();
                    if (t.Name.IndexOf("Label", StringComparison.OrdinalIgnoreCase) < 0 && t.Name.IndexOf("View", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (TrySetColorOnLabelViewInstance(viewObj, color)) return true;
                }
            }
            catch { }
            return false;
        }

        private static bool TrySetColorOnLabelViewInstance(object labelView, UnityEngine.Color color)
        {
            if (labelView == null) return false;
            try
            {
                var viewType = labelView.GetType();
                bool didSet = false;
                var colorProp = viewType.GetProperty("Color", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (colorProp != null && colorProp.PropertyType == typeof(UnityEngine.Color))
                {
                    colorProp.SetValue(labelView, color, null);
                    didSet = true;
                }
                else
                {
                    var colorField = viewType.GetField("color", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (colorField != null && colorField.FieldType == typeof(UnityEngine.Color))
                    {
                        colorField.SetValue(labelView, color);
                        didSet = true;
                    }
                }
                var nestedLabel = GetPropertyOrField(labelView, "label") ?? GetPropertyOrField(labelView, "Label");
                if (nestedLabel != null)
                {
                    TrySetLabelColor(nestedLabel, color);
                    didSet = true;
                }
                return didSet;
            }
            catch { return false; }
        }

        /// <summary>Sets UILabel color on this GameObject and children. If color is null, leaves default (no change).</summary>
        /// <param name="includeEffectColors">When true, also sets NGUI outline/effect colors (popup denied text); inventory grid uses false.</param>
        internal static void SetLabelColorOnEntry(object entryGameObject, UnityEngine.Color? color, bool includeEffectColors = false)
        {
            if (entryGameObject == null || !color.HasValue) return;
            try
            {
                var goType = entryGameObject.GetType();
                var getComponentMethod = goType.GetMethod("GetComponent", new[] { typeof(string) });
                if (getComponentMethod != null)
                {
                    var labelComponent = getComponentMethod.Invoke(entryGameObject, new object[] { "UILabel" });
                    if (labelComponent != null)
                        TrySetLabelColor(labelComponent, color.Value, includeEffectColors);
                }

                var transformProp = goType.GetProperty("transform", BindingFlags.Instance | BindingFlags.Public);
                if (transformProp == null) return;
                var transform = transformProp.GetValue(entryGameObject, null);
                if (transform == null) return;
                var transformType = transform.GetType();
                var getChildMethod = transformType.GetMethod("GetChild", new[] { typeof(int) });
                var childCountProp = transformType.GetProperty("childCount", BindingFlags.Instance | BindingFlags.Public);
                if (getChildMethod == null || childCountProp == null) return;
                int childCount = (int)childCountProp.GetValue(transform, null);
                for (int i = 0; i < childCount; i++)
                {
                    var child = getChildMethod.Invoke(transform, new object[] { i });
                    if (child != null)
                        SetLabelColorOnEntry(child, color, includeEffectColors);
                }
            }
            catch { }
        }

        private static void TrySetLabelColor(object labelComponent, UnityEngine.Color color, bool includeEffectColors = false)
        {
            if (labelComponent == null) return;
            try
            {
                var t = labelComponent.GetType();
                foreach (var name in new[] { "color", "mColor", "Color" })
                {
                    var prop = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null && prop.PropertyType == typeof(UnityEngine.Color))
                    {
                        prop.SetValue(labelComponent, color, null);
                        break;
                    }
                    var field = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field != null && field.FieldType == typeof(UnityEngine.Color))
                    {
                        field.SetValue(labelComponent, color);
                        break;
                    }
                }

                if (!includeEffectColors)
                    return;
                var fx = new UnityEngine.Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 1f);
                foreach (var name in new[] { "effectColor", "EffectColor" })
                {
                    var prop = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null && prop.PropertyType == typeof(UnityEngine.Color) && prop.CanWrite)
                    {
                        prop.SetValue(labelComponent, fx, null);
                        return;
                    }
                    var field = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field != null && field.FieldType == typeof(UnityEngine.Color))
                    {
                        field.SetValue(labelComponent, fx);
                        return;
                    }
                }
            }
            catch { }
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

        private static Array GetItemStackControllers(object grid, Type gridType)
        {
            var getMethod = gridType.GetMethod("GetItemStackControllers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getMethod != null)
            {
                var result = getMethod.Invoke(grid, null);
                return result as Array;
            }
            var controllers = GetPropertyOrField(grid, "itemControllers");
            if (controllers is Array arr) return arr;
            return null;
        }

        private static IEnumerable GetEquipmentSlotControllers(object grid, Type gridType)
        {
            var itemControllers = GetPropertyOrField(grid, "itemControllers");
            if (itemControllers != null)
            {
                var dictType = itemControllers.GetType();
                if (typeof(IDictionary).IsAssignableFrom(dictType))
                {
                    var dict = (IDictionary)itemControllers;
                    foreach (DictionaryEntry e in dict)
                    {
                        if (e.Value != null) yield return e.Value;
                    }
                    yield break;
                }
                var valuesProp = dictType.GetProperty("Values", BindingFlags.Instance | BindingFlags.Public);
                if (valuesProp != null)
                {
                    var values = valuesProp.GetValue(itemControllers, null);
                    if (values is IEnumerable en) foreach (var v in en) yield return v;
                    yield break;
                }
            }
            // Fallback: try GetSlot(EquipmentSlots) for each enum value
            var getSlotMethod = gridType.GetMethod("GetSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getSlotMethod != null)
            {
                var slotEnum = getSlotMethod.GetParameters()[0].ParameterType;
                if (slotEnum.IsEnum)
                {
                    foreach (var val in Enum.GetValues(slotEnum))
                    {
                        var slot = getSlotMethod.Invoke(grid, new[] { val });
                        if (slot != null) yield return slot;
                    }
                }
            }
        }

        private static ItemStack GetItemStackFromController(object controller)
        {
            if (controller == null) return null;
            var stack = GetPropertyOrField(controller, "ItemStack") ?? GetPropertyOrField(controller, "itemStack");
            return stack as ItemStack;
        }

        private static ItemStack GetItemStackFromEquipmentController(object controller)
        {
            if (controller == null) return null;
            var stack = GetPropertyOrField(controller, "ItemStack");
            if (stack is ItemStack s && s != null && !s.IsEmpty()) return s;
            var itemValue = GetPropertyOrField(controller, "ItemValue");
            if (itemValue is ItemValue iv && iv != null && !iv.IsEmpty())
                return new ItemStack(iv, 1);
            return null;
        }
    }
}
