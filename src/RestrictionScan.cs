using System;
using System.Collections.Generic;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Hermetic restriction evaluation over item stacks (no UI, no GetLocalPlayer).
    /// Shared by grid coloring and Stopwatch benchmarks.
    /// </summary>
    internal static class RestrictionScan
    {
        public readonly struct Result
        {
            public readonly int Evaluated;
            public readonly int Restricted;

            public Result(int evaluated, int restricted)
            {
                Evaluated = evaluated;
                Restricted = restricted;
            }
        }

        private static readonly object MemoLock = new object();
        private static Dictionary<string, int> _requiredLevelMemo;

        /// <summary>Clear cross-frame required-level memo (config/map reload or explicit invalidation).</summary>
        public static void ClearCaches()
        {
            lock (MemoLock)
            {
                _requiredLevelMemo = null;
            }
        }

        /// <summary>Stable cache key: mapKey + quality + skillGroup.</summary>
        public static string MakeRequiredLevelKey(string mapKey, int quality, string skillGroup)
        {
            return (mapKey ?? "") + "\u0001" + quality.ToString() + "\u0001" + (skillGroup ?? "");
        }

        /// <summary>
        /// Evaluate stacks with injected progression and player skill levels.
        /// When <paramref name="useRequiredLevelMemo"/> is true, required levels are memoized across calls until <see cref="ClearCaches"/>.
        /// </summary>
        public static Result Evaluate(
            IReadOnlyList<ItemStack> stacks,
            object progression,
            Func<string, int> getPlayerCraftingLevel,
            bool useRequiredLevelMemo = false,
            bool[] restrictedOut = null)
        {
            if (stacks == null || getPlayerCraftingLevel == null)
                return new Result(0, 0);

            var skillCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var scanRequiredCache = useRequiredLevelMemo
                ? null
                : new Dictionary<string, int>(StringComparer.Ordinal);

            int evaluated = 0;
            int restricted = 0;
            for (int i = 0; i < stacks.Count; i++)
            {
                var stack = stacks[i];
                if (stack == null || stack.IsEmpty())
                {
                    if (restrictedOut != null && i < restrictedOut.Length)
                        restrictedOut[i] = false;
                    continue;
                }

                var itemValue = RestrictionHelper.GetItemValue(stack);
                if (itemValue == null)
                {
                    if (restrictedOut != null && i < restrictedOut.Length)
                        restrictedOut[i] = false;
                    continue;
                }

                var itemClass = itemValue.ItemClass;
                if (itemClass == null)
                {
                    if (restrictedOut != null && i < restrictedOut.Length)
                        restrictedOut[i] = false;
                    continue;
                }

                evaluated++;
                bool isRestricted = EvaluateOne(
                    itemClass,
                    itemValue,
                    progression,
                    getPlayerCraftingLevel,
                    skillCache,
                    useRequiredLevelMemo,
                    scanRequiredCache);

                if (isRestricted)
                    restricted++;
                if (restrictedOut != null && i < restrictedOut.Length)
                    restrictedOut[i] = isRestricted;
            }

            return new Result(evaluated, restricted);
        }

        /// <summary>
        /// Core per-item check used by live grid path and hermetic scan.
        /// </summary>
        public static bool EvaluateOne(
            ItemClass itemClass,
            ItemValue itemValue,
            object progression,
            Func<string, int> getPlayerCraftingLevel,
            IDictionary<string, int> skillLevelCache = null,
            bool useRequiredLevelMemo = false,
            IDictionary<string, int> requiredLevelCache = null)
        {
            if (itemClass == null || itemValue == null || getPlayerCraftingLevel == null)
                return false;

            var skillGroup = GameReflection.GetCraftingSkillGroup(itemClass, itemValue);
            if (string.IsNullOrWhiteSpace(skillGroup))
                return false;
            if (ModConfig.Instance == null)
                return false;
            if (!IsRestrictionEnabledForSkillGroup(skillGroup))
                return false;

            int playerLevel;
            if (skillLevelCache != null)
            {
                if (!skillLevelCache.TryGetValue(skillGroup, out playerLevel))
                {
                    playerLevel = getPlayerCraftingLevel(skillGroup);
                    skillLevelCache[skillGroup] = playerLevel;
                }
            }
            else
            {
                playerLevel = getPlayerCraftingLevel(skillGroup);
            }

            int requiredLevel = GetRequiredLevelCached(
                itemClass,
                itemValue,
                skillGroup,
                progression,
                useRequiredLevelMemo,
                requiredLevelCache);

            return LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true);
        }

        private static int GetRequiredLevelCached(
            ItemClass itemClass,
            ItemValue itemValue,
            string skillGroup,
            object progression,
            bool useRequiredLevelMemo,
            IDictionary<string, int> requiredLevelCache)
        {
            var mapKey = itemClass.Name ?? "";
            int quality = GameReflection.GetQuality(itemValue);
            var key = MakeRequiredLevelKey(mapKey, quality, skillGroup);

            if (useRequiredLevelMemo)
            {
                lock (MemoLock)
                {
                    if (_requiredLevelMemo != null && _requiredLevelMemo.TryGetValue(key, out var memoized))
                        return memoized;
                }
            }

            if (requiredLevelCache != null && requiredLevelCache.TryGetValue(key, out var scanCached))
                return scanCached;

            int required = GameReflection.GetRequiredLevelForItemForUnitTest(itemClass, itemValue, progression);

            if (useRequiredLevelMemo)
            {
                lock (MemoLock)
                {
                    if (_requiredLevelMemo == null)
                        _requiredLevelMemo = new Dictionary<string, int>(StringComparer.Ordinal);
                    _requiredLevelMemo[key] = required;
                }
            }

            if (requiredLevelCache != null)
                requiredLevelCache[key] = required;

            return required;
        }

        private static bool IsRestrictionEnabledForSkillGroup(string skillGroup)
        {
            if (string.IsNullOrWhiteSpace(skillGroup) || ModConfig.Instance == null) return false;
            var configName = GameReflection.ToProgressionOrConfigName(skillGroup);
            return ModConfig.Instance.IsRestrictionEnabledForSkill(configName);
        }
    }
}
