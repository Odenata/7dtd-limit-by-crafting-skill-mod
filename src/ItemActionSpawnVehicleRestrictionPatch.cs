using System;
using System.Reflection;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Blocks deploying a vehicle from the held item when Vehicles restrictions apply.
    /// Client-only prefix; dedicated servers should run the same mod for consistent enforcement.
    /// </summary>
    internal static class ItemActionSpawnVehicleRestrictionPatch
    {
        public static bool Prefix(object __instance, object _actionData, bool _bReleased)
        {
            try
            {
                if (!_bReleased) return true;
                if (ModConfig.Instance == null || !ModConfig.Instance.IsRestrictionEnabledForSkill("Vehicles"))
                    return true;

                var invData = UiDragDropReflection.GetPropertyOrField(_actionData, "invData");
                if (invData == null) return true;
                var holdingEntity = UiDragDropReflection.GetPropertyOrField(invData, "holdingEntity");
                if (holdingEntity == null || holdingEntity.GetType().Name.IndexOf("EntityPlayer", StringComparison.Ordinal) < 0)
                    return true;

                object inventory = UiDragDropReflection.GetPropertyOrField(holdingEntity, "inventory");
                if (inventory == null) return true;
                ItemValue iv = UiDragDropReflection.GetPropertyOrField(inventory, "holdingItemItemValue") as ItemValue;
                if (iv == null)
                {
                    var hi = UiDragDropReflection.GetPropertyOrField(inventory, "holdingItem");
                    if (hi is ItemValue hiv) iv = hiv;
                }
                if (iv == null) return true;

                var stack = new ItemStack(iv, 1);
                if (!RestrictionHelper.IsItemRestricted(stack)) return true;

                RestrictionFeedback.ShowRestrictionPopupForBlockedItemStack(stack);
                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] ItemActionSpawnVehicle.ExecuteAction BLOCKED (restricted vehicle item)");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] SpawnVehicle patch error: " + ex.Message);
                return true;
            }
        }
    }
}
