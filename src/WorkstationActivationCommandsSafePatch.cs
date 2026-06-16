using System;
using System.Reflection;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// v3 forge blocks use <see cref="BlockWorkstation"/> activation helpers that call
    /// <c>TileEntityWorkstation.InputIsEmpty()</c>. A TE with a null <c>input</c> array (e.g. after a prior
    /// half-open UI session corrupted the save) throws when the radial menu builds activation commands.
    /// Swallow and return the block's default command list so restriction popups still work cleanly.
    /// </summary>
    internal static class WorkstationActivationCommandsSafePatch
    {
        private static readonly FieldInfo CmdsField = ResolveCmdsField();

        private static FieldInfo ResolveCmdsField()
        {
            try
            {
                var workstationType = typeof(Equipment).Assembly.GetType("BlockWorkstation");
                return workstationType?.GetField("cmds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            catch
            {
                return null;
            }
        }

        public static Exception FinalizerGetBlockActivationCommands(object __instance, Exception __exception, ref object __result)
        {
            if (__exception == null)
                return null;

            if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                ModApi.DebugLog("[LimitByCraftingSkill] GetBlockActivationCommands finalizer: " + __exception.Message);

            if (__result == null && __instance != null && CmdsField != null)
                __result = CmdsField.GetValue(__instance);

            return null;
        }

        /// <summary>
        /// <see cref="BlockForge.GetActivationText"/> uses the same unsafe <c>InputIsEmpty</c> path when smelter UI is enabled.
        /// </summary>
        public static Exception FinalizerForgeGetActivationText(object __instance, object _blockValue, Exception __exception, ref string __result)
        {
            if (__exception == null)
                return null;

            if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                ModApi.DebugLog("[LimitByCraftingSkill] BlockForge.GetActivationText finalizer: " + __exception.Message);

            if (string.IsNullOrEmpty(__result) && _blockValue != null)
            {
                try
                {
                    var block = GameReflection.GetBlockFromBlockValue(_blockValue);
                    var name = GameReflection.GetBlockNameForMap(block);
                    if (!string.IsNullOrWhiteSpace(name))
                        __result = name;
                }
                catch
                {
                    // ignored
                }
            }

            return null;
        }
    }
}
