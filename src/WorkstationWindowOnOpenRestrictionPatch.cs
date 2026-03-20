using System;
using System.Reflection;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Fallback UI hook: if block activation paths aren't hit for some workstations (e.g. Chemistry Station),
    /// the workstation window still opens and runs OnOpen(). We read workstationBlock from the window
    /// instance and apply the Workstations restriction there.
    /// </summary>
    internal static class WorkstationWindowOnOpenRestrictionPatch
    {
        /// <summary>
        /// Prefix for XUiC_WorkstationWindowGroup.OnOpen(). Return false to skip the game's OnOpen logic.
        /// </summary>
        public static bool Prefix(object __instance)
        {
            try
            {
                if (ModConfig.Instance == null || !ModConfig.Instance.IsRestrictionEnabledForSkill("Workstations"))
                    return true;
                if (__instance == null)
                    return true;

                var workstationBlock = GetWorkstationBlockFromWindow(__instance);
                if (workstationBlock == null)
                    return true;

                var requiredLevel = GameReflection.GetRequiredLevelForWorkstationBlock(workstationBlock);
                var block = GameReflection.GetBlockFromBlockValue(workstationBlock);
                var blockNameForMap = GameReflection.GetBlockNameForMap(block);

                var player = GameReflection.GetLocalPlayer() as EntityAlive;
                var playerLevel = player != null ? GameReflection.GetPlayerCraftingLevel(player, "Workstations") : -1;

                if (requiredLevel <= 0)
                    return true;
                if (player == null)
                    return true;

                if (!LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true))
                    return true;

                var displayName = string.IsNullOrWhiteSpace(blockNameForMap) ? "workstation" : blockNameForMap;
                RestrictionFeedback.ShowRestrictionPopup(player, displayName, "Workstations", playerLevel, requiredLevel);

                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] Workstation UI BLOCKED via OnOpen (Workstations " + playerLevel + "/" + requiredLevel + ")");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] WorkstationWindowOnOpenRestrictionPatch error: " + ex.Message);
                return true;
            }
        }

        private static object GetWorkstationBlockFromWindow(object window)
        {
            try
            {
                var t = window.GetType();
                var prop = t.GetProperty("workstationBlock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null)
                    return prop.GetValue(window, null);
                var field = t.GetField("workstationBlock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    return field.GetValue(window);
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}

