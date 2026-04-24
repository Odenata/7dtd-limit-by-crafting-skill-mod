using System;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Blocks primary item use (throw, place block, rocket fire, eat/drink/meds) when the held item is crafting-restricted.
    /// </summary>
    internal static class ItemActionExecuteRestrictionPatch
    {
        /// <summary>Food, drinks, and most medical items use <c>ItemActionEat</c>; validate on mouse release like throw (avoids spamming checks every hold tick).</summary>
        internal static bool PrefixEat(object __instance, object _actionData, bool _bReleased)
        {
            return PrefixCore(_actionData, _bReleased, requireRelease: true);
        }

        /// <summary>Inventory / UI instant use path (e.g. context action) that bypasses <see cref="PrefixEat"/>.</summary>
        internal static bool PrefixEatExecuteInstant(object __instance, EntityAlive ent, ItemStack stack, bool isHeldItem, XUiC_ItemStack stackController)
        {
            try
            {
                if (ModConfig.Instance == null) return true;
                if (stack == null || stack.IsEmpty()) return true;
                if (!RestrictionHelper.IsItemRestricted(stack)) return true;
                RestrictionFeedback.ShowRestrictionPopupForBlockedItemStack(stack);
                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] ItemActionEat.ExecuteInstantAction BLOCKED");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] ItemActionEat.ExecuteInstantAction restriction error: " + ex.Message);
                return true;
            }
        }

        internal static bool PrefixThrowAway(object __instance, object _actionData, bool _bReleased)
        {
            return PrefixCore(_actionData, _bReleased, requireRelease: true);
        }

        internal static bool PrefixThrownWeapon(object __instance, object _actionData, bool _bReleased)
        {
            return PrefixCore(_actionData, _bReleased, requireRelease: true);
        }

        internal static bool PrefixPlaceAsBlock(object __instance, object _actionData, bool _bReleased)
        {
            return PrefixCore(_actionData, _bReleased, requireRelease: false);
        }

        internal static bool PrefixProjectile(object __instance, object _actionData, bool _bReleased)
        {
            return PrefixCore(_actionData, _bReleased, requireRelease: true);
        }

        private static bool PrefixCore(object _actionData, bool _bReleased, bool requireRelease)
        {
            try
            {
                if (requireRelease && !_bReleased) return true;
                if (ModConfig.Instance == null) return true;

                var stack = TryGetHeldStackFromActionData(_actionData);
                if (stack == null || stack.IsEmpty()) return true;

                if (!RestrictionHelper.IsItemRestricted(stack)) return true;

                RestrictionFeedback.ShowRestrictionPopupForBlockedItemStack(stack);
                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] ItemAction.ExecuteAction BLOCKED (use / throw / place)");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] ItemAction execute restriction error: " + ex.Message);
                return true;
            }
        }

        private static ItemStack TryGetHeldStackFromActionData(object actionData)
        {
            if (actionData == null) return null;
            var invData = UiDragDropReflection.GetPropertyOrField(actionData, "invData");
            if (invData == null) return null;

            var stack = UiDragDropReflection.GetPropertyOrField(invData, "itemStack") as ItemStack;
            if (stack != null && !stack.IsEmpty()) return stack;

            if (UiDragDropReflection.GetPropertyOrField(invData, "itemValue") is ItemValue iv && iv != null)
                return new ItemStack(iv, 1);

            try
            {
                var holdingEntity = UiDragDropReflection.GetPropertyOrField(invData, "holdingEntity");
                if (holdingEntity == null) return null;
                object inventory = UiDragDropReflection.GetPropertyOrField(holdingEntity, "inventory");
                if (inventory == null) return null;
                ItemValue hiv = UiDragDropReflection.GetPropertyOrField(inventory, "holdingItemItemValue") as ItemValue;
                if (hiv == null)
                {
                    var hi = UiDragDropReflection.GetPropertyOrField(inventory, "holdingItem");
                    if (hi is ItemValue hiVal) hiv = hiVal;
                }
                if (hiv != null && hiv.ItemClass != null)
                    return new ItemStack(hiv, 1);
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }
}
