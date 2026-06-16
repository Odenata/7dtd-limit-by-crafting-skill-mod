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
                ItemStack dragStack = UiDragDropReflection.GetDragStackFromXUiChild(__instance);
                if (dragStack == null || dragStack.IsEmpty()) return true;
                if (!RestrictionHelper.IsItemRestricted(dragStack)) return true;

                RestrictionFeedback.ShowRestrictionPopupForBlockedItemStack(dragStack);
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
    }
}
