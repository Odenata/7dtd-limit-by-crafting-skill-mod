using System;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Blocks opening collector-style workstation UIs when the player's Workstations level is below the block's required level.
    /// Broad BlockWorkstation prefixes are intentionally not registered; this logic is currently used by the narrow
    /// BlockCollector path (Dew Collector / Apiary), where UI-only hooks can miss the primary activation flow.
    /// </summary>
    internal static class WorkstationOpenRestrictionPatch
    {
        /// <summary>
        /// Prefix for BlockCollector.OnBlockActivated(String _commandName, WorldBase, int, Vector3i, BlockValue, EntityPlayerLocal).
        /// Same logic as Prefix, with the command-name argument ignored.
        /// </summary>
        public static bool PrefixWithCommand(object __instance, object _commandName, object _world, int _cIdx, object _blockPos, object _blockValue, object _player)
        {
            return Prefix(__instance, _world, _cIdx, _blockPos, _blockValue, _player);
        }

        /// <summary>
        /// Prefix for BlockCollector.OnBlockActivated(WorldBase, int, Vector3i, BlockValue, EntityPlayerLocal).
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
