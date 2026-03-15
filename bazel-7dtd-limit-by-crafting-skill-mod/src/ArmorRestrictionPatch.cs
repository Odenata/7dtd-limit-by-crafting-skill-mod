using System;
using System.Reflection;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Prevents placing restricted armor into player equipment slots.
    /// Slot layout (which indices are armor) is game-dependent; see docs/GAME_API_NOTES.md.
    /// </summary>
    [HarmonyPatch(typeof(Bag), "SetSlot", new Type[] { typeof(int), typeof(ItemStack), typeof(bool) })]
    internal static class ArmorRestrictionPatch
    {
        private const int ArmorSlotStart = 0;
        private const int ArmorSlotCount = 6;

        static bool Prefix(Bag __instance, int index, ItemStack itemStack)
        {
            if (itemStack == null || itemStack.IsEmpty()) return true;
            if (index < ArmorSlotStart || index >= ArmorSlotStart + ArmorSlotCount) return true;

            var player = GetLocalPlayer();
            if (player == null) return true;
            var equipmentBag = GetEquipmentBag(player);
            if (equipmentBag != __instance) return true;

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

        private static Bag GetEquipmentBag(EntityAlive entity)
        {
            if (entity == null) return null;
            try
            {
                var bagProp = entity.GetType().GetProperty("bag", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return bagProp?.GetValue(entity, null) as Bag;
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
