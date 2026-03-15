using System.Reflection;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Reflection-based access to game types for player crafting level and item required level.
    /// Hides member names so the mod can tolerate game version drift; see docs/GAME_API_NOTES.md and RUNTIME_API_MISMATCH_DEBUGGING in dev-tools.
    /// </summary>
    internal static class GameReflection
    {
        /// <summary>
        /// Gets the player's current level for the given crafting skill group name.
        /// Returns 0 if the entity or skill cannot be resolved (e.g. in mocks).
        /// </summary>
        internal static int GetPlayerCraftingLevel(EntityAlive entity, string craftingSkillGroup)
        {
            if (entity == null || string.IsNullOrWhiteSpace(craftingSkillGroup)) return 0;
            try
            {
                var entityType = entity.GetType();
                var progressionProp = entityType.GetProperty("Progression", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (progressionProp == null) return 0;
                var progression = progressionProp.GetValue(entity, null);
                if (progression == null) return 0;
                var progType = progression.GetType();
                var getValueMethod = progType.GetMethod("GetProgressionValue", new[] { typeof(string) });
                if (getValueMethod == null) return 0;
                var pv = getValueMethod.Invoke(progression, new object[] { craftingSkillGroup });
                if (pv == null) return 0;
                var levelProp = pv.GetType().GetProperty("Level", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (levelProp == null) return 0;
                var level = levelProp.GetValue(pv, null);
                return level is int i ? i : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the crafting skill group name for the item class (e.g. "HarvestingTools", "Armor").
        /// Tries CraftingSkillGroup then Group. Returns null if not found.
        /// </summary>
        internal static string GetCraftingSkillGroup(ItemClass itemClass)
        {
            if (itemClass == null) return null;
            var t = itemClass.GetType();
            var f = t.GetField("CraftingSkillGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                var v = f.GetValue(itemClass);
                return v as string;
            }
            var p = t.GetProperty("CraftingSkillGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null)
            {
                var v = p.GetValue(itemClass, null);
                return v as string;
            }
            return itemClass.Group;
        }

        /// <summary>
        /// Gets the quality value for the item (0 if no quality). Uses reflection for game compatibility.
        /// </summary>
        internal static int GetQuality(ItemValue itemValue)
        {
            if (itemValue == null) return 0;
            var t = itemValue.GetType();
            var f = t.GetField("Quality", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                var v = f.GetValue(itemValue);
                if (v is ushort u) return u;
                if (v is int i) return i;
            }
            var p = t.GetProperty("Quality", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null)
            {
                var v = p.GetValue(itemValue, null);
                if (v is ushort u) return u;
                if (v is int i) return i;
            }
            return 0;
        }

        /// <summary>
        /// Returns whether the item has a quality attribute (affects whether we apply level restriction).
        /// </summary>
        internal static bool HasQuality(ItemValue itemValue)
        {
            if (itemValue == null) return false;
            var t = itemValue.GetType();
            var p = t.GetProperty("HasQuality", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null)
            {
                var v = p.GetValue(itemValue, null);
                return v is bool b && b;
            }
            return GetQuality(itemValue) > 0;
        }

        /// <summary>
        /// Gets the minimum crafting level required to use this item.
        /// Uses progression data when available; otherwise uses quality as a stand-in (required level = quality).
        /// Returns 0 if the item has no level requirement (e.g. no quality, or skill group empty).
        /// </summary>
        internal static int GetRequiredLevelForItem(ItemClass itemClass, ItemValue itemValue)
        {
            if (itemClass == null || itemValue == null) return 0;
            var skillGroup = GetCraftingSkillGroup(itemClass);
            if (string.IsNullOrWhiteSpace(skillGroup)) return 0;
            if (!HasQuality(itemValue)) return 0;
            var quality = GetQuality(itemValue);
            if (quality <= 0) return 0;
            return quality;
        }
    }
}
