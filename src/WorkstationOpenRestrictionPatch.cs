using System;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Blocks opening the placed workstation UI (e.g. press E on Forge/Workbench) when the player's
    /// Workstations level is below the block's required level. Shows the red restriction popup.
    /// </summary>
    internal static class WorkstationOpenRestrictionPatch
    {
        /// <summary>
        /// Prefix for BlockWorkstation.OnBlockActivated(String _commandName, WorldBase, int, Vector3i, BlockValue, EntityPlayerLocal).
        /// Used when the game opens a workstation via a command (e.g. Chemistry Bench). Same logic as Prefix.
        /// </summary>
        public static bool PrefixWithCommand(object __instance, object _commandName, object _world, int _cIdx, object _blockPos, object _blockValue, object _player)
        {
            return Prefix(__instance, _world, _cIdx, _blockPos, _blockValue, _player);
        }

        /// <summary>
        /// Prefix for BlockCompositeTileEntity.OnBlockActivated(String _commandName, WorldBase, int, Vector3i, BlockValue, EntityPlayerLocal).
        /// Harmony parameter name matching can differ across types (_cIdx vs _clrIdx). Use Harmony's index-based param name (__2)
        /// to reliably bind the int argument across both naming variants.
        /// </summary>
        public static bool PrefixWithCommandClrIdx(object __instance, object _commandName, object _world, int __2, object _blockPos, object _blockValue, object _player)
        {
            return Prefix(__instance, _world, __2, _blockPos, _blockValue, _player);
        }

        /// <summary>
        /// Prefix for BlockWorkstation.OnBlockActivated(WorldBase, int, Vector3i, BlockValue, EntityPlayerLocal).
        /// Return false to skip opening the UI and show popup when restricted.
        /// </summary>
        public static bool Prefix(object __instance, object _world, int _cIdx, object _blockPos, object _blockValue, object _player)
        {
            try
            {
                if (ModConfig.Instance == null || !ModConfig.Instance.IsRestrictionEnabledForSkill("Workstations"))
                    return true;
                if (_blockValue == null || _player == null)
                    return true;

                var requiredLevel = GameReflection.GetRequiredLevelForWorkstationBlock(_blockValue);
                if (requiredLevel <= 0)
                {
                    return true;
                }

                var player = _player as EntityAlive;
                if (player == null)
                    return true;

                var playerLevel = GameReflection.GetPlayerCraftingLevel(player, "Workstations");
                if (!LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true))
                {
                    return true;
                }

                var block = GameReflection.GetBlockFromBlockValue(_blockValue);
                var blockName = GameReflection.GetBlockNameForMap(block);
                var displayName = string.IsNullOrWhiteSpace(blockName) ? "workstation" : blockName;
                RestrictionFeedback.ShowRestrictionPopup(player, displayName, "Workstations", playerLevel, requiredLevel);

                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] Workstation open BLOCKED (Workstations " + playerLevel + "/" + requiredLevel + ")");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] Workstation open patch error: " + ex.Message);
                return true;
            }
        }
    }
}
