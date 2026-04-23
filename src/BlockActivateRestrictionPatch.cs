using System;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Prefix helpers for Block.ActivateBlock / ActivateBlockOnce (currently not registered in ModApi —
    /// skipping those methods broke client interaction until rejoin). Kept for a possible safer revival.
    /// Prefix for Block.ActivateBlock and Block.ActivateBlockOnce so we restrict workstation opens
    /// (e.g. Chemistry Station) even when the game does not call OnBlockActivated on BlockWorkstation or Block.
    /// This is the single entry point for "use block" before the block's activation logic runs.
    /// </summary>
    internal static class BlockActivateRestrictionPatch
    {
        /// <summary>
        /// Prefix for Block.ActivateBlock(WorldBase, int, Vector3i, BlockValue, bool, bool).
        /// Uses Harmony's __args to avoid compile-time dependency on WorldBase/Vector3i/BlockValue types.
        /// </summary>
        public static bool PrefixActivateBlock(object __instance, object[] __args, ref bool __result)
        {
            // args: [0]=world, [1]=cIdx, [2]=blockPos, [3]=blockValue, [4]=isOn, [5]=isPowered
            var blockValue = (__args != null && __args.Length > 3) ? __args[3] : null;
            return CheckAndRestrict(blockValue, ref __result, "ActivateBlock");
        }

        /// <summary>
        /// Prefix for Block.ActivateBlockOnce(WorldBase, int, Vector3i, BlockValue).
        /// Uses Harmony's __args to avoid compile-time dependency on WorldBase/Vector3i/BlockValue types.
        /// </summary>
        public static bool PrefixActivateBlockOnce(object __instance, object[] __args, ref bool __result)
        {
            // args: [0]=world, [1]=cIdx, [2]=blockPos, [3]=blockValue
            var blockValue = (__args != null && __args.Length > 3) ? __args[3] : null;
            return CheckAndRestrict(blockValue, ref __result, "ActivateBlockOnce");
        }

        private static bool CheckAndRestrict(object _blockValue, ref bool __result, string methodName)
        {
            try
            {
                var block = _blockValue != null ? GameReflection.GetBlockFromBlockValue(_blockValue) : null;
                var blockTypeName = block?.GetType().Name ?? "null";
                var blockNameForMap = block != null ? GameReflection.GetBlockNameForMap(block) : "null";
                var requiredLevel = _blockValue != null ? GameReflection.GetRequiredLevelForWorkstationBlock(_blockValue) : -1;
                var player = GameReflection.GetLocalPlayer() as EntityAlive;
                var playerLevel = player != null ? GameReflection.GetPlayerCraftingLevel(player, "Workstations") : -1;

                if (ModConfig.Instance == null || !ModConfig.Instance.IsRestrictionEnabledForSkill("Workstations"))
                    return true;
                if (_blockValue == null)
                    return true;

                if (requiredLevel <= 0)
                {
                    return true;
                }

                if (player == null)
                    return true;

                if (!LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true))
                {
                    return true;
                }

                var displayName = string.IsNullOrWhiteSpace(blockNameForMap) ? "workstation" : blockNameForMap;
                RestrictionFeedback.ShowRestrictionPopup(player, displayName, "Workstations", playerLevel, requiredLevel);

                if (ModConfig.Instance?.DebugMode == true)
                    ModApi.DebugLog("[LimitByCraftingSkill] Workstation activate BLOCKED via " + methodName + " (Workstations " + playerLevel + "/" + requiredLevel + ")");

                __result = false;
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance?.DebugMode == true)
                    ModApi.DebugLog("[LimitByCraftingSkill] BlockActivateRestrictionPatch error: " + ex.Message);
                return true;
            }
        }
    }
}
