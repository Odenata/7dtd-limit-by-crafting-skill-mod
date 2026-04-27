using System;
using System.Reflection;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Non-shipping diagnostic helper for Equipment.SetSlotItem behavior.
    /// Do not Harmony-patch this in normal builds: it never blocks, and SetSlotItem is an unsafe item-loss
    /// point because the source stack may already have been changed. Blocking happens at earlier UI hooks.
    /// </summary>
    internal static class EquipmentRestrictionPatch
    {
        static bool Prefix(Equipment __instance, int index, ItemValue value, bool isLocal)
        {
            try
            {
                if (value == null || value.IsEmpty()) return true;
                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog($"Equipment SetSlotItem invoked slot={index}");
                var player = GameReflection.GetLocalPlayer();
                if (player == null)
                {
                    if (ModConfig.Instance.DebugMode) ModApi.DebugLog($"Equipment slot={index}: no local player");
                    return true;
                }
                var playerEquipment = GetPlayerEquipment(player);
                if (playerEquipment != __instance)
                {
                    if (ModConfig.Instance.DebugMode) ModApi.DebugLog($"Equipment slot={index}: not local player equipment (patch skipped)");
                    return true;
                }

                var itemClass = value.ItemClass;
                if (itemClass == null) return true;

                var skillGroup = GameReflection.GetCraftingSkillGroup(itemClass, value);
                if (string.IsNullOrWhiteSpace(skillGroup))
                {
                    if (ModConfig.Instance.DebugMode)
                        ModApi.DebugLog($"Equipment SetSlotItem: no skill group for slot {index}");
                    return true;
                }
                var configName = GameReflection.ToProgressionOrConfigName(skillGroup);
                if (!ModConfig.Instance.IsRestrictionEnabledForSkill(configName)) return true;

                var requiredLevel = GameReflection.GetRequiredLevelForItem(itemClass, value);
                var playerLevel = GameReflection.GetPlayerCraftingLevel(player, skillGroup);
                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog($"Equipment slot={index} skill={configName} playerLvl={playerLevel} requiredLvl={requiredLevel}");
                if (LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true))
                {
                    if (ModConfig.Instance.DebugMode)
                    {
                        ModApi.DebugLog($"Equipment would block slot {index}: {configName} player={playerLevel} required={requiredLevel} (allowing to prevent item loss)");
                        ModApi.DebugLog("SetSlotItem call stack: " + Environment.StackTrace);
                    }
                    // Do NOT return false here: the game has already removed the item from source; skipping SetSlotItem would delete it.
                }
                return true;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog($"Equipment patch error: {ex.Message}");
                return true;
            }
        }

        private static Equipment GetPlayerEquipment(EntityAlive entity)
        {
            if (entity == null) return null;
            try
            {
                var t = entity.GetType();
                var field = t.GetField("equipment", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    var v = field.GetValue(entity);
                    if (v is Equipment eq) return eq;
                }
                var prop = t.GetProperty("equipment", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop?.GetValue(entity, null) as Equipment;
            }
            catch
            {
                return null;
            }
        }
    }
}
