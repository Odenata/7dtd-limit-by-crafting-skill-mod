using System;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Blocks opening the workstation UI when GameManager.workstationOpened is called (single choke point for all workstations
    /// including Chemistry Bench). When the player's Workstations level is below the block's required level, shows the red popup and skips opening.
    /// </summary>
    internal static class WorkstationOpenedRestrictionPatch
    {
        /// <summary>
        /// Prefix for GameManager.workstationOpened(TileEntityWorkstation _te, LocalPlayerUI _playerUI). Return false to skip opening the UI.
        /// </summary>
        public static bool Prefix(object _te, object _playerUI)
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
                    ModApi.DebugLog("[LimitByCraftingSkill] Workstation open BLOCKED via workstationOpened (Workstations " + playerLevel + "/" + requiredLevel + ")");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] WorkstationOpenedRestrictionPatch error: " + ex.Message);
                return true;
            }
        }
    }
}
