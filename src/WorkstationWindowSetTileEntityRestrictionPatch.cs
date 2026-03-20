using System;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// When the workstation UI window is given a tile entity (SetTileEntity), check restriction before binding.
    /// If the player's Workstations level is below the block's required level, show the red popup and skip binding (window may show empty; user can close it).
    /// This path is used when the game opens the workstation UI without going through GameManager.workstationOpened.
    /// </summary>
    internal static class WorkstationWindowSetTileEntityRestrictionPatch
    {
        /// <summary>
        /// Prefix for XUiC_WorkstationWindowGroup.SetTileEntity(TileEntityWorkstation _te). Return false to skip binding the TE when restricted.
        /// </summary>
        public static bool Prefix(object _te)
        {
            try
            {
                if (ModConfig.Instance == null || !ModConfig.Instance.IsRestrictionEnabledForSkill("Workstations"))
                    return true;
                if (_te == null)
                    return true;

                var blockValue = GameReflection.GetBlockValueFromTileEntity(_te);
                if (blockValue == null)
                    return true;

                var block = GameReflection.GetBlockFromBlockValue(blockValue);
                var requiredLevel = GameReflection.GetRequiredLevelForWorkstationBlock(blockValue);
                if (requiredLevel <= 0)
                    return true;

                var player = GameReflection.GetLocalPlayer() as EntityAlive;
                if (player == null)
                    return true;

                var playerLevel = GameReflection.GetPlayerCraftingLevel(player, "Workstations");
                if (!LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true))
                    return true;

                var blockName = GameReflection.GetBlockNameForMap(block);
                var displayName = string.IsNullOrWhiteSpace(blockName) ? "workstation" : blockName;
                RestrictionFeedback.ShowRestrictionPopup(player, displayName, "Workstations", playerLevel, requiredLevel);

                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] Workstation UI BLOCKED via SetTileEntity (Workstations " + playerLevel + "/" + requiredLevel + ")");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] WorkstationWindowSetTileEntityRestrictionPatch error: " + ex.Message);
                return true;
            }
        }
    }
}
