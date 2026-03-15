using System;
using System.Reflection;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    [HarmonyPatch(typeof(Inventory), "SetItem", new Type[] { typeof(int), typeof(ItemStack) })]
    internal static class HotbarRestrictionPatch
    {
        private const int HotbarSlotCount = 10;

        static bool Prefix(Inventory __instance, int idx, ItemStack itemStack)
        {
            if (itemStack == null || itemStack.IsEmpty()) return true;
            var player = GetLocalPlayer();
            if (player == null) return true;
            var playerInv = GetInventory(player);
            if (playerInv != __instance) return true;
            if (idx < 0 || idx >= HotbarSlotCount) return true;

            var itemValue = GetItemValue(itemStack);
            if (itemValue == null) return true;
            var itemClass = itemValue.ItemClass;
            if (itemClass == null) return true;

            var skillGroup = GameReflection.GetCraftingSkillGroup(itemClass);
            if (string.IsNullOrWhiteSpace(skillGroup)) return true;
            if (!ModConfig.Instance.IsRestrictionEnabledForSkill(skillGroup)) return true;

            var requiredLevel = GameReflection.GetRequiredLevelForItem(itemClass, itemValue);
            var playerLevel = GameReflection.GetPlayerCraftingLevel(player, skillGroup);
            if (LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true))
            {
                return false;
            }
            return true;
        }

        private static EntityAlive GetLocalPlayer()
        {
            try
            {
                var gmType = typeof(GameManager);
                var instProp = gmType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (instProp == null) return null;
                var gm = instProp.GetValue(null, null);
                if (gm == null) return null;
                var gmT = gm.GetType();
                var playerField = gmT.GetField("myEntityPlayerLocal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? gmT.GetField("MyEntityPlayerLocal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (playerField == null) return null;
                return playerField.GetValue(gm) as EntityAlive;
            }
            catch
            {
                return null;
            }
        }

        private static Inventory GetInventory(EntityAlive entity)
        {
            if (entity == null) return null;
            try
            {
                var prop = entity.GetType().GetProperty("inventory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
