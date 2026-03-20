using System;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Blocks the drive action (entering as driver) when the player's Vehicles level is below the
    /// vehicle's required level. Allows open inventory, refuel, pick up, passenger. Shows the red restriction popup.
    /// </summary>
    internal static class VehicleDriveRestrictionPatch
    {
        /// <summary>
        /// Prefix for EntityDriveable.EnterVehicle(EntityAlive _entity). Return false to skip attaching (drive blocked).
        /// </summary>
        public static bool Prefix(object __instance, object _entity)
        {
            try
            {
                if (ModConfig.Instance == null || !ModConfig.Instance.IsRestrictionEnabledForSkill("Vehicles"))
                    return true;
                if (_entity == null || __instance == null)
                    return true;

                var player = _entity as EntityAlive;
                if (player == null)
                    return true;
                if (player.GetType().Name.IndexOf("EntityPlayerLocal", StringComparison.Ordinal) < 0)
                    return true;

                var requiredLevel = GameReflection.GetRequiredLevelForVehicleEntity(__instance);
                if (requiredLevel <= 0)
                {
                    return true;
                }

                var playerLevel = GameReflection.GetPlayerCraftingLevel(player, "Vehicles");
                if (!LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true))
                {
                    return true;
                }

                var vehicleDisplayName = GameReflection.GetVehicleEntityDisplayName(__instance);
                RestrictionFeedback.ShowRestrictionPopup(player, vehicleDisplayName, "Vehicles", playerLevel, requiredLevel);

                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] Vehicle drive BLOCKED (Vehicles " + playerLevel + "/" + requiredLevel + ")");
                return false;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] Vehicle drive patch error: " + ex.Message);
                return true;
            }
        }
    }
}
