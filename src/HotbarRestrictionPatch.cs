using System;
using System.Reflection;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Would restrict hotbar by level. We never return false: skipping SetItem after the game has
    /// moved the item can cause item loss. Restriction must be implemented at an earlier hook.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), "SetItem", new Type[] { typeof(int), typeof(ItemStack) })]
    internal static class HotbarRestrictionPatch
    {
        private const int HotbarSlotCountFallback = 10;

        static bool Prefix(Inventory __instance, int _idx, ItemStack _itemStack)
        {
            if (_itemStack == null || _itemStack.IsEmpty()) return true;
            if (ModConfig.Instance.DebugMode)
                ModApi.DebugLog($"Hotbar SetItem invoked idx={_idx}");
            var player = GameReflection.GetLocalPlayer();
            if (player == null)
            {
                if (ModConfig.Instance.DebugMode) ModApi.DebugLog("Hotbar SetItem: no local player");
                return true;
            }
            var playerInv = GetInventory(player);
            if (playerInv != __instance)
            {
                if (ModConfig.Instance.DebugMode) ModApi.DebugLog($"Hotbar SetItem: not player inv (idx={_idx})");
                return true;
            }
            int hotbarCount = GetPublicSlotsPlayMode(__instance);
            if (_idx < 0 || _idx >= hotbarCount) return true;

            var itemValue = GetItemValue(_itemStack);
            if (itemValue == null)
            {
                if (ModConfig.Instance.DebugMode) ModApi.DebugLog($"Hotbar SetItem: no itemValue (idx={_idx})");
                return true;
            }
            var itemClass = itemValue.ItemClass;
            if (itemClass == null) return true;

            var skillGroup = GameReflection.GetCraftingSkillGroup(itemClass, itemValue);
            if (string.IsNullOrWhiteSpace(skillGroup)) return true;
            var configName = GameReflection.ToProgressionOrConfigName(skillGroup);
            if (!ModConfig.Instance.IsRestrictionEnabledForSkill(configName)) return true;

            var requiredLevel = GameReflection.GetRequiredLevelForItem(itemClass, itemValue);
            var playerLevel = GameReflection.GetPlayerCraftingLevel(player, skillGroup);
            if (LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true))
            {
                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog($"Hotbar would block idx={_idx}: {configName} player={playerLevel} required={requiredLevel} (allowing to prevent item loss)");
                // Do NOT return false: can cause item loss; restrict at an earlier hook instead.
            }
            return true;
        }

        private static int GetPublicSlotsPlayMode(Inventory inv)
        {
            if (inv == null) return HotbarSlotCountFallback;
            try
            {
                var t = inv.GetType();
                var prop = t.GetProperty("PUBLIC_SLOTS_PLAYMODE", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null)
                {
                    var v = prop.GetValue(inv, null);
                    if (v is int i && i > 0) return i;
                }
            }
            catch { }
            return HotbarSlotCountFallback;
        }

        private static Inventory GetInventory(EntityAlive entity)
        {
            if (entity == null) return null;
            try
            {
                var t = entity.GetType();
                var field = t.GetField("inventory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    var v = field.GetValue(entity);
                    if (v is Inventory inv) return inv;
                }
                var prop = t.GetProperty("inventory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop?.GetValue(entity, null) as Inventory;
            }
            catch
            {
                return null;
            }
        }

        private static ItemValue GetItemValue(ItemStack stack)
        {
            if (stack == null) return null;
            try
            {
                var prop = stack.GetType().GetProperty("itemValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop?.GetValue(stack, null) as ItemValue;
            }
            catch
            {
                return null;
            }
        }
    }
}
